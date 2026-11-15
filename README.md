# Repodog .NET Kickstarter

An opinionated ASP.NET Core API starter with Identity authentication, roles/policies, feature management, MediatR, FluentValidation, SQL Server, structured logging and integration tests. It works with [repodog-nextjs-kickstarter](https://github.com/badbatch/repodog-nextjs-kickstarter) through a shared endpoint contract, and can serve another frontend implementing that contract.

## Install

Install the .NET SDK from `.tool-versions` with [mise](https://mise.jdx.dev/getting-started.html). Docker is needed for local SQL Server and the integration-test database containers.

```sh
mise install
dotnet tool restore
dotnet restore Kickstarter.sln
cp .env.example .env
```

The local Docker setup includes SQL Server for application data and Mailpit for viewing development emails. It also offers an optional log-viewing stack: an OpenTelemetry collector forwards API logs to Loki for storage, and Grafana provides a browser interface for searching them. Those three logging services only start when you enable the `observability` profile; the API logs to the console without them.

Configure `.env` before starting Compose:

- `MSSQL_SA_PASSWORD`: the SQL Server administrator password.
- `GRAFANA_ADMIN_PASSWORD`: the password for Grafana's local administrator account. The current Compose file requires this value even when the optional logging services are disabled, because variable interpolation happens before profile selection. Setting it does not start Grafana.
- Optionally set `BOOTSTRAP_USER_EMAIL` and `BOOTSTRAP_USER_PASSWORD` to seed a privileged example account.

Do not copy credentials into application source. See [Logging and observability](#logging-and-observability) for how to start and use the optional logging services.

## Run

```sh
docker compose up --build
```

| Service                            | Local address                           |
| ---------------------------------- | --------------------------------------- |
| API                                | <http://localhost:8080>                 |
| Swagger UI, Development only       | <http://localhost:8080/swagger>         |
| OpenAPI document, Development only | <http://localhost:8080/openapi/v1.json> |
| Mailpit email inbox                | <http://localhost:8025>                 |

Compose configures the API to connect to SQL Server through `db` and SMTP through `mailpit`. SQL state is retained in the named volume; `docker compose down` stops services without deleting that state.

To run the API outside Docker, make SQL Server available, set `ConnectionStrings__ApplicationDb`, and run:

```sh
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:8080 \
dotnet run --project src/Kickstarter.Api
```

These environment values select the Development host and the same port used by Compose. The host-run process uses its own environment configuration, not the Compose service settings. Host-run SMTP defaults to `localhost:1025`; configure a reachable SMTP relay when testing account recovery.

## Test and validate

```sh
dotnet csharpier check .
dotnet build Kickstarter.sln --configuration Release --no-restore --warnaserror
dotnet test Kickstarter.sln --configuration Release --no-build --settings Coverage.runsettings
```

The integration tests start disposable SQL Server containers through Testcontainers and require Docker. Fixtures use `WebApplicationFactory`, test data builders, fake time and captured email services. Scenarios cover registration/login, refresh, validation, health, password recovery, roles/policies and access changes after refresh. New tests follow Given/When/Then naming and Arrange/Act/Assert sections.

For the full browser-to-API path, place the Next.js starter beside this repository and run `pnpm test:e2e:docker` there. It builds both apps, provisions disposable accounts/database state and runs Playwright, then removes the stack.

## Structure and coding patterns

| Project                              | Responsibility                                                                                                  |
| ------------------------------------ | --------------------------------------------------------------------------------------------------------------- |
| `src/Kickstarter.Api`                | HTTP contracts at the transport boundary, controllers, authorization, startup and middleware.                   |
| `src/Kickstarter.Application`        | Request/response DTOs, commands, queries, handlers, validation, repository/service interfaces and result types. |
| `src/Kickstarter.Domain`             | Domain entities; initially the Identity application user.                                                       |
| `src/Kickstarter.Infrastructure`     | EF Core persistence, Identity operations and SMTP implementations.                                              |
| `tests/Kickstarter.TestCommon`       | Shared application/database fixtures, builders and test support.                                                |
| `tests/Kickstarter.IntegrationTests` | HTTP/database/auth integration scenarios.                                                                       |

Within `src/Kickstarter.Application`, code is organized into:

- `Features/<Feature>/Commands/`, `Features/<Feature>/Queries/` and `Features/<Feature>/Handlers/` for application operations.
- `Contracts/<Feature>/` for HTTP request and response contracts.
- `Validation/<Feature>/` for request validators.

Persistence and service implementations live in `src/Kickstarter.Infrastructure`. Controllers validate HTTP requests, dispatch application commands or queries through MediatR, and map the results back to HTTP responses.

Keep cancellation asynchronous and propagate cancellation tokens where supported. Prefer injected `TimeProvider` for time-dependent behavior so tests can control time. Central package versions live in `Directory.Packages.props`; nullable reference types and Roslynator are configured centrally. CSharpier supplies consistent formatting.

## Authentication and account recovery

The starter uses ASP.NET Core Identity backed by SQL Server, with unique email addresses and bearer authentication as the default scheme. Account confirmation is not required by the current configuration.

| Endpoint                            | Behavior                                                                                      |
| ----------------------------------- | --------------------------------------------------------------------------------------------- |
| `POST /api/v1/auth/register`        | Accepts `email`, `password` and optional `userName`; creates an ordinary account.             |
| `POST /api/v1/auth/login`           | Accepts credentials; returns `tokenType`, `accessToken`, `expiresIn` and `refreshToken`.      |
| `POST /api/v1/auth/refresh`         | Accepts `refreshToken`; returns replacement tokens.                                           |
| `GET /api/v1/users/me`              | Returns identity, roles and satisfied resource-independent policies.                          |
| `POST /api/v1/auth/forgot-password` | Starts recovery; valid email-shaped requests return 204 without disclosing account existence. |
| `POST /api/v1/auth/reset-password`  | Accepts `email`, `token` and `newPassword` to complete recovery.                              |

Register and sign in separately. In Swagger, call login, copy the access token into the Bearer authorization control and try `/api/v1/users/me` or `/api/v1/example`. Other clients send `Authorization: Bearer <access-token>`. Identity's access/refresh tokens are opaque protected tokens, not JWTs.

Token creation and validation use the Identity bearer handler; application handlers use `IIdentityRepository` to work with identities. Refresh validates the security stamp and builds a new principal. Roles in existing access tokens are snapshots: editing an account's role does not immediately revoke already-issued tokens. There is no separate bearer-token revocation endpoint. Preserve Data Protection keys when deployment instances must continue accepting the same protected tokens.

Password-reset tokens expire after one hour. `AccountRecovery:PasswordResetUrl` controls the frontend link in the email. SMTP settings live under `Email`: host, port, TLS, credentials and sender identity. Local Compose captures emails in Mailpit. The API provides recovery endpoints and email delivery; implementing a reset-password page belongs to the consuming frontend.

## Roles and authorization policies

[AuthorizationPolicies.cs](src/Kickstarter.Api/Authorization/AuthorizationPolicies.cs) defines the `ExampleAccess` policy, requiring authentication and the `ExampleUser` role. [ExampleController.cs](src/Kickstarter.Api/Controllers/ExampleController.cs) demonstrates both access levels:

- `GET /api/v1/example` requires an authenticated account.
- `GET /api/v1/example/restricted` additionally requires `ExampleAccess`.

Use `[Authorize]` for authentication and `[Authorize(Policy = AuthorizationPolicies.ExampleAccess)]` for a named policy. Add new policies through `AuthorizationPolicies.Configure`; let policies express the role/claim requirements rather than introducing a separate permissions vocabulary.

`CurrentUserResponseFactory` evaluates the explicit `CurrentUserPolicies` allowlist with `IAuthorizationService`. `/users/me` returns `id`, `userName`, `email`, `roles` and satisfied `policies`. Only put resource-independent policies in that allowlist; resource-specific authorization still belongs on the operation being performed.

The startup bootstrapper ensures the `ExampleUser` role exists. When both `BootstrapUser:Email` and `BootstrapUser:Password` are configured, it creates the account if needed and assigns that role. Compose maps `BOOTSTRAP_USER_EMAIL` / `BOOTSTRAP_USER_PASSWORD` into those settings. Public registration never grants the role, and leaving both bootstrap values blank creates no example account.

## Feature management

`Microsoft.FeatureManagement.AspNetCore` is registered with `AddFeatureManagement()` in [Program.cs](src/Kickstarter.Api/Program.cs). Flags are read from the `FeatureManagement` configuration section:

```json
{
  "FeatureManagement": {
    "ExampleFeature": false
  }
}
```

Inject `IFeatureManager` into the controller or service that makes the feature decision, then evaluate it asynchronously:

```csharp
var isEnabled = await featureManager.IsEnabledAsync("ExampleFeature");
```

Use the result to choose the supported behavior or return an appropriate unavailable response. Add feature filters when a plain boolean is insufficient. The included `ExampleFeature` is configuration scaffolding: no current endpoint consumes it, so setting it alone does not change the example routes.

Backend feature flags and the Next.js `@next-box/feature-flags` configuration are separate. Their concepts align, but this starter does not synchronize them or expose a flag endpoint. Always enforce authorization independently of feature visibility in a frontend.

## MediatR and application results

MediatR scans the Application assembly for request handlers. A command represents an action; a query represents a read. For example, registration declares an `IRequest<ServiceResult<UserResponse>>`, and `RegisterUserHandler` implements its matching `IRequestHandler`.

The controller dispatches through the mediator:

```csharp
var result = await mediator.Send(
    new RegisterUserCommand(request.Email, request.Password, request.UserName),
    cancellationToken
);

```

Handlers perform application work through interfaces implemented by Infrastructure and return ServiceResult<T> outcomes that controllers map to typed HTTP responses using ApiControllerBase; follow the user and health features for this pattern, while ExampleController demonstrates authorization directly without MediatR.

FluentValidation validators are discovered from the Application assembly. Controllers explicitly call `ValidateAsync` before sending requests through MediatR. Validation is not an automatically installed mediator pipeline, so adding a validator alone does not execute it for a new endpoint. Add the validator dependency and validation call, then write integration cases for invalid input and expected HTTP results.

## Persistence and configuration

`ApplicationDbContext` extends Identity's EF Core context for the application user, GUID keys and Identity roles. SQL Server is configured through `ConnectionStrings:ApplicationDb`. Add entities and EF configuration in the appropriate Domain/Infrastructure locations and register new repository implementations through dependency injection.

Startup currently calls `EnsureCreatedAsync` to initialize an empty database. It does not apply schema migrations to an existing database. Introduce an EF migrations/baseline workflow and change startup initialization before evolving a persisted deployment's schema.

Configuration uses standard ASP.NET Core sources, including `appsettings.json`, environment-specific settings and environment overrides. Double underscores express nesting:

```dotenv
ConnectionStrings__ApplicationDb=your-connection-string
AccountRecovery__PasswordResetUrl=http://localhost:3000/reset-password
Observability__OtlpEndpoint=http://localhost:4318
FeatureManagement__ExampleFeature=true
```

SMTP and account recovery settings are bound to options types. Use separate environment configuration for infrastructure addresses and secrets rather than hard-coding them in handlers.

## HTTP contracts, OpenAPI and errors

API controllers use explicit URL-segment versioning under `/api/v1/...`; no unversioned controller aliases are provided. Swagger UI and `/openapi/v1.json` are available in Development. Named route operations become predictable generated-client method names, so preserve those names when changing implementation details.

DTOs and typed controller results describe the contract. JSON enums are serialized as camelCase strings rather than numeric values. Validation/application results are mapped by the API base controller. The exception middleware also maps missing resources to 404, argument errors to 400 and invalid-operation/database conflicts to 409; database conflicts return a generic message.

`GET /health` is a process check and `GET /health/database` tests database connectivity. These routes are version-neutral and outside the versioned Swagger document.

## Logging and observability

Serilog reads configured levels, enriches from log context, writes to the console and records HTTP request completion. [RequestLogContextMiddleware.cs](src/Kickstarter.Api/Startup/RequestLogContextMiddleware.cs) adds:

| Input                                    | Log property                                                     |
| ---------------------------------------- | ---------------------------------------------------------------- |
| `X-Correlation-Id`                       | `CorrelationId`, falling back to the ASP.NET request identifier. |
| `X-Request-Id`                           | `ExternalRequestId`, when supplied.                              |
| `X-Feature` or `feature` query parameter | `Feature`, when supplied.                                        |
| Configured route-value names             | Named fields such as `Version` and `Id`.                         |

Identifiers are bounded and checked before entering the log scope. Add useful route keys through `RequestLogging:RouteValueProperties`. `Feature` is a log label, not a feature-management toggle.

Console logging is enabled without any additional services. For a searchable local log history, the optional Compose `observability` profile adds:

- **OpenTelemetry collector**: receives API logs over OTLP and forwards them to Loki.
- **Loki**: stores the logs.
- **Grafana**: provides the browser interface for exploring the logs, with the Loki data source already configured.

To enable it, set these values in `.env`:

```dotenv
OTLP_ENDPOINT=http://otel-collector:4318
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=your-local-grafana-password
```

Then start the application and logging services together:

```sh
docker compose --profile observability up --build
```

Open Grafana at <http://localhost:3001> and sign in with `GRAFANA_ADMIN_USER` and `GRAFANA_ADMIN_PASSWORD`. Make an API request, then open Grafana's Explore view and select the provisioned **Loki** data source to find its logs. The collector and Loki run within Compose; the API sends logs to the collector's service address, not Grafana's browser address.

Compose maps `OTLP_ENDPOINT` into the API's `Observability:OtlpEndpoint` setting, which enables the OpenTelemetry log sink. Leave `OTLP_ENDPOINT` empty for console-only logging with the default `docker compose up --build` command. This wiring exports logs; it does not install a complete tracing/metrics instrumentation pipeline.

## Integration with the Next.js kickstarter

The frontend uses generated clients from this API's OpenAPI document. Its credentials provider signs in here, then calls `/api/v1/users/me`. NextAuth stores API credentials in an encrypted HttpOnly session cookie and exposes only the public identity/access information to browser JavaScript. Authenticated Next.js BFF handlers attach the bearer token when forwarding requests here.

Set the frontend's `KICKSTARTER_SERVER__API__BASE_URL` to the reachable API address. For development against Compose, that is normally `http://localhost:8080`; inside the paired E2E stack, the API service address is `http://api:8080`. The browser only needs the Next.js BFF URL, normally `/api`.

After changing a backend contract, export `/openapi/v1.json` into the frontend's root `openapi.json` and run `pnpm generate` there. For policy changes, keep the API policy definition, current-user policy allowlist and frontend policy constant aligned. The API is the authority for access; UI/BFF checks provide earlier feedback and do not replace backend enforcement.

The BFF sends request and correlation IDs for structured logging. A session correlation ID groups activity while each browser request has its own request ID. A refreshed API identity also updates frontend roles/policies through `/users/me`.

## GitHub Actions

The PR workflow checks formatting, builds with warnings treated as errors and runs .NET tests/coverage. It then calls the reusable E2E workflow in the Next.js repository. The caller's merge commit is paired with the same source branch in the counterpart when present; otherwise `E2E_APP_FALLBACK_REF` selects the frontend fallback, or its default branch is discovered. Resolved commit SHAs are used for checkout.

Configure `CROSS_REPO_READ_TOKEN` with read-only Contents access to both repositories and allow reusable-workflow access from this repository. The frontend workflow must exist on the ref specified by the caller. Fork PRs do not receive repository secrets. Test reports and browser reports are uploaded as artifacts.

## Creating another application

[.repodog-bootstrap.json](.repodog-bootstrap.json) describes the project/namespace replacements and optional pairing used by Repodog's app generator. A supplied counterpart retains cross-repository E2E integration; an omitted counterpart generates a backend with independent validation, ready to serve a frontend of your choice. Neither mode automatically creates commits or publishes GitHub repositories.

### What the manifest does

[.repodog-bootstrap.json](.repodog-bootstrap.json) is the recipe used by `repodog new app` to turn this repository into a named application. The application does not read it at runtime. Maintain it alongside the starter's source files: changing a filename, namespace, workflow or text used by a replacement can require a manifest change too.

The generator downloads the configured GitHub revision, reads the manifest, prepares the application in a temporary directory and copies the completed result to a new destination. It omits the source `.git` directory and the manifest itself. The generated `.repodog-origin.json` records `repository`, requested `ref` and resolved `commit`. No dependency installation, Git initialization, commits or GitHub repository creation happen as part of materialization.

### Generate from this starter

```sh
repodog new app dotnet
```

Answer the application name, description and GitHub owner prompts. Supply the counterpart repository name to keep the paired setup, or leave it blank for independent validation. The CLI also supports configuration defaults and question overrides.

The default source is this starter's GitHub repository at `main`. To test a published branch, tag or commit containing manifest edits, set the source in a project or global `.repodogrc`:

```json
{
  "appBootstraps": {
    "dotnet": {
      "repository": "badbatch/repodog-dotnet-kickstarter",
      "ref": "your-template-branch"
    }
  }
}
```

Replace `your-template-branch` with an existing branch, tag or commit ref. Editing a local starter checkout does not change what the CLI downloads from GitHub. Use a revision containing your changes when testing through the command, and use the CLI's fixture tests for local generator development. Existing generated applications are not updated automatically.

### Manifest fields

| Field          | Required | Meaning                                                                                                                    |
| -------------- | -------- | -------------------------------------------------------------------------------------------------------------------------- |
| `version`      | Yes      | Manifest format version. Currently `1`; this is not the application/package version.                                       |
| `files`        | Yes      | Exact UTF-8 file paths to process with the normal replacements. This is not a list of all files to copy.                   |
| `replacements` | Yes      | Ordered `{ "from": "literal source text", "to": "replacement template" }` rules applied to each listed file.               |
| `renamePaths`  | Yes      | Ordered `{ "from": "old/path", "to": "new/path/template" }` rules for files or directories.                                |
| `exclude`      | Yes      | Exact file or directory paths omitted from the generated application. Excluding a directory also excludes its descendants. |
| `whenMissing`  | No       | A map from variable name to additional exclusions and file-specific replacements, used when that value is absent or blank. |

Arrays can be empty. Paths are relative to the starter root and are literal paths, not glob patterns. Files not listed in `files` are still copied unless excluded; their contents are unchanged unless targeted by a conditional file replacement. Keep binary assets out of `files`.

### Variables and replacement syntax

The application generator supplies these values:

| Variable                | Source or default                                                                                                       |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `name`                  | Application repository/folder name, such as `my-app-ui`.                                                                |
| `description`           | The description answer, when supplied.                                                                                  |
| `owner`                 | GitHub owner answer.                                                                                                    |
| `repository`            | Derived `owner/name`.                                                                                                   |
| `counterpartRepository` | Optional other repository name; blank means standalone generation.                                                      |
| `counterpartPath`       | Derived `../counterpartRepository`, or an empty string when standalone.                                                 |
| `namespace`             | Explicit value or PascalCase `name`, such as `MyAppUi`.                                                                 |
| `configPrefix`          | Explicit value or uppercase `name` with hyphens changed to underscores and a trailing underscore, such as `MY_APP_UI_`. |

The existing Repodog `templateVariables` and `questionOverrides` configuration can provide defaults or additional answers. Custom values remain available to replacement templates. Definitions are selected under `new.app.nextjs` or `new.app.dotnet`, with an optional custom type path for more specific configuration.

Use `{{name}}` for a literal value. Use `{{description|json}}` when replacing an entire JSON string value: the generator adds quotes and escapes embedded quotes/newlines correctly. For example:

```json
{
  "from": "\"Starter description\"",
  "to": "{{description|json}}"
}
```

Do not surround `{{description|json}}` with another pair of JSON quotes in the generated content. Templates can reference variables, but they cannot execute JavaScript or shell commands. `from` is always literal text, not a regex. All occurrences are replaced, and rules run in array order; a later rule can match text produced by an earlier one.

### .NET example

This reduced example illustrates project and namespace replacement. It is not a replacement for this repository's complete manifest:

```json
{
  "version": 1,
  "files": ["Starter.sln", "src/Starter.Api/Starter.Api.csproj"],
  "replacements": [{ "from": "Starter", "to": "{{namespace}}" }],
  "renamePaths": [
    {
      "from": "src/Starter.Api/Starter.Api.csproj",
      "to": "src/Starter.Api/{{namespace}}.Api.csproj"
    },
    { "from": "src/Starter.Api", "to": "src/{{namespace}}.Api" },
    { "from": "Starter.sln", "to": "{{namespace}}.sln" }
  ],
  "exclude": []
}
```

The real recipe includes all relevant C# namespaces, project references, solution entries, test projects, Docker paths and CI commands. When adding a project or a file containing the starter namespace, update both content rules and path renames where necessary. Renaming the `.csproj` before its parent directory keeps subsequent source paths valid.

### Optional counterpart configuration

A `whenMissing` entry contains both an `exclude` array and a `fileReplacements` array. Each file replacement has `file`, `from` and `to` fields:

```json
{
  "whenMissing": {
    "counterpartRepository": {
      "exclude": ["paired-example.txt"],
      "fileReplacements": [
        {
          "file": "README.md",
          "from": "Use the paired example.",
          "to": "Configure your own compatible backend or frontend."
        }
      ]
    }
  }
}
```

This fragment illustrates the shape; use paths and text that actually exist in the starter. A provided counterpart leaves these omission rules inactive. A missing, empty or whitespace-only value activates them. Conditional replacements target their named file directly, even when it is not listed in `files`.

In this starter, `counterpartRepository` controls the paired E2E configuration and related documentation. Omitting it keeps the application's own validation workflow and removes cross-repository E2E dependencies. The Next.js application still expects the documented authentication/API contract; an independently hosted or non-.NET backend can implement it.

### Generation order

1. Read and validate the version-1 manifest.
2. Select applicable `whenMissing` entries and validate exclusion paths.
3. Copy the source, omitting `.git`, the manifest and all applicable exclusions; reject symbolic links.
4. Apply conditional `fileReplacements` to the copied files.
5. Apply normal `replacements` to the remaining files listed in `files`.
6. Apply `renamePaths` in order, then write `.repodog-origin.json`.
7. Copy into a new destination. An existing destination is refused; a partially created destination is removed on failure. Temporary files are cleaned up.

Paths used by replacement and rename operations must stay inside the application directory. Reference original filenames in `files` and conditional replacements because renaming happens afterwards. Rename child files before their parent directories. Do not leave rules targeting files removed by an exclusion.

### Editing and verifying the manifest

Manual edits are expected. They affect future generations from that GitHub revision, not applications already created. Commit and publish a verified manifest change together with the starter change it supports.

When changing the starter:

1. Add a text file to `files` if its contents need customization; leave ordinary copied files and binary assets out.
2. Update literal `from` text and original paths when renaming or rewriting starter files.
3. Keep `renamePaths` ordered from child files to parent directories.
4. Review `whenMissing`, especially workflow, package-script and README replacements, for both standalone and paired output.
5. Generate into fresh temporary destinations with a different name/owner/prefix/namespace. Exercise both a supplied and an omitted counterpart.
6. Inspect generated metadata, links, workflows and configuration, then run the generated application's install, build, validation and relevant E2E commands.

A missing file, invalid manifest, unresolved variable used by a replacement, unsafe path or symbolic link causes generation to fail. However, a valid literal `from` value that no longer appears in a file currently matches nothing silently. Runtime validation checks the structure and path rules; it cannot prove the resulting application is correct. Keep tests for generation behavior and verify actual generated applications after editing the recipe.

### Implementation and type definitions

The manifest contract belongs to the Repodog CLI. In the `repodog` repository:

- `cli/new/src/app/types.ts` defines `BootstrapManifest`, `BootstrapReplacement`, `BootstrapFileReplacement` and `BootstrapOmission`.
- `cli/new/src/app/generateApp.ts` collects answers, resolves variables, downloads the source and calls the materializer.
- `cli/new/src/app/getValidAppVariablesOrThrow.ts` resolves and validates application variable values.
- `cli/new/src/app/materializeApp/materializeApp.ts` coordinates generation; adjacent files implement copying, replacements, renaming and destination cleanup.
- `cli/new/src/app/materializeApp/loadValidBootstrapManifestOrThrow.ts` performs runtime manifest validation.

There is currently no published JSON Schema or `$schema` binding for editor completion. TypeScript definitions are checked when building the CLI; JSON files are checked by the CLI's runtime validator when generating an application. A syntactically valid JSON edit is not, by itself, evidence that a recipe will generate a working app.

## Changelog

Check out the [features, fixes and more](CHANGELOG.md).

## License

Repodog .NET Kickstarter is [MIT Licensed](LICENSE).

# Repodog .NET Kickstarter

An opinionated ASP.NET Core API paired with `repodog-nextjs-kickstarter`. It retains the Domain, Application, Infrastructure, API, TestCommon, and IntegrationTests project boundaries without any product domain.

## Included foundations

- ASP.NET Core Identity with opaque bearer access tokens and refresh tokens.
- Registration, sign-in, current-user identity, and password recovery endpoints.
- Roles and named authorization policies, evaluated by the API and exposed through `/users/me`.
- SQL Server persistence, FluentValidation, MediatR, health checks, API versioning and Swagger/OpenAPI.
- Request/correlation IDs, structured Serilog logging, and optional OpenTelemetry/Loki/Grafana configuration.
- CSharpier, Roslynator, integration fixtures, test data builders, and PR workflows.

## Development

Install the versions in `.tool-versions` with mise. Copy `.env.example` to `.env`, set a strong `MSSQL_SA_PASSWORD`, and set `GRAFANA_ADMIN_PASSWORD` for the optional observability stack. Optionally provide `BOOTSTRAP_USER_EMAIL` and `BOOTSTRAP_USER_PASSWORD` to seed a privileged demonstration account.

```sh
mise install
dotnet tool restore
docker compose up --build
```

The API runs at <http://localhost:8080>, with Swagger at `/swagger` and its document at `/openapi/v1.json`. Mailpit captures recovery emails at <http://localhost:8025>. Add `--profile observability` when starting Compose to include Loki, the OpenTelemetry collector and Grafana; set `OTLP_ENDPOINT=http://otel-collector:4318` to send logs there.

For development outside Docker, configure `ConnectionStrings__ApplicationDb`, then run `dotnet run --project src/Kickstarter.Api`. The database schema is initialized with `EnsureCreated`; replace this with an EF migrations workflow before evolving an existing deployment's schema. The development Compose database persists until you explicitly remove its volume.

## Shared endpoint contract

- `POST /api/v1/auth/register`: email, password and optional user name; returns the created identity.
- `POST /api/v1/auth/login`: email/password; returns `tokenType`, `accessToken`, `expiresIn`, and `refreshToken`.
- `POST /api/v1/auth/refresh`: refresh token; returns replacement tokens.
- `GET /api/v1/users/me`: authenticated identity with roles and satisfied resource-independent policies.
- `POST /api/v1/auth/forgot-password` and `/api/v1/auth/reset-password`: recovery through configured SMTP.
- `GET /api/v1/example`: a working example for any authenticated user.
- `GET /api/v1/example/restricted`: the same pattern with the `ExampleAccess` policy.
- `GET /health` and `/health/database`: process and database health.

`ExampleAccess` requires authentication plus the `ExampleUser` role. It is a demonstration of optional authorization, not a blanket access requirement. The frontend allows ordinary users into its authenticated area and only applies this policy to its restricted example. Public registration never grants roles. `BootstrapUser` can provision the example role/account for local use; leave its credentials unset when you do not want an account seeded.

Tokens contain an identity snapshot. Refresh revalidates the security stamp and creates a new principal; the frontend then reloads `/users/me`. Role changes are reflected after refresh, not immediately in an existing bearer token. Password-reset lifetimes and SMTP settings are configured in the startup/application settings.

## Verification

```sh
dotnet restore Kickstarter.sln
dotnet tool restore
dotnet csharpier check .
dotnet build Kickstarter.sln --configuration Release --no-restore --warnaserror
dotnet test Kickstarter.sln --configuration Release --no-build --settings Coverage.runsettings
```

The integration tests start disposable SQL Server containers and require Docker. They cover identity, validation, health, password recovery, policy evaluation, current-user access information, and permission changes after refresh. Tests use Given/When/Then names and Arrange/Act/Assert sections.

From the sibling frontend, `pnpm test:e2e:docker` builds both applications, provisions disposable accounts and a database, and runs the browser suite. No payment provider, image store, or other business service is required.

## GitHub Actions

The API PR workflow checks formatting, builds with compiler/analyzer warnings treated as errors, runs tests and coverage, then calls the frontend's reusable E2E workflow. A source branch with the same name is selected in the sibling repository when it exists. Otherwise, `E2E_APP_FALLBACK_REF` controls the frontend fallback; its default branch is discovered when unset. The caller's merge commit and resolved counterpart SHA are used for checkout.

Configure a `CROSS_REPO_READ_TOKEN` Actions secret with read-only Contents access to both repositories, and allow workflow sharing from the frontend repository's Actions settings. The shared workflow must be published on the frontend's `main` branch before this caller can resolve it. Update the caller's `owner/repository@ref` when creating a different project pair. No repository secrets or commits are created automatically.

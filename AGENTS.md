# Repository guidance for Codex

- Follow the repository-root `.editorconfig`, `Directory.Build.props`, and `Directory.Packages.props` settings.
- Build with the .NET SDK and C# language version supported by the project target frameworks. Projects target .NET 10 and currently use the SDK's default C# language version.
- Write readable, maintainable, idiomatic C# and preserve the established project conventions.
- Respect nullable reference types. Avoid unnecessary null-forgiving operators and fix nullability issues at their source.
- Dispose owned resources correctly and use asynchronous disposal when available.
- Propagate cancellation tokens through asynchronous operations when the API provides one.
- Use asynchronous APIs correctly and avoid blocking asynchronous work with `.Result`, `.Wait()`, or equivalent calls.
- Avoid unnecessary allocations and repeated enumeration where it harms clarity or performance.
- Follow secure coding practices, including parameterized database access and safe handling of untrusted input. Never add hard-coded secrets.
- Avoid unnecessary abstractions, dependencies, and unrelated refactors.
- Preserve existing behavior unless a change is explicitly requested.
- Add or update tests when observable behavior changes.
- Resolve relevant analyzer diagnostics introduced by a change. Do not suppress diagnostics merely to make the build pass.
- Use the narrowest justified suppression for a genuine exception and explain it at the suppression site.
- Avoid unrelated formatting changes and follow the repository's configured CSharpier and EditorConfig conventions.
- Run builds and relevant tests after modifications. Report commands that could not be executed and never claim validation succeeded unless those commands actually succeeded.

## Git changes

Do not create commits or push changes without explicit user approval.

# Clean Architecture Template (.NET 10)

A .NET 10 API template based on Clean Architecture, custom CQRS (without MediatR),
Carter minimal APIs, PostgreSQL, JWT authentication, Google and Apple sign-in,
Serilog, Seq, OpenTelemetry, xUnit and Testcontainers.

## Architecture

The solution is split into five production projects:

```text
Domain
  ↑
Application
  ↑             ↑
Infrastructure  Presentation
          ↑     ↑
             Web
```

- **Domain** contains entities, enums, constants and domain abstractions. It does
  not reference any outer layer.
- **Application** contains use cases, CQRS contracts, commands, queries,
  handlers, DTOs, validation rules and service/repository interfaces. It
  references Domain only.
- **Infrastructure** implements persistence, repositories, token generation and
  EF Core configuration. It references Application and Domain.
- **Presentation** exposes Carter endpoint modules and translates HTTP input
  into commands and queries. It references Application.
- **Web** is the composition root. It configures dependency injection,
  authentication, authorization, rate limiting, CORS, observability and
  application startup.

These dependency rules are enforced by `ArchitectureTests`; they are not only
documentation.

## Custom CQRS pipeline

The project uses its own CQRS abstractions instead of MediatR:

- `ICommand<TResponse>` and `ICommandHandler<TCommand, TResponse>`;
- `IQuery<TResponse>` and `IQueryHandler<TQuery, TResponse>`;
- `IValidatable` for request validation;
- `Result<T>` for explicit success and failure results.

Handlers are discovered automatically with Scrutor and wrapped with decorators:

1. structured logging;
2. validation and short-circuiting;
3. unhandled-exception conversion to `Result<T>`.

Commands and queries own their validation through `Validate()`. Endpoints remain
thin: they bind the HTTP request, create a command or query and delegate it to
the corresponding handler.

## Enforced naming conventions

The `tests/ArchitectureTests` project uses reflection to protect architectural
and naming conventions:

- domain entity classes end in `Entity`;
- Carter modules end in `Endpoints`, are `sealed`, implement `ICarterModule`
  and expose a private constant `EndpointTag` matching the feature name;
- concrete repositories follow `NameRepository` / `INameRepository`;
- EF Core configurations end in `Configuration`;
- commands and queries are records ending in `Command` or `Query`;
- feature handlers end in `CommandHandler` or `QueryHandler`;
- feature handlers are `internal sealed`;
- DTO names end in `Request` or `Response`;
- inner layers cannot reference forbidden outer layers.

Run them with:

```powershell
dotnet test tests/ArchitectureTests/ArchitectureTests.csproj
```

Add or change a convention in `ArchitectureTests` whenever a structural rule
becomes part of the project contract.

## HTTP endpoints

### Auth

Anonymous endpoints:

| Method | Route                     | Purpose                                                                               |
| ------ | ------------------------- | ------------------------------------------------------------------------------------- |
| `POST` | `/api/auth/sign-in`       | Validate a Google/Apple identity token, register or sign in the user and issue tokens |
| `POST` | `/api/auth/refresh-token` | Validate and rotate a refresh token                                                   |

JWT-protected endpoints:

| Method   | Route                   | Purpose                                             |
| -------- | ----------------------- | --------------------------------------------------- |
| `POST`   | `/api/auth/logout`      | Revoke the refresh token for the requested device   |
| `POST`   | `/api/auth/logout-all`  | Revoke every refresh-token session for the user     |
| `DELETE` | `/api/auth/delete-user` | Delete the authenticated user and associated tokens |

### ToDo

| Method   | Route            | Purpose          |
| -------- | ---------------- | ---------------- |
| `GET`    | `/api/todo/`     | Get all ToDos    |
| `GET`    | `/api/todo/{id}` | Get a ToDo by ID |
| `POST`   | `/api/todo/`     | Create a ToDo    |
| `PATCH`  | `/api/todo/`     | Update a ToDo    |
| `DELETE` | `/api/todo/{id}` | Delete a ToDo    |

ToDo endpoints currently use the anonymous rate-limit policy. Add
`RequireAuthorization()` when ToDo records become user-owned.

## Authentication flow

### Sign-in and registration

1. The client sends the provider, identity token and device ID. Apple sign-in
   also requires the single-use authorization code returned by Apple.
2. `IGoogleAuthService` or `IAppleAuthService` validates the external identity.
3. For Apple, the server exchanges the authorization code at `/auth/token` and
   verifies that the returned identity token has the same Apple subject as the
   identity token supplied by the client.
4. The application loads the user by provider and provider ID.
5. A missing user is registered after checking email uniqueness.
6. The server creates a JWT access token and a cryptographically random refresh
   token.
7. Only the refresh-token hash is stored in PostgreSQL; the raw value is
   returned to the client once.

The API never trusts an email supplied separately by the client. It obtains the
email from the validated provider identity token. Apple returns the full user
object only during the first authorization, while the identity token normally
continues to contain the email claim.

Every Apple sign-in must send both `idToken` and `authorizationCode`. The code
exchange must succeed before the account is created or a login is completed.
The latest Apple refresh token replaces the previous provider credential. It is
encrypted with AES-256-GCM before persistence and decrypted only when the
authorization must be revoked during account deletion.

### Refresh-token rotation

Refresh tokens are bound to a device:

- the incoming raw token is hashed before lookup;
- revoked and expired tokens are rejected;
- the device ID must match;
- successful refresh rotates the token through one atomic conditional update;
- the previous raw token cannot be replayed.

There is one refresh-token row per user/device, enforced by a unique database
index.

### Logout and user deletion

- Logout is idempotent for missing or already-revoked tokens.
- A token cannot be revoked from a different device.
- Logout revokes only the refresh-token session identified by the supplied
  token and device ID.
- `/logout-all` atomically revokes every active refresh-token session belonging
  to the authenticated user.
- Access tokens are stateless and are not queried against PostgreSQL on each
  request. Tokens issued before logout remain valid until their short
  expiration; the client must discard its local access token after logout.
- User deletion requires a valid JWT.
- Deleting a user cascades to local refresh tokens.
- Apple provider tokens are revoked through Apple's `/auth/revoke` endpoint
  before deleting an Apple account.
- Apple deletion fails without removing local data if the provider credential
  is missing or Apple rejects the revocation. A subsequent successful Apple
  sign-in refreshes the stored credential, after which deletion can be retried.

### JWT configuration

Configure these values through user secrets, environment variables or a secure
secret store:

```json
{
    "Jwt": {
        "Issuer": "CCTemplate",
        "Audience": "CCTemplate.Client",
        "SigningKey": "can be generated with this command: openssl rand -base64 64",
        "AccessTokenMinutes": 15,
        "RefreshTokenDays": 60,
        "RefreshTokenBytes": 64,
        "RefreshTokenHmacKey": "can be generated with this command: openssl rand -base64 64",
        "ProviderTokenEncryptionKey": "must decode to exactly 32 bytes; generate with: openssl rand -base64 32"
    }
}
```

Never commit production signing keys, provider private keys or passwords.
Set `Jwt:ProviderTokenEncryptionKey` from a separate secret; do not reuse the
JWT signing key or refresh-token HMAC key.

`appsettings.Development.json` and `docker-compose.override.yml` contain
explicitly fake but structurally valid local values so the application can
start without additional authentication secrets. Replace them through user
secrets when testing real provider login. An Azure deployment should provide
the production values through App Service settings or a Key Vault-backed
configuration provider.

Provider settings live under:

```text
Authentication:Google
Authentication:Apple
```

JWT, encryption-key and provider options are validated during application
startup.

For Apple, set `Audience` to the exact App ID or Services ID used as the
`client_id` during authorization. Store `TeamId`, `KeyId` and the contents of
the Sign in with Apple `.p8` private key in user secrets, environment variables
or a production secret store. For example:

```powershell
dotnet user-secrets set "Authentication:Apple:Audience" "com.example.app" --project src/Web
dotnet user-secrets set "Authentication:Apple:TeamId" "YOUR_TEAM_ID" --project src/Web
dotnet user-secrets set "Authentication:Apple:KeyId" "YOUR_KEY_ID" --project src/Web
dotnet user-secrets set "Authentication:Apple:PrivateKey" "BASE64_OR_PEM_P8_KEY" --project src/Web
```

Do not commit the real `.p8` key. The server generates a short-lived ES256
client secret for each Apple token exchange and revocation request.

ASP.NET Core validates issuer, audience, signing key and lifetime with zero
clock skew. `CurrentUser` reads the authenticated user ID from the JWT subject
claim.

## Persistence

The default database is PostgreSQL 16 through EF Core and Npgsql.

Infrastructure provides:

- `ApplicationDbContext`;
- snake_case naming conventions;
- EF Core entity configurations;
- auditable entity interception;
- ToDo, user and refresh-token repositories;
- case-insensitive PostgreSQL `citext` columns for email and username;
- unique provider identity, email, username and token indexes;
- cascade deletion from users to refresh tokens.

Database startup is controlled by:

```json
{
    "Database": {
        "ApplyMigration": true,
        "SeedData": false,
        "DatabaseProvider": "PostgreSQL"
    }
}
```

## TypeGen

TypeGen generates TypeScript contracts from annotated Application DTOs.
Configuration is stored in:

```text
src/Application/tgconfig.json
```

Generation is opt-in and runs after a Debug `Web.csproj` build:

```powershell
dotnet build src/Web/Web.csproj -p:GenerateTypeScript=true
```

Release builds skip generation because `tgconfig.json` intentionally reads the
Debug assemblies. This prevents a Release build from silently generating
contracts from stale Debug output.

The target executes from the Application directory so TypeGen can find its
configuration. `GenerateTypeScript` defaults to `false` in
`Directory.Build.props`: normal builds and tests therefore never write into the
frontend and do not require the frontend directory to exist. Passing the
property explicitly keeps contract generation an intentional build action and
avoids creating multiple MSBuild graph instances for the same project.

## Observability: Serilog, Seq and OpenTelemetry

Observability has two complementary paths.

### Structured logs

Serilog is the ASP.NET Core logging provider. It:

- starts with a bootstrap console logger;
- reads logging configuration from ASP.NET Core configuration;
- enriches events with log context, machine name, thread ID and application
  name;
- writes to the console;
- writes structured logs to Seq when `Seq:ServerUrl` is configured.

CQRS logging records command/query name, duration and outcome. Authentication
tokens and request payloads must not be logged.

### Distributed traces

OpenTelemetry tracing instruments:

- incoming ASP.NET Core requests;
- outgoing `HttpClient` calls;
- recorded exceptions.

Traces are exported using OTLP. In Docker Compose:

```text
OpenTelemetry__OtlpEndpoint=http://seq:5341/ingest/otlp
OpenTelemetry__OtlpProtocol=HttpProtobuf
```

Seq receives both Serilog events and OTLP traces. The local Seq UI is available
at:

```text
http://localhost:8081
```

When the Compose volume is initialized for the first time, sign in with:

```text
Username: admin
Password: ChangeMe123!
```

`SEQ_FIRSTRUN_ADMINPASSWORD` is an initial bootstrap password and Seq requires it
to be changed at the first login. It is read only when the Seq data volume is
initialized. Changing the value in `docker-compose.yml` later does not update
the password stored in an existing `cctemplate-seq-data` volume; change an
existing password from **Settings > Users** in the Seq user interface instead.

The committed password is for local development only. Replace it before the
first startup of any shared or non-local environment. For unattended
deployments, prefer `SEQ_FIRSTRUN_ADMINPASSWORDHASH`, which does not trigger the
first-login password change.

Only tracing is configured currently. OpenTelemetry metrics are not yet
registered.

## Rate limiting and CORS

Two fixed-window policies are registered:

- anonymous requests, partitioned by remote IP;
- authenticated requests, partitioned by identity when available and otherwise
  by remote IP.

Rejected requests receive HTTP `429`.

Development and production CORS policies are configured separately in the Web
layer.

## Testing strategy

### Application unit tests

`tests/Application.UnitTests` covers:

- command/query validation;
- ToDo handlers;
- Auth sign-in, refresh, logout and profile-deletion handlers;
- success and failure branches with mocked dependencies.

Use `Theory` for equivalent input variations and `Fact` for distinct behavior
or workflows.

```powershell
dotnet test tests/Application.UnitTests/Application.UnitTests.csproj
```

### Architecture tests

`tests/ArchitectureTests` verifies layer boundaries and naming conventions.

```powershell
dotnet test tests/ArchitectureTests/ArchitectureTests.csproj
```

### Integration tests

`tests/Infrastructure.IntegrationTests` starts one PostgreSQL 16 Testcontainer
for the complete test collection. Test classes share the same
`WebApplicationFactory` and container, execute serially and clean their data
between tests.

The suite covers:

- the complete ToDo HTTP and persistence flow;
- Google sign-in with external-provider mocks;
- registration and existing-user login;
- Apple authorization-code exchange, provider-token persistence and revocation;
- refresh-token rotation, replay prevention, expiry and device binding;
- current-session and all-session logout, including multi-device isolation;
- authenticated and anonymous logout attempts;
- JWT-protected profile deletion;
- persistence state after mutations.

Docker must be running:

```powershell
dotnet test tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj
```

The Testing environment uses test-only JWT settings and applies the real EF Core
migrations to the disposable container. A migration test also verifies that the
runtime model has no pending changes.

## Local development with Docker Compose

The Compose stack contains:

- `web` — API on `http://localhost:5000`;
- `db` — PostgreSQL 16 on port `5432`;
- `seq` — log/trace ingestion on `5341`, UI on `8081`.

Start it with:

```powershell
docker compose up --build
```

The example Compose and development settings contain local-only credentials.
Replace all passwords and secrets before deploying anywhere outside a local
development environment.

## Repository layout

```text
src/
  Domain/
  Application/
  Infrastructure/
  Presentation/
  Web/

tests/
  Application.UnitTests/
  ArchitectureTests/
  Infrastructure.IntegrationTests/
```

Package versions are managed centrally in `Directory.Packages.props`, while
shared compiler settings live in `Directory.Build.props`.

## Useful commands

```powershell
dotnet restore CCTemplate.slnx
dotnet build src/Web/Web.csproj
dotnet test tests/Application.UnitTests/Application.UnitTests.csproj
dotnet test tests/ArchitectureTests/ArchitectureTests.csproj
dotnet test tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj
docker compose up --build
```

## Contributing

When adding a feature:

1. keep dependencies pointing inward;
2. follow the enforced naming conventions;
3. add unit tests for validation and handler branches;
4. add integration tests for HTTP/persistence flows;
5. avoid logging credentials, identity tokens and refresh tokens;
6. update TypeGen contracts when DTOs change.

---

**Author:** [Manuel Raso](https://github.com/ShadyManu)  
**LinkedIn:** [linkedin.com/in/manuel-raso](https://www.linkedin.com/in/manuel-raso)  
**Website:** [manuelraso.dev](https://manuelraso.dev?source=cctemplate)

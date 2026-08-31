# SampleIdentityConsumer — Reference Client

A reference implementation of a client application consuming the
`Shared.Authorization` SDK and logging in exclusively through SSO
(Microsoft Entra ID / OpenID Connect). Use it as the template for a new
application.

## The 4-step contract

A new application only needs to do these four things (see the
[Vision doc](../../UserManagementPoC-Vision.md) and
[authorization-design.md](../../authorization-design.md)):

1. **Reference the shared packages.** Add project references to
   `Shared/Shared`, `Shared/Security`, and `Shared/Authorization`
   (or the NuGet packages). See `SampleIdentityConsumer.csproj`.
2. **Register the authorization client.** Call `AddIdentityAuthorization()`
   in `Program.cs`, pointing `Authority` at the Identity service:
   ```csharp
   builder.Services.AddIdentityAuthorization(options =>
   {
       options.ServiceName = "identity";
       options.Authority = "https://localhost:7057";
   });
   ```
   This registers the HTTP evaluation client, the policy provider, the
   authorization handler, `ICurrentUser`, and the bearer-token forwarding.
3. **Implement the resolvers.**
   - `IWorkflowContextResolver` — maps each request to the required
     permissions/roles. See `Services/SampleWorkflowContextResolver.cs`
     (the client owns the workflow&rarr;permission mapping).
   - `IResourceScopeResolver` (optional) — resolves the resource's
     `BankId`/`BranchId` scope. See `Services/SampleResourceScopeResolver.cs`
     (reads `?bank=&branch=` query values).
4. **Decorate endpoints.** Add `[AuthorizeWorkflow]`, `[AuthorizeAnyRole]`,
   `[AuthorizeAnyPermission]`, etc. See the `Controllers/Sample*Controller.cs`
   files. **No authorization logic lives in the endpoints.**

## The SSO login journey

The browser demo (`https://localhost:7205/`) walks through the whole journey:

1. `GET /api/sso/login` redirects to the Identity service
   `GET /api/auth/login?clientId=...&returnUrl=...`.
2. Identity challenges Microsoft Entra; after sign-in the browser returns to
   Identity `/signin-oidc`, which maps the Entra principal to a local user
   and mints a short-lived, single-use authorization code.
3. The browser lands on `GET /api/sso/callback?code=...`; the consumer's
   backend exchanges the code at `POST /api/auth/token` (clientId + clientSecret)
   and receives `{ access_token, refresh_token, expires_at, user }`.
4. The tokens are stored in **HttpOnly cookies**; the demo-only
   `CookieToBearerMiddleware` bridges the cookie into the `Authorization`
   header on each request, so the JwtBearer scheme and the SDK's evaluation
   handler work for browser-driven calls.
   *(PoC UI only — production is an Angular SPA that holds the token and sends
   `Authorization: Bearer <token>` on every API call; no cookies.)*
5. `GET /api/sample/me` shows the signed-in user (via Identity `/api/auth/me`);
   the demo page calls each protected endpoint and shows **200 vs 403**.

### Components

| File | Purpose |
|---|---|
| `Program.cs` | JwtBearer + `AddIdentityAuthorization` + resolvers + `AddIdentitySsoClient` + static assets |
| `Middleware/CookieToBearerMiddleware.cs` | Demo-only cookie&rarr;bearer bridge (from `Shared.Authorization`) — PoC UI only; production Angular SPA uses a Bearer token directly |
| `Services/IdentitySsoClient.cs` | Typed client: code exchange (`POST /api/auth/token`) + `GET /api/auth/me` (from `Shared.Authorization`; registered via `AddIdentitySsoClient`) |
| `Controllers/SsoController.cs` | `login` (redirect), `callback` (exchange + cookies — PoC UI only; handles `?error=` from the Identity service and redirects to `/`), `logout` (clear cookies — PoC UI only) |
| `Controllers/SampleMeController.cs` | `GET /api/sample/me` — current user |
| `Controllers/Sample*Controller.cs` | Protected endpoints demonstrating the attribute family |
| `wwwroot/` | Minimal Bootstrap demo UI |

## Configuration

- `IdentityAuthority` — browser-facing Identity URL (injected by Aspire via
  the AppHost `WithEnvironment("IdentityAuthority", identity.GetEndpoint("https"))`;
  falls back to `https://localhost:7057`).
- `IdentityClient:ClientId` / `IdentityClient:ClientSecret` — must match an
  `ApiClients` entry in the Identity service `appsettings.json` (the same entry
  also defines the consumer's allowed return-URL hosts).
- `JwtSettings` — the shared signing key/issuer/audience used to validate the
  Identity JWT (mirror the Identity service settings).

## Adding a new consumer

1. Copy this project (or reference the packages directly).
2. Register a new `ApiClients` entry in
   `Core/Identity/appsettings.json` (clientId, clientSecret,
   `AllowedReturnUrlHosts` for your app's origin).
3. Add your Entra app registration redirect URI
   `https://localhost:<identity-port>/signin-oidc`.
4. Implement your `IWorkflowContextResolver` (and optionally
   `IResourceScopeResolver`).
5. Decorate endpoints and wire the service into `AppHost`.

## Real Entra testing checklist

1. Register an app in Entra ID; capture `TenantId`, `ClientId`, `ClientSecret`.
2. Set the web redirect URI to the Identity service's
   `https://localhost:7057/signin-oidc`.
3. Replace the `OpenIdConnect` placeholders in
   `Core/Identity/appsettings.json`.
4. Ensure local users (UserManagementAdmin) have emails matching the Entra
   accounts (for first-login auto-link), or pre-link via
   `POST /api/auth/users/{userId}/logins` (`provider=EntraId`,
   `providerKey` = the account ObjectId).
5. Browse to `https://localhost:7205/` and sign in.
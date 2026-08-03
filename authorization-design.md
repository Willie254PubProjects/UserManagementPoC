# Auth & Authorization Service Design

**Project:** UserManagementPoC

------------------------------------------------------------------------

## 1. Executive Summary

This document describes the design of the authentication and
authorization service implemented in this proof of concept.

The system follows a **three-tier architecture**:

```
┌─────────────────────┐
│   Client App        │  Consumes Shared.Authorization SDK
│   (SampleConsumer)  │  Contains zero auth logic
├─────────────────────┤
│   Identity Service  │  Merged auth + authorization engine
│   (Core.Identity)   │
├─────────────────────┤
│ UserManagementAdmin │  Identity data, sessions, permissions
│   (Core.Admin)      │
└─────────────────────┘
```

The central objectives are:

-   **Interface-first** -- every capability is defined by a contract.
    Implementations are replaceable through DI.
-   **Lightweight tokens** -- JWTs carry only identity claims and a
    session version. No roles, no permissions, no workflow data.
-   **On-demand authorization** -- permissions and roles are fetched by
    the Identity service at evaluation time, not embedded in tokens.
-   **Attribute-driven** -- endpoints declare requirements declaratively
    through a family of authorization attributes.
-   **Session-scoped caching** -- permission/role caches are keyed to a
    user's `SecurityVersion` and `PermissionVersion`, enabling automatic
    invalidation on logout/forced session expiry and on role/permission
    assignment changes.

------------------------------------------------------------------------

## 2. Solution Map

```
UserManagementPoC
│
├── Shared
│   ├── Shared               Cross-cutting contracts, base entities,
│   │                        caching abstraction (ICacheService),
│   │                        ICurrentUser, repositories
│   ├── Security             Authentication contracts only:
│   │                        ITokenGenerator, ITokenValidator,
│   │                        IUserAuthenticator, models
│   └── Authorization        Public SDK: attributes, contracts,
│                            HTTP client, policy provider,
│                            authorization handler
│
├── Core
│   ├── Identity             Merged auth + authorization service.
│   │                        Implements IUserAuthenticator,
│   │                        ITokenGenerator, ITokenValidator,
│   │                        IAuthorizationEvaluator.
│   │                        Calls UserManagementAdmin over HTTP.
│   │
│   ├── UserManagementAdmin  ASP.NET Core Identity data, sessions,
│   │                        permission catalogue, role/permission
│   │                        assignment, seed data.
│   │
│   └── SampleIdentityConsumer  Reference client app. Registers the
│   │                        SDK, implements IWorkflowContextResolver,
│   │                        decorates endpoints with attributes.
│
│   (Hosting)
└── AppHost                  Aspire orchestration
```

------------------------------------------------------------------------

## 3. Interface-First Design

Every capability in the auth/authorization pipeline is defined by a
public interface. The SDK ships contracts only. Implementations live in
the Identity service or the consuming application.

### 3.1 Contract Catalogue

| Interface | Purpose | Default Implementation | Location |
|---|---|---|---|
| `IAuthorizationEvaluator` | Evaluate an authorization request | `AuthorizationService` (Identity) | Shared.Authorization |
| `IWorkflowContextResolver` | Resolve workflow context from HTTP request | Client app provides | Shared.Authorization |
| `IResourceScopeResolver` | Resolve the target resource's scope (`BankId`/`BranchId`) for a request | `NoOpResourceScopeResolver` (SDK default) | Shared.Authorization |
| `ITokenGenerator` | Generate JWT + refresh token | `TokenService` (Identity) | Shared.Security |
| `ITokenValidator` | Validate a JWT and return user info | `TokenService` (Identity) | Shared.Security |
| `IUserAuthenticator` | Authenticate credentials and issue tokens | `AuthenticationService` (Identity) | Shared.Security |
| `IEncryptionService` | Encrypt/decrypt credentials during login | `AesEncryptionService` (Shared.Security) | Shared.Security |
| `IKeyVaultService` | Provide signing/encryption secrets from configuration | `ConfigKeyVaultService` (Identity) | Shared.Security |
| `ICurrentUser` | Access current user identity (Id, UserName, DisplayName, Email, BankId, BranchId, CountryCode) from JWT claims | `CurrentUser` (SDK internal) | Shared.Shared |
| `ICacheService` | Generic caching abstraction | `MemoryCacheService` (Identity) | Shared.Shared |
| `IUserManagementApiClient` | HTTP client to UserManagementAdmin | `UserManagementApiClient` (Identity) | Core.Identity |

### 3.2 Replaceability

Because every dependency is registered through DI, any consumer may
replace a default implementation:

``` csharp
// Identity service registers the real implementation
services.AddScoped<IAuthorizationEvaluator, AuthorizationService>();

// A test project substitutes a mock
services.AddScoped<IAuthorizationEvaluator>(_ =>
    new MockAuthorizationEvaluator { Result = AuthorizationResult.Allowed() });

// A client app provides its own workflow resolver
services.AddScoped<IWorkflowContextResolver, CustomWorkflowContextResolver>();

// A client app provides its own resource scope resolver
services.AddScoped<IResourceScopeResolver, CustomResourceScopeResolver>();
```

The SDK registration method `AddIdentityAuthorization()` registers only
the infrastructure -- HTTP client, policy provider, handler -- but never
hardcodes the evaluation logic.

``` csharp
    public static IServiceCollection AddIdentityAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureOptions = null)
{
    // Options
    services.AddSingleton(options);

    // Token forwarding
    services.AddTransient<BearerTokenHandler>();

    // HTTP client with token forwarding
    services.AddHttpClient<IAuthorizationEvaluator, AuthorizationClient>(client =>
    {
        if (!string.IsNullOrEmpty(options.Authority))
            client.BaseAddress = new Uri(options.Authority);
    }).AddHttpMessageHandler<BearerTokenHandler>();

    // Infrastructure
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUser, CurrentUser>();
    services.AddScoped<IResourceScopeResolver, NoOpResourceScopeResolver>();
    services.AddScoped<IAuthorizationHandler, AuthorizationEvaluationHandler>();
    services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

    return services;
}
```

The SDK registers `IResourceScopeResolver` to the default
`NoOpResourceScopeResolver`, so scope resolution always has a fallback.
A consumer that needs explicit resource scope simply registers its own
implementation, which replaces the no-op through DI.

A client application therefore needs only:

``` csharp
// Program.cs
builder.Services.AddIdentityAuthorization(options =>
{
    options.Authority = "https://identity.company.com";
    options.ServiceName = "identity";
});
builder.Services.AddScoped<IWorkflowContextResolver, MyWorkflowResolver>();
builder.Services.AddScoped<IResourceScopeResolver, MyScopeResolver>();
```

------------------------------------------------------------------------

## 4. JWT Design & Registration Abstraction

### 4.1 Claims Factory

Token generation is abstracted through `ITokenGenerator` and
`ClaimsFactory`. The Identity service owns the implementation; no client
app ever constructs claims or signs tokens.

``` csharp
public class ClaimsFactory
{
    public List<Claim> Create(UserInfo user, string? securityVersion = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("display_name", user.DisplayName),
            new("bank_id", user.BankId),
            new("branch_id", user.BranchId),
            new("country_code", user.CountryCode)
        };

        if (!string.IsNullOrEmpty(securityVersion))
            claims.Add(new("security_version", securityVersion));

        return claims;
    }
}
```

### 4.2 Token Contents

The JWT is intentionally **lightweight**:

| Claim | Source | Purpose |
|---|---|---|
| `sub` (NameIdentifier) | User.Id | Identify the user |
| `name` | User.UserName | Username |
| `email` | User.Email | Contact / lookup |
| `display_name` | User.DisplayName | Display name |
| `bank_id` | User.BankId | Subsidiary bank identifier |
| `branch_id` | User.BranchId | Branch identifier |
| `country_code` | User.CountryCode | Country code |
| `security_version` | Session GUID | Link token to server-side session |

The token maps the entire identity of `UserInfo` (which implements
`ICurrentUser`). Explicitly excluded:

-   **Roles** -- not embedded
-   **Permissions** -- not embedded
-   **Workflow data** -- not embedded
-   **given_name / family_name** -- not embedded (only `display_name`)

### 4.3 Token Abstraction

``` csharp
public interface ITokenGenerator
{
    Task<TokenResponse> GenerateTokenAsync(
        UserInfo user,
        string? securityVersion,
        CancellationToken ct);
}

public interface ITokenValidator
{
    Task<UserInfo?> ValidateTokenAsync(
        string token,
        CancellationToken ct);
}
```

`TokenService` (the default implementation) signs using
`HMACSHA256` with a symmetric key from configuration. The access token
lifetime is configurable (default **15 minutes**). Refresh tokens are
stored in cache with a **30-minute** TTL and single-use semantics
(deleted on validation).

The server-side session mirrors the refresh window. `UserSession` is
created with `ExpiresAt` set to the same **30-minute** default, and the
session lookup verifies `IsActive`, `ExpiresAt`, and the idle timeout.
The refresh flow re-validates the session against
UserManagementAdmin before issuing a new token, so an expired or
logged-out session rejects both evaluation and refresh.

------------------------------------------------------------------------

## 5. Lightweight Token Strategy

### 5.1 Why Not Embed Roles or Permissions?

Embedding roles or permissions in a JWT creates several problems:

-   **Token size** -- grows linearly with the number of roles and
    permissions, affecting every HTTP request header.
-   **Stale data** -- once issued, the token is valid until expiry.
    There is no way to revoke a specific permission without
    invalidating the entire token.
-   **Logout complexity** -- the client must discard the token, but a
    stolen token remains usable until expiry.
-   **Cross-system drift** -- if permissions change in the admin system,
    already-issued tokens carry the old grants.

### 5.2 The SecurityVersion Solution

Instead of embedding entitlements, each user session is tracked
server-side with a `SecurityVersion` -- a GUID generated at session
creation.

```
Login
  │
  ├── Create UserSession with SecurityVersion = new GUID
  │
  └── Embed SecurityVersion in JWT as "security_version" claim

Authorization Request
  │
  ├── Read "security_version" from JWT
  ├── Check session is still active via UserManagementAdmin
  ├── Fetch roles + permissions on-demand (with caching)
  └── Evaluate against requirements

Logout
  │
  ├── Set session IsActive = false
  ├── Regenerate SecurityVersion to a new GUID
  └── Old JWT becomes rejected on next request
```

This means:

-   **Immediate revocation** -- logout changes the version; the old
    token is dead instantly.
-   **Cache busting** -- the cache key includes `securityVersion`, so
    stale permissions are never served after invalidation.
-   **Minimal token** -- the JWT carries only identity and the version
    fingerprint.

### 5.3 Permission Fetch is Server-Side Only

```
┌──────────────┐         ┌────────────────┐         ┌──────────────────┐
│  JWT Token   │         │ Identity        │         │ UserManagement   │
│              │         │ Service         │         │ Admin            │
│ sub          │         │                  │         │                  │
│ name         │         │  On every eval:  │         │  Stores:         │
│ email        │         │                  │         │  ─ Users         │
│ display_name │         │  1. Validate JWT │────────►│  ─ Sessions      │
│ bank_id      │ ──────► │  2. Validate ses │  HTTP   │  ─ Roles         │
│ branch_id    │         │  3. Fetch roles  │◄────────│  ─ Permissions   │
│ country_code │         │  4. Fetch perms  │         │  ─ Assignments   │
│ security_ver │         │  5. Evaluate     │         │                  │
│ NO roles     │         │                  │         │                  │
│ NO perms     │         │                  │         │                  │
│ NO workflow  │         │                  │         │                  │
└──────────────┘         └────────────────┘         └──────────────────┘
```

The client application never sees the permission data. It calls the
Identity service's `/api/authorization/evaluate` endpoint and receives
only an `AuthorizationResult { IsAllowed, Reason }`.

------------------------------------------------------------------------

## 6. Authorization Attribute Family

### 6.1 Attribute Hierarchy

```
AuthorizeAttribute (ASP.NET Core)
│
├── AuthorizeWorkflowAttribute
│       Policy = "WorkflowAuthorization"
│       → Runtime resolution via IWorkflowContextResolver
│
└── AuthRequirementAttribute (base)
        Policy = composite string: "Type|Operator|item1,item2"
        │
        ├── AuthorizeAnyRoleAttribute         (Or)
        ├── AuthorizeAllRolesAttribute        (And)
        ├── AuthorizeAnyPermissionAttribute   (Or)
        └── AuthorizeAllPermissionsAttribute  (And)
```

### 6.2 AuthorizeWorkflowAttribute

This attribute carries no explicit policy data. It simply sets
`Policy = "WorkflowAuthorization"`. The actual permission requirements
are resolved at runtime by the client app's `IWorkflowContextResolver`.

``` csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeWorkflowAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "WorkflowAuthorization";

    public AuthorizeWorkflowAttribute()
    {
        Policy = PolicyPrefix;
    }
}
```

Usage:

``` csharp
[AuthorizeWorkflow]
public async Task<IActionResult> Approve(string workflow, string action)
```

The endpoint is decoupled from the specific permission names. The
resolver maps `(workflow, action)` to the required permission set.

### 6.3 AuthRequirementAttribute (Base)

All explicit-requirement attributes inherit from this base class. It
constructs a composite policy string that the
`AuthorizationPolicyProvider` later parses.

``` csharp
public class AuthRequirementAttribute : AuthorizeAttribute
{
    public AuthRequirementAttribute(
        AuthPolicyType policyType,
        AuthOperator authOperator,
        params string[] items)
    {
        Policy = AuthPolicyName.Create(policyType, authOperator, items);
    }
}
```

### 6.4 Shared Permission & Role Constants

To keep attributes free of magic strings, well-known permission and
role names live as constants in `Shared.Authorization.Constants`. Both
are organized into subfolders so each domain area stays in its own file
instead of growing one file for everything.

**Permissions** -- `Permissions` is a `static partial class` split
across `Constants/Permissions/`, one file per domain area. Each file
nests a static class holding that area's
`Create`/`View`/`Edit`/`Approve`/`Submit`/`Invoke` constants. Usage
remains clear and readable: `Permissions.CardPrinting.Create`.

```
Constants/Permissions/
├── CardPrinting.cs   → partial Permissions { static class CardPrinting { ... } }
├── Account.cs        → partial Permissions { static class Account { ... } }
└── CardRequest.cs    → partial Permissions { static class CardRequest { ... } }
```

``` csharp
// Constants/Permissions/CardRequest.cs
namespace UserManagementPoC.Shared.Authorization.Constants;

public static partial class Permissions
{
    public static class CardRequest
    {
        public const string Create = "CardRequest.Create";
        public const string View   = "CardRequest.View";
        // ...
    }
}
```

**Roles (BshRoles)** -- the role names used by `[AuthorizeAnyRole]` /
`[AuthorizeAllRoles]` live in `Constants/Roles/BshRoles.cs`:

``` csharp
// Constants/Roles/BshRoles.cs
namespace UserManagementPoC.Shared.Authorization.Constants;

public static class BshRoles
{
    public const string Administrator = "Administrator";
    public const string Manager       = "Manager";
    public const string Viewer        = "Viewer";
}
```

Attributes consume these constants instead of magic strings. Adding a
new permission area or role is a one-file addition to the relevant
subfolder.

### 6.5 Concrete Attributes

| Attribute | PolicyType | Operator | String Format |
|---|---|---|---|
| `[AuthorizeAnyRole(BshRoles.Administrator, BshRoles.Manager)]` | Role | Or | `Role\|Or\|Administrator,Manager` |
| `[AuthorizeAllRoles(BshRoles.Administrator)]` | Role | And | `Role\|And\|Administrator` |
| `[AuthorizeAnyPermission(Permissions.CardPrinting.Create)]` | Permission | Or | `Permission\|Or\|CardPrinting.Create` |
| `[AuthorizeAllPermissions(Permissions.Account.View, Permissions.CardRequest.View)]` | Permission | And | `Permission\|And\|Account.View,CardRequest.View` |

``` csharp
// User must hold at least one of these permissions
[AuthorizeAnyPermission(Permissions.CardPrinting.Create, Permissions.CardPrinting.Approve)]
public IActionResult CreateCardPrinting() { ... }

// User must hold all specified roles
[AuthorizeAllRoles(BshRoles.Administrator)]
public IActionResult AdminOnly() { ... }
```

All attribute values reference the shared constants
(`Permissions.*` for permissions, `BshRoles.*` for roles) defined in
§6.4 -- never hand-typed strings.

Multiple attributes on the same endpoint combine with **AND**
semantics -- each attribute produces its own `IdentityAuthorizationRequirement`,
and ASP.NET Core requires all handlers to succeed.

### 6.6 Policy Name Encoding

The `AuthPolicyName` helper converts structured data into a flat string
that survives ASP.NET Core's policy-name-based architecture:

``` csharp
// AuthPolicyName.Create(AuthPolicyType.Permission, AuthOperator.Or, ["CardPrinting.Create"])
// → "Permission|Or|CardPrinting.Create"
```

The `AuthorizationPolicyProvider` reverses this:

``` csharp
// "Permission|Or|CardPrinting.Create"
// → IdentityAuthorizationRequirement { PolicyType = Permission, Operator = Or, Items = ["CardPrinting.Create"] }
```

------------------------------------------------------------------------

## 7. Authorization Evaluation Pipeline

### 7.1 Component Roles

```
┌──────────────────────────────────────────────────────────────────┐
│ CLIENT APPLICATION                                                │
│                                                                  │
│  [AuthorizeWorkflow]                                             │
│       │                                                          │
│       ▼                                                          │
│  AuthorizationPolicyProvider                                     │
│   ─ Decodes policy name → IdentityAuthorizationRequirement       │
│       │                                                          │
│       ▼                                                          │
│  AuthorizationEvaluationHandler (AuthorizationHandler)           │
│   ─ Reads ICurrentUser                                           │
│   ─ Resolves WorkflowContext via IWorkflowContextResolver        │
│     (if no PolicyType on requirement)                            │
│     OR                                                           │
│   ─ Populates roles/permissions from requirement (if PolicyType) │
│   ─ Resolves resource scope via IResourceScopeResolver           │
│     (falls back to Workflow?.BankId/BranchId, then current user) │
│   ─ Builds AuthorizationContext + BankId/BranchId                │
│       │                                                          │
│       ▼                                                          │
│  AuthorizationClient (IAuthorizationEvaluator)                   │
│   ─ POSTs AuthorizationContext to Identity service               │
│   ─ Forwards original JWT via BearerTokenHandler                  │
│       │                                                          │
└───────┼──────────────────────────────────────────────────────────┘
        │  HTTPS
        ▼
┌──────────────────────────────────────────────────────────────────┐
│ IDENTITY SERVICE                                                  │
│                                                                  │
│  AuthorizationController                                         │
│   ─ POST /api/authorization/evaluate                             │
│       │                                                          │
│       ▼                                                          │
│  AuthorizationService (IAuthorizationEvaluator)                  │
│   ─ Reads security_version from JWT                              │
│   ─ Validates session via UserManagementApiClient                │
│   ─ Fetches roles (cached)                                       │
│   ─ Fetches permissions (cached)                                  │
│   ─ Enforces resource scope (bank guard + branch match)          │
│   ─ Evaluates required vs granted                                │
│   ─ Returns AuthorizationResult                                  │
│       │                                                          │
└───────┼──────────────────────────────────────────────────────────┘
        │  HTTPS
        ▼
┌──────────────────────────────────────────────────────────────────┐
│ USERMANAGEMENT ADMIN                                              │
│                                                                  │
│  AuthController                                                  │
│   ├── GET  /api/auth/users/{id}/roles                            │
│   ├── GET  /api/auth/users/{id}/permissions                      │
│   ├── GET  /api/auth/sessions/{securityVersion}                  │
│   └── POST /api/auth/sessions/{securityVersion}/invalidate       │
└──────────────────────────────────────────────────────────────────┘
```

The box shows the authorization subset of the admin service. It also
exposes the auth endpoints used during login (`POST /api/auth/verify-credentials`,
`POST /api/auth/sessions`, `GET /api/auth/users/{id}`) -- see §10.1.

### 7.2 Handler: Two Branches

The `AuthorizationEvaluationHandler` handles two cases through a single
`AuthorizationHandler<IdentityAuthorizationRequirement>`:

``` csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    IdentityAuthorizationRequirement requirement)
{
    if (!_currentUser.IsAuthenticated) { context.Fail(); return; }

    var authContext = new AuthorizationContext { UserId = _currentUser.Id! };

    if (requirement.PolicyType.HasValue)
    {
        // Branch 1: Explicit attribute (AnyRole, AllRoles, AnyPermission, AllPermissions)
        authContext.Operator = requirement.Operator;
        if (requirement.PolicyType == AuthPolicyType.Role)
            authContext.Roles = requirement.Items;
        else
            authContext.Permissions = requirement.Items;
    }
    else
    {
        // Branch 2: Workflow resolution (AuthorizeWorkflow)
        var httpContext = _httpContextAccessor.HttpContext;
        authContext.Workflow = await _workflowContextResolver.ResolveAsync(httpContext);
    }

    // Resource scope resolution (always runs)
    var resourceScope = await _resourceScopeResolver.ResolveAsync(
        _httpContextAccessor.HttpContext);
    // Precedence: explicit Workflow → resolved resource scope → current user claims
    authContext.BankId   = authContext.Workflow?.BankId
                            ?? resourceScope?.BankId
                            ?? _currentUser.BankId;
    authContext.BranchId = authContext.Workflow?.BranchId
                            ?? resourceScope?.BranchId
                            ?? _currentUser.BranchId;

    var result = await _evaluator.EvaluateAsync(authContext);
    if (result.IsAllowed) context.Succeed(requirement);
    else context.Fail();
}
```

**Branch 1 -- Attribute-declared:** The requirement carries explicit
`Items` (role names or permission names), an `Operator` (And/Or), and a
`PolicyType` (Role/Permission). No workflow resolution needed.

**Branch 2 -- Workflow-resolved:** The requirement is empty (from
`[AuthorizeWorkflow]`). The handler calls `IWorkflowContextResolver`
to produce a `WorkflowContext` containing the permission set. The
client application owns this resolver.

**Resource scope (always runs):** Independently of the branch, the
handler calls `IResourceScopeResolver` to determine the target
resource's scope. The effective `BankId`/`BranchId` are resolved with
the precedence `Workflow` → `ResourceScope` → current-user claims.
Before evaluating, these are attached to the `AuthorizationContext` so
the authorization engine can enforce organizational scope.

### 7.3 Server-Side Evaluation

The core evaluation logic in `AuthorizationService`:

```
AuthorizationService.EvaluateAsync(context)
│
├── Extract security_version from JWT claims
│
├── Validate session: GET /api/auth/sessions/{securityVersion}
│   └── If inactive or expired (ExpiresAt / idle timeout) → DENIED
│
├── Scope guard (bank)
│   ├── If context.BankId and the user's bank_id claim are both set
│   │   and differ → resource is outside the user's subsidiary → DENIED
│
├── Role evaluation (if roles required)
│   ├── GET /api/auth/users/{userId}/roles (cached)
│   ├── Match required roles against granted roles
│   │   └── A role counts only if its Scope contains the resource
│   │       BranchId (fail-closed: an empty resource BranchId or an
│   │       empty grant Scope fails the scope check → DENIED)
│   └── Operator: Or (any match) or And (all match)
│
├── Permission evaluation (if permissions required)
│   ├── GET /api/auth/users/{userId}/permissions (cached)
│   ├── Match required permissions against granted permissions
│   │   └── A permission counts only if its Scope contains the
│   │       resource BranchId (fail-closed: an empty resource BranchId
│   │       or an empty grant Scope fails the scope check → DENIED)
│   └── Operator: Or for workflow-sourced, configurable for attributes
│
└── ALLOW if all checks pass, DENY otherwise
```

Permissions and roles are compared case-insensitively. For
workflow-sourced permissions, OR semantics apply -- holding any one of
the required permissions grants access.

`AuthorizationContext` also carries `BankId` and `BranchId`. These are
consumed by the authorization engine to enforce organizational scope:
the requesting user's assigned roles/permissions must align with the
resolved scope (a specific subsidiary/branch) for the call to be
allowed. The handler resolves them with the precedence `Workflow` →
`IResourceScopeResolver` → current-user claims (see §7.2).

### 7.4 Resource Scope Enforcement and Flexibility

The `IResourceScopeResolver` is the extension point that decides what
scope a request applies to. The SDK ships a `NoOpResourceScopeResolver`
that returns `null`, which makes the engine fall back to the current
user's own `BankId`/`BranchId` claims (effectively "self-scoped"). The
client application replaces it to tailor scope resolution to its
environment:

``` csharp
public interface IResourceScopeResolver
{
    Task<ResourceScope?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
```

Because it receives the full `HttpContext`, a resolver can derive scope
from virtually any request signal, making it extremely flexible:

-   **Database / repository lookup** -- resolve the `BankId`/`BranchId`
    from a record identified by a route parameter (e.g. the account or
    client being acted upon). This is the most common and robust source,
    since the scope is derived from the actual resource.
-   **Query-string parameters** -- read `?bank=&branch=` for ad-hoc,
    framed scope selection (as demonstrated in the sample consumer).
-   **HTTP headers** -- a gateway or upstream service can stamp headers
    (e.g. `X-Tenant`, `X-Branch`) that the resolver trusts.
-   **Route values / path** -- parse a segment such as
    `/api/accounts/{branchId}/...`.
-   **Claims** -- fall back to `ClaimTypes` from the authenticated
    identity when nothing more specific is present.
-   **Composition** -- apply any of the above in a chosen precedence,
    returning `null` to delegate to the next fallback in the handler's
    chain (`Workflow` → resolved scope → current-user claims).

The return value is optional on purpose: returning `null` signals "no
specific scope," letting the pipeline fall back deterministically. No
resolver implementation ever needs to know about the others.

A built-in reference resolver is provided for the PoC
(`SampleResourceScopeResolver` in the consumer) that reads
`bank`/`branch` query-string values, demonstrating the query-string
strategy end to end.

------------------------------------------------------------------------

## 8. Caching Strategy

### 8.1 Cache Architecture

```
                    AuthorizationService
                           │
                    ┌──────┴──────┐
                    │             │
                    ▼             ▼
           ┌──────────────┐  ┌──────────────┐
           │ Roles Cache  │  │ Permissions  │
           │              │  │ Cache        │
           │ key:         │  │              │
           │ roles:{uid}: │  │ key:         │
           │   {sv}:{pv}  │  │ perms:{uid}: │
           │ TTL: 3 min   │  │   {sv}:{pv}  │
           │              │  │ TTL: 3 min   │
           └──────────────┘  └──────────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │  ICacheService │
                    │  (abstraction) │
                    └──────┬───────┘
                           │
                    ┌──────┴───────┐
                    │MemoryCache    │
                    │(IMemoryCache) │
                    └──────────────┘
```

### 8.2 Cache Key Design

Each cache key incorporates the user's `SecurityVersion` **and**
`PermissionVersion` (the latter returned with the session validation
response):

```
roles:{userId}:{securityVersion}:{permissionVersion}
permissions:{userId}:{securityVersion}:{permissionVersion}
```

`PermissionVersion` is a second, independent invalidation lever: it is
bumped whenever a user's role or permission assignments change
(`PermissionVersionService`), so stale grants are never served even
without a logout. `SecurityVersion` handles logout/forced session
expiry; `PermissionVersion` handles assignment changes.

Example:

```
roles:usr_abc123:3f8a2b1c-...:12    → RoleDto[] { { Code = "Administrator", Description = "Full system administrator", Scope = ["KE", "001"] } }
permissions:usr_abc123:3f8a2b1c-...:12 → PermissionDto[] { { Code = "CardPrinting.Create", Description = "Create CardPrinting", Scope = ["KE", "001"] } }
```

### 8.3 Cache Duration

-   **Default TTL:** 3 minutes (`TimeSpan.FromMinutes(3)`)
-   **Cache implementation:** `IMemoryCache` via `MemoryCacheService`
    (singleton, in-process)
-   **Refresh tokens:** 30 minutes, single-use (deleted on validation)

### 8.4 Automatic Invalidation on Logout

The `SecurityVersion` is the linchpin of cache invalidation:

```
Login:
  → CreateSession(session with SecurityVersion = "A")
  → JWT carries security_version = "A"
  → Cache keys: roles:usr:A:0, permissions:usr:A:0

Logout:
  → InvalidateSession("A")
    → IsActive = false
    → SecurityVersion = "B" (new GUID)
  → Old JWT with security_version = "A" is now rejected at session check
  → Cache entries for version "A" expire naturally (3 min TTL)
  → New login creates cache under version "B"

Assignment change (no logout):
  → PermissionVersion bumps 0 → 1
  → Cache keys roles:usr:A:1 / permissions:usr:A:1 are rebuilt on next request
  → Old keys under PermissionVersion 0 expire naturally

No explicit cache eviction is needed. A version change makes the old
cache keys unreachable.
```

### 8.5 ICacheService Abstraction

The cache interface lives in `Shared.Shared` so every layer can depend
on it without coupling to a specific implementation:

``` csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
    Task RefreshAsync(string key, CancellationToken ct);
}
```

The Identity service registers `MemoryCacheService`, but a distributed
cache (Redis, SQL Server) could replace it without changing the
authorization logic.

------------------------------------------------------------------------

## 9. Client Integration Walkthrough

A new application requires four steps.

### Step 1: Reference the NuGet Package

``` xml
<PackageReference Include="UserManagementPoC.Shared.Authorization" Version="*" />
```

### Step 2: Register the Authorization Client

``` csharp
// Program.cs
builder.Services.AddIdentityAuthorization(options =>
{
    options.Authority = "https://identity.company.com";
    options.ServiceName = "identity";
});
```

This registers:

-   `IAuthorizationEvaluator` → HTTP client to Identity service
-   `IAuthorizationPolicyProvider` → custom policy decoder
-   `IAuthorizationHandler` → evaluation handler
-   `ICurrentUser` → user context from HTTP
-   `IResourceScopeResolver` → `NoOpResourceScopeResolver` (default;
    overridden by the app when explicit resource scope is needed)
-   `BearerTokenHandler` → automatic token forwarding

### Step 3: Implement IWorkflowContextResolver

``` csharp
public class MyResolver : IWorkflowContextResolver
{
    public Task<WorkflowContext> ResolveAsync(
        HttpContext httpContext,
        CancellationToken ct)
    {
        var routeData = httpContext.GetRouteData();
        var workflow = routeData.Values["workflow"]?.ToString();
        var action = routeData.Values["action"]?.ToString();

        return Task.FromResult(new WorkflowContext
        {
            WorkflowName = workflow,
            Action = action,
            RequiredPermissions = ResolvePermissions(workflow, action)
        });
    }
}
```

Register it:

``` csharp
builder.Services.AddScoped<IWorkflowContextResolver, MyResolver>();
```

### Step 3b: Implement IResourceScopeResolver (optional)

For organizational-scope enforcement, provide a resolver so the engine
knows which bank/branch the request targets. If omitted, the SDK's
`NoOpResourceScopeResolver` returns `null` and the engine falls back to
the current user's own scope. A query-string example:

``` csharp
public class MyScopeResolver : IResourceScopeResolver
{
    public Task<ResourceScope?> ResolveAsync(
        HttpContext httpContext, CancellationToken ct = default)
    {
        var bank   = httpContext.Request.Query["bank"].FirstOrDefault();
        var branch = httpContext.Request.Query["branch"].FirstOrDefault();
        if (string.IsNullOrEmpty(bank)) return Task.FromResult<ResourceScope?>(null);
        return Task.FromResult<ResourceScope?>(new ResourceScope(bank, branch));
    }
}
```

Register it:

``` csharp
builder.Services.AddScoped<IResourceScopeResolver, MyScopeResolver>();
```

### Step 4: Decorate Endpoints

``` csharp
[ApiController]
[Route("api/card-printing")]
public class CardPrintingController : ControllerBase
{
    [HttpPost]
    [AuthorizeWorkflow]
    public async Task<IActionResult> CreateCardPrinting(...)
    { /* business logic only */ }

    [HttpGet("{id}")]
    [AuthorizeAnyPermission(Permissions.CardPrinting.View)]
    public async Task<IActionResult> GetCardPrinting(string id)
    { /* business logic only */ }

    [HttpPost("{id}/approve")]
    [AuthorizeAllRoles(BshRoles.Manager)]
    [AuthorizeWorkflow]
    public async Task<IActionResult> ApproveCardPrinting(string id)
    { /* business logic only */ }
}
```

The application contains **zero authorization logic**. It owns:

-   The mapping from HTTP requests to workflow permissions (the
    resolver)
-   The endpoint decoration (attributes)

Everything else -- session validation, permission fetching, caching,
policy resolution, evaluation -- is handled by the framework.

------------------------------------------------------------------------

## 10. Complete Data Flows

### 10.1 Login Flow

```
Client                     Identity                   UserManagementAdmin
  │                          │                              │
  │ POST /api/auth/login     │                              │
  │ { username, password }   │                              │
  │─────────────────────────►│                              │
  │                          │ Encrypt password (AES)       │
  │                          │                              │
  │                          │ POST /api/auth/verify-creds  │
  │                          │─────────────────────────────►│
  │                          │                              │ Verify via
  │                          │                              │ SignInManager
  │                          │   { Success, UserInfo }      │
  │                          │◄─────────────────────────────│
  │                          │                              │
  │                          │ POST /api/auth/sessions      │
  │                          │ { UserId, RemoteIp, UA }     │
  │                          │─────────────────────────────►│
  │                          │                              │ Create UserSession
  │                          │   { SecurityVersion = GUID } │ (SecurityVersion)
  │                          │◄─────────────────────────────│
  │                          │                              │
  │                          │ ClaimsFactory.Create(user, sv)│
  │                          │ TokenService.GenerateToken() │
  │                          │                              │
  │  { AccessToken,          │                              │
  │    RefreshToken,         │                              │
  │    ExpiresAt }           │                              │
  │◄─────────────────────────│                              │
```

### 10.2 Workflow Authorization Flow

```
Client                     Identity                   UserManagementAdmin
  │                          │                              │
  │ GET /api/sample/loan/    │                              │
  │   create                 │                              │
  │ [AuthorizeWorkflow]      │                              │
  │                          │                              │
  │ PolicyProvider resolves  │                              │
  │ "WorkflowAuthorization"  │                              │
  │ → empty WorkflowAuthReq  │                              │
  │                          │                              │
  │ Handler calls            │                              │
  │ IWorkflowContextResolver │                              │
  │ → WorkflowContext        │                              │
  │   { RequiredPerms:       │                              │
  │     ["CardPrinting.Create"] }                            │
  │                          │                              │
  │ Handler calls            │                              │
  │ IResourceScopeResolver   │                              │
  │ → scope from ?bank=      │                              │
  │   &branch= querystring   │                              │
  │                          │                              │
  │ POST /api/authorization  │                              │
  │   /evaluate              │                              │
  │ { UserId, Workflow,      │                              │
  │   BankId, BranchId }     │                              │
  │ + Bearer token           │                              │
  │─────────────────────────►│                              │
  │                          │ Read security_version        │
  │                          │ from JWT                     │
  │                          │                              │
  │                          │ GET /api/auth/sessions/{sv}  │
  │                          │─────────────────────────────►│
  │                          │   { IsActive: true }         │
  │                          │◄─────────────────────────────│
  │                          │                              │
  │                          │ Cache miss?                  │
  │                          │ GET /api/auth/users/{id}/    │
  │                          │   permissions                │
  │                          │─────────────────────────────►│
  │                          │   ApiResponse<PermissionDto[]>│
  │                          │◄─────────────────────────────│
  │                          │ Store in cache (3 min)       │
  │                          │                              │
  │                          │ Evaluate:                    │
  │                          │ "CardPrinting.Create" in      │
  │                          │ granted permissions? → YES   │
  │                          │                              │
  │  { IsAllowed: true }     │                              │
  │◄─────────────────────────│                              │
```

### 10.3 Direct Attribute Authorization Flow

```
Client                     Identity                   UserManagementAdmin
  │                          │                              │
  │ GET /api/sample/         │                              │
  │   permission-check       │                              │
  │ [AuthorizeAllPermissions │                              │
  │   ("CardPrinting.Create")]│                              │
  │                          │                              │
  │ PolicyProvider parses    │                              │
  │ "Permission|And|         │                              │
  │  CardPrinting.Create"    │                              │
  │ → WorkflowAuthReq        │                              │
  │   { PolicyType=Permission│                              │
  │     Operator=And         │                              │
  │     Items=["CardPrinting.Create"] }                      │
  │                          │                              │
  │ Handler builds:          │                              │
  │ AuthorizationContext     │                              │
  │ { UserId, Permissions:   │                              │
  │   ["CardPrinting.Create"] │                              │
  │   Operator=And }         │                              │
  │                          │                              │
  │ POST /api/authorization  │                              │
  │   /evaluate (same flow   │                              │
  │   as above)              │─────────────────────────────►│
  │                          │                              │
  │  { IsAllowed: true }     │                              │
  │◄─────────────────────────│                              │
```

### 10.4 Logout Flow

```
Client                     Identity                   UserManagementAdmin
  │                          │                              │
  │ POST /api/auth/logout    │                              │
  │ [Authorize]              │                              │
  │ + Bearer token           │                              │
  │─────────────────────────►│                              │
  │                          │ Read "security_version"      │
  │                          │ from JWT → "A"               │
  │                          │                              │
  │                          │ POST /api/auth/sessions/     │
  │                          │   A/invalidate               │
  │                          │─────────────────────────────►│
  │                          │                              │
  │                          │                              │ Set IsActive=false
  │                          │                              │ Regenerate
  │                          │                              │ SecurityVersion → "B"
  │                          │◄─────────────────────────────│
  │                          │                              │
  │  "Logged out"            │                              │
  │◄─────────────────────────│                              │
```

After logout, any request with the old token hits:

```
AuthorizationService:
  → Read security_version = "A"
  → GET /api/auth/sessions/A → { IsActive: false }
  → DENIED
```

The next login creates a session with version "B", and permissions are
freshly fetched and cached under the new key.

------------------------------------------------------------------------

## 11. Design Rules

1.  **Separate authentication from authorization data.** The Identity
    service exposes distinct API routes (`api/auth/*` and
    `api/authorization/*`) even though they run in the same process.

2.  **Defend interfaces, not implementations.** Every capability is a
    contract. The SDK ships no evaluation logic -- only client plumbing
    (HTTP client, handler, policy provider). Consumers, tests, and the
    Identity service itself all depend on interfaces.

3.  **Keep tokens lightweight.** JWTs carry identity claims and a
    session version only. No roles, no permissions, no workflow data.
    Entitlements are fetched on-demand server-side.

4.  **Use SecurityVersion as the revocation mechanism.** A single GUID
    in the token links it to the server-side session. Invalidation
    requires nothing more than generating a new GUID.

5.  **Decouple workflow knowledge from the engine.** The authorization
    engine never understands business workflows. It receives a
    resolved `WorkflowContext` and evaluates permissions with OR
    semantics. Client applications own the mapping via
    `IWorkflowContextResolver`.

6.  **Prefer declarative attributes over imperative checks.**
    Authorization requirements are expressed as `[AuthorizeWorkflow]`,
    `[AuthorizeAnyPermission]`, `[AuthorizeAllRoles]`, etc. The
    endpoint contains no authorization logic, only business logic.

7.  **Encode structured policy data in flat strings.** Composite policy
    names (`"Permission|Or|A,B"`) bridge the gap between structured
    attributes and ASP.NET Core's string-based policy model. Encoding
    is symmetric with decoding.

8.  **Cache by session version.** Cache keys incorporate
    `SecurityVersion`, so changing the version implicitly invalidates
    all cached data for that session. No explicit cache eviction is
    necessary.

9.  **Single handler, two branches.** One `AuthorizationHandler`
    handles both workflow-resolved and attribute-declared requirements.
    The presence of `PolicyType` determines the branch.

10. **Forward the bearer token.** The client app's HTTP client
    automatically forwards the incoming JWT to the Identity service.
    The `BearerTokenHandler` enables this transparently so the
    Identity service can validate the session.

------------------------------------------------------------------------

## 12. End-to-End Principle

``` text
┌─────────────────────────────────────────────────────────────┐
│                                                              │
│   Application Development Contract                           │
│                                                              │
│   1. Reference Shared.Authorization (NuGet)                  │
│                                                              │
│   2. Register: AddIdentityAuthorization()                    │
│                                                              │
│   3. Implement: IWorkflowContextResolver                     │
│                                                              │
│   4. Implement: IResourceScopeResolver                       │
│      (optional; NoOp falls back to current-user scope)       │
│                                                              │
│   5. Decorate: [AuthorizeWorkflow]                           │
│                                                              │
│   ─────────────────────────────────────────────              │
│                                                              │
│   Everything else is handled by the framework:               │
│                                                              │
│   → JWT validation and forwarding                            │
│   → Session verification                                     │
│   → Permission and role fetching (cached)                    │
│   → Policy resolution                                        │
│   → Authorization evaluation                                 │
│   → Audit trail (via server-side session tracking)           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

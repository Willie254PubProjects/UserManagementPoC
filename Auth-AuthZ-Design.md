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
    user's `SecurityVersion`, enabling automatic invalidation on logout
    or forced session expiry.

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
| `IPermissionProvider` | Retrieve user permissions | Not yet used in pipeline | Shared.Authorization |
| `IPermissionDefinitionProvider` | Enumerate known permission definitions | -- | Shared.Authorization |
| `ITokenGenerator` | Generate JWT + refresh token | `TokenService` (Identity) | Shared.Security |
| `ITokenValidator` | Validate a JWT and return user info | `TokenService` (Identity) | Shared.Security |
| `IUserAuthenticator` | Authenticate credentials and issue tokens | `AuthenticationService` (Identity) | Shared.Security |
| `ICurrentUser` | Access current user from HTTP context | `CurrentUser` (SDK internal) | Shared.Shared |
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
    services.AddScoped<IAuthorizationHandler, AuthorizationEvaluationHandler>();
    services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

    return services;
}
```

A client application therefore needs only:

``` csharp
// Program.cs
builder.Services.AddIdentityAuthorization(options =>
{
    options.Authority = "https://identity.company.com";
    options.ServiceName = "identity";
});
builder.Services.AddScoped<IWorkflowContextResolver, MyResolver>();
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
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("given_name", user.FirstName),
            new("family_name", user.LastName)
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
| `name` | User.Username | Display name |
| `email` | User.Email | Contact / lookup |
| `given_name` | User.FirstName | Personalization |
| `family_name` | User.LastName | Personalization |
| `security_version` | Session GUID | Link token to server-side session |

Explicitly excluded:

-   **Roles** -- not embedded
-   **Permissions** -- not embedded
-   **Workflow data** -- not embedded
-   **Organizational scope** -- not embedded

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
`HMACSHA256` with a symmetric key from configuration. Token lifetime is
configurable (default 60 minutes). Refresh tokens are stored in cache
with a 7-day TTL and single-use semantics.

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
│ security_ver │ ──────► │  1. Validate JWT │────────►│  ─ Sessions      │
│              │         │  2. Validate ses │  HTTP   │  ─ Roles         │
│ NO roles     │         │  3. Fetch roles  │◄────────│  ─ Permissions   │
│ NO perms     │         │  4. Fetch perms  │         │  ─ Assignments   │
│ NO workflow  │         │  5. Evaluate     │         │                  │
└──────────────┘         └────────────────┘         └──────────────────┘
```

The client application never sees the permission data. It calls the
Identity service's `/api/authorization/evaluate` endpoint and receives
only `AuthorizationResult { IsAllowed }`.

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

### 6.4 Concrete Attributes

| Attribute | PolicyType | Operator | String Format |
|---|---|---|---|
| `[AuthorizeAnyRole("Admin", "Manager")]` | Role | Or | `Role|Or|Admin,Manager` |
| `[AuthorizeAllRoles("Admin", "Manager")]` | Role | And | `Role|And|Admin,Manager` |
| `[AuthorizeAnyPermission("Loan.Create.Invoke")]` | Permission | Or | `Permission|Or|Loan.Create.Invoke` |
| `[AuthorizeAllPermissions("A", "B")]` | Permission | And | `Permission|And|A,B` |

``` csharp
// User must hold at least one of these permissions
[AuthorizeAnyPermission("Loan.Create.Invoke", "Loan.Approve.*")]
public IActionResult CreateLoan() { ... }

// User must hold all specified roles
[AuthorizeAllRoles("Administrator")]
public IActionResult AdminOnly() { ... }
```

Multiple attributes on the same endpoint combine with **AND**
semantics -- each attribute produces its own `IdentityAuthorizationRequirement`,
and ASP.NET Core requires all handlers to succeed.

### 6.5 Policy Name Encoding

The `AuthPolicyName` helper converts structured data into a flat string
that survives ASP.NET Core's policy-name-based architecture:

``` csharp
// AuthPolicyName.Create(AuthPolicyType.Permission, AuthOperator.Or, ["Loan.Create.Invoke"])
// → "Permission|Or|Loan.Create.Invoke"
```

The `AuthorizationPolicyProvider` reverses this:

``` csharp
// "Permission|Or|Loan.Create.Invoke"
// → IdentityAuthorizationRequirement { PolicyType = Permission, Operator = Or, Items = ["Loan.Create.Invoke"] }
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
│   ─ Builds AuthorizationContext                                  │
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

### 7.3 Server-Side Evaluation

The core evaluation logic in `AuthorizationService`:

```
AuthorizationService.EvaluateAsync(context)
│
├── Extract security_version from JWT claims
│
├── Validate session: GET /api/auth/sessions/{securityVersion}
│   └── If inactive → DENIED
│
├── Role evaluation (if roles required)
│   ├── GET /api/auth/users/{userId}/roles (cached)
│   ├── Match required roles against granted roles
│   └── Operator: Or (any match) or And (all match)
│
├── Permission evaluation (if permissions required)
│   ├── GET /api/auth/users/{userId}/permissions (cached)
│   ├── Match required permissions against granted permissions
│   └── Operator: Or for workflow-sourced, configurable for attributes
│
└── ALLOW if all checks pass, DENY otherwise
```

Permissions and roles are compared case-insensitively. For
workflow-sourced permissions, OR semantics apply -- holding any one of
the required permissions grants access.

`AuthorizationContext` also carries optional `BankId` and `BranchId`
fields. They are reserved as placeholders for future organizational-scope
evaluation (restricting access to a specific subsidiary or branch) and
are not yet consumed by the engine.

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
           │   {sv}       │  │ perms:{uid}: │
           │ TTL: 3 min   │  │   {sv}       │
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

Each cache key incorporates the user's `SecurityVersion`:

```
roles:{userId}:{securityVersion}
permissions:{userId}:{securityVersion}
```

Example:

```
roles:usr_abc123:3f8a2b1c-...    → IReadOnlySet<string> { "Administrator" }
permissions:usr_abc123:3f8a2b1c-... → IReadOnlySet<string> { "Loan.Create.Invoke", "Loan.View.*" }
```

### 8.3 Cache Duration

-   **Default TTL:** 3 minutes (`TimeSpan.FromMinutes(3)`)
-   **Cache implementation:** `IMemoryCache` via `MemoryCacheService`
    (singleton, in-process)
-   **Refresh tokens:** 7 days, single-use (deleted on validation)

### 8.4 Automatic Invalidation on Logout

The `SecurityVersion` is the linchpin of cache invalidation:

```
Login:
  → CreateSession(session with SecurityVersion = "A")
  → JWT carries security_version = "A"
  → Cache keys: roles:usr:A, permissions:usr:A

Logout:
  → InvalidateSession("A")
    → IsActive = false
    → SecurityVersion = "B" (new GUID)
  → Old JWT with security_version = "A" is now rejected at session check
  → Cache entries for version "A" expire naturally (3 min TTL)
  → New login creates cache under version "B"

No explicit cache eviction is needed. The version change makes
the old cache keys unreachable.
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

### Step 4: Decorate Endpoints

``` csharp
[ApiController]
[Route("api/loans")]
public class LoanController : ControllerBase
{
    [HttpPost]
    [AuthorizeWorkflow]
    public async Task<IActionResult> CreateLoan(...)
    { /* business logic only */ }

    [HttpGet("{id}")]
    [AuthorizeAnyPermission("Loan.View.*")]
    public async Task<IActionResult> GetLoan(string id)
    { /* business logic only */ }

    [HttpPost("{id}/approve")]
    [AuthorizeAllRoles("Loan Supervisor")]
    [AuthorizeWorkflow]
    public async Task<IActionResult> ApproveLoan(string id)
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
  │     ["Loan.Create.Invoke"] }                            │
  │                          │                              │
  │ POST /api/authorization  │                              │
  │   /evaluate              │                              │
  │ { UserId, Workflow }     │                              │
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
  │                          │   IReadOnlySet<string>       │
  │                          │◄─────────────────────────────│
  │                          │ Store in cache (3 min)       │
  │                          │                              │
  │                          │ Evaluate:                    │
  │                          │ "Loan.Create.Invoke" in      │
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
  │   ("Loan.Create.Invoke")]│                              │
  │                          │                              │
  │ PolicyProvider parses    │                              │
  │ "Permission|And|         │                              │
  │  Loan.Create.Invoke"     │                              │
  │ → WorkflowAuthReq        │                              │
  │   { PolicyType=Permission│                              │
  │     Operator=And         │                              │
  │     Items=["Loan.Create.Invoke"] }                      │
  │                          │                              │
  │ Handler builds:          │                              │
  │ AuthorizationContext     │                              │
  │ { UserId, Permissions:   │                              │
  │   ["Loan.Create.Invoke"] │                              │
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
    contract. The SDK ships zero implementation logic. Consumers,
    tests, and the Identity service itself all depend on interfaces.

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
│   4. Decorate: [AuthorizeWorkflow]                           │
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

# User Management PoC

A proof of concept demonstrating a **centralized authentication and
authorization platform** that any .NET microservice can adopt with minimal
effort. It proves the viability of a single auth identity across services,
delivered as a clean, interface-first SDK (`Shared.Authorization`) backed by a
merged auth + authorization service (`Core.Identity`) and a usable
user-admin service (`Core.UserManagementAdmin`).

## What this PoC demonstrates

-   **Centralized auth for all microservices** — one Identity service issues
    and validates JWTs and evaluates authorization on demand for every
    consuming application.
-   **A clean SDK abstraction** — client apps reference `Shared.Authorization`,
    register it in DI, implement `IWorkflowContextResolver` (and optionally
    `IResourceScopeResolver`), and decorate endpoints with authorization
    attributes. **The application contains zero authorization logic.**
-   **A usable user-admin service** — `Core.UserManagementAdmin` manages
    identity data, sessions, the permission catalogue, roles and permissions
    with scoped assignments, organizational units, access groups, and seed
    data.

## Solution structure

```
UserManagementPoC
├── Shared
│   ├── Shared           Cross-cutting contracts, base entities, ICacheService,
│   │                    ICurrentUser, repositories
│   ├── Security         Authentication contracts (ITokenGenerator,
│   │                    ITokenValidator, IEncryptionService)
│   └── Authorization    Public SDK: authorization attributes, contracts,
│                        HTTP client, policy provider, evaluation handler,
│                        permission/role constants
├── Core
│   ├── Identity         Merged auth + authorization engine (tokens, sessions,
│   │                    on-demand permission evaluation, caching)
│   ├── UserManagementAdmin  Identity data, sessions, permission catalogue,
│   │                        role/permission assignment, org units, access groups
│   └── SampleIdentityConsumer  Reference client app consuming the SDK
└── AppHost              Aspire orchestration
```

## Design decisions

-   **Interface-first** — every capability is a contract; implementations are
    replaceable through DI.
-   **Lightweight tokens** — JWTs carry identity claims plus a session version;
    no roles, no permissions, no workflow data. Entitlements are fetched
    on-demand, server-side.
-   **Attribute-driven** — endpoints declare requirements declaratively
    (`[AuthorizeWorkflow]`, `[AuthorizeAnyRole]`, `[AuthorizeAllPermissions]`, ...).
-   **Session-scoped caching & revocation** — tokens are linked to a
    server-side `SecurityVersion`; caches keyed by `SecurityVersion` +
    `PermissionVersion` invalidate on logout or on assignment changes.
-   **Resource scope enforcement** — organizational scope (`BankId`/`BranchId`)
    is resolved per request via `IResourceScopeResolver` and verified at
    evaluation time.
-   **Short-lived tokens** — access token 15 minutes, refresh token 30 minutes
    (single-use); the server-side session mirrors the refresh window.

For the full authentication and authorization design — tokens, sessions,
attributes, the evaluation pipeline, scope, and caching — see
[`authorization-design.md`](./authorization-design.md).

## Quickstart

```sh
dotnet run --project AppHost
```

AppHost (Aspire) starts the Identity service, UserManagementAdmin, and the
SampleIdentityConsumer. SSO (Microsoft Entra ID / OpenID Connect) is the only
login method. Open the sample consumer's demo UI at
`https://localhost:7205/` and sign in: the browser runs the Entra flow, the
consumer exchanges the returned one-time code for the app JWT at the Identity
`POST /api/auth/token` endpoint, and the page then lets you call the
`/api/sample/*` endpoints to see workflow, role, permission, and resource-scope
authorization in action (200 vs 403). See
[`Core/SampleIdentityConsumer/README.md`](./Core/SampleIdentityConsumer/README.md)
for the full onboarding guide.

## Documentation

-   [`authorization-design.md`](./authorization-design.md) — auth & authorization
    service design. 

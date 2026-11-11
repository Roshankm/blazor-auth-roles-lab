# Blazor Auth & Roles Lab

[![CI](https://github.com/Roshankm/blazor-auth-roles-lab/actions/workflows/ci.yml/badge.svg)](https://github.com/Roshankm/blazor-auth-roles-lab/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)

A .NET 10 Blazor reference app covering **cookie sign-in and every common authorization style**: roles, claims, custom requirements and resource-based rules. No third-party packages.

Part of my set of Blazor reference labs, each one focused on a single feature with tests and CI.

## Demo users

Password for all three: `Passw0rd!`

| User | Role | Department | Hired |
|---|---|---|---|
| alice | Admin | IT | 8 years ago |
| bob | Manager | Finance | 3 years ago |
| carol | Employee | Finance | 4 months ago |

## Authorization styles

| Page | Rule | Style |
|---|---|---|
| `/admin` | `[Authorize(Roles = "Admin")]` | Role |
| `/approvals` | `RequireRole("Manager", "Admin")` | Role-based policy |
| `/finance` | `RequireClaim("department", "Finance")` | Claim-based policy (Alice is admin but **not** allowed) |
| `/senior` | `MinimumTenureRequirement(2)` from the `hire_date` claim | Custom requirement and handler |
| `/leave` | Edit button per row: owner of a pending request, or admin | Resource-based (`AuthorizeView Resource=...`) |
| `/` | Live "what can I do" table using `IAuthorizationService` | Programmatic checks |

## Security details

* Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<T>`.
* Sign-in runs in a **static SSR** form, so the cookie is written in a normal HTTP request with antiforgery protection.
* Sign-out is a `POST` that validates an antiforgery token, so it can't be triggered by a link.
* Return URLs are checked to block open redirects (`//evil.com`, `/\evil.com`, absolute URLs).
* The cookie is `HttpOnly`, `SameSite=Lax` and sliding, with a one-hour lifetime.

## Run it

```bash
dotnet run --project src/AuthLab --launch-profile https
dotnet test
```

## Tests

* **Handler tests** cover the tenure boundary (exactly two years vs one day short), every resource-based case and open-redirect protection.
* **End-to-end tests** with `WebApplicationFactory` post the real login form with its antiforgery token, keep the cookie, and check each user against each protected page.

## License

[MIT](LICENSE)

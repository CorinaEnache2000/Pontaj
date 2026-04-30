# Pontaj

Internal time-tracking web app for **Clinica Sante**. ASP.NET Core MVC + Razor (server-rendered), JWT auth via Active Directory, SQL Server via EF Core. Romanian UI.

## Stack

- **.NET 9** (SDK 9.0.200), `Microsoft.NET.Sdk.Web`, `Pontaj/Pontaj.csproj`
- **EF Core 9** (SQL Server, DB-first scaffolding) — DB `Pontaj` on `DB01\SQL01`, **Windows-integrated auth** (no password — runs as the app's process identity)
- **JWT bearer** auth — two `JwtBearer` schemes share validation params: `JwtHeader` (Authorization header, default for API) and `JwtCookie` (`sessionToken` cookie, MVC views, redirects to `/Account/Login` on challenge)
- **Bootstrap 5** + Bootstrap Icons (CDN) + jQuery (kept for compatibility)
- **System.DirectoryServices** for AD lookups (LDAP server `Dc-01.intranet.local`, domain `INTRANET`)
- Windows-only assembly: `[assembly: SupportedOSPlatform("windows")]`

## Project layout

```
Pontaj/
  Controllers/
    AccountController.cs          [ApiController] /api/account/{login,me} — JwtHeader scheme; login is [AllowAnonymous]
    AccountViewsController.cs     /Account/Login Razor view — [AllowAnonymous]
    HomeController.cs             [Authorize(JwtCookie)] — landing page
  Database/Pontaj/                EF-scaffolded; PontajContext has connection string in OnConfiguring (no password)
    PontajContext.cs              partial class — re-apply OnConfiguring cleanup after every scaffold
    AppUser.cs / UserRole.cs / UserXUserRole.cs / LogEntry.cs / Configuration.cs
  Repositories/                   Per-aggregate, interface-backed (IUserRepository etc.); writes deferred, caller invokes SaveChangesAsync
  Services/Login/
    ActiveDirectoryService.cs     LDAP bind for credential validation; DirectorySearcher for memberOf
    RoleService.cs / UserService.cs
    JwtTokenService.cs            CreateToken + CreateTokenFromPrincipal (re-issues from existing claims, no DB hit)
    JwtRuntimeOptions.cs          Singleton holding signing key / lifetime / refresh threshold (loaded at startup)
    JwtSettings.cs                Issuer/Audience consts + Configuration row name consts
    AuthSchemes.cs                Scheme name + cookie name consts
  Filters/JwtRefreshFilter.cs     Global IAsyncActionFilter; refreshes JWT on ResponseBase results when remaining < threshold
  Models/ResponseBase.cs          API envelope: { Status, Reason, Data, Token }
  Views/
    Shared/_Layout.cshtml         html lang="ro"; toast container; navbar hideable via ViewData["HideNavbar"]; Clinica Sante footer
    Account/Login.cshtml          Centered card, no <form> (JS-driven), session-expired solid alert
  wwwroot/
    images/logo-sante.jpg         Clinica Sante logo (copied from ManVan)
    js/api.js                     Backend client — see "Frontend"
    js/site.js                    showToast(opType, content)
    js/<area>/<page>.js           Page scripts mirroring view paths (e.g. js/account/login.js)
  Program.cs                      DI registration; bootstrap reads JWT config from DB; FallbackPolicy = RequireAuthenticatedUser
```

## Database

`Pontaj` on `DB01\SQL01` — connection string lives in `PontajContext.OnConfiguring` and is intentionally checked into source (no password — Integrated Security).

**Tables**: `AppUser`, `UserRole`, `UserXUserRole`, `LogEntries`, `Configuration`.

**`Configuration`** rows (key/value):
- `Jwt:SigningKey` — HS256 symmetric key (rotate by updating; needs process restart)
- `Jwt:TokenLifetimeSeconds` — default `28800` (8h)
- `Jwt:RefreshThresholdPercent` — default `25` (refresh on next authorized response when remaining < 25% of lifetime)

**Schema changes**: re-scaffold via
```
dotnet ef dbcontext scaffold "Server=DB01\SQL01;Database=Pontaj;Integrated Security=True;TrustServerCertificate=True;Encrypt=True" Microsoft.EntityFrameworkCore.SqlServer --project Pontaj/Pontaj.csproj --output-dir Database/Pontaj --context PontajContext --context-namespace Pontaj.Database.Pontaj --namespace Pontaj.Database.Pontaj --use-database-names --force
```
Then re-apply the `OnConfiguring` cleanup (drop `#warning`, wrap in `if (!optionsBuilder.IsConfigured)`).

## Authentication flow

1. Browser hits `/` → `HomeController` requires `JwtCookie` scheme → reads `sessionToken` cookie → fails → `OnChallenge` redirects to `/Account/Login`.
2. Login page submits to `POST /api/account/login` (Bearer-less). `ActiveDirectoryService.Authenticate` does an LDAP bind; `GetUserInfo` + `GetUserGroups` query AD via `DirectorySearcher`. `RoleService.GetRolesFromADGroupsAsync` matches `memberOf` CNs against `UserRole.ADGroupName`. `UserService.GetOrCreateUserAsync` auto-provisions the `AppUser`. `JwtTokenService.CreateToken` issues a fresh JWT. Returns `ResponseBase.Success(...) { Token = jwt }`.
3. `apiRequest` in JS sees `envelope.token` and **mirrors it to both `localStorage["sessionToken"]` and `document.cookie sessionToken`** (`SameSite=Strict; Secure; Path=/`). Then `window.location.href = '/'`.
4. Subsequent navigations send the cookie; subsequent API calls send `Authorization: Bearer <jwt>`. **API endpoints ignore the cookie**, MVC endpoints ignore the header.
5. `JwtRefreshFilter` runs on every authorized action returning `ResponseBase`; re-issues a JWT when `exp - now < TokenLifetime * RefreshThresholdPercent / 100` and slots it into `response.Token`. Same JS path picks it up and refreshes both stores.
6. On 401 with a Bearer attached, **or** on `apiRequest`'s pre-flight `isJwtExpired`, JS clears localStorage + cookie, sets `sessionStorage["isSessionExpired"] = '1'`, redirects to `/Account/Login`. The login page calls `consumeSessionExpiredFlag()` and shows a solid (non-toast) yellow alert.

**AD groups → roles** (in `dbo.UserRole`):
- `G_Pontaj_Admin` → `ADMINISTRATOR`
- `G_Pontaj_Director` → `DIRECTOR`
- `G_Pontaj_Utilizator` → `UTILIZATOR`

User must be in at least one to log in (otherwise 403 with `Nu aveți drept de acces la această aplicație.`).

## API contract — `ResponseBase` envelope

JSON endpoints return:
```json
{
  "status": "success" | "error",
  "reason": "human-readable string" | null,
  "data": <any payload>,
  "token": "<refreshed JWT>" | null
}
```

- Errors set `status = "error"`, `reason` to the (Romanian) message. HTTP status reflects error class (400/401/403).
- `token` is non-null only when the server is issuing or refreshing one. JS auto-mirrors it to localStorage + cookie.
- HTML endpoints (e.g. partial views) return `text/html`; the JS client handles them via `expect: 'html'`.

## Frontend conventions (rules)

- **All user-facing strings in Romanian with proper diacritics** (ă, â, î, ș, ț). Identifiers stay in English. UTF-8 only — never write transliterations like "Parola" instead of "Parolă". `<html lang="ro">` everywhere.
- **No inline `<script>` in Razor views.** Page scripts live at `wwwroot/js/<area-lowercase>/<page-lowercase>.js`, mirroring the view path. Reference from `@section Scripts` with a single `<script src>` line.
- **Top-level JS constants prefixed `APP_`** (not project-name prefix). Storage-key string values should also avoid the project name (`sessionToken`, not `pontaj.jwt`).
- **Always brace `if`/`else` bodies**, even single statements. No `if (cond) doX();` shorthand.
- **Use `rem` for layout sizes**, not hardcoded px. Exception: arithmetic on DOM measurements (`offsetHeight` / `scrollHeight`) which always return px.
- **`apiRequest({...})`** ([wwwroot/js/api.js](Pontaj/wwwroot/js/api.js)) is the only way JS talks to the backend. XHR-based, single function. Body types: `'json'` (default), `'form'` (urlencoded), `FormData` auto-detected as multipart. `expect: 'json' | 'html' | 'blob'`. Set `skipAuth: true` for unauthenticated calls.
- **`showToast(opType, content)`** ([wwwroot/js/site.js](Pontaj/wwwroot/js/site.js)) is the global toast helper. `opType ∈ {'error','success','warning','information'}`. Used as the default error fallback in `apiRequest` if no `onError` is given.

## Security posture

Internal corporate LAN only. Default to most restrictive:

- **`SameSite=Strict`** on the session cookie (deep-links from external sites are not a concern; user is already on the intranet).
- **`Secure`** + HTTPS enforced (`UseHttpsRedirection`).
- **`FallbackPolicy = RequireAuthenticatedUser()`** — every endpoint requires auth unless `[AllowAnonymous]`.
- **`MapStaticAssets().AllowAnonymous()`** — static files (CSS/JS/fonts) must be reachable on the login page.
- **JWT signing key** lives in `dbo.Configuration` (not committed). Other JWT settings same.
- **Cookie is JS-set, not HttpOnly** — XSS surface = same as localStorage. If you ever need HttpOnly, the server has to `Set-Cookie` on login.

## Build / run

```bash
dotnet build Pontaj/Pontaj.csproj      # full build
dotnet msbuild Pontaj/Pontaj.csproj -t:Compile -nologo -verbosity:minimal   # compile-only (use when app is running and holds Pontaj.exe)
dotnet run --project Pontaj
```

**Common gotcha**: when the app is running, full builds fail with MSB3027 (file lock on `Pontaj.exe`). The C# compile already succeeded — just stop the running app, or use the `-t:Compile` form to verify code without trying to copy the exe.

## Known design choices worth not relitigating

- **MVC, not Razor Pages** (mirrors ManVan).
- **Per-aggregate repositories with interfaces.** Writes are deferred — repos don't auto-call `SaveChangesAsync`; the caller does.
- **Token storage**: localStorage + JS-set cookie, mirrored in lockstep. JS controls both.
- **Sliding refresh runs on every authorized action returning `ResponseBase`.** Stateless JWT — no revocation list. Rotating keys invalidates all in-flight tokens, requires process restart.
- **`AccountController` (API) and `AccountViewsController` (Razor)** are split because mixing `[ApiController]` and view-returning actions on the same class is awkward.
- **Footer is themed Clinica Sante** (logo at right). Login page hides the navbar via `ViewData["HideNavbar"] = true` but keeps the footer.

# Migration Summary: .NET Framework 4.8 → .NET 10

## Result
**Build: SUCCEEDED with 0 errors**

All 620 remaining warnings are NuGet security advisories (NU1901/NU1902/NU1903) for `Magick.NET-Q8-AnyCPU` 14.10.0. No compiler errors. No CSxxxx or MSBuild errors.

---

## What Was Migrated

### Bookstore.Web — Full ASP.NET Core Migration
| Area | Change |
|------|--------|
| Project file | Legacy `.csproj` (ToolsVersion 15.0) → SDK-style `Microsoft.NET.Sdk.Web`, `net10.0` |
| OWIN pipeline | Removed `Microsoft.Owin.*`, `Owin`, `Autofac.Mvc5`, `Autofac.Owin` |
| Global.asax / Startup.cs | Replaced by `Program.cs` (ASP.NET Core minimal hosting model) |
| Dependency injection | Autofac → built-in ASP.NET Core DI (`AddControllersWithViews`, `AddScoped`, etc.) |
| Authentication | OWIN OpenIdConnect + OWIN Cookies → `Microsoft.AspNetCore.Authentication.OpenIdConnect` + `Microsoft.AspNetCore.Authentication.Cookies` |
| Local auth middleware | OWIN `OwinMiddleware` → ASP.NET Core `IMiddleware` |
| MVC controllers | `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`; `ActionResult` → `IActionResult` |
| Routing | `RouteConfig.RegisterRoutes` → `app.MapControllerRoute` in `Program.cs` |
| Areas | `AreaRegistration.RegisterArea` → `[Area("Admin")]` attribute + route in `Program.cs` |
| `HttpPostedFileBase` | → `Microsoft.AspNetCore.Http.IFormFile`; `.InputStream` → `.OpenReadStream()` |
| `HttpContextBase` | → `Microsoft.AspNetCore.Http.HttpContext` |
| Cookies | `HttpCookie` + `Response.Cookies.Add` → `Response.Cookies.Append(key, value, CookieOptions)` |
| `SelectListItem` | `System.Web.Mvc.SelectListItem` → `Microsoft.AspNetCore.Mvc.Rendering.SelectListItem` |
| Bundling | `System.Web.Optimization` (BundleConfig) removed; static files served directly |
| Configuration | `System.Configuration.ConfigurationManager` → `Microsoft.Extensions.Configuration.IConfiguration` via updated `BookstoreConfiguration` |
| Logging | Static NLog setup → `NLog.Web.AspNetCore` with `builder.Host.UseNLog()` |
| `Html.EnumDropDownListFor` | → `Html.DropDownListFor` + `Html.GetEnumSelectList<T>()` |
| `Html.Partial` / `Html.RenderPartial` | → `<partial>` tag helper and `await Html.PartialAsync` |
| `Web.config` | → `appsettings.json` + `appsettings.Development.json` |
| Views Web.config | Removed legacy MVC Razor view configuration files |
| NuGet packages | Removed: `EntityFramework 6.5.1`, `Autofac.Mvc5`, `Autofac.Owin`, `Microsoft.Owin.*`, `WebGrease`, `Antlr`, `System.Web.Optimization`, `NuGet.Core`, `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` |
| NuGet packages | Added: `Microsoft.AspNetCore.Authentication.OpenIdConnect 10.0.0`, `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation 10.0.0`, `Microsoft.EntityFrameworkCore.SqlServer 10.0.0`, `NLog.Web.AspNetCore 5.3.15` |

### Bookstore.Data — EF6 → EF Core 10
| Area | Change |
|------|--------|
| Project file | `netstandard2.0` → `net10.0` (single TFM; EF Core SqlServer requires net6+) |
| `ApplicationDbContext` | `System.Data.Entity.DbContext` → `Microsoft.EntityFrameworkCore.DbContext`; constructor accepts `DbContextOptions<T>` |
| Fluent API | `HasRequired(...).WithMany().WillCascadeOnDelete(false)` → `HasOne(...).WithMany().OnDelete(DeleteBehavior.NoAction)` |
| Nested `Include` | `Include("OrderItems.Select(y => y.Book)")` → `.Include(x => x.OrderItems).ThenInclude(y => y.Book)` |
| `PluralizingTableNameConvention` | Manual entity-by-entity table name suppression via `entityType.SetTableName(entityType.DisplayName())` |
| `Database.SetInitializer` | Removed; replaced by `EnsureCreated()` + `BookstoreDbInitializer.Seed()` at startup |
| `BookstoreDbInitializer` | `DropCreateDatabaseIfModelChanges<T>` → static `Seed(ApplicationDbContext)` method |
| `PaginatedList<T>` | `System.Data.Entity.CountAsync/ToListAsync` → `Microsoft.EntityFrameworkCore` equivalents |
| All repositories | `System.Data.Entity` → `Microsoft.EntityFrameworkCore` |
| `OfferRepository` | Removed stray `using Amazon.Auth.AccessControlPolicy` import |
| `BookstoreConfiguration` | `System.Configuration.ConfigurationManager` → `IConfiguration`-backed; initialized from `Program.cs` |
| NuGet packages | Removed: `EntityFramework 6.5.1`, `System.Configuration.ConfigurationManager`, `System.Data.DataSetExtensions` |
| NuGet packages | Added: `Microsoft.EntityFrameworkCore.SqlServer 10.0.0` |
| `Magick.NET` | Upgraded from 14.6.0 → 14.10.0 to reduce vulnerability advisories |

### Bookstore.Domain / Bookstore.Common
- Legacy `Properties/AssemblyInfo.cs` cleared (SDK auto-generates these attributes; duplicates caused CS0579 errors)

---

## Remaining Warnings (Non-Blocking)

| Warning | Count | Reason | Action Needed |
|---------|-------|--------|---------------|
| NU1901/NU1902/NU1903 | ~620 | `Magick.NET-Q8-AnyCPU 14.10.0` vulnerability advisories | Versions 14.11.0+ require net10.0 but also have incompatible API changes. See Next Steps. |
| CS0618/CS0612 | 3 | `CloudFrontWebDistribution` obsolete API in `Bookstore.Cdk/CoreStack.cs` | Pre-existing CDK code; update to use `Distribution` in a separate CDK modernization pass. |

---

## Next Steps

1. **Magick.NET vulnerabilities**: Versions above 14.10.0 have API breaks and compatibility issues with net10.0. Consider migrating image processing to a different library (e.g., `ImageSharp` from `SixLabors.ImageSharp`) to escape the Magick.NET vulnerability chain entirely. This is documented in KB file `21-system-drawing-migration.md`.

2. **Database migrations**: The migration uses `EnsureCreated()` which does not support incremental schema migrations. For production use, run `dotnet ef migrations add InitialCreate` and switch to `Database.Migrate()` in `Program.cs`.

3. **AWS SSM parameter loading**: `ConfigurationSetup.cs` (which loaded settings from AWS SSM at startup) was replaced by `BookstoreConfiguration` + `appsettings.json`. AWS-mode deployments that need SSM parameters at startup should use `Amazon.Extensions.Configuration.SystemsManager` NuGet package as an `IConfigurationProvider` in `Program.cs`.

4. **CDK CoreStack**: `CloudFrontWebDistribution` is obsolete. Update `Bookstore.Cdk/CoreStack.cs` to use `Distribution` (from `Amazon.CDK.AWS.CloudFront`). This is unrelated to the .NET Framework migration.

5. **Static file paths**: Book cover images reference `/Content/Images/coverimages/...` from seed data. These are served from `wwwroot/`. Ensure cover images are placed under `wwwroot/Content/Images/coverimages/` or update the URLs in seed data.

6. **HTTPS in ECS**: The ECS Dockerfile targets HTTP. The Cognito OIDC requires HTTPS for redirect URIs (except localhost). For full Cognito support in ECS, configure a load balancer with HTTPS termination and update the callback URLs.

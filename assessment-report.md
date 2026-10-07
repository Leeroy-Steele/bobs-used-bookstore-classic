# Assessment Report: BobsBookstoreClassic

## Solution Overview

| Attribute | Value |
|-----------|-------|
| **Solution Name** | BobsBookstoreClassic |
| **Total Projects** | 5 |
| **Target Framework** | net10.0 |
| **Total Lines of Code** | 8592 |
| **Overall Complexity** | Critical |
| **Total NuGet Packages** | 62 (across all projects) |
| **Incompatible Packages** | 12 |
| **.NET Core Readiness** | Not Ready |
| **Linux Readiness** | Not Ready |

## Executive Summary

**Solution Migration Mode: COMPLEX**

BobsBookstoreClassic is a layered ASP.NET MVC 5 e-commerce application built on .NET Framework 4.8 with OWIN-based authentication, Autofac dependency injection, Entity Framework 6, and multiple AWS service integrations (S3, Rekognition, SSM, CloudWatch). The solution follows a classic layered architecture: **Bookstore.Web** (MVC 5 front-end with Admin area) → **Bookstore.Data** (EF6 repositories, S3/Rekognition services) → **Bookstore.Domain** (entities, domain services, interfaces) → **Bookstore.Common** (shared constants). A separate **Bookstore.Cdk** project provides AWS CDK infrastructure-as-code.

The solution's OWIN middleware pipeline handles OpenID Connect authentication with cookie-based sessions, wired through `Startup.cs` and `Global.asax.cs`. Autofac.Mvc5 and Autofac.Owin integrate the DI container into the MVC 5 and OWIN pipelines respectively — both are .NET Framework-only and must be replaced. The web project alone carries 54 NuGet packages, 12 of which are incompatible with net10.0.

- **2 Low-complexity projects** (Bookstore.Domain, Bookstore.Cdk) — Bookstore.Domain is a pure domain library with no NuGet packages requiring only SDK-style conversion; Bookstore.Cdk is already SDK-style and needs only a TFM bump from net6.0 to net10.0
- **1 Medium-complexity project** (Bookstore.Data) — EF6 data layer with AWS SDK integrations requiring SDK-style conversion and EF6-to-EF Core migration
- **1 Critical-complexity project** (Bookstore.Web) — MVC 5 web application with OWIN auth, Autofac DI, 54 packages (12 incompatible), 15 controllers, 44 Razor views, area registration, and bundling
- **1 No-migration project** (Bookstore.Common) — Already targets netstandard2.0; fully cross-platform compatible

The primary transformation challenge is migrating Bookstore.Web from ASP.NET MVC 5 with OWIN to ASP.NET Core MVC on net10.0. This requires replacing the entire OWIN authentication pipeline with ASP.NET Core's built-in cookie and OpenID Connect authentication middleware, converting Autofac.Mvc5/Autofac.Owin DI to ASP.NET Core's native DI (with optional Autofac.Extensions.DependencyInjection), migrating `Global.asax` + `Startup.cs` to a unified `Program.cs`, converting BundleConfig to static file serving, and adapting all controllers and views from `System.Web.Mvc` to `Microsoft.AspNetCore.Mvc`.

### Key Statistics

| Metric | Count |
|--------|-------|
| Projects requiring format conversion | 3 (legacy to SDK-style) |
| Blocking issues | 0 |
| Razor views to migrate | 44 |
| Controllers to migrate | 15 |
| Total estimated changes | 100 |

## Project Analysis Table

| Project | Current Framework | Target | LOC | Packages | Incompatible | Complexity |
|---------|-------------------|--------|-----|----------|--------------|------------|
| Bookstore.Common | netstandard2.0 | netstandard2.0 | 6 | 0 | 0 | N/A |
| Bookstore.Domain | net4.8 | net10.0 | 1813 | 0 | 0 | Low |
| Bookstore.Data | net4.8 | net10.0 | 1042 | 4 | 0 | Medium |
| Bookstore.Cdk | net6.0 | net10.0 | 595 | 4 | 0 | Low |
| Bookstore.Web | net4.8 | net10.0 | 5136 | 54 | 12 | Critical |

## Cross-Project Package Summary

| Package | Used By | Version(s) | Compatible | Notes |
|---------|---------|------------|------------|-------|
| EntityFramework | Bookstore.Data, Bookstore.Web | 6.5.1 | Yes* | Compatible via netstandard2.1 but should be replaced with Microsoft.EntityFrameworkCore for net10.0; latest 6.5.2 |
| AWSSDK.S3 | Bookstore.Data, Bookstore.Web | 3.7.416.5 | Yes | netstandard2.0; latest 4.0.104.1 |
| AWSSDK.Rekognition | Bookstore.Data, Bookstore.Web | 3.7.400.129 | Yes | netstandard2.0; latest 4.0.101.1 |
| AWSSDK.Core | Bookstore.Web | 3.7.402.35 | Yes | netstandard2.0; latest 4.0.102.8 (transitive for Bookstore.Data) |
| Autofac | Bookstore.Web | 8.2.1 | Yes | netstandard2.0; use Autofac.Extensions.DependencyInjection for Core integration |
| Autofac.Mvc5 | Bookstore.Web | 6.1.0 | No | .NETFramework only (even latest 7.0.0 targets net4.8.1); replace with Autofac.Extensions.DependencyInjection |
| Autofac.Owin | Bookstore.Web | 7.1.0 | No | .NETFramework only (even latest 8.0.0 targets net4.8.1); replace with ASP.NET Core middleware |
| Microsoft.AspNet.Mvc | Bookstore.Web | 5.3.0 | No | .NETFramework only; replaced by Microsoft.AspNetCore.Mvc (built into Sdk.Web) |
| Microsoft.Owin | Bookstore.Web | 4.2.2 | No | .NETFramework only; replaced by ASP.NET Core middleware pipeline |
| Microsoft.Owin.Security.OpenIdConnect | Bookstore.Web | 4.2.2 | No | .NETFramework only; replaced by Microsoft.AspNetCore.Authentication.OpenIdConnect |
| NLog | Bookstore.Web | 5.4.0 | Yes | netstandard2.0; latest 6.2.1 |
| Newtonsoft.Json | Bookstore.Web | 13.0.3 | Yes | netstandard2.0; latest 13.0.4 |

## Cross-Project Dependencies

Bookstore.Web (Critical)
  - Bookstore.Common (N/A — no migration needed)
  - Bookstore.Data (Medium)
    - Bookstore.Domain (Low)
  - Bookstore.Domain (Low)

Bookstore.Cdk (Low)
  - Bookstore.Common (N/A — no migration needed)

### Recommended Transformation Order (Dependency-First)

1. **Bookstore.Domain** — Leaf library with zero NuGet packages; convert to SDK-style targeting net10.0
2. **Bookstore.Data** — Depends only on Bookstore.Domain; convert to SDK-style, upgrade AWS SDK packages, migrate EF6 to EF Core
3. **Bookstore.Cdk** — Depends only on Bookstore.Common (no migration needed); bump TFM from net6.0 to net10.0 and upgrade CDK packages
4. **Bookstore.Web** — Root web project depending on all others; full MVC 5 → ASP.NET Core MVC migration (migrate last)

## Key Findings

1. **ASP.NET MVC 5 to ASP.NET Core MVC rewrite required**: Bookstore.Web is built on System.Web.Mvc with 15 controllers, 44 Razor views, and an Admin area. All controllers must migrate from `System.Web.Mvc.Controller` to `Microsoft.AspNetCore.Mvc.Controller`, and views must be updated for Tag Helpers and ASP.NET Core conventions.

2. **OWIN authentication pipeline must be replaced**: The application uses Microsoft.Owin.Security with OpenIdConnect and Cookie authentication configured in `Startup.cs`. This entire pipeline must be rewritten to ASP.NET Core's built-in authentication middleware (`AddAuthentication().AddOpenIdConnect().AddCookie()`).

3. **Autofac DI container integration must change**: Autofac.Mvc5 (6.1.0) and Autofac.Owin (7.1.0) are .NET Framework-only with no modern .NET successor. Replace with `Autofac.Extensions.DependencyInjection` to integrate Autofac with ASP.NET Core's `IServiceProvider`, or migrate entirely to ASP.NET Core's built-in DI.

4. **Entity Framework 6 should migrate to EF Core**: EntityFramework 6.5.1 technically runs on netstandard2.1, but Microsoft recommends EF Core for new .NET development. The `ApplicationDbContext`, `BookstoreDbInitializer`, and 7 repository classes in Bookstore.Data need conversion to EF Core APIs.

5. **Three projects require legacy-to-SDK-style project format conversion**: Bookstore.Domain, Bookstore.Data, and Bookstore.Web use old-style `.csproj` with `<Import>` of MSBuild targets, `<Compile Include>` item lists, and `<Reference>` with `<HintPath>`. All must be converted to SDK-style format.

6. **AWS SDK packages require major version upgrade**: All AWSSDK.* packages are on v3.7.x; the latest stable versions are v4.0.x with improved netstandard2.0/net8.0 support. This is a major version change with potential breaking API changes.

7. **Bookstore.Common needs no migration**: Already targets netstandard2.0 and has no NuGet packages — it is fully cross-platform compatible and should be left unchanged.

## External Dependencies

| Dependency | Type | Impact |
|------------|------|--------|
| Amazon S3 | Cloud Service | Used for file storage (cover images); AWSSDK.S3 upgrade required |
| Amazon Rekognition | Cloud Service | Used for image validation; AWSSDK.Rekognition upgrade required |
| AWS Systems Manager (SSM) | Cloud Service | Used for configuration/secrets; AWSSDK.SimpleSystemsManagement upgrade required |
| Amazon CloudWatch Logs | Cloud Service | Used for logging via NLog + AWS.Logger.NLog; package upgrade required |
| OpenID Connect Provider | Identity | OWIN-based OIDC auth must be migrated to ASP.NET Core Authentication |
| SQL Server | Database | EF6 DbContext/connection strings must migrate to EF Core with appsettings.json |
| AWS CDK | Infrastructure | CDK project needs net6.0 → net10.0 TFM bump |

## Actionable Next Steps

1. **Phase 1 — Convert leaf libraries** (Low risk): Convert Bookstore.Domain to SDK-style net10.0 project. Verify it builds successfully before proceeding.

2. **Phase 2 — Migrate data layer** (Medium risk): Convert Bookstore.Data to SDK-style net10.0, upgrade AWSSDK packages to v4.x, migrate EF6 to EF Core (DbContext, repositories, initializer). Verify Bookstore.Data + Bookstore.Domain build together.

3. **Phase 3 — Upgrade CDK project** (Low risk): Bump Bookstore.Cdk TFM from net6.0 to net10.0 and upgrade Amazon.CDK.Lib, Cdklabs.CdkNag, and Constructs to latest versions.

4. **Phase 4 — Migrate web application** (High risk): Convert Bookstore.Web to SDK-style ASP.NET Core (Sdk.Web) net10.0 project. Replace OWIN auth with Core middleware, replace Autofac.Mvc5/Owin with Autofac.Extensions.DependencyInjection or Core DI, migrate Global.asax + Startup.cs to Program.cs, convert all controllers and views, remove bundling in favor of static files, and move Content/Scripts to wwwroot.

5. **Phase 5 — Integration verification** (Medium risk): Build the full solution, verify all project references resolve, test authentication flow, and validate ECS/CDK deployment configuration.

---

## Per-Project Assessment Details

### Bookstore.Domain

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net4.8 |
| **Lines of Code** | 1813 |
| **NuGet Packages** | 0 |
| **Project References** | 0 |
| **Complexity** | Low |
| **Estimated Changes** | 2 |



#### Migration Analysis

##### Migration Strategy

1. Convert old-style `.csproj` to SDK-style format: remove `<Import>` of `Microsoft.Common.props` and `Microsoft.CSharp.targets`, remove all `<Compile Include>` items (SDK-style uses globbing), remove `<Reference>` for framework assemblies (`System`, `System.Core`, `System.ComponentModel.DataAnnotations`, etc.).
2. Set `<TargetFramework>net10.0</TargetFramework>` and preserve `<RootNamespace>Bookstore.Domain</RootNamespace>` and `<AssemblyName>Bookstore.Domain</AssemblyName>`.
3. Remove `Properties/AssemblyInfo.cs` and set `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` (or let it default to true in SDK-style).

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| Minimal risk — pure domain model | Low | No NuGet packages, no framework-specific APIs; purely POCOs, interfaces, and domain services using `System.ComponentModel.DataAnnotations` (available in net10.0) |

##### Recommendations

1. Migrate this project first as it is the foundation leaf library with zero dependencies and zero NuGet packages — a safe starting point that validates the SDK-style conversion process.
2. Verify that all `System.ComponentModel.DataAnnotations` usages compile under net10.0 (they will — the namespace is part of the shared framework).

##### Cross-Project Impact

Bookstore.Domain is referenced by both Bookstore.Data and Bookstore.Web. It must be migrated and building successfully before either dependent project can proceed. Bookstore.Common (netstandard2.0, no migration) is not a dependency of this project.

---

### Bookstore.Data

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net4.8 |
| **Lines of Code** | 1042 |
| **NuGet Packages** | 4 |
| **Project References** | 1 |
| **Complexity** | Medium |
| **Estimated Changes** | 8 |

#### Package Compatibility (4 packages, 0 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| AWSSDK.Rekognition | 3.7.400.129 | COMPATIBLE | UpgradePackage |
| AWSSDK.S3 | 3.7.416.5 | COMPATIBLE | UpgradePackage |
| EntityFramework | 6.5.1 | COMPATIBLE | ReplacePackage |
| Magick.NET-Q8-AnyCPU | 14.6.0 | COMPATIBLE | UpgradePackage |

#### Project Dependencies (1)

- Bookstore.Domain

#### Legacy Files Inventory (1 files across 1 kinds)

| Kind | Files |
|------|------:|
| `.config` | 1 |

#### Migration Analysis

##### Migration Strategy

1. Convert old-style `.csproj` to SDK-style format: remove `<Import>` of `Microsoft.Common.props` and `Microsoft.CSharp.targets`, remove all `<Compile Include>` items, remove framework `<Reference>` elements. Keep `<PackageReference>` elements and `<ProjectReference>` to Bookstore.Domain.
2. Set `<TargetFramework>net10.0</TargetFramework>`, preserve `<RootNamespace>Bookstore.Data</RootNamespace>` and `<AssemblyName>Bookstore.Data</AssemblyName>`.
3. Remove `Properties/AssemblyInfo.cs` and rely on SDK-style auto-generation.
4. Replace `EntityFramework` 6.5.1 with `Microsoft.EntityFrameworkCore.SqlServer` (latest for net10.0). Migrate `ApplicationDbContext.cs` from `System.Data.Entity.DbContext` to `Microsoft.EntityFrameworkCore.DbContext`, update `BookstoreDbInitializer.cs` from `DropCreateDatabaseIfModelChanges<T>` to EF Core migrations or `EnsureCreated`, and convert all 7 repository classes from EF6 LINQ patterns to EF Core (e.g., `Include`/`ThenInclude` syntax).
5. Upgrade `AWSSDK.Rekognition` from 3.7.400.129 to latest 4.x and `AWSSDK.S3` from 3.7.416.5 to latest 4.x. Review for v4 breaking API changes.
6. Upgrade `Magick.NET-Q8-AnyCPU` from 14.6.0 to latest 14.17.2.
7. Remove `App.config` — configuration will be handled by `appsettings.json` in the web host and injected via DI.
8. Replace `BookstoreConfiguration.cs` references to `ConfigurationManager` with `IConfiguration` injection.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| EF6 to EF Core migration | High | `ApplicationDbContext`, `BookstoreDbInitializer`, and 7 repositories must be converted; EF Core has different navigation property loading, seeding, and migration semantics |
| AWSSDK v3 → v4 breaking changes | Medium | Major version bump may change async APIs, credential resolution, and service client constructors |
| ConfigurationManager usage | Low | `BookstoreConfiguration.cs` reads connection strings/settings via `System.Configuration`; must switch to injected `IConfiguration` |

##### Recommendations

1. Migrate EF6 to EF Core carefully — create EF Core migrations to match the existing database schema rather than relying on `EnsureCreated` in production.
2. Test AWSSDK v4 package upgrades in isolation to catch breaking API changes before integrating with the web project.
3. Introduce constructor injection for `IConfiguration` in `BookstoreConfiguration.cs` to replace static `ConfigurationManager` access.

##### Cross-Project Impact

Bookstore.Data depends on Bookstore.Domain (must be migrated first). Bookstore.Web depends on Bookstore.Data — the EF Core migration here directly impacts how the web project registers its `DbContext` in `Program.cs` (via `AddDbContext<ApplicationDbContext>`). The web project's `DependencyInjectionSetup.cs` currently configures Autofac registrations for repositories; these must align with the new EF Core context.

---

### Bookstore.Cdk

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net6.0 |
| **Lines of Code** | 595 |
| **NuGet Packages** | 4 |
| **Project References** | 1 |
| **Complexity** | Low |
| **Estimated Changes** | 5 |

#### Package Compatibility (4 packages, 0 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| Amazon.CDK.Lib | 2.188.0 | COMPATIBLE | UpgradePackage |
| Cdklabs.CdkNag | 2.35.66 | COMPATIBLE | UpgradePackage |
| Constructs | 10.4.2 | COMPATIBLE | UpgradePackage |
| Amazon.Jsii.Analyzers | * | COMPATIBLE | KeepPackage |

#### Project Dependencies (1)

- Bookstore.Common

#### Migration Analysis

##### Migration Strategy

1. Change `<TargetFramework>` from `net6.0` to `net10.0` in the SDK-style `.csproj` (already SDK-style — no format conversion needed).
2. Remove `<RollForward>Major</RollForward>` — it is no longer needed when targeting net10.0 directly.
3. Upgrade `Amazon.CDK.Lib` from 2.188.0 to latest 2.x stable version.
4. Upgrade `Cdklabs.CdkNag` from 2.35.66 to latest stable version.
5. Upgrade `Constructs` from 10.4.2 to latest 10.x stable version.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| CDK construct API changes | Low | CDK Lib 2.x maintains backward compatibility within the v2 line; minor API additions are non-breaking |
| net6.0 → net10.0 runtime behavior | Low | CDK is a build-time code-generation tool; runtime behavior changes in .NET 10 are unlikely to affect CDK synthesis |

##### Recommendations

1. Upgrade CDK packages to the latest 2.x versions and run `cdk synth` to verify CloudFormation template generation is unaffected.
2. Review `EcsStack.cs` after Bookstore.Web migration to ensure the Docker/ECS configuration aligns with the new ASP.NET Core application startup (port binding, health checks).

##### Cross-Project Impact

Bookstore.Cdk depends only on Bookstore.Common (netstandard2.0, no migration needed). It is independent of the Bookstore.Domain → Data → Web migration chain and can be upgraded in parallel. However, the ECS stack definition may need updates after the web project migrates (e.g., container port, health check path).

---

### Bookstore.Web

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net4.8 |
| **Lines of Code** | 5136 |
| **NuGet Packages** | 54 |
| **Project References** | 3 |
| **Complexity** | Critical |
| **Estimated Changes** | 85 |

#### Package Compatibility (54 packages, 12 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| Antlr | 3.5.0.2 | COMPATIBLE | ReplacePackage |
| Autofac | 8.2.1 | COMPATIBLE | KeepPackage |
| Autofac.Mvc5 | 6.1.0 | INCOMPATIBLE | ReplacePackage |
| Autofac.Owin | 7.1.0 | INCOMPATIBLE | ReplacePackage |
| AWS.Logger.Core | 3.3.3 | COMPATIBLE | UpgradePackage |
| AWS.Logger.NLog | 3.3.4 | COMPATIBLE | UpgradePackage |
| AWSSDK.CloudWatchLogs | 3.7.410.17 | COMPATIBLE | UpgradePackage |
| AWSSDK.Core | 3.7.402.35 | COMPATIBLE | UpgradePackage |
| AWSSDK.Rekognition | 3.7.400.129 | COMPATIBLE | UpgradePackage |
| AWSSDK.S3 | 3.7.416.5 | COMPATIBLE | UpgradePackage |
| AWSSDK.SimpleSystemsManagement | 3.7.404.10 | COMPATIBLE | UpgradePackage |
| EntityFramework | 6.5.1 | COMPATIBLE | ReplacePackage |
| jQuery | 3.7.1 | COMPATIBLE | ReplacePackage |
| jQuery.Validation | 1.21.0 | COMPATIBLE | ReplacePackage |
| Microsoft.AspNet.Mvc | 5.3.0 | INCOMPATIBLE | ReplacePackage |
| Microsoft.AspNet.Razor | 3.3.0 | INCOMPATIBLE | ReplacePackage |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | INCOMPATIBLE | ReplacePackage |
| Microsoft.AspNet.WebPages | 3.3.0 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Bcl.AsyncInterfaces | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.Bcl.Memory | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.Bcl.TimeProvider | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 4.1.0 | COMPATIBLE | ReplacePackage |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.Extensions.Logging.Abstractions | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.IdentityModel.Abstractions | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.JsonWebTokens | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Logging | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Protocols | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Tokens | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.jQuery.Unobtrusive.Validation | 4.0.0 | COMPATIBLE | ReplacePackage |
| Microsoft.Owin | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Host.SystemWeb | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Security | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Security.Cookies | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Security.OpenIdConnect | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Web.Infrastructure | 2.0.1 | COMPATIBLE | ReplacePackage |
| Modernizr | 2.8.3 | COMPATIBLE | ReplacePackage |
| Newtonsoft.Json | 13.0.3 | COMPATIBLE | UpgradePackage |
| NLog | 5.4.0 | COMPATIBLE | UpgradePackage |
| Owin | 1.0 | INCOMPATIBLE | ReplacePackage |
| System.Buffers | 4.6.1 | COMPATIBLE | ReplacePackage |
| System.Diagnostics.DiagnosticSource | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.IdentityModel.Tokens.Jwt | 8.7.0 | COMPATIBLE | KeepPackage |
| System.IO.Pipelines | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.Memory | 4.6.3 | COMPATIBLE | ReplacePackage |
| System.Numerics.Vectors | 4.6.1 | COMPATIBLE | ReplacePackage |
| System.Runtime.CompilerServices.Unsafe | 6.1.2 | COMPATIBLE | ReplacePackage |
| System.Text.Encoding | 4.3.0 | COMPATIBLE | ReplacePackage |
| System.Text.Encodings.Web | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.Text.Json | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.Threading.Tasks.Extensions | 4.6.3 | COMPATIBLE | ReplacePackage |
| System.ValueTuple | 4.6.1 | COMPATIBLE | ReplacePackage |
| WebGrease | 1.6.0 | COMPATIBLE | ReplacePackage |

#### Project Dependencies (3)

- Bookstore.Common
- Bookstore.Data
- Bookstore.Domain

#### Legacy Files Inventory (51 files across 3 kinds)

| Kind | Files |
|------|------:|
| `.asax` | 1 |
| `.config` | 6 |
| `.cshtml` | 44 |

#### Migration Analysis

##### Migration Strategy

1. **Convert project format**: Replace old-style `.csproj` with SDK-style `Microsoft.NET.Sdk.Web` targeting `net10.0`. Remove all `<Compile Include>`, `<Content Include>`, `<Reference>` elements. Convert `packages.config` to `<PackageReference>` elements. Remove `Properties/AssemblyInfo.cs`.
2. **Remove 12 incompatible packages**: Remove Microsoft.AspNet.Mvc (5.3.0), Microsoft.AspNet.Razor (3.3.0), Microsoft.AspNet.WebPages (3.3.0), Microsoft.AspNet.Web.Optimization (1.1.3), Microsoft.Owin (4.2.2), Microsoft.Owin.Host.SystemWeb (4.2.2), Microsoft.Owin.Security (4.2.2), Microsoft.Owin.Security.Cookies (4.2.2), Microsoft.Owin.Security.OpenIdConnect (4.2.2), Owin (1.0), Autofac.Mvc5 (6.1.0), Autofac.Owin (7.1.0). These are replaced by ASP.NET Core built-in framework, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, and `Autofac.Extensions.DependencyInjection`.
3. **Remove polyfill/BCL packages**: Remove 14 System.* polyfill packages (System.Buffers, System.Memory, System.Numerics.Vectors, System.Runtime.CompilerServices.Unsafe, System.Text.Encoding, System.Text.Encodings.Web, System.Text.Json, System.Threading.Tasks.Extensions, System.ValueTuple, System.IO.Pipelines, System.Diagnostics.DiagnosticSource, Microsoft.Bcl.AsyncInterfaces, Microsoft.Bcl.Memory, Microsoft.Bcl.TimeProvider) — all built into net10.0 runtime. Remove Microsoft.Extensions.DependencyInjection.Abstractions and Microsoft.Extensions.Logging.Abstractions (provided by the shared framework).
4. **Remove content/tooling packages**: Remove jQuery (3.7.1), jQuery.Validation (1.21.0), Microsoft.jQuery.Unobtrusive.Validation (4.0.0), Modernizr (2.8.3), Antlr (3.5.0.2), WebGrease (1.6.0), Microsoft.CodeDom.Providers.DotNetCompilerPlatform (4.1.0), Microsoft.Web.Infrastructure (2.0.1). Deliver client-side assets via `wwwroot/lib/` or LibMan instead of NuGet.
5. **Upgrade retained packages**: Upgrade AWS.Logger.Core to 4.0.3, AWS.Logger.NLog to 5.0.1, AWSSDK.CloudWatchLogs to latest 4.x, AWSSDK.Core to latest 4.x, AWSSDK.Rekognition to latest 4.x, AWSSDK.S3 to latest 4.x, AWSSDK.SimpleSystemsManagement to latest 4.x, Newtonsoft.Json to 13.0.4, NLog to latest 6.x. Replace EntityFramework 6.5.1 with Microsoft.EntityFrameworkCore (handled in Bookstore.Data).
6. **Replace Global.asax + OWIN Startup with Program.cs**: Merge `Global.asax.cs` (application lifecycle, route registration, bundle config, filter config, DI setup, logging setup, configuration setup) and `Startup.cs` (OWIN OpenIdConnect + Cookie authentication) into a single `Program.cs` using ASP.NET Core's `WebApplicationBuilder`.
7. **Migrate authentication**: Replace OWIN `UseCookieAuthentication` + `UseOpenIdConnectAuthentication` from `Startup.cs` and `AuthenticationSetup.cs` with ASP.NET Core `builder.Services.AddAuthentication().AddCookie().AddOpenIdConnect()`. Migrate `LocalAuthenticationMiddleware.cs` (OWIN middleware) to ASP.NET Core middleware. Update `ClaimsPrincipalExtensions.cs` and `HttpContextExtensions.cs` to remove `System.Web` / `Microsoft.Owin` dependencies.
8. **Migrate DI**: Replace Autofac.Mvc5 `DependencyInjectionSetup.cs` (`DependencyResolver.SetResolver`) with either `builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())` + `ConfigureContainer<ContainerBuilder>` or convert all registrations to ASP.NET Core native DI (`builder.Services.AddScoped/AddTransient/AddSingleton`).
9. **Migrate controllers**: Update all 15 controllers (9 in Controllers/, 6 in Areas/Admin/Controllers/) from `System.Web.Mvc.Controller` to `Microsoft.AspNetCore.Mvc.Controller`. Replace `HttpPostedFileBase` with `IFormFile`, `SelectList` adjustments, `JsonRequestBehavior.AllowGet` removal, `Url.Action` / `RedirectToAction` signature updates.
10. **Migrate views**: Update 44 `.cshtml` files — replace `@Html.ActionLink` / `@Html.BeginForm` with Tag Helpers (`<a asp-action>`, `<form asp-action>`), remove `@Scripts.Render` / `@Styles.Render` bundle references, update `_ViewImports.cshtml` with `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`.
11. **Migrate routing and areas**: Replace `RouteConfig.cs` / `RouteTable.Routes.MapRoute` with endpoint routing in `Program.cs` (`app.MapControllerRoute`). Replace `AdminAreaRegistration.cs` with `[Area("Admin")]` attribute-based area routing.
12. **Migrate static files**: Move `Content/` and `Scripts/` directories to `wwwroot/`. Replace `BundleConfig.cs` bundling/minification with static file serving (`app.UseStaticFiles()`). Update view references accordingly.
13. **Migrate configuration**: Replace `Web.config` `<appSettings>` / `<connectionStrings>` with `appsettings.json`. Remove `ConfigurationSetup.cs` static `ConfigurationManager` usage in favor of `IConfiguration` injection. Remove `Web.Debug.config` / `Web.Release.config` transforms in favor of `appsettings.Development.json` / `appsettings.Production.json`.
14. **Migrate logging**: Update `LoggingSetup.cs` from NLog with `Web.config`-based configuration to NLog with `Microsoft.Extensions.Logging` integration (`NLog.Web.AspNetCore`).

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| OWIN → Core auth migration | High | OpenIdConnect + Cookie auth pipeline must be rewritten; token validation, claims transformation, and session handling may differ |
| Autofac.Mvc5/Owin replacement | High | DI registrations in `DependencyInjectionSetup.cs` must be mapped to Core DI or Autofac.Extensions.DependencyInjection; lifetime scopes may differ |
| MVC 5 → Core MVC controller changes | High | 15 controllers with `System.Web.Mvc` types, `HttpPostedFileBase`, `JsonRequestBehavior`, and area registration must be rewritten |
| 44 Razor views with MVC 5 helpers | Medium | `@Html.ActionLink`, `@Html.BeginForm`, `@Scripts.Render`, `@Styles.Render` must all be converted to ASP.NET Core equivalents |
| BundleConfig removal | Medium | `BundleConfig.cs` with `ScriptBundle`/`StyleBundle` has no direct Core equivalent; must switch to static file serving or a bundler tool |
| AWSSDK v3 → v4 breaking changes | Medium | Major version bump across 5 AWS SDK packages |
| Admin area registration | Medium | `AdminAreaRegistration.cs` uses MVC 5's `AreaRegistration` pattern; must convert to attribute-based `[Area]` routing |
| IOwinRequestExtensions usage | Medium | Custom OWIN request extensions must be rewritten for `HttpContext` |
| Static file path changes | Low | `Content/` and `Scripts/` paths change to `wwwroot/`; view references must be updated |

##### Recommendations

1. Tackle the OWIN-to-Core auth migration early in the web project transformation — it is the highest-risk component and gates the ability to test the application end-to-end.
2. Consider converting Autofac registrations to ASP.NET Core built-in DI to reduce dependency count, unless complex lifetime management (e.g., named registrations, property injection) is used in `DependencyInjectionSetup.cs`.
3. Use `Microsoft.AspNetCore.Authentication.OpenIdConnect` as the direct replacement for `Microsoft.Owin.Security.OpenIdConnect` — it provides equivalent OIDC functionality with ASP.NET Core's middleware model.
4. Move `Content/` and `Scripts/` to `wwwroot/` early and update all view references in a single pass using bulk find-and-replace.
5. Add `Microsoft.AspNetCore.Authentication.OpenIdConnect` NuGet package explicitly for OIDC support.

##### Cross-Project Impact

Bookstore.Web is the root of the dependency tree — it depends on Bookstore.Common (no migration), Bookstore.Domain (SDK conversion), and Bookstore.Data (SDK conversion + EF Core migration). All three must be fully migrated and building before the web project migration can begin. The web project's `DependencyInjectionSetup.cs` registers types from Bookstore.Data and Bookstore.Domain — these registrations must align with the migrated project APIs. The CDK project's `EcsStack.cs` defines the container configuration for the web app and may need updates for the new ASP.NET Core port binding and health check endpoints.

---

### Bookstore.Common

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | netstandard2.0 |
| **Lines of Code** | 6 |
| **NuGet Packages** | 0 |
| **Project References** | 0 |
| **Complexity** | N/A |
| **Estimated Changes** | 0 |

#### Migration Analysis

##### Migration Strategy

No migration needed. Bookstore.Common targets `netstandard2.0`, which is fully compatible with net10.0. The project contains only a single `Constants.cs` file with shared constant values. Leave it completely unchanged.

##### Risks & Architectural Concerns

No risks. The `netstandard2.0` target is cross-platform compatible and will be consumed by both the migrated net10.0 projects (Bookstore.Web, Bookstore.Cdk) without any issues.

##### Recommendations

1. Do not change the target framework — `netstandard2.0` ensures maximum compatibility across all consumers.
2. No action required for this project.

##### Cross-Project Impact

Bookstore.Common is referenced by Bookstore.Web and Bookstore.Cdk. Since it requires no migration, it poses no risk to either consumer. It will continue to work as-is after all dependent projects are migrated to net10.0.

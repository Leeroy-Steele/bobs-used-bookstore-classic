using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Web;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Populate BookstoreConfiguration from IConfiguration (converts ":" to "/" for key compatibility)
ConfigurationSetup.InitializeConfiguration(builder.Configuration);

// Configure AWS SSM overrides if needed
ConfigurationSetup.ConfigureAwsSettings();

// Configure NLog
LoggingSetup.ConfigureLogging(builder.Logging, builder.Configuration);

// Add ASP.NET Core MVC with a global Authorize filter (allow anonymous on specific actions)
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Configure authentication
AuthenticationConfig.ConfigureAuthentication(builder.Services, builder.Configuration);

// Register application services (DI)
DependencyInjectionSetup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Local auth middleware (bypasses Cognito when running locally)
if (BookstoreConfiguration.TryGetSetting("Services/Authentication") != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Ensure database is created and seeded on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    BookstoreDbInitializer.Seed(dbContext);
}

LogManager.Shutdown();
app.Run();

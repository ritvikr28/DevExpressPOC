using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Security.Resources;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Web.Extensions;
using DXApplication1.Services;
using ESS.Platform.Authorization.Authentication;
using ESS.Platform.Authorization.Dependencies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

AppDomain.CurrentDomain.SetData("DataDirectory", builder.Environment.ContentRootPath);
builder.Services.AddDevExpressControls();

// Register Azure Blob Storage service
builder.Services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

//// Configure JWT Bearer token validation.
//// Authority is intentionally omitted to avoid OIDC discovery network calls that can
//// fail and block all token validation. All validation flags are disabled for
//// development/testing purposes; in production these should be enabled and configured.
//// The OnMessageReceived event parses the JWT structure and authenticates the principal
//// without signature validation — bypassing any handler-specific validation paths.
//if (!builder.Environment.IsDevelopment())
//{
//    throw new InvalidOperationException(
//        "JWT token validation bypass is only allowed in the Development environment. " +
//        "Configure Authority and signing keys for non-development deployments.");
//}

// Static handler instance reused across all requests.
//var devJwtHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

//builder.Services.AddAuthentication("Bearer")
//    .AddJwtBearer("Bearer", options =>
//    {
//        // Only disable HTTPS metadata validation in development (no Authority is set, so
//        // this is a no-op in practice, but kept explicit for clarity).
//        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

//        // NOTE: All validation flags are disabled for development/testing.
//        // In production these must be enabled and an Authority/signing keys configured.
//        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//        {
//            ValidateIssuer = false,
//            ValidateAudience = false,
//            ValidateLifetime = false,
//            ValidateIssuerSigningKey = false,
//            RequireSignedTokens = false
//        };

//        options.Events = new JwtBearerEvents
//        {
//            // OnMessageReceived fires before the token handler runs.
//            // Setting context.Principal + context.Success() short-circuits the rest
//            // of the validation pipeline, so no OIDC discovery or signature check occurs.
//            OnMessageReceived = context =>
//            {
//                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
//                if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
//                {
//                    var token = authHeader.Substring("Bearer ".Length).Trim();
//                    if (!string.IsNullOrEmpty(token))
//                    {
//                        try
//                        {
//                            if (devJwtHandler.CanReadToken(token))
//                            {
//                                var jwt = devJwtHandler.ReadJwtToken(token);
//                                var identity = new System.Security.Claims.ClaimsIdentity(jwt.Claims, "Bearer");
//                                context.Principal = new System.Security.Claims.ClaimsPrincipal(identity);
//                                context.Success();

//                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//                                logger.LogDebug("Token parsed for user: {Username}", context.Principal.Identity?.Name);
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//                            logger.LogWarning("Failed to parse JWT token: {Message}", ex.Message);
//                        }
//                    }
//                }
//                return Task.CompletedTask;
//            }
//        };
//    });

builder.Services.AddClaimResolverServiceCollection();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddScheme<JwtBearerOptions, JwtAuthenticationHandler>(JwtBearerDefaults.AuthenticationScheme, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ReportStorageWebExtension, CustomReportStorageWebExtension>();
builder.Services.AddMvc();
builder.Services.AddControllers();
builder.Services.ConfigureReportingServices(configurator =>
{
    if (builder.Environment.IsDevelopment())
        configurator.UseDevelopmentMode();

    configurator.ConfigureReportDesigner(designerConfigurator =>
    {
        // Register your API-based connection provider for the designer
        designerConfigurator.RegisterDataSourceWizardJsonConnectionStorage<CustomApiDataConnectionStorage>(true);
    });
    configurator.ConfigureWebDocumentViewer(viewerConfigurator =>
    {
        viewerConfigurator.UseCachedReportSourceBuilder();
        // Register your API-based connection provider for the viewer
        viewerConfigurator.RegisterJsonDataConnectionProviderFactory<CustomJsonDataConnectionProviderFactory>();
    });
});

var app = builder.Build();
var contentDirectoryAllowRule = DirectoryAccessRule.Allow(new DirectoryInfo(Path.Combine(app.Environment.ContentRootPath, "Content")).FullName);
AccessSettings.ReportingSpecificResources.SetRules(contentDirectoryAllowRule, UrlAccessRule.Deny());
DevExpress.XtraReports.Configuration.Settings.Default.UserDesignerOptions.DataBindingMode = DevExpress.XtraReports.UI.DataBindingMode.Expressions;

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseDevExpressControls();
System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
app.UseEndpoints(endpoints => endpoints.MapControllers());

app.MapFallbackToFile("/index.html");

app.Run();
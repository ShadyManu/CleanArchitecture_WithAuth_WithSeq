using Application;
using Carter;
using Infrastructure;
using Infrastructure.Data;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Presentation;
using Serilog;
using Web;
using Web.Constants;

// Bootstrap logger: captures anything logged before the host (and its configuration) is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "CCtemplate.Web")
            .WriteTo.Console();

        var seqServerUrl = context.Configuration["Seq:ServerUrl"];
        if (!string.IsNullOrWhiteSpace(seqServerUrl))
            configuration.WriteTo.Seq(seqServerUrl);
    });

    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(builder.Environment.ApplicationName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                var endpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    options.Endpoint = new Uri(endpoint);
                }

                options.Protocol = builder.Configuration.GetValue<OtlpExportProtocol>(
                    "OpenTelemetry:OtlpProtocol");
            }));

    // Add services from the different layers.
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationDependencyInjection();
    builder.Services.AddPresentationDependencyInjection();
    builder.Services.AddWebDependencyInjection(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseCors(CorsPolicyConstants.LocalPolicy);
    }
    else
    {
        app.UseExceptionHandler("/error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseCors(CorsPolicyConstants.ProdPolicy);
    }

    var shouldApplyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigration");
    if (shouldApplyMigrations)
    {
        await app.Services.MigrateAsync();
    }

    var shouldSeedData = app.Configuration.GetValue<bool>("Database:SeedData");
    if (shouldSeedData)
    {
        await app.Services.SeedAsync();
    }

    app.MapFallbackToFile("index.html");

    app.UseForwardedHeaders();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    app.MapCarter(); // Will add with reflection all endpoints which extend ICarterModule

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown on purpose by EF Core design-time tools
    // (e.g. `dotnet ef migrations add`) once they've extracted the DbContext; it's not a real crash.
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace Web
{
    public partial class Program { }
}

using Bogus;
using Microsoft.EntityFrameworkCore;
using opentelem.Data;
using opentelem.Middleware;
using opentelem.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
// using OpenTelemetry.Exporter.Prometheus;
// using System.Diagnostics.Metrics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Meter meter = new("MyApiService.QueryMetrics", "1.0.0");
// Histogram<double> queryDurationHistogram = meter.CreateHistogram<double>("sql.query.duration", unit: "seconds", description: "Duration of SQL queries");

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=sqlserver;Database=MyApiDb;User=sa;Password=Your_password123;TrustServerCertificate=true;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
      sqlOptions.CommandTimeout(60);
    }));

builder.Services.AddControllers();

builder.Services.AddOpenTelemetry()
    .WithMetrics(meterProviderBuilder =>
    {
      meterProviderBuilder
          .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApiService"))
          .AddAspNetCoreInstrumentation()
          .AddSqlClientInstrumentation()
          .AddPrometheusExporter();
    })
    .WithTracing(tracerProviderBuilder =>
    {
      tracerProviderBuilder
          .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApiService"))
          .AddAspNetCoreInstrumentation()
          .AddSqlClientInstrumentation(options =>
          {
            options.SetDbStatementForText = true;
            options.Enrich = (activity, eventName, rawObject) =>
              {
                if (rawObject is System.Data.Common.DbCommand command)
                {
                  foreach (System.Data.Common.DbParameter param in command.Parameters)
                  {
                    activity.SetTag($"db.param.{param.ParameterName}", param.Value);
                  }
                }
              };
          })
          .AddEntityFrameworkCoreInstrumentation()
          .AddConsoleExporter();
    });

WebApplication app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
  ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  dbContext.Database.Migrate();

  if (dbContext.Products.Count() <= 10000)
  {
    SeedDatabase(dbContext);
  }
}

app.Run();

static void SeedDatabase(ApplicationDbContext context)
{
  Faker<Product> productFaker = new Faker<Product>()
    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
    .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000));

  List<Product> products = productFaker.Generate(10000);

  context.Products.AddRange(products);
  context.SaveChanges();
}

using Bogus;
using Microsoft.EntityFrameworkCore;
using opentelem.Data;
using opentelem.Middleware;
using opentelem.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=sqlserver;Database=MyApiDb;User=sa;Password=Your_password123;TrustServerCertificate=true;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();

builder.Services.AddOpenTelemetry()
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
      // if everything is working as expected, you can replace the ConsoleExporter with the following:
      // .AddApplicationInsightsExporter(o =>
      // {
      //     // Replace with your actual Application Insights connection string
      //     o.ConnectionString = "InstrumentationKey=INSTRUMENTATION_KEY"
      // });
    });

WebApplication app = builder.Build();

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

using Bogus;
using Microsoft.EntityFrameworkCore;
using opentelem.Data;
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

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    if (dbContext.Products.Count() <= 10000)
    {
        SeedDatabase(dbContext);
    }
}

app.Run();

static void SeedDatabase(ApplicationDbContext context)
{
    var productFaker = new Faker<Product>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000));

    var products = productFaker.Generate(10000);

    context.Products.AddRange(products);
    context.SaveChanges();
}

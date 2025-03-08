using Bogus;
using Microsoft.EntityFrameworkCore;
using opentelem.Data;
using opentelem.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Connection string for SQL Server (from environment variables or configuration)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=sqlserver;Database=MyApiDb;User=sa;Password=Your_password123;TrustServerCertificate=true;";

// Add EF Core DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add controllers (or minimal endpoints as preferred)
builder.Services.AddControllers();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApiService"))
            .AddAspNetCoreInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                // Optionally configure filtering to capture query details
                options.SetDbStatementForText = true;
            })
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter();
            // .AddApplicationInsightsExporter(o =>
            // {
            //     // Replace with your actual Application Insights connection string
            //     o.ConnectionString = "InstrumentationKey=YOUR_INSTRUMENTATION_KEY;IngestionEndpoint=https://YOUR_REGION.endpoint.applicationinsights.azure.com/";
            // });
    });

var app = builder.Build();

// Configure middleware
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Apply pending migrations
    dbContext.Database.Migrate();

    // Seed data if needed
    if (dbContext.Products.Count() <= 10000)
    {
        SeedDatabase(dbContext);
    }
}

app.Run();

static void SeedDatabase(ApplicationDbContext context)
{
    // Configure a Faker for Product
    var productFaker = new Faker<Product>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000));

    // Generate a large number of products (e.g., 10,000)
    var products = productFaker.Generate(10000);

    context.Products.AddRange(products);
    context.SaveChanges();

    Console.WriteLine("Database seeded with sample data.");
}

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Safety net: if an entity with navigation properties ever gets returned, don't loop forever.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session (used for storing UserId and Role after login)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Where banner and profile images are stored. "Local" (default) saves them in wwwroot/uploads
// (allowed by the lecturer instead of Azure). Set "Storage:Provider": "Azure" to use Azure Blob Storage.
builder.Services.Configure<AzureBlobOptions>(builder.Configuration.GetSection("AzureBlob"));
if (string.Equals(builder.Configuration["Storage:Provider"], "Azure", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
else
    builder.Services.AddSingleton<IBlobStorageService, LocalFileStorageService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for the RaceDay event management platform (PROG6212 POE Part 2). " +
                      "Register and log in first: login starts a session, and the Organiser-only and " +
                      "Participant-only endpoints check the role stored in that session."
    });

    // Pull the /// summaries, parameter notes and response descriptions into the Swagger UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

// Development only: create the database and tables if they do not exist yet, so the project runs on a fresh machine.
// (If the database already exists from your migrations, this does nothing.)
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RaceDayAPI.Data.RaceDayDbContext>().Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not create or reach the database. Check ConnectionStrings:DefaultConnection.");
    }
}

// --- Middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    });
}

// Redirecting http to https would break the web app, which calls this API over plain http locally and in Docker.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // serves wwwroot/uploads images
app.UseSession(); // must come before UseAuthorization / controllers that read session

app.UseAuthorization();

app.MapControllers();

app.Run();

// Needed so the Tests project can reference the entry point via WebApplicationFactory<Program>
public partial class Program { }

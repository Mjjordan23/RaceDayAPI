using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllersWithViews(options =>
{
    // Every POST form must carry an anti-forgery token (the form tag helper adds it for us).
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// The web app keeps its own session: who is logged in, their role, and the API's session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDayWeb.Session"; // different from the API's cookie, because localhost cookies are shared across ports
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// The one place that talks to the Part 2 API. The address comes from ApiSettings:BaseUrl
// (overridden in Docker with the ApiSettings__BaseUrl environment variable).
RaceDay.Web.Helpers.Fmt.PublicApiBase = builder.Configuration["ApiSettings:PublicBaseUrl"] ?? "http://localhost:5254/";
var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5254/";
if (!baseUrl.EndsWith('/')) baseUrl += "/";

builder.Services.AddHttpClient<ApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,        // ApiService passes the API session cookie by hand, per logged-in user
    AllowAutoRedirect = false
});

var app = builder.Build();

// --- Middleware pipeline ---
// Friendly error pages instead of raw errors.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

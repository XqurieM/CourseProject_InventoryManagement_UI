using InventoryManagement.UI.Filters;
using InventoryManagement.UI.Options;
using InventoryManagement.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BackendApiOptions>(builder.Configuration.GetSection(BackendApiOptions.SectionName));
builder.Services.Configure<MicrosoftLoginOptions>(builder.Configuration.GetSection(MicrosoftLoginOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".InventoryManagement.UI.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddHttpClient(BackendApiClient.HttpClientName);

builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<IAuthenticationFacade, AuthenticationFacade>();
builder.Services.AddScoped<IInventoryFacade, InventoryFacade>();
builder.Services.AddScoped<IDashboardFacade, DashboardFacade>();
builder.Services.AddScoped<IAdminFacade, AdminFacade>();
builder.Services.AddScoped<IUiLocalizationService, UiLocalizationService>();
builder.Services.AddScoped<BackendApiClient>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<PopulateLayoutStateFilter>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/Index");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

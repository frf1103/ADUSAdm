using ADUSAdm.Data;
using ADUSAdm.FormatingConfiguration;
using ADUSAdm.Shared;
using MathNet.Numerics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var urlconvite = builder.Configuration.GetSection("AppSettings").GetSection("urlconvite").Value;
var urlAPI = builder.Configuration.GetSection("AppSettings").GetSection("urlapi").Value;
builder.Services.AddDbContext<ADUScontext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

/*builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ADUScontext>();
*/

//builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//    .AddEntityFrameworkStores<ADUScontext>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
})
    .AddEntityFrameworkStores<ADUScontext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/Auth/Login");

builder.Services.AddHttpClient<ADUSClient.Controller.MoedaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.ParceiroControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.SharedControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.AssinaturaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.ParceiroControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.ParcelaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.ParametroGuruControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.BancoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.ContaCorrenteControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.MovimentoCaixaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.CentroCustoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.PlanoContaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.TransacaoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.TransacBancoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<ADUSClient.Controller.ConviteControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<ADUSAdm.Shared.IEmailSender, EmailSender>();
//builder.Services.AddTransient<IUsuarioService, UsuContservice>();

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
});

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Adicionar IHttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<SessionManager>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<ImportacaoService>();
builder.Services.AddSignalR();

// Carregar configurações de cultura do appsettings.json

/*
var supportedCultures = builder.Configuration.GetSection("Culture:SupportedCultures").Get<string[]>();
var defaultCulture = builder.Configuration["Culture:DefaultCulture"];

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCulturesList = supportedCultures.Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = supportedCulturesList;
    options.SupportedUICultures = supportedCulturesList;

    options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
    {
        // Here you can customize the culture selection logic
        return new ProviderCultureResult(defaultCulture);
    }));
});
*/

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .SetBasePath(Directory.GetCurrentDirectory())
    .Build();

var sectionDev = config.GetSection("AMBIENTE:DEV");

if (sectionDev.Exists() && sectionDev.Value.ToString() == "0")
{
    builder.WebHost.ConfigureKestrel((context, serverOptions) =>
    {
        serverOptions.Listen(System.Net.IPAddress.Any, config.GetValue<int>("HOST:HTTP"));
        serverOptions.Listen(System.Net.IPAddress.Any, config.GetValue<int>("HOST:HTTPS"));
    });
}

//builder.Services.AddScoped<ImportacaoService>();

// ?? Se estiver usando SignalR
//builder.Services.AddSignalR();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ADUScontext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    /*
    builder.WebHost.ConfigureKestrel((context, serverOptions) =>
    {
        serverOptions.Listen(System.Net.IPAddress.Any, config.GetValue<int>("HOST:HTTP"));
        serverOptions.Listen(System.Net.IPAddress.Any, config.GetValue<int>("HOST:HTTPS"));
    }); */

    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Configurar SessionManager
var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
//SessionManager.Configure(httpContextAccessor);

var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//app.MapRazorPages();

app.MapHub<ImportProgressHub>("/hub/importProgress");
app.Run();
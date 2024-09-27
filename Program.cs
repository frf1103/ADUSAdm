using FarmPlannerAdm.Data;
using FarmPlannerAdm.FormatingConfiguration;
using FarmPlannerAdm.Shared;
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
builder.Services.AddDbContext<FarmPlannercontext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

/*builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<FarmPlannercontext>();
*/

//builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//    .AddEntityFrameworkStores<FarmPlannercontext>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
})
    .AddEntityFrameworkStores<FarmPlannercontext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/Auth/Login");

builder.Services.AddHttpClient<FarmPlannerClient.Controller.CulturaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.TecnologiaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.MoedaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.ClasseContaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.GrupoProdutoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.MarcaMaquinaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.PrincipioAtivoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.RegiaoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.TipoOperacaoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.ModeloMaquinaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.ParceiroControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.ContaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.OrganizacaoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.PreferUsuControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.AnoAgricolaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.FazendaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.SharedControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.ProdutoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.OperacaoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.MaquinaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.ConfigAreaControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.UnidadeControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.PlanejOperacaoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.PlanejamentoCompraControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.OrcamentoProdutoControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.AddHttpClient<FarmPlannerClient.Controller.PedidoCompraControllerClient>(client =>
{
    client.BaseAddress = new Uri(urlAPI.ToString());
});

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<FarmPlannerAdm.Shared.IEmailSender, EmailSender>();
builder.Services.AddTransient<IUsuarioService, UsuContservice>();

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
/*
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.Listen(System.Net.IPAddress.Any, config.GetValue<int>("HOST:HTTP"));
    serverOptions.Listen(System.Net.IPAddress.Any, config.GetValue<int>("HOST:HTTPS"));
});
*/
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FarmPlannercontext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
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

app.Run();
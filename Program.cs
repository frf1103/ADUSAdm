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

// --------------------------------------
// Configuração do banco de dados
// --------------------------------------

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ADUScontext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --------------------------------------
// Identity e autenticação
// --------------------------------------

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
})
    .AddEntityFrameworkStores<ADUScontext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
});

// --------------------------------------
// Configuração dos HttpClients
// --------------------------------------

var urlAPI = builder.Configuration.GetSection("AppSettings").GetSection("urlapi").Value;

builder.Services.AddHttpClient<ADUSClient.Controller.MoedaControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.ParceiroControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.SharedControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.AssinaturaControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.ParcelaControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.ParametroGuruControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.BancoControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.ContaCorrenteControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.MovimentoCaixaControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.CentroCustoControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.PlanoContaControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.TransacaoControllerClient>(c => c.BaseAddress = new Uri(urlAPI));
builder.Services.AddHttpClient<ADUSClient.Controller.TransacBancoControllerClient>(c => c.BaseAddress = new Uri(urlAPI));

// --------------------------------------
// Configuração de serviços auxiliares
// --------------------------------------

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<ADUSAdm.Shared.IEmailSender, EmailSender>();

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

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<SessionManager>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<ImportacaoService>();
builder.Services.AddSignalR();

// --------------------------------------
// Configuração de Localização (opcional)
// --------------------------------------

var supportedCultures = builder.Configuration.GetSection("Culture:SupportedCultures").Get<string[]>();
var defaultCulture = builder.Configuration["Culture:DefaultCulture"];

if (supportedCultures != null && supportedCultures.Any() && !string.IsNullOrEmpty(defaultCulture))
{
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCulturesList = supportedCultures.Select(c => new CultureInfo(c)).ToList();

        options.DefaultRequestCulture = new RequestCulture(defaultCulture);
        options.SupportedCultures = supportedCulturesList;
        options.SupportedUICultures = supportedCulturesList;

        options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
        {
            return new ProviderCultureResult(defaultCulture);
        }));
    });
}

var app = builder.Build();

// ✅ ✅ ADICIONADO PARA FUNCIONAR EM APLICATIVO VIRTUAL:
var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

// --------------------------------------
// Migrations (opcional, cuidado em produção)
// --------------------------------------

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ADUScontext>();
    db.Database.Migrate();
}

// --------------------------------------
// Pipeline de middlewares
// --------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

var locOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()?.Value;
if (locOptions != null)
{
    app.UseRequestLocalization(locOptions);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ImportProgressHub>("/hub/importProgress");

app.Run();
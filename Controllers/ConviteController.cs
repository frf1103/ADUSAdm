using ADUSClient.Convite;
using ADUSClient.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super,Afiliado,Coprodutor")]
    public class ConviteController : Controller
    {
        private readonly ConviteControllerClient _clienteAPI;
        private readonly ParceiroControllerClient _parceiroAPI;
        private readonly SessionManager _sessionManager;
        private readonly Shared.IEmailSender _emailSender;

        public ConviteController(ConviteControllerClient clienteAPI, ParceiroControllerClient parceiroAPI, SessionManager sessionManager, Shared.IEmailSender emailSender)
        {
            _clienteAPI = clienteAPI;
            _parceiroAPI = parceiroAPI;
            _sessionManager = sessionManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? idAfiliado, string? idCoprodutor, int status = 2, int expirados = 2, string? titular = null)
        {
            if (_sessionManager.userrole == "Afiliado" || _sessionManager.userrole == "Coprodutor")
            {
                if (_sessionManager.userrole == "Afiliado")
                {
                    if (idAfiliado != _sessionManager.idafiliado)
                    {
                        return View();
                    }
                }

                if (_sessionManager.userrole == "Coprodutor")
                {
                    if (idCoprodutor != _sessionManager.idcoprodutor)
                    {
                        return View();
                    }
                }
            }

            ViewBag.Titulo = "Convites";
            ViewBag.idAfiliado = idAfiliado;
            ViewBag.idCoprodutor = idCoprodutor;
            ViewBag.status = status.ToString();
            ViewBag.expirados = expirados.ToString();
            ViewBag.urlconvite = _sessionManager.urlconvite;
            ViewBag.titular = titular;

            var afiliados = await _parceiroAPI.Lista("", false, false, true, false);
            var coprodutores = await _parceiroAPI.Lista("", false, false, false, true);

            ViewBag.Afiliados = afiliados.Select(a => new SelectListItem { Text = a.razaoSocial, Value = a.id });
            ViewBag.Coprodutores = coprodutores.Select(c => new SelectListItem { Text = c.razaoSocial, Value = c.id });

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> meusconvites(int status = 3, int expirados = 0, string titular = null)
        {
            if (_sessionManager.userrole == "Afiliado")
            {
                if (_sessionManager.userrole != "Afiliado")
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Titulo = "Meus Convites";
            ViewBag.idAfiliado = _sessionManager.idafiliado;

            ViewBag.status = status.ToString();
            ViewBag.expirados = expirados.ToString();
            ViewBag.urlconvite = _sessionManager.urlconvite;

            var convites = await _clienteAPI.Listar(_sessionManager.idafiliado, "0", status, expirados, titular);

            return View(convites);
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string? idAfiliado, string? idCoprodutor, int status, int expirados)
        {
            var convites = await _clienteAPI.Listar(idAfiliado, idCoprodutor, status, expirados);
            return Json(convites);
        }

        [HttpGet]
        [Authorize(Roles = "Afiliado")]
        public async Task<IActionResult> Adicionar(string? id, string? idAfiliado, string? idCoprodutor, int acao = 0)
        {
            ConviteViewModel convite;
            ViewBag.idacao = acao;

            if (acao == 1)
            {
                convite = new ConviteViewModel { IdConvite = Guid.NewGuid().ToString("N"), DataCriacao = DateTime.Now, DataExpiracao = DateTime.Now.AddDays(3), Status = 0, IdAfiliado = _sessionManager.idafiliado };
                ViewBag.Titulo = "Novo Convite";
                ViewBag.Acao = "Adicionar";
            }
            else
            {
                convite = await _clienteAPI.GetById(id);
                ViewBag.Acao = acao == 2 ? "Editar" : acao == 3 ? "Excluir" : "Ver";
                ViewBag.Titulo = ViewBag.Acao + " Convite";
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    convite = JsonConvert.DeserializeObject<ConviteViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            ViewBag.idAfiliado = idAfiliado;
            ViewBag.idCoprodutor = idCoprodutor;

            return View(convite);
        }

        [HttpPost]
        [Authorize(Roles = "Afiliado")]
        public async Task<IActionResult> Adicionar(ConviteViewModel dados)
        {
            dados.IdAfiliado = _sessionManager.idafiliado;
            var response = await _clienteAPI.Adicionar(dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { idAfiliado = dados.IdAfiliado, idCoprodutor = _sessionManager.idcoprodutor });
            }

            var msg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, msg);
            return View("Adicionar", dados);
        }

        [HttpPost]
        [Authorize(Roles = "Afiliado")]
        public async Task<IActionResult> Editar(string id, ConviteViewModel dados)
        {
            var response = await _clienteAPI.Salvar(id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { idAfiliado = dados.IdAfiliado });
            }

            var msg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, msg);
            TempData["Erro"] = msg;
            TempData["dados"] = JsonConvert.SerializeObject(dados);
            return RedirectToAction("Adicionar", new { acao = 2, id = dados.IdConvite });
        }

        [HttpPost]
        [Authorize(Roles = "Afiliado")]
        public async Task<IActionResult> Excluir(string id, ConviteViewModel dados)
        {
            var response = await _clienteAPI.Excluir(id);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { idAfiliado = dados.IdAfiliado });
            }

            var msg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, msg);
            return View("Adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> MailConvite([FromBody] MailConviteDTO model)
        {
            try
            {
                await _emailSender.SendEmailAsync(model.email, "Convite para Teca Social", model.url);
                return Json(new { success = true, message = "Email enviado com sucesso." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email: {ex.Message}");
                return Json(new { success = false, message = "Falha ao enviar o email." });
            }
            /*
            if (!string.IsNullOrEmpty(@ref))
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(7), // persiste por 7 dias
                    HttpOnly = true,                      // não acessível via JS
                    IsEssential = true,                   // não depende de consentimento
                    Secure = true,                        // importante se estiver usando HTTPS
                    SameSite = SameSiteMode.Lax           // evita bloqueio em redirecionamentos simples
                };

                Response.Cookies.Append("IdConvite", @ref, options);
            }

            if (!string.IsNullOrEmpty(@ref))
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(7), // persiste por 7 dias
                    HttpOnly = true,                      // não acessível via JS
                    IsEssential = true,                   // não depende de consentimento
                    Secure = true,                        // importante se estiver usando HTTPS
                    SameSite = SameSiteMode.Lax           // evita bloqueio em redirecionamentos simples
                };

                Response.Cookies.Append("IdConvite", @ref, options);
            }
            return Json(new { success = true, redirectUrl = "https://localhost:7030" });
            // Redireciona para a página que desejar
            //return Redirect("https://localhost:7030/");
            // Redireciona para a página que desejar
            // return Redirect("https://localhost:7030/");

            return Redirect("https://lp.tecasocial.com.br");
            */
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Irprateca(string @idfil, string @idplataforma)
        {
            /*
            if (!string.IsNullOrEmpty(@ref))
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(7), // persiste por 7 dias
                    HttpOnly = true,                      // não acessível via JS
                    IsEssential = true,                   // não depende de consentimento
                    Secure = true,                        // importante se estiver usando HTTPS
                    SameSite = SameSiteMode.Lax           // evita bloqueio em redirecionamentos simples
                };

                Response.Cookies.Append("IdConvite", @ref, options);
            }
            */

            if (!string.IsNullOrEmpty(@idfil))
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(7), // persiste por 7 dias
                    HttpOnly = true,                      // não acessível via JS
                    IsEssential = true,                   // não depende de consentimento
                    Secure = true,                        // importante se estiver usando HTTPS
                    SameSite = SameSiteMode.Lax           // evita bloqueio em redirecionamentos simples
                };

                Response.Cookies.Append("idafiliado", @idfil, options);
            }

            if (!string.IsNullOrEmpty(idplataforma))
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(7), // persiste por 7 dias
                    HttpOnly = true,                      // não acessível via JS
                    IsEssential = true,                   // não depende de consentimento
                    Secure = true,                        // importante se estiver usando HTTPS
                    SameSite = SameSiteMode.Lax           // evita bloqueio em redirecionamentos simples
                };

                Response.Cookies.Append("idplataforma", @idplataforma, options);
            }
            return Redirect("http://localhost:5000/");
            //return Json(new { success = true, redirectUrl = "https://localhost:7030" });
            // Redireciona para a página que desejar
            //return Redirect("https://localhost:7030/");
            // Redireciona para a página que desejar
            // return Redirect("https://localhost:7030/");

            //return Redirect("https://lp.tecasocial.com.br");
        }

        public class MailConviteDTO
        {
            public string email { get; set; }
            public string idconvite { get; set; }
            public string url { get; set; }
        }
    }
}
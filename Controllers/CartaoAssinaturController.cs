using ADUSClient.Assinatura;
using ADUSClient.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class CartaoAssinaturaController : Controller
    {
        private readonly CartaoAssinaturaControllerClient _clienteAPI;

        public CartaoAssinaturaController(CartaoAssinaturaControllerClient clienteAPI)
        {
            _clienteAPI = clienteAPI;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string idAssinatura)
        {
            ViewBag.Titulo = "Cartões da Assinatura";
            ViewBag.IdAssinatura = idAssinatura;

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int? id, string idAssinatura, int acao = 0)
        {
            CartaoAssinaturaViewModel c;

            ViewBag.IdAcao = acao;
            ViewBag.IdAssinatura = idAssinatura;

            if (acao == 1) // Novo
            {
                c = new CartaoAssinaturaViewModel
                {
                    IdAssinatura = idAssinatura,
                    Ativo = true
                };

                ViewBag.Titulo = "Adicionar Cartão";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                c = await _clienteAPI.GetById(id.Value);

                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar Cartão";
                    ViewBag.Acao = "editar";
                }
                else if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir Cartão";
                    ViewBag.Acao = "excluir";
                }
                else if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar Cartão";
                    ViewBag.Acao = "ver";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<CartaoAssinaturaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(CartaoAssinaturaViewModel dados)
        {
            var response = await _clienteAPI.Adicionar(dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { idAssinatura = dados.IdAssinatura });
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View("Adicionar", dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, CartaoAssinaturaViewModel dados)
        {
            var response = await _clienteAPI.Atualizar(id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { idAssinatura = dados.IdAssinatura });
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            TempData["Erro"] = erro;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            return RedirectToAction("Adicionar", new { acao = 2, id = id, idAssinatura = dados.IdAssinatura });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, CartaoAssinaturaViewModel dados)
        {
            var response = await _clienteAPI.Excluir(id);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { idAssinatura = dados.IdAssinatura });
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View("Adicionar", dados);
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string idAssinatura)
        {
            var cartoes = await _clienteAPI.ListarPorAssinatura(idAssinatura);
            return Json(cartoes);
        }
    }
}
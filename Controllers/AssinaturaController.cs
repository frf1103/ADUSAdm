using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ADUSClient.Enum;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ADUSAdm.Shared;

using Newtonsoft.Json;
using ADUSClient.Assinatura;
using ADUSClient.Localidade;
using ADUSAdm.ViewModel;
using ADUSClient.Parceiro;

using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using RestSharp;
using Azure.Core;
using Humanizer;
using MathNet.Numerics;
using Azure;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Admin,Super,User, UserV")]
    public class AssinaturaController : Controller
    {
        private readonly ADUSClient.Controller.AssinaturaControllerClient _culturaAPI;
        private readonly ADUSClient.Controller.SharedControllerClient _shareAPI;
        private readonly ADUSClient.Controller.ParceiroControllerClient _parceiroAPI;
        private readonly ADUSClient.Controller.ParametroGuruControllerClient _parametroAPI;

        private readonly ADUSClient.Controller.ParcelaControllerClient _parcelaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public AssinaturaController(ADUSClient.Controller.AssinaturaControllerClient culturaAPI, ADUSClient.Controller.ParceiroControllerClient parceiroAPI, SessionManager sessionManager,
            ADUSClient.Controller.SharedControllerClient shareAPI,
            ADUSClient.Controller.ParametroGuruControllerClient parametroAPI,

            ADUSClient.Controller.ParcelaControllerClient parcelaAPI
            )
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
            _shareAPI = shareAPI;
            _parceiroAPI = parceiroAPI;
            _parametroAPI = parametroAPI;

            _parcelaAPI = parcelaAPI;
        }

        public async Task<IActionResult> Index(DateTime ini, DateTime fim, string? filtro, int pagina = 1, string? idparceiro = "0", int status = 3, int forma = 3)
        {
            //Task<List<ADUSClient.Assinatura.ListAssinaturaViewModel>> ret = _culturaAPI.Lista(filtro);
            //List<ADUSClient.Assinatura.ListAssinaturaViewModel> c = await ret;

            Task<List<ListParceiroViewModel>> retc = _parceiroAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.SelectedOptionS = idparceiro.ToString();
            ViewBag.permissao = (_sessionManager.userrole != "UserV");

            if (ini.Year == 1)
            {
                ViewBag.dtinicio = (DateTime.Now.AddDays(-30)).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtinicio = ini.ToString("yyyy-MM-dd");
            }
            if (fim.Year == 1)
            {
                ViewBag.dtfim = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtfim = fim.ToString("yyyy-MM-dd");
            }
            ViewBag.Status = status.ToString();
            ViewBag.Forma = forma.ToString();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Adicionar()
        {
            ADUSClient.Assinatura.AssinaturaViewModel c = new ADUSClient.Assinatura.AssinaturaViewModel();

            ViewBag.Status = new[] {
                new SelectListItem { Text = "Ativa", Value = StatusAssinatura.Ativa.ToString() },
                new SelectListItem { Text = "Pendente", Value = StatusAssinatura.Pendente.ToString() },
                new SelectListItem { Text = "Cancelada", Value = StatusAssinatura.Cancelada.ToString() }
            };

            ViewBag.FormaPagto = new[] {
                new SelectListItem {Text = "Cartão", Value = FormaPagto.Cartao.ToString()},
                new SelectListItem { Text = "Boleto", Value = FormaPagto.Boleto.ToString() },
                new SelectListItem { Text = "Pix", Value = FormaPagto.Pix.ToString() },
            };

            Task<List<ListParceiroViewModel>> retc = _parceiroAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<AssinaturaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }
            c.id = Guid.NewGuid().ToString("N");
            c.datavenda = DateTime.Now;
            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Editar(string id, int acao = 0)
        {
            ViewBag.acao = acao;
            Task<ADUSClient.Assinatura.AssinaturaViewModel> ret = _culturaAPI.ListaById(id);
            ADUSClient.Assinatura.AssinaturaViewModel c = await ret;

            ViewBag.Id = id;
            ViewBag.Status = new[] {
                new SelectListItem { Text = "Ativa", Value = StatusAssinatura.Ativa.ToString() },
                new SelectListItem { Text = "Pendente", Value = StatusAssinatura.Pendente.ToString() },
                new SelectListItem { Text = "Cancelada", Value = StatusAssinatura.Cancelada.ToString() }
            };

            ViewBag.FormaPagto = new[] {
                new SelectListItem {Text = "Cartão", Value = FormaPagto.Cartao.ToString()},
                new SelectListItem { Text = "Boleto", Value = FormaPagto.Boleto.ToString() },
                new SelectListItem { Text = "Pix", Value = FormaPagto.Pix.ToString() },
            };

            Task<List<ListParceiroViewModel>> retc = _parceiroAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.Parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<AssinaturaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Excluir(string id)
        {
            ViewBag.Id = id;
            ViewBag.Status = new[] {
                new SelectListItem { Text = "Ativa", Value = StatusAssinatura.Ativa.ToString() },
                new SelectListItem { Text = "Pendente", Value = StatusAssinatura.Pendente.ToString() },
                new SelectListItem { Text = "Cancelada", Value = StatusAssinatura.Cancelada.ToString() }
            };

            ViewBag.FormaPagto = new[] {
                new SelectListItem {Text = "Cartão", Value = FormaPagto.Cartao.ToString()},
                new SelectListItem { Text = "Boleto", Value = FormaPagto.Boleto.ToString() }
            };

            Task<List<ListParceiroViewModel>> retc = _parceiroAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.Parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<ADUSClient.Assinatura.AssinaturaViewModel> ret = _culturaAPI.ListaById(id);
            ADUSClient.Assinatura.AssinaturaViewModel c = await ret;
            //ViewBag.tipoclasse = c.tipoAssinatura;

            return View(c);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Editar(string id, ADUSClient.Assinatura.AssinaturaViewModel dados)
        {
            var response = await _culturaAPI.Salvar(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            ModelState.AddModelError(string.Empty, x);
            //return View("editar");

            return RedirectToAction("editar", new { id = dados.id, acao = 2 });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Adicionar(ADUSClient.Assinatura.AssinaturaViewModel dados)
        {
            //  dados.id = Guid.NewGuid().ToString("N");
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ADUSClient.Assinatura.AssinaturaViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }

            var x = await response.Content.ReadAsStringAsync();

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            ModelState.AddModelError(string.Empty, x);
            return RedirectToAction("adicionar");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Excluir(string id, ADUSClient.Assinatura.AssinaturaViewModel dados)
        {
            //ADUSClient.Assinatura.AssinaturaViewModel dados=new ADUSClient.Assinatura.AssinaturaViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<JsonResult> GetData(DateTime ini, DateTime fim, string idparceiro, string status, int forma, string? filtro)
        {
            Task<List<ADUSClient.Assinatura.ListAssinaturaViewModel>> ret = _culturaAPI.Lista(ini, fim, idparceiro, status, forma, filtro);
            List<ADUSClient.Assinatura.ListAssinaturaViewModel> c = await ret;
            var cx = c.OrderBy(x => x.datavenda);

            return Json(cx);
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetDataAssinaGuru()
        {
            var client = new HttpClient();
            bool hasMore = true;
            string? nextCursor = null;
            Task<ADUSClient.ParametrosGuruViewModel> ret = _parametroAPI.ListaById(1);
            ADUSClient.ParametrosGuruViewModel c = await ret;

            DateTime dini = c.ultdata, dfim = c.ultdata.AddDays(1);
            if (c.ultdata.ToString("yyyy-MM-dd") == "2024-10-01")
            {
                dfim = dini.AddDays(180);
            }
            //string addressSub = c.urlsub + "?started_at_ini=" + dini.ToString("yyyy-MM-dd") + "&started_at_end=" + dfim.ToString("yyyy-MM-dd");

            string responsebody;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            while (hasMore)
            {
                var url = string.IsNullOrEmpty(nextCursor) ? c.urlsub : $"{c.urlsub}?cursor={Uri.EscapeDataString(nextCursor)}";

                url = url + (string.IsNullOrEmpty(nextCursor) ? "?" : "&") + "started_at_ini = " + dini.ToString("yyyy - MM - dd") + " & started_at_end = " + dfim.ToString("yyyy - MM - dd");
                // Autenticação, se necessário
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.token);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SubscriptionRoot>(content, options);

                foreach (var sub in result.Data)
                {
                    // Converter UnixTime
                    DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(sub.Created_At).UtcDateTime;
                    string doc = sub.Contact.Doc;
                    int tipopessoa = 0;
                    if (doc.Trim().Length > 11)
                    {
                        tipopessoa = 1;
                    }

                    Task<ADUSClient.Parceiro.ParceiroViewModel> retp = _parceiroAPI.ListaById(sub.Contact.Id);
                    ADUSClient.Parceiro.ParceiroViewModel t = await retp;

                    if (t == null)
                    {
                        _parceiroAPI.Adicionar(new ParceiroViewModel
                        {
                            registro = doc,
                            tipodePessoa = (TipodePessoa)tipopessoa,
                            dtNascimento = createdAt,
                            estadoCivil = 0,
                            fantasia = sub.Contact.Name,
                            razaoSocial = sub.Contact.Name,
                            cep = "74000000",
                            logradouro = "AV ",
                            bairro = "BAIRRO",
                            fone1 = sub.Contact.Phone_Local_Code.Trim() + sub.Contact.Phone_Number,
                            numero = "0",
                            email = sub.Contact.Email,
                            id = sub.Contact.Id,
                            iduf = 26,
                            idcidade = 1928,
                            profissao = "A DEFINIR"
                        });
                    };

                    // Verificar se já existe
                    Task<ADUSClient.Assinatura.AssinaturaViewModel> reta = _culturaAPI.ListaById(sub.Id);
                    ADUSClient.Assinatura.AssinaturaViewModel a = await reta;

                    if (a == null)
                    {
                        _culturaAPI.Adicionar(new AssinaturaViewModel
                        {
                            id = sub.Id,
                            datavenda = createdAt,
                            idformapagto = ((FormaPagto)(sub.Payment_Method == "credit_card" ? 0 : 1)),
                            idparceiro = sub.Contact.Id,
                            idplataforma = sub.Id,
                            status = (StatusAssinatura)1,
                            preco = 47,
                            qtd = 1,
                            valor = 47,
                            observacao = sub.Product.Marketplace_Name
                        });
                    }
                }

                hasMore = (result.has_more_pages == 1);
                nextCursor = result.next_cursor;
            }
            _parametroAPI.Salvar("1", new ADUSClient.ParametrosGuruViewModel
            {
                ultdata = dfim.AddDays(1),
                token = c.token,
                urlsub = c.urlsub,
                urltransac = c.urltransac
            });

            /*

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + c.token);

            DateTime dini = c.ultdata, dfim = c.ultdata.AddDays(1);
            if (c.ultdata.ToString("yyyy-MM-dd") == "2024-10-01")
            {
                dfim = dini.AddDays(180);
            }
            string addressSub = c.urlsub + "?started_at_ini=" + dini.ToString("yyyy-MM-dd") + "&started_at_end=" + dfim.ToString("yyyy-MM-dd");

            try
            {
                HttpResponseMessage? response = await client.GetAsync(addressSub);
                response.EnsureSuccessStatusCode();
                responsebody = await response.Content.ReadAsStringAsync();
                //responsebody = responsebody.Replace("\\", "");
            }
            catch (Exception e)
            {
                responsebody = "erro:" + e.Message;
            }

            var root = JsonSerializer.Deserialize<SubscriptionRoot>(responsebody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });*/

            return Json("OK");
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetDataGuru()
        {
            var client = new HttpClient();
            bool hasMore = true;
            string? nextCursor = null;
            Task<ADUSClient.ParametrosGuruViewModel> ret = _parametroAPI.ListaById(1);
            ADUSClient.ParametrosGuruViewModel c = await ret;

            DateTime dini = c.ultdata, dfim = c.ultdata.AddDays(30);
            if (c.ultdata.ToString("yyyy-MM-dd") == "2024-10-01")
            {
                dfim = dini.AddDays(90);
            }
            //string addressSub = c.urlsub + "?started_at_ini=" + dini.ToString("yyyy-MM-dd") + "&started_at_end=" + dfim.ToString("yyyy-MM-dd");

            string responsebody;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            while (hasMore)
            {
                var url = string.IsNullOrEmpty(nextCursor) ? c.urltransac : $"{c.urltransac}?cursor={Uri.EscapeDataString(nextCursor)}";

                url = url + (string.IsNullOrEmpty(nextCursor) ? "?" : "&") + "ordered_at_ini=" + dini.ToString("yyyy-MM-dd") + " & ordered_at_end=" + dfim.ToString("yyyy-MM-dd");
                //+ "&transaction_status=" + status;
                // Autenticação, se necessário
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.token);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var jsonDoc = JsonDocument.Parse(content);
                var transactions = new List<RootObject>();
                string logPath = "erros.log";

                foreach (var element in jsonDoc.RootElement.GetProperty("data").EnumerateArray())
                {
                    try
                    {
                        var itemJson = element.GetRawText();
                        var transacao = JsonSerializer.Deserialize<RootObject>(itemJson);
                        if (transacao != null)
                            transactions.Add(transacao);
                    }
                    catch (Exception ex)
                    {
                        var erro = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Erro ao desserializar item: {ex.Message}{Environment.NewLine}";
                        continue;
                        //File.AppendAllText(logPath, erro);
                    }
                }

                // var result = JsonSerializer.Deserialize<Transactions>(content, options);

                foreach (var sub in transactions)
                {
                    // Converter UnixTime
                    DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(sub.dates.created_at).UtcDateTime;
                    string doc = sub.contact.doc;
                    int tipopessoa = 0;
                    if (doc.Trim().Length > 11)
                    {
                        tipopessoa = 1;
                    }

                    Task<ADUSClient.Parceiro.ParceiroViewModel> retp = _parceiroAPI.ListaById(sub.contact.id);
                    ADUSClient.Parceiro.ParceiroViewModel t = await retp;

                    if (t == null)
                    {
                        Task<ADUSClient.Parceiro.ViaCep> retc = _parceiroAPI.BuscarEnderecoAsync(sub.contact.address_zip_code);
                        ADUSClient.Parceiro.ViaCep viacep = await retc;

                        Task<ADUSClient.Localidade.UFViewModel> retuf = _shareAPI.ListaUFIBGE(viacep.Ibge.Substring(0, 2));
                        ADUSClient.Localidade.UFViewModel uf = await retuf;

                        Task<ADUSClient.Localidade.MunicipioViewModel> retcid = _shareAPI.ListaCidadeIBGE(viacep.Ibge);
                        ADUSClient.Localidade.MunicipioViewModel cid = await retcid;

                        _parceiroAPI.Adicionar(new ParceiroViewModel
                        {
                            registro = doc,
                            tipodePessoa = (TipodePessoa)tipopessoa,
                            dtNascimento = createdAt,
                            estadoCivil = 0,
                            fantasia = sub.contact.name,
                            razaoSocial = sub.contact.name,
                            cep = sub.contact.address_zip_code,
                            logradouro = sub.contact.address,
                            bairro = sub.contact.address_district,
                            fone1 = sub.contact.phone_local_code.Trim() + sub.contact.phone_number,
                            numero = sub.contact.address_number,
                            complemento = sub.contact.address_comp,
                            email = sub.contact.email,
                            id = sub.contact.id,
                            iduf = uf.id,
                            idcidade = cid.id,
                            profissao = "A DEFINIR"
                        });
                    };

                    // Verificar se já existe
                    Task<ADUSClient.Assinatura.AssinaturaViewModel> reta = _culturaAPI.ListaById(sub.subscription.id);
                    ADUSClient.Assinatura.AssinaturaViewModel a = await reta;

                    FormaPagto forma = FormaPagto.Pix;
                    if (sub.payment.method.ToLower() == "credit_card")
                    {
                        forma = FormaPagto.Cartao;
                    }
                    if (sub.payment.method.ToLower() == "billet")
                    {
                        forma = FormaPagto.Boleto;
                    }

                    if (a == null)
                    {
                        _culturaAPI.Adicionar(new AssinaturaViewModel
                        {
                            id = sub.subscription.internal_id,
                            datavenda = createdAt,
                            idformapagto = forma,
                            idparceiro = sub.contact.id,
                            idplataforma = sub.subscription.id,
                            status = (StatusAssinatura)1,
                            preco = sub.product.unit_value,
                            qtd = sub.product.qty,
                            valor = sub.product.total_value,
                            observacao = sub.product.marketplace_name
                        });
                    }

                    //incluir parcela

                    DateTime dtcr = DateTime.Now;
                    if (sub.dates.OrderedAt != null)
                    {
                        dtcr = DateTimeOffset.FromUnixTimeSeconds(sub.dates.created_at).UtcDateTime;
                    }

                    Task<ADUSClient.Parcela.ParcelaViewModel> retparc = _parcelaAPI.ListaByIdCheckout(sub.id);
                    ADUSClient.Parcela.ParcelaViewModel parc = await retparc;
                    if (parc == null)
                    {
                        string nossonumero = null;
                        if (sub.payment.marketplace_name.ToLower() == "pagarme" || sub.payment.marketplace_name.ToLower() == "pagarme2")
                        {
                            if (forma == FormaPagto.Cartao || forma == FormaPagto.Boleto)
                            {
                                nossonumero = sub.payment.acquirer.nsu;
                            }
                            else
                            {
                                nossonumero = sub.payment.pix.qrcode.signature;
                            }
                        }
                        _parcelaAPI.Adicionar(new ADUSClient.Parcela.ParcelaViewModel
                        {
                            id = Guid.NewGuid().ToString("N"),
                            datavencimento = dtcr,
                            idformapagto = forma,
                            numparcela = sub.invoice.cycle,
                            idassinatura = sub.subscription.internal_id,
                            plataforma = sub.payment.marketplace_name,
                            valor = (decimal)sub.product.total_value,
                            idcheckout = sub.id,
                            nossonumero = nossonumero,
                            descontos = 0,
                            acrescimos = 0,
                            valorliquido = (decimal)sub.product.total_value,
                            descontoantecipacao = 0,
                            descontoplataforma = 0,
                            comissao = 0,
                            databaixa = (sub.payment.acquirer.code == "0000") ? dtcr : null
                        });
                    }
                    else
                    {
                        parc.databaixa = (sub.payment.acquirer.code == "0000") ? dtcr : null;
                        _parcelaAPI.Salvar(parc.id, parc);
                    }
                }

                if (jsonDoc.RootElement.TryGetProperty("has_more_pages", out var hasMorePagesProperty))
                {
                    hasMore = (hasMorePagesProperty.GetInt32() == 1);
                }
                if (hasMore)
                {
                    if (jsonDoc.RootElement.TryGetProperty("next_cursor", out var nextC))
                    {
                        nextCursor = nextC.GetString();
                    }
                }
            }
            _parametroAPI.Salvar("1", new ADUSClient.ParametrosGuruViewModel
            {
                ultdata = dfim.AddDays(1),
                token = c.token,
                urlsub = c.urlsub,
                urltransac = c.urltransac
            });

            return Json("OK");
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetNSU()
        {
            /*
            var client = new HttpClient();
            var chave = "sk_871c3d7c606a4be3bd48d4f86b68c58f";
            var authBytes = Encoding.UTF8.GetBytes($"{chave}:");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await client.GetAsync("https://api.pagar.me/core/v5/transactions/3923823083");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json); */
            var secretKey = "sk_871c3d7c606a4be3bd48d4f86b68c58f";

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;

            while (hasMore)
            {
                string url = "https://api.pagar.me/core/v5/charges";

                if (cursor == null)
                    url += "?created_since=2024-12-07&created_until=2024-12-09&limit=100";
                else
                    url = cursor;

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("Authorization", $"Basic {base64Auth}");
                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorMessage);
                    break;
                }

                // Adicione sua lógica de deserialização aqui
                allPayables.Add(response.Content); // ou deserialize

                var json = response.Content; // string com JSON recebido do Pagar.me

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var id = item.GetProperty("id").GetString();
                        var acquirerNSU = item.GetProperty("gateway_id").GetString();
                    }
                }
                // Verifica o header 'X-PagarMe-Cursor'
                if (root.TryGetProperty("paging", out var page))
                {
                    if (page.TryGetProperty("next", out var x))
                    {
                        cursor = x.GetString();
                    }
                    else hasMore = false;
                }
                else
                {
                    hasMore = false; // última página
                }
                /*
                var options = new RestClientOptions(url);
                var client = new RestClient(options);
                var request = new RestRequest("");

                var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
                request.AddHeader("accept", "application/json");

                request.AddHeader("Authorization", $"Basic {base64Auth}");
                var response = await client.GetAsync(request);

                //            Console.WriteLine("{0}", response.Content);
                */
            }
            return Json("OK");
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetBaixasByData(string ini, string fim)
        {
            /*
            var client = new HttpClient();
            var chave = "sk_871c3d7c606a4be3bd48d4f86b68c58f";
            var authBytes = Encoding.UTF8.GetBytes($"{chave}:");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await client.GetAsync("https://api.pagar.me/core/v5/transactions/3923823083");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json); */
            var secretKey = "sk_871c3d7c606a4be3bd48d4f86b68c58f";

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;
            int pagina = 1;

            while (hasMore)
            {
                string url = "https://api.pagar.me/core/v5/balance/operations";

                if (cursor == null)
                    url += "?created_since=" + ini + "&created_until=" + fim + "&size=100&page=" + pagina.ToString();
                else
                    url = cursor;

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("Authorization", $"Basic {base64Auth}");
                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorMessage);
                    break;
                }

                // Adicione sua lógica de deserialização aqui
                allPayables.Add(response.Content); // ou deserialize

                var json = response.Content; // string com JSON recebido do Pagar.me

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                //var data = JsonConvert.DeserializeObject<List<Operation>>(json);

                if (root.GetProperty("data").GetArrayLength() == 0)
                    hasMore = false;
                else
                {
                    // Processar dados
                    pagina++;
                }
                /*
                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                   //     var id = item.GetProperty("id").GetString();
                   //     var acquirerNSU = item.GetProperty("gateway_id").GetString();
                    }
                }
                // Verifica o header 'X-PagarMe-Cursor'
                if (root.TryGetProperty("paging", out var page))
                {
                    if (page.TryGetProperty("next", out var x))
                    {
                        cursor = x.GetString();
                    }
                    else hasMore = false;
                }
                else
                {
                    hasMore = false; // última página
                }

                var options = new RestClientOptions(url);
                var client = new RestClient(options);
                var request = new RestRequest("");

                var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
                request.AddHeader("accept", "application/json");

                request.AddHeader("Authorization", $"Basic {base64Auth}");
                var response = await client.GetAsync(request);

                //            Console.WriteLine("{0}", response.Content);
                */
            }
            return Json("OK");
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetBaixasById(string nsu)
        {
            /*
            var client = new HttpClient();
            var chave = "sk_871c3d7c606a4be3bd48d4f86b68c58f";
            var authBytes = Encoding.UTF8.GetBytes($"{chave}:");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await client.GetAsync("https://api.pagar.me/core/v5/transactions/3923823083");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json); */
            var secretKey = "sk_871c3d7c606a4be3bd48d4f86b68c58f";

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;

            while (hasMore)
            {
                string url = "https://api.pagar.me/core/v5/payables";

                if (cursor == null)
                    url += "?gateway_id=" + nsu;
                else
                    url = cursor;

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("Authorization", $"Basic {base64Auth}");
                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorMessage);
                    break;
                }

                // Adicione sua lógica de deserialização aqui
                allPayables.Add(response.Content); // ou deserialize

                var json = response.Content; // string com JSON recebido do Pagar.me

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var id = item.GetProperty("id").GetString();
                        var acquirerNSU = item.GetProperty("gateway_id").GetString();
                    }
                }
                // Verifica o header 'X-PagarMe-Cursor'
                if (root.TryGetProperty("paging", out var page))
                {
                    if (page.TryGetProperty("next", out var x))
                    {
                        cursor = x.GetString();
                    }
                    else hasMore = false;
                }
                else
                {
                    hasMore = false; // última página
                }
                /*
                var options = new RestClientOptions(url);
                var client = new RestClient(options);
                var request = new RestRequest("");

                var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
                request.AddHeader("accept", "application/json");

                request.AddHeader("Authorization", $"Basic {base64Auth}");
                var response = await client.GetAsync(request);

                //            Console.WriteLine("{0}", response.Content);
                */
            }
            return Json("OK");
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetBaixasAsaasByData(string ini, string fim)
        {
            /*
            var client = new HttpClient();
            var chave = "sk_871c3d7c606a4be3bd48d4f86b68c58f";
            var authBytes = Encoding.UTF8.GetBytes($"{chave}:");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await client.GetAsync("https://api.pagar.me/core/v5/transactions/3923823083");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json); */
            var secretKey = "$aact_prod_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmIyNmM2YWIzLThmOGUtNDY5Mi1hNDNkLWJiNDk4YTRmNGNjOTo6JGFhY2hfZTI5MTFhMGMtYjdkNi00MzhlLWI2OTEtOTYxNzYzMmI2NDBk";

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;

            while (hasMore)
            {
                string url = "https://api.asaas.com/v3/financialTransactions";

                if (cursor == null)
                    url += "?startDate=" + ini + "&finishDate=" + fim;
                //url += "?paymentDate=" + ini;
                else
                    url = cursor;

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("access_token", secretKey); // ou "Authorization", $"Bearer {apiKey}"

                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorMessage);
                    break;
                }

                // Adicione sua lógica de deserialização aqui
                allPayables.Add(response.Content); // ou deserialize

                var json = response.Content; // string com JSON recebido do Pagar.me

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var id = item.GetProperty("id").GetString();
                        var acquirerNSU = item.GetProperty("gateway_id").GetString();
                    }
                }
                // Verifica o header 'X-PagarMe-Cursor'
                if (root.TryGetProperty("paging", out var page))
                {
                    if (page.TryGetProperty("next", out var x))
                    {
                        cursor = x.GetString();
                    }
                    else hasMore = false;
                }
                else
                {
                    hasMore = false; // última página
                }
                /*
                var options = new RestClientOptions(url);
                var client = new RestClient(options);
                var request = new RestRequest("");

                var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
                request.AddHeader("accept", "application/json");

                request.AddHeader("Authorization", $"Basic {base64Auth}");
                var response = await client.GetAsync(request);

                //            Console.WriteLine("{0}", response.Content);
                */
            }
            return Json("OK");
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetBaixasAsaasById(string nsu)
        {
            /*
            var client = new HttpClient();
            var chave = "sk_871c3d7c606a4be3bd48d4f86b68c58f";
            var authBytes = Encoding.UTF8.GetBytes($"{chave}:");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await client.GetAsync("https://api.pagar.me/core/v5/transactions/3923823083");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json); */
            var secretKey = "$aact_prod_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmIyNmM2YWIzLThmOGUtNDY5Mi1hNDNkLWJiNDk4YTRmNGNjOTo6JGFhY2hfZTI5MTFhMGMtYjdkNi00MzhlLWI2OTEtOTYxNzYzMmI2NDBk";

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;

            while (hasMore)
            {
                string url = "https://api.asaas.com/v3/payments/";

                if (cursor == null)
                    url += nsu;
                else
                    url = cursor;

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("access_token", secretKey); // ou "Authorization", $"Bearer {apiKey}"

                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorMessage);
                    break;
                }

                // Adicione sua lógica de deserialização aqui
                allPayables.Add(response.Content); // ou deserialize

                var json = response.Content; // string com JSON recebido do Pagar.me

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var id = item.GetProperty("id").GetString();
                        var acquirerNSU = item.GetProperty("gateway_id").GetString();
                    }
                }
                // Verifica o header 'X-PagarMe-Cursor'
                if (root.TryGetProperty("paging", out var page))
                {
                    if (page.TryGetProperty("next", out var x))
                    {
                        cursor = x.GetString();
                    }
                    else hasMore = false;
                }
                else
                {
                    hasMore = false; // última página
                }
                /*
                var options = new RestClientOptions(url);
                var client = new RestClient(options);
                var request = new RestRequest("");

                var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
                request.AddHeader("accept", "application/json");

                request.AddHeader("Authorization", $"Basic {base64Auth}");
                var response = await client.GetAsync(request);

                //            Console.WriteLine("{0}", response.Content);
                */
            }
            return Json("OK");
        }
    }
}
using ADUSAdm.Data;
using ADUSAdm.Shared;
using ADUSClient;
using ADUSClient.Assinatura;
using ADUSClient.Controller;
using ADUSClient.Enum;
using ADUSClient.Parceiro;
using ADUSClient.Parcela;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ADUSAdm.Services
{
    public class CheckoutService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CheckoutService> _logger;
        private readonly ADUScontext _context;
        private readonly ParametroGuruControllerClient _parametros;
        private readonly CartaoAssinaturaControllerClient _cartoes;
        private readonly ParceiroControllerClient _parceiro;
        private readonly SharedControllerClient _shared;
        private readonly AssinaturaControllerClient _assinatura;
        private readonly ParcelaControllerClient _parcela;
        private readonly ASAASSettings _asaasSettings;
        private readonly LogCheckoutControllerClient _log;

        public CheckoutService(
            IHttpClientFactory httpClientFactory,
            ILogger<CheckoutService> logger,
            ADUScontext context,
            ParametroGuruControllerClient parametros,
            CartaoAssinaturaControllerClient cartoes,
            ParceiroControllerClient parceiro,
            SharedControllerClient shared,
            AssinaturaControllerClient assinatura,
            ParcelaControllerClient parcela,
            IOptions<ASAASSettings> asaasSettings,
            LogCheckoutControllerClient log
        )
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _context = context;
            _parametros = parametros;
            _cartoes = cartoes;
            _parceiro = parceiro;
            _shared = shared;
            _assinatura = assinatura;
            _parcela = parcela;
            _asaasSettings = asaasSettings.Value;
            _log = log;
        }

        //ProcessarCheckout(c, p.registro, p.cctoken, p.billingType, remoteIp, p.idafiliado, " ", " ", p.idassinatura, p.numparcela.ToString());
        public async Task<string?> ProcessarCheckout(CheckoutViewModel model, string remoteIp, string? idAfiliado, string? subsid, string? idparcela)
        {
            try
            {
                string billingType = model.FormaPagamento.ToUpper() switch
                {
                    "PIX" => "PIX",
                    "BOLETO" => "BOLETO",
                    _ => "CREDIT_CARD"
                };

                string idcliente = await BuscarClienteAsaas(model, remoteIp);
                string idcl = await CriarOuRecuperarParceiroADUS(model, idcliente);

                string cctoken = "", bandeira = "", ccdigitos = "";
                if (billingType == "CREDIT_CARD" && subsid == null)
                {
                    TokenCartaoViewModel tc = new TokenCartaoViewModel
                    {
                        Email = model.Email,
                        Cep = model.Cep,
                        Nome = model.Nome,
                        NomeTitular = model.NomeTitular,
                        Complemento = model.Complemento,
                        cpfCnpj = model.cpfCnpj,
                        Cvv = model.Cvv,
                        Numero = model.Numero,
                        NumeroCartao = model.NumeroCartao,
                        Telefone = model.Telefone,
                        Validade = model.Validade
                    };
                    (cctoken, bandeira, ccdigitos) = await TokenizarCartao(tc, idcliente, remoteIp);
                }
                if (billingType == "CREDIT_CARD" && subsid != null)
                {
                    cctoken = model.TokenCartao;
                }
                if (billingType == "CREDIT_CARD" && string.IsNullOrEmpty(cctoken))
                {
                    await RegistrarLog(model.Nome, remoteIp, "Assinatura sem cartão definido", "", "", "", "400", "Formato da validade inválido");
                    return null;
                }

                string invoiceUrl = await AssinaturaOuPagamento(
                    model, idcliente, cctoken, billingType,
                    remoteIp, idAfiliado, ccdigitos, bandeira, subsid, idparcela);

                return invoiceUrl;
            }
            catch (Exception ex)
            {
                await RegistrarLog(model.Nome, remoteIp, "Erro Geral Checkout", "", "", "", "500", ex.ToString());
                _logger.LogError(ex, "Erro inesperado no checkout");
                return null;
            }
        }

        public async Task<(string cctoken, string bandeira, string ccdigitos)> TokenizarCartao(
            TokenCartaoViewModel model, string idcliente, string remoteIp)
        {
            if (idcliente == "X")
            {
                idcliente = await BuscarClienteAsaas(new CheckoutViewModel
                {
                    cpfCnpj = model.cpfCnpj
                }, remoteIp);
            }
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                customer = idcliente,
                creditCard = new
                {
                    holderName = model.NomeTitular,
                    number = model.NumeroCartao,
                    expiryMonth = model.Validade.Split('/')[0],
                    expiryYear = model.Validade.Split('/')[1],
                    ccv = model.Cvv
                },
                creditCardHolderInfo = new
                {
                    name = model.Nome,
                    email = model.Email,
                    cpfCnpj = FBSLIb.StringLib.Somentenumero(model.cpfCnpj),
                    postalCode = model.Cep,
                    addressNumber = model.Numero,
                    addressComplement = model.Complemento,
                    phone = model.Telefone
                },
                remoteIp = remoteIp
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, _asaasSettings.urltokencartao)
            {
                Content = content
            };
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("access_token", _asaasSettings.access_token);
            request.Headers.Add("User-Agent", _asaasSettings.useragent);

            await RegistrarLog(model.Nome, remoteIp, "POST Tokenizar Cartão", _asaasSettings.urltokencartao, json, "", "Iniciando");

            var response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            await RegistrarLog(model.Nome, remoteIp, "POST Tokenizar Cartão", _asaasSettings.urltokencartao, json, responseBody, response.StatusCode.ToString());

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Falha ao tokenizar cartão.");
            }

            var doc = JsonDocument.Parse(responseBody);
            return (
                doc.RootElement.GetProperty("creditCardToken").GetString(),
                doc.RootElement.GetProperty("creditCardBrand").GetString(),
                doc.RootElement.GetProperty("creditCardNumber").GetString()
            );
        }

        private async Task<string> BuscarClienteAsaas(CheckoutViewModel model, string remoteIp)
        {
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_asaasSettings.urlparceiro}?cpfCnpj={FBSLIb.StringLib.Somentenumero(model.cpfCnpj)}");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("access_token", _asaasSettings.access_token);
            request.Headers.Add("User-Agent", _asaasSettings.useragent);

            await RegistrarLog(model.Nome, remoteIp, "GET Cliente Asaas", request.RequestUri.ToString(), "", "", "Iniciando");

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            await RegistrarLog(model.Nome, remoteIp, "GET Cliente Asaas", request.RequestUri.ToString(), "", body, response.StatusCode.ToString());

            var doc = JsonDocument.Parse(body);

            if (doc.RootElement.GetProperty("data").GetArrayLength() == 0)
            {
                var clientePayload = new
                {
                    name = model.Nome,
                    email = model.Email,
                    phone = $"+{model.Ddi}{model.Telefone}",
                    cpfCnpj = model.cpfCnpj
                };

                var json = JsonSerializer.Serialize(clientePayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var postRequest = new HttpRequestMessage(HttpMethod.Post, _asaasSettings.urlparceiro)
                {
                    Content = content
                };
                postRequest.Headers.Add("accept", "application/json");
                postRequest.Headers.Add("access_token", _asaasSettings.access_token);
                postRequest.Headers.Add("User-Agent", _asaasSettings.useragent);

                await RegistrarLog(model.Nome, remoteIp, "POST Criação Cliente Asaas", _asaasSettings.urlparceiro, json, "", "Iniciando");

                response = await client.SendAsync(postRequest);
                body = await response.Content.ReadAsStringAsync();

                await RegistrarLog(model.Nome, remoteIp, "POST Criação Cliente Asaas", _asaasSettings.urlparceiro, json, body, response.StatusCode.ToString());

                var createdDoc = JsonDocument.Parse(body);
                return createdDoc.RootElement.GetProperty("id").GetString();
            }
            else
            {
                return doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
            }
        }

        private async Task<string> CriarOuRecuperarParceiroADUS(CheckoutViewModel model, string idclienteAsaas)
        {
            var cliente = await _parceiro.Lista(FBSLIb.StringLib.Somentenumero(model.cpfCnpj), true, true, true, true);
            if (cliente.Count > 0)
                return cliente[0].id;

            var uf = await _shared.ListaUFBySigla(model.Estado);
            var cidade = await _shared.ListaCidade(uf[0].id, model.Cidade);

            string idcl = Guid.NewGuid().ToString();
            var parv = new ParceiroViewModel
            {
                id = idcl,
                idCidade = cidade[0].id,
                fantasia = model.Nome,
                email = model.Email,
                fone1 = model.Telefone,
                estadoCivil = 0,
                dtNascimento = DateTime.Now.Date,
                bairro = model.Bairro,
                logradouro = model.Logradouro,
                numero = model.Numero,
                razaoSocial = model.Nome,
                cep = model.Cep,
                complemento = model.Complemento,
                iduf = uf[0].id,
                isassinante = true,
                sexo = ADUSClient.Enum.TipoSexo.Indiferente,
                tipodePessoa = model.cpfCnpj.Length > 15
                    ? ADUSClient.Enum.TipodePessoa.Jurídica
                    : ADUSClient.Enum.TipodePessoa.Física,
                profissao = "INDEFINIDO",
                registro = FBSLIb.StringLib.Somentenumero(model.cpfCnpj)
            };

            await _parceiro.Adicionar(parv);
            return idcl;
        }

        private async Task RegistrarLog(string nomeCliente, string ip, string tipoOperacao, string url, string payload, string retorno, string statusHttp, string? erro = null)
        {
            var log = new LogCheckoutViewModel
            {
                NomeCliente = nomeCliente,
                IpOrigem = ip,
                TipoOperacao = tipoOperacao,
                UrlRequisicao = url,
                PayloadEnviado = payload,
                RetornoApi = retorno,
                StatusHttp = statusHttp,
                Erro = erro
            };

            _log.Adicionar(log);
        }

        private async Task<string> AssinaturaOuPagamento(
            CheckoutViewModel model,
            string idcliente,
            string cctoken,
            string billingType,
            string remoteIp,
            string? idAfiliado,
            string ccdigitos,
            string bandeira,
            string? subsid = null, string? idparcela = null)
        {
            var client = new HttpClient();
            string idsub;
            /*
            if (model.FormaPagamento != "Parcelado")
            {
                var subscriptionPayload = new
                {
                    customer = idcliente,
                    billingType = billingType,
                    value = model.ValorTotal,
                    nextDueDate = DateTime.Now.Date,
                    cycle = "MONTHLY",
                    description = $"{model.QuantidadeArvores} ARVORES PROJETO TECA SOCIAL",
                    maxPayments = 84,
                    creditCardToken = cctoken,
                    remoteIp = remoteip,
                    externalReference = idafiliado
                };

                var json = JsonSerializer.Serialize(subscriptionPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, _AsaasSettings.urlsubscription)
                {
                    Content = content
                };
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("access_token", _AsaasSettings.access_token);
                request.Headers.Add("User-Agent", _AsaasSettings.useragent);

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                await RegistrarLog(model.Nome, remoteip, "POST Assinatura", _AsaasSettings.urlsubscription, json, body, response.StatusCode.ToString(), (!response.IsSuccessStatusCode ? "Erro ao assinar" : null));
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Erro ao criar assinatura Asaas: {body}");
                    throw new Exception("Falha ao criar assinatura.");
                }

                using var doc = JsonDocument.Parse(body);
                idsub = doc.RootElement.GetProperty("id").GetString();
            }
            else
            {*/
            if (subsid == null)
            {
                idsub = Guid.NewGuid().ToString();
            }
            else
            {
                idsub = subsid;
                idcliente = await BuscarClienteAsaas(model, remoteIp);
            }

            List<object> split = null;

            var afiliado = await _parceiro.ListaById(idAfiliado);

            if (afiliado != null)
            {
                split = new List<object>
                {
                    new {
                        walletId = afiliado.idwallet,
                        percentualValue = afiliado.percomissao,
                        status = "RECEIVED"
                    },
                    new {
                        walletId = afiliado.idwalletcoprodutor,
                        percentualValue = afiliado.percomissaocoprodutor,
                        status = "RECEIVED"
                    }
                };
            }
            string? invoiceurl = null;
            DateTime vcto = DateTime.Now.Date;
            if (subsid != null || billingType == "BOLETO")
            {
                vcto = model.DataVencimento.Value;
            }

            // ou, se tiver splits:

            var paymentPayload = new
            {
                customer = idcliente,
                billingType = billingType,
                value = model.ValorTotal,
                dueDate = vcto,
                description = $"{model.QuantidadeArvores} ARVORES PROJETO TECA SOCIAL",
                installmentCount = (model.FormaPagamento == "Parcelado") ? model.Parcelas : null,
                totalValue = model.ValorTotal,
                creditCardToken = cctoken,
                externalReference = idsub,
                remoteIp = remoteIp,
                split = (split != null && split.Any()) ? split : null
            };

            var json = JsonSerializer.Serialize(paymentPayload,
             new JsonSerializerOptions
             {
                 DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
             }
            );
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _asaasSettings.urlpayments)
            {
                Content = content
            };
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("access_token", _asaasSettings.access_token);
            request.Headers.Add("User-Agent", _asaasSettings.useragent);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            await RegistrarLog(model.Nome, remoteIp, "POST Cobranca", _asaasSettings.urlsubscription, json, body, response.StatusCode.ToString(), (!response.IsSuccessStatusCode ? "Erro na cobranca" : null));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Erro ao criar pagamento Asaas: {body}");
                throw new Exception("Falha ao criar pagamento.");
            }
            else
            {
                using var doc = JsonDocument.Parse(body);
                string status = doc.RootElement.GetProperty("status").GetString();
                if (status == "CONFIRMED" || status == "PENDING")
                {
                    string idcl = await CriarOuRecuperarParceiroADUS(model, idcliente);
                    if (subsid == null)
                    {
                        await AddAssinaturaADUS(model, idsub, idcl, idAfiliado);
                    }
                    if (billingType == "CREDIT_CARD" && subsid == null)
                    {
                        await _cartoes.Adicionar(new CartaoAssinaturaViewModel
                        {
                            Ativo = true,
                            Bandeira = bandeira,
                            IdAssinatura = idsub,
                            IdToken = cctoken,
                            UltimosDigitos = ccdigitos
                        });
                    }

                    string paymentid = doc.RootElement.GetProperty("id").GetString();
                    invoiceurl = doc.RootElement.GetProperty("invoiceUrl").GetString();
                    var netvalue = doc.RootElement.TryGetProperty("netValue", out var nv) && nv.ValueKind == JsonValueKind.Number ? nv.GetDecimal() : (decimal?)model.ValorTotal;
                    if (model.FormaPagamento != "Parcelado")
                    {
                        if (subsid == null)
                        {
                            DateTime dvcto = (model.FormaPagamento != "Boleto") ? DateTime.Now.Date : model.DataVencimento.Value;
                            int ano = dvcto.Year;
                            int mes = dvcto.Month;
                            int dia = dvcto.Day;
                            for (var i = 1; i <= 84; i++)
                            {
                                var parcela = new ParcelaViewModel
                                {
                                    id = Guid.NewGuid().ToString(),
                                    idassinatura = idsub,
                                    idcheckout = (i == 1) ? invoiceurl : null,
                                    nossonumero = (i == 1) ? paymentid : null,
                                    idparceiro = idcl,
                                    datavencimento = dvcto,
                                    valor = model.ValorTotal,
                                    valorliquido = (decimal)netvalue - (decimal)0.10 * model.ValorTotal,
                                    plataforma = "Asaas",
                                    idformapagto = billingType switch
                                    {
                                        "PIX" => FormaPagto.Pix,
                                        "BOLETO" => FormaPagto.Boleto,
                                        _ => FormaPagto.Cartao
                                    },
                                    dataestimadapagto = null,
                                    numparcela = i,
                                    comissao = (decimal)0.10 * model.ValorTotal,
                                    acrescimos = 0,
                                    descontoantecipacao = 0,
                                    descontoplataforma = model.ValorTotal - (decimal)netvalue,
                                    descontos = 0,
                                    databaixa = (billingType == "CREDIT_CARD" && status == "CONFIRMED" && i == 1) ? DateTime.Now.Date : null
                                };
                                await _parcela.Adicionar(parcela);

                                mes++;
                                if (mes > 12)
                                {
                                    mes = 1;
                                    ano++;
                                }
                                dvcto = FBSLIb.DateLib.ObterDataComDiaFixo(ano, mes, dia);
                            }
                        }
                        else
                        {
                            DateTime? estimatedCreditDate = (billingType == "CREDIT_CARD") ? doc.RootElement.TryGetProperty("estimatedCreditDate", out var ec) && ec.ValueKind == JsonValueKind.String
                                ? ec.GetDateTime()
                                : null : null;
                            var p = await _parcela.ListaById(idparcela);
                            if (p != null)
                            {
                                p.valorliquido = (decimal)netvalue - (decimal)0.10 * model.ValorTotal;
                                p.nossonumero = paymentid;
                                p.descontoplataforma = model.ValorTotal - (decimal)netvalue;
                                p.dataestimadapagto = estimatedCreditDate;
                                p.idcheckout = invoiceurl;
                                _parcela.Salvar(p.id, p);
                            }
                        }
                    }
                }
            }

            return invoiceurl;
        }

        public async Task<string> AddAssinaturaADUS(CheckoutViewModel m, string id, string idcliente, string idafiliado)
        {
            FormaPagto idforma = (m.FormaPagamento == "PIX") ? FormaPagto.Pix : (m.FormaPagamento == "BOLETO") ? FormaPagto.Boleto : FormaPagto.Cartao;
            await _assinatura.Adicionar(new AssinaturaViewModel
            {
                id = id,
                datavenda = DateTime.Now.Date,
                idformapagto = idforma,
                idparceiro = idcliente,
                idplataforma = id,
                status = (StatusAssinatura)1,
                preco = 47,
                qtd = m.QuantidadeArvores,
                valor = m.QuantidadeArvores * 47,
                observacao = " ",
                plataforma = "ASAAS",
                idafiliado = idafiliado
            });

            return null;
        }
    }
}
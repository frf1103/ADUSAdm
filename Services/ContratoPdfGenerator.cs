using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using ADUSClient.Assinatura;

public static class ContratoPdfGenerator
{
    private static IContainer CellStyle(IContainer container)
    {
        return container.Padding(2).Border(1).BorderColor(Colors.Grey.Darken2);
    }

    public static byte[] Gerar(AssinaturaContratoViewModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(12));

                page.Header().Text("CONTRATO DE COMPRA E VENDA DE ÁRVORE EM PÉ PARA ENTREGA FUTURA")
                                 .FontSize(16).SemiBold().AlignCenter();

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    // PREÂMBULO
                    col.Item().Text("PREÂMBULO").Bold().FontSize(14).AlignCenter();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(180);
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("COMPRADOR:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.comprador);

                        table.Cell().Element(CellStyle).Text((model.registrocomprador.Length <= 11) ? "CPF:" : "CNPJ:").Bold();
                        table.Cell().Element(CellStyle).Text(model.registrocomprador);

                        table.Cell().Element(CellStyle).Text("EMAIL:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.emailcomprador);

                        table.Cell().Element(CellStyle).Text("ENDEREÇO:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.enderecocomprador);

                        table.Cell().Element(CellStyle).Text("MUNICIPIO:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.municipiocomprador);

                        table.Cell().Element(CellStyle).Text("UF:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.ufcomprador);

                        table.Cell().Element(CellStyle).Text("CEP:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.cepcomprador);

                        table.Cell().Element(CellStyle).Text("TELEFONE:").Bold().ToString().ToUpper();
                        table.Cell().Element(CellStyle).Text(model.fonecomprador);
                        if (model.registrocomprador.Length <= 11)
                        {
                            table.Cell().Element(CellStyle).Text("ESTADO CIVIL:").Bold().ToString().ToUpper();
                            table.Cell().Element(CellStyle).Text(model.estadocivil);
                        }
                        else
                        {
                            table.Cell().Element(CellStyle).Text("REPRESENTANTE LEGAL:").Bold().ToString().ToUpper();
                            table.Cell().Element(CellStyle).Text(model.nomerepresentante);
                            table.Cell().Element(CellStyle).Text("CPF REPRESENTANTE LEGAL:").Bold().ToString().ToUpper();
                            table.Cell().Element(CellStyle).Text(model.cpfrepresentante);
                        }
                    });

                    col.Item().PaddingVertical(5).LineHorizontal(1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(180);
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("VENDEDOR:").Bold();
                        table.Cell().Element(CellStyle).Text("ASSOCIAÇÃO DO DESENVOLVIMENTO URBANO SUSTENTÁVEL - ADUS");

                        table.Cell().Element(CellStyle).Text("CNPJ:").Bold();
                        table.Cell().Element(CellStyle).Text("36.987.628/0001-70");

                        table.Cell().Element(CellStyle).Text("ENDEREÇO:").Bold();
                        table.Cell().Element(CellStyle).Text("RUA VENEZA, Nº 36, RESIDENCIAL FLORENÇA, SINOP - MT");

                        table.Cell().Element(CellStyle).Text("EMAIL:").Bold();
                        table.Cell().Element(CellStyle).Text("adus@gfn.agr.br");

                        table.Cell().Element(CellStyle).Text("TELFONE:").Bold();
                        table.Cell().Element(CellStyle).Text("(62) 99418-1626");

                        table.Cell().Element(CellStyle).Text("REPRESENTANTE LEGAL:").Bold();
                        table.Cell().Element(CellStyle).Text("VILSON JOSÉ VIAN");
                    });

                    col.Item().PaddingVertical(5).LineHorizontal(1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(180);
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("QUANTIDADE DE ÁRVORES:").Bold();
                        table.Cell().Element(CellStyle).Text(model.qtd);

                        table.Cell().Element(CellStyle).Text("VALOR TOTAL:").Bold();
                        table.Cell().Element(CellStyle).Text("R$ " + model.valor.ToString("N2", new CultureInfo("pt-BR")));

                        table.Cell().Element(CellStyle).Text("FORMA DE PAGAMENTO:").Bold();
                        table.Cell().Element(CellStyle).Text(model.formapagto);
                    });

                    // CLÁUSULAS FIXAS
                    col.Item().PaddingTop(15).Text("CLÁUSULAS DO CONTRATO").Bold().FontSize(14).AlignCenter();

                    var clausulas = ClausulasContratoTexto();
                    foreach (var paragrafo in clausulas)
                    {
                        if (paragrafo.StartsWith("CLÁUSULA"))
                        {
                            col.Item().Text(paragrafo)
                                .Bold()
                                .FontSize(13);
                        }
                        else
                        {
                            col.Item().Text(txt =>
                            {
                                txt.Span(paragrafo).FontSize(12);
                            });
                        }
                    }

                    // Rodapé
                    col.Item().PaddingTop(20).Text($"Sinop - MT, " + model.datavenda + ".");
                    col.Item().PaddingTop(20).Text("__________________________________________\nADUS - VENDEDORA");
                    col.Item().PaddingTop(20).Text($"__________________________________________\n{model.comprador} - COMPRADOR");

                    // Testemunhas
                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Nome da Testemunha").Bold();
                            header.Cell().Text("CPF").Bold();
                        });

                        for (int i = 0; i < 2; i++)
                        {
                            table.Cell().Text("___________________________________");
                            table.Cell().Text("______________________________");
                        }
                    });
                });

                page.Footer().AlignCenter().Text("ADUS - www.tecasocial.com.br");
            });
        }).GeneratePdf();
    }

    private static List<string> ClausulasContratoTexto()
    {
        return new List<string>
        {
            "CLÁUSULA PRIMEIRA – DO OBJETO",
            "O presente contrato tem como objeto a comercialização de árvores da espécie Tectona Grandis – TECA, com potencial de produção de 1m³ por árvore, cubados pela fórmula Hoppus; altura de 12m por árvore. O plantio possui como finalidade arrecadar recursos para manutenção de alunos bolsistas das Escolas selecionadas pela ADUS.",
            "As árvores encontram-se em processo de aquisição e/ou preparo de solo e estarão disponibilizadas ao COMPRADOR quando atingirem as especificações técnicas e o período de maturidade de 18 anos, contados a partir da assinatura do presente contrato.",
            "O acompanhamento poderá ser feito via site (http://tecasocial.com.br) ou aplicativo da ADUS. A aquisição caracteriza compra e venda para entrega futura, observando o crescimento e amadurecimento do espécime.",

            "CLÁUSULA SEGUNDA – DO PREÇO E FORMA DE PAGAMENTO",
            "2.1. O preço total ajustado está descrito nos itens 3 e 4 do preâmbulo. As formas de pagamento incluem boleto bancário, PIX com QR Code ou cartão de crédito, podendo ser à vista ou parcelado.",
            "2.2. O não pagamento sujeita o COMPRADOR a multa moratória de 2% e juros de 1% ao mês sobre valor corrigido pelo IGP-M.",
            "2.3. A inadimplência por até 60 dias poderá acarretar na rescisão do contrato.",
            "2.4. Compras eletrônicas são formalizadas mediante cadastro na plataforma e aceitação dos termos contratuais.",

            "CLÁUSULA TERCEIRA – DA VIGÊNCIA",
            "3.1. O contrato terá vigência de até 18 anos, período de maturidade para corte das árvores.",

            "CLÁUSULA QUARTA – DO PLANTIO, MANUTENÇÃO, CORTE, ARMAZENAMENTO E RETIRADA DAS ÁRVORES",
            "4.1. Todos os encargos de plantio, manutenção e conservação serão da ADUS até a entrega ao COMPRADOR.",
            "4.2. O manejo será adequado, observando normas ambientais e uso racional de defensivos.",
            "4.3. A ADUS notificará o COMPRADOR sobre o início da colheita. O COMPRADOR terá 90 dias para retirada. Após esse prazo, incidem custos de armazenagem.",
            "4.4. Após 180 dias sem retirada, a ADUS poderá vender a árvore a terceiros, depositando o valor (descontados os custos) ao COMPRADOR.",
            "4.5. Despesas com transporte, taxas, seguros e encargos da retirada são responsabilidade do COMPRADOR.",
            "4.6. A ADUS poderá ser contratada para esses serviços com valores disponíveis no site.",
            "4.7. A ADUS será responsável pelos encargos trabalhistas, cíveis e ambientais de seus colaboradores.",
            "4.8. A limpeza da área do projeto será realizada após retirada das árvores.",
            "4.9. A ADUS manterá uma reserva de 17% da floresta como garantia da entrega ou indenização ao COMPRADOR.",

            "CLÁUSULA QUINTA – DA AUSÊNCIA, INCAPACIDADE E FALECIMENTO DO COMPRADOR",
            "5.1. Em caso de incapacidade ou ausência judicial, o curador ou representante assumirá os direitos e obrigações.",
            "5.2. No caso de sucessão ou falecimento, o inventariante ou herdeiros assumirão as obrigações contratuais.",

            "CLÁUSULA SEXTA – DA PESSOA JURÍDICA COMPRADORA",
            "6.1. Encerramento, recuperação judicial ou falência implicará na responsabilidade dos sócios ou administrador judicial.",
            "6.2. Em caso de fusão, cisão ou aquisição, a sucessora responderá pelas obrigações contratuais.",

            "CLÁUSULA SÉTIMA – DO DIREITO DE DESISTÊNCIA",
            "7.1. O COMPRADOR tem 7 dias corridos após assinatura (em compras virtuais) para desistência. A ADUS fará devolução conforme direito ao arrependimento do CDC.",
            "7.2. Compras presenciais seguem as regras de rescisão da Cláusula Décima Primeira.",

            "CLÁUSULA OITAVA – DA TRANSPARÊNCIA",
            "8.1. A ADUS manterá página atualizada sobre o projeto. Visitação presencial será permitida mediante agendamento.",

            "CLÁUSULA NONA – DA PROTEÇÃO DE DADOS",
            "9.1. A ADUS tratará os dados conforme LGPD, utilizando-os somente para execução do contrato.",
            "9.2. Garantirá confidencialidade e treinamento de seus colaboradores.",
            "9.3. Usará medidas técnicas e administrativas de segurança.",
            "9.4. Não compartilhará dados com terceiros sem necessidade contratual.",
            "9.5. Ao final do contrato, os dados serão descartados conforme prazos legais.",

            "CLÁUSULA DÉCIMA – ANTICORRUPÇÃO",
            "10.1. A ADUS compromete-se a cumprir legislação anticorrupção, improbidade e prevenção à lavagem de dinheiro.",

            "CLÁUSULA DÉCIMA PRIMEIRA – INADIMPLEMENTO E RESCISÃO",
            "11.1. Inadimplência de duas parcelas permite antecipação da dívida ou rescisão com perda de valores pagos.",
            "11.2. O COMPRADOR concorda com a destinação social dos recursos pela ADUS.",

            "CLÁUSULA DÉCIMA SEGUNDA – CONDIÇÕES GERAIS",
            "12.1. Contrato irrevogável e irretratável, vinculando herdeiros e sucessores.",
            "12.2. Tolerância em obrigações não implica renúncia.",
            "12.3. Obrigações assumidas pela ADUS em nome do COMPRADOR serão reembolsadas com multa e correção.",
            "12.4. Comunicações serão válidas por correspondência ou e-mail com comprovação.",
            "12.5. As partes declaram ter poderes, conhecimento e patrimônio para assumir este contrato.",
            "12.6. O presente contrato é título executivo extrajudicial (CPC, art. 784, III).",
            "12.7. Os considerandos integram o contrato como declarações de vontade.",

            "CLÁUSULA DÉCIMA TERCEIRA – FORO",
            "13.1. Instrumento firmado por meio eletrônico conforme MP 2.200-2/01.",
            "13.2. Fica eleito o foro da Comarca de Sinop - MT, renunciando-se a qualquer outro, por mais privilegiado que seja."
        };
    }
}
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
                        "DAS CONSIDERAÇÕES INICIAIS:",

            "(I) CONSIDERANDO que a ADUS desenvolve atividade com fim social, como, a defesa, o estudo, a coordenação, aquisições de bens ou serviços, o desenvolvimento de projetos e construções e infraestruturas, realizar ou custear pesquisas, efetuar doações, desenvolver e executar campanhas de arrecadação, estimular e exigir dos entes Públicos a adoção de medidas, realizar e divulgar pesquisas, promover cursos e outros, promover e apoiar atividades culturais e educacionais, fazer uso de meios jurídicos e extrajudiciais, as ações, o apoio, a realização de atividades e em especial para arrecadar recurso para manutenção de alunos bolsistas das Escolas selecionadas pela ADUS.",

            "(II) CONSIDERANDO que a ADUS, com o objetivo de cumprir com as finalidades do seu Estatuto Social (Doc. Anexo), está desenvolvendo o projeto de aquisição e/ou plantio de árvore Tectona Grandis - TECA, denominado de “TECA SOCIAL”, que consiste no plantio, trato e venda de árvores em pé, cujo resultado financeiro será destinado o percentual de 100% do lucro líquido, para doação, com finalidade de manutenção de alunos bolsistas das Escolas selecionadas pela ADUS.",

            "(III) CONSIDERANDO que, caso seja verificado que qualquer Instituição envolvida, para a qual será destinada as doações com termos e condições de aplicação exclusiva, não esteja aplicando adequadamente os recursos advindo da doação, poderá a ADUS suspender as doações para aquela instituição e direcioná-los para outra instituição, com mesma finalidade, que aplique os recursos doados, dentro da mesma localidade. Tais informações serão disponibilizadas nos canais de transparência, no site da ADUS (http://tecasocial.com.br) e/ou via aplicativo a ser indicado em momento oportuno.",

            "(IV) CONSIDERANDO que, a ADUS apresentou previamente aos diversos Parceiros e Interessados, o projeto a ser desenvolvido, com detalhamento de forma minuciosa acerca do procedimento que ocorrerá entre o plantio e a colheita das árvores que serão produzidas, em especial, mas não se limitando, ao tempo de maturidade da árvore, que leva cerca de 18 (dezoito) anos para sua colheita, dentre outras especificidades.",

            "(V) CONSIDERANDO que o plantio das árvores espécie Tectona Grandis – TECA, será realizado no imóvel rural denominado Fazenda Igarapézinho, localizada na zona rural, no município de Cametá/PA, objeto da Matrícula 4.666 do CRI do 1° Ofício de Cametá, Estado do Pará, cujo imóvel será utilizado para realização do plantio das árvores da espécie Tectona Grandis – TECA.",

            "(VI) CONSIDERANDO que, a ADUS irá utilizar a “Área do Projeto” para o desenvolvimento do Projeto “TECA SOCIAL”, conservando-a como se a coisa fosse sua, devendo para tanto explorá-la com a aplicação de técnicas modernas e adequadas para a região, e sob as normas de melhor uso da terra, fazendo-se a correção do solo, combate a erosão, adubação, conservação de suas riquezas naturais, especialmente os rios, nascentes, áreas florestais e recursos de fauna e flora, nos termos da legislação ambiental, responsabilidade que poderá inclusive ser partilhada em Contrato de Parceria Rural, nos moldes da Lei 4.504/64 e do Decreto 59.566/66.",

            "(VII) CONSIDERANDO que, o COMPRADOR, após apresentação do projeto a ser desenvolvido, tem interesse em realizar a aquisição de árvores em pé da espécie Tectona Grandis – TECA, com a finalidade de obter para si, e fazer uso conforme seu interesse, da madeira proveniente da árvore após sua colheita e corte;",

            "(VIII) CONSIDERANDO que, o COMPRADOR tem plena consciência e está plenamente de acordo com o cunho humanitário do Projeto Teca Social e da atuação da ADUS no sentido de destinar parte dos recursos advindos com o presente contrato para ações sociais, visando o desenvolvimento do seu objeto social.",

            "RESOLVEM as PARTES, de comum acordo, celebrar o presente INSTRUMENTO PARTICULAR DE COMPRA E VENDA DE ÁRVORE EM PÉ PARA ENTREGA FUTURA, na forma das cláusulas e condições a seguir descritas.",

            "CLÁUSULA PRIMEIRA – DO OBJETO",

            "1.1. O presente contrato tem como objeto a comercialização de árvores da espécie Tectona Grandis – TECA, com Potencial de produção de 1m³ (um metro cúbico) por árvore, cubados pela fórmula Hoppus; Altura de 12m (doze metros) por árvore. Plantio que possui como finalidade precípua arrecadar recursos para manutenção de alunos bolsistas das Escolas selecionadas pela ADUS, nos moldes e percentuais mencionados no Considerando II.",

            "1.2. As árvores encontram-se em processo de aquisição e/ou preparo de solo para início do plantio, no imóvel rural descrito no Considerando V acima, e estarão disponibilizadas ao COMPRADOR somente quando atingidas as especificações mínimas descritas no item 1.1 acima, bem como, quando atingirem seu período de maturidade que corresponde a 18 (dezoito) anos, conforme exposto no Considerando IV, contados a partir da assinatura do presente contrato.",

            "1.3. O COMPRADOR poderá realizar o acompanhamento de desenvolvimento da(s) árvore(s) adquiridas por meio do site da ADUS (http://tecasocial.com.br) e/ou via aplicativo, em que estarão disponíveis todas as informações de forma transparente ao adquirente.",

            "1.4. O COMPRADOR concorda e manifesta ciência, de que a aquisição da(s) árvore(s), nos termos indicados no item 1.1. desta Cláusula Primeira, se trata de compra e venda para entrega futura, de acordo com o prazo de vigência do presente instrumento, bem como de acordo com o prazo necessário para o crescimento e amadurecimento do espécime até o alcance do seu ponto adequado de corte e colheita.",

            "CLÁUSULA SEGUNDA – DO PREÇO E FORMA DE PAGAMENTO",

            "2.1. O preço total, certo e previamente ajustado entre as partes para a venda da árvore em pé objeto deste contrato está descrito e detalhado nos itens 3 e 4 do Preâmbulo acima.",

            "2.1.1. O valor total da venda, levando-se em conta o quantitativo de árvores adquiridas pelo COMPRADOR bem como a quantidade de parcelas e a forma de pagamento, está estabelecido no Preâmbulo do presente Instrumento.",

            "2.1.2. O pagamento na modalidade à vista, ou, na modalidade parcelada deverá ser realizado mediante as seguintes formas de pagamento, conforme detalhado no Preâmbulo do presente Contrato de Compra e Venda de Árvore em Pé para Entrega Futura:",

            "a) Pagamento do valor total à vista, mediante emissão de boleto bancário, pela ADUS contra o COMPRADOR, e encaminhado a este último para pagamento, com antecedência mínima de 05 (cinco) dias antes do vencimento;",

            "b) Pagamento do valor total à vista, por meio de transferência PIX (com QR Code emitido pela ADUS), contra o COMPRADOR, e encaminhado a este último para pagamento, com antecedência mínima de 05 (cinco) dias antes do vencimento;",

            "c) Pagamento do valor total à vista, por meio de transação bancária de crédito, mediante cartão de crédito;",

            "d) Pagamento do valor total, de forma parcelada, mediante emissão de boleto bancário, pela ADUS contra o COMPRADOR, e encaminhado a este último para pagamento, com antecedência mínima de 05 (cinco) dias antes do vencimento;",

            "e) Pagamento do valor total, de forma parcelada, mediante transferência PIX (com QR Code emitido pela ADUS), contra o COMPRADOR, e encaminhado a este último para pagamento, com antecedência mínima de 05 (cinco) dias antes do vencimento;",

            "f) Pagamento do valor total, de forma parcelada, mediante transação bancária de crédito rotativo e/ou recorrente, mediante cartão de crédito;",

            "2.2. Sem prejuízo do disposto no item 2.1 e seguintes do presente contrato, o não pagamento de qualquer das parcelas avençadas, por culpa do COMPRADOR, sujeitá-lo-á ao pagamento de multa moratória de 2% (dois por cento) sobre o total do débito em atraso e juros de mora de 1% (um por cento) ao mês, aplicado sobre o valor corrigido monetariamente pelo índice IGP-M/FGV, contados diariamente (pro-rata-die) a partir da data do vencimento de cada parcela, até a data do efetivo pagamento.",

            "2.3. A inadimplência de qualquer das parcelas descritas na forma de pagamento do preâmbulo acima, por prazo de até 60 (sessenta) dias, a ADUS poderá, a seu exclusivo critério, exercer a opção discriminada no item 11.1 deste instrumento, com as cominações descritas em seus itens e subitens.",

            "2.4. Nas vendas realizadas de forma eletrônica (site) o aceite do COMPRADOR acerca da proposta e condições de comercialização se dará com a realização do seu cadastro na plataforma virtual.",

            "CLÁUSULA TERCEIRA – DA VIGÊNCIA",

            "3.1. O presente contrato terá seu prazo de vigência máximo de 18 (dezoito) anos que corresponde ao tempo de maturidade para corte da árvore, devendo ainda ser observadas as condições descritas no item 1.2 para entrega da árvore.",

            "CLÁUSULA QUARTA – DO PLANTIO, MANUTENÇÃO, CORTE, ARMAZENAMENTO E RETIRADA DAS ÁRVORES",

            "4.1. Por força do presente contrato, todos os encargos referentes ao plantio, à manutenção e conservação das árvores serão incorridos pela ADUS até o momento em que elas ficarem disponíveis para o COMPRADOR.",

            "CLÁUSULA QUINTA – DA AUSÊNCIA, INCAPACIDADE E FALECIMENTO DO COMPRADOR NA VIGÊNCIA DO CONTRATO",

            "5.1. Em se tratando de COMPRADOR pessoa física, caso sobrevenha ocorrência de incapacidade ou ausência reconhecida judicialmente, seu respectivo curador nomeado judicialmente ou representante, responderá em nome daquele pelos direitos e obrigações oriundos do presente contrato.",

            "CLÁUSULA SEXTA – DO ENCERRAMENTO, RECUPERAÇÃO JUDICIAL, FALÊNCIA, E SUCESSÃO DA PESSOA JURÍDICA COMPRADORA",

            "6.1. Em caso de COMPRADOR pessoa jurídica, na eventual ocorrência de seu encerramento, e/ou decretação de recuperação judicial ou falência, responderá em nome daquela, os seus sócios ou o administrador judicial nomeado, pelos direitos e obrigações oriundos do presente contrato.",

            "CLÁUSULA SÉTIMA – DO DIREITO DE DESISTÊNCIA DO COMPRADOR",

            "7.1. O COMPRADOR, na qualidade de consumidor, terá o prazo de 7 (sete) dias corridos contados da data de assinatura do Contrato de Compra e Venda, para desistir da compra realizada, desde que esta tenha sido realizada de forma virtual.",

            "CLÁUSULA OITAVA – DA TRANSPARÊNCIA E DIREITO DE FISCALIZAÇÃO E VISITAÇÃO DO COMPRADOR",

            "8.1. A ADUS disponibilizará e manterá página na internet devidamente atualizada com a evolução da plantação. Haverá a possibilidade de visitação presencial pelo COMPRADOR.",

            "CLÁUSULA NONA – DA PROTEÇÃO AOS DADOS PESSOAIS",

            "9.1. A ADUS se compromete em tratar os dados pessoais envolvidos na confecção e necessários à execução do Contrato de Prestação de Serviços.",

            "CLÁUSULA DÉCIMA – DAS DISPOSIÇÕES ANTICORRUPÇÃO",

            "10.1. A ADUS compromete-se ao cumprimento da legislação brasileira anticorrupção.",

            "CLÁUSULA DÉCIMA PRIMEIRA – DO INADIMPLEMENTO E DA RESCISÃO CONTRATUAL",

            "11.1. Na hipótese de inadimplência por parte do COMPRADOR(A), de 02(duas) ou mais parcelas, poderá a ADUS considerar a totalidade da dívida vencida antecipadamente.",

            "CLÁUSULA DÉCIMA SEGUNDA - DAS CONDIÇÕES GERAIS",

            "12.1. O presente contrato de compra e venda é celebrado em caráter irrevogável e irretratável, não admitindo arrependimento para qualquer das partes.",

            "CLÁUSULA DÉCIMA TERCEIRA – DO FORO",

            "13.1. As partes elegem o foro da Comarca de Sinop, Estado de Mato Grosso, para dirimir as questões oriundas da interpretação ou aplicação deste contrato.",
        };
    }
}
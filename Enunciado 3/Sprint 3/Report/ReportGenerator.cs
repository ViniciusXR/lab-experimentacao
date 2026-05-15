using System;
using System.Collections.Generic;
using System.IO;
using Lab03S03.Analysis;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lab03S03.Report
{
    public static class ReportGenerator
    {
        public static void Generate(List<AnalysisResult> results, string outputPath, int totalPrs)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, results, totalPrs));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(outputPath);
        }

        private static void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Laboratório 03 — Caracterizando a Atividade de Code Review no GitHub")
                    .FontSize(15).Bold().FontColor(Colors.Orange.Darken2);

                col.Item().Text("Relatório Final · Lab03S03 · Análise, Visualização e Síntese")
                    .FontSize(11).SemiBold().FontColor(Colors.Blue.Darken2);

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Engenharia de Software  ·  Laboratório de Experimentação de Software  ·  6º Período  ·  PUC Minas")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    row.AutoItem().Text($"Gerado em: {System.DateTime.Now:dd/MM/yyyy}")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });

                col.Item().Text("Professor: Danilo de Quadros Maia Filho")
                    .FontSize(8.5f).FontColor(Colors.Grey.Darken2);

                col.Item().PaddingTop(5).PaddingBottom(3)
                    .Background(Colors.Orange.Lighten4)
                    .Border(1).BorderColor(Colors.Orange.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .Text("Sthel Felipe Torres  ·  Vinicius Xavier Ramalho")
                    .FontSize(12).Bold().FontColor(Colors.Orange.Darken3);

                col.Item().LineHorizontal(2).LineColor(Colors.Orange.Medium);
            });
        }

        private static void ComposeContent(IContainer container, List<AnalysisResult> results, int totalPrs)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                // Flowcharts (Box 1)
                col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Background(Colors.Blue.Lighten5).Column(box =>
                {
                    box.Item().Text("1. Planejamento e execução do experimento").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                    box.Item().Text("As figuras abaixo documentam o planejamento experimental (desenho inicial, etapas e organização da análise).").FontSize(10);

                    string flow1Path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(results[0].ChartPath)), "charts", "fluxograma_etapas_1_2.png");
                    if (File.Exists(flow1Path))
                    {
                        box.Item().PaddingTop(8).AlignCenter().Height(400).Image(flow1Path).FitArea();
                    }
                });

                col.Item().PageBreak();

                col.Item().Column(x =>
                {
                    x.Spacing(6);
                    x.Item().Text("1.1 Evidência visual do planejamento - Parte 2").SemiBold().FontSize(10);
                    string flow2Path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(results[0].ChartPath)), "charts", "fluxograma_etapas_3_4.png");
                    if (File.Exists(flow2Path))
                    {
                         x.Item().AlignCenter().Height(400).Image(flow2Path).FitArea();
                    }
                });

                col.Item().PageBreak();

                // Intro Box
                col.Item().Border(1).BorderColor(Colors.Orange.Lighten3).Padding(10).Background(Colors.Orange.Lighten5).Column(box =>
                {
                    box.Item().Text("2. Introdução e hipóteses informais").Bold().FontSize(13).FontColor(Colors.Orange.Darken3);
                    box.Item().Text($"Este relatório apresenta análises sobre o processo de code review no GitHub, utilizando dados coletados via API. Exploramos o impacto do tamanho do PR, tempo de análise, descrições e interações nas taxas de merge e esforço de revisão. Total de Pull Requests analisados neste relatório: {totalPrs}");
                    box.Item().PaddingTop(4).Text("Questões de pesquisa e hipóteses (informais):").SemiBold();
                    box.Item().Text("• RQ01: PRs menores têm maior chance de serem aceitos.");
                    box.Item().Text("• RQ02: PRs que levam mais tempo em análise tendem a ser rejeitados.");
                    box.Item().Text("• RQ03: PRs com descrições mais detalhadas têm maior chance de serem aceitos.");
                    box.Item().Text("• RQ04: PRs com mais interações tendem a ser aceitos.");
                    box.Item().Text("• RQ05: PRs maiores geram mais revisões.");
                    box.Item().Text("• RQ06: PRs com maior tempo de análise acumulam mais revisões.");
                    box.Item().Text("• RQ07: Descrições mais longas correlacionam com mais revisões.");
                    box.Item().Text("• RQ08: PRs com mais participantes e comentários naturalmente têm mais revisões.");
                });

                // Metodologia Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("3. Metodologia").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);

                    box.Item().PaddingTop(4).Text("3.1 Criação do Dataset").SemiBold().FontSize(11);
                    box.Item().Text($"Os dados foram coletados via API GraphQL do GitHub. O dataset é composto pelos {totalPrs} PRs que satisfazem todos os critérios abaixo:");
                    box.Item().PaddingTop(3).Text("• Repositórios: 200 repositórios mais populares do GitHub (ordenados por número de estrelas).").FontSize(10);
                    box.Item().Text("• Requisito mínimo por repositório: pelo menos 100 PRs com status MERGED ou CLOSED.").FontSize(10);
                    box.Item().Text("• Status do PR: apenas MERGED ou CLOSED (PRs abertos foram excluídos).").FontSize(10);
                    box.Item().Text("• Revisões: apenas PRs com pelo menos uma revisão registrada (campo reviews.totalCount ≥ 1).").FontSize(10);
                    box.Item().Text("• Filtro anti-bot: apenas PRs cujo intervalo entre a criação e o fechamento/merge é superior a 1 hora — eliminando revisões automáticas realizadas por bots ou ferramentas de CI/CD.").FontSize(10);

                    box.Item().PaddingTop(6).Text("3.2 Teste Estatístico").SemiBold().FontSize(11);
                    box.Item().Text("Utilizamos o coeficiente de correlação de Spearman para todas as análises. A escolha é justificada pela natureza não-paramétrica dos dados: as métricas coletadas (linhas de código, tempo de análise, número de comentários) apresentam assimetria severa e outliers extremos, tornando a distribuição normal inviável e o teste de Pearson inadequado. O Spearman opera sobre postos (ranks), sendo robusto a essas distorções. Para cada correlação, calculamos também o p-valor (aproximação t-Student bilateral) com limiar de significância α = 0,05.");

                    box.Item().PaddingTop(6).Text("3.3 Sumarização dos Resultados").SemiBold().FontSize(11);
                    box.Item().Text("Conforme indicado no enunciado, as análises utilizam os valores medianos calculados sobre todos os PRs do dataset — sem divisão por repositório. Para a Dimensão A (Status do PR), a variável dependente é binária: MERGED = 1, CLOSED = 0. Para a Dimensão B (Número de Revisões), a variável dependente é o campo reviews_count.");
                });

                // Resultados — título da seção
                col.Item().PaddingTop(4).Text("4. Resultados (RQs)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);

                // Cada RQ em sua própria caixa para permitir quebras de página limpas e gráficos maiores
                foreach (var res in results)
                {
                    col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Column(box =>
                    {
                        box.Item().Text($"{res.RQCode} — {res.Title}").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        box.Item().PaddingTop(2).Text($"Hipótese: {res.Hypothesis}").Italic().FontSize(10).FontColor(Colors.Grey.Darken2);

                        box.Item().PaddingVertical(5).Element(c => ComposeMedianTable(c, res));

                        if (!string.IsNullOrEmpty(res.ChartPath) && File.Exists(res.ChartPath))
                        {
                            box.Item().PaddingVertical(4).AlignCenter().Height(420).Image(res.ChartPath).FitArea();
                        }

                        box.Item().PaddingTop(4).Text($"Correlação [{res.PrimaryLabel}]: {res.Interpretation} (ρ = {res.SpearmanRho:F3}, p = {res.PValue:e3}{(res.PValue > 0.05 ? " — sem significância estatística" : "")})").FontSize(10);

                        if (res.HasSecondaryMetric)
                        {
                            box.Item().Text($"  ↳ [{res.SecondaryLabel}]: {res.SecondaryInterpretation} (ρ = {res.SecondarySpearmanRho:F3}, p = {res.SecondaryPValue:e3}{(res.SecondaryPValue > 0.05 ? " — sem significância estatística" : "")})").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }

                        box.Item().PaddingTop(4).Text($"Discussão: {res.Discussion}").FontSize(10);
                    });
                }

                // Conclusion Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("5. Discussão Geral e Conclusão").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                    box.Item().Text("Analisando as 8 Questões de Pesquisa, observamos que 5 de 8 hipóteses foram confirmadas (RQ02, RQ05, RQ06, RQ07, RQ08) e apenas 3 foram refutadas (RQ01, RQ03, RQ04). Dentre os achados confirmados, o relacionamento de interações dita positivamente a cadência e número de revisões com a maior força do relatório (RQ08). Curiosamente, outras hipóteses formuladas apresentaram resultados inesperados: a RQ01 surpreendeu ao mostrar levemente que PRs MAIORES tendem a ser aceitos; já a RQ03 e a RQ04 não apresentaram significância estatística, evidenciando que maiores descrições ou alto volume de interações não garantem aprovação (P-Valor > 0.05).");
                    box.Item().PaddingTop(4).Text("Limitações do estudo e Trabalhos Futuros:").SemiBold();
                    box.Item().Text("Observa-se que em variáveis como 'Review Count' a mediana massivamente se assenta em torno de 1,00, apontando que variabilidades altas acontecem fundamentalmente em PRs contendo grandes distorções, com as correlações em geral não ultrapassando a limitação de tendências fracas e moderadas. Para trabalhos futuros, sugere-se analisar isoladamente apenas PRs com review_count ≥ 2 para verificar se os padrões se mantêm ou fortalecem em revisões mais elaboradas.");
                });
            });
        }

        private static void ComposeMedianTable(IContainer container, AnalysisResult r)
        {
            bool isStatusVsMetric = r.RQCode == "RQ01" || r.RQCode == "RQ02" || r.RQCode == "RQ03" || r.RQCode == "RQ04";
            string primaryLabel = string.IsNullOrEmpty(r.PrimaryLabel) ? r.Title : r.PrimaryLabel;

            container.Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Métrica").Bold();
                    if (isStatusVsMetric)
                    {
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana MERGED").Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana CLOSED").Bold();
                    }
                    else
                    {
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Med. Variável Indep.").Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana Revisões").Bold();
                    }
                });

                // Linha da métrica primária
                table.Cell().Padding(2).Text(primaryLabel);
                table.Cell().Padding(2).Text(r.MedianMerged.ToString("F2"));
                table.Cell().Padding(2).Text(r.MedianClosed.ToString("F2"));

                // Linha da métrica secundária (Tamanho: total de linhas; Interações: participantes)
                if (r.HasSecondaryMetric)
                {
                    table.Cell().Padding(2).Text(r.SecondaryLabel).FontColor(Colors.Grey.Darken2);
                    table.Cell().Padding(2).Text(r.SecondaryMedianMerged.ToString("F2")).FontColor(Colors.Grey.Darken2);
                    table.Cell().Padding(2).Text(r.SecondaryMedianClosed.ToString("F2")).FontColor(Colors.Grey.Darken2);
                }
            });
        }
    }
}
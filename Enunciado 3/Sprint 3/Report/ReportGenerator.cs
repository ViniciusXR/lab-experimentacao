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
                col.Item().Text("Enunciado 3 — Caracterizando a Atividade de Code Review").FontSize(16).Bold().FontColor(Colors.Orange.Darken2);
                col.Item().Text("Relatório final").FontSize(12).SemiBold();
                col.Item().Text("Laboratório de Experimentação de Software").FontSize(10).FontColor(Colors.Grey.Darken2);
                col.Item().Text("PUC Minas | Período: 6º | Alunos: Sthel Felipe Torres, Vinicius Xavier Ramalho").FontSize(10).Bold();
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
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
                    box.Item().Text("Os dados foram coletados dos 200 repositórios mais populares. Foram selecionados PRs fechados ou mergeados com estado de revisão. Usamos o coeficiente de correlação de Spearman devido à natureza não-paramétrica dos dados (assimetria severa e outliers extremos em métricas como linhas de código e tempo de análise, onde a média e distribuição normal falham para modelar a realidade adequadamente).");
                });

                // Resultados
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("4. Resultados (RQs)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);

                    for (int i = 0; i < results.Count; i++)
                    {
                        var res = results[i];
                        box.Item().PaddingTop(10).Text($"{res.RQCode} - {res.Title}").FontSize(11).Bold();
                        box.Item().Text($"Hipótese: {res.Hypothesis}").Italic().FontSize(10).FontColor(Colors.Grey.Darken2);

                        // Table
                        string unit = "";
                        if (res.RQCode == "RQ01" || res.RQCode == "RQ05") unit = " (Arquivos)";
                        if (res.RQCode == "RQ02" || res.RQCode == "RQ06") unit = " (Horas)";
                        if (res.RQCode == "RQ03" || res.RQCode == "RQ07") unit = " (Caracteres)";
                        if (res.RQCode == "RQ04" || res.RQCode == "RQ08") unit = " (Interações)";

                        box.Item().PaddingVertical(5).Element(c => ComposeMedianTable(c, res, unit));

                        // Image
                        if (!string.IsNullOrEmpty(res.ChartPath) && File.Exists(res.ChartPath))
                        {
                            box.Item().AlignCenter().Height(300).Image(res.ChartPath).FitArea();
                        }

                        // Discusion
                        box.Item().PaddingTop(5).Text($"Interpretação da correlação: {res.Interpretation} (rho = {res.SpearmanRho:F3}, p = {res.PValue:e3}{(res.PValue > 0.05 ? " - Sem significância estatística" : "")})").FontSize(10);
                        box.Item().Text($"Discussão: {res.Discussion}").FontSize(10);

                        if (i < results.Count - 1)
                        {
                            box.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                        }
                    }
                });

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

        private static void ComposeMedianTable(IContainer container, AnalysisResult r, string unit)
        {
            bool isStatusVsMetric = r.RQCode.StartsWith("RQ01") || r.RQCode.StartsWith("RQ02") || r.RQCode.StartsWith("RQ03") || r.RQCode.StartsWith("RQ04");

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
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text($"Mediana MERGED{unit}").Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text($"Mediana CLOSED{unit}").Bold();
                    }
                    else
                    {
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text($"Med. Variável Indep.{unit}").Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana Global Revisões").Bold();
                    }
                });

                table.Cell().Padding(2).Text(r.Title);
                table.Cell().Padding(2).Text(r.MedianMerged.ToString("F2"));
                table.Cell().Padding(2).Text(r.MedianClosed.ToString("F2"));
            });
        }
    }
}
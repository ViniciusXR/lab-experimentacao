using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lab03S03.Analysis;
using Lab03S03.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lab03S03.Report
{
    public static class ReportGenerator
    {
        public static void Generate(List<AnalysisResult> results, string outputPath, List<PullRequest> prs)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            int totalPrs = prs.Count;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, results, totalPrs, prs));
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

                col.Item().Text("Alunos: Sthel Felipe Torres e Vinicius Xavier Ramalho")
                    .FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);

                col.Item().LineHorizontal(2).LineColor(Colors.Orange.Medium);
            });
        }

        private static void ComposeContent(IContainer container, List<AnalysisResult> results, int totalPrs, List<PullRequest> prs)
        {
            var pt = CultureInfo.GetCultureInfo("pt-BR");
            var globalRows = GlobalDescriptiveStats.SummarizeAll(prs);
            string interpretacaoGlobal = GlobalDescriptiveStats.BuildInterpretation(globalRows, totalPrs);

            string chartsDir = results.Count > 0 && !string.IsNullOrEmpty(results[0].ChartPath)
                ? Path.GetDirectoryName(results[0].ChartPath) ?? ""
                : "";

            container.Column(col =>
            {
                col.Spacing(12);

                ReportNarrativeContent.ComposeIndice(col);
                col.Item().PageBreak();

                ReportNarrativeContent.ComposeIntroducaoCompleta(col, totalPrs);
                col.Item().PageBreak();

                // Flowcharts — secção 2 (após índice e introdução)
                col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Background(Colors.Blue.Lighten5).Column(box =>
                {
                    box.Item().Text("2. Planejamento e execução do experimento").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                    box.Item().Text("As figuras abaixo documentam o planejamento experimental (desenho inicial, etapas e organização da análise).").FontSize(10);

                    string flow1Path = Path.Combine(chartsDir, "fluxograma_etapas_1_2.png");
                    if (File.Exists(flow1Path))
                    {
                        box.Item().PaddingTop(8).AlignCenter().Height(400).Image(flow1Path).FitArea();
                    }
                });

                col.Item().PageBreak();

                col.Item().Column(x =>
                {
                    x.Spacing(6);
                    x.Item().Text("2.1 Evidência visual do planejamento — Parte 2").SemiBold().FontSize(10);
                    string flow2Path = Path.Combine(chartsDir, "fluxograma_etapas_3_4.png");
                    if (File.Exists(flow2Path))
                    {
                         x.Item().AlignCenter().Height(400).Image(flow2Path).FitArea();
                    }
                });

                col.Item().PageBreak();

                // Metodologia Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("3. Metodologia").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);

                    box.Item().PaddingTop(4).Text("3.1 Criação do Dataset").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text($"Os dados foram coletados via API GraphQL do GitHub. O dataset é composto pelos {totalPrs} PRs que satisfazem todos os critérios abaixo:").FontSize(12);
                    box.Item().PaddingTop(3).Text("• Repositórios: 200 repositórios mais populares do GitHub (ordenados por número de estrelas).").FontSize(11);
                    box.Item().Text("• Requisito mínimo por repositório: pelo menos 100 PRs com status MERGED ou CLOSED.").FontSize(11);
                    box.Item().Text("• Status do PR: apenas MERGED ou CLOSED (PRs abertos foram excluídos).").FontSize(11);
                    box.Item().Text("• Revisões: apenas PRs com pelo menos uma revisão registrada (campo reviews.totalCount ≥ 1).").FontSize(11);
                    box.Item().Text("• Filtro anti-bot: apenas PRs cujo intervalo entre a criação e o fechamento/merge é superior a 1 hora.").FontSize(11);

                    box.Item().PaddingTop(8).Text("3.2 Teste Estatístico").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text("Utilizamos o coeficiente de correlação de Spearman para todas as análises. A escolha é justificada pela natureza não-paramétrica dos dados: as métricas coletadas apresentam assimetria severa e outliers extremos, tornando a distribuição normal inviável e o teste de Pearson inadequado. O Spearman opera sobre postos (ranks), sendo robusto a essas distorções. Calculamos também o p-valor (aproximação t-Student bilateral) com limiar de significância α = 0,05.").FontSize(12);

                    box.Item().PaddingTop(8).Text("3.3 Sumarização dos Resultados").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text("Para a Dimensão A (Status do PR), a variável dependente é binária (MERGED = 1, CLOSED = 0). A agregação dessas métricas é baseada nas correlações da população estatística gerada pelo Dataset completo de repositórios.").FontSize(12);
                });

                // Resultados — título da seção
                col.Item().PaddingTop(4).Text("4. Resultados (RQs)").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);

                // Cada RQ em sua própria caixa para permitir quebras de página limpas e gráficos maiores
                foreach (var res in results)
                {
                    col.Item().PageBreak();
                    col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Column(box =>
                    {
                        box.Item().Text($"{res.RQCode} — {res.Title}").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                        box.Item().PaddingTop(2).Text($"Hipótese: {res.Hypothesis}").Italic().FontSize(12).FontColor(Colors.Grey.Darken2);

                        box.Item().PaddingVertical(5).Element(c => ComposeMedianTable(c, res));

                        if (!string.IsNullOrEmpty(res.ChartPath) && File.Exists(res.ChartPath))
                        {
                            box.Item().PaddingVertical(8).AlignCenter().Image(res.ChartPath).FitArea();
                        }

                        string pValLabel = res.PValue < 0.001 ? "< 0,001" : $"= {res.PValue:e3}";
                        box.Item().PaddingTop(10).Text($"Correlação [{res.PrimaryLabel}]: {res.Interpretation} (ρ = {res.SpearmanRho:F3}, p {pValLabel}{(res.PValue > 0.05 ? " — sem significância estatística" : "")})").FontSize(12).SemiBold();

                        if (res.HasSecondaryMetric)
                        {
                            string secPValLabel = res.SecondaryPValue < 0.001 ? "< 0,001" : $"= {res.SecondaryPValue:e3}";
                            box.Item().PaddingTop(2).Text($"  ↳ [{res.SecondaryLabel}]: {res.SecondaryInterpretation} (ρ = {res.SecondarySpearmanRho:F3}, p {secPValLabel}{(res.SecondaryPValue > 0.05 ? " — sem significância estatística" : "")})").FontSize(11).FontColor(Colors.Grey.Darken1);
                        }

                        box.Item().PaddingTop(8).Text($"Discussão: {res.Discussion}").FontSize(12);
                    });
                }

                col.Item().PageBreak();

                // Sumarização global (estilo Lab02 — antes da discussão final)
                col.Item().Border(1).BorderColor(Colors.Teal.Lighten3).Padding(10).Background(Colors.Teal.Lighten5).Column(box =>
                {
                    box.Item().Text("5. Sumarização global das métricas (processo)").Bold().FontSize(15).FontColor(Colors.Teal.Darken3);
                    box.Item().PaddingTop(4).Text("Tabela com estatísticas descritivas (média, mediana e desvio padrão) sobre todo o dataset de PRs, no mesmo espírito do relatório do Laboratório 02 — visão agregada antes das correlações por RQ.").FontSize(10);
                    box.Item().PaddingTop(8).Element(c => ComposeGlobalMetricsTable(c, globalRows, pt));
                    box.Item().PaddingTop(10).Text("5.1 Interpretação (processo):").SemiBold().FontSize(11);
                    box.Item().PaddingTop(2).Text(interpretacaoGlobal).FontSize(10);
                });

                col.Item().PageBreak();

                // Conclusion Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("6. Discussão Geral e Conclusão").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);
                    box.Item().PaddingTop(4).Text("Analisando as 8 Questões de Pesquisa, observamos que 5 de 8 hipóteses foram confirmadas (RQ02, RQ05, RQ06, RQ07, RQ08) e apenas 3 foram refutadas (RQ01, RQ03, RQ04). Dentre os achados confirmados, o relacionamento de interações dita positivamente a cadência e número de revisões com a maior força do relatório (RQ08). Curiosamente, outras hipóteses formuladas apresentaram resultados inesperados: a RQ01 surpreendeu ao mostrar levemente que PRs MAIORES tendem a ser aceitos; a RQ03 não apresentou significância estatística (p-valor > 0,05), evidenciando que o comprimento da descrição não influencia diretamente na aprovação; e a RQ04, embora estatisticamente significativa (p-valor < 0,05), revelou uma correlação negativa, indicando que um maior volume de comentários está associado à rejeição do PR, possivelmente devido a discussões extensas geradas por códigos problemáticos antes de seu fechamento definitivo.").FontSize(12);
                    box.Item().PaddingTop(8).Text("Limitações do estudo e Trabalhos Futuros:").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text("Observa-se que em variáveis como 'Review Count' a mediana massivamente se assenta em torno de 1,00, apontando que variabilidades altas acontecem fundamentalmente em PRs contendo grandes distorções, com as correlações em geral não ultrapassando a limitação de tendências fracas e moderadas. Para trabalhos futuros, sugere-se analisar isoladamente apenas PRs com review_count ≥ 2 para verificar se os padrões se mantêm ou fortalecem em revisões mais elaboradas.").FontSize(12);
                });
            });
        }

        private static void ComposeGlobalMetricsTable(IContainer container, List<GlobalMetricSummary> rows, CultureInfo pt)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.ConstantColumn(50);
                    c.ConstantColumn(70);
                    c.ConstantColumn(70);
                    c.ConstantColumn(80);
                });

                table.Header(h =>
                {
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Métrica").Bold();
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("n").Bold();
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Média").Bold();
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana").Bold();
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Desvio padrão").Bold();
                });

                foreach (var r in rows)
                {
                    table.Cell().Padding(2).Text(r.Label).FontSize(9);
                    table.Cell().Padding(2).Text(r.N.ToString("N0", pt)).FontSize(9);
                    table.Cell().Padding(2).Text(r.Mean.ToString("N2", pt)).FontSize(9);
                    table.Cell().Padding(2).Text(r.Median.ToString("N2", pt)).FontSize(9);
                    table.Cell().Padding(2).Text(r.StdDev.ToString("N2", pt)).FontSize(9);
                }
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
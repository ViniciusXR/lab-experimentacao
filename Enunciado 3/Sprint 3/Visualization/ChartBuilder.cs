using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lab03S03.Analysis;
using Lab03S03.Models;
using MathNet.Numerics.Statistics;
using ScottPlot;

namespace Lab03S03.Visualization
{
    public static class ChartBuilder
    {
        public static string Generate(List<PullRequest> prs, AnalysisResult result, string outputDir)
        {
            var plt = new Plot();
            string fileName = "";

            var merged = prs.Where(p => p.Status.ToUpper() == "MERGED").ToList();
            var closed = prs.Where(p => p.Status.ToUpper() == "CLOSED").ToList();

            if (result.RQCode.StartsWith("RQ01") || result.RQCode.StartsWith("RQ02") || result.RQCode.StartsWith("RQ03") || result.RQCode.StartsWith("RQ04"))
            {
                // Boxplots for categorical
                System.Func<PullRequest, double> selector = null;
                string yLabel = "";
                switch (result.RQCode)
                {
                    case "RQ01": selector = p => p.FilesChanged; fileName = "rq01_tamanho.png"; yLabel = "Tamanho (Arquivos)"; break;
                    case "RQ02": selector = p => p.AnalysisTimeH; fileName = "rq02_tempo.png"; yLabel = "Tempo (Horas)"; break;
                    case "RQ03": selector = p => p.BodyLength; fileName = "rq03_descricao.png"; yLabel = "Tamanho da Descrição"; break;
                    case "RQ04": selector = p => p.Comments; fileName = "rq04_interacoes.png"; yLabel = "Total de Comentários"; break;
                }

                if (selector != null)
                {
                    var mergedVals = merged.Select(selector).ToArray();
                    var closedVals = closed.Select(selector).ToArray();

                    if (mergedVals.Length > 0 && closedVals.Length > 0)
                    {
                        double medMerged = Statistics.Median(mergedVals);
                        double medClosed = Statistics.Median(closedVals);

                        var barPlot = plt.Add.Bars(new double[] { medMerged, medClosed });
                        barPlot.Bars[0].FillColor = Colors.Blue.WithAlpha(0.85);
                        barPlot.Bars[1].FillColor = Colors.Red.WithAlpha(0.75);

                        // Add Interquartile Range (IQR) as Error Lines
                        double q1m = Statistics.QuantileCustom(mergedVals, 0.25, QuantileDefinition.R8);
                        double q3m = Statistics.QuantileCustom(mergedVals, 0.75, QuantileDefinition.R8);
                        var err1 = plt.Add.Line(0, q1m, 0, q3m);
                        err1.LineStyle.Color = ScottPlot.Colors.Black;
                        err1.LineStyle.Width = 2;

                        double q1c = Statistics.QuantileCustom(closedVals, 0.25, QuantileDefinition.R8);
                        double q3c = Statistics.QuantileCustom(closedVals, 0.75, QuantileDefinition.R8);
                        var err2 = plt.Add.Line(1, q1c, 1, q3c);
                        err2.LineStyle.Color = ScottPlot.Colors.Black;
                        err2.LineStyle.Width = 2;

                        // Add Caps to error lines
                        var err1Top = plt.Add.Line(-0.05, q3m, 0.05, q3m); err1Top.LineStyle.Color = ScottPlot.Colors.Black; err1Top.LineStyle.Width = 2;
                        var err1Bot = plt.Add.Line(-0.05, q1m, 0.05, q1m); err1Bot.LineStyle.Color = ScottPlot.Colors.Black; err1Bot.LineStyle.Width = 2;

                        var err2Top = plt.Add.Line(0.95, q3c, 1.05, q3c); err2Top.LineStyle.Color = ScottPlot.Colors.Black; err2Top.LineStyle.Width = 2;
                        var err2Bot = plt.Add.Line(0.95, q1c, 1.05, q1c); err2Bot.LineStyle.Color = ScottPlot.Colors.Black; err2Bot.LineStyle.Width = 2;

                        // Basic config
                        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                            new double[] { 0, 1 },
                            new string[] { "MERGED", "CLOSED" });

                        // Limita Y: mostra o IQR completo com pequena margem, sem o espaço vazio dos outliers extremos
                        double q3Max = System.Math.Max(q3m, q3c);
                        double medMax = System.Math.Max(medMerged, medClosed);
                        double maxLim = System.Math.Max(q3Max * 1.25, medMax * 4.0);
                        if (maxLim < 2) maxLim = 2;
                        plt.Axes.SetLimitsY(0, maxLim);

                        plt.YLabel($"{yLabel} (Mediana e IQR/Q1-Q3)");
                    }
                }
            }
            else
            {
                // Gráfico de barras por faixas (bins) da variável independente
                // Mostra a MEDIANA de revisões por faixa - muito mais legível que scatter
                // quando a variável dependente (review_count) é discreta e concentrada
                System.Func<PullRequest, double> xSelector = null;
                string xLabel = "";
                (double max, string label)[] bins = null;

                switch (result.RQCode)
                {
                    case "RQ05":
                        xSelector = p => p.FilesChanged;
                        fileName = "rq05_tamanho_revisoes.png";
                        xLabel = "Faixa de Tamanho (Arquivos Alterados)";
                        bins = new (double, string)[] {
                            (1, "1"), (3, "2-3"), (5, "4-5"), (10, "6-10"),
                            (20, "11-20"), (50, "21-50"), (double.MaxValue, "50+")
                        };
                        break;
                    case "RQ06":
                        xSelector = p => p.AnalysisTimeH;
                        fileName = "rq06_tempo_revisoes.png";
                        xLabel = "Faixa de Tempo de Análise (Horas)";
                        bins = new (double, string)[] {
                            (6, "≤ 6h"), (24, "6-24h"), (72, "1-3 dias"), (168, "3-7 dias"),
                            (720, "1-4 semanas"), (double.MaxValue, "> 1 mês")
                        };
                        break;
                    case "RQ07":
                        xSelector = p => p.BodyLength;
                        fileName = "rq07_descricao_revisoes.png";
                        xLabel = "Faixa de Tamanho da Descrição (Caracteres)";
                        bins = new (double, string)[] {
                            (0, "Vazia"), (200, "1-200"), (500, "201-500"), (1000, "501-1k"),
                            (2000, "1k-2k"), (5000, "2k-5k"), (double.MaxValue, "5k+")
                        };
                        break;
                    case "RQ08":
                        xSelector = p => p.Comments;
                        fileName = "rq08_interacoes_revisoes.png";
                        xLabel = "Faixa de Interações (Total de Comentários)";
                        bins = new (double, string)[] {
                            (1, "≤ 1"), (3, "2-3"), (5, "4-5"), (10, "6-10"),
                            (20, "11-20"), (double.MaxValue, "20+")
                        };
                        break;
                }

                if (xSelector != null && bins != null)
                {
                    // Agrupar PRs em faixas e calcular mediana e contagem em cada uma
                    var groups = new List<(string Label, double MedianReviews, int Count)>();
                    double prevMax = double.MinValue;

                    foreach (var (binMax, binLabel) in bins)
                    {
                        var max = binMax;
                        var lower = prevMax;
                        var bucket = prs
                            .Where(p =>
                            {
                                double v = xSelector(p);
                                return v > lower && v <= max;
                            })
                            .Select(p => (double)p.ReviewCount)
                            .ToArray();

                        double med = bucket.Length > 0 ? Statistics.Median(bucket) : 0;
                        groups.Add((binLabel, med, bucket.Length));
                        prevMax = max;
                    }

                    // Remover faixas vazias para não poluir o gráfico
                    var nonEmpty = groups.Where(g => g.Count > 0).ToList();

                    if (nonEmpty.Count > 0)
                    {
                        double[] medians = nonEmpty.Select(g => g.MedianReviews).ToArray();
                        string[] labels = nonEmpty.Select(g => $"{g.Label}\n(n={g.Count})").ToArray();
                        double[] positions = Enumerable.Range(0, nonEmpty.Count).Select(i => (double)i).ToArray();

                        var barPlot = plt.Add.Bars(medians);
                        for (int i = 0; i < barPlot.Bars.Count; i++)
                        {
                            barPlot.Bars[i].FillColor = Colors.Blue.WithAlpha(0.4 + 0.55 * ((double)i / System.Math.Max(1, barPlot.Bars.Count - 1)));
                        }

                        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, labels);

                        // Eixo Y com margem confortável acima da maior mediana
                        double yMaxData = medians.Max();
                        double yLim = System.Math.Max(yMaxData * 1.4, 2);
                        plt.Axes.SetLimitsY(0, yLim);

                        plt.XLabel(xLabel);
                        plt.YLabel("Mediana de Revisões");
                    }
                }
            }

            plt.Title(result.Title);

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string fullPath = Path.Combine(outputDir, fileName);
            plt.SavePng(fullPath, 1400, 750);

            result.ChartPath = fullPath;
            return fullPath;
        }
    }
}
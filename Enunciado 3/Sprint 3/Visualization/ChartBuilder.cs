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

                        var bars = plt.Add.Bars(new double[] { medMerged, medClosed });

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

                        // Limit Y axis using 95th Percentile to avoid extreme outliers squishing the bars
                        double p95Merged = Statistics.QuantileCustom(mergedVals, 0.95, QuantileDefinition.R8);
                        double p95Closed = Statistics.QuantileCustom(closedVals, 0.95, QuantileDefinition.R8);
                        double maxLim = System.Math.Max(p95Merged, p95Closed) * 1.5;
                        if (maxLim > 10)
                        {
                            plt.Axes.SetLimitsY(0, maxLim);
                        }

                        plt.YLabel($"{yLabel} (Mediana e IQR/Q1-Q3)");
                    }
                }
            }
            else
            {
                // Scatterplots
                System.Func<PullRequest, double> xSelector = null;
                string xLabel = "";
                switch (result.RQCode)
                {
                    case "RQ05": xSelector = p => p.FilesChanged; fileName = "rq05_tamanho_revisoes.png"; xLabel = "Tamanho (Arquivos)"; break;
                    case "RQ06": xSelector = p => p.AnalysisTimeH; fileName = "rq06_tempo_revisoes.png"; xLabel = "Tempo (Horas)"; break;
                    case "RQ07": xSelector = p => p.BodyLength; fileName = "rq07_descricao_revisoes.png"; xLabel = "Tamanho da Descrição"; break;
                    case "RQ08": xSelector = p => p.Comments; fileName = "rq08_interacoes_revisoes.png"; xLabel = "Total de Comentários"; break;
                }

                if (xSelector != null)
                {
                    var xs = prs.Select(xSelector).ToArray();
                    var ys = prs.Select(p => (double)p.ReviewCount).ToArray();
                    if (xs.Length > 0 && ys.Length > 0)
                    {
                        var scatter = plt.Add.Scatter(xs, ys);
                        scatter.LineStyle.Width = 0; // Turn off lines, so it is just points
                        scatter.MarkerStyle.Size = 5;

                        // Linear Regression trend line using MathNet.Numerics.LinearRegression.SimpleRegression.Fit
                        var fit = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(xs, ys);

                        double minX = xs.Min();
                        double maxX = xs.Max();

                        // Line formula: y = a + b*x (where fit.Item1 = intercept, fit.Item2 = slope)
                        double y1 = fit.Item1 + fit.Item2 * minX;
                        double y2 = fit.Item1 + fit.Item2 * maxX;

                        var line = plt.Add.Line(minX, y1, maxX, y2);
                        line.LineStyle.Width = 3;
                        line.LineStyle.Color = Colors.Red;

                        plt.XLabel(xLabel);
                        plt.YLabel("Quantidade de Revisões");
                    }
                }
            }

            plt.Title(result.Title);

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string fullPath = Path.Combine(outputDir, fileName);
            plt.SavePng(fullPath, 800, 500);

            result.ChartPath = fullPath;
            return fullPath;
        }
    }
}
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
                        var boxes = new List<ScottPlot.Box>();
                        var positionsList = new double[] { 0, 1 };
                        var labelsList = new string[] { $"MERGED\n(n={mergedVals.Length})", $"CLOSED\n(n={closedVals.Length})" };

                        // Process merged vals (Position 0)
                        double minM = mergedVals.Min();
                        double maxValM = mergedVals.Max();
                        double q1M = Statistics.QuantileCustom(mergedVals, 0.25, QuantileDefinition.R8);
                        double medianM = Statistics.Median(mergedVals);
                        double q3M = Statistics.QuantileCustom(mergedVals, 0.75, QuantileDefinition.R8);

                        double iqrM = q3M - q1M;
                        double lowerWhiskerM = mergedVals.Where(x => x >= q1M - 1.5 * iqrM).DefaultIfEmpty(minM).Min();
                        double upperWhiskerM = mergedVals.Where(x => x <= q3M + 1.5 * iqrM).DefaultIfEmpty(maxValM).Max();

                        boxes.Add(new ScottPlot.Box
                        {
                            Position = 0,
                            BoxMin = q1M,
                            BoxMax = q3M,
                            BoxMiddle = medianM,
                            WhiskerMin = lowerWhiskerM,
                            WhiskerMax = upperWhiskerM
                        });

                        // Process closed vals (Position 1)
                        double minC = closedVals.Min();
                        double maxValC = closedVals.Max();
                        double q1C = Statistics.QuantileCustom(closedVals, 0.25, QuantileDefinition.R8);
                        double medianC = Statistics.Median(closedVals);
                        double q3C = Statistics.QuantileCustom(closedVals, 0.75, QuantileDefinition.R8);

                        double iqrC = q3C - q1C;
                        double lowerWhiskerC = closedVals.Where(x => x >= q1C - 1.5 * iqrC).DefaultIfEmpty(minC).Min();
                        double upperWhiskerC = closedVals.Where(x => x <= q3C + 1.5 * iqrC).DefaultIfEmpty(maxValC).Max();

                        boxes.Add(new ScottPlot.Box
                        {
                            Position = 1,
                            BoxMin = q1C,
                            BoxMax = q3C,
                            BoxMiddle = medianC,
                            WhiskerMin = lowerWhiskerC,
                            WhiskerMax = upperWhiskerC
                        });

                        var bp = plt.Add.Boxes(boxes);

                        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positionsList, labelsList);

                        double yMaxData = boxes.Max(b => b.WhiskerMax ?? b.BoxMax);
                        double yLim = System.Math.Max(yMaxData * 1.1, 2);
                        plt.Axes.SetLimitsY(0, yLim);

                        plt.Axes.Bottom.TickLabelStyle.FontSize = 26;
                        plt.Axes.Left.TickLabelStyle.FontSize = 20;
                        plt.YLabel($"{yLabel} (Boxplot)");
                        plt.Axes.Left.Label.FontSize = 32;
                    }
                }
            }
            else
            {
                // Scatter plot bruto com dados exatos (sem binning) e Regressão Linear
                System.Func<PullRequest, double> xSelector = null;
                string xLabel = "";

                switch (result.RQCode)
                {
                    case "RQ05":
                        xSelector = p => p.FilesChanged;
                        fileName = "rq05_tamanho_revisoes.png";
                        xLabel = "Tamanho (Arquivos Alterados)";
                        break;
                    case "RQ06":
                        xSelector = p => p.AnalysisTimeH;
                        fileName = "rq06_tempo_revisoes.png";
                        xLabel = "Tempo de Análise (Horas)";
                        break;
                    case "RQ07":
                        xSelector = p => p.BodyLength;
                        fileName = "rq07_descricao_revisoes.png";
                        xLabel = "Tamanho da Descrição (Caracteres)";
                        break;
                    case "RQ08":
                        xSelector = p => p.Comments;
                        fileName = "rq08_interacoes_revisoes.png";
                        xLabel = "Total de Interações (Comentários)";
                        break;
                }

                if (xSelector != null)
                {
                    var xData = prs.Select(xSelector).ToArray();
                    var yData = prs.Select(p => (double)p.ReviewCount).ToArray();

                    if (xData.Length > 0 && yData.Length > 0)
                    {
                        // Plot scatter (dados brutos) com alta transparência
                        var sp = plt.Add.Scatter(xData, yData);
                        sp.LineStyle.Width = 0; // Desabilita as linhas que conectam os pontos e gera apenas um scatter
                        sp.MarkerStyle.Shape = ScottPlot.MarkerShape.FilledCircle;
                        sp.MarkerStyle.Size = 4;
                        sp.MarkerStyle.FillColor = Colors.Blue.WithAlpha(0.15); // Transparência de 15% para suportar overplotting
                        sp.MarkerStyle.LineColor = Colors.Transparent;

                        // Linear Regression
                        ScottPlot.Statistics.LinearRegression reg = new(xData, yData);

                        // Obter limites reais (min e max) de X
                        double xMin = xData.Min();
                        double xMax = xData.Max();

                        // Calcular Ys para a linha de tendência com base na regressão
                        double yOut1 = reg.GetValue(xMin);
                        double yOut2 = reg.GetValue(xMax);

                        // Plotar a trendline sobre os dados brutos com alta densidade
                        var lineInfo = plt.Add.Line(xMin, yOut1, xMax, yOut2);
                        lineInfo.LineStyle.Color = Colors.Red;
                        lineInfo.LineStyle.Width = 3;

                        // Configurações e Fontes 
                        plt.Axes.Bottom.TickLabelStyle.FontSize = 18;
                        plt.Axes.Left.TickLabelStyle.FontSize = 20;

                        plt.XLabel(xLabel);
                        plt.Axes.Bottom.Label.FontSize = 30;

                        plt.YLabel("Revisões (Scatter)");
                        plt.Axes.Left.Label.FontSize = 30;

                        // Limitadores dinâmicos Y baseados nos quartis e regressão pra não quebrar a visualização,
                        // cortando outliers vazios ultra extremos no eixo Y.
                        double maxLimY = MathNet.Numerics.Statistics.Statistics.QuantileCustom(yData, 0.99, MathNet.Numerics.Statistics.QuantileDefinition.R8) * 1.5;
                        if (maxLimY < 3) maxLimY = 3;
                        plt.Axes.SetLimitsY(-0.5, maxLimY);

                        double maxLimX = MathNet.Numerics.Statistics.Statistics.QuantileCustom(xData, 0.95, MathNet.Numerics.Statistics.QuantileDefinition.R8) * 2;
                        if (maxLimX < 2) maxLimX = 2;
                        plt.Axes.SetLimitsX(-maxLimX * 0.05, maxLimX);
                    }
                }
            }

            plt.Title(result.Title);
            plt.Axes.Title.Label.FontSize = 34;

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string fullPath = Path.Combine(outputDir, fileName);
            plt.SavePng(fullPath, 1400, 750);

            result.ChartPath = fullPath;
            return fullPath;
        }
    }
}
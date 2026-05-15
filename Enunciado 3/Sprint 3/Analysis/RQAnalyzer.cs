using System;
using System.Collections.Generic;
using System.Linq;
using Lab03S03.Models;

namespace Lab03S03.Analysis
{
    public static class RQAnalyzer
    {
        public static AnalysisResult Analyze(List<PullRequest> prs, string rqCode)
        {
            var result = new AnalysisResult { RQCode = rqCode };

            var merged = prs.Where(p => p.Status.Equals("MERGED", StringComparison.OrdinalIgnoreCase)).ToList();
            var closed = prs.Where(p => p.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase)).ToList();

            // Prepare independent variable for RQs 05-08 (review_count)
            double[] allReviewCounts = prs.Select(p => (double)p.ReviewCount).ToArray();

            switch (rqCode)
            {
                case "RQ01":
                    result.Title = "Status de aceite vs. Tamanho do PR";
                    result.Hypothesis = "PRs menores (menos arquivos/linhas) têm maior chance de serem aceitos (MERGED).";
                    result.PrimaryLabel = "Arquivos Alterados";
                    result.MedianMerged = StatisticsHelper.Median(merged.Select(p => (double)p.FilesChanged));
                    result.MedianClosed = StatisticsHelper.Median(closed.Select(p => (double)p.FilesChanged));
                    AnalyzeStatusCorrelation(prs, p => (double)p.FilesChanged, result);
                    // Métrica secundária de Tamanho: total de linhas adicionadas + removidas
                    result.HasSecondaryMetric = true;
                    result.SecondaryLabel = "Total de Linhas (adições + remoções)";
                    result.SecondaryMedianMerged = StatisticsHelper.Median(merged.Select(p => (double)(p.LinesAdded + p.LinesRemoved)));
                    result.SecondaryMedianClosed = StatisticsHelper.Median(closed.Select(p => (double)(p.LinesAdded + p.LinesRemoved)));
                    AnalyzeStatusCorrelationSecondary(prs, p => (double)(p.LinesAdded + p.LinesRemoved), result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística de que o tamanho influencie o aceite." :
                        (result.SpearmanRho < 0 ? "Hipótese confirmada: PRs menores têm maior chance de serem aceitos, a correlação é negativa." : "Hipótese refutada: O coeficiente é positivo indicando que PRs maiores tendem a ser aceitos.");
                    break;

                case "RQ02":
                    result.Title = "Status de aceite vs. Tempo de análise";
                    result.Hypothesis = "PRs que levam mais tempo em análise tendem a ser rejeitados (CLOSED).";
                    result.PrimaryLabel = "Tempo de Análise (Horas)";
                    result.MedianMerged = StatisticsHelper.Median(merged.Select(p => p.AnalysisTimeH));
                    result.MedianClosed = StatisticsHelper.Median(closed.Select(p => p.AnalysisTimeH));
                    AnalyzeStatusCorrelation(prs, p => p.AnalysisTimeH, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística." :
                        (result.SpearmanRho < 0 ? "Hipótese confirmada: Tempo longo se correlaciona com rejeição (coeficiente negativo)." : "Hipótese refutada.");
                    break;

                case "RQ03":
                    result.Title = "Status de aceite vs. Descrição";
                    result.Hypothesis = "PRs com descrições mais detalhadas têm maior chance de serem aceitos.";
                    result.PrimaryLabel = "Comprimento da Descrição (Caracteres)";
                    result.MedianMerged = StatisticsHelper.Median(merged.Select(p => (double)p.BodyLength));
                    result.MedianClosed = StatisticsHelper.Median(closed.Select(p => (double)p.BodyLength));
                    AnalyzeStatusCorrelation(prs, p => (double)p.BodyLength, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Nenhuma evidência estatística de que a descrição influencia o aceite." :
                        (result.SpearmanRho > 0 ? "Hipótese confirmada: Maior descrição correlaciona com aceite." : "Hipótese refutada.");
                    break;

                case "RQ04":
                    result.Title = "Status de aceite vs. Interações";
                    result.Hypothesis = "PRs com mais interações (comentários/participantes) tendem a ser aceitos.";
                    result.PrimaryLabel = "Comentários";
                    result.MedianMerged = StatisticsHelper.Median(merged.Select(p => (double)p.Comments));
                    result.MedianClosed = StatisticsHelper.Median(closed.Select(p => (double)p.Comments));
                    AnalyzeStatusCorrelation(prs, p => (double)p.Comments, result);
                    // Métrica secundária de Interações: número de participantes
                    result.HasSecondaryMetric = true;
                    result.SecondaryLabel = "Participantes";
                    result.SecondaryMedianMerged = StatisticsHelper.Median(merged.Select(p => (double)p.Participants));
                    result.SecondaryMedianClosed = StatisticsHelper.Median(closed.Select(p => (double)p.Participants));
                    AnalyzeStatusCorrelationSecondary(prs, p => (double)p.Participants, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística." :
                        (result.SpearmanRho > 0 ? "Hipótese confirmada: Mais interações correlacionam com aceite." : "Hipótese refutada: O coeficiente negativo indica que PRs com MAIS interações tendem a ser REJEITADOS. Isso ocorre pois PRs polêmicos ou com problemas severos de implementação naturalmente geram extensos debates antes de serem finalmente fechados em vez de mergeados rapidamente.");
                    break;

                case "RQ05":
                    result.Title = "Revisões vs. Tamanho do PR";
                    result.Hypothesis = "PRs maiores geram mais revisões, pois há mais código para revisar.";
                    result.PrimaryLabel = "Arquivos Alterados";
                    result.MedianMerged = StatisticsHelper.Median(prs.Select(p => (double)p.FilesChanged));
                    result.MedianClosed = StatisticsHelper.Median(allReviewCounts);
                    AnalyzeCorrelation(prs, p => (double)p.FilesChanged, p => (double)p.ReviewCount, result);
                    // Métrica secundária de Tamanho: total de linhas adicionadas + removidas
                    result.HasSecondaryMetric = true;
                    result.SecondaryLabel = "Total de Linhas (adições + remoções)";
                    result.SecondaryMedianMerged = StatisticsHelper.Median(prs.Select(p => (double)(p.LinesAdded + p.LinesRemoved)));
                    result.SecondaryMedianClosed = result.MedianClosed;
                    AnalyzeCorrelationSecondary(prs, p => (double)(p.LinesAdded + p.LinesRemoved), p => (double)p.ReviewCount, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística." :
                        (result.SpearmanRho > 0 ? "Hipótese confirmada: Maior tamanho correlaciona com mais revisões." : "Hipótese refutada.");
                    break;

                case "RQ06":
                    result.Title = "Revisões vs. Tempo de análise";
                    result.Hypothesis = "PRs com maior tempo de análise acumulam mais revisões.";
                    result.PrimaryLabel = "Tempo de Análise (Horas)";
                    result.MedianMerged = StatisticsHelper.Median(prs.Select(p => p.AnalysisTimeH));
                    result.MedianClosed = StatisticsHelper.Median(allReviewCounts);
                    AnalyzeCorrelation(prs, p => p.AnalysisTimeH, p => (double)p.ReviewCount, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística." :
                        (result.SpearmanRho > 0 ? "Hipótese confirmada: Maior tempo correlaciona com mais revisões." : "Hipótese refutada.");
                    break;

                case "RQ07":
                    result.Title = "Revisões vs. Descrição";
                    result.Hypothesis = "Descrições mais longas correlacionam com mais revisões (mais contexto → mais discussão).";
                    result.PrimaryLabel = "Comprimento da Descrição (Caracteres)";
                    result.MedianMerged = StatisticsHelper.Median(prs.Select(p => (double)p.BodyLength));
                    result.MedianClosed = StatisticsHelper.Median(allReviewCounts);
                    AnalyzeCorrelation(prs, p => (double)p.BodyLength, p => (double)p.ReviewCount, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística." :
                        (result.SpearmanRho > 0 ? "Hipótese confirmada: Maior descrição correlaciona com mais revisões." : "Hipótese refutada.");
                    break;

                case "RQ08":
                    result.Title = "Revisões vs. Interações";
                    result.Hypothesis = "PRs com mais participantes e comentários naturalmente têm mais revisões.";
                    result.PrimaryLabel = "Comentários";
                    result.MedianMerged = StatisticsHelper.Median(prs.Select(p => (double)p.Comments));
                    result.MedianClosed = StatisticsHelper.Median(allReviewCounts);
                    AnalyzeCorrelation(prs, p => (double)p.Comments, p => (double)p.ReviewCount, result);
                    // Métrica secundária de Interações: número de participantes
                    result.HasSecondaryMetric = true;
                    result.SecondaryLabel = "Participantes";
                    result.SecondaryMedianMerged = StatisticsHelper.Median(prs.Select(p => (double)p.Participants));
                    result.SecondaryMedianClosed = result.MedianClosed;
                    AnalyzeCorrelationSecondary(prs, p => (double)p.Participants, p => (double)p.ReviewCount, result);
                    result.Discussion = result.PValue > 0.05 ? "Hipótese refutada: Não há significância estatística." :
                        (result.SpearmanRho > 0 ? "Hipótese confirmada: Mais interações correlacionam com mais revisões." : "Hipótese refutada.");
                    break;
            }

            return result;
        }

        private static void AnalyzeStatusCorrelation(List<PullRequest> prs, Func<PullRequest, double> independentVariableSelector, AnalysisResult result)
        {
            var x = prs.Select(independentVariableSelector).ToArray();
            var y = prs.Select(p => p.Status.Equals("MERGED", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0).ToArray();

            var (rho, p) = StatisticsHelper.Spearman(x, y);
            result.SpearmanRho = rho;
            result.PValue = p;
            result.Interpretation = StatisticsHelper.InterpretRho(rho);
        }

        private static void AnalyzeStatusCorrelationSecondary(List<PullRequest> prs, Func<PullRequest, double> selector, AnalysisResult result)
        {
            var x = prs.Select(selector).ToArray();
            var y = prs.Select(p => p.Status.Equals("MERGED", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0).ToArray();

            var (rho, p) = StatisticsHelper.Spearman(x, y);
            result.SecondarySpearmanRho = rho;
            result.SecondaryPValue = p;
            result.SecondaryInterpretation = StatisticsHelper.InterpretRho(rho);
        }

        private static void AnalyzeCorrelation(List<PullRequest> prs, Func<PullRequest, double> independentVariableSelector, Func<PullRequest, double> dependentVariableSelector, AnalysisResult result)
        {
            var x = prs.Select(independentVariableSelector).ToArray();
            var y = prs.Select(dependentVariableSelector).ToArray();

            var (rho, p) = StatisticsHelper.Spearman(x, y);
            result.SpearmanRho = rho;
            result.PValue = p;
            result.Interpretation = StatisticsHelper.InterpretRho(rho);
        }

        private static void AnalyzeCorrelationSecondary(List<PullRequest> prs, Func<PullRequest, double> xSelector, Func<PullRequest, double> ySelector, AnalysisResult result)
        {
            var x = prs.Select(xSelector).ToArray();
            var y = prs.Select(ySelector).ToArray();

            var (rho, p) = StatisticsHelper.Spearman(x, y);
            result.SecondarySpearmanRho = rho;
            result.SecondaryPValue = p;
            result.SecondaryInterpretation = StatisticsHelper.InterpretRho(rho);
        }
    }
}
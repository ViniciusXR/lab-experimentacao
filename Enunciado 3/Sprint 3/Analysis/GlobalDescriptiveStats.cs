using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Lab03S03.Models;

namespace Lab03S03.Analysis
{
    public sealed class GlobalMetricSummary
    {
        public string Label { get; init; } = "";
        public int N { get; init; }
        public double Mean { get; init; }
        public double Median { get; init; }
        public double StdDev { get; init; }
    }

    public static class GlobalDescriptiveStats
    {
        public static List<GlobalMetricSummary> SummarizeAll(List<PullRequest> prs)
        {
            int n = prs.Count;
            if (n == 0)
                return new List<GlobalMetricSummary>();

            return new List<GlobalMetricSummary>
            {
                Mk("Arquivos alterados", n, prs.Select(p => (double)p.FilesChanged)),
                Mk("Total de linhas (adições + remoções)", n, prs.Select(p => (double)(p.LinesAdded + p.LinesRemoved))),
                Mk("Tempo de análise (horas)", n, prs.Select(p => p.AnalysisTimeH)),
                Mk("Comprimento da descrição (caracteres)", n, prs.Select(p => (double)p.BodyLength)),
                Mk("Comentários", n, prs.Select(p => (double)p.Comments)),
                Mk("Participantes", n, prs.Select(p => (double)p.Participants)),
                Mk("Número de revisões", n, prs.Select(p => (double)p.ReviewCount))
            };
        }

        private static GlobalMetricSummary Mk(string label, int n, IEnumerable<double> seq)
        {
            var arr = seq.ToArray();
            double mean = arr.Average();
            double median = MedianLocal(arr);
            double std = StdDevSample(arr);
            return new GlobalMetricSummary
            {
                Label = label,
                N = n,
                Mean = mean,
                Median = median,
                StdDev = std
            };
        }

        private static double MedianLocal(double[] arr)
        {
            if (arr.Length == 0) return 0;
            var s = (double[])arr.Clone();
            Array.Sort(s);
            int m = s.Length / 2;
            if (s.Length % 2 == 0)
                return (s[m - 1] + s[m]) / 2.0;
            return s[m];
        }

        private static double StdDevSample(double[] arr)
        {
            if (arr.Length < 2) return 0;
            double mean = arr.Average();
            double sumSq = arr.Sum(x => (x - mean) * (x - mean));
            return Math.Sqrt(sumSq / (arr.Length - 1));
        }

        /// <summary>
        /// Parágrafo curto estilo Lab02 (5.1 interpretação processo).
        /// </summary>
        public static string BuildInterpretation(List<GlobalMetricSummary> rows, int totalPrs)
        {
            var pt = CultureInfo.GetCultureInfo("pt-BR");
            GlobalMetricSummary? Find(string contains) =>
                rows.FirstOrDefault(r => r.Label.Contains(contains, StringComparison.OrdinalIgnoreCase));

            var linhas = Find("linhas");
            var tempo = Find("Tempo");
            var rev = Find("revisões");
            var com = Find("Comentários");

            var sb = new StringBuilder();
            sb.Append("Sumarização global (média, mediana e desvio padrão) sobre todos os PRs do dataset (n = ");
            sb.Append(totalPrs.ToString("N0", pt));
            sb.Append("). ");

            if (tempo != null)
            {
                double cvTempo = tempo.Mean > 1e-9 ? tempo.StdDev / tempo.Mean : 0;
                sb.Append("O tempo de análise apresenta média ");
                sb.Append(tempo.Mean.ToString("N2", pt));
                sb.Append(" h e mediana ");
                sb.Append(tempo.Median.ToString("N2", pt));
                sb.Append(" h (desvio ");
                sb.Append(tempo.StdDev.ToString("N2", pt));
                sb.Append(" h; CV ≈ ");
                sb.Append(cvTempo.ToString("N2", pt));
                sb.Append("), compatível com distribuição fortemente assimétrica e cauda longa — poucos PRs tramitam muito tempo. ");
            }

            if (linhas != null)
            {
                sb.Append("O total de linhas (adições + remoções) tem mediana bem inferior à média (");
                sb.Append(linhas.Median.ToString("N2", pt));
                sb.Append(" vs ");
                sb.Append(linhas.Mean.ToString("N2", pt));
                sb.Append("), típico de patches pequenos com exceções muito grandes. ");
            }

            if (rev != null && com != null)
            {
                sb.Append("O número de revisões concentra-se em valores baixos (mediana ");
                sb.Append(rev.Median.ToString("N2", pt));
                sb.Append("), enquanto comentários e participantes mostram dispersão moderada (mediana de comentários ");
                sb.Append(com.Median.ToString("N2", pt));
                sb.Append("), refletindo a maioria de PRs enxutos e um subconjunto com discussão intensa.");
            }

            return sb.ToString();
        }
    }
}

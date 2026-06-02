using Enunciado4.Sprint1.Models;
using ScottPlot;

namespace Enunciado4.Sprint1.Visualization;

public static class Rq3Charts
{
    public static IReadOnlyList<string> GenerateAll(Lab04Dataset data, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        return
        [
            SaveDispersaoQuadrantes(data, Path.Combine(outputDir, "01_dispersao_quadrantes.png")),
            SaveScoreRisco(data, Path.Combine(outputDir, "02_score_risco_top30.png")),
            SaveResumoQuadrantes(data, Path.Combine(outputDir, "03_resumo_quadrantes.png"))
        ];
    }

    private static string SaveDispersaoQuadrantes(Lab04Dataset data, string path)
    {
        var plt = new Plot();
        plt.Title("Frequência × Severidade por CWE — quadrantes de risco");
        plt.XLabel("log10(Nº de CVEs)");
        plt.YLabel("CVSS médio (0–10)");

        foreach (var grupo in data.Rq3Dispersao.GroupBy(d => d.Quadrante))
        {
            var xs = grupo.Select(g => Math.Log10(Math.Max(g.CvesCount, 1))).ToArray();
            var ys = grupo.Select(g => g.CvssMedio).ToArray();
            var scatter = plt.Add.Scatter(xs, ys);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.Color = ChartPalette.GetQuadrante(grupo.Key);
            scatter.LegendText = grupo.Key;
        }

        plt.ShowLegend(Alignment.UpperRight);
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string SaveScoreRisco(Lab04Dataset data, string path)
    {
        var top = data.Rq3ScoreRisco.OrderBy(r => r.RankRisco).Take(15).Reverse().ToList();
        var plt = new Plot();
        plt.Title("Score de risco composto — Top 15 (frequência × severidade)");
        plt.XLabel("Score (0–1)");

        var pos = Enumerable.Range(0, top.Count).Select(i => (double)i).ToArray();
        var vals = top.Select(t => t.ScoreRisco).ToArray();
        var bars = plt.Add.Bars(pos, vals);
        bars.Horizontal = true;

        var faixaCores = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["Critico"] = Color.FromHex("#C0392B"),
            ["Alto"] = Color.FromHex("#E8702A"),
            ["Moderado"] = Color.FromHex("#2E86C1"),
            ["Baixo"] = Color.FromHex("#95A5A6")
        };
        for (var i = 0; i < top.Count; i++)
            bars.Bars[i].FillColor = faixaCores.GetValueOrDefault(top[i].FaixaRisco, Colors.Gray);

        var labels = top.Select(t => TruncarLabel(t.CweLabel)).ToArray();
        plt.Axes.Left.SetTicks(pos, labels);
        plt.Axes.Left.Min = -0.5;
        plt.Axes.Left.Max = top.Count - 0.5;
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string SaveResumoQuadrantes(Lab04Dataset data, string path)
    {
        var plt = new Plot();
        plt.Title("Distribuição das 749 CWEs por quadrante de risco");

        var valores = data.Rq3Quadrantes.Select(q => (double)q.NCwes).ToArray();
        var labels = data.Rq3Quadrantes.Select(q => $"{q.Quadrante}\n{q.Pct:F1}%").ToArray();
        var pie = plt.Add.Pie(valores);
        pie.ExplodeFraction = 0.04;
        for (var i = 0; i < pie.Slices.Count; i++)
        {
            pie.Slices[i].Label = labels[i];
            pie.Slices[i].FillColor = ChartPalette.GetQuadrante(data.Rq3Quadrantes[i].Quadrante);
        }

        plt.HideGrid();
        plt.Axes.Frameless();
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string TruncarLabel(string label)
    {
        var curto = label.Contains(" - ") ? label.Split(" - ", 2)[1] : label;
        return curto.Length <= 24 ? curto : curto[..21] + "…";
    }
}

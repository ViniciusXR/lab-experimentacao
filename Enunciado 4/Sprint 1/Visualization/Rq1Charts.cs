using Enunciado4.Sprint1.Models;
using ScottPlot;

namespace Enunciado4.Sprint1.Visualization;

public static class Rq1Charts
{
    public static IReadOnlyList<string> GenerateAll(Lab04Dataset data, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        return
        [
            SavePareto(data, Path.Combine(outputDir, "01_pareto_top30.png")),
            SaveTendenciaTop10(data, Path.Combine(outputDir, "02_tendencia_top10.png")),
            SaveCurvaLorenz(data, Path.Combine(outputDir, "03_curva_lorenz.png")),
            SavePersistencia(data, Path.Combine(outputDir, "04_persistencia_jaccard.png"))
        ];
    }

    private static string SavePareto(Lab04Dataset data, string path)
    {
        var top = data.Rq1Frequencia.Where(r => r.Top30).OrderBy(r => r.RankFreq).ToList();
        var plt = new Plot();
        plt.Title("Pareto de frequência: poucas CWEs concentram quase tudo");
        plt.XLabel("CWE (Top 30 por frequência)");
        plt.YLabel("Nº de CVEs");

        var pos = Enumerable.Range(0, top.Count).Select(i => (double)i).ToArray();
        var counts = top.Select(t => (double)t.CvesCount).ToArray();
        var bars = plt.Add.Bars(pos, counts);
        bars.Color = Color.FromHex("#1565C0");

        var pct = top.Select(t => t.PctAcumulado).ToArray();
        var line = plt.Add.Scatter(pos, pct);
        line.Color = Color.FromHex("#C0392B");
        line.LineWidth = 2;
        line.MarkerSize = 0;
        line.LegendText = "% acumulada";
        line.Axes.YAxis = plt.Axes.AddRightAxis();
        plt.Axes.Right.Label.Text = "% acumulada";

        var labels = top.Select(t => Truncar(t.CweNomeCurto, 18)).ToArray();
        plt.Axes.Bottom.SetTicks(pos, labels);
        plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
        plt.ShowLegend();
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string SaveTendenciaTop10(Lab04Dataset data, string path)
    {
        var plt = new Plot();
        plt.Title("Evolução anual das 10 CWEs mais frequentes");
        plt.XLabel("Ano");
        plt.YLabel("Nº de CVEs no ano");

        var cwes = data.Rq1Tendencia.Select(t => t.CweNomeCurto).Distinct().OrderBy(c => c).ToList();
        Color[] cores =
        [
            Color.FromHex("#1565C0"), Color.FromHex("#C0392B"), Color.FromHex("#2E7D32"),
            Color.FromHex("#EF6C00"), Color.FromHex("#6A1B9A"), Color.FromHex("#00838F"),
            Color.FromHex("#AD1457"), Color.FromHex("#558B2F"), Color.FromHex("#4527A0"),
            Color.FromHex("#F9A825")
        ];
        for (var i = 0; i < cwes.Count; i++)
        {
            var serie = data.Rq1Tendencia.Where(t => t.CweNomeCurto == cwes[i]).OrderBy(t => t.Ano).ToList();
            var xs = serie.Select(s => (double)s.Ano).ToArray();
            var ys = serie.Select(s => (double)s.CvesCount).ToArray();
            var line = plt.Add.Scatter(xs, ys);
            line.Color = cores[i % cores.Length];
            line.LineWidth = 2;
            line.MarkerSize = 4;
            line.LegendText = Truncar(cwes[i], 22);
        }

        plt.ShowLegend(Alignment.LowerRight);
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string SaveCurvaLorenz(Lab04Dataset data, string path)
    {
        var plt = new Plot();
        plt.Title("Curva de Lorenz — concentração de CVEs (Gini = 0,92)");
        plt.XLabel("% acumulado de CWEs");
        plt.YLabel("% acumulado de CVEs");

        var xs = data.Rq1Lorenz.Select(r => r.PctCwesAcumulado).ToArray();
        var ys = data.Rq1Lorenz.Select(r => r.PctCvesAcumulado).ToArray();
        var eq = data.Rq1Lorenz.Select(r => r.LinhaIgualdade).ToArray();

        var lorenz = plt.Add.Scatter(xs, ys);
        lorenz.Color = Color.FromHex("#1565C0");
        lorenz.LineWidth = 2;
        lorenz.MarkerSize = 0;
        lorenz.LegendText = "Curva observada";

        var igualdade = plt.Add.Scatter(xs, eq);
        igualdade.Color = Color.FromHex("#95A5A6");
        igualdade.LineWidth = 2;
        igualdade.MarkerSize = 0;
        igualdade.LegendText = "Igualdade perfeita";

        plt.ShowLegend();
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string SavePersistencia(Lab04Dataset data, string path)
    {
        var plt = new Plot();
        plt.Title("Estabilidade do Top-20 entre anos (índice de Jaccard)");
        plt.XLabel("Transição anual");
        plt.YLabel("Jaccard (0–1)");

        var items = data.Rq1Persistencia.ToList();
        var pos = Enumerable.Range(0, items.Count).Select(i => (double)i).ToArray();
        var vals = items.Select(i => i.Jaccard).ToArray();
        var line = plt.Add.Scatter(pos, vals);
        line.Color = Color.FromHex("#2E86C1");
        line.LineWidth = 2;
        line.MarkerSize = 4;

        var labels = items.Select(i => i.Transicao).ToArray();
        plt.Axes.Bottom.SetTicks(pos, labels);
        plt.Axes.Bottom.TickLabelStyle.Rotation = 60;
        plt.Axes.Left.Min = 0;
        plt.Axes.Left.Max = 1;
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string Truncar(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}

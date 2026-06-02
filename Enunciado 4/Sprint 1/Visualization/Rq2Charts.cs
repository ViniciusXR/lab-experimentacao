using Enunciado4.Sprint1.Models;
using ScottPlot;

namespace Enunciado4.Sprint1.Visualization;

public static class Rq2Charts
{
    public static IReadOnlyList<string> GenerateAll(Lab04Dataset data, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        return
        [
            SaveTopCvssMedio(data, Path.Combine(outputDir, "01_top30_cvss_medio.png")),
            SaveTopHighCritical(data, Path.Combine(outputDir, "02_top30_high_critical.png"))
        ];
    }

    private static string SaveTopCvssMedio(Lab04Dataset data, string path)
    {
        var top = data.Rq2Severidade
            .Where(r => r.Top30Cvss && r.CvesCount >= 30)
            .OrderByDescending(r => r.CvssMedio)
            .Take(20)
            .Reverse()
            .ToList();

        return SaveHorizontalBars(
            path,
            "Top 20 CWEs por CVSS médio (n ≥ 30 CVEs)",
            "CVSS médio (0–10)",
            top.Select(t => TruncarLabel(t.CweLabel)).ToArray(),
            top.Select(t => t.CvssMedio).ToArray(),
            Color.FromHex("#7E0023"));
    }

    private static string SaveTopHighCritical(Lab04Dataset data, string path)
    {
        var top = data.Rq2Severidade
            .Where(r => r.CvesCount >= 30)
            .OrderByDescending(r => r.HighCriticalPct)
            .Take(20)
            .Reverse()
            .ToList();

        return SaveHorizontalBars(
            path,
            "Top 20 CWEs por % HIGH/CRITICAL (n ≥ 30 CVEs)",
            "% HIGH/CRITICAL",
            top.Select(t => TruncarLabel(t.CweLabel)).ToArray(),
            top.Select(t => t.HighCriticalPct).ToArray(),
            Color.FromHex("#E8702A"));
    }

    private static string SaveHorizontalBars(
        string path, string title, string xLabel, string[] labels, double[] values, Color color)
    {
        var plt = new Plot();
        plt.Title(title);
        plt.XLabel(xLabel);

        var pos = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();
        var bars = plt.Add.Bars(pos, values);
        bars.Color = color;
        bars.Horizontal = true;

        plt.Axes.Left.SetTicks(pos, labels);
        plt.Axes.Left.Min = -0.5;
        plt.Axes.Left.Max = values.Length - 0.5;
        ChartPalette.Save(plt, path);
        return path;
    }

    private static string TruncarLabel(string label)
    {
        var curto = label.Contains(" - ") ? label.Split(" - ", 2)[1] : label;
        return curto.Length <= 28 ? curto : curto[..25] + "…";
    }
}

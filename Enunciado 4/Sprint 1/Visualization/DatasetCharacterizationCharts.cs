using Enunciado4.Sprint1.Models;
using ScottPlot;
using ScottPlot.Colormaps;
using SkiaSharp;

namespace Enunciado4.Sprint1.Visualization;

public static class DatasetCharacterizationCharts
{
    private static readonly Dictionary<string, Color> SeveridadeCores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LOW"] = Color.FromHex("#2E7D32"),
        ["MEDIUM"] = Color.FromHex("#F9A825"),
        ["HIGH"] = Color.FromHex("#EF6C00"),
        ["CRITICAL"] = Color.FromHex("#B71C1C")
    };

    public static IReadOnlyList<string> GenerateAll(CharacterizationData data, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var paths = new List<string>
        {
            SaveCvesPorAno(data, Path.Combine(outputDir, "01_cves_por_ano.png")),
            SaveSeveridadeDonut(data, Path.Combine(outputDir, "02_severidade_donut.png")),
            SaveSeveridadePorAno(data, Path.Combine(outputDir, "03_severidade_por_ano_100pct.png")),
            SaveDistribuicaoCvss(data, Path.Combine(outputDir, "04_distribuicao_cvss.png"))
        };
        return paths;
    }

    private static string SaveCvesPorAno(CharacterizationData data, string path)
    {
        var plt = new Plot();
        plt.Title("Volume de CVEs por ano (1999–2025)");
        plt.XLabel("Ano");
        plt.YLabel("Nº de CVEs distintos");

        var anos = data.PorAno.Select(r => (double)r.Ano).ToArray();
        var valores = data.PorAno.Select(r => (double)r.CvesDistintos).ToArray();
        var bars = plt.Add.Bars(anos, valores);
        bars.Color = Color.FromHex("#1565C0");

        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(5);
        plt.Axes.Margins(bottom: 0, top: 0.15);
        SavePlot(plt, path);
        return path;
    }

    private static string SaveSeveridadeDonut(CharacterizationData data, string path)
    {
        var plt = new Plot();
        plt.Title("CVEs por classe de severidade (1999–2025)");

        var valores = data.PorSeveridade.Select(r => r.NCves).Select(v => (double)v).ToArray();
        var labels = data.PorSeveridade
            .Select(r => $"{r.Severidade}\n{r.Pct:F1}%")
            .ToArray();
        var cores = data.PorSeveridade
            .Select(r => SeveridadeCores.GetValueOrDefault(r.Severidade, Colors.Gray))
            .ToArray();

        var pie = plt.Add.Pie(valores);
        pie.ExplodeFraction = 0.05;
        for (var i = 0; i < pie.Slices.Count; i++)
        {
            pie.Slices[i].FillColor = cores[i];
            pie.Slices[i].Label = labels[i];
        }

        plt.HideGrid();
        plt.Axes.Frameless();
        SavePlot(plt, path);
        return path;
    }

    private static string SaveSeveridadePorAno(CharacterizationData data, string path)
    {
        var plt = new Plot();
        plt.Title("Composição de severidade por ano (%)");
        plt.XLabel("Ano");
        plt.YLabel("Percentual no ano (%)");

        var anos = data.SeveridadePorAno.Select(r => r.Ano).Distinct().OrderBy(a => a).ToList();
        var severidades = data.PorSeveridade.OrderBy(s => s.Ordem).Select(s => s.Severidade).ToList();

        var posicoes = anos.Select((_, i) => (double)i).ToArray();
        var bases = new double[anos.Count];
        var labels = anos.Select(a => a.ToString()).ToArray();
        var stackedBars = new List<Bar>();

        foreach (var severidade in severidades)
        {
            var cor = SeveridadeCores.GetValueOrDefault(severidade, Colors.Gray);
            for (var i = 0; i < anos.Count; i++)
            {
                var item = data.SeveridadePorAno.FirstOrDefault(r =>
                    r.Ano == anos[i] && r.Severidade.Equals(severidade, StringComparison.OrdinalIgnoreCase));
                var valor = item?.PctNoAno ?? 0;
                stackedBars.Add(new Bar
                {
                    Position = posicoes[i],
                    Value = bases[i] + valor,
                    ValueBase = bases[i],
                    FillColor = cor,
                    LineColor = cor
                });
                bases[i] += valor;
            }
        }

        plt.Add.Bars(stackedBars);

        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            posicoes, labels);
        plt.Axes.Bottom.Min = -0.5;
        plt.Axes.Bottom.Max = anos.Count - 0.5;
        plt.Axes.Left.Max = 100;
        SavePlot(plt, path);
        return path;
    }

    private static string SaveDistribuicaoCvss(CharacterizationData data, string path)
    {
        var plt = new Plot();
        plt.Title("Distribuição do score CVSS");
        plt.XLabel("Score CVSS (0–10)");
        plt.YLabel("Nº de CVEs");

        var grupos = data.DistribuicaoCvss
            .GroupBy(d => d.FaixaSeveridade, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => OrdemSeveridade(g.Key))
            .ToList();

        foreach (var grupo in grupos)
        {
            var xs = grupo.Select(g => g.CvssScoreArredondado).ToArray();
            var ys = grupo.Select(g => (double)g.NCves).ToArray();
            var scatter = plt.Add.Scatter(xs, ys);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.Color = SeveridadeCores.GetValueOrDefault(grupo.Key, Colors.Gray);
            scatter.LegendText = grupo.Key;
        }

        plt.ShowLegend(Alignment.UpperRight);
        SavePlot(plt, path);
        return path;
    }

    private static int OrdemSeveridade(string severidade) => severidade.ToUpperInvariant() switch
    {
        "LOW" => 1,
        "MEDIUM" => 2,
        "HIGH" => 3,
        "CRITICAL" => 4,
        _ => 5
    };

    private static void SavePlot(Plot plt, string path)
    {
        plt.SavePng(path, 1400, 800);
    }
}

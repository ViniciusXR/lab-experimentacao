using System.Globalization;
using System.Net;
using System.Text;

namespace Enunciado5.Sprint3;

internal static class Program
{
    private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static async Task Main()
    {
        var labRoot = FindLabRoot();
        var outputRoot = Path.Combine(labRoot, "lab05_entrega");
        var dataDir = Path.Combine(outputRoot, "dados");
        var dashboardDir = Path.Combine(outputRoot, "dashboards");
        Directory.CreateDirectory(dashboardDir);

        var summaryPath = Path.Combine(dataDir, "resumo_estatistico.csv");
        var testsPath = Path.Combine(dataDir, "testes_estatisticos.csv");
        var measurementsPath = Path.Combine(dataDir, "medicoes_lab05.csv");

        if (!File.Exists(summaryPath) || !File.Exists(testsPath))
        {
            Console.WriteLine("Execute a Sprint 2 antes da Sprint 3 para gerar os CSVs de entrada.");
            return;
        }

        var summaries = ReadSummaries(summaryPath);
        var comparisons = ReadComparisons(testsPath);
        var measurementCount = File.Exists(measurementsPath)
            ? Math.Max(0, File.ReadLines(measurementsPath).Count() - 1)
            : summaries.Sum(row => row.N);

        var html = DashboardBuilder.Build(summaries, comparisons, measurementCount);
        var outputPath = Path.Combine(dashboardDir, "dashboard_lab05.html");
        await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8);

        Console.WriteLine("Lab05S03 concluido.");
        Console.WriteLine($"Dashboard gerado em: {outputPath}");
        Console.ReadKey();
    }

    private static string FindLabRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (current.Name.Equals("Enunciado 5", StringComparison.OrdinalIgnoreCase))
            {
                return current.FullName;
            }

            var candidate = Path.Combine(current.FullName, "Enunciado 5");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static List<SummaryRow> ReadSummaries(string path)
    {
        return File.ReadLines(path)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var parts = line.Split(';');
                return new SummaryRow(
                    parts[0],
                    parts[1],
                    parts[2],
                    int.Parse(parts[3], CsvCulture),
                    double.Parse(parts[5], CsvCulture),
                    double.Parse(parts[8], CsvCulture),
                    double.Parse(parts[10], CsvCulture));
            })
            .ToList();
    }

    private static List<ComparisonRow> ReadComparisons(string path)
    {
        return File.ReadLines(path)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var parts = line.Split(';');
                return new ComparisonRow(
                    parts[0],
                    parts[1],
                    double.Parse(parts[2], CsvCulture),
                    double.Parse(parts[3], CsvCulture),
                    double.Parse(parts[4], CsvCulture),
                    double.Parse(parts[5], CsvCulture),
                    double.Parse(parts[7], CsvCulture),
                    double.Parse(parts[8], CsvCulture),
                    double.Parse(parts[9], CsvCulture),
                    double.Parse(parts[10], CsvCulture));
            })
            .ToList();
    }

    internal static string F(double value, int decimals = 2) => value.ToString($"F{decimals}", PtBr);
    internal static string N(double value) => value.ToString("N0", PtBr);
    internal static string H(string value) => WebUtility.HtmlEncode(value);
}

internal sealed record SummaryRow(
    string ScenarioId,
    string ScenarioName,
    string Treatment,
    int N,
    double MedianMs,
    double P95Ms,
    double MedianBytes);

internal sealed record ComparisonRow(
    string ScenarioId,
    string ScenarioName,
    double RestMedianMs,
    double GraphQlMedianMs,
    double TimeReductionPct,
    double TimePValue,
    double RestMedianBytes,
    double GraphQlMedianBytes,
    double SizeReductionPct,
    double SizePValue);

internal static class DashboardBuilder
{
    public static string Build(
        IReadOnlyList<SummaryRow> summaries,
        IReadOnlyList<ComparisonRow> comparisons,
        int measurementCount)
    {
        var aggregate = comparisons.Single(row => row.ScenarioId == "ALL");
        var scenarioComparisons = comparisons.Where(row => row.ScenarioId != "ALL").ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"pt-BR\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine("  <title>Lab05 - Dashboard GraphQL vs REST</title>");
        builder.AppendLine(GetCss());
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<header>");
        builder.AppendLine("  <div>");
        builder.AppendLine("    <p class=\"eyebrow\">Laboratorio 05</p>");
        builder.AppendLine("    <h1>GraphQL vs REST</h1>");
        builder.AppendLine("    <p class=\"subtitle\">Dashboard de resultados do experimento controlado</p>");
        builder.AppendLine("  </div>");
        builder.AppendLine("  <div class=\"status\">RQ1 e RQ2 apoiadas no cenario controlado</div>");
        builder.AppendLine("</header>");

        builder.AppendLine("<main>");
        builder.AppendLine("<section class=\"kpis\" aria-label=\"Indicadores principais\">");
        builder.AppendLine(Kpi("Medições", Program.N(measurementCount), "trials coletados"));
        builder.AppendLine(Kpi("Cenários", scenarioComparisons.Length.ToString(CultureInfo.InvariantCulture), "consultas equivalentes"));
        builder.AppendLine(Kpi("Redução de tempo", $"{Program.F(aggregate.TimeReductionPct)}%", "mediana agregada"));
        builder.AppendLine(Kpi("Redução de tamanho", $"{Program.F(aggregate.SizeReductionPct)}%", "mediana agregada"));
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"grid two\">");
        builder.AppendLine(Panel("Tempo de resposta por cenário", GroupedBarChart(
            scenarioComparisons,
            row => row.RestMedianMs,
            row => row.GraphQlMedianMs,
            "ms",
            4)));
        builder.AppendLine(Panel("Tamanho da resposta por cenário", GroupedBarChart(
            scenarioComparisons,
            row => row.RestMedianBytes,
            row => row.GraphQlMedianBytes,
            "bytes",
            0)));
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"grid two\">");
        builder.AppendLine(Panel("Redução percentual observada", ReductionChart(scenarioComparisons)));
        builder.AppendLine(Panel("Síntese estatística", ComparisonTable(scenarioComparisons)));
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel full\">");
        builder.AppendLine("  <h2>Resumo por tratamento</h2>");
        builder.AppendLine(SummaryTable(summaries));
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"discussion\">");
        builder.AppendLine("  <h2>Discussão</h2>");
        builder.AppendLine("  <p>As consultas GraphQL retornaram payloads menores em todos os cenários porque projetam somente os campos usados pelo cliente. A queda no tamanho também reduziu o custo de serialização e parsing JSON, resultando em menor mediana de tempo no agregado.</p>");
        builder.AppendLine("  <p>Como a execução foi local e determinística, os resultados devem ser interpretados como evidência do efeito de over-fetching, não como comparação universal entre todas as implementações reais de REST e GraphQL.</p>");
        builder.AppendLine("</section>");
        builder.AppendLine("</main>");
        builder.AppendLine("</body></html>");

        return builder.ToString();
    }

    private static string Kpi(string label, string value, string caption) => $"""
        <article class="kpi">
          <span>{Program.H(label)}</span>
          <strong>{Program.H(value)}</strong>
          <small>{Program.H(caption)}</small>
        </article>
        """;

    private static string Panel(string title, string content) => $"""
        <section class="panel">
          <h2>{Program.H(title)}</h2>
          {content}
        </section>
        """;

    private static string GroupedBarChart(
        IReadOnlyList<ComparisonRow> rows,
        Func<ComparisonRow, double> rest,
        Func<ComparisonRow, double> graphQl,
        string unit,
        int decimals)
    {
        var width = 760;
        var height = 340;
        var left = 72;
        var top = 28;
        var chartWidth = width - left - 28;
        var chartHeight = 230;
        var max = rows.SelectMany(row => new[] { rest(row), graphQl(row) }).Max() * 1.12;
        var groupWidth = chartWidth / rows.Count;
        var barWidth = Math.Min(34, groupWidth * 0.25);

        var svg = new StringBuilder();
        svg.AppendLine($"""<svg viewBox="0 0 {width} {height}" role="img" aria-label="Grafico de barras">""");
        svg.AppendLine($"""<line x1="{left}" y1="{top + chartHeight}" x2="{left + chartWidth}" y2="{top + chartHeight}" class="axis" />""");
        svg.AppendLine($"""<line x1="{left}" y1="{top}" x2="{left}" y2="{top + chartHeight}" class="axis" />""");

        for (var i = 0; i <= 4; i++)
        {
            var value = max * i / 4.0;
            var y = top + chartHeight - (value / max * chartHeight);
            svg.AppendLine($"""<line x1="{left}" y1="{y:F2}" x2="{left + chartWidth}" y2="{y:F2}" class="gridline" />""");
            svg.AppendLine($"""<text x="{left - 8}" y="{y + 4:F2}" class="tick" text-anchor="end">{FormatValue(value, unit, decimals)}</text>""");
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var center = left + i * groupWidth + groupWidth / 2.0;
            var restHeight = rest(row) / max * chartHeight;
            var graphHeight = graphQl(row) / max * chartHeight;
            var restX = center - barWidth - 4;
            var graphX = center + 4;
            var restY = top + chartHeight - restHeight;
            var graphY = top + chartHeight - graphHeight;

            svg.AppendLine($"""<rect x="{restX:F2}" y="{restY:F2}" width="{barWidth:F2}" height="{restHeight:F2}" class="bar rest"><title>{row.ScenarioId} REST: {FormatValue(rest(row), unit, decimals)}</title></rect>""");
            svg.AppendLine($"""<rect x="{graphX:F2}" y="{graphY:F2}" width="{barWidth:F2}" height="{graphHeight:F2}" class="bar graphql"><title>{row.ScenarioId} GraphQL: {FormatValue(graphQl(row), unit, decimals)}</title></rect>""");
            svg.AppendLine($"""<text x="{center:F2}" y="{top + chartHeight + 25}" class="label" text-anchor="middle">{row.ScenarioId}</text>""");
        }

        svg.AppendLine($"""<rect x="{left + 8}" y="{height - 40}" width="14" height="14" class="bar rest" /><text x="{left + 28}" y="{height - 28}" class="legend">REST</text>""");
        svg.AppendLine($"""<rect x="{left + 104}" y="{height - 40}" width="14" height="14" class="bar graphql" /><text x="{left + 124}" y="{height - 28}" class="legend">GraphQL</text>""");
        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    private static string ReductionChart(IReadOnlyList<ComparisonRow> rows)
    {
        var width = 760;
        var height = 340;
        var left = 72;
        var top = 28;
        var chartWidth = width - left - 28;
        var chartHeight = 230;
        var max = rows.SelectMany(row => new[] { row.TimeReductionPct, row.SizeReductionPct }).Max() * 1.12;
        var groupWidth = chartWidth / rows.Count;
        var barWidth = Math.Min(34, groupWidth * 0.25);
        var svg = new StringBuilder();

        svg.AppendLine($"""<svg viewBox="0 0 {width} {height}" role="img" aria-label="Grafico de reducao percentual">""");
        svg.AppendLine($"""<line x1="{left}" y1="{top + chartHeight}" x2="{left + chartWidth}" y2="{top + chartHeight}" class="axis" />""");
        svg.AppendLine($"""<line x1="{left}" y1="{top}" x2="{left}" y2="{top + chartHeight}" class="axis" />""");

        for (var i = 0; i <= 4; i++)
        {
            var value = max * i / 4.0;
            var y = top + chartHeight - (value / max * chartHeight);
            svg.AppendLine($"""<line x1="{left}" y1="{y:F2}" x2="{left + chartWidth}" y2="{y:F2}" class="gridline" />""");
            svg.AppendLine($"""<text x="{left - 8}" y="{y + 4:F2}" class="tick" text-anchor="end">{Program.F(value, 0)}%</text>""");
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var center = left + i * groupWidth + groupWidth / 2.0;
            var timeHeight = row.TimeReductionPct / max * chartHeight;
            var sizeHeight = row.SizeReductionPct / max * chartHeight;
            var timeX = center - barWidth - 4;
            var sizeX = center + 4;
            var timeY = top + chartHeight - timeHeight;
            var sizeY = top + chartHeight - sizeHeight;

            svg.AppendLine($"""<rect x="{timeX:F2}" y="{timeY:F2}" width="{barWidth:F2}" height="{timeHeight:F2}" class="bar time"><title>{row.ScenarioId} tempo: {Program.F(row.TimeReductionPct)}%</title></rect>""");
            svg.AppendLine($"""<rect x="{sizeX:F2}" y="{sizeY:F2}" width="{barWidth:F2}" height="{sizeHeight:F2}" class="bar size"><title>{row.ScenarioId} tamanho: {Program.F(row.SizeReductionPct)}%</title></rect>""");
            svg.AppendLine($"""<text x="{center:F2}" y="{top + chartHeight + 25}" class="label" text-anchor="middle">{row.ScenarioId}</text>""");
        }

        svg.AppendLine($"""<rect x="{left + 8}" y="{height - 40}" width="14" height="14" class="bar time" /><text x="{left + 28}" y="{height - 28}" class="legend">Tempo</text>""");
        svg.AppendLine($"""<rect x="{left + 112}" y="{height - 40}" width="14" height="14" class="bar size" /><text x="{left + 132}" y="{height - 28}" class="legend">Tamanho</text>""");
        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    private static string ComparisonTable(IEnumerable<ComparisonRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>Cenario</th><th>Tempo</th><th>p tempo</th><th>Tamanho</th><th>p tamanho</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{Program.H(row.ScenarioId)}</td>");
            builder.AppendLine($"<td>{Program.F(row.TimeReductionPct)}%</td>");
            builder.AppendLine($"<td>{P(row.TimePValue)}</td>");
            builder.AppendLine($"<td>{Program.F(row.SizeReductionPct)}%</td>");
            builder.AppendLine($"<td>{P(row.SizePValue)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody></table>");
        return builder.ToString();
    }

    private static string SummaryTable(IEnumerable<SummaryRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>Cenario</th><th>Tratamento</th><th>n</th><th>Mediana ms</th><th>P95 ms</th><th>Mediana bytes</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{Program.H(row.ScenarioId)} - {Program.H(row.ScenarioName)}</td>");
            builder.AppendLine($"<td><span class=\"pill {row.Treatment.ToLowerInvariant()}\">{Program.H(row.Treatment)}</span></td>");
            builder.AppendLine($"<td>{row.N}</td>");
            builder.AppendLine($"<td>{Program.F(row.MedianMs, 4)}</td>");
            builder.AppendLine($"<td>{Program.F(row.P95Ms, 4)}</td>");
            builder.AppendLine($"<td>{Program.N(row.MedianBytes)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody></table>");
        return builder.ToString();
    }

    private static string FormatValue(double value, string unit, int decimals)
    {
        return unit == "bytes"
            ? Program.N(value)
            : $"{Program.F(value, decimals)} {unit}";
    }

    private static string P(double value) => value < 0.0001 ? "< 0,0001" : Program.F(value, 4);

    private static string GetCss() => """
        <style>
        :root {
          --page: #f7f8fb;
          --ink: #17202a;
          --muted: #5d6978;
          --line: #d7dde7;
          --panel: #ffffff;
          --rest: #d5534a;
          --graphql: #1f8a70;
          --time: #3867d6;
          --size: #a35c1a;
          --accent: #253858;
        }

        * { box-sizing: border-box; }
        body {
          margin: 0;
          background: var(--page);
          color: var(--ink);
          font-family: "Segoe UI", Arial, sans-serif;
          line-height: 1.45;
        }

        header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 24px;
          padding: 24px 32px;
          color: #fff;
          background: #253858;
          border-bottom: 5px solid #1f8a70;
        }

        h1, h2, p { margin: 0; }
        h1 { font-size: clamp(30px, 5vw, 54px); font-weight: 750; letter-spacing: 0; }
        h2 { margin-bottom: 16px; font-size: 18px; color: var(--accent); }
        .eyebrow { color: #b7d8d0; font-size: 13px; font-weight: 700; text-transform: uppercase; }
        .subtitle { color: #d9e4f2; font-size: 16px; }
        .status {
          max-width: 280px;
          padding: 10px 14px;
          border: 1px solid rgba(255,255,255,.24);
          border-radius: 8px;
          color: #ecfff9;
          font-weight: 650;
          text-align: center;
        }

        main { width: min(1280px, calc(100% - 32px)); margin: 24px auto 40px; }
        .kpis {
          display: grid;
          grid-template-columns: repeat(4, minmax(0, 1fr));
          gap: 14px;
          margin-bottom: 18px;
        }

        .kpi, .panel, .discussion {
          background: var(--panel);
          border: 1px solid var(--line);
          border-radius: 8px;
          box-shadow: 0 6px 18px rgba(23, 32, 42, .06);
        }

        .kpi { padding: 16px; min-height: 112px; }
        .kpi span { display: block; color: var(--muted); font-size: 13px; font-weight: 700; text-transform: uppercase; }
        .kpi strong { display: block; margin-top: 8px; font-size: 30px; color: var(--accent); }
        .kpi small { display: block; margin-top: 6px; color: var(--muted); }

        .grid { display: grid; gap: 18px; margin-bottom: 18px; }
        .grid.two { grid-template-columns: repeat(2, minmax(0, 1fr)); }
        .panel { padding: 18px; overflow-x: auto; }
        .panel.full { margin-bottom: 18px; }
        .discussion { padding: 20px; display: grid; gap: 10px; }

        svg { width: 100%; height: auto; display: block; }
        .axis { stroke: #536170; stroke-width: 1.25; }
        .gridline { stroke: #e4e8ef; stroke-width: 1; }
        .tick, .label, .legend { fill: #536170; font-size: 12px; }
        .bar { rx: 3px; }
        .rest { fill: var(--rest); }
        .graphql { fill: var(--graphql); }
        .time { fill: var(--time); }
        .size { fill: var(--size); }

        table { width: 100%; border-collapse: collapse; font-size: 14px; }
        th, td { padding: 10px 12px; border-bottom: 1px solid var(--line); text-align: left; white-space: nowrap; }
        th { color: var(--accent); background: #edf1f7; font-size: 12px; text-transform: uppercase; }
        tr:last-child td { border-bottom: 0; }
        .pill { display: inline-block; min-width: 76px; padding: 4px 8px; border-radius: 999px; color: #fff; text-align: center; font-size: 12px; font-weight: 700; }
        .pill.rest { background: var(--rest); }
        .pill.graphql { background: var(--graphql); }

        @media (max-width: 900px) {
          header { align-items: flex-start; flex-direction: column; }
          .kpis, .grid.two { grid-template-columns: 1fr; }
          main { width: min(100% - 20px, 1280px); }
        }
        </style>
        """;
}

using System.Globalization;
using System.Text;
using Enunciado4.Sprint1.Models;

namespace Enunciado4.Sprint1.Report;

public static class Lab04DashboardHtml
{
    public static async Task GenerateAllAsync(
        Lab04Dataset data,
        GeneratedCharts charts,
        string dashboardsDir)
    {
        Directory.CreateDirectory(dashboardsDir);
        await File.WriteAllTextAsync(
            Path.Combine(dashboardsDir, "sprint1.html"),
            BuildPage(data, charts.Caracterizacao, null, null, null, dashboardsDir, "Sprint 1", ["caracterizacao"]),
            Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(dashboardsDir, "sprint2.html"),
            BuildPage(data, charts.Caracterizacao, charts.Rq1, charts.Rq2, null, dashboardsDir,
                "Sprint 2", ["caracterizacao", "rq1", "rq2"]),
            Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(dashboardsDir, "dashboard_final.html"),
            BuildPage(data, charts.Caracterizacao, charts.Rq1, charts.Rq2, charts.Rq3, dashboardsDir,
                "Dashboard Final — Lab04", ["caracterizacao", "rq1", "rq2", "rq3", "gqm"]),
            Encoding.UTF8);
    }

    private static string BuildPage(
        Lab04Dataset data,
        IReadOnlyList<string>? p1,
        IReadOnlyList<string>? rq1,
        IReadOnlyList<string>? rq2,
        IReadOnlyList<string>? rq3,
        string dashboardsDir,
        string titulo,
        string[] sections)
    {
        var k = data.Characterization.Kpis;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
        sb.AppendLine($"<title>{titulo}</title>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<header><h1>{titulo}</h1>");
        sb.AppendLine("<p>TI6 — Priorização estratégica de CWEs · NIST NVD 1999–2025</p></header>");

        sb.AppendLine("<nav class=\"tabs\">");
        if (sections.Contains("caracterizacao"))
            sb.AppendLine("<a href=\"#p1\">P1 · Caracterização</a>");
        if (sections.Contains("rq1"))
            sb.AppendLine("<a href=\"#p2\">P2 · RQ1</a>");
        if (sections.Contains("rq2"))
            sb.AppendLine("<a href=\"#p3\">P3 · RQ2</a>");
        if (sections.Contains("rq3"))
            sb.AppendLine("<a href=\"#p4\">P4 · RQ3</a>");
        if (sections.Contains("gqm"))
            sb.AppendLine("<a href=\"#p5\">Síntese GQM</a>");
        sb.AppendLine("</nav>");

        if (sections.Contains("caracterizacao") && p1 is not null)
        {
            sb.AppendLine("<section id=\"p1\" class=\"page\">");
            sb.AppendLine("<h2>Caracterização do Dataset — Vulnerabilidades NVD (1999–2025)</h2>");
            sb.AppendLine(KpiHtml(k));
            sb.AppendLine(NoteHtml("Fonte: NIST NVD. Unidade: relação CVE↔CWE. Relações (278.912) > CVEs distintos (253.802) pois cada CVE pode ter múltiplas CWEs."));
            sb.AppendLine(SeveridadeTableHtml(data));
            sb.AppendLine(ChartsHtml(p1, dashboardsDir));
            sb.AppendLine("</section>");
        }

        if (sections.Contains("rq1") && rq1 is not null)
        {
            sb.AppendLine("<section id=\"p2\" class=\"page\">");
            sb.AppendLine("<h2>RQ1 — Quais CWEs concentram a maior parte das vulnerabilidades?</h2>");
            sb.AppendLine(QuestionHtml("Hipótese: distribuição não-uniforme (Pareto). Veredito: 38 CWEs = 80%; Gini = 0,92."));
            sb.AppendLine(KpiMiniHtml("CWEs Pareto 80%: 38", $"Gini: {k.GiniFrequencia:F4}"));
            sb.AppendLine(ChartsHtml(rq1, dashboardsDir));
            sb.AppendLine("</section>");
        }

        if (sections.Contains("rq2") && rq2 is not null)
        {
            sb.AppendLine("<section id=\"p3\" class=\"page\">");
            sb.AppendLine("<h2>RQ2 — Quais CWEs têm maior potencial técnico de dano (CVSS)?</h2>");
            sb.AppendLine(QuestionHtml("Hipótese: severidade varia entre CWEs. Veredito: Kruskal-Wallis p < 0,001; cluster CVSS ≈ 9,8."));
            sb.AppendLine(KpiMiniHtml($"CVSS médio global: {k.CvssMedioGlobal:F2}", $"Mediana: {k.CvssMedianaGlobal:F2}"));
            sb.AppendLine(ChartsHtml(rq2, dashboardsDir));
            sb.AppendLine("</section>");
        }

        if (sections.Contains("rq3") && rq3 is not null)
        {
            sb.AppendLine("<section id=\"p4\" class=\"page\">");
            sb.AppendLine("<h2>RQ3 — Ser frequente é o mesmo que ser perigoso?</h2>");
            sb.AppendLine(QuestionHtml("Veredito: Spearman ρ ≈ −0,02 (independentes), mas 53 CWEs em Dupla Ameaça."));
            sb.AppendLine(KpiMiniHtml("Spearman ρ: −0,018", "CWEs Dupla Ameaça: 53"));
            sb.AppendLine(ChartsHtml(rq3, dashboardsDir));
            sb.AppendLine(PriorizacaoTableHtml(data));
            sb.AppendLine("</section>");
        }

        if (sections.Contains("gqm"))
        {
            sb.AppendLine("<section id=\"p5\" class=\"page\">");
            sb.AppendLine("<h2>Síntese GQM — Pergunta → Métrica → Veredito</h2>");
            sb.AppendLine(GqmTableHtml(data));
            sb.AppendLine(EstatisticasTableHtml(data));
            sb.AppendLine(NoteHtml("Exportar PDF: Ctrl+P → Salvar como PDF (marque 'Gráficos de fundo')."));
            sb.AppendLine("</section>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string KpiHtml(KpisGerais k) => $"""
        <div class="kpis">
          <div class="kpi"><span>CVEs distintos</span><strong>{k.TotalCvesDistintos:N0}</strong></div>
          <div class="kpi"><span>Relações CVE↔CWE</span><strong>{k.TotalRelacoesCveCwe:N0}</strong></div>
          <div class="kpi"><span>CWEs distintas</span><strong>{k.TotalCwesDistintas:N0}</strong></div>
          <div class="kpi"><span>Cobertura CVSS</span><strong>{k.CoberturaCvssPct:F2}%</strong></div>
          <div class="kpi"><span>CVSS médio</span><strong>{k.CvssMedioGlobal:F2}</strong></div>
          <div class="kpi"><span>CVSS mediana</span><strong>{k.CvssMedianaGlobal:F2}</strong></div>
          <div class="kpi"><span>Gini</span><strong>{k.GiniFrequencia:F4}</strong></div>
          <div class="kpi"><span>Período</span><strong>{k.AnoInicio}–{k.AnoFim}</strong></div>
        </div>
        """;

    private static string KpiMiniHtml(string a, string b) =>
        $"""<div class="kpis mini"><div class="kpi"><span>{a}</span></div><div class="kpi"><span>{b}</span></div></div>""";

    private static string SeveridadeTableHtml(Lab04Dataset data)
    {
        var rows = string.Join('\n', data.Characterization.PorSeveridade.Select(s =>
            $"<tr><td>{s.Severidade}</td><td>{s.NCves:N0}</td><td>{s.Pct:F2}%</td><td>{s.CvssMedia:F2}</td><td>{s.CvssMediana:F2}</td></tr>"));
        return $"""
            <table><thead><tr><th>Severidade</th><th>Nº CVEs</th><th>%</th><th>CVSS médio</th><th>CVSS mediana</th></tr></thead>
            <tbody>{rows}</tbody></table>
            """;
    }

    private static string PriorizacaoTableHtml(Lab04Dataset data)
    {
        var rows = string.Join('\n', data.Priorizacao.Take(20).Select(p =>
            $"<tr><td>{p.Prioridade}</td><td>{p.CweId}</td><td>{p.CweNomeCurto}</td><td>{p.CvesCount:N0}</td><td>{p.CvssMedio:F2}</td><td>{p.HighCriticalPct:F1}%</td><td>{p.ScoreRisco:F4}</td></tr>"));
        return $"""
            <h3>Fila de priorização estratégica (Top 20 — quadrante Dupla Ameaça)</h3>
            <table><thead><tr><th>#</th><th>CWE</th><th>Nome</th><th>CVEs</th><th>CVSS médio</th><th>% HI/CR</th><th>Score</th></tr></thead>
            <tbody>{rows}</tbody></table>
            """;
    }

    private static string GqmTableHtml(Lab04Dataset data)
    {
        var rows = string.Join('\n', data.Gqm.Select(g =>
            $"<tr><td>{g.Pergunta}</td><td>{g.Titulo}</td><td>{g.Metricas}</td><td>{g.Veredito}</td><td>{g.Evidencia}</td></tr>"));
        return $"""
            <table><thead><tr><th>ID</th><th>Pergunta</th><th>Métricas</th><th>Veredito</th><th>Evidência</th></tr></thead>
            <tbody>{rows}</tbody></table>
            """;
    }

    private static string EstatisticasTableHtml(Lab04Dataset data)
    {
        var rows = string.Join('\n', data.Estatisticas.Select(e =>
            $"<tr><td>{e.Teste}</td><td>{e.Hipotese}</td><td>{e.PValor}</td><td>{e.Resultado}</td></tr>"));
        return $"""
            <h3>Testes estatísticos</h3>
            <table><thead><tr><th>Teste</th><th>Hipótese</th><th>p-valor</th><th>Resultado</th></tr></thead>
            <tbody>{rows}</tbody></table>
            """;
    }

    private static string ChartsHtml(IReadOnlyList<string> charts, string dashboardsDir)
    {
        var sb = new StringBuilder("<div class=\"charts\">");
        foreach (var chart in charts)
        {
            var rel = Path.GetRelativePath(dashboardsDir, chart).Replace('\\', '/');
            var title = Path.GetFileNameWithoutExtension(chart).Replace('_', ' ');
            sb.AppendLine($"""<figure><h3>{title}</h3><img src="{rel}" alt="{title}"/></figure>""");
        }
        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string QuestionHtml(string text) => $"""<div class="question">{text}</div>""";
    private static string NoteHtml(string text) => $"""<div class="note">{text}</div>""";

    private static string GetCss() => """
        <style>
        :root{--bg:#0f172a;--card:#1e293b;--text:#e2e8f0;--accent:#38bdf8;--muted:#94a3b8}
        *{box-sizing:border-box}body{margin:0;font-family:Segoe UI,system-ui,sans-serif;background:var(--bg);color:var(--text)}
        header{padding:1.5rem 2rem;border-bottom:1px solid #334155}header h1{margin:0 0 .4rem;font-size:1.5rem}header p{margin:0;color:var(--muted)}
        .tabs{display:flex;flex-wrap:wrap;gap:.5rem;padding:1rem 2rem;background:#1e293b;position:sticky;top:0;z-index:10}
        .tabs a{color:var(--accent);text-decoration:none;padding:.4rem .8rem;border:1px solid #334155;border-radius:6px;font-size:.85rem}
        .page{padding:1.5rem 2rem;border-bottom:2px solid #334155;page-break-after:always}
        .page h2{font-size:1.2rem;margin:0 0 1rem}
        .page h3{font-size:1rem;margin:1.5rem 0 .75rem}
        .kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:.75rem;margin-bottom:1rem}
        .kpis.mini{grid-template-columns:repeat(auto-fit,minmax(200px,1fr))}
        .kpi{background:var(--card);border:1px solid #334155;border-radius:10px;padding:.75rem}
        .kpi span{display:block;font-size:.75rem;color:var(--muted);text-transform:uppercase}
        .kpi strong{display:block;font-size:1.3rem;color:var(--accent);margin-top:.25rem}
        .question,.note{padding:1rem;border-radius:8px;margin-bottom:1rem;line-height:1.5}
        .question{background:#172554;border-left:4px solid var(--accent)}
        .note{background:#1a2e1a;border-left:4px solid #3B8C3F;color:#cbd5e1}
        table{width:100%;border-collapse:collapse;background:var(--card);border-radius:10px;overflow:hidden;margin-bottom:1rem;font-size:.85rem}
        th,td{padding:.6rem .8rem;text-align:left;border-bottom:1px solid #334155}
        th{background:#334155}
        .charts{display:grid;grid-template-columns:repeat(auto-fit,minmax(400px,1fr));gap:1rem}
        figure{background:var(--card);border:1px solid #334155;border-radius:10px;padding:.75rem;margin:0}
        figure h3{margin:0 0 .5rem;font-size:.9rem;color:var(--muted)}
        figure img{width:100%;height:auto;border-radius:6px;background:#fff}
        @media print{
          .tabs{display:none}
          body{background:#fff;color:#000}
          .page,.kpi,figure,table{background:#fff;border-color:#ccc}
          .kpi strong{color:#000}
        }
        </style>
        """;
}

using System.Globalization;
using System.Text;
using Enunciado4.Sprint1.Models;

namespace Enunciado4.Sprint1.Report;

public static class Lab04ReportGenerator
{
    public static async Task GenerateAllAsync(
        Lab04Dataset data,
        GeneratedCharts charts,
        string relatoriosDir)
    {
        Directory.CreateDirectory(relatoriosDir);
        await File.WriteAllTextAsync(
            Path.Combine(relatoriosDir, "sprint1_caracterizacao.md"),
            BuildSprint1(data, charts.Caracterizacao), Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(relatoriosDir, "sprint2_rq1_rq2.md"),
            BuildSprint2(data, charts.Rq1, charts.Rq2), Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(relatoriosDir, "sprint3_relatorio_final.md"),
            BuildSprint3Final(data, charts), Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(relatoriosDir, "artigo_ti6_atualizado.md"),
            BuildArtigoTi6(data, charts), Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(relatoriosDir, "checklist_entrega.md"),
            BuildChecklist(), Encoding.UTF8);
    }

    private static string BuildSprint1(Lab04Dataset data, IReadOnlyList<string> charts)
    {
        var k = data.Characterization.Kpis;
        var sb = new StringBuilder();
        sb.AppendLine("# Lab04 — Sprint 1: Caracterização do Dataset");
        sb.AppendLine();
        sb.AppendLine("## KPIs globais");
        sb.AppendLine($"- **{k.TotalCvesDistintos:N0}** CVEs distintos · **{k.TotalRelacoesCveCwe:N0}** relações CVE↔CWE · **{k.TotalCwesDistintas}** CWEs");
        sb.AppendLine($"- CVSS médio **{k.CvssMedioGlobal:F2}** · mediana **{k.CvssMedianaGlobal:F2}** · Gini **{k.GiniFrequencia:F4}**");
        sb.AppendLine();
        sb.AppendLine("## Subgrupos");
        foreach (var s in data.Characterization.PorSeveridade)
            sb.AppendLine($"- **{s.Severidade}:** {s.NCves:N0} CVEs ({s.Pct:F1}%) — CVSS mediana {s.CvssMediana:F2}");
        sb.AppendLine();
        sb.AppendLine("## Gráficos");
        foreach (var c in charts) sb.AppendLine($"- `{Path.GetFileName(c)}`");
        return sb.ToString();
    }

    private static string BuildSprint2(Lab04Dataset data, IReadOnlyList<string> rq1, IReadOnlyList<string> rq2)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Lab04 — Sprint 2: RQ1 e RQ2");
        sb.AppendLine();
        sb.AppendLine("## RQ1 — Quais CWEs concentram a maior parte das vulnerabilidades?");
        sb.AppendLine("**Veredito:** CONFIRMADA — 38 CWEs cobrem 80% das relações; Gini = 0,92.");
        sb.AppendLine();
        sb.AppendLine("### Gráficos RQ1");
        foreach (var c in rq1) sb.AppendLine($"- `{Path.GetFileName(c)}`");
        sb.AppendLine();
        sb.AppendLine("## RQ2 — Quais CWEs têm maior potencial técnico de dano (CVSS)?");
        sb.AppendLine("**Veredito:** CONFIRMADA — Kruskal-Wallis p < 0,001; cluster com CVSS ≈ 9,8.");
        sb.AppendLine();
        sb.AppendLine("### Gráficos RQ2");
        foreach (var c in rq2) sb.AppendLine($"- `{Path.GetFileName(c)}`");
        return sb.ToString();
    }

    private static string BuildSprint3Final(Lab04Dataset data, GeneratedCharts charts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Lab04 — Sprint 3: Dashboard Final + Artigo TI6");
        sb.AppendLine();
        sb.AppendLine("## Entregáveis");
        sb.AppendLine("- `dashboards/dashboard_final.html` — dashboard completo (4 páginas + síntese GQM)");
        sb.AppendLine("- `relatorios/artigo_ti6_atualizado.md` — Seções 3 e 4 do artigo com figuras");
        sb.AppendLine("- Exportar dashboard em PDF: abrir `dashboard_final.html` → Ctrl+P → Salvar como PDF");
        sb.AppendLine();
        sb.AppendLine("## RQ3 — Ser frequente é o mesmo que ser perigoso?");
        sb.AppendLine("**Veredito:** CONFIRMADA com nuance — Spearman ρ ≈ −0,02 (independentes), mas **53 CWEs** em Dupla Ameaça.");
        sb.AppendLine();
        sb.AppendLine("## Fila de priorização (Top 10)");
        sb.AppendLine("| # | CWE | CVEs | CVSS médio | Score risco |");
        sb.AppendLine("|---|-----|-----:|-----------:|------------:|");
        foreach (var p in data.Priorizacao.Take(10))
            sb.AppendLine($"| {p.Prioridade} | {p.CweNomeCurto} | {p.CvesCount:N0} | {p.CvssMedio:F2} | {p.ScoreRisco:F4} |");
        sb.AppendLine();
        sb.AppendLine("## Total de visualizações geradas");
        sb.AppendLine($"- Caracterização: {charts.Caracterizacao.Count}");
        sb.AppendLine($"- RQ1: {charts.Rq1.Count}");
        sb.AppendLine($"- RQ2: {charts.Rq2.Count}");
        sb.AppendLine($"- RQ3: {charts.Rq3.Count}");
        return sb.ToString();
    }

    private static string BuildArtigoTi6(Lab04Dataset data, GeneratedCharts charts)
    {
        var k = data.Characterization.Kpis;
        var sb = new StringBuilder();
        sb.AppendLine("# Artigo TI6 — Atualização Lab04 (Seções 3 e 4)");
        sb.AppendLine();
        sb.AppendLine("*Inteligência de dados aplicada à segurança: priorização estratégica de CWEs*");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 3. Metodologia — Caracterização do Dataset");
        sb.AppendLine();
        sb.AppendLine("A base compreende **253.802 CVEs distintos** e **278.912 relações CVE↔CWE** " +
                        "extraídas da NIST NVD (1999–2025), abrangendo **749 CWEs** distintas com cobertura CVSS de **99,99%**.");
        sb.AppendLine();
        sb.AppendLine("**Figura 1** apresenta o volume anual de CVEs distintos, evidenciando crescimento acelerado " +
                        "a partir de 2017 e pico em 2025 (~45 mil CVEs), o que sustenta a relevância do problema de fadiga de alertas.");
        sb.AppendLine();
        sb.AppendLine("**Figura 2** mostra a distribuição por classe de severidade CVSS no período completo: " +
                        "MEDIUM concentra 45,5% das CVEs, HIGH 39,0% e CRITICAL 11,5%.");
        sb.AppendLine();
        sb.AppendLine("**Figura 3** detalha a composição percentual de severidade por ano, revelando aumento da fatia " +
                        "HIGH/CRITICAL após 2016 (entrada do CVSS v3).");
        sb.AppendLine();
        sb.AppendLine("**Figura 4** exibe a distribuição do score CVSS (multimodal, com picos em ~5,5; ~7,5; ~9,8). " +
                        $"A mediana global ({k.CvssMedianaGlobal:F2}) é preferível à média ({k.CvssMedioGlobal:F2}) por ser robusta a outliers.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 4. Resultados");
        sb.AppendLine();
        sb.AppendLine("### 4.1 RQ1 — Frequência");
        sb.AppendLine();
        sb.AppendLine("**Figura 5** (Pareto) confirma concentração extrema: **38 CWEs** respondem por **80%** das relações. " +
                        "CWE-79 (XSS) lidera isoladamente com 41.171 ocorrências.");
        sb.AppendLine();
        sb.AppendLine("**Figura 6** mostra a evolução temporal das 10 CWEs mais frequentes; XSS dispara após 2017.");
        sb.AppendLine();
        sb.AppendLine("**Figura 7** (Curva de Lorenz) quantifica a desigualdade (Gini = 0,92).");
        sb.AppendLine();
        sb.AppendLine("### 4.2 RQ2 — Severidade");
        sb.AppendLine();
        sb.AppendLine("**Figura 8** apresenta CWEs com maior CVSS médio (n ≥ 30); cluster em ~9,8.");
        sb.AppendLine("**Figura 9** complementa com % HIGH/CRITICAL por CWE. Kruskal-Wallis: p < 0,001.");
        sb.AppendLine();
        sb.AppendLine("### 4.3 RQ3 — Interação Frequência × Severidade");
        sb.AppendLine();
        sb.AppendLine("**Figura 10** (dispersão) mostra independência global (Spearman ρ ≈ −0,02, p = 0,62), " +
                        "mas **53 CWEs** no quadrante Dupla Ameaça combinam alta frequência e alta severidade.");
        sb.AppendLine();
        sb.AppendLine("**Figura 11** ranqueia CWEs pelo score de risco composto; **Figura 12** resume os quadrantes.");
        sb.AppendLine();
        sb.AppendLine("**Tabela 1** lista a fila de priorização estratégica (53 CWEs Dupla Ameaça), " +
                        "liderada por SQL Injection (CWE-89), Out-of-bounds Write (CWE-787) e Buffer Overflow (CWE-119).");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Síntese GQM");
        sb.AppendLine();
        sb.AppendLine("| Pergunta | Veredito | Evidência |");
        sb.AppendLine("|----------|----------|-----------|");
        foreach (var g in data.Gqm)
            sb.AppendLine($"| {g.Titulo} | {g.Veredito} | {g.Evidencia} |");
        sb.AppendLine();
        sb.AppendLine("## Testes estatísticos");
        sb.AppendLine();
        sb.AppendLine("| Teste | p-valor | Resultado |");
        sb.AppendLine("|-------|---------|-----------|");
        foreach (var e in data.Estatisticas)
            sb.AppendLine($"| {e.Teste} | {e.PValor} | {e.Resultado} |");
        return sb.ToString();
    }

    private static string BuildChecklist() => """
        # Checklist de Entrega — Lab04 Completo

        ## Sprint 1 (Lab04S01)
        - [x] Caracterização do dataset (KPIs + subgrupos severidade/tempo)
        - [x] Gráficos gerados em `graficos/p1_caracterizacao/`
        - [x] Dashboard Sprint 1: `dashboards/sprint1.html`

        ## Sprint 2 (Lab04S02)
        - [x] Visualizações RQ1 (Pareto, tendência, Lorenz, Jaccard)
        - [x] Visualizações RQ2 (CVSS médio, % HIGH/CRITICAL)
        - [x] Dashboard Sprint 2: `dashboards/sprint2.html`

        ## Sprint 3 (Lab04S03)
        - [x] Dashboard final: `dashboards/dashboard_final.html`
        - [x] Artigo TI6 atualizado: `relatorios/artigo_ti6_atualizado.md`
        - [ ] Exportar PDF: abrir dashboard_final.html → Imprimir → Salvar como PDF
        - [ ] (Opcional) Replicar no Power BI seguindo `GUIA_DASHBOARD_POWERBI.md`
        - [ ] Apresentar em aula com participação de todos os membros

        ## Power BI (se exigido pelo professor)
        1. Importar `dashboard_powerbi.xlsx`
        2. Montar 4 páginas conforme guia
        3. Exportar `.pbix` e PDF pelo Power BI Desktop
        """;
}

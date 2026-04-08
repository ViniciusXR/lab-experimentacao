using System.Globalization;
using System.Text;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using QuestPDF.Infrastructure;
using Spectre.Console;

namespace Enunciado2.Sprint3;

/*
 * ═══════════════════════════════════════════════════════════════════════════
 * LAB 02 (Enunciado 2) — MAPA DO ENUNCIADO → ESTE CÓDIGO (Sprint 3)
 * ═══════════════════════════════════════════════════════════════════════════
 * Relatório final (PDF + Markdown): intro, metodologia, resultados, discussão — GerarRelatorioMarkdown + RelatorioLab02Pdf.
 *
 * RQ01 Popularidade × qualidade: métrica processo = estrelas → EscreverQuartisPorRq (rq01_*) + ExecutarBonus (scatter estrelas×CBO/DIT/LCOM).
 * RQ02 Maturidade × qualidade: idade_anos → rq02_* + correlações idade×CK.
 * RQ03 Atividade × qualidade: releases → rq03_* + correlações releases×CK.
 * RQ04 Tamanho × qualidade: LOC Java (+ comentários no consolidado) → rq04_* + loc×CK; disk_usage_kb também em ExecutarBonus.
 *
 * Sumarização (média, mediana, desvio) por repositório: já vem nas colunas do consolidado (Sprint 1/2);
 * aqui: resumo GLOBAL entre repositórios — EscreverResumoGlobal; e por quartil — EscreverQuartisPorRq.
 *
 * Bônus (+1): gráficos PNG (ScottPlot) + Pearson/Spearman + p aprox. (t, n−2 g.l.) — ExecutarBonus, correlacoes_*.csv.
 * Relatório Markdown: após o bônus, embute tabelas RQ e CSVs de correlação; inclui conclusões, referências e lista de entregáveis.
 * Autores: `--autores=`, `LAB02_AUTORES` ou `lab02_autores.txt` na pasta da Sprint 3.
 * Saída lab02_sprint3_output: apagada e recriada após validar consolidado (cada dotnet run começa do zero).
 *
 * O que depende de dados: se o consolidado não tiver CK (CBO/DIT/LCOM), RQ×qualidade e scatter processo×CK ficam vazios
 * até correr Sprint 1 (--coleta-lote) + Sprint 2. Apresentação oral na aula é pedido do enunciado (fora do código).
 *
 * Pipeline único: por defeito este executável corre Sprint 1 → Sprint 2 → Sprint 3 (subprocessos dotnet).
 * Use --apenas-sprint3 para só regenerar relatório a partir do consolidado já existente.
 * --refetch força nova busca da lista (sem --skip-fetch na Sprint 1). Coleta CK: repasse --coleta-lote, --ck-jar=..., etc.
 * ═══════════════════════════════════════════════════════════════════════════
 */

internal sealed class RepoRow
{
    public string Nome { get; init; } = "";
    public double? Estrelas { get; init; }
    public double? Forks { get; init; }
    public double? Releases { get; init; }
    public double? IdadeAnos { get; init; }
    public double? DiskKb { get; init; }
    public double? LocJava { get; init; }
    public double? Comentarios { get; init; }
    public double? Classes { get; init; }
    public double? CboMedia { get; init; }
    public double? DitMedia { get; init; }
    public double? LcomMedia { get; init; }
    public double? CboMediana { get; init; }
    public double? DitMediana { get; init; }
    public double? LcomMediana { get; init; }

    public bool TemCk => CboMedia.HasValue && DitMedia.HasValue && LcomMedia.HasValue;
}

/// <summary>
/// LAB 02 — Sprint 3: sumarização (média/mediana/desvio), hipóteses, relatório Markdown,
/// análise das RQs, bônus (Pearson/Spearman + gráficos) e relatório final em PDF.
/// </summary>
static class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var pastaS3 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        // Entrada Lab02S02: um CSV com métricas de processo + (opcional) CK agregado por repositório.
        var consolidadoPadrao = Path.Combine(pastaS3, "..", "Sprint 2", "lab02_sprint2_output", "lab02_medicoes_consolidado.csv");
        var path = ObterArg(args, "--input") ?? consolidadoPadrao;
        path = Path.GetFullPath(path);

        // Saída: CSVs, Markdown, PNGs e PDF (recriada do zero a cada execução bem iniciada).
        var outDir = Path.Combine(pastaS3, "lab02_sprint3_output");
        var bonusDir = Path.Combine(outDir, "bonus");

        AnsiConsole.Write(new Panel(new Markup(
                "[bold]LAB 02[/] — Pipeline completo (S1 → S2 → S3)\n[dim]Enunciado 2 — basta [cyan]dotnet run[/] nesta pasta; [cyan]--apenas-sprint3[/] = só regenerar relatório.[/]"))
            .Header("[bold green]Java + CK[/]")
            .Border(BoxBorder.Rounded));

        var apenasS3 = args.Contains("--apenas-sprint3", StringComparer.OrdinalIgnoreCase);
        if (!apenasS3)
        {
            AnsiConsole.MarkupLine(
                "[dim]A correr Sprint 1 e 2 automaticamente. Para pular e só gerar PDF/Markdown:[/] [cyan]--apenas-sprint3[/]");
            if (!Lab02Pipeline.ExecutarSprint1E2(pastaS3, args))
            {
                AnsiConsole.MarkupLine("[red]Corrija os erros acima (ex.: token GitHub na Sprint 1) e tente novamente.[/]");
                return;
            }

            path = Path.GetFullPath(ObterArg(args, "--input") ?? consolidadoPadrao);
        }
        else
            AnsiConsole.MarkupLine("[yellow]--apenas-sprint3:[/] a ignorar Sprint 1 e 2.");

        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Arquivo não encontrado:[/] {Markup.Escape(path)}");
            if (apenasS3)
                AnsiConsole.MarkupLine("[dim]Corra sem [cyan]--apenas-sprint3[/] para gerar o consolidado, ou use [cyan]--input=[/].");
            else
                AnsiConsole.MarkupLine("[dim]A Sprint 2 devia ter criado o consolidado; verifique erros na Sprint 1 (lista repos).");
            return;
        }

        var rows = CarregarConsolidado(path);
        if (rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nenhuma linha válida no consolidado.[/]");
            return;
        }

        if (Directory.Exists(outDir))
        {
            Directory.Delete(outDir, recursive: true);
            AnsiConsole.MarkupLine("[dim]Pasta de saída anterior removida; geração a partir de zero.[/]");
        }

        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(bonusDir);

        var nComCk = rows.Count(r => r.TemCk);
        AnsiConsole.MarkupLine($"Repositórios no consolidado: [bold]{rows.Count}[/] | com métricas CK: [bold]{nComCk}[/]");

        // Lab02 — medidas centrais globais (complementa a sumarização "por repo" que veio no consolidado).
        EscreverResumoGlobal(rows, Path.Combine(outDir, "resumo_global_metricas.csv"));
        // Lab02 — RQ01–RQ04: comparação quartis processo × médias CK (precisa CK no consolidado).
        EscreverQuartisPorRq(rows, outDir);
        // Lab02 — Bônus: gráficos correlação + Pearson/Spearman + p-valor aproximado (antes do Markdown para embutir CSVs).
        ExecutarBonus(rows, bonusDir);
        GraficosRelatorioPdf.GerarTodos(rows, bonusDir);
        // Lab02 — relatório Markdown (inclui tabelas RQ + correlações dos ficheiros em bonus/).
        GerarRelatorioMarkdown(rows, nComCk, outDir, bonusDir, path);

        if (!args.Contains("--no-pdf", StringComparer.OrdinalIgnoreCase))
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var autores = ResolverAutores(args, pastaS3);
            try
            {
                // Mesmo conteúdo do relatório, consolidado em PDF (entrega tipo “relatório final”).
                var pdfPath = RelatorioLab02Pdf.Gerar(path, outDir, bonusDir, autores, rows, nComCk);
                AnsiConsole.MarkupLine($"[green]PDF:[/] [cyan]{Markup.Escape(pdfPath)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Falha ao gerar PDF:[/] {Markup.Escape(ex.Message)}");
            }
        }

        AnsiConsole.MarkupLine($"[green]Concluído.[/] Saída em [cyan]{Markup.Escape(outDir)}[/]");
    }

    /// <summary>Lê <c>lab02_medicoes_consolidado.csv</c> (Sprint 2): processo + colunas CK por repositório.</summary>
    private static List<RepoRow> CarregarConsolidado(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2) return new List<RepoRow>();
        var h = lines[0].Split(';');
        int I(string name)
        {
            for (var i = 0; i < h.Length; i++)
                if (string.Equals(h[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        var inome = I("nome_completo");
        var list = new List<RepoRow>();
        if (inome < 0) return list;

        double? P(string[] cols, int idx) =>
            idx >= 0 && idx < cols.Length && double.TryParse(cols[idx].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? v
                : null;

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(';');
            if (c.Length <= inome) continue;
            var nome = c[inome].Trim();
            if (string.IsNullOrEmpty(nome)) continue;

            var cbo = P(c, I("cbo_media"));
            var dit = P(c, I("dit_media"));
            var lcom = P(c, I("lcom_media"));
            list.Add(new RepoRow
            {
                Nome = nome,
                Estrelas = P(c, I("estrelas")),
                Forks = P(c, I("forks")),
                Releases = P(c, I("releases")),
                IdadeAnos = P(c, I("idade_anos")),
                DiskKb = P(c, I("disk_usage_kb")),
                LocJava = P(c, I("loc_java")),
                Comentarios = P(c, I("comentarios_linhas")),
                Classes = P(c, I("classes_analisadas")),
                CboMedia = cbo,
                DitMedia = dit,
                LcomMedia = lcom,
                CboMediana = P(c, I("cbo_mediana")),
                DitMediana = P(c, I("dit_mediana")),
                LcomMediana = P(c, I("lcom_mediana"))
            });
        }

        return list;
    }

    /// <summary>Estatísticas descritivas entre repositórios (média/mediana/dp de estrelas, idade, CK, etc.).</summary>
    private static void EscreverResumoGlobal(List<RepoRow> rows, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("metrica;n;media;mediana;desvio_padrao");
        void Linha(string nome, IEnumerable<double> vals)
        {
            var v = vals.Where(x => !double.IsNaN(x) && !double.IsInfinity(x)).ToList();
            if (v.Count == 0)
            {
                sb.AppendLine($"{nome};0;;;");
                return;
            }

            sb.AppendLine(string.Join(";", nome, v.Count,
                v.Average().ToString("F4", CultureInfo.InvariantCulture),
                MedianaLista(v).ToString("F4", CultureInfo.InvariantCulture),
                DesvioAmostral(v).ToString("F4", CultureInfo.InvariantCulture)));
        }

        Linha("estrelas", rows.Select(r => r.Estrelas).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("idade_anos", rows.Select(r => r.IdadeAnos).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("releases", rows.Select(r => r.Releases).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("forks", rows.Select(r => r.Forks).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("disk_usage_kb", rows.Select(r => r.DiskKb).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("loc_java", rows.Select(r => r.LocJava).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("linhas_comentario", rows.Select(r => r.Comentarios).Where(x => x.HasValue).Select(x => x!.Value));
        Linha("cbo_media_por_repo", rows.Where(r => r.CboMedia.HasValue).Select(r => r.CboMedia!.Value));
        Linha("dit_media_por_repo", rows.Where(r => r.DitMedia.HasValue).Select(r => r.DitMedia!.Value));
        Linha("lcom_media_por_repo", rows.Where(r => r.LcomMedia.HasValue).Select(r => r.LcomMedia!.Value));

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>RQ01–RQ04: quartis da variável de processo vs médias de CBO/DIT/LCOM (só repos com CK).</summary>
    private static void EscreverQuartisPorRq(List<RepoRow> rows, string outDir)
    {
        EscreverQuartilMetrica(rows, r => r.Estrelas, "RQ01_popularidade_estrelas",
            Path.Combine(outDir, "rq01_quartis_estrelas_vs_ck.csv"));
        EscreverQuartilMetrica(rows, r => r.IdadeAnos, "RQ02_maturidade_idade",
            Path.Combine(outDir, "rq02_quartis_idade_vs_ck.csv"));
        EscreverQuartilMetrica(rows, r => r.Releases, "RQ03_atividade_releases",
            Path.Combine(outDir, "rq03_quartis_releases_vs_ck.csv"));
        EscreverQuartilMetrica(rows, r => r.LocJava, "RQ04_tamanho_loc_java",
            Path.Combine(outDir, "rq04_quartis_loc_vs_ck.csv"));
    }

    private static void EscreverQuartilMetrica(
        List<RepoRow> rows,
        Func<RepoRow, double?> selector,
        string titulo,
        string path)
    {
        var comProcesso = rows.Where(r => selector(r).HasValue).ToList();
        if (comProcesso.Count < 4)
        {
            File.WriteAllText(path, $"# {titulo}\n# Poucos dados para quartis (n={comProcesso.Count}).\n", Encoding.UTF8);
            return;
        }

        var valores = comProcesso.Select(r => selector(r)!.Value).OrderBy(x => x).ToList();
        var q1 = valores[valores.Count / 4];
        var q2 = valores[valores.Count / 2];
        var q3 = valores[(valores.Count * 3) / 4];

        string Faixa(double v)
        {
            if (v <= q1) return "Q1_baixo";
            if (v <= q2) return "Q2";
            if (v <= q3) return "Q3";
            return "Q4_alto";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{titulo};quartil_processo;n_com_ck;cbo_media_media;cbo_media_mediana;dit_media_media;lcom_media_media");

        foreach (var faixa in new[] { "Q1_baixo", "Q2", "Q3", "Q4_alto" })
        {
            var grupo = comProcesso.Where(r => Faixa(selector(r)!.Value) == faixa).Where(r => r.TemCk).ToList();
            if (grupo.Count == 0)
            {
                sb.AppendLine($"{titulo};{faixa};0;;;;");
                continue;
            }

            var cbo = grupo.Select(r => r.CboMedia!.Value).ToList();
            var dit = grupo.Select(r => r.DitMedia!.Value).ToList();
            var lcom = grupo.Select(r => r.LcomMedia!.Value).ToList();
            sb.AppendLine(string.Join(";", titulo, faixa, grupo.Count,
                cbo.Average().ToString("F4", CultureInfo.InvariantCulture),
                MedianaLista(cbo).ToString("F4", CultureInfo.InvariantCulture),
                dit.Average().ToString("F4", CultureInfo.InvariantCulture),
                lcom.Average().ToString("F4", CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>Documento exigido pelo enunciado (Markdown espelhado no PDF): hipóteses, método, resultados, RQs, discussão, conclusões, entregáveis.</summary>
    private static void GerarRelatorioMarkdown(List<RepoRow> rows, int nComCk, string outDir, string bonusDir, string consolidadoPath)
    {
        var pathMd = Path.Combine(outDir, "RELATORIO_LAB02.md");
        var n = rows.Count;
        var est = rows.Where(r => r.Estrelas.HasValue).Select(r => r.Estrelas!.Value).ToList();
        var idade = rows.Where(r => r.IdadeAnos.HasValue).Select(r => r.IdadeAnos!.Value).ToList();
        var rel = rows.Where(r => r.Releases.HasValue).Select(r => r.Releases!.Value).ToList();
        var loc = rows.Where(r => r.LocJava.HasValue).Select(r => r.LocJava!.Value).ToList();
        var forks = rows.Where(r => r.Forks.HasValue).Select(r => r.Forks!.Value).ToList();
        var disk = rows.Where(r => r.DiskKb.HasValue).Select(r => r.DiskKb!.Value).ToList();
        var com = rows.Where(r => r.Comentarios.HasValue).Select(r => r.Comentarios!.Value).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# Laboratório 02 — Qualidade de sistemas Java (CK + GitHub)");
        sb.AppendLine();
        sb.AppendLine("## Mapeamento do enunciado (checklist)");
        sb.AppendLine();
        sb.AppendLine("| Pedido no LAB 02 | Onde está coberto |");
        sb.AppendLine("|------------------|-------------------|");
        sb.AppendLine("| Top repositórios Java + dados GitHub | Sprint 1 → `repos_java_1000.csv`; consolidado Sprint 2 |");
        sb.AppendLine("| Automação clone + métricas CK (CBO, DIT, LCOM) | Sprint 1: `--coleta-ck`, `--coleta-lote` |");
        sb.AppendLine("| CSV com medições (1000 repos) | Sprint 2 → `lab02_medicoes_consolidado.csv` |");
        sb.AppendLine("| Hipóteses informais | Secção 1 abaixo + PDF |");
        sb.AppendLine("| Metodologia | Secção 2 |");
        sb.AppendLine("| Resultados por RQ + sumarização | Secções 3–3.2, `resumo_global_metricas.csv`, `rq0*_*.csv` |");
        sb.AppendLine("| Discussão e conclusões | Secções 4–5 |");
        sb.AppendLine("| **Bônus:** gráficos + Pearson/Spearman | Secção 6; pasta `bonus/`; PDF secção 5–7 |");
        sb.AppendLine("| Entregáveis listados | Secção 8 abaixo |");
        sb.AppendLine("| Apresentação na aula de entrega | Preparação humana (slides) — não gerada por código |");
        sb.AppendLine();
        sb.AppendLine("## 1. Introdução e hipóteses (informais)");
        sb.AppendLine();
        sb.AppendLine("- **RQ01 (popularidade × qualidade):** hipótese — repositórios muito populares tendem a acumular código legado e integrações, podendo elevar acoplamento (CBO) e reduzir coesão (LCOM); por outro lado, projetos maduros podem investir em refatoração. Esperamos correlações fracas ou moderadas, não determinísticas.");
        sb.AppendLine("- **RQ02 (maturidade × qualidade):** hipótese — maior idade correlaciona-se com hierarquias mais profundas (DIT) e mais acoplamento histórico, mas também com mais oportunidades de saneamento.");
        sb.AppendLine("- **RQ03 (atividade × qualidade):** hipótese — mais *releases* sugere manutenção ativa, possivelmente associada a melhor organização modular (menos LCOM extremo), embora ritmo alto também possa introduzir dívidas técnicas.");
        sb.AppendLine("- **RQ04 (tamanho × qualidade):** hipótese — maior LOC costuma acompanhar mais classes e dependências, elevando CBO; LCOM pode subir em módulos mal particionados.");
        sb.AppendLine();
        sb.AppendLine("## 2. Metodologia");
        sb.AppendLine();
        sb.AppendLine("- Lista dos repositórios Java mais populares (GitHub) e métricas de processo: estrelas, idade (anos), releases, tamanho (LOC Java e linhas de comentário quando disponíveis via CK), `disk_usage_kb` quando presente na API.");
        sb.AppendLine("- Métricas de qualidade (CK): CBO, DIT e LCOM, sumarizadas **por repositório** (média, mediana e desvio entre classes), conforme orientação do enunciado.");
        sb.AppendLine("- Análise: estatísticas descritivas globais; estratificação por quartis das métricas de processo; correlação de Pearson e Spearman (bilateral) com p-valor aproximado via estatística *t* com *n−2* graus de liberdade (uso comum em laboratórios).");
        sb.AppendLine($"- **Ficheiro consolidado:** `{Path.GetFileName(consolidadoPath)}` (caminho absoluto na pasta Sprint 2 após `dotnet run` na Sprint 2).");
        sb.AppendLine($"- **Amostra deste relatório:** *n* = {n} repositórios na lista consolidada; repositórios com CK preenchido: **{nComCk}** (para correlações e quartis de qualidade, exige CK por repositório).");
        sb.AppendLine();
        sb.AppendLine("## 3. Resultados — processo (todos os repositórios da lista)");
        sb.AppendLine();
        sb.AppendLine("| Métrica | n | Média | Mediana | Desvio padrão |");
        sb.AppendLine("|---------|---|-------|---------|---------------|");
        void Tabela(string nome, List<double> v)
        {
            if (v.Count == 0)
                sb.AppendLine($"| {nome} | 0 | — | — | — |");
            else
                sb.AppendLine($"| {nome} | {v.Count} | {v.Average():F2} | {MedianaLista(v):F2} | {DesvioAmostral(v):F2} |");
        }

        Tabela("Estrelas", est);
        Tabela("Forks", forks);
        Tabela("Idade (anos)", idade);
        Tabela("Releases", rel);
        Tabela("Disco (KB, API)", disk);
        Tabela("LOC Java (onde medido)", loc);
        Tabela("Linhas de comentário (CK)", com);
        sb.AppendLine();

        sb.AppendLine("### 3.0 Correlações exploratórias (somente métricas de processo)");
        sb.AppendLine();
        sb.AppendLine("Relação entre variáveis de processo (Pearson / Spearman, *p* bilateral aproximado):");
        sb.AppendLine();
        EscreverLinhaCorrelacao(sb, rows, r => r.Estrelas, r => r.IdadeAnos, "Estrelas", "Idade (anos)");
        EscreverLinhaCorrelacao(sb, rows, r => r.Estrelas, r => r.Releases, "Estrelas", "Releases");
        EscreverLinhaCorrelacao(sb, rows, r => r.IdadeAnos, r => r.Releases, "Idade (anos)", "Releases");
        sb.AppendLine();

        if (nComCk > 0)
        {
            var cbo = rows.Where(r => r.CboMedia.HasValue).Select(r => r.CboMedia!.Value).ToList();
            var dit = rows.Where(r => r.DitMedia.HasValue).Select(r => r.DitMedia!.Value).ToList();
            var lcom = rows.Where(r => r.LcomMedia.HasValue).Select(r => r.LcomMedia!.Value).ToList();
            sb.AppendLine("## 3.1 Qualidade (CK) — repositórios com medição");
            sb.AppendLine();
            Tabela("CBO (média por repo)", cbo);
            Tabela("DIT (média por repo)", dit);
            Tabela("LCOM (média por repo)", lcom);
            sb.AppendLine();
        }
        else
            sb.AppendLine("> **Nota:** não há linhas com CBO/DIT/LCOM preenchidos no consolidado. Rode a coleta CK (Sprint 1: `--coleta-lote` com `ck.jar`) e regenere o consolidado na Sprint 2 para habilitar as comparações RQ01–RQ04 com qualidade.");
        sb.AppendLine();

        sb.AppendLine("## 3.2 Resultados por questão de pesquisa (quartis de processo × médias CK)");
        sb.AppendLine();
        sb.AppendLine("Tabelas geradas a partir dos CSV `rq01`–`rq04` (colunas vazias ou *—* quando `n_com_ck = 0`).");
        sb.AppendLine();
        AnexarTabelaMarkdownDeCsv(sb, "RQ01 — popularidade (estrelas)", Path.Combine(outDir, "rq01_quartis_estrelas_vs_ck.csv"));
        AnexarTabelaMarkdownDeCsv(sb, "RQ02 — maturidade (idade)", Path.Combine(outDir, "rq02_quartis_idade_vs_ck.csv"));
        AnexarTabelaMarkdownDeCsv(sb, "RQ03 — atividade (releases)", Path.Combine(outDir, "rq03_quartis_releases_vs_ck.csv"));
        AnexarTabelaMarkdownDeCsv(sb, "RQ04 — tamanho (LOC Java)", Path.Combine(outDir, "rq04_quartis_loc_vs_ck.csv"));

        sb.AppendLine("### 3.3 Correlações (ficheiros CSV em `bonus/`)");
        sb.AppendLine();
        AnexarTabelaMarkdownDeCsv(sb, "Processo × processo", Path.Combine(bonusDir, "correlacoes_apenas_processo.csv"));
        AnexarTabelaMarkdownDeCsv(sb, "Processo × qualidade (CK)", Path.Combine(bonusDir, "correlacoes_processo_vs_ck.csv"));

        sb.AppendLine("## 4. Discussão (hipóteses × dados observados)");
        sb.AppendLine();
        sb.AppendLine("- Com **apenas métricas de processo**, observamos a distribuição de popularidade, idade e *releases* no *top* Java; ela é fortemente assimétrica (poucos repositórios concentram muitas estrelas).");
        if (nComCk >= 10)
            sb.AppendLine("- Com **CK disponível para vários repositórios**, as tabelas por quartil e os gráficos em `lab02_sprint3_output/bonus/` permitem confrontar se faixas de popularidade/maturidade/atividade/tamanho acompanham diferenças nas médias de CBO/DIT/LCOM.");
        else
            sb.AppendLine("- Para **validar** as hipóteses sobre qualidade, é necessário completar o consolidado com CK por repositório (Sprint 1 em lote + Sprint 2). Os ficheiros `rq0*_quartis_*.csv` preenchem as médias por quartil quando houver sobreposição entre processo e CK.");
        sb.AppendLine("- **Limitações:** métricas CK dependem do escaneamento do CK na árvore de fontes; comparações entre repositórios com estruturas diferentes exigem cautela. *Releases* podem estar subcontados consoante o endpoint da API usado na Sprint 1.");
        sb.AppendLine();

        sb.AppendLine("## 5. Conclusões");
        sb.AppendLine();
        if (nComCk == 0)
        {
            sb.AppendLine("- O pipeline de **dados de processo** (GitHub) está materializado no consolidado e nas estatísticas globais.");
            sb.AppendLine("- As **questões de pesquisa sobre qualidade (CK)** ficam em aberto neste relatório até existirem medições CK fundidas na Sprint 2; o código e os CSVs já produzem a estrutura (quartis e correlações) pronta para receber esses valores.");
            sb.AppendLine("- Recomendação: executar `--coleta-lote` na Sprint 1 (com espaço em disco e tempo), depois Sprint 2 e voltar a correr a Sprint 3 para um relatório completo com evidência numérica RQ01–RQ04.");
        }
        else if (nComCk < n)
        {
            sb.AppendLine($"- Dispõe-se de CK para **{nComCk}** de **{n}** repositórios: as inferências sobre qualidade aplicam-se a este subconjunto (possível viés de seleção se a coleta não for aleatória).");
            sb.AppendLine("- As tabelas por quartil e as correlações processo×CK permitem uma leitura exploratória das RQs; significado prático exige interpretação no contexto de cada métrica CK.");
        }
        else
        {
            sb.AppendLine("- O consolidado inclui CK para **todos** os repositórios analisados: as RQ01–RQ04 podem ser confrontadas com a amostra completa.");
            sb.AppendLine("- Combine a leitura das correlações com os gráficos em `bonus/` e com as diferenças entre quartis para sustentar a discussão oral.");
        }

        sb.AppendLine();
        sb.AppendLine("## 6. Bônus (figuras e testes de correlação)");
        sb.AppendLine();
        sb.AppendLine("- Gráficos PNG: pasta `bonus/` (`pdf_*.png` agregados + `scatter_*.png` quando há pares válidos, *n* ≥ 4).");
        sb.AppendLine("- Relatório final em PDF: `Relatorio_Final_Lab02.pdf` (gerado na mesma pasta que este Markdown).");
        sb.AppendLine();
        sb.AppendLine("## 7. Referências");
        sb.AppendLine();
        sb.AppendLine("- Aniche, M. **CK** (Chidamber & Kemerer). https://github.com/mauricioaniche/ck");
        sb.AppendLine("- GitHub REST / GraphQL API. https://docs.github.com/");
        sb.AppendLine("- MathNet.Numerics (correlações). https://github.com/mathnet/mathnet-numerics");
        sb.AppendLine();
        sb.AppendLine("## 8. Entregáveis gerados (pasta `lab02_sprint3_output/`)");
        sb.AppendLine();
        sb.AppendLine("- `RELATORIO_LAB02.md` — este relatório.");
        sb.AppendLine("- `Relatorio_Final_Lab02.pdf` — versão PDF (se não usar `--no-pdf`).");
        sb.AppendLine("- `resumo_global_metricas.csv` — estatísticas globais.");
        sb.AppendLine("- `rq01_quartis_estrelas_vs_ck.csv` … `rq04_quartis_loc_vs_ck.csv` — estratificação por quartis.");
        sb.AppendLine("- `bonus/correlacoes_*.csv`, `bonus/*.png` — bônus.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*Gerado automaticamente pela Sprint 3 (`dotnet run`).*");

        File.WriteAllText(pathMd, sb.ToString(), Encoding.UTF8);
    }

    private static void EscreverLinhaCorrelacao(
        StringBuilder sb,
        List<RepoRow> rows,
        Func<RepoRow, double?> fx,
        Func<RepoRow, double?> fy,
        string rotuloX,
        string rotuloY)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        foreach (var r in rows)
        {
            var xv = fx(r);
            var yv = fy(r);
            if (!xv.HasValue || !yv.HasValue) continue;
            xs.Add(xv.Value);
            ys.Add(yv.Value);
        }

        if (xs.Count < 4)
        {
            sb.AppendLine($"- **{rotuloX} × {rotuloY}:** dados insuficientes.");
            return;
        }

        var xa = xs.ToArray();
        var ya = ys.ToArray();
        var pr = Correlation.Pearson(xa, ya);
        var sr = Spearman(xa, ya);
        sb.AppendLine(
            $"- **{rotuloX} × {rotuloY}** (*n* = {xa.Length}): Pearson *r* = {pr:F4} (*p* ≈ {PValorPearson(pr, xa.Length):E3}); " +
            $"Spearman ρ = {sr:F4} (*p* ≈ {PValorSpearman(sr, xa.Length):E3}).");
    }

    /// <summary>Bônus: dispersões + <c>correlacoes_*.csv</c> (MathNet Pearson/Spearman; p bilateral ~ StudentT n−2).</summary>
    private static void ExecutarBonus(List<RepoRow> rows, string bonusDir)
    {
        (string nomeX, string nomeY, Func<RepoRow, double?> fx, Func<RepoRow, double?> fy)[] pares =
        [
            ("estrelas", "cbo_media", r => r.Estrelas, r => r.CboMedia),
            ("estrelas", "dit_media", r => r.Estrelas, r => r.DitMedia),
            ("estrelas", "lcom_media", r => r.Estrelas, r => r.LcomMedia),
            ("idade_anos", "cbo_media", r => r.IdadeAnos, r => r.CboMedia),
            ("idade_anos", "dit_media", r => r.IdadeAnos, r => r.DitMedia),
            ("idade_anos", "lcom_media", r => r.IdadeAnos, r => r.LcomMedia),
            ("releases", "cbo_media", r => r.Releases, r => r.CboMedia),
            ("releases", "dit_media", r => r.Releases, r => r.DitMedia),
            ("releases", "lcom_media", r => r.Releases, r => r.LcomMedia),
            ("loc_java", "cbo_media", r => r.LocJava, r => r.CboMedia),
            ("loc_java", "dit_media", r => r.LocJava, r => r.DitMedia),
            ("loc_java", "lcom_media", r => r.LocJava, r => r.LcomMedia),
            ("disk_usage_kb", "cbo_media", r => r.DiskKb, r => r.CboMedia),
            ("disk_usage_kb", "dit_media", r => r.DiskKb, r => r.DitMedia),
            ("disk_usage_kb", "lcom_media", r => r.DiskKb, r => r.LcomMedia)
        ];

        var rel = new StringBuilder();
        rel.AppendLine("x;y;pearson_r;pearson_p_aprox;spearman_rho;spearman_p_aprox;n");

        foreach (var (nomeX, nomeY, fx, fy) in pares)
        {
            var xs = new List<double>();
            var ys = new List<double>();
            foreach (var r in rows)
            {
                var xv = fx(r);
                var yv = fy(r);
                if (!xv.HasValue || !yv.HasValue) continue;
                xs.Add(xv.Value);
                ys.Add(yv.Value);
            }

            if (xs.Count < 4) continue;

            var xa = xs.ToArray();
            var ya = ys.ToArray();
            var pr = Correlation.Pearson(xa, ya);
            var sr = Spearman(xa, ya);
            rel.AppendLine(string.Join(";", nomeX, nomeY,
                pr.ToString("F4", CultureInfo.InvariantCulture),
                PValorPearson(pr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                sr.ToString("F4", CultureInfo.InvariantCulture),
                PValorSpearman(sr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                xa.Length));

            var nomeArq = $"scatter_{nomeX}_{nomeY}.png";
            try
            {
                var plt = new ScottPlot.Plot();
                plt.Add.Scatter(xa, ya);
                plt.XLabel(nomeX);
                plt.YLabel(nomeY);
                plt.Title($"Correlação: {nomeX} × {nomeY} (n={xa.Length})");
                plt.SavePng(Path.Combine(bonusDir, nomeArq), 900, 600);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Gráfico {nomeX}/{nomeY}:[/] {Markup.Escape(ex.Message)}");
            }
        }

        File.WriteAllText(Path.Combine(bonusDir, "correlacoes_processo_vs_ck.csv"), rel.ToString(), Encoding.UTF8);

        var procOnly = new List<(string a, string b, double[] xa, double[] xb)>();
        void AddProc(Func<RepoRow, double?> f1, Func<RepoRow, double?> f2, string n1, string n2)
        {
            var a = new List<double>();
            var b = new List<double>();
            foreach (var r in rows)
            {
                var v1 = f1(r);
                var v2 = f2(r);
                if (!v1.HasValue || !v2.HasValue) continue;
                a.Add(v1.Value);
                b.Add(v2.Value);
            }

            if (a.Count >= 4)
                procOnly.Add((n1, n2, a.ToArray(), b.ToArray()));
        }

        AddProc(r => r.Estrelas, r => r.IdadeAnos, "estrelas", "idade_anos");
        AddProc(r => r.Estrelas, r => r.Releases, "estrelas", "releases");
        AddProc(r => r.IdadeAnos, r => r.Releases, "idade_anos", "releases");

        var sbProc = new StringBuilder();
        sbProc.AppendLine("x;y;pearson_r;pearson_p_aprox;spearman_rho;spearman_p_aprox;n");
        foreach (var (na, nb, xa, xb) in procOnly)
        {
            var pr = Correlation.Pearson(xa, xb);
            var sr = Spearman(xa, xb);
            sbProc.AppendLine(string.Join(";", na, nb,
                pr.ToString("F4", CultureInfo.InvariantCulture),
                PValorPearson(pr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                sr.ToString("F4", CultureInfo.InvariantCulture),
                PValorSpearman(sr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                xa.Length));
            try
            {
                var plt = new ScottPlot.Plot();
                plt.Add.Scatter(xa, xb);
                plt.XLabel(na);
                plt.YLabel(nb);
                plt.Title($"Exploratório (processo): {na} × {nb}");
                plt.SavePng(Path.Combine(bonusDir, $"scatter_proc_{na}_{nb}.png"), 900, 600);
            }
            catch { /* ignore */ }
        }

        File.WriteAllText(Path.Combine(bonusDir, "correlacoes_apenas_processo.csv"), sbProc.ToString(), Encoding.UTF8);
    }

    private static double Spearman(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2) return double.NaN;
        return Correlation.Pearson(Rank(x), Rank(y));
    }

    private static double[] Rank(double[] values)
    {
        var n = values.Length;
        var indexed = values.Select((v, i) => (v, i)).OrderBy(t => t.v).ToArray();
        var ranks = new double[n];
        for (var k = 0; k < n;)
        {
            var j = k;
            while (j < n && indexed[j].v == indexed[k].v) j++;
            var avgRank = (k + 1 + j) / 2.0;
            for (var t = k; t < j; t++)
                ranks[indexed[t].i] = avgRank;
            k = j;
        }

        return ranks;
    }

    private static double PValorPearson(double r, int n)
    {
        if (n <= 3 || double.IsNaN(r)) return double.NaN;
        var df = n - 2;
        var den = Math.Sqrt(Math.Max(1e-15, 1 - r * r));
        var tStat = Math.Abs(r) * Math.Sqrt(df) / den;
        var t = new StudentT(0, 1, df);
        var cdf = t.CumulativeDistribution(tStat);
        return 2 * Math.Min(cdf, 1 - cdf);
    }

    private static double PValorSpearman(double rho, int n)
    {
        if (n <= 3 || double.IsNaN(rho)) return double.NaN;
        var df = n - 2;
        var den = Math.Sqrt(Math.Max(1e-15, 1 - rho * rho));
        var tStat = Math.Abs(rho) * Math.Sqrt(df) / den;
        var t = new StudentT(0, 1, df);
        var cdf = t.CumulativeDistribution(tStat);
        return 2 * Math.Min(cdf, 1 - cdf);
    }

    private static double MedianaLista(List<double> v)
    {
        if (v.Count == 0) return double.NaN;
        var o = v.OrderBy(x => x).ToList();
        var n = o.Count;
        return n % 2 == 1 ? o[n / 2] : (o[n / 2 - 1] + o[n / 2]) / 2;
    }

    private static double DesvioAmostral(List<double> v)
    {
        if (v.Count < 2) return 0;
        var m = v.Average();
        return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Count - 1));
    }

    private static string? ObterArg(string[] args, string flag)
    {
        var eq = flag + "=";
        foreach (var a in args)
        {
            if (a.StartsWith(eq, StringComparison.OrdinalIgnoreCase))
                return a[eq.Length..].Trim().Trim('"');
        }

        return null;
    }

    /// <summary>Ordem: <c>--autores=</c>, variável <c>LAB02_AUTORES</c>, ficheiro <c>lab02_autores.txt</c> na pasta da Sprint 3.</summary>
    private static string ResolverAutores(string[] args, string pastaSprint3)
    {
        var a = ObterArg(args, "--autores");
        if (!string.IsNullOrWhiteSpace(a)) return a.Trim();
        a = Environment.GetEnvironmentVariable("LAB02_AUTORES");
        if (!string.IsNullOrWhiteSpace(a)) return a.Trim();
        var f = Path.Combine(pastaSprint3, "lab02_autores.txt");
        if (File.Exists(f))
        {
            foreach (var linha in File.ReadAllLines(f, Encoding.UTF8))
            {
                var t = linha.Trim();
                if (t.Length == 0 || t.StartsWith('#')) continue;
                return t;
            }
        }

        return "Acadêmicos: [preencher nomes do grupo]";
    }

    private static string MdCelula(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "—";
        return texto.Replace('|', '/').Replace('\n', ' ').Trim();
    }

    private static void AnexarTabelaMarkdownDeCsv(StringBuilder sb, string titulo, string csvPath)
    {
        if (!File.Exists(csvPath)) return;
        var linhas = new List<string[]>();
        foreach (var line in File.ReadAllLines(csvPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            if (!line.Contains(';')) continue;
            linhas.Add(line.Split(';'));
        }

        if (linhas.Count == 0) return;
        sb.AppendLine($"#### {titulo}");
        sb.AppendLine();
        var cols = linhas[0].Length;
        sb.Append('|');
        foreach (var h in linhas[0])
            sb.Append(' ').Append(MdCelula(h)).Append(" |");
        sb.AppendLine();
        sb.Append('|');
        for (var i = 0; i < cols; i++)
            sb.Append("---|");
        sb.AppendLine();
        foreach (var row in linhas.Skip(1))
        {
            sb.Append('|');
            for (var i = 0; i < cols; i++)
            {
                var cell = i < row.Length ? row[i] : "";
                sb.Append(' ').Append(MdCelula(string.IsNullOrWhiteSpace(cell) ? "—" : cell)).Append(" |");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
    }
}

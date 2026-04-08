using System.Globalization;
using System.Text;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Enunciado2.Sprint3;

/// <summary>Gera o PDF final do LAB 02 (QuestPDF), chamado ao final da Sprint 3.</summary>
/// <remarks>
/// Replica no PDF o pedido de “relatório final”: secções 1–6 alinhadas ao enunciado (intro, método, resultados RQ, bônus, discussão).
/// Secção 7: figuras em bonus/ — prefixo pdf_* (GraficosRelatorioPdf após ExecutarBonus), dispersões scatter_* e correlações em CSV. Correlações: lê CSV ou recalcula se só houver cabeçalho.
/// </remarks>
internal static class RelatorioLab02Pdf
{
    private static int OrdemFiguraPdf(string caminho)
    {
        var n = Path.GetFileName(caminho);
        if (n.StartsWith("pdf_", StringComparison.OrdinalIgnoreCase)) return 0;
        if (n.StartsWith("scatter_proc_", StringComparison.OrdinalIgnoreCase)) return 1;
        if (n.StartsWith("scatter_", StringComparison.OrdinalIgnoreCase)) return 2;
        return 3;
    }

    private const string TituloLab = "LAB 02 — Qualidade de sistemas Java (CK + GitHub)";
    private const string Subtitulo = "Relatório final — Enunciado 2";
    private const string Disciplina = "Laboratório de Experimentação de Software";

    /// <summary>Escreve <c>Relatorio_Final_Lab02.pdf</c> em <paramref name="outDir"/>.</summary>
    public static string Gerar(
        string consolidadoPath,
        string outDir,
        string bonusDir,
        string autores,
        List<RepoRow> rows,
        int nCk)
    {
        var pngs = Directory.Exists(bonusDir)
            ? Directory.GetFiles(bonusDir, "*.png", SearchOption.TopDirectoryOnly)
                .OrderBy(OrdemFiguraPdf)
                .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : new List<string>();

        var corrProc = LerCsvLinhas(Path.Combine(bonusDir, "correlacoes_apenas_processo.csv"));
        var corrCk = LerCsvLinhas(Path.Combine(bonusDir, "correlacoes_processo_vs_ck.csv"));

        if (corrProc.Count <= 1)
            corrProc = CorrelacoesProcessoComputadas(rows);
        if (corrCk.Count <= 1)
            corrCk = CorrelacoesCkComputadas(rows);

        var rqTabelas = new List<(string Titulo, List<string[]> Linhas)>();
        foreach (var nome in new[]
                 {
                     "rq01_quartis_estrelas_vs_ck.csv", "rq02_quartis_idade_vs_ck.csv",
                     "rq03_quartis_releases_vs_ck.csv", "rq04_quartis_loc_vs_ck.csv"
                 })
        {
            var p = Path.Combine(outDir, nome);
            if (!File.Exists(p)) continue;
            var linhas = LerCsvLinhas(p);
            if (linhas.Count > 0)
                rqTabelas.Add((nome.Replace(".csv", "", StringComparison.OrdinalIgnoreCase), linhas));
        }

        var pdfPath = Path.Combine(outDir, "Relatorio_Final_Lab02.pdf");
        var doc = new RelatorioLab02Document(
            autores,
            rows,
            nCk,
            corrProc,
            corrCk,
            rqTabelas,
            pngs,
            consolidadoPath,
            outDir);

        doc.GeneratePdf(pdfPath);
        return pdfPath;
    }

    private static List<string[]> LerCsvLinhas(string path)
    {
        var list = new List<string[]>();
        if (!File.Exists(path)) return list;
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
            if (line.Contains(';'))
                list.Add(line.Split(';'));
        }

        return list;
    }

    private static List<string[]> CorrelacoesProcessoComputadas(List<RepoRow> rows)
    {
        var head = new[] { "x", "y", "pearson_r", "pearson_p_aprox", "spearman_rho", "spearman_p_aprox", "n" };
        var list = new List<string[]> { head };
        void Add(Func<RepoRow, double?> fx, Func<RepoRow, double?> fy, string nx, string ny)
        {
            var xs = new List<double>();
            var ys = new List<double>();
            foreach (var r in rows)
            {
                var a = fx(r);
                var b = fy(r);
                if (!a.HasValue || !b.HasValue) continue;
                xs.Add(a.Value);
                ys.Add(b.Value);
            }

            if (xs.Count < 4) return;
            var xa = xs.ToArray();
            var ya = ys.ToArray();
            var pr = Correlation.Pearson(xa, ya);
            var sr = Spearman(xa, ya);
            list.Add(new[]
            {
                nx, ny,
                pr.ToString("F4", CultureInfo.InvariantCulture),
                PValorAprox(pr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                sr.ToString("F4", CultureInfo.InvariantCulture),
                PValorAprox(sr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                xa.Length.ToString(CultureInfo.InvariantCulture)
            });
        }

        Add(r => r.Estrelas, r => r.IdadeAnos, "estrelas", "idade_anos");
        Add(r => r.Estrelas, r => r.Releases, "estrelas", "releases");
        Add(r => r.IdadeAnos, r => r.Releases, "idade_anos", "releases");
        return list.Count > 1 ? list : new List<string[]>();
    }

    private static List<string[]> CorrelacoesCkComputadas(List<RepoRow> rows)
    {
        var head = new[] { "x", "y", "pearson_r", "pearson_p_aprox", "spearman_rho", "spearman_p_aprox", "n" };
        var list = new List<string[]> { head };
        (string nx, string ny, Func<RepoRow, double?> fx, Func<RepoRow, double?> fy)[] pares =
        [
            ("estrelas", "cbo_media", r => r.Estrelas, r => r.CboMedia),
            ("estrelas", "dit_media", r => r.Estrelas, r => r.DitMedia),
            ("estrelas", "lcom_media", r => r.Estrelas, r => r.LcomMedia),
            ("idade_anos", "cbo_media", r => r.IdadeAnos, r => r.CboMedia),
            ("releases", "cbo_media", r => r.Releases, r => r.CboMedia),
            ("loc_java", "cbo_media", r => r.LocJava, r => r.CboMedia)
        ];

        foreach (var (nx, ny, fx, fy) in pares)
        {
            var xs = new List<double>();
            var ys = new List<double>();
            foreach (var r in rows)
            {
                var a = fx(r);
                var b = fy(r);
                if (!a.HasValue || !b.HasValue) continue;
                xs.Add(a.Value);
                ys.Add(b.Value);
            }

            if (xs.Count < 4) continue;
            var xa = xs.ToArray();
            var ya = ys.ToArray();
            var pr = Correlation.Pearson(xa, ya);
            var sr = Spearman(xa, ya);
            list.Add(new[]
            {
                nx, ny,
                pr.ToString("F4", CultureInfo.InvariantCulture),
                PValorAprox(pr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                sr.ToString("F4", CultureInfo.InvariantCulture),
                PValorAprox(sr, xa.Length).ToString("E3", CultureInfo.InvariantCulture),
                xa.Length.ToString(CultureInfo.InvariantCulture)
            });
        }

        return list.Count > 1 ? list : new List<string[]>();
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

    private static double PValorAprox(double r, int n)
    {
        if (n <= 3 || double.IsNaN(r)) return double.NaN;
        var df = n - 2;
        var den = Math.Sqrt(Math.Max(1e-15, 1 - r * r));
        var tStat = Math.Abs(r) * Math.Sqrt(df) / den;
        var t = new StudentT(0, 1, df);
        var cdf = t.CumulativeDistribution(tStat);
        return 2 * Math.Min(cdf, 1 - cdf);
    }

    private sealed class RelatorioLab02Document : IDocument
    {
        private readonly string _autores;
        private readonly List<RepoRow> _rows;
        private readonly int _nCk;
        private readonly List<string[]> _corrProc;
        private readonly List<string[]> _corrCk;
        private readonly List<(string Titulo, List<string[]> Linhas)> _rqTabelas;
        private readonly List<string> _pngs;
        private readonly string _consolidadoPath;
        private readonly string _saidaPath;

        public RelatorioLab02Document(
            string autores,
            List<RepoRow> rows,
            int nCk,
            List<string[]> corrProc,
            List<string[]> corrCk,
            List<(string Titulo, List<string[]> Linhas)> rqTabelas,
            List<string> pngs,
            string consolidadoPath,
            string saidaPath)
        {
            _autores = autores;
            _rows = rows;
            _nCk = nCk;
            _corrProc = corrProc;
            _corrCk = corrCk;
            _rqTabelas = rqTabelas;
            _pngs = pngs;
            _consolidadoPath = consolidadoPath;
            _saidaPath = saidaPath;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        }

        private void ComposeHeader(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Text(TituloLab).FontSize(16).Bold().FontColor(Colors.Orange.Darken2);
                col.Item().Text(Subtitulo).FontSize(12).SemiBold();
                col.Item().Text(Disciplina).FontSize(10).FontColor(Colors.Grey.Darken2);
                col.Item().Text(_autores).FontSize(10).Bold();
                col.Item().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} • n = {_rows.Count} repositórios • com CK: {_nCk}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(BoxIntro);
                col.Item().Element(BoxMetodologia);
                col.Item().Element(BoxMetricasProcesso);
                if (_nCk > 0)
                    col.Item().Element(BoxMetricasCk);

                col.Item().Element(BoxRqs);
                col.Item().Element(BoxBonus);
                col.Item().Element(BoxDiscussao);
                col.Item().PageBreak();
                col.Item().Element(BoxFiguras);
                col.Item().Element(BoxReferencias);
            });
        }

        private void BoxIntro(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Orange.Lighten3).Padding(10).Background(Colors.Orange.Lighten5).Column(col =>
            {
                col.Item().Text("1. Introdução e hipóteses informais").Bold().FontSize(13).FontColor(Colors.Orange.Darken3);
                col.Item().Text(
                    "Este relatório atende ao Laboratório 02: estudo das características de qualidade (CBO, DIT, LCOM via CK) " +
                    "em repositórios Java populares, relacionando-as a métricas de processo (popularidade, maturidade, atividade, tamanho).");
                col.Item().PaddingTop(4).Text("Questões de pesquisa e hipóteses (informais):").SemiBold();
                col.Item().Text("• RQ01 — Popularidade (estrelas) × qualidade: projetos muito populares podem acumular legado e acoplamento; espera-se correlação fraca a moderada.");
                col.Item().Text("• RQ02 — Maturidade (idade) × qualidade: maior idade pode associar-se a hierarquias mais profundas (DIT) e histórico de dependências.");
                col.Item().Text("• RQ03 — Atividade (releases) × qualidade: mais releases sugere manutenção ativa, com efeitos ambíguos em coesão/acoplamento.");
                col.Item().Text("• RQ04 — Tamanho (LOC Java / comentários) × qualidade: maior base de código tende a elevar acoplamento se o desenho modular não acompanhar.");
            });
        }

        private void BoxMetodologia(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("2. Metodologia").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("• Seleção dos 1.000 repositórios Java mais populares no GitHub (API GraphQL ou REST).");
                col.Item().Text("• Métricas de processo: estrelas; idade (anos); número de releases; tamanho em disco (quando disponível); LOC Java e linhas de comentário estimadas na coleta CK.");
                col.Item().Text("• Métricas de qualidade: CK (CBO, DIT, LCOM) com sumarização por repositório — média, mediana e desvio padrão entre classes.");
                col.Item().Text("• Análise: estatísticas descritivas globais; estratificação por quartis das variáveis de processo; correlações de Pearson e Spearman com p-valor bilateral aproximado (t com n−2 g.l.).");
                col.Item().PaddingTop(4).Text($"Fonte de dados consolidada: {Path.GetFileName(_consolidadoPath)}").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
            });
        }

        private void BoxMetricasProcesso(IContainer c)
        {
            var e = _rows.Where(x => x.Estrelas.HasValue).Select(x => x.Estrelas!.Value).ToList();
            var fk = _rows.Where(x => x.Forks.HasValue).Select(x => x.Forks!.Value).ToList();
            var id = _rows.Where(x => x.IdadeAnos.HasValue).Select(x => x.IdadeAnos!.Value).ToList();
            var r = _rows.Where(x => x.Releases.HasValue).Select(x => x.Releases!.Value).ToList();
            var dk = _rows.Where(x => x.DiskKb.HasValue).Select(x => x.DiskKb!.Value).ToList();
            var loc = _rows.Where(x => x.LocJava.HasValue).Select(x => x.LocJava!.Value).ToList();
            var com = _rows.Where(x => x.Comentarios.HasValue).Select(x => x.Comentarios!.Value).ToList();

            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("3. Resultados — métricas de processo (lista completa)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("Sumarização global (média, mediana e desvio padrão), alinhada ao CSV resumo_global_metricas.csv.").FontSize(9).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(6).Table(t =>
                {
                    t.ColumnsDefinition(d =>
                    {
                        d.RelativeColumn(2f);
                        d.RelativeColumn(0.7f);
                        d.RelativeColumn(1.1f);
                        d.RelativeColumn(1.1f);
                        d.RelativeColumn(1.1f);
                    });
                    Cabecalho(t, "Métrica", "n", "Média", "Mediana", "Desvio padrão");
                    LinhaStat(t, "Estrelas", e);
                    LinhaStat(t, "Forks", fk);
                    LinhaStat(t, "Idade (anos)", id);
                    LinhaStat(t, "Releases", r);
                    LinhaStat(t, "Disco (KB)", dk);
                    LinhaStat(t, "LOC Java", loc);
                    LinhaStat(t, "Comentários (linhas)", com);
                });
                col.Item().PaddingTop(10).Text(MontarInterpretacaoProcesso(e, id, r)).FontSize(9).LineHeight(1.25f);
            });
        }

        private void BoxMetricasCk(IContainer c)
        {
            var cbo = _rows.Where(x => x.CboMedia.HasValue).Select(x => x.CboMedia!.Value).ToList();
            var dit = _rows.Where(x => x.DitMedia.HasValue).Select(x => x.DitMedia!.Value).ToList();
            var lcom = _rows.Where(x => x.LcomMedia.HasValue).Select(x => x.LcomMedia!.Value).ToList();

            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("3.1 Qualidade (CK) — repositórios medidos").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(6).Table(t =>
                {
                    t.ColumnsDefinition(d =>
                    {
                        d.RelativeColumn(2f);
                        d.RelativeColumn(0.7f);
                        d.RelativeColumn(1.1f);
                        d.RelativeColumn(1.1f);
                        d.RelativeColumn(1.1f);
                    });
                    Cabecalho(t, "Métrica CK", "n", "Média", "Mediana", "Desvio padrão");
                    LinhaStat(t, "CBO (média/repo)", cbo);
                    LinhaStat(t, "DIT (média/repo)", dit);
                    LinhaStat(t, "LCOM (média/repo)", lcom);
                });
            });
        }

        private void BoxRqs(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("4. Resultados por questão de pesquisa (estratificação por quartis)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text(
                    "Para cada RQ, comparam-se quartis da métrica de processo com médias de CBO/DIT/LCOM nos repositórios que possuem CK. " +
                    "Se n_com_ck = 0 em todos os quartis, complete a coleta CK em lote (Sprint 1: --coleta-lote), regenere Sprint 2 e volte a executar a Sprint 3.")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                if (_rqTabelas.Count == 0)
                {
                    col.Item().PaddingTop(4).Text("Tabelas RQ vazias ou ausentes — necessário consolidado com métricas CK.").Italic();
                    return;
                }

                foreach (var (titulo, linhas) in _rqTabelas)
                {
                    col.Item().PaddingTop(8).Text(titulo.Replace('_', ' ')).SemiBold();
                    col.Item().Table(t => TabelaGenerica(t, linhas));
                }
            });
        }

        private void BoxBonus(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Purple.Lighten3).Padding(10).Background(Colors.Purple.Lighten5).Column(col =>
            {
                col.Item().Text("5. Bônus — correlação (Pearson / Spearman) e figuras").Bold().FontSize(13).FontColor(Colors.Purple.Darken3);
                col.Item().Text("Testes de correlação com p-valor aproximado; gráficos de dispersão na secção de figuras (gerados nesta Sprint 3).").FontSize(9);

                col.Item().PaddingTop(6).Text("5.1 Processo × processo (exploratório)").SemiBold();
                if (_corrProc.Count > 0)
                    col.Item().Table(t => TabelaGenerica(t, _corrProc));
                else
                    col.Item().Text("—").Italic();

                col.Item().PaddingTop(8).Text("5.2 Processo × qualidade (CK)").SemiBold();
                if (_corrCk.Count > 0)
                    col.Item().Table(t => TabelaGenerica(t, _corrCk));
                else
                    col.Item().Text("Sem pares completos (exige CK no consolidado).").Italic();
            });
        }

        private void BoxDiscussao(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Green.Lighten3).Padding(10).Background(Colors.Green.Lighten5).Column(col =>
            {
                col.Item().Text("6. Discussão, limitações e entrega").Bold().FontSize(13).FontColor(Colors.Green.Darken3);
                col.Item().Text("• A distribuição de estrelas no top Java é tipicamente assimétrica (poucos repositórios concentram a maior parte da popularidade).");
                col.Item().Text("• Com CK disponível para um subconjunto ou para todos, as tabelas por quartil e as correlações permitem confrontar as hipóteses RQ01–RQ04 com evidência numérica.");
                col.Item().Text("• Limitações: CK depende da árvore de fontes analisada; comparações entre projetos com estruturas diferentes exigem cautela. Releases podem estar incompletos consoante o endpoint da API na Sprint 1.");
                col.Item().Text("• Entregas do laboratório: lista dos 1.000 repositórios; automação (clone + CK); CSV consolidado; hipóteses; análise, gráficos e correlações; relatório Markdown + PDF (Sprint 3).");
                col.Item().PaddingTop(10).Text("6.1 Conclusões").Bold().FontSize(12).FontColor(Colors.Green.Darken3);
                col.Item().Text(MontarConclusoesPdf()).FontSize(9).LineHeight(1.25f);
            });
        }

        private string MontarConclusoesPdf()
        {
            var n = _rows.Count;
            if (_nCk == 0)
                return "• Os dados de processo (GitHub) estão refletidos nas tabelas e no resumo global. " +
                       "• As RQs sobre qualidade (CK) carecem de medições no consolidado — execute a coleta CK em lote na Sprint 1 e regenere a Sprint 2 antes de voltar à Sprint 3. " +
                       "• A estrutura de quartis e correlações já está preparada para receber esses valores.";

            if (_nCk < n)
                return $"• CK disponível para {_nCk} de {n} repositórios: interpretar resultados como válidos para este subconjunto (atenção a viés se a coleta não for representativa). " +
                       "• Cruze quartis, correlações e figuras em bonus/ para a discussão e a apresentação oral.";

            return "• Consolidado com CK para toda a amostra: as RQ01–RQ04 podem ser discutidas com base na amostra completa. " +
                   "• Sintetize correlações significativas e padrões entre quartis na oral, sempre com as limitações do CK em mente.";
        }

        private void BoxFiguras(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Text("7. Figuras (pasta bonus/*.png)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("Inclui gráficos agregados (pdf_*.png: médias, histogramas, contagem por quartil) e dispersões de correlação quando há dados CK.").FontSize(8).FontColor(Colors.Grey.Darken1);
                if (_pngs.Count == 0)
                {
                    col.Item().Text("Nenhuma figura — a Sprint 3 gera gráficos quando há pares válidos (n ≥ 4).").Italic();
                    return;
                }

                const int maxFiguras = 24;
                var max = Math.Min(maxFiguras, _pngs.Count);
                for (var i = 0; i < max; i++)
                {
                    var p = _pngs[i];
                    col.Item().PaddingTop(10).Text(Path.GetFileName(p)).FontSize(9).SemiBold();
                    try
                    {
                        col.Item().PaddingTop(4).Image(p).FitWidth();
                    }
                    catch
                    {
                        col.Item().Text("(não foi possível incorporar a imagem)").FontSize(8).Italic();
                    }
                }

                if (_pngs.Count > max)
                    col.Item().Text($"(+ {_pngs.Count - max} figura(s) adicionais na pasta bonus.)").FontSize(8).Italic();
            });
        }

        private void BoxReferencias(IContainer c)
        {
            c.PaddingTop(16).Column(col =>
            {
                col.Item().Text("Referências (ferramentas)").Bold().FontSize(11);
                col.Item().Text("• CK — Aniche, M. Ferramenta de métricas Chidamber & Kemerer. https://github.com/mauricioaniche/ck");
                col.Item().Text("• GitHub REST/GraphQL API — https://docs.github.com/");
                col.Item().PaddingTop(8).Text($"Artefactos: {_saidaPath}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        }

        private static void Cabecalho(TableDescriptor t, params string[] cols)
        {
            t.Header(h =>
            {
                if (cols.Length == 0) return;
                h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(cols[0]).SemiBold().FontSize(9);
                for (var i = 1; i < cols.Length; i++)
                    h.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(cols[i]).SemiBold().FontSize(9);
            });
        }

        private static string MontarInterpretacaoProcesso(List<double> estrelas, List<double> idade, List<double> releases)
        {
            var partes = new List<string>();
            if (estrelas.Count > 0)
            {
                var m = estrelas.Average();
                var med = Mediana(estrelas);
                var dp = Desvio(estrelas);
                var cv = m > 1e-9 ? dp / m : 0;
                partes.Add(
                    $"Estrelas (n = {estrelas.Count}): média {m:F0}, mediana {med:F0}, desvio {dp:F0}. " +
                    $"A mediana inferior à média e o CV ≈ {cv:F2} são compatíveis com distribuição enviesada (cauda longa de repositórios muito populares).");
            }

            if (idade.Count > 0)
            {
                partes.Add(
                    $"Idade (n = {idade.Count}): média {idade.Average():F2} anos, mediana {Mediana(idade):F2} — a amostra tende a projetos já estabelecidos.");
            }

            if (releases.Count > 0)
            {
                var m = releases.Average();
                var med = Mediana(releases);
                partes.Add(
                    $"Releases (n = {releases.Count}): média {m:F1} frente a mediana {med:F1}, indicando alguns projetos com histórico de publicações muito intenso.");
            }

            return partes.Count > 0
                ? "3.1 Interpretação (processo): " + string.Join(" ", partes)
                : "3.1 Interpretação (processo): sem valores numéricos suficientes neste consolidado.";
        }

        private static void LinhaStat(TableDescriptor t, string nome, List<double> v)
        {
            if (v.Count == 0)
            {
                t.Cell().Element(Bd).Text(nome).FontSize(9);
                t.Cell().Element(Bd).AlignRight().Text("0").FontSize(9);
                t.Cell().Element(Bd).AlignRight().Text("—").FontSize(9);
                t.Cell().Element(Bd).AlignRight().Text("—").FontSize(9);
                t.Cell().Element(Bd).AlignRight().Text("—").FontSize(9);
                return;
            }

            var m = v.Average();
            var med = Mediana(v);
            var dp = Desvio(v);
            t.Cell().Element(Bd).Text(nome).FontSize(9);
            t.Cell().Element(Bd).AlignRight().Text(v.Count.ToString(CultureInfo.InvariantCulture)).FontSize(9);
            t.Cell().Element(Bd).AlignRight().Text(m.ToString("F2", CultureInfo.InvariantCulture)).FontSize(9);
            t.Cell().Element(Bd).AlignRight().Text(med.ToString("F2", CultureInfo.InvariantCulture)).FontSize(9);
            t.Cell().Element(Bd).AlignRight().Text(dp.ToString("F2", CultureInfo.InvariantCulture)).FontSize(9);

            static IContainer Bd(IContainer x) => x.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4);
        }

        private static double Mediana(List<double> v)
        {
            var o = v.OrderBy(x => x).ToList();
            var n = o.Count;
            return n % 2 == 1 ? o[n / 2] : (o[n / 2 - 1] + o[n / 2]) / 2;
        }

        private static double Desvio(List<double> v)
        {
            if (v.Count < 2) return 0;
            var m = v.Average();
            return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Count - 1));
        }

        private static void TabelaGenerica(TableDescriptor table, List<string[]> linhas)
        {
            if (linhas.Count == 0) return;
            var nCol = linhas.Max(r => r.Length);
            table.ColumnsDefinition(d =>
            {
                d.RelativeColumn(1.4f);
                if (nCol >= 2)
                    d.RelativeColumn(1f);
                for (var i = 2; i < nCol; i++)
                    d.RelativeColumn(0.95f);
            });

            var head = linhas[0];
            table.Header(h =>
            {
                for (var i = 0; i < nCol; i++)
                {
                    var txt = i < head.Length ? head[i] : "";
                    var cell = h.Cell().Background(Colors.Grey.Lighten2).Padding(3);
                    if (i <= 1)
                        cell.Text(txt).SemiBold().FontSize(7);
                    else
                        cell.AlignRight().Text(txt).SemiBold().FontSize(7);
                }
            });

            foreach (var row in linhas.Skip(1))
            {
                for (var i = 0; i < nCol; i++)
                {
                    var raw = i < row.Length ? row[i] : "";
                    var display = string.IsNullOrWhiteSpace(raw) ? "—" : raw.Trim();
                    var cell = table.Cell().Element(Bd);
                    if (i <= 1)
                        cell.Text(display).FontSize(7);
                    else
                        cell.AlignRight().Text(display).FontSize(7);
                }
            }

            static IContainer Bd(IContainer x) => x.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3);
        }
    }
}

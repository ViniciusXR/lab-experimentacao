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

    private const string TituloLab = "Enunciado 2 — Qualidade de sistemas Java (CK + GitHub)";
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
        var pastaSprint3 = Directory.GetParent(outDir)?.FullName ?? outDir;
        var imagensPlanejamento = DescobrirImagensPlanejamento(pastaSprint3, outDir, bonusDir);
        var doc = new RelatorioLab02Document(
            autores,
            rows,
            nCk,
            corrProc,
            corrCk,
            rqTabelas,
            pngs,
            imagensPlanejamento,
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

    private static List<string> DescobrirImagensPlanejamento(string pastaSprint3, string outDir, string bonusDir)
    {
        var candidatos = new[]
        {
            Path.Combine(pastaSprint3, "img01vert.png"),
            Path.Combine(pastaSprint3, "img02vert.png"),
            Path.Combine(outDir, "img01vert.png"),
            Path.Combine(outDir, "img02vert.png"),
            Path.Combine(bonusDir, "img01vert.png"),
            Path.Combine(bonusDir, "img02vert.png")
        };

        var encontrados = new List<string>();
        foreach (var c in candidatos)
            if (File.Exists(c))
                encontrados.Add(c);

        return encontrados;
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
                if (!a.HasValue || !b.HasValue ||
                    double.IsNaN(a.Value) || double.IsInfinity(a.Value) ||
                    double.IsNaN(b.Value) || double.IsInfinity(b.Value))
                    continue;
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
                if (!a.HasValue || !b.HasValue ||
                    double.IsNaN(a.Value) || double.IsInfinity(a.Value) ||
                    double.IsNaN(b.Value) || double.IsInfinity(b.Value))
                    continue;
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
        private readonly List<string> _imagensPlanejamento;
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
            List<string> imagensPlanejamento,
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
            _imagensPlanejamento = imagensPlanejamento;
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
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(BoxPlanejamentoExperimento);
                col.Item().Element(BoxGlossario);
                col.Item().Element(BoxIntro);
                col.Item().Element(BoxMetodologia);
                col.Item().Element(BoxMetricasProcesso);
                if (_nCk > 0)
                    col.Item().Element(BoxMetricasCk);

                col.Item().Element(BoxRqs);
                col.Item().Element(BoxBonus);
                col.Item().PageBreak();
                col.Item().Element(BoxFiguras);
                col.Item().Element(BoxConclusoesTrabalho);
                col.Item().Element(BoxReferencias);
            });
        }

        private void BoxGlossario(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("2. Glossário").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("• LOC (Lines of Code): total de linhas de código-fonte.");
                col.Item().Text("• CBO (Coupling Between Objects): nível de acoplamento entre classes/objetos.");
                col.Item().Text("• DIT (Depth of Inheritance Tree): profundidade da árvore de herança de uma classe.");
                col.Item().Text("• LCOM (Lack of Cohesion of Methods): falta de coesão entre métodos de uma classe.");
                col.Item().Text("• NOC (Number of Children): número de subclasses diretas de uma classe.");
                col.Item().Text("• CK: conjunto de métricas de qualidade orientadas a objetos (Chidamber & Kemerer).");
                col.Item().Text("• RQ (Research Question): questão de pesquisa formulada no experimento.");
                col.Item().Text("• Quartil (Q1–Q4): divisão da amostra ordenada em quatro grupos de mesma ordem estatística.");
                col.Item().Text("• Pearson (r): correlação linear entre duas variáveis numéricas.");
                col.Item().Text("• Spearman (ρ): correlação de postos (monotônica), menos sensível a não linearidade.");
                col.Item().Text("• p-valor: medida de significância estatística da correlação observada.");
                col.Item().Text("• n: número de observações válidas usadas no cálculo.");
            });
        }

        private void BoxPlanejamentoExperimento(IContainer c)
        {
            if (_imagensPlanejamento.Count == 0) return;

            c.Column(col =>
            {
                col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Background(Colors.Blue.Lighten5).Column(x =>
                {
                    x.Item().Text("1. Planejamento e execução do experimento").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                    x.Item().Text(
                        "As figuras abaixo documentam o planejamento experimental (desenho inicial, etapas e organização da análise). ")
                        .FontSize(9);

                    try
                    {
                        x.Item().PaddingTop(8).AlignCenter().Height(500).Image(_imagensPlanejamento[0]).FitArea();
                    }
                    catch
                    {
                        x.Item().Text("(não foi possível incorporar a imagem)").Italic().FontSize(8);
                    }
                });

                for (var i = 1; i < _imagensPlanejamento.Count; i++)
                {
                    var img = _imagensPlanejamento[i];
                    col.Item().PageBreak();
                    col.Item().Column(x =>
                    {
                        x.Spacing(6);
                        x.Item().Text($"1.{i + 1} Evidência visual do planejamento").SemiBold().FontSize(10);
                        try
                        {
                            x.Item().AlignCenter().Height(680).Image(img).FitArea();
                        }
                        catch
                        {
                            x.Item().Text("(não foi possível incorporar a imagem)").Italic().FontSize(8);
                        }
                    });
                }
            });
        }

        private void BoxIntro(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Orange.Lighten3).Padding(10).Background(Colors.Orange.Lighten5).Column(col =>
            {
                col.Item().Text("3. Introdução e hipóteses informais").Bold().FontSize(13).FontColor(Colors.Orange.Darken3);
                col.Item().Text(
                    "Este relatório atende ao Laboratório 02: estudo das características de qualidade (CBO, DIT, LCOM via CK) " +
                    "em repositórios Java populares, relacionando-as a métricas de processo (popularidade, maturidade, atividade, tamanho).");
                col.Item().PaddingTop(4).Text("Questões de pesquisa e hipóteses (informais):").SemiBold();
                col.Item().Text("• RQ01 — Popularidade (estrelas) × qualidade: projetos muito populares podem acumular legado e acoplamento; espera-se correlação fraca a moderada.");
                col.Item().Text("• RQ02 — Maturidade (idade) × qualidade: maior idade pode associar-se a hierarquias mais profundas (DIT) e histórico de dependências.");
                col.Item().Text("• RQ03 — Atividade (releases) × qualidade: mais releases sugere manutenção ativa, com efeitos ambíguos em coesão/acoplamento.");
                col.Item().Text("• RQ04 — Tamanho (LOC Java / comentários) × qualidade: maior base de código tende a elevar acoplamento se o desenho modular não acompanhar.");

                col.Item().PaddingTop(8).Text("3.1 Hipóteses adicionais").SemiBold();
                col.Item().Text("• RQ05 - Crescimento acelerado (RQ04): repositórios com LOC mais alto tendem a elevar CBO e LCOM, sugerindo perda de coesão e maior acoplamento arquitetural.");
                col.Item().Text("• RQ06 - Atividade intensa (RQ03): projetos com muitos releases tendem a manter DIT mais raso e usar mais composição/injeção de dependências, mantendo CBO em faixa moderada/alta.");
                col.Item().Text("• RQ07 - Peso do legado (RQ02): repositórios mais antigos tendem a apresentar DIT mais elevado por maior dependência histórica de herança clássica.");
            });
        }

        private void BoxMetodologia(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("4. Metodologia").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("• Seleção dos 1.000 repositórios Java mais populares no GitHub (API GraphQL ou REST).");
                col.Item().Text("• Métricas de processo: estrelas; idade (anos); número de releases; tamanho em disco (quando disponível); LOC Java e linhas de comentário estimadas na coleta CK.");
                col.Item().Text("• Métricas de qualidade: CK (CBO, DIT, LCOM) com sumarização por repositório — média, mediana e desvio padrão entre classes.");
                col.Item().Text("• Análise: estatísticas descritivas globais; estratificação por quartis das variáveis de processo; correlações de Pearson e Spearman com p-valor bilateral aproximado (t com n−2 g.l.).");
                col.Item().PaddingTop(4).Text($"Fonte de dados consolidada: {Path.GetFileName(_consolidadoPath)}").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
            });
        }

        private void BoxMetricasProcesso(IContainer c)
        {
            var e = ValoresValidos(_rows.Select(x => x.Estrelas));
            var fk = ValoresValidos(_rows.Select(x => x.Forks));
            var id = ValoresValidos(_rows.Select(x => x.IdadeAnos));
            var r = ValoresValidos(_rows.Select(x => x.Releases));
            var dk = ValoresValidos(_rows.Select(x => x.DiskKb));
            var loc = ValoresValidos(_rows.Select(x => x.LocJava));
            var com = ValoresValidos(_rows.Select(x => x.Comentarios));

            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("5. Resultados — Métricas de processo").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("Sumarização global (média, mediana e desvio padrão), alinhada ao CSV resumo_global_metricas.csv.").FontSize(9).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Amostra consolidada utilizada nesta execução: n = {_rows.Count} repositórios.").FontSize(8).FontColor(Colors.Grey.Darken1);
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
                    LinhaStat(t, "LOC Java", loc);
                    LinhaStat(t, "Comentários (linhas)", com);
                });
                col.Item().PaddingTop(10).Text(MontarInterpretacaoProcesso(e, id, r)).FontSize(9).LineHeight(1.25f);
            });
        }

        private void BoxMetricasCk(IContainer c)
        {
            var cbo = ValoresValidos(_rows.Select(x => x.CboMedia));
            var dit = ValoresValidos(_rows.Select(x => x.DitMedia));
            var lcom = ValoresValidos(_rows.Select(x => x.LcomMedia));

            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("5.2 Qualidade (CK) — repositórios medidos").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
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
                col.Item().Text("6. Resultados por questão de pesquisa (estratificação por quartis)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
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
                    col.Item().PaddingTop(8).Text(TituloAmigavelRq(titulo)).SemiBold();
                    col.Item().Table(t => TabelaGenerica(t, LinhasComCabecalhoAmigavel(linhas)));
                }
            });
        }

        private void BoxBonus(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Purple.Lighten3).Padding(10).Background(Colors.Purple.Lighten5).Column(col =>
            {
                col.Item().Text("7. Bônus — correlação (Pearson / Spearman) e figuras").Bold().FontSize(13).FontColor(Colors.Purple.Darken3);
                col.Item().Text("Testes de correlação com p-valor aproximado; gráficos de dispersão na secção de figuras (gerados nesta Sprint 3).").FontSize(9);

                col.Item().PaddingTop(6).Text("7.1 Processo × processo (exploratório)").SemiBold();
                if (_corrProc.Count > 0)
                    col.Item().Table(t => TabelaGenerica(t, LinhasComCabecalhoAmigavel(_corrProc)));
                else
                    col.Item().Text("—").Italic();

                col.Item().PaddingTop(8).Text("7.2 Processo × qualidade (CK)").SemiBold();
                if (_corrCk.Count > 0)
                    col.Item().Table(t => TabelaGenerica(t, _corrCk));
                else
                    col.Item().Text("Sem pares completos (exige CK no consolidado).").Italic();
            });
        }

        private void BoxConclusoesTrabalho(IContainer c)
        {
            c.Border(1).BorderColor(Colors.Green.Lighten3).Padding(10).Background(Colors.Green.Lighten5).Column(col =>
            {
                col.Item().Text("9. Conclusões do trabalho").Bold().FontSize(13).FontColor(Colors.Green.Darken3);
                col.Item().PaddingTop(6).Text("""
A análise consolidada dos repositórios Java permitiu avaliar empiricamente o impacto das métricas de processo na qualidade estrutural dos sistemas. Com base nos testes estatísticos, conclui-se:

RQ01: Qual a relação entre a popularidade dos repositórios e as suas características de qualidade?

    Conclusão: A popularidade não degrada a arquitetura do sistema. Observou-se uma relação linear fraca e negativa com o acoplamento (Pearson r=−0.1318). Projetos de alto engajamento tendem a compensar a complexidade inerente ao seu tamanho adotando processos rigorosos de revisão de código, governança de contribuição e automação de CI/CD.

RQ02 (e Hipótese 3): Qual a relação entre a maturidade dos repositórios e as suas características de qualidade?

    Conclusão: Projetos mais antigos possuem hierarquias de herança mais profundas. A idade do repositório apresentou uma correlação positiva significativa com a profundidade da árvore de herança (DIT, Spearman ρ=0.2798), com a mediana subindo de 1.25 (menos de 3 anos) para 1.41 (mais de 7 anos). Isso reflete a evolução do ecossistema Java: projetos legados apoiam-se em hierarquias clássicas de orientação a objetos, enquanto sistemas mais recentes privilegiam composição e Inversão de Controle (IoC).

RQ03 (e Hipótese 2): Qual a relação entre a atividade dos repositórios e as suas características de qualidade?

    Conclusão: Alta atividade eleva o acoplamento, mas mantém a herança estável. Projetos com maior volume de releases registraram um aumento significativo no acoplamento (CBO, Spearman ρ=0.4041), enquanto a profundidade de herança sofreu pouca variação (DIT, Pearson r=0.0507). Esse padrão demonstra que ciclos de entrega contínuos exigem arquiteturas orientadas a interfaces, que garantem flexibilidade sem aprofundar a herança, mas inevitavelmente aumentam a interdependência modular.

RQ04 (e Hipótese 1): Qual a relação entre o tamanho dos repositórios e as suas características de qualidade?

    Conclusão: O crescimento acelerado prejudica fortemente a coesão e aumenta o acoplamento estrutural. O tamanho do código (LOC) apresentou forte associação positiva tanto com a falta de coesão (LCOM, Spearman ρ=0.4542) quanto com o acoplamento (CBO, Spearman ρ=0.4237). As métricas indicam que, em bases extensas, é notavelmente mais difícil manter fronteiras arquiteturais. Isso resulta no surgimento de "God Classes" sobrecarregadas, evidenciado pelo LCOM mediano que salta de 11.72 em projetos pequenos para 39.92 nos grandes.
""").FontSize(9).LineHeight(1.25f);
            });
        }

        private string ParagrafoHipotese1()
        {
            var rhoLocLcom = ObterSpearman("loc_java", "lcom_media");
            var rhoLocCbo = ObterSpearman("loc_java", "cbo_media");
            return "RQ04 e Hipótese 1 (tamanho vs coesão/acoplamento): " +
                   $"observou-se associação positiva entre LOC e LCOM (ρ={Fmt(rhoLocLcom)}) e entre LOC e CBO (ρ={Fmt(rhoLocCbo)}). " +
                   $"No resumo descritivo por grupos de tamanho, {ResumoMedianasPorTamanho(r => r.LcomMedia)} " +
                   "Esse padrão é compatível com o crescimento de classes sobrecarregadas e maior esforço para manter fronteiras arquiteturais em bases de código extensas.";
        }

        private string ParagrafoHipotese2()
        {
            var rcbo = ObterPearson("releases", "cbo_media");
            var rdit = ObterPearson("releases", "dit_media");
            return "RQ03 e Hipótese 2 (atividade vs CBO/DIT): " +
                   $"projetos com mais releases apresentaram aumento de CBO (r={Fmt(rcbo)}), enquanto DIT variou pouco (r={Fmt(rdit)}). " +
                   "Esse comportamento é coerente com arquiteturas orientadas à composição e interfaces, que privilegiam flexibilidade de evolução contínua sem aprofundar herança.";
        }

        private string ParagrafoHipotese3()
        {
            var rho = ObterSpearman("idade_anos", "dit_media");
            return "RQ02 e Hipótese 3 (maturidade vs herança): " +
                   $"a idade mostrou associação positiva com DIT (ρ={Fmt(rho)}), e por faixas etárias {ResumoMedianasDitPorIdade()} " +
                   "O resultado é compatível com a transição do ecossistema Java: projetos legados tendem a manter hierarquias clássicas mais profundas, enquanto projetos recentes privilegiam IoC e composição.";
        }

        private string ParagrafoRq01()
        {
            var r = ObterPearson("estrelas", "cbo_media");
            return "RQ01 (popularidade vs qualidade): " +
                   $"a relação com CBO foi fraca (r={Fmt(r)}), sugerindo ausência de tendência arquitetural forte apenas pelo volume de estrelas. " +
                   "Projetos muito populares podem compensar complexidade com revisão de código, automação e governança de contribuição.";
        }

        private static string Fmt(double? v) => v.HasValue ? v.Value.ToString("F4", CultureInfo.InvariantCulture) : "—";

        private double? ObterPearson(string x, string y)
        {
            foreach (var row in _corrCk)
            {
                if (row.Length < 3) continue;
                if (!string.Equals(row[0], x, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(row[1], y, StringComparison.OrdinalIgnoreCase)) continue;
                if (double.TryParse(row[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                    return r;
            }

            return null;
        }

        private double? ObterSpearman(string x, string y)
        {
            foreach (var row in _corrCk)
            {
                if (row.Length < 5) continue;
                if (!string.Equals(row[0], x, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(row[1], y, StringComparison.OrdinalIgnoreCase)) continue;
                if (double.TryParse(row[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var rho))
                    return rho;
            }

            return null;
        }

        private string ResumoMedianasPorTamanho(Func<RepoRow, double?> metric)
        {
            var comLoc = _rows.Where(r => r.LocJava.HasValue && r.LocJava.Value > 0).OrderBy(r => r.LocJava).ToList();
            if (comLoc.Count < 9) return "dados insuficientes.";
            var q1 = comLoc[comLoc.Count / 3].LocJava!.Value;
            var q2 = comLoc[(comLoc.Count * 2) / 3].LocJava!.Value;
            var p = new List<double>();
            var m = new List<double>();
            var g = new List<double>();

            foreach (var r in comLoc)
            {
                var v = metric(r);
                if (!v.HasValue || double.IsNaN(v.Value) || double.IsInfinity(v.Value)) continue;
                if (r.LocJava!.Value <= q1) p.Add(v.Value);
                else if (r.LocJava.Value <= q2) m.Add(v.Value);
                else g.Add(v.Value);
            }

            return $"P={MedianaListaOuTraco(p)}, M={MedianaListaOuTraco(m)}, G={MedianaListaOuTraco(g)}.";
        }

        private string ResumoMedianasDitPorIdade()
        {
            var a = new List<double>();
            var b = new List<double>();
            var c = new List<double>();
            foreach (var r in _rows)
            {
                if (!r.IdadeAnos.HasValue || !r.DitMedia.HasValue) continue;
                var dit = r.DitMedia.Value;
                if (double.IsNaN(dit) || double.IsInfinity(dit)) continue;
                if (r.IdadeAnos.Value < 3) a.Add(dit);
                else if (r.IdadeAnos.Value <= 7) b.Add(dit);
                else c.Add(dit);
            }

            return $"<3={MedianaListaOuTraco(a)}, 3–7={MedianaListaOuTraco(b)}, >7={MedianaListaOuTraco(c)}.";
        }

        private static string MedianaListaOuTraco(List<double> v)
        {
            if (v.Count == 0) return "—";
            return Mediana(v).ToString("F2", CultureInfo.InvariantCulture);
        }

        private static string TituloAmigavelRq(string titulo)
        {
            var t = titulo.ToLowerInvariant();
            if (t.Contains("rq01")) return "RQ01 — Popularidade (estrelas) × qualidade";
            if (t.Contains("rq02")) return "RQ02 — Maturidade (idade) × qualidade";
            if (t.Contains("rq03")) return "RQ03 — Atividade (releases) × qualidade";
            if (t.Contains("rq04")) return "RQ04 — Tamanho (LOC Java) × qualidade";
            return titulo.Replace('_', ' ');
        }

        private static List<string[]> LinhasComCabecalhoAmigavel(List<string[]> linhas)
        {
            if (linhas.Count == 0) return linhas;
            var copia = linhas.Select(r => r.ToArray()).ToList();
            var head = copia[0];
            for (var i = 0; i < head.Length; i++)
                head[i] = RotuloAmigavel(head[i], i == 0);

            for (var r = 1; r < copia.Count; r++)
            {
                for (var c = 0; c < copia[r].Length; c++)
                    copia[r][c] = ValorAmigavel(copia[r][c]);
            }

            return copia;
        }

        private static string RotuloAmigavel(string raw, bool primeiraColuna)
        {
            var s = (raw ?? "").Trim().ToLowerInvariant().Replace(' ', '_')
                .Replace("icom", "lcom", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(s) && primeiraColuna) return "Variável X";
            return s switch
            {
                "x" => "Variável X",
                "y" => "Variável Y",
                "faixa" => "Faixa",
                "n_total" => "n total",
                "n_com_ck" => "n com CK",
                "cbo_media_media" => "Média CBO",
                "cbo_media_mediana" => "Mediana CBO",
                "dit_media_media" => "Média DIT",
                "dit_media_mediana" => "Mediana DIT",
                "lcom_media_media" => "Média LCOM",
                "lcom_media_mediana" => "Mediana LCOM",
                "pearson_r" => "r (Pearson)",
                "pearson_p_aprox" => "Valor-p (Pearson)",
                "spearman_rho" => "ρ (Spearman)",
                "spearman_p_aprox" => "Valor-p (Spearman)",
                "n" => "n",
                _ => raw
                    .Replace("lcom", "LCOM", StringComparison.OrdinalIgnoreCase)
                    .Replace("cbo", "CBO", StringComparison.OrdinalIgnoreCase)
                    .Replace("dit", "DIT", StringComparison.OrdinalIgnoreCase)
                    .Replace("loc", "LOC", StringComparison.OrdinalIgnoreCase)
                    .Replace('_', ' ')
            };
        }

        private static string ValorAmigavel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var v = raw
                .Replace("icom", "lcom", StringComparison.OrdinalIgnoreCase)
                .Replace("maturidade idade", "maturidade_idade", StringComparison.OrdinalIgnoreCase)
                .Replace("rq01_popularidade_estrelas", "RQ01 — Popularidade (estrelas)", StringComparison.OrdinalIgnoreCase)
                .Replace("rq02_maturidade_idade", "RQ02 — Maturidade (idade)", StringComparison.OrdinalIgnoreCase)
                .Replace("rq03_atividade_releases", "RQ03 — Atividade (releases)", StringComparison.OrdinalIgnoreCase)
                .Replace("rq04_tamanho_loc_java", "RQ04 — Tamanho (LOC Java)", StringComparison.OrdinalIgnoreCase)
                .Trim();

            var chave = v.ToLowerInvariant().Replace(' ', '_');
            return chave switch
            {
                "q1_baixo" => "1º Quartil (Baixo)",
                "q2_medio_baixo" => "2º Quartil (Médio-baixo)",
                "q3_medio_alto" => "3º Quartil (Médio-alto)",
                "q4_alto" => "4º Quartil (Alto)",
                _ => v
            };
        }

        private string LegendaFiguraSecao9(string caminho)
        {
            var nome = Path.GetFileNameWithoutExtension(caminho).ToLowerInvariant();
            return nome switch
            {
                "pdf_17_rq01_popularidade_log10_vs_cbo_scatter" => "Gráfico de dispersão com tendência entre popularidade (estrelas em log10) e CBO; associação fraca.",
                "pdf_03_rq02_maturidade_vs_cbo_scatter_trend" => "Dispersão com linha de tendência entre maturidade (idade) e CBO para avaliar efeito de legado arquitetural.",
                "pdf_06_rq03_atividade_vs_cbo_scatter" => "Dispersão entre atividade de releases e CBO, indicando variação de acoplamento conforme o ritmo de evolução.",
                "pdf_09_rq04_tamanho_vs_cbo_scatter" => "Dispersão entre tamanho do repositório (LOC em log10) e CBO, com tendência positiva.",
                "pdf_11_rq04_tamanho_vs_lcom_scatter" => "Gráfico de dispersão com linha de tendência relacionando tamanho do repositório (LOC em log10) e LCOM, com correlação positiva moderada.",
                "pdf_14_hip2_releases_vs_cbo_trend" => "Tendência de CBO em função de releases, apoiando a hipótese de aumento de acoplamento com atividade intensa.",
                "pdf_15_hip2_releases_vs_dit_trend" => "Tendência de DIT em função de releases, com variação discreta na profundidade de herança.",
                "pdf_18_rq01_boxplot_estrelas_cbo" => "Boxplot de CBO por faixas de popularidade (estrelas), comparando dispersão e medianas entre quartis.",
                "pdf_19_rq02_boxplot_idade_dit" => "Boxplot de DIT por faixas de idade, destacando o comportamento de herança por maturidade.",
                "pdf_20_rq03_boxplot_releases_cbo" => "Boxplot de CBO por faixas de releases, evidenciando diferença de acoplamento entre níveis de atividade.",
                "pdf_21_rq04_boxplot_loc_lcom" => $"Boxplot de LCOM por grupos de tamanho (Pequeno, Médio, Grande), reforçando o crescimento das medianas ({ResumoMedianasPorTamanho(r => r.LcomMedia)}).",
                "pdf_22_heatmap_correlacoes" => "Mapa de calor das correlações entre métricas de processo e qualidade (CK).",
                _ => "Figura de apoio à interpretação dos resultados quantitativos."
            };
        }

        private string MontarConclusoesPdf()
        {
            var n = _rows.Count;
            if (_nCk == 0)
                return "• Os dados de processo (GitHub) estão refletidos nas tabelas e no resumo global. " +
                       "• As RQs sobre qualidade (CK) carecem de medições no consolidado — execute a coleta CK em lote na Sprint 1 e regenere a Sprint 2 antes de voltar à Sprint 3. " +
                       "• A estrutura de quartis e correlações já está preparada para receber esses valores.";

            return "• Consolidado com CK para toda a amostra: as RQ01–RQ04 podem ser discutidas com base na amostra completa. ";
        }

        private void BoxFiguras(IContainer c)
        {
            var figurasSelecionadas = _pngs
                .Where(FiguraSecao9Selecionada)
                .ToList();

            c.Column(col =>
            {
                col.Item().Text("8. Figuras").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                if (figurasSelecionadas.Count == 0)
                {
                    col.Item().Text("Nenhuma figura — a Sprint 3 gera gráficos quando há pares válidos (n ≥ 4).").Italic();
                    return;
                }

                const int maxFiguras = 6;
                var max = Math.Min(maxFiguras, figurasSelecionadas.Count);
                for (var i = 0; i < max; i++)
                {
                    var p = figurasSelecionadas[i];
                    col.Item().PaddingTop(10).Text(TituloFiguraSecao9(p)).FontSize(9).SemiBold();
                    try
                    {
                        col.Item().PaddingTop(4).Image(p).FitWidth();
                        col.Item().PaddingTop(3)
                            .Text($"Figura {i + 1}: {LegendaFiguraSecao9(p)}")
                            .FontSize(8)
                            .Italic()
                            .FontColor(Colors.Grey.Darken2);
                    }
                    catch
                    {
                        col.Item().Text("(não foi possível incorporar a imagem)").FontSize(8).Italic();
                    }
                }

                if (figurasSelecionadas.Count > max)
                    col.Item().Text($"(+ {figurasSelecionadas.Count - max} figura(s) adicionais na pasta bonus.)").FontSize(8).Italic();
            });
        }

        private static bool FiguraSecao9Selecionada(string caminho)
        {
            var nome = Path.GetFileNameWithoutExtension(caminho).ToLowerInvariant();
            return nome is "pdf_17_rq01_popularidade_log10_vs_cbo_scatter"
                or "pdf_03_rq02_maturidade_vs_cbo_scatter_trend"
                or "pdf_06_rq03_atividade_vs_cbo_scatter"
                or "pdf_09_rq04_tamanho_vs_cbo_scatter"
                or "pdf_11_rq04_tamanho_vs_lcom_scatter"
                or "pdf_14_hip2_releases_vs_cbo_trend"
                or "pdf_15_hip2_releases_vs_dit_trend"
                or "pdf_18_rq01_boxplot_estrelas_cbo"
                or "pdf_19_rq02_boxplot_idade_dit"
                or "pdf_20_rq03_boxplot_releases_cbo"
                or "pdf_21_rq04_boxplot_loc_lcom"
                or "pdf_23_rq05_loc_vs_cbo_lcom_por_tercis"
                or "pdf_24_rq06_releases_vs_cbo_dit_quartis"
                or "pdf_25_rq07_idade_vs_dit_boxplot"
                or "pdf_22_heatmap_correlacoes";
        }

        private static string TituloFiguraSecao9(string caminho)
        {
            var n = Path.GetFileNameWithoutExtension(caminho).ToLowerInvariant();

            if (n.Contains("rq01")) return "RQ01 — Popularidade × Qualidade";
            if (n.Contains("rq02")) return "RQ02 — Maturidade × Qualidade";
            if (n.Contains("rq03")) return "RQ03 — Atividade × Qualidade";
            if (n.Contains("rq04")) return "RQ04 — Tamanho × Qualidade";
            if (n.Contains("rq05")) return "RQ05 — Crescimento acelerado (LOC vs CBO/LCOM)";
            if (n.Contains("rq06")) return "RQ06 — Atividade intensa (releases vs CBO/DIT)";
            if (n.Contains("rq07")) return "RQ07 — Peso do legado (idade vs DIT)";
            if (n.Contains("heatmap")) return "Mapa de calor — correlações entre métricas";

            if (n.Contains("hip1")) return "RQ04 — Hipótese 1 (tamanho e qualidade)";
            if (n.Contains("hip2")) return "RQ03 — Hipótese 2 (atividade e arquitetura)";
            if (n.Contains("hip3")) return "RQ02 — Hipótese 3 (maturidade e DIT)";

            return "Gráfico de apoio";
        }

        private void BoxReferencias(IContainer c)
        {
            c.PaddingTop(16).Column(col =>
            {
                col.Item().Text("10. Referências (ferramentas)").Bold().FontSize(11);
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
                ? "5.1 Interpretação (processo): " + string.Join(" ", partes)
                : "5.1 Interpretação (processo): sem valores numéricos suficientes neste consolidado.";
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

        private static List<double> ValoresValidos(IEnumerable<double?> origem) =>
            origem.Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Where(x => !double.IsNaN(x) && !double.IsInfinity(x))
                .ToList();

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

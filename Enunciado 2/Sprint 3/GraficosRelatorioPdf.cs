using MathNet.Numerics.Statistics;

namespace Enunciado2.Sprint3;

/// <summary>
/// Gráficos extras (prefixo <c>pdf_</c>) para o relatório PDF — barras, quartis e histogramas de processo.
/// </summary>
internal static class GraficosRelatorioPdf
{
    public static void GerarTodos(List<RepoRow> rows, string bonusDir)
    {
        if (rows.Count == 0 || !Directory.Exists(bonusDir)) return;
        Directory.CreateDirectory(bonusDir);

        try
        {
            GerarGraficosRqsScatter(rows, bonusDir);
        }
        catch { /* ignore */ }

        try
        {
            GerarBoxplotsRqs(rows, bonusDir);
        }
        catch { /* ignore */ }

        try
        {
            GerarHeatmapCorrelacoes(rows, bonusDir);
        }
        catch { /* ignore */ }

        try
        {
            GerarGraficosHipoteses(rows, bonusDir);
        }
        catch { /* ignore */ }

        try
        {
            GerarGraficosQuestoesAdicionais(rows, bonusDir);
        }
        catch { /* ignore */ }

        try
        {
            GerarBarrasMediasProcesso(rows, Path.Combine(bonusDir, "pdf_01_barras_medias_processo.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarHistograma(rows, r => r.Estrelas, "Estrelas (distribuição)", Path.Combine(bonusDir, "pdf_02_hist_estrelas.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarBarrasQuartilContagem(rows, r => r.Estrelas, "Repos por quartil de estrelas", Path.Combine(bonusDir, "pdf_03_quartis_estrelas.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarBarrasQuartilContagem(rows, r => r.IdadeAnos, "Repos por quartil de idade (anos)", Path.Combine(bonusDir, "pdf_04_quartis_idade.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarBarrasQuartilContagem(rows, r => r.Releases, "Repos por quartil de releases", Path.Combine(bonusDir, "pdf_05_quartis_releases.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarBarrasMediasCk(rows, Path.Combine(bonusDir, "pdf_06_barras_medias_ck.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarCurvasCkPorQuartil(
                rows,
                r => r.Estrelas,
                "RQ01 — Popularidade (estrelas) × qualidade (CK por quartil)",
                Path.Combine(bonusDir, "pdf_07_rq01_popularidade_vs_ck_quartis.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarCurvasCkPorQuartil(
                rows,
                r => r.IdadeAnos,
                "RQ02 — Maturidade (idade) × qualidade (CK por quartil)",
                Path.Combine(bonusDir, "pdf_08_rq02_maturidade_vs_ck_quartis.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarCurvasCkPorQuartil(
                rows,
                r => r.Releases,
                "RQ03 — Atividade (releases) × qualidade (CK por quartil)",
                Path.Combine(bonusDir, "pdf_09_rq03_atividade_vs_ck_quartis.png"));
        }
        catch { /* ignore */ }

        try
        {
            GerarCurvasCkPorQuartil(
                rows,
                r => r.LocJava,
                "RQ04 — Tamanho (LOC Java) × qualidade (CK por quartil)",
                Path.Combine(bonusDir, "pdf_10_rq04_tamanho_vs_ck_quartis.png"));
        }
        catch { /* ignore */ }
    }

    private static void GerarGraficosRqsScatter(List<RepoRow> rows, string bonusDir)
    {
        // RQ01 (extra) em escala log, conforme solicitado
        GerarScatterRq(rows, r => r.Estrelas, "Estrelas (log10)", r => r.CboMedia, "CBO", true, false,
            false, Path.Combine(bonusDir, "pdf_17_rq01_popularidade_log10_vs_cbo_scatter.png"));

        // RQ01 Popularidade (estrelas)
        GerarScatterRq(rows, r => r.Estrelas, "Estrelas", r => r.CboMedia, "CBO", false, false,
            false, Path.Combine(bonusDir, "pdf_00_rq01_popularidade_vs_cbo_scatter.png"));
        GerarScatterRq(rows, r => r.Estrelas, "Estrelas", r => r.DitMedia, "DIT", false, false,
            false, Path.Combine(bonusDir, "pdf_01_rq01_popularidade_vs_dit_scatter.png"));
        GerarScatterRq(rows, r => r.Estrelas, "Estrelas", r => r.LcomMedia, "LCOM", false, false,
            false, Path.Combine(bonusDir, "pdf_02_rq01_popularidade_vs_lcom_scatter.png"));

        // RQ02 Maturidade com linha de tendência
        GerarScatterRq(rows, r => r.IdadeAnos, "Idade (anos)", r => r.CboMedia, "CBO", false, false,
            true, Path.Combine(bonusDir, "pdf_03_rq02_maturidade_vs_cbo_scatter_trend.png"));
        GerarScatterRq(rows, r => r.IdadeAnos, "Idade (anos)", r => r.DitMedia, "DIT", false, false,
            true, Path.Combine(bonusDir, "pdf_04_rq02_maturidade_vs_dit_scatter_trend.png"));
        GerarScatterRq(rows, r => r.IdadeAnos, "Idade (anos)", r => r.LcomMedia, "LCOM", false, false,
            true, Path.Combine(bonusDir, "pdf_05_rq02_maturidade_vs_lcom_scatter_trend.png"));

        // RQ03 Atividade (escala log no eixo X) com linha de tendência
        GerarScatterRq(rows, r => r.Releases, "Releases (log10)", r => r.CboMedia, "CBO", true, false,
            true, Path.Combine(bonusDir, "pdf_06_rq03_atividade_vs_cbo_scatter.png"));
        GerarScatterRq(rows, r => r.Releases, "Releases (log10)", r => r.DitMedia, "DIT", true, false,
            false, Path.Combine(bonusDir, "pdf_07_rq03_atividade_vs_dit_scatter.png"));
        GerarScatterRq(rows, r => r.Releases, "Releases (log10)", r => r.LcomMedia, "LCOM", true, false,
            false, Path.Combine(bonusDir, "pdf_08_rq03_atividade_vs_lcom_scatter.png"));

        // RQ04 Tamanho (LOC) em escala log no eixo X com linha de tendência
        GerarScatterRq(rows, r => r.LocJava, "LOC Java (log10)", r => r.CboMedia, "CBO", true, false,
            true, Path.Combine(bonusDir, "pdf_09_rq04_tamanho_vs_cbo_scatter.png"));
        GerarScatterRq(rows, r => r.LocJava, "LOC Java (log10)", r => r.DitMedia, "DIT", true, false,
            false, Path.Combine(bonusDir, "pdf_10_rq04_tamanho_vs_dit_scatter.png"));
        GerarScatterRq(rows, r => r.LocJava, "LOC Java (log10)", r => r.LcomMedia, "LCOM (log10)", true, true,
            true, Path.Combine(bonusDir, "pdf_11_rq04_tamanho_vs_lcom_scatter.png"));
    }

    private static void GerarBoxplotsRqs(List<RepoRow> rows, string bonusDir)
    {
        GerarBoxplotPorQuartis(rows, r => r.Estrelas, r => r.CboMedia,
            "RQ01 — Quartis de Estrelas vs CBO (boxplot)",
            Path.Combine(bonusDir, "pdf_18_rq01_boxplot_estrelas_cbo.png"));

        GerarBoxplotPorQuartis(rows, r => r.IdadeAnos, r => r.DitMedia,
            "RQ02 — Quartis de Idade vs DIT (boxplot)",
            Path.Combine(bonusDir, "pdf_19_rq02_boxplot_idade_dit.png"));

        GerarBoxplotPorQuartis(rows, r => r.Releases, r => r.CboMedia,
            "RQ03 — Quartis de Releases vs CBO (boxplot)",
            Path.Combine(bonusDir, "pdf_20_rq03_boxplot_releases_cbo.png"));

        GerarBoxplotPorQuartis(rows, r => r.LocJava, r => r.LcomMedia,
            "RQ04 — Quartis de LOC vs LCOM (boxplot)",
            Path.Combine(bonusDir, "pdf_21_rq04_boxplot_loc_lcom.png"));
    }

    private static void GerarHeatmapCorrelacoes(List<RepoRow> rows, string bonusDir)
    {
        var vars = new (string Nome, Func<RepoRow, double?> Sel)[]
        {
            ("Estrelas", r => r.Estrelas),
            ("Idade", r => r.IdadeAnos),
            ("Releases", r => r.Releases),
            ("LOC", r => r.LocJava),
            ("CBO", r => r.CboMedia),
            ("DIT", r => r.DitMedia),
            ("LCOM", r => r.LcomMedia)
        };

        var n = vars.Length;
        var data = new double[n, n];
        for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
            data[i, j] = i == j ? 1.0 : CorrelacaoPearsonPares(rows, vars[i].Sel, vars[j].Sel) ?? 0;

        var plt = new ScottPlot.Plot();
        plt.Add.Heatmap(data);
        var labels = vars.Select(v => v.Nome).ToArray();
        var pos = Enumerable.Range(0, n).Select(x => (double)x).ToArray();
        plt.Axes.Bottom.SetTicks(pos, labels);
        plt.Axes.Left.SetTicks(pos, labels);
        plt.Title("Heatmap de correlações (Pearson)");
        plt.SavePng(Path.Combine(bonusDir, "pdf_22_heatmap_correlacoes.png"), 1100, 800);
    }

    private static double? CorrelacaoPearsonPares(
        List<RepoRow> rows,
        Func<RepoRow, double?> xSel,
        Func<RepoRow, double?> ySel)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        foreach (var r in rows)
        {
            var x = xSel(r);
            var y = ySel(r);
            if (!x.HasValue || !y.HasValue ||
                double.IsNaN(x.Value) || double.IsInfinity(x.Value) ||
                double.IsNaN(y.Value) || double.IsInfinity(y.Value))
                continue;
            xs.Add(x.Value);
            ys.Add(y.Value);
        }

        if (xs.Count < 4) return null;
        return Correlation.Pearson(xs.ToArray(), ys.ToArray());
    }

    private static void GerarBoxplotPorQuartis(
        List<RepoRow> rows,
        Func<RepoRow, double?> xSel,
        Func<RepoRow, double?> ySel,
        string titulo,
        string path)
    {
        var pares = rows
            .Select(r => (x: xSel(r), y: ySel(r)))
            .Where(t => t.x.HasValue && t.y.HasValue &&
                        !double.IsNaN(t.x!.Value) && !double.IsInfinity(t.x.Value) &&
                        !double.IsNaN(t.y!.Value) && !double.IsInfinity(t.y.Value))
            .Select(t => (x: t.x!.Value, y: t.y!.Value))
            .ToList();

        if (pares.Count < 12) return;

        var xsOrd = pares.Select(p => p.x).OrderBy(v => v).ToList();
        var q1 = xsOrd[xsOrd.Count / 4];
        var q2 = xsOrd[xsOrd.Count / 2];
        var q3 = xsOrd[(xsOrd.Count * 3) / 4];

        int Faixa(double x)
        {
            if (x <= q1) return 0;
            if (x <= q2) return 1;
            if (x <= q3) return 2;
            return 3;
        }

        var grupos = new List<double>[] { new(), new(), new(), new() };
        foreach (var p in pares)
            grupos[Faixa(p.x)].Add(p.y);

        var plt = new ScottPlot.Plot();
        for (var i = 0; i < 4; i++)
        {
            if (grupos[i].Count < 3) continue;
            var stats = CincoNumeros(grupos[i]);
            DesenharBoxplot(plt, i + 1, stats.min, stats.q1, stats.med, stats.q3, stats.max);
        }

        var ticks = new[] { 1d, 2d, 3d, 4d };
        plt.Axes.Bottom.SetTicks(ticks, new[] { "Q1", "Q2", "Q3", "Q4" });
        plt.Title(titulo);
        plt.XLabel("Quartis da variável de processo");
        plt.YLabel("Distribuição da métrica de qualidade");
        plt.SavePng(path, 1100, 600);
    }

    private static (double min, double q1, double med, double q3, double max) CincoNumeros(List<double> v)
    {
        var o = v.OrderBy(x => x).ToList();
        var n = o.Count;
        return (
            o.First(),
            o[n / 4],
            n % 2 == 1 ? o[n / 2] : (o[n / 2 - 1] + o[n / 2]) / 2,
            o[(n * 3) / 4],
            o.Last()
        );
    }

    private static void DesenharBoxplot(ScottPlot.Plot plt, double x, double min, double q1, double med, double q3, double max)
    {
        const double w = 0.25;
        plt.Add.Scatter(new[] { x, x }, new[] { min, max });
        plt.Add.Scatter(new[] { x - w, x + w }, new[] { q1, q1 });
        plt.Add.Scatter(new[] { x - w, x + w }, new[] { q3, q3 });
        plt.Add.Scatter(new[] { x - w, x - w }, new[] { q1, q3 });
        plt.Add.Scatter(new[] { x + w, x + w }, new[] { q1, q3 });
        plt.Add.Scatter(new[] { x - w, x + w }, new[] { med, med });
    }

    private static void GerarGraficosHipoteses(List<RepoRow> rows, string bonusDir)
    {
        GerarBarrasMedianasPorTamanho(rows, r => r.CboMedia, "Hipótese 1 — CBO por tamanho (LOC)",
            Path.Combine(bonusDir, "pdf_12_hip1_cbo_por_tamanho.png"));
        GerarBarrasMedianasPorTamanho(rows, r => r.LcomMedia, "Hipótese 1 — LCOM por tamanho (LOC)",
            Path.Combine(bonusDir, "pdf_13_hip1_lcom_por_tamanho.png"));

        GerarScatterRq(rows, r => r.Releases, "Releases (log10)", r => r.CboMedia, "CBO", true, false,
            true, Path.Combine(bonusDir, "pdf_14_hip2_releases_vs_cbo_trend.png"));
        GerarScatterRq(rows, r => r.Releases, "Releases (log10)", r => r.DitMedia, "DIT", true, false,
            true, Path.Combine(bonusDir, "pdf_15_hip2_releases_vs_dit_trend.png"));

        GerarBarrasDitPorFaixaIdade(rows, Path.Combine(bonusDir, "pdf_16_hip3_dit_por_faixa_idade.png"));
    }

    private static void GerarGraficosQuestoesAdicionais(List<RepoRow> rows, string bonusDir)
    {
        GerarLinhasTamanhoVsCboLcom(rows,
            Path.Combine(bonusDir, "pdf_23_rq05_loc_vs_cbo_lcom_por_tercis.png"));

        GerarLinhasReleasesVsCboDit(rows,
            Path.Combine(bonusDir, "pdf_24_rq06_releases_vs_cbo_dit_quartis.png"));

        GerarBoxplotDitPorFaixaIdade(rows,
            Path.Combine(bonusDir, "pdf_25_rq07_idade_vs_dit_boxplot.png"));
    }

    private static void GerarLinhasTamanhoVsCboLcom(List<RepoRow> rows, string path)
    {
        var comLoc = rows
            .Where(r => r.LocJava.HasValue && r.LocJava.Value > 0)
            .OrderBy(r => r.LocJava)
            .ToList();
        if (comLoc.Count < 12) return;

        var q1 = comLoc[comLoc.Count / 3].LocJava!.Value;
        var q2 = comLoc[(comLoc.Count * 2) / 3].LocJava!.Value;

        int Grupo(double loc)
        {
            if (loc <= q1) return 0;
            if (loc <= q2) return 1;
            return 2;
        }

        var cbo = new List<double>[] { new(), new(), new() };
        var lcom = new List<double>[] { new(), new(), new() };

        foreach (var r in comLoc)
        {
            var g = Grupo(r.LocJava!.Value);
            if (r.CboMedia.HasValue && !double.IsNaN(r.CboMedia.Value) && !double.IsInfinity(r.CboMedia.Value))
                cbo[g].Add(r.CboMedia.Value);
            if (r.LcomMedia.HasValue && !double.IsNaN(r.LcomMedia.Value) && !double.IsInfinity(r.LcomMedia.Value))
                lcom[g].Add(r.LcomMedia.Value);
        }

        if (cbo.All(g => g.Count == 0) && lcom.All(g => g.Count == 0)) return;

        var x = new[] { 0d, 1d, 2d };
        var cboMed = cbo.Select(Mediana).ToArray();
        var lcomMed = lcom.Select(Mediana).ToArray();
        var plt = new ScottPlot.Plot();

        var s1 = plt.Add.Scatter(x, cboMed);
        s1.LegendText = "CBO (mediana)";
        s1.MarkerSize = 6;

        var s2 = plt.Add.Scatter(x, lcomMed);
        s2.LegendText = "LCOM (mediana)";
        s2.MarkerSize = 6;

        plt.Axes.Bottom.SetTicks(x, new[] { "Pequeno", "Médio", "Grande" });
        plt.Title("RQ05 — Crescimento (LOC) vs CBO/LCOM por grupos de tamanho");
        plt.XLabel("Grupo de tamanho (tercis de LOC Java)");
        plt.YLabel("Mediana da métrica CK");
        plt.ShowLegend();
        plt.SavePng(path, 1100, 560);
    }

    private static void GerarLinhasReleasesVsCboDit(List<RepoRow> rows, string path)
    {
        var comRel = rows
            .Where(r => r.Releases.HasValue &&
                        !double.IsNaN(r.Releases.Value) &&
                        !double.IsInfinity(r.Releases.Value))
            .OrderBy(r => r.Releases)
            .ToList();
        if (comRel.Count < 12) return;

        var valores = comRel.Select(r => r.Releases!.Value).OrderBy(v => v).ToList();
        var q1 = valores[valores.Count / 4];
        var q2 = valores[valores.Count / 2];
        var q3 = valores[(valores.Count * 3) / 4];

        int Faixa(double x)
        {
            if (x <= q1) return 0;
            if (x <= q2) return 1;
            if (x <= q3) return 2;
            return 3;
        }

        var cbo = new List<double>[] { new(), new(), new(), new() };
        var dit = new List<double>[] { new(), new(), new(), new() };

        foreach (var r in comRel)
        {
            var g = Faixa(r.Releases!.Value);
            if (r.CboMedia.HasValue && !double.IsNaN(r.CboMedia.Value) && !double.IsInfinity(r.CboMedia.Value))
                cbo[g].Add(r.CboMedia.Value);
            if (r.DitMedia.HasValue && !double.IsNaN(r.DitMedia.Value) && !double.IsInfinity(r.DitMedia.Value))
                dit[g].Add(r.DitMedia.Value);
        }

        if (cbo.All(g => g.Count == 0) && dit.All(g => g.Count == 0)) return;

        var x = new[] { 1d, 2d, 3d, 4d };
        var cboMed = cbo.Select(Mediana).ToArray();
        var ditMed = dit.Select(Mediana).ToArray();
        var plt = new ScottPlot.Plot();

        var s1 = plt.Add.Scatter(x, cboMed);
        s1.LegendText = "CBO (mediana)";
        s1.MarkerSize = 6;

        var s2 = plt.Add.Scatter(x, ditMed);
        s2.LegendText = "DIT (mediana)";
        s2.MarkerSize = 6;

        plt.Axes.Bottom.SetTicks(x, new[] { "Q1", "Q2", "Q3", "Q4" });
        plt.Title("RQ06 — Releases vs CBO/DIT por quartis de atividade");
        plt.XLabel("Quartis de releases");
        plt.YLabel("Mediana da métrica CK");
        plt.ShowLegend();
        plt.SavePng(path, 1100, 560);
    }

    private static void GerarBoxplotDitPorFaixaIdade(List<RepoRow> rows, string path)
    {
        var g1 = new List<double>();
        var g2 = new List<double>();
        var g3 = new List<double>();

        foreach (var r in rows)
        {
            if (!r.IdadeAnos.HasValue || !r.DitMedia.HasValue) continue;
            var idade = r.IdadeAnos.Value;
            var dit = r.DitMedia.Value;
            if (double.IsNaN(dit) || double.IsInfinity(dit)) continue;

            if (idade < 3) g1.Add(dit);
            else if (idade <= 7) g2.Add(dit);
            else g3.Add(dit);
        }

        if (g1.Count < 3 && g2.Count < 3 && g3.Count < 3) return;

        var plt = new ScottPlot.Plot();
        if (g1.Count >= 3)
        {
            var s = CincoNumeros(g1);
            DesenharBoxplot(plt, 1, s.min, s.q1, s.med, s.q3, s.max);
        }

        if (g2.Count >= 3)
        {
            var s = CincoNumeros(g2);
            DesenharBoxplot(plt, 2, s.min, s.q1, s.med, s.q3, s.max);
        }

        if (g3.Count >= 3)
        {
            var s = CincoNumeros(g3);
            DesenharBoxplot(plt, 3, s.min, s.q1, s.med, s.q3, s.max);
        }

        plt.Axes.Bottom.SetTicks(new[] { 1d, 2d, 3d }, new[] { "< 3 anos", "3 a 7 anos", "> 7 anos" });
        plt.Title("RQ07 — DIT por faixa de idade (boxplot)");
        plt.XLabel("Faixa etária do repositório");
        plt.YLabel("Distribuição de DIT");
        plt.SavePng(path, 1050, 560);
    }

    private static void GerarScatterRq(
        List<RepoRow> rows,
        Func<RepoRow, double?> xSel,
        string nomeX,
        Func<RepoRow, double?> ySel,
        string nomeY,
        bool logX,
        bool logY,
        bool trend,
        string path)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        foreach (var r in rows)
        {
            var xv = xSel(r);
            var yv = ySel(r);
            if (!xv.HasValue || !yv.HasValue ||
                double.IsNaN(xv.Value) || double.IsInfinity(xv.Value) ||
                double.IsNaN(yv.Value) || double.IsInfinity(yv.Value))
                continue;

            var x = xv.Value;
            if (logX)
                x = Math.Log10(Math.Max(1, x));

            xs.Add(x);
            var y = yv.Value;
            if (logY)
                y = Math.Log10(Math.Max(1, y));

            ys.Add(y);
        }

        if (xs.Count < 5) return;

        var xa = xs.ToArray();
        var ya = ys.ToArray();
        var plt = new ScottPlot.Plot();
        var pontos = plt.Add.Scatter(xa, ya);
        pontos.LineWidth = 0;
        pontos.MarkerSize = 3;
        if (trend)
            AdicionarTendencia(plt, xa, ya);

        plt.Title($"{nomeX} × {nomeY}");
        plt.XLabel(nomeX);
        plt.YLabel(nomeY);
        plt.SavePng(path, 1100, 600);
    }

    private static void AdicionarTendencia(ScottPlot.Plot plt, double[] x, double[] y)
    {
        if (x.Length < 2) return;
        var mx = x.Average();
        var my = y.Average();
        var den = 0.0;
        var num = 0.0;
        for (var i = 0; i < x.Length; i++)
        {
            var dx = x[i] - mx;
            den += dx * dx;
            num += dx * (y[i] - my);
        }

        if (Math.Abs(den) < 1e-12) return;
        var b = num / den;
        var a = my - b * mx;
        var x1 = x.Min();
        var x2 = x.Max();
        var y1 = a + b * x1;
        var y2 = a + b * x2;
        var linha = plt.Add.Scatter(new[] { x1, x2 }, new[] { y1, y2 });
        linha.MarkerSize = 0;
        linha.LineWidth = 2;
    }

    private static void GerarBarrasMedianasPorTamanho(
        List<RepoRow> rows,
        Func<RepoRow, double?> ySel,
        string titulo,
        string path)
    {
        var comLoc = rows.Where(r => r.LocJava.HasValue && r.LocJava > 0).OrderBy(r => r.LocJava).ToList();
        if (comLoc.Count < 9) return;

        var q1 = comLoc[comLoc.Count / 3].LocJava!.Value;
        var q2 = comLoc[(comLoc.Count * 2) / 3].LocJava!.Value;

        var pequenos = new List<double>();
        var medios = new List<double>();
        var grandes = new List<double>();

        foreach (var r in comLoc)
        {
            var y = ySel(r);
            if (!y.HasValue || double.IsNaN(y.Value) || double.IsInfinity(y.Value)) continue;
            var loc = r.LocJava!.Value;
            if (loc <= q1) pequenos.Add(y.Value);
            else if (loc <= q2) medios.Add(y.Value);
            else grandes.Add(y.Value);
        }

        if (pequenos.Count == 0 && medios.Count == 0 && grandes.Count == 0) return;

        var vals = new[]
        {
            Mediana(pequenos),
            Mediana(medios),
            Mediana(grandes)
        };
        var pos = new[] { 0d, 1d, 2d };
        var plt = new ScottPlot.Plot();
        plt.Add.Bars(pos, vals);
        plt.Axes.Bottom.SetTicks(pos, new[] { "Pequeno", "Médio", "Grande" });
        plt.Title(titulo + " (mediana por grupo)");
        plt.YLabel("Valor da métrica");
        plt.SavePng(path, 1000, 520);
    }

    private static void GerarBarrasDitPorFaixaIdade(List<RepoRow> rows, string path)
    {
        var g1 = new List<double>();
        var g2 = new List<double>();
        var g3 = new List<double>();

        foreach (var r in rows)
        {
            if (!r.IdadeAnos.HasValue || !r.DitMedia.HasValue) continue;
            var idade = r.IdadeAnos.Value;
            var dit = r.DitMedia.Value;
            if (double.IsNaN(dit) || double.IsInfinity(dit)) continue;
            if (idade < 3) g1.Add(dit);
            else if (idade <= 7) g2.Add(dit);
            else g3.Add(dit);
        }

        if (g1.Count == 0 && g2.Count == 0 && g3.Count == 0) return;

        var vals = new[] { Mediana(g1), Mediana(g2), Mediana(g3) };
        var pos = new[] { 0d, 1d, 2d };
        var plt = new ScottPlot.Plot();
        plt.Add.Bars(pos, vals);
        plt.Axes.Bottom.SetTicks(pos, new[] { "< 3 anos", "3 a 7 anos", "> 7 anos" });
        plt.Title("Hipótese 3 — DIT por faixa de idade (mediana)");
        plt.YLabel("DIT");
        plt.SavePng(path, 1000, 520);
    }

    private static double Mediana(List<double> vals)
    {
        if (vals.Count == 0) return 0;
        var o = vals.OrderBy(x => x).ToList();
        var n = o.Count;
        return n % 2 == 1 ? o[n / 2] : (o[n / 2 - 1] + o[n / 2]) / 2;
    }

    private static void GerarBarrasMediasProcesso(List<RepoRow> rows, string path)
    {
        var e = ValoresValidos(rows.Select(r => r.Estrelas));
        var id = ValoresValidos(rows.Select(r => r.IdadeAnos));
        var rel = ValoresValidos(rows.Select(r => r.Releases));
        if (e.Count == 0 && id.Count == 0 && rel.Count == 0) return;

        // Escala: estrelas em milhares para caber no mesmo gráfico que idade/releases
        var v1 = e.Count > 0 ? e.Average() / 1000.0 : 0;
        var v2 = id.Count > 0 ? id.Average() : 0;
        var v3 = rel.Count > 0 ? rel.Average() : 0;
        var starsLabel = e.Count > 0 ? $"Estrelas (÷1000)\n≈{e.Average():F0} ★" : "Estrelas (÷1000)\n—";
        var labels = new[] { starsLabel, $"Idade (anos)\n{v2:F2}", $"Releases (méd.)\n{v3:F2}" };
        var vals = new[] { v1, v2, v3 };

        var plt = new ScottPlot.Plot();
        var positions = Enumerable.Range(0, vals.Length).Select(i => (double)i).ToArray();
        var bars = plt.Add.Bars(positions, vals);
        bars.Horizontal = true;
        plt.Axes.Left.SetTicks(positions, labels);
        plt.Title("Médias globais — métricas de processo");
        plt.Axes.Margins(left: 0.15);
        plt.SavePng(path, 900, 420);
    }

    private static void GerarHistograma(List<RepoRow> rows, Func<RepoRow, double?> sel, string tituloY, string path)
    {
        var v = ValoresValidos(rows.Select(sel));
        if (v.Count < 5) return;
        var min = v.Min();
        var max = v.Max();
        if (Math.Abs(max - min) < 1e-9) return;
        const int bins = 16;
        var counts = new double[bins];
        foreach (var x in v)
        {
            var t = (x - min) / (max - min);
            var idx = (int)Math.Clamp(t * bins, 0, bins - 1);
            counts[idx]++;
        }

        var plt = new ScottPlot.Plot();
        var xs = Enumerable.Range(0, bins).Select(i => (double)i).ToArray();
        plt.Add.Bars(xs, counts);
        plt.Title("Histograma — " + tituloY);
        plt.XLabel("Faixa (índice)");
        plt.YLabel("Nº repositórios");
        plt.SavePng(path, 900, 500);
    }

    private static void GerarBarrasQuartilContagem(List<RepoRow> rows, Func<RepoRow, double?> sel, string titulo, string path)
    {
        var com = rows.Where(r =>
        {
            var v = sel(r);
            return v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value);
        }).ToList();
        if (com.Count < 4) return;
        var valores = com.Select(r => sel(r)!.Value).OrderBy(x => x).ToList();
        var q1 = valores[valores.Count / 4];
        var q2 = valores[valores.Count / 2];
        var q3 = valores[(valores.Count * 3) / 4];

        int Faixa(double x)
        {
            if (x <= q1) return 0;
            if (x <= q2) return 1;
            if (x <= q3) return 2;
            return 3;
        }

        var cnt = new double[4];
        foreach (var r in com)
            cnt[Faixa(sel(r)!.Value)]++;

        var plt = new ScottPlot.Plot();
        var labels = new[] { "Q1 (baixo)", "Q2", "Q3", "Q4 (alto)" };
        var positions = new double[] { 0, 1, 2, 3 };
        plt.Add.Bars(positions, cnt);
        plt.Axes.Bottom.SetTicks(positions, labels);
        plt.Title(titulo + $" (n total = {com.Count})");
        plt.YLabel("Repositórios");
        plt.SavePng(path, 900, 480);
    }

    private static void GerarBarrasMediasCk(List<RepoRow> rows, string path)
    {
        var com = rows.Where(r =>
            r.TemCk &&
            !double.IsNaN(r.CboMedia!.Value) && !double.IsInfinity(r.CboMedia.Value) &&
            !double.IsNaN(r.DitMedia!.Value) && !double.IsInfinity(r.DitMedia.Value) &&
            !double.IsNaN(r.LcomMedia!.Value) && !double.IsInfinity(r.LcomMedia.Value)).ToList();
        if (com.Count == 0) return;
        var cbo = com.Average(r => r.CboMedia!.Value);
        var dit = com.Average(r => r.DitMedia!.Value);
        var lcom = com.Average(r => r.LcomMedia!.Value);

        var plt = new ScottPlot.Plot();
        var vals = new[] { cbo, dit, lcom };
        var positions = new[] { 0d, 1, 2 };
        var bars = plt.Add.Bars(positions, vals);
        bars.Horizontal = true;
        plt.Axes.Left.SetTicks(positions, new[] { "CBO médio", "DIT médio", "LCOM médio" });
        plt.Title($"Médias CK entre repositórios medidos (n = {com.Count})");
        plt.SavePng(path, 900, 380);
    }

    private static void GerarCurvasCkPorQuartil(
        List<RepoRow> rows,
        Func<RepoRow, double?> processoSelector,
        string titulo,
        string path)
    {
        var comProcesso = rows.Where(r =>
        {
            var v = processoSelector(r);
            return v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value);
        }).ToList();

        if (comProcesso.Count < 8)
            return;

        var valores = comProcesso.Select(r => processoSelector(r)!.Value).OrderBy(x => x).ToList();
        var q1 = valores[valores.Count / 4];
        var q2 = valores[valores.Count / 2];
        var q3 = valores[(valores.Count * 3) / 4];

        int Faixa(double v)
        {
            if (v <= q1) return 0;
            if (v <= q2) return 1;
            if (v <= q3) return 2;
            return 3;
        }

        var grupos = new List<RepoRow>[4]
        {
            new(), new(), new(), new()
        };

        foreach (var r in comProcesso)
        {
            if (!r.TemCk) continue;
            grupos[Faixa(processoSelector(r)!.Value)].Add(r);
        }

        if (grupos.All(g => g.Count == 0))
            return;

        var x = new[] { 1d, 2d, 3d, 4d };
        var cbo = grupos.Select(g => g.Count > 0 ? g.Average(r => r.CboMedia!.Value) : double.NaN).ToArray();
        var dit = grupos.Select(g => g.Count > 0 ? g.Average(r => r.DitMedia!.Value) : double.NaN).ToArray();
        var lcom = grupos.Select(g => g.Count > 0 ? g.Average(r => r.LcomMedia!.Value) : double.NaN).ToArray();

        var plt = new ScottPlot.Plot();
        plt.Add.Scatter(x, cbo);
        plt.Add.Scatter(x, dit);
        plt.Add.Scatter(x, lcom);
        plt.Axes.Bottom.SetTicks(x, new[] { "Q1", "Q2", "Q3", "Q4" });
        plt.Title(titulo);
        plt.XLabel("Quartis da métrica de processo");
        plt.YLabel("Média da métrica CK (linhas por cor: CBO, DIT, LCOM)");
        plt.SavePng(path, 1100, 520);
    }

    private static List<double> ValoresValidos(IEnumerable<double?> origem) =>
        origem.Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Where(x => !double.IsNaN(x) && !double.IsInfinity(x))
            .ToList();
}

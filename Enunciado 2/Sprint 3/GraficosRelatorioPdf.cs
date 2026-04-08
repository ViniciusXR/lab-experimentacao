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
    }

    private static void GerarBarrasMediasProcesso(List<RepoRow> rows, string path)
    {
        var e = rows.Where(r => r.Estrelas.HasValue).Select(r => r.Estrelas!.Value).ToList();
        var id = rows.Where(r => r.IdadeAnos.HasValue).Select(r => r.IdadeAnos!.Value).ToList();
        var rel = rows.Where(r => r.Releases.HasValue).Select(r => r.Releases!.Value).ToList();
        if (e.Count == 0 && id.Count == 0 && rel.Count == 0) return;

        // Escala: estrelas em milhares para caber no mesmo gráfico que idade/releases
        var v1 = e.Count > 0 ? e.Average() / 1000.0 : 0;
        var v2 = id.Count > 0 ? id.Average() : 0;
        var v3 = rel.Count > 0 ? rel.Average() : 0;
        var labels = new[] { $"Estrelas (÷1000)\n≈{e.Average():F0} ★", $"Idade (anos)\n{v2:F2}", $"Releases (méd.)\n{v3:F2}" };
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
        var v = rows.Select(sel).Where(x => x.HasValue).Select(x => x!.Value).ToList();
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
        var com = rows.Where(r => sel(r).HasValue).ToList();
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
        var com = rows.Where(r => r.TemCk).ToList();
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
}

using Enunciado4.Sprint1.Data;
using Enunciado4.Sprint1.Models;
using Enunciado4.Sprint1.Report;
using Enunciado4.Sprint1.Visualization;

namespace Enunciado4.Sprint1;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Enunciado 4 — Lab04 Completo (Sprints 1, 2 e 3)");
        Console.WriteLine(new string('=', 60));

        var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var dadosDir = Path.Combine(pastaProjeto, "dados");
        var outputDir = Path.Combine(pastaProjeto, "lab04_entrega");

        if (!Directory.Exists(dadosDir))
        {
            Console.WriteLine($"ERRO: pasta de dados não encontrada em {dadosDir}");
            return;
        }

        var graficosDir = Path.Combine(outputDir, "graficos");
        var p1Dir = Path.Combine(graficosDir, "p1_caracterizacao");
        var rq1Dir = Path.Combine(graficosDir, "p2_rq1");
        var rq2Dir = Path.Combine(graficosDir, "p3_rq2");
        var rq3Dir = Path.Combine(graficosDir, "p4_rq3");
        var relatoriosDir = Path.Combine(outputDir, "relatorios");
        var dashboardsDir = Path.Combine(outputDir, "dashboards");

        Console.WriteLine($"Carregando dados de: {dadosDir}");
        var data = CsvLoader.LoadFull(dadosDir);

        Console.WriteLine();
        Console.WriteLine("[Sprint 1] Caracterização do dataset...");
        var chartsP1 = DatasetCharacterizationCharts.GenerateAll(data.Characterization, p1Dir);
        LogCharts(chartsP1);

        Console.WriteLine();
        Console.WriteLine("[Sprint 2] RQ1 — Frequência...");
        var chartsRq1 = Rq1Charts.GenerateAll(data, rq1Dir);
        LogCharts(chartsRq1);

        Console.WriteLine();
        Console.WriteLine("[Sprint 2] RQ2 — Severidade...");
        var chartsRq2 = Rq2Charts.GenerateAll(data, rq2Dir);
        LogCharts(chartsRq2);

        Console.WriteLine();
        Console.WriteLine("[Sprint 3] RQ3 — Interação + dashboard final...");
        var chartsRq3 = Rq3Charts.GenerateAll(data, rq3Dir);
        LogCharts(chartsRq3);

        var allCharts = new GeneratedCharts(chartsP1, chartsRq1, chartsRq2, chartsRq3);

        Console.WriteLine();
        Console.WriteLine("Gerando relatórios...");
        await Lab04ReportGenerator.GenerateAllAsync(data, allCharts, relatoriosDir);
        Console.WriteLine("  ✓ relatorios/*.md");

        Console.WriteLine();
        Console.WriteLine("Gerando dashboards HTML...");
        await Lab04DashboardHtml.GenerateAllAsync(data, allCharts, dashboardsDir);
        Console.WriteLine("  ✓ dashboards/sprint1.html");
        Console.WriteLine("  ✓ dashboards/sprint2.html");
        Console.WriteLine("  ✓ dashboards/dashboard_final.html");

        Console.WriteLine();
        Console.WriteLine("Lab04 COMPLETO gerado com sucesso!");
        Console.WriteLine($"Pasta de entrega: {outputDir}");
        Console.WriteLine();
        Console.WriteLine("Próximos passos:");
        Console.WriteLine("  1. Abra dashboards/dashboard_final.html no navegador");
        Console.WriteLine("  2. Ctrl+P → Salvar como PDF (entrega Sprint 3)");
        Console.WriteLine("  3. Leia relatorios/artigo_ti6_atualizado.md para o artigo");
        Console.WriteLine("  4. (Opcional) Replique no Power BI com GUIA_DASHBOARD_POWERBI.md");
    }

    private static void LogCharts(IReadOnlyList<string> charts)
    {
        foreach (var c in charts)
            Console.WriteLine($"  ✓ {Path.GetFileName(c)}");
    }
}

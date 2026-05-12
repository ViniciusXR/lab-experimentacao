using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Lab03S03.Analysis;
using Lab03S03.Models;
using Lab03S03.Report;
using Lab03S03.Visualization;

namespace Enunciado3.Sprint3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Lab03S03: Análise, Visualização e Relatório Final");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "Enunciado 3"));

            string datasetPath = Path.Combine(projRoot, "Sprint 2", "enunciado3_sprint2_output", "dataset.csv");
            if (!File.Exists(datasetPath))
            {
                 datasetPath = Path.Combine(projRoot, "Sprint 1", "dataset.csv");
            }
            if (!File.Exists(datasetPath))
            {
                 Console.WriteLine("Dataset não encontrado! Dataser esperado em: " + datasetPath);
                 return;
            }

            Console.WriteLine($"Lendo dataset de: {datasetPath}");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                Delimiter = ";"
            };

            List<PullRequest> prs = new List<PullRequest>();
            using (var reader = new StreamReader(datasetPath))
            using (var csv = new CsvReader(reader, config))
            {
                prs = csv.GetRecords<PullRequest>().ToList();
            }

            Console.WriteLine($"Carregados {prs.Count} PRs.");

            List<AnalysisResult> results = new List<AnalysisResult>();
            string[] rqs = { "RQ01", "RQ02", "RQ03", "RQ04", "RQ05", "RQ06", "RQ07", "RQ08" };

            string sprint3Dir = Path.Combine(projRoot, "Sprint 3");

            string chartDir = Path.Combine(sprint3Dir, "output", "charts");
            if (!Directory.Exists(chartDir))
            {
                Directory.CreateDirectory(chartDir);
            }

            Console.WriteLine("Gerando fluxogramas de processos com SkiaSharp...");
            FlowchartBuilder.GenerateImages(chartDir);

            foreach (var rq in rqs)
            {
                Console.WriteLine($"Analisando {rq}...");
                var result = RQAnalyzer.Analyze(prs, rq);
                ChartBuilder.Generate(prs, result, chartDir);
                results.Add(result);
            }

            string reportDir = Path.Combine(sprint3Dir, "output");
            if (!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }
            string reportPath = Path.Combine(reportDir, "relatorio_lab03s03.pdf");

            Console.WriteLine("Gerando relatorio PDF...");
            ReportGenerator.Generate(results, reportPath, prs.Count);

            Console.WriteLine($"Relatório gerado com sucesso: {reportPath}");
        }
    }
}

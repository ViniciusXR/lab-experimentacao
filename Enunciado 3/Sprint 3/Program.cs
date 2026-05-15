using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Lab03S03.Analysis;
using Lab03S03.Models;
using Lab03S03.Report;
using Lab03S03.Visualization;

namespace Enunciado3.Sprint3;

internal static class Program
{
    /// <summary>
    /// Localiza a pasta "Enunciado 3" subindo a partir do executável, do processo ou do diretório atual.
    /// Evita contar ".." manualmente (quebra com publish/RID ou mudança de TFM).
    /// </summary>
    private static string ResolveEnunciado3Root()
    {
        IEnumerable<string> StartingPoints()
        {
            yield return AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                var d = Path.GetDirectoryName(Environment.ProcessPath);
                if (!string.IsNullOrEmpty(d))
                    yield return d;
            }

            yield return Directory.GetCurrentDirectory();
        }

        foreach (var start in StartingPoints().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(start))
                continue;

            var full = Path.GetFullPath(start);
            for (var dir = new DirectoryInfo(full); dir != null; dir = dir.Parent)
            {
                var sprint3Proj = Path.Combine(dir.FullName, "Sprint 3", "Sprint3.csproj");
                if (!File.Exists(sprint3Proj))
                    continue;

                var ds2 = Path.Combine(dir.FullName, "Sprint 2", "enunciado3_sprint2_output", "dataset.csv");
                var ds1 = Path.Combine(dir.FullName, "Sprint 1", "dataset.csv");
                if (File.Exists(ds2) || File.Exists(ds1))
                    return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Não foi possível localizar a pasta do Enunciado 3 (com Sprint 3/Sprint3.csproj e dataset em Sprint 2 ou Sprint 1). " +
            "Abra o terminal em 'Enunciado 3/Sprint 3' e execute: dotnet run");
    }

    private static void Main(string[] args)
    {
        Console.WriteLine("Lab03S03: Análise, Visualização e Relatório Final");
        Console.WriteLine($"Runtime: {Environment.Version} | OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

        try
        {
            string projRoot = args.Length > 0 && Directory.Exists(args[0])
                ? Path.GetFullPath(args[0])
                : ResolveEnunciado3Root();

            Console.WriteLine($"Raiz Enunciado 3: {projRoot}");

            string datasetPath = Path.Combine(projRoot, "Sprint 2", "enunciado3_sprint2_output", "dataset.csv");
            if (!File.Exists(datasetPath))
                datasetPath = Path.Combine(projRoot, "Sprint 1", "dataset.csv");

            if (!File.Exists(datasetPath))
            {
                Console.Error.WriteLine("Dataset não encontrado. Caminhos tentados:");
                Console.Error.WriteLine("  " + Path.Combine(projRoot, "Sprint 2", "enunciado3_sprint2_output", "dataset.csv"));
                Console.Error.WriteLine("  " + Path.Combine(projRoot, "Sprint 1", "dataset.csv"));
                Environment.ExitCode = 1;
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

            List<PullRequest> prs;
            using (var reader = new StreamReader(datasetPath))
            using (var csv = new CsvReader(reader, config))
            {
                prs = csv.GetRecords<PullRequest>().ToList();
            }

            Console.WriteLine($"Carregados {prs.Count} PRs.");

            var results = new List<AnalysisResult>();
            string[] rqs = { "RQ01", "RQ02", "RQ03", "RQ04", "RQ05", "RQ06", "RQ07", "RQ08" };

            string sprint3Dir = Path.Combine(projRoot, "Sprint 3");

            string chartDir = Path.Combine(sprint3Dir, "output", "charts");
            Directory.CreateDirectory(chartDir);

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
            Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, "relatorio_lab03s03.pdf");

            Console.WriteLine("Gerando relatorio PDF...");
            ReportGenerator.Generate(results, reportPath, prs);

            string reportRootCopy = Path.Combine(projRoot, "relatorio_lab03s03.pdf");
            File.Copy(reportPath, reportRootCopy, overwrite: true);

            Console.WriteLine($"Relatório gerado com sucesso: {reportPath}");
            Console.WriteLine($"Cópia na raiz do Enunciado 3: {reportRootCopy}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("ERRO ao executar o Lab03S03:");
            Console.Error.WriteLine(ex.ToString());
            Console.Error.WriteLine();
            Console.Error.WriteLine("Verifique: (1) SDK .NET 8 instalado — rode: dotnet --list-sdks");
            Console.Error.WriteLine("            (2) Pasta do projeto e dataset.csv conforme o repositório");
            Console.Error.WriteLine("            (3) Opcional: dotnet run -- \"/caminho/absoluto/para/Enunciado 3\"");
            Environment.ExitCode = 1;
        }
    }
}

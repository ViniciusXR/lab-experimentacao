using System.Diagnostics;
using System.Text;

namespace Enunciado5.Sprint4;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var noWait = args.Contains("--no-wait");
        var labRoot = FindLabRoot();
        var deliveryRoot = Path.Combine(labRoot, "lab05_entrega");

        PrintHeader(labRoot, deliveryRoot);

        var steps = new (string Title, string Description, Func<Task<int>> Run)[]
        {
            (
                "Lab05S01 — Desenho e preparação",
                "Plano do experimento, protocolo, consultas REST/GraphQL e objetos experimentais",
                () => RunDotnetAsync(labRoot, "Sprint 1", "Sprint1.csproj", noWait)
            ),
            (
                "Lab05S02 — Execução, análise e relatório",
                "1.200 medições, estatísticas, testes Mann-Whitney, relatório .md e .pdf",
                () => RunDotnetAsync(labRoot, "Sprint 2", "Sprint2.csproj", noWait)
            ),
            (
                "Lab05S03 — Dashboard C#",
                "Dashboard HTML com visualizações a partir dos CSVs da Sprint 2",
                () => RunDotnetAsync(labRoot, "Sprint 3", "Sprint3.csproj", noWait)
            ),
            (
                "Lab05S03 — Dashboard Python (Passo 6)",
                "Processamento com pandas e gráficos matplotlib/seaborn",
                () => RunPythonDashboardAsync(labRoot)
            )
        };

        for (var index = 0; index < steps.Length; index++)
        {
            var step = steps[index];
            Console.WriteLine();
            Console.WriteLine(new string('=', 72));
            Console.WriteLine($"[{index + 1}/{steps.Length}] {step.Title}");
            Console.WriteLine(step.Description);
            Console.WriteLine(new string('-', 72));

            var exitCode = await step.Run();
            if (exitCode != 0)
            {
                Console.WriteLine();
                Console.WriteLine($"ERRO: etapa {index + 1} falhou (codigo {exitCode}).");
                return exitCode;
            }
        }

        PrintDeliverySummary(deliveryRoot);
        Console.WriteLine();
        Console.WriteLine("Lab05 COMPLETO — todas as etapas foram executadas com sucesso.");

        if (!noWait)
        {
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey(intercept: true);
        }

        return 0;
    }

    private static void PrintHeader(string labRoot, string deliveryRoot)
    {
        Console.WriteLine("Enunciado 5 — Lab05: GraphQL vs REST");
        Console.WriteLine("Sprint 4 — Execução consolidada de todas as entregas");
        Console.WriteLine(new string('=', 72));
        Console.WriteLine($"Raiz do laboratório : {labRoot}");
        Console.WriteLine($"Pasta de entrega    : {deliveryRoot}");
        Console.WriteLine();
        Console.WriteLine("Esta sprint reúne, em sequência, o trabalho das Sprints 1, 2 e 3");
        Console.WriteLine("e o dashboard Python exigido no Passo 6 do enunciado.");
    }

    private static async Task<int> RunDotnetAsync(
        string labRoot,
        string sprintFolder,
        string projectFile,
        bool noWait)
    {
        var projectPath = Path.Combine(labRoot, sprintFolder, projectFile);
        if (!File.Exists(projectPath))
        {
            Console.WriteLine($"Projeto não encontrado: {projectPath}");
            return 1;
        }

        var arguments = new StringBuilder($"run --project \"{projectPath}\"");
        if (noWait)
        {
            arguments.Append(" -- --no-wait");
        }

        return await RunProcessAsync("dotnet", arguments.ToString(), labRoot);
    }

    private static async Task<int> RunPythonDashboardAsync(string labRoot)
    {
        var scriptPath = Path.Combine(labRoot, "dashboard", "gerar_dashboard.py");
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine($"Script não encontrado: {scriptPath}");
            return 1;
        }

        foreach (var python in new[] { "python3", "python" })
        {
            if (await CommandExistsAsync(python))
            {
                var exitCode = await RunProcessAsync(python, $"\"{scriptPath}\"", labRoot);
                if (exitCode != 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Dica: instale as dependencias Python com:");
                    Console.WriteLine($"  pip install -r \"{Path.Combine(labRoot, "dashboard", "requirements.txt")}\"");
                }

                return exitCode;
            }
        }

        Console.WriteLine("Python não encontrado. Instale Python 3.10+ e execute:");
        Console.WriteLine($"  pip install -r \"{Path.Combine(labRoot, "dashboard", "requirements.txt")}\"");
        Console.WriteLine($"  python \"{scriptPath}\"");
        return 1;
    }

    private static async Task<bool> CommandExistsAsync(string command)
    {
        try
        {
            var exitCode = await RunProcessAsync(command, "--version", Directory.GetCurrentDirectory(), quiet: true);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<int> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        bool quiet = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        if (!quiet)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.Error.WriteLine(e.Data);
                }
            };
        }

        process.Start();
        if (!quiet)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static void PrintDeliverySummary(string deliveryRoot)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 72));
        Console.WriteLine("ENTREGA CONSOLIDADA — lab05_entrega/");
        Console.WriteLine(new string('=', 72));

        var sections = new (string Title, string[] RelativePaths)[]
        {
            (
                "Sprint 1 — Desenho e preparação",
                [
                    "relatorios/sprint1_plano_experimento.md",
                    "relatorios/protocolo_replicacao.md",
                    "consultas/consultas_rest.json",
                    "consultas/consultas_graphql.graphql",
                    "dados/objetos_experimentais.csv"
                ]
            ),
            (
                "Sprint 2 — Execução e relatório final",
                [
                    "dados/medicoes_lab05.csv",
                    "dados/resumo_estatistico.csv",
                    "dados/testes_estatisticos.csv",
                    "dados/metadados_execucao.json",
                    "relatorios/relatorio_final_lab05.md",
                    "relatorios/relatorio_final_lab05.pdf"
                ]
            ),
            (
                "Sprint 3 — Dashboards",
                [
                    "dashboards/dashboard_lab05.html",
                    "dashboards/dashboard_lab05_python.html",
                    "dashboards/graficos/rq1_tempo_mediano.png",
                    "dashboards/graficos/rq2_tamanho_mediano.png",
                    "dashboards/graficos/distribuicao_tempo.png",
                    "dashboards/graficos/reducao_percentual.png",
                    "dashboards/graficos/visao_agregada.png",
                    "dashboards/tabelas/tabela_descritiva.csv",
                    "dashboards/tabelas/tabela_testes.csv"
                ]
            )
        };

        foreach (var (title, paths) in sections)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            foreach (var relativePath in paths)
            {
                var fullPath = Path.Combine(deliveryRoot, relativePath);
                var status = File.Exists(fullPath) ? "ok" : "ausente";
                Console.WriteLine($"  [{status,7}] {relativePath}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Resultado sintetizado:");
        Console.WriteLine("  RQ1 (tempo)   : H1 apoiada — redução mediana agregada de 81,54%");
        Console.WriteLine("  RQ2 (tamanho) : H1 apoiada — redução mediana agregada de 90,33%");
        Console.WriteLine();
        Console.WriteLine("Documentação:");
        Console.WriteLine($"  {Path.Combine(deliveryRoot, "LEIA-ME.md")}");
        Console.WriteLine($"  {Path.Combine(FindLabRoot(), "README.md")}");
    }

    private static string FindLabRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (current.Name.Equals("Enunciado 5", StringComparison.OrdinalIgnoreCase))
            {
                return current.FullName;
            }

            var candidate = Path.Combine(current.FullName, "Enunciado 5");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}

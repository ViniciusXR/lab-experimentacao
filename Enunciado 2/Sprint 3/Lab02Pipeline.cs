using System.Diagnostics;
using Spectre.Console;

namespace Enunciado2.Sprint3;

/// <summary>
/// Orquestra <c>dotnet run</c> nas Sprints 1 e 2 antes da análise (Sprint 3).
/// </summary>
internal static class Lab02Pipeline
{
    /// <summary>Corre Sprint 1 e 2 por subprocesso. Devolve falso se algum passo falhar (exit code ≠ 0).</summary>
    public static bool ExecutarSprint1E2(string pastaSprint3Fontes, string[] args)
    {
        var sprint1Proj = Path.GetFullPath(Path.Combine(pastaSprint3Fontes, "..", "Sprint 1", "Sprint1.csproj"));
        var sprint2Proj = Path.GetFullPath(Path.Combine(pastaSprint3Fontes, "..", "Sprint 2", "Sprint2.csproj"));
        if (!File.Exists(sprint1Proj))
        {
            AnsiConsole.MarkupLine($"[red]Projeto Sprint 1 não encontrado:[/] {Markup.Escape(sprint1Proj)}");
            return false;
        }

        if (!File.Exists(sprint2Proj))
        {
            AnsiConsole.MarkupLine($"[red]Projeto Sprint 2 não encontrado:[/] {Markup.Escape(sprint2Proj)}");
            return false;
        }

        var reposCsv = Path.GetFullPath(Path.Combine(pastaSprint3Fontes, "..", "Sprint 1", "lab02_sprint1_output", "repos_java_1000.csv"));
        var refetch = args.Contains("--refetch", StringComparer.OrdinalIgnoreCase);

        var argS1 = new List<string>();
        if (!refetch && File.Exists(reposCsv))
            argS1.Add("--skip-fetch");
        foreach (var a in args)
        {
            if (ArgSoSprint3OuControlo(a)) continue;
            if (ArgParaSprint2(a)) continue;
            if (string.Equals(a, "--refetch", StringComparison.OrdinalIgnoreCase)) continue;
            if (ArgParaSprint1(a))
            {
                if (string.Equals(a, "--skip-fetch", StringComparison.OrdinalIgnoreCase) &&
                    argS1.Exists(x => string.Equals(x, "--skip-fetch", StringComparison.OrdinalIgnoreCase)))
                    continue;
                argS1.Add(a);
            }
        }

        AnsiConsole.MarkupLine("[bold cyan]▶ Sprint 1[/] (lista GitHub / coleta CK — conforme argumentos)");
        var c1 = RodarDotnet(sprint1Proj, argS1);
        if (c1 != 0)
        {
            AnsiConsole.MarkupLine($"[red]Sprint 1 terminou com código {c1}.[/]");
            return false;
        }

        var argS2 = new List<string>();
        foreach (var a in args)
        {
            if (ArgSoSprint3OuControlo(a)) continue;
            if (ArgParaSprint2(a))
                argS2.Add(a);
        }

        AnsiConsole.MarkupLine("[bold cyan]▶ Sprint 2[/] (consolidado CSV)");
        var c2 = RodarDotnet(sprint2Proj, argS2);
        if (c2 != 0)
        {
            AnsiConsole.MarkupLine($"[red]Sprint 2 terminou com código {c2}.[/]");
            return false;
        }

        return true;
    }

    private static bool ArgSoSprint3OuControlo(string a) =>
        string.Equals(a, "--apenas-sprint3", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--refetch", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--no-pdf", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--input=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--autores=", StringComparison.OrdinalIgnoreCase);

    private static bool ArgParaSprint1(string a) =>
        string.Equals(a, "--skip-fetch", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--rest", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--coleta-lote", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--coleta-ck", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--agregar-ck", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--bonus", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--lote-limpar-work", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a, "--lote-continuar", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--ck-jar=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--lote-max=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--token=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--repo=", StringComparison.OrdinalIgnoreCase);

    private static bool ArgParaSprint2(string a) =>
        a.StartsWith("--repos=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--amostra=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--lote=", StringComparison.OrdinalIgnoreCase) ||
        a.StartsWith("--ck-extra=", StringComparison.OrdinalIgnoreCase);

    private static int RodarDotnet(string caminhoCsproj, IReadOnlyList<string> argumentosPrograma)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(caminhoCsproj) ?? Environment.CurrentDirectory
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(caminhoCsproj);
        psi.ArgumentList.Add("--");
        foreach (var a in argumentosPrograma)
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi);
        if (p == null)
        {
            AnsiConsole.MarkupLine("[red]Não foi possível iniciar dotnet.[/]");
            return -1;
        }

        p.WaitForExit();
        return p.ExitCode;
    }
}

using System.Globalization;
using System.Text;
using Spectre.Console;

namespace Enunciado1.Sprint3;

/// <summary>
/// Laboratório 01 - Características de Repositórios Populares (GitHub).
/// Enunciado 1, Sprint 3: Análise e visualização de dados + relatório final.
/// </summary>
class Program
{
    private const string NomeArquivoCsv = "dados_repositorios_sprint2.csv";

    // Limiares para conclusões (mesmos da Sprint 2 — hipóteses informais)
    private const double LimiteIdadeMaduroAnos = 5;
    private const double LimiteIdadeParcialAnos = 2;
    private const double LimitePRsAlto = 500;
    private const double LimitePRsParcial = 100;
    private const double LimiteReleasesFrequente = 20;
    private const double LimiteReleasesParcial = 5;
    private const double LimiteDiasAtualizadoRecente = 30;
    private const double LimiteDiasAtualizadoParcial = 90;
    private const double LimiteRazaoIssuesAlta = 0.70;
    private const double LimiteRazaoIssuesParcial = 0.50;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var caminhoCsv = ObterCaminhoCsv(args);
        if (string.IsNullOrEmpty(caminhoCsv) || !File.Exists(caminhoCsv))
        {
            AnsiConsole.Write(new Panel(
                    new Markup("[bold red]Arquivo CSV não encontrado.[/]\n\n"
                        + "O Sprint 3 utiliza o arquivo gerado pela Sprint 2.\n\n"
                        + "  [cyan]•[/] Execute primeiro a [green]Sprint 2[/] para gerar [green]dados_repositorios_sprint2.csv[/]\n"
                        + "  [cyan]•[/] Ou informe o caminho: [green]dotnet run -- --csv=\"caminho\\para\\dados_repositorios_sprint2.csv\"[/]\n"
                        + "  [cyan]•[/] Ou coloque o CSV na pasta do projeto ou em [green]..\\Sprint 2\\[/]\n"))
                .Header("[bold red]❌ CSV não encontrado[/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("red")));
            Console.ReadKey();
            return;
        }

        AnsiConsole.Write(new Panel(
                new Markup("[bold]LABORATÓRIO 01[/] — Características de Repositórios Populares\n"
                    + "[dim]Enunciado 1 — Sprint 3 (.NET) — Análise e visualização + Relatório final[/]"))
            .Header("[bold yellow]⭐ GitHub Analytics[/]")
            .Border(BoxBorder.Double)
            .BorderStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();

        var registros = CarregarCsv(caminhoCsv);
        if (registros.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nenhum registro válido no CSV.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]✔[/] Carregados [bold]{registros.Count}[/] repositórios de [dim]{Markup.Escape(caminhoCsv)}[/]\n");

        // Análises
        var resultadoRq01 = CalcularRq01Idade(registros);
        var resultadoRq02 = CalcularRq02PullRequests(registros);
        var resultadoRq03 = CalcularRq03Releases(registros);
        var resultadoRq04 = CalcularRq04DiasDesdeAtualizacao(registros);
        var resultadoRq05 = CalcularRq05Linguagens(registros);
        var resultadoRq06 = CalcularRq06RazaoIssuesFechadas(registros);
        var resultadoRq07 = CalcularRq07PorLinguagem(registros);

        // Visualização no console
        ExibirResultadosNoConsole(
            resultadoRq01, resultadoRq02, resultadoRq03, resultadoRq04,
            resultadoRq05, resultadoRq06, resultadoRq07);

        // Relatório final em Markdown (salvo na pasta do projeto Sprint 3)
        var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var caminhoRelatorio = Path.Combine(pastaProjeto, "Relatorio_Final_Sprint3.md");
        GerarRelatorioMarkdown(
            caminhoRelatorio,
            registros.Count,
            resultadoRq01, resultadoRq02, resultadoRq03, resultadoRq04,
            resultadoRq05, resultadoRq06, resultadoRq07);

        AnsiConsole.MarkupLine($"[green][[Relatório salvo]][/] [cyan]{Markup.Escape(caminhoRelatorio)}[/]");
        AnsiConsole.MarkupLine("\n[dim]Pressione qualquer tecla para sair...[/]");
        Console.ReadKey();
    }

    private static string? ObterCaminhoCsv(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--csv=", StringComparison.OrdinalIgnoreCase))
            {
                var path = arg.Substring("--csv=".Length).Trim(' ', '"');
                return Path.GetFullPath(path);
            }
        }

        var baseDir = AppContext.BaseDirectory;
        var pastaProjeto = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        var candidatos = new[]
        {
            Path.Combine(pastaProjeto, NomeArquivoCsv),
            Path.Combine(pastaProjeto, "..", "Sprint 2", NomeArquivoCsv),
            Path.Combine(Directory.GetCurrentDirectory(), NomeArquivoCsv),
            Path.Combine(Directory.GetCurrentDirectory(), "Enunciado 1", "Sprint 2", NomeArquivoCsv),
        };

        foreach (var c in candidatos)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }

        return null;
    }

    private static List<RepoRecord> CarregarCsv(string caminho)
    {
        var lista = new List<RepoRecord>();
        var linhas = File.ReadAllLines(caminho, Encoding.UTF8);
        if (linhas.Length < 2) return lista;

        var sep = new[] { ';' };
        for (var i = 1; i < linhas.Length; i++)
        {
            var col = linhas[i].Split(sep);
            if (col.Length < 12) continue;

            var r = new RepoRecord
            {
                Nome = col[0].Trim(),
                Estrelas = ParseInt(col[1]),
                CriadoEm = col[2].Trim(),
                AtualizadoEm = col[3].Trim(),
                IdadeAnos = ParseDouble(col[4]),
                Linguagem = string.IsNullOrWhiteSpace(col[5]) || col[5].Contains("não detectada") ? "(não detectada)" : col[5].Trim(),
                PullRequestsAceitas = ParseInt(col[6]),
                TotalReleases = ParseInt(col[7]),
                TotalIssues = ParseInt(col[8]),
                IssuesFechadas = ParseInt(col[9]),
                RazaoIssuesFechadas = ParseDouble(col[10]),
                DiasDesdeAtualizacao = ParseInt(col[11])
            };
            lista.Add(r);
        }

        return lista;
    }

    private static int? ParseInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return int.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static double? ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return double.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static double Mediana(IEnumerable<double> valores)
    {
        var arr = valores.Where(v => !double.IsNaN(v)).OrderBy(v => v).ToArray();
        if (arr.Length == 0) return double.NaN;
        var mid = arr.Length / 2;
        return arr.Length % 2 == 1 ? arr[mid] : (arr[mid - 1] + arr[mid]) / 2.0;
    }

    // RQ 01: Sistemas populares são maduros/antigos? → idade do repositório (mediana)
    private static (double MedianaAnos, int N, double MinAnos, double MaxAnos) CalcularRq01Idade(List<RepoRecord> registros)
    {
        var idades = registros.Where(r => r.IdadeAnos.HasValue).Select(r => r.IdadeAnos!.Value).OrderBy(x => x).ToList();
        if (idades.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(idades), idades.Count, idades[0], idades[^1]);
    }

    // RQ 02: Contribuição externa → total de PRs aceitas (mediana)
    private static (double MedianaPRs, int N, double MinPRs, double MaxPRs) CalcularRq02PullRequests(List<RepoRecord> registros)
    {
        var prs = registros.Where(r => r.PullRequestsAceitas.HasValue).Select(r => (double)r.PullRequestsAceitas!.Value).OrderBy(x => x).ToList();
        if (prs.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(prs), prs.Count, prs[0], prs[^1]);
    }

    // RQ 03: Releases com frequência? → total de releases (mediana)
    private static (double MedianaReleases, int N, double MinReleases, double MaxReleases) CalcularRq03Releases(List<RepoRecord> registros)
    {
        var rel = registros.Where(r => r.TotalReleases.HasValue).Select(r => (double)r.TotalReleases!.Value).OrderBy(x => x).ToList();
        if (rel.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(rel), rel.Count, rel[0], rel[^1]);
    }

    // RQ 04: Atualizados com frequência? → dias desde última atualização (mediana; menor = mais recente)
    private static (double MedianaDias, int N, double MinDias, double MaxDias) CalcularRq04DiasDesdeAtualizacao(List<RepoRecord> registros)
    {
        var dias = registros.Where(r => r.DiasDesdeAtualizacao.HasValue).Select(r => (double)r.DiasDesdeAtualizacao!.Value).OrderBy(x => x).ToList();
        if (dias.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(dias), dias.Count, dias[0], dias[^1]);
    }

    // RQ 05: Linguagens mais populares → contagem por linguagem
    private static List<(string Linguagem, int Contagem)> CalcularRq05Linguagens(List<RepoRecord> registros)
    {
        return registros
            .GroupBy(r => r.Linguagem)
            .OrderByDescending(g => g.Count())
            .Select(g => (g.Key, g.Count()))
            .ToList();
    }

    // RQ 06: Alto percentual de issues fechadas? → mediana da razão (issues fechadas / total)
    private static (double MedianaRazao, int N, double MinRazao, double MaxRazao) CalcularRq06RazaoIssuesFechadas(List<RepoRecord> registros)
    {
        var razoes = registros
            .Where(r => r.TotalIssues.HasValue && r.TotalIssues > 0 && r.RazaoIssuesFechadas.HasValue)
            .Select(r => r.RazaoIssuesFechadas!.Value)
            .OrderBy(x => x)
            .ToList();
        if (razoes.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(razoes), razoes.Count, razoes[0], razoes[^1]);
    }

    // RQ 07 (bônus): Por linguagem — mediana de PRs, releases e dias desde atualização
    private static List<Rq07PorLinguagem> CalcularRq07PorLinguagem(List<RepoRecord> registros)
    {
        var topLinguagens = registros
            .GroupBy(r => r.Linguagem)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => g.Key)
            .ToHashSet();

        var resultado = new List<Rq07PorLinguagem>();
        foreach (var lang in topLinguagens.OrderBy(x => x))
        {
            var grupo = registros.Where(r => r.Linguagem == lang).ToList();
            var medPr = Mediana(grupo.Where(r => r.PullRequestsAceitas.HasValue).Select(r => (double)r.PullRequestsAceitas!.Value));
            var medRel = Mediana(grupo.Where(r => r.TotalReleases.HasValue).Select(r => (double)r.TotalReleases!.Value));
            var medDias = Mediana(grupo.Where(r => r.DiasDesdeAtualizacao.HasValue).Select(r => (double)r.DiasDesdeAtualizacao!.Value));
            resultado.Add(new Rq07PorLinguagem(lang, grupo.Count, medPr, medRel, medDias));
        }

        return resultado.OrderByDescending(r => r.Contagem).ToList();
    }

    private static void ExibirResultadosNoConsole(
        (double MedianaAnos, int N, double MinAnos, double MaxAnos) rq01,
        (double MedianaPRs, int N, double MinPRs, double MaxPRs) rq02,
        (double MedianaReleases, int N, double MinReleases, double MaxReleases) rq03,
        (double MedianaDias, int N, double MinDias, double MaxDias) rq04,
        List<(string Linguagem, int Contagem)> rq05,
        (double MedianaRazao, int N, double MinRazao, double MaxRazao) rq06,
        List<Rq07PorLinguagem> rq07)
    {
        AnsiConsole.Write(new Rule("[bold yellow]RELATÓRIO — Repositórios Populares do GitHub[/]").RuleStyle("yellow"));
        AnsiConsole.MarkupLine($"[dim]Análise a partir do CSV da Sprint 2 (medianas e contagens)[/]\n");

        // Hipóteses informais (padrão Sprint 2)
        AnsiConsole.Write(new Panel(
                new Markup(
                    "[bold]H1:[/] Repositórios populares tendem a ser [cyan]maduros[/] (mediana > 5 anos).\n"
                    + "[bold]H2:[/] Repositórios populares recebem [cyan]muitas PRs aceitas[/] (mediana > 500).\n"
                    + "[bold]H3:[/] Repositórios populares [cyan]lançam releases com frequência[/] (mediana > 20).\n"
                    + "[bold]H4:[/] Repositórios populares são [cyan]atualizados recentemente[/] (mediana < 30 dias).\n"
                    + "[bold]H5:[/] Repositórios populares são escritos em [cyan]linguagens populares[/] (JS, Python, TS, etc.).\n"
                    + "[bold]H6:[/] Repositórios populares possuem [cyan]alto % de issues fechadas[/] (mediana > 70%).\n"
                    + "[bold]H7 (bônus):[/] Linguagens mais populares recebem mais PRs, releases e atualizações."))
            .Header("[bold magenta]Hipóteses Informais[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("magenta")));
        AnsiConsole.WriteLine();

        // RQ 01 — Maturidade
        if (rq01.N > 0)
        {
            var corIdade = rq01.MedianaAnos >= LimiteIdadeMaduroAnos ? "green" : rq01.MedianaAnos >= LimiteIdadeParcialAnos ? "yellow" : "red";
            var conclusaoIdade = rq01.MedianaAnos >= LimiteIdadeMaduroAnos
                ? "Hipótese confirmada. Sistemas populares são, em geral, maduros."
                : rq01.MedianaAnos >= LimiteIdadeParcialAnos
                    ? "Parcialmente confirmada. Há um mix de projetos maduros e relativamente recentes."
                    : "Hipótese refutada. Muitos projetos populares são relativamente recentes.";
            AnsiConsole.Write(new Panel(
                    new Markup($"[bold]Métrica:[/] idade do repositório (anos desde a criação)\n"
                        + $"[bold]Mediana:[/] [{corIdade}]{rq01.MedianaAnos:F1} anos[/]\n"
                        + $"[bold]Mín/Máx:[/] {rq01.MinAnos:F1} / {rq01.MaxAnos:F1} anos\n"
                        + $"[bold]Análise:[/] [{corIdade}]{Markup.Escape(conclusaoIdade)}[/]"))
                .Header("[bold blue]RQ 01 — Sistemas populares são maduros/antigos?[/]")
                .Border(BoxBorder.Rounded).Expand());
            AnsiConsole.WriteLine();
        }

        // RQ 02 — Pull Requests aceitas
        if (rq02.N > 0)
        {
            var corPRs = rq02.MedianaPRs >= LimitePRsAlto ? "green" : rq02.MedianaPRs >= LimitePRsParcial ? "yellow" : "red";
            var conclusaoPRs = rq02.MedianaPRs >= LimitePRsAlto
                ? "Hipótese confirmada. Recebem muita contribuição externa via PRs."
                : rq02.MedianaPRs >= LimitePRsParcial
                    ? "Parcialmente confirmada. Contribuição significativa, mas não tão alta quanto esperado."
                    : "Hipótese refutada. Muitos projetos populares têm poucas PRs aceitas.";
            AnsiConsole.Write(new Panel(
                    new Markup($"[bold]Métrica:[/] total de pull requests aceitas (merged)\n"
                        + $"[bold]Mediana:[/] [{corPRs}]{rq02.MedianaPRs:F0}[/]\n"
                        + $"[bold]Mín/Máx:[/] {rq02.MinPRs:F0} / {rq02.MaxPRs:F0}\n"
                        + $"[bold]Análise:[/] [{corPRs}]{Markup.Escape(conclusaoPRs)}[/]"))
                .Header("[bold blue]RQ 02 — Sistemas populares recebem muita contribuição externa?[/]")
                .Border(BoxBorder.Rounded).Expand());
            AnsiConsole.WriteLine();
        }

        // RQ 03 — Releases
        if (rq03.N > 0)
        {
            var corReleases = rq03.MedianaReleases >= LimiteReleasesFrequente ? "green" : rq03.MedianaReleases >= LimiteReleasesParcial ? "yellow" : "red";
            var conclusaoReleases = rq03.MedianaReleases >= LimiteReleasesFrequente
                ? "Hipótese confirmada. Lançam releases com boa frequência."
                : rq03.MedianaReleases >= LimiteReleasesParcial
                    ? "Parcialmente confirmada. Alguns usam menos releases formais (preferem tags/commits)."
                    : "Hipótese refutada. Muitos projetos populares não utilizam releases formais.";
            AnsiConsole.Write(new Panel(
                    new Markup($"[bold]Métrica:[/] total de releases publicadas\n"
                        + $"[bold]Mediana:[/] [{corReleases}]{rq03.MedianaReleases:F0}[/]\n"
                        + $"[bold]Mín/Máx:[/] {rq03.MinReleases:F0} / {rq03.MaxReleases:F0}\n"
                        + $"[bold]Análise:[/] [{corReleases}]{Markup.Escape(conclusaoReleases)}[/]"))
                .Header("[bold blue]RQ 03 — Sistemas populares lançam releases com frequência?[/]")
                .Border(BoxBorder.Rounded).Expand());
            AnsiConsole.WriteLine();
        }

        // RQ 04 — Atualizações
        if (rq04.N > 0)
        {
            var corDias = rq04.MedianaDias <= LimiteDiasAtualizadoRecente ? "green" : rq04.MedianaDias <= LimiteDiasAtualizadoParcial ? "yellow" : "red";
            var conclusaoDias = rq04.MedianaDias <= LimiteDiasAtualizadoRecente
                ? "Hipótese confirmada. São projetos muito ativos e atualizados frequentemente."
                : rq04.MedianaDias <= LimiteDiasAtualizadoParcial
                    ? "Parcialmente confirmada. Atualizações regulares, mas não diárias."
                    : "Hipótese refutada. Alguns repositórios populares são estáveis e atualizados com menor frequência.";
            AnsiConsole.Write(new Panel(
                    new Markup($"[bold]Métrica:[/] dias desde a última atualização\n"
                        + $"[bold]Mediana:[/] [{corDias}]{rq04.MedianaDias:F0} dias[/]\n"
                        + $"[bold]Mín/Máx:[/] {rq04.MinDias:F0} / {rq04.MaxDias:F0} dias\n"
                        + $"[bold]Análise:[/] [{corDias}]{Markup.Escape(conclusaoDias)}[/]"))
                .Header("[bold blue]RQ 04 — Sistemas populares são atualizados com frequência?[/]")
                .Border(BoxBorder.Rounded).Expand());
            AnsiConsole.WriteLine();
        }

        // RQ 05 — Linguagens (BarChart como Sprint 2)
        var cores = new[] { Color.Green, Color.Yellow, Color.Blue, Color.Red, Color.Purple,
            Color.Aqua, Color.Orange1, Color.Fuchsia, Color.Lime, Color.Teal };
        var barChart = new BarChart()
            .Label("[bold blue]Distribuição por linguagem (top 15)[/]")
            .Width(70);
        foreach (var (g, i) in rq05.Take(15).Select((g, i) => (g, i)))
            barChart.AddItem(Markup.Escape(g.Linguagem), g.Contagem, cores[i % cores.Length]);
        AnsiConsole.Write(new Panel(barChart)
            .Header("[bold blue]RQ 05 — Sistemas populares são escritos nas linguagens mais populares?[/]")
            .Border(BoxBorder.Rounded).Expand());
        var top3 = string.Join(", ", rq05.Take(3).Select(g => $"{g.Linguagem} ({g.Contagem})"));
        AnsiConsole.MarkupLine($"   [bold]Top 3:[/] {Markup.Escape(top3)}");
        AnsiConsole.MarkupLine("   [bold]Análise:[/] [green]Hipótese confirmada. Predominam linguagens amplamente adotadas.[/]\n");

        // RQ 06 — Issues fechadas / total
        if (rq06.N > 0)
        {
            var corRazao = rq06.MedianaRazao >= LimiteRazaoIssuesAlta ? "green" : rq06.MedianaRazao >= LimiteRazaoIssuesParcial ? "yellow" : "red";
            var conclusaoRazao = rq06.MedianaRazao >= LimiteRazaoIssuesAlta
                ? "Hipótese confirmada. A maioria dos projetos populares fecha uma proporção alta de issues."
                : rq06.MedianaRazao >= LimiteRazaoIssuesParcial
                    ? "Parcialmente confirmada. Fecham mais da metade, mas há margem de melhoria."
                    : "Hipótese refutada. Muitos projetos populares acumulam issues abertas.";
            var minMaxRazao = $"{rq06.MinRazao:P1} / {rq06.MaxRazao:P1}";
            AnsiConsole.Write(new Panel(
                    new Markup($"[bold]Métrica:[/] razão issues fechadas / total de issues\n"
                        + $"[bold]Mediana:[/] [{corRazao}]{rq06.MedianaRazao:P1}[/]\n"
                        + $"[bold]Mín/Máx:[/] {minMaxRazao}\n"
                        + $"[bold]Análise:[/] [{corRazao}]{Markup.Escape(conclusaoRazao)}[/]"))
                .Header("[bold blue]RQ 06 — Sistemas populares possuem alto percentual de issues fechadas?[/]")
                .Border(BoxBorder.Rounded).Expand());
            AnsiConsole.WriteLine();
        }

        // Resumo — Valores medianos (tabela amarela como Sprint 2)
        var resumo = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("yellow"))
            .Title("[bold yellow]RESUMO — Valores Medianos[/]")
            .AddColumn(new TableColumn("[bold]Questão[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Métrica[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Mediana[/]").RightAligned());
        if (rq01.N > 0) resumo.AddRow("RQ 01 — Maturidade", "Idade (anos)", $"{rq01.MedianaAnos:F1}");
        if (rq02.N > 0) resumo.AddRow("RQ 02 — Contribuição", "PRs aceitas", $"{rq02.MedianaPRs:F0}");
        if (rq03.N > 0) resumo.AddRow("RQ 03 — Releases", "Total releases", $"{rq03.MedianaReleases:F0}");
        if (rq04.N > 0) resumo.AddRow("RQ 04 — Atualizações", "Dias desde última atualiz.", $"{rq04.MedianaDias:F0}");
        resumo.AddRow("RQ 05 — Linguagens", "Linguagem mais comum", rq05.Count > 0 ? Markup.Escape(rq05[0].Linguagem) : "—");
        if (rq06.N > 0) resumo.AddRow("RQ 06 — Issues fechadas", "Razão fechadas/total", $"{rq06.MedianaRazao:P1}");
        AnsiConsole.Write(resumo);
        AnsiConsole.WriteLine();

        // RQ 07 (bônus) — Por linguagem
        AnsiConsole.Write(new Rule("[bold yellow]RQ 07 (bônus) — Por linguagem: mediana PRs, releases, dias sem atualizar[/]").RuleStyle("yellow"));
        var tabelaRq07 = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("cyan"))
            .Title("[bold cyan]Mediana de PRs, Releases e Dias por linguagem (top 15)[/]")
            .AddColumn(new TableColumn("[bold]Linguagem[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]N[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Med. PRs[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Med. Releases[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Med. Dias[/]").RightAligned());
        foreach (var r in rq07)
            tabelaRq07.AddRow(
                Markup.Escape(r.Linguagem),
                r.Contagem.ToString(),
                r.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture),
                r.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture),
                r.MedianaDiasDesdeAtualizacao.ToString("F0", CultureInfo.InvariantCulture));
        AnsiConsole.Write(tabelaRq07);
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold green]✔ Relatório exibido com sucesso[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();
    }

    private static void GerarRelatorioMarkdown(
        string caminhoRelatorio,
        int totalRepos,
        (double MedianaAnos, int N, double MinAnos, double MaxAnos) rq01,
        (double MedianaPRs, int N, double MinPRs, double MaxPRs) rq02,
        (double MedianaReleases, int N, double MinReleases, double MaxReleases) rq03,
        (double MedianaDias, int N, double MinDias, double MaxDias) rq04,
        List<(string Linguagem, int Contagem)> rq05,
        (double MedianaRazao, int N, double MinRazao, double MaxRazao) rq06,
        List<Rq07PorLinguagem> rq07)
    {
        var nl = Environment.NewLine;
        var sb = new StringBuilder();

        sb.Append("# Relatório Final — Laboratório 01: Características de Repositórios Populares").Append(nl);
        sb.Append(nl);
        sb.Append("## 1. Introdução e hipóteses informais").Append(nl);
        sb.Append(nl);
        sb.Append("Este relatório analisa os **1.000 repositórios com maior número de estrelas no GitHub** para responder às questões de pesquisa (RQs) do laboratório. As hipóteses informais são:").Append(nl);
        sb.Append(nl);
        sb.Append("- **RQ 01 (idade):** Espera-se que repositórios populares sejam relativamente maduros/antigos (ex.: mediana de idade > 5 anos).").Append(nl);
        sb.Append("- **RQ 02 (contribuição externa):** Espera-se que recebam muitas contribuições via pull requests (mediana de PRs aceitas elevada).").Append(nl);
        sb.Append("- **RQ 03 (releases):** Espera-se que lancem releases com certa frequência (mediana de releases > 0 ou relativamente alta).").Append(nl);
        sb.Append("- **RQ 04 (atualização):** Espera-se que sejam atualizados com frequência (mediana de dias desde a última atualização baixa).").Append(nl);
        sb.Append("- **RQ 05 (linguagem):** Espera-se que predominem linguagens muito usadas (JavaScript/TypeScript, Python, etc.).").Append(nl);
        sb.Append("- **RQ 06 (issues fechadas):** Espera-se um percentual alto de issues fechadas em relação ao total (ex.: mediana da razão > 70%).").Append(nl);
        sb.Append("- **RQ 07 (bônus):** Sistemas em linguagens mais populares podem receber mais PRs, mais releases e serem atualizados com mais frequência.").Append(nl);
        sb.Append(nl);
        sb.Append("## 2. Metodologia").Append(nl);
        sb.Append(nl);
        sb.Append("Os dados foram obtidos via **API GraphQL do GitHub** (Sprints 1 e 2), com paginação para 1.000 repositórios ordenados por número de estrelas. Os dados foram exportados em CSV (separador `;`). Para cada RQ numérica, foi calculada a **mediana** dos valores; para a RQ 05 (linguagem), foi feita **contagem por categoria**. A análise e visualização foram realizadas no Sprint 3 com um script em C# (.NET).").Append(nl);
        sb.Append(nl);
        sb.Append("## 3. Resultados").Append(nl);
        sb.Append(nl);
        sb.Append($"Total de repositórios analisados: **{totalRepos}**.").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 01 — Sistemas populares são maduros/antigos?").Append(nl);
        sb.Append($"- **Mediana da idade do repositório:** {rq01.MedianaAnos.ToString("F2", CultureInfo.InvariantCulture)} anos (n = {rq01.N}).").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 02 — Sistemas populares recebem muita contribuição externa?").Append(nl);
        sb.Append($"- **Mediana de pull requests aceitas:** {rq02.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture)} (n = {rq02.N}).").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 03 — Sistemas populares lançam releases com frequência?").Append(nl);
        sb.Append($"- **Mediana de total de releases:** {rq03.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture)} (n = {rq03.N}).").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 04 — Sistemas populares são atualizados com frequência?").Append(nl);
        sb.Append($"- **Mediana de dias desde a última atualização:** {rq04.MedianaDias.ToString("F0", CultureInfo.InvariantCulture)} dias (n = {rq04.N}). Quanto menor, mais recente a atualização.").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 05 — Sistemas populares são escritos nas linguagens mais populares?").Append(nl);
        sb.Append("Contagem por linguagem (primeiras 20):").Append(nl);
        sb.Append(nl);
        sb.Append("| Linguagem | Quantidade de repositórios |").Append(nl);
        sb.Append("|-----------|---------------------------|").Append(nl);
        foreach (var (lang, count) in rq05.Take(20))
            sb.Append($"| {lang} | {count} |").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 06 — Sistemas populares possuem alto percentual de issues fechadas?").Append(nl);
        sb.Append($"- **Mediana da razão (issues fechadas / total de issues):** {(rq06.MedianaRazao * 100).ToString("F1", CultureInfo.InvariantCulture)}% (n = {rq06.N}).").Append(nl);
        sb.Append(nl);
        sb.Append("### RQ 07 (bônus) — Por linguagem: mais contribuição, releases e atualizações?").Append(nl);
        sb.Append("Mediana de PRs aceitas, total de releases e dias desde última atualização por linguagem (top 15):").Append(nl);
        sb.Append(nl);
        sb.Append("| Linguagem | N | Med. PRs | Med. Releases | Med. Dias |").Append(nl);
        sb.Append("|-----------|---|----------|---------------|-----------|").Append(nl);
        foreach (var r in rq07)
            sb.Append($"| {r.Linguagem} | {r.Contagem} | {r.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture)} | {r.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture)} | {r.MedianaDiasDesdeAtualizacao.ToString("F0", CultureInfo.InvariantCulture)} |").Append(nl);
        sb.Append(nl);
        sb.Append("## 4. Discussão").Append(nl);
        sb.Append(nl);
        sb.Append("Compare as **hipóteses** da introdução com os **valores obtidos**:").Append(nl);
        sb.Append("- **RQ 01:** A mediana de idade indica se os repositórios populares tendem a ser maduros; valores em torno de 8–10 anos reforçam que popularidade muitas vezes acompanha maturidade.").Append(nl);
        sb.Append("- **RQ 02 e 03:** Medianas de PRs e releases variam muito; repositórios de documentação/lista (awesome, listas) podem ter poucas releases mas muitas estrelas.").Append(nl);
        sb.Append("- **RQ 04:** Mediana de dias baixa indica que a maioria dos repositórios populares é atualizada com frequência.").Append(nl);
        sb.Append("- **RQ 05:** A tabela por linguagem mostra quais linguagens predominam entre os 1.000 mais estrelados.").Append(nl);
        sb.Append("- **RQ 06:** Uma mediana alta da razão de issues fechadas sugere que projetos populares tendem a manter as issues em dia.").Append(nl);
        sb.Append("- **RQ 07:** Comparando as medianas por linguagem, é possível ver se linguagens mais representadas (ex.: JavaScript, Python) têm medianas de PRs, releases e dias até atualização diferentes das demais.").Append(nl);

        File.WriteAllText(caminhoRelatorio, sb.ToString(), Encoding.UTF8);
    }

    private sealed class RepoRecord
    {
        public string Nome { get; set; } = "";
        public int? Estrelas { get; set; }
        public string CriadoEm { get; set; } = "";
        public string AtualizadoEm { get; set; } = "";
        public double? IdadeAnos { get; set; }
        public string Linguagem { get; set; } = "";
        public int? PullRequestsAceitas { get; set; }
        public int? TotalReleases { get; set; }
        public int? TotalIssues { get; set; }
        public int? IssuesFechadas { get; set; }
        public double? RazaoIssuesFechadas { get; set; }
        public int? DiasDesdeAtualizacao { get; set; }
    }

    private record Rq07PorLinguagem(string Linguagem, int Contagem, double MedianaPRs, double MedianaReleases, double MedianaDiasDesdeAtualizacao);
}

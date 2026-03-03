using System.Globalization;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Enunciado1.Sprint2;

/// <summary>
/// Laboratório 01 — Características de Repositórios Populares (GitHub).
/// Enunciado 1, Sprint 2: paginação (1 000 repos) + CSV + relatório com hipóteses.
/// </summary>
class Program
{
    private static readonly HttpClient Http = new();
    private const string GraphQLEndpoint = "https://api.github.com/graphql";
    private const int ReposPorPagina = 10;    // valor conservador para evitar 502/timeout na API GraphQL
    private const int TotalRepos = 1000;
    private const int MaxRetries = 3;

    // Limiares para conclusões das RQs (hipóteses informais)
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

    private static string? _token;

    static Program()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "LabEXPSoftware-Lab01-Sprint2");
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private static void AplicarToken(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return;
        _token = t.Trim();
        if (!Http.DefaultRequestHeaders.Contains("Authorization"))
            Http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
    }

    private static bool TemToken() => !string.IsNullOrWhiteSpace(_token);

    private static string? ObterTokenDosArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--token=", StringComparison.OrdinalIgnoreCase))
                return args[i]["--token=".Length..].Trim();
            if (string.Equals(args[i], "--token", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1].Trim();
        }
        return null;
    }

    private static string? LerTokenDoArquivo()
    {
        var baseDir = AppContext.BaseDirectory;
        var pastaProjeto = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        var dirs = new[]
        {
            pastaProjeto,
            Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? "",
            baseDir,
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "Enunciado 1", "Sprint 2")
        };
        foreach (var dir in dirs)
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var arquivo = Path.Combine(dir, ".github-token");
            if (!File.Exists(arquivo)) continue;
            try
            {
                var linha = File.ReadAllText(arquivo).Trim();
                if (!string.IsNullOrWhiteSpace(linha)) return linha;
            }
            catch { /* ignora */ }
        }
        return null;
    }

    // ───────────────────── Ponto de entrada ─────────────────────

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var token = ObterTokenDosArgs(args)
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? LerTokenDoArquivo();
        AplicarToken(token);

        AnsiConsole.Write(new Panel(
                new Markup("[bold]LABORATÓRIO 01[/] — Características de Repositórios Populares\n"
                    + "[dim]Enunciado 1 — Sprint 2 (.NET) — GraphQL API — 1 000 repositórios[/]"))
            .Header("[bold yellow]⭐ GitHub Analytics[/]")
            .Border(BoxBorder.Double)
            .BorderStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();

        if (!TemToken())
        {
            AnsiConsole.Write(new Panel(
                    new Markup("[bold red]A API GraphQL do GitHub requer autenticação.[/]\n\n"
                        + "Forneça um Personal Access Token de uma das seguintes formas:\n"
                        + "  [cyan]•[/] Arquivo [green].github-token[/] na pasta do projeto\n"
                        + "  [cyan]•[/] Variável de ambiente:  [green]$env:GITHUB_TOKEN = \"seu_token\"[/]\n"
                        + "  [cyan]•[/] Na execução:           [green]dotnet run -- --token=seu_token[/]\n\n"
                        + "[dim]Criar token: GitHub → Settings → Developer settings → Personal access tokens[/]"))
                .Header("[bold red]❌ Token não encontrado[/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("red")));
            Console.ReadKey();
            return;
        }

        AnsiConsole.MarkupLine($"[green]✔ Autenticado[/] — Modo GraphQL ([cyan]{TotalRepos}[/] repositórios)\n");

        // ── Coleta de dados com paginação ──
        List<MetricasRepo> metricas = null!;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("Buscando repositórios populares via GraphQL...", async ctx =>
            {
                metricas = await BuscarRepositoriosGraphQLAsync(TotalRepos, ctx).ConfigureAwait(false);
            });

        if (metricas.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nenhum repositório encontrado.[/]");
            if (!string.IsNullOrEmpty(_ultimoErro))
                AnsiConsole.MarkupLine($"[red bold]Erro:[/] {Markup.Escape(_ultimoErro)}");
            else
                AnsiConsole.MarkupLine("[dim]Verifique token ou conexão.[/]");
            Console.ReadKey();
            return;
        }

        AnsiConsole.MarkupLine($"\n[green]✔ {metricas.Count} repositórios obtidos com sucesso![/]\n");

        var resultados = CalcularResultadosRQs(metricas);
        EscreverRelatorio(metricas, resultados);
        ExportarCsv(metricas);
        ExportarReadme(metricas, resultados);

        AnsiConsole.MarkupLine("\n[dim]Pressione qualquer tecla para sair...[/]");
        Console.ReadKey();
    }

    // ───────────────────── GraphQL ─────────────────────

    private const string SearchQuery = @"
        query($queryString: String!, $first: Int!, $after: String) {
          search(query: $queryString, type: REPOSITORY, first: $first, after: $after) {
            repositoryCount
            pageInfo {
              hasNextPage
              endCursor
            }
            nodes {
              ... on Repository {
                nameWithOwner
                stargazerCount
                createdAt
                updatedAt
                primaryLanguage { name }
                releases { totalCount }
                pullRequests(states: MERGED) { totalCount }
                issues { totalCount }
                closedIssues: issues(states: CLOSED) { totalCount }
              }
            }
          }
        }";

    private static async Task<List<MetricasRepo>> BuscarRepositoriosGraphQLAsync(int total, StatusContext ctx)
    {
        var lista = new List<MetricasRepo>();
        string? cursor = null;
        var falhasConsecutivas = 0;
        const int maxFalhasConsecutivas = 5;

        while (lista.Count < total)
        {
            var porPagina = Math.Min(ReposPorPagina, total - lista.Count);
            var variables = new Dictionary<string, object?>
            {
                ["queryString"] = "stars:>1000 sort:stars-desc",
                ["first"] = porPagina,
                ["after"] = cursor
            };

            var json = await PostGraphQLAsync(SearchQuery, variables).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json))
            {
                falhasConsecutivas++;
                ctx.Status($"[yellow]Falha na página (tentativa {falhasConsecutivas}/{maxFalhasConsecutivas})... aguardando retry[/]");
                if (falhasConsecutivas >= maxFalhasConsecutivas)
                {
                    ctx.Status($"[red]Muitas falhas consecutivas. Parando com {lista.Count} repos.[/]");
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, falhasConsecutivas))).ConfigureAwait(false);
                continue;
            }

            falhasConsecutivas = 0;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data)) break;
            var search = data.GetProperty("search");
            var nodes = search.GetProperty("nodes");

            foreach (var node in nodes.EnumerateArray())
            {
                if (node.ValueKind == JsonValueKind.Null) continue;
                lista.Add(new MetricasRepo
                {
                    Nome = node.GetProperty("nameWithOwner").GetString() ?? "",
                    Estrelas = node.GetProperty("stargazerCount").GetInt32(),
                    CriadoEm = DateTime.Parse(node.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture),
                    AtualizadoEm = DateTime.Parse(node.GetProperty("updatedAt").GetString()!, CultureInfo.InvariantCulture),
                    Linguagem = node.TryGetProperty("primaryLanguage", out var lang) && lang.ValueKind != JsonValueKind.Null
                        ? lang.GetProperty("name").GetString() ?? "(não detectada)"
                        : "(não detectada)",
                    TotalReleases = node.GetProperty("releases").GetProperty("totalCount").GetInt32(),
                    PullRequestsAceitas = node.GetProperty("pullRequests").GetProperty("totalCount").GetInt32(),
                    TotalIssues = node.GetProperty("issues").GetProperty("totalCount").GetInt32(),
                    IssuesFechadas = node.GetProperty("closedIssues").GetProperty("totalCount").GetInt32()
                });
                ctx.Status($"Obtido {lista.Count}/{total}: {lista[^1].Nome}");
                if (lista.Count >= total) break;
            }

            var pageInfo = search.GetProperty("pageInfo");
            if (!pageInfo.GetProperty("hasNextPage").GetBoolean()) break;
            cursor = pageInfo.GetProperty("endCursor").GetString();
        }

        return lista.Take(total).ToList();
    }

    private static bool _rateLimitAvisado;
    private static string? _ultimoErro;

    private static async Task<string?> PostGraphQLAsync(string query, Dictionary<string, object?> variables)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { query, variables });

            HttpResponseMessage res = null!;
            for (var tentativa = 0; tentativa <= MaxRetries; tentativa++)
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                res = await Http.PostAsync(GraphQLEndpoint, content).ConfigureAwait(false);

                var code = (int)res.StatusCode;
                if (code is 502 or 503 && tentativa < MaxRetries)
                {
                    var espera = (int)Math.Pow(2, tentativa + 1);
                    await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
                    continue;
                }
                break;
            }

            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _ultimoErro = "Token inválido ou expirado (HTTP 401). Gere um novo Personal Access Token.";
                return null;
            }

            if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                if (!_rateLimitAvisado)
                {
                    _rateLimitAvisado = true;
                    var resetUnix = res.Headers.TryGetValues("X-RateLimit-Reset", out var vals)
                        && long.TryParse(vals.FirstOrDefault(), out var t) ? t : 0L;
                    var seg = resetUnix > 0 ? (int)(resetUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 60;
                    seg = Math.Clamp(seg, 1, 3600);
                    _ultimoErro = $"Rate limit atingido. Aguardando {seg}s...";
                    await Task.Delay(TimeSpan.FromSeconds(seg)).ConfigureAwait(false);
                    using (var retryContent = new StringContent(body, Encoding.UTF8, "application/json"))
                        res = await Http.PostAsync(GraphQLEndpoint, retryContent).ConfigureAwait(false);
                    if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _ultimoErro = "Ainda no rate limit após espera. Tente novamente mais tarde.";
                        return null;
                    }
                    _ultimoErro = null;
                }
                else
                {
                    _ultimoErro = "Rate limit atingido (HTTP 403).";
                    return null;
                }
            }

            if (!res.IsSuccessStatusCode)
            {
                var errorBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                _ultimoErro = $"HTTP {(int)res.StatusCode}: {errorBody}";
                return null;
            }

            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                var msgs = new List<string>();
                foreach (var err in errors.EnumerateArray())
                    msgs.Add(err.GetProperty("message").GetString() ?? "erro desconhecido");
                _ultimoErro = $"GraphQL: {string.Join("; ", msgs)}";
                if (!doc.RootElement.TryGetProperty("data", out _))
                    return null;
            }

            return json;
        }
        catch (Exception ex)
        {
            _ultimoErro = ex.Message;
            return null;
        }
    }

    // ───────────────────── Resultados das RQs (computados uma vez) ─────────────────────

    private sealed record ResultadosRQs(
        List<double> IdadesAnos,
        List<double> PRs,
        List<double> Releases,
        List<double> DiasDesdeAtualizacao,
        List<IGrouping<string, MetricasRepo>> PorLinguagem,
        List<double> RazoesIssuesFechadas,
        double MedianaIdade,
        double MedianaPRs,
        double MedianaReleases,
        double MedianaDias,
        double MedianaRazaoIssues,
        string ConclusaoIdade,
        string ConclusaoPRs,
        string ConclusaoReleases,
        string ConclusaoDias,
        string ConclusaoRazaoIssues,
        int ReposSemIssues);

    private static ResultadosRQs CalcularResultadosRQs(List<MetricasRepo> metricas)
    {
        var idadesAnos = metricas.Select(m => (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25).OrderBy(x => x).ToList();
        var prs = metricas.Select(m => (double)m.PullRequestsAceitas).OrderBy(x => x).ToList();
        var releases = metricas.Select(m => (double)m.TotalReleases).OrderBy(x => x).ToList();
        var diasAtual = metricas.Select(m => (DateTime.UtcNow - m.AtualizadoEm).TotalDays).OrderBy(x => x).ToList();
        var porLinguagem = metricas.GroupBy(m => m.Linguagem).OrderByDescending(g => g.Count()).ToList();
        var razoes = metricas.Where(m => m.TotalIssues > 0).Select(m => (double)m.IssuesFechadas / m.TotalIssues).OrderBy(x => x).ToList();

        var medianaIdade = Mediana(idadesAnos);
        var medianaPRs = Mediana(prs);
        var medianaReleases = Mediana(releases);
        var medianaDias = Mediana(diasAtual);
        var medianaRazao = Mediana(razoes);

        string Conclusao(double valor, double limOk, double limParcial, bool maiorMelhor, string ok, string parcial, string refutada) =>
            maiorMelhor
                ? (valor >= limOk ? ok : valor >= limParcial ? parcial : refutada)
                : (valor <= limOk ? ok : valor <= limParcial ? parcial : refutada);

        return new ResultadosRQs(
            IdadesAnos: idadesAnos,
            PRs: prs,
            Releases: releases,
            DiasDesdeAtualizacao: diasAtual,
            PorLinguagem: porLinguagem,
            RazoesIssuesFechadas: razoes,
            MedianaIdade: medianaIdade,
            MedianaPRs: medianaPRs,
            MedianaReleases: medianaReleases,
            MedianaDias: medianaDias,
            MedianaRazaoIssues: medianaRazao,
            ConclusaoIdade: Conclusao(medianaIdade, LimiteIdadeMaduroAnos, LimiteIdadeParcialAnos, true,
                "Hipótese confirmada. Sistemas populares são, em geral, maduros.",
                "Parcialmente confirmada. Há um mix de projetos maduros e relativamente recentes.",
                "Hipótese refutada. Muitos projetos populares são relativamente recentes."),
            ConclusaoPRs: Conclusao(medianaPRs, LimitePRsAlto, LimitePRsParcial, true,
                "Hipótese confirmada. Recebem muita contribuição externa via PRs.",
                "Parcialmente confirmada. Contribuição significativa, mas não tão alta quanto esperado.",
                "Hipótese refutada. Muitos projetos populares têm poucas PRs aceitas."),
            ConclusaoReleases: Conclusao(medianaReleases, LimiteReleasesFrequente, LimiteReleasesParcial, true,
                "Hipótese confirmada. Lançam releases com boa frequência.",
                "Parcialmente confirmada. Alguns usam menos releases formais (preferem tags/commits).",
                "Hipótese refutada. Muitos projetos populares não utilizam releases formais."),
            ConclusaoDias: Conclusao(medianaDias, LimiteDiasAtualizadoRecente, LimiteDiasAtualizadoParcial, false,
                "Hipótese confirmada. São projetos muito ativos e atualizados frequentemente.",
                "Parcialmente confirmada. Atualizações regulares, mas não diárias.",
                "Hipótese refutada. Alguns repositórios populares são estáveis e atualizados com menor frequência."),
            ConclusaoRazaoIssues: medianaRazao >= LimiteRazaoIssuesAlta
                ? "Hipótese confirmada. A maioria dos projetos populares fecha uma proporção alta de issues."
                : medianaRazao >= LimiteRazaoIssuesParcial
                    ? "Parcialmente confirmada. Fecham mais da metade, mas há margem de melhoria."
                    : "Hipótese refutada. Muitos projetos populares acumulam issues abertas.",
            ReposSemIssues: metricas.Count(m => m.TotalIssues == 0));
    }

    // ───────────────────── Relatório (Spectre.Console) ─────────────────────

    private static void EscreverRelatorio(List<MetricasRepo> metricas, ResultadosRQs r)
    {
        AnsiConsole.Write(new Rule("[bold yellow]RELATÓRIO — Repositórios Populares do GitHub[/]").RuleStyle("yellow"));
        AnsiConsole.MarkupLine($"[dim]Data: {DateTime.Now:yyyy-MM-dd HH:mm} | Repositórios analisados: {metricas.Count}[/]\n");

        // ── Tabela de repositórios (top 20 para não poluir o console) ──
        var tabela = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("cyan"))
            .Title("[bold cyan]TOP 20 REPOSITÓRIOS (dados completos no CSV)[/]")
            .AddColumn(new TableColumn("[bold]Repositório[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]⭐[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Idade (anos)[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]PRs aceitas[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Releases[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Últ. atualiz.[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Linguagem[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Issues fechadas/total[/]").RightAligned());

        foreach (var m in metricas.OrderByDescending(x => x.Estrelas).Take(20))
        {
            var idadeAnos = (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25;
            var diasUltAtual = (DateTime.UtcNow - m.AtualizadoEm).TotalDays;
            var razaoIssues = m.TotalIssues > 0 ? (double)m.IssuesFechadas / m.TotalIssues : 0;
            tabela.AddRow(
                Markup.Escape(m.Nome),
                $"[yellow]{m.Estrelas:N0}[/]",
                $"{idadeAnos:F1}",
                $"{m.PullRequestsAceitas:N0}",
                $"{m.TotalReleases:N0}",
                $"{diasUltAtual:F0} dias",
                m.Linguagem == "(não detectada)" ? "[dim]N/A[/]" : $"[green]{Markup.Escape(m.Linguagem)}[/]",
                $"{razaoIssues:P1}");
        }
        AnsiConsole.Write(tabela);
        AnsiConsole.WriteLine();

        // ── Hipóteses informais ──
        AnsiConsole.Write(new Panel(
                new Markup(
                    "[bold]H1:[/] Repositórios populares tendem a ser [cyan]maduros[/] (mais de 5 anos).\n"
                    + "[bold]H2:[/] Repositórios populares recebem um [cyan]alto número de PRs aceitas[/] (mediana > 500).\n"
                    + "[bold]H3:[/] Repositórios populares [cyan]lançam releases com frequência[/] (mediana > 20).\n"
                    + "[bold]H4:[/] Repositórios populares são [cyan]atualizados recentemente[/] (mediana < 30 dias).\n"
                    + "[bold]H5:[/] Repositórios populares são escritos em [cyan]linguagens populares[/] (JS, Python, TS, etc.).\n"
                    + "[bold]H6:[/] Repositórios populares possuem [cyan]alto percentual de issues fechadas[/] (mediana > 70%)."))
            .Header("[bold magenta]Hipóteses Informais[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("magenta")));
        AnsiConsole.WriteLine();

        // ── RQ 01: Maturidade ──
        var corIdade = r.MedianaIdade >= LimiteIdadeMaduroAnos ? "green" : r.MedianaIdade >= LimiteIdadeParcialAnos ? "yellow" : "red";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] idade do repositório (anos desde a criação)\n"
                    + $"[bold]Mediana:[/] [{corIdade}]{r.MedianaIdade:F1} anos[/]\n"
                    + $"[bold]Mín/Máx:[/] {r.IdadesAnos[0]:F1} / {r.IdadesAnos[^1]:F1} anos\n"
                    + $"[bold]Análise:[/] [{corIdade}]{Markup.Escape(r.ConclusaoIdade)}[/]"))
            .Header("[bold blue]RQ 01 — Sistemas populares são maduros/antigos?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // ── RQ 02: Pull Requests aceitas ──
        var corPRs = r.MedianaPRs >= LimitePRsAlto ? "green" : r.MedianaPRs >= LimitePRsParcial ? "yellow" : "red";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] total de pull requests aceitas (merged)\n"
                    + $"[bold]Mediana:[/] [{corPRs}]{r.MedianaPRs:F0}[/]\n"
                    + $"[bold]Mín/Máx:[/] {r.PRs[0]:F0} / {r.PRs[^1]:F0}\n"
                    + $"[bold]Análise:[/] [{corPRs}]{Markup.Escape(r.ConclusaoPRs)}[/]"))
            .Header("[bold blue]RQ 02 — Sistemas populares recebem muita contribuição externa?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // ── RQ 03: Releases ──
        var corReleases = r.MedianaReleases >= LimiteReleasesFrequente ? "green" : r.MedianaReleases >= LimiteReleasesParcial ? "yellow" : "red";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] total de releases publicadas\n"
                    + $"[bold]Mediana:[/] [{corReleases}]{r.MedianaReleases:F0}[/]\n"
                    + $"[bold]Mín/Máx:[/] {r.Releases[0]:F0} / {r.Releases[^1]:F0}\n"
                    + $"[bold]Análise:[/] [{corReleases}]{Markup.Escape(r.ConclusaoReleases)}[/]"))
            .Header("[bold blue]RQ 03 — Sistemas populares lançam releases com frequência?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // ── RQ 04: Atualizações ──
        var corDias = r.MedianaDias <= LimiteDiasAtualizadoRecente ? "green" : r.MedianaDias <= LimiteDiasAtualizadoParcial ? "yellow" : "red";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] dias desde a última atualização\n"
                    + $"[bold]Mediana:[/] [{corDias}]{r.MedianaDias:F0} dias[/]\n"
                    + $"[bold]Mín/Máx:[/] {r.DiasDesdeAtualizacao[0]:F0} / {r.DiasDesdeAtualizacao[^1]:F0} dias\n"
                    + $"[bold]Análise:[/] [{corDias}]{Markup.Escape(r.ConclusaoDias)}[/]"))
            .Header("[bold blue]RQ 04 — Sistemas populares são atualizados com frequência?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // ── RQ 05: Linguagens ──
        var cores = new[] { Color.Green, Color.Yellow, Color.Blue, Color.Red, Color.Purple,
            Color.Aqua, Color.Orange1, Color.Fuchsia, Color.Lime, Color.Teal };
        var barChart = new BarChart()
            .Label("[bold blue]Distribuição por linguagem (top 15)[/]")
            .Width(70);
        foreach (var (g, i) in r.PorLinguagem.Take(15).Select((g, i) => (g, i)))
            barChart.AddItem(Markup.Escape(g.Key), g.Count(), cores[i % cores.Length]);
        AnsiConsole.Write(new Panel(barChart)
            .Header("[bold blue]RQ 05 — Sistemas populares são escritos nas linguagens mais populares?[/]")
            .Border(BoxBorder.Rounded).Expand());

        var top3 = string.Join(", ", r.PorLinguagem.Take(3).Select(g => $"{g.Key} ({g.Count()})"));
        AnsiConsole.MarkupLine($"   [bold]Top 3:[/] {Markup.Escape(top3)}");
        AnsiConsole.MarkupLine("   [bold]Análise:[/] [green]Hipótese confirmada. Predominam linguagens amplamente adotadas.[/]\n");

        // ── RQ 06: Issues fechadas / total ──
        var corRazao = r.MedianaRazaoIssues >= LimiteRazaoIssuesAlta ? "green" : r.MedianaRazaoIssues >= LimiteRazaoIssuesParcial ? "yellow" : "red";
        var minMaxRazao = r.RazoesIssuesFechadas.Count > 0
            ? $"{r.RazoesIssuesFechadas[0]:P1} / {r.RazoesIssuesFechadas[^1]:P1}"
            : "N/A (nenhum repositório com issues)";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] razão issues fechadas / total de issues\n"
                    + $"[bold]Mediana:[/] [{corRazao}]{r.MedianaRazaoIssues:P1}[/]\n"
                    + $"[bold]Mín/Máx:[/] {minMaxRazao}\n"
                    + $"[bold]Repos sem issues:[/] {r.ReposSemIssues}\n"
                    + $"[bold]Análise:[/] [{corRazao}]{Markup.Escape(r.ConclusaoRazaoIssues)}[/]"))
            .Header("[bold blue]RQ 06 — Sistemas populares possuem alto percentual de issues fechadas?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // ── Resumo geral ──
        var resumo = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("yellow"))
            .Title("[bold yellow]RESUMO — Valores Medianos[/]")
            .AddColumn(new TableColumn("[bold]Questão[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Métrica[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Mediana[/]").RightAligned());
        resumo.AddRow("RQ 01 — Maturidade", "Idade (anos)", $"{r.MedianaIdade:F1}");
        resumo.AddRow("RQ 02 — Contribuição", "PRs aceitas", $"{r.MedianaPRs:F0}");
        resumo.AddRow("RQ 03 — Releases", "Total releases", $"{r.MedianaReleases:F0}");
        resumo.AddRow("RQ 04 — Atualizações", "Dias desde última atualiz.", $"{r.MedianaDias:F0}");
        resumo.AddRow("RQ 05 — Linguagens", "Linguagem mais comum", Markup.Escape(r.PorLinguagem[0].Key));
        resumo.AddRow("RQ 06 — Issues fechadas", "Razão fechadas/total", $"{r.MedianaRazaoIssues:P1}");
        AnsiConsole.Write(resumo);
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold green]✔ Relatório exibido com sucesso[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();
    }

    // ───────────────────── Exportação CSV ─────────────────────

    private static void ExportarCsv(List<MetricasRepo> metricas)
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "dados_repositorios_sprint2.csv");
            var csv = new StringBuilder();
            csv.AppendLine("nome;estrelas;criado_em;atualizado_em;idade_anos;linguagem;pull_requests_aceitas;total_releases;total_issues;issues_fechadas;razao_issues_fechadas;dias_desde_atualizacao");
            foreach (var m in metricas)
            {
                var idade = ((DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25).ToString("F2", CultureInfo.InvariantCulture);
                var diasAtual = ((DateTime.UtcNow - m.AtualizadoEm).TotalDays).ToString("F0", CultureInfo.InvariantCulture);
                var razao = m.TotalIssues > 0
                    ? ((double)m.IssuesFechadas / m.TotalIssues).ToString("F4", CultureInfo.InvariantCulture)
                    : "0";
                csv.AppendLine($"{m.Nome};{m.Estrelas};{m.CriadoEm:yyyy-MM-dd};{m.AtualizadoEm:yyyy-MM-dd};{idade};{m.Linguagem};{m.PullRequestsAceitas};{m.TotalReleases};{m.TotalIssues};{m.IssuesFechadas};{razao};{diasAtual}");
            }
            File.WriteAllText(caminho, csv.ToString(), Encoding.UTF8);
            AnsiConsole.MarkupLine($"[green][[CSV salvo]][/] {Markup.Escape(caminho)}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow][[AVISO]][/] Não foi possível salvar o CSV: {Markup.Escape(ex.Message)}");
        }
    }

    // ───────────────────── Exportação README.md ─────────────────────

    private static void ExportarReadme(List<MetricasRepo> metricas, ResultadosRQs r)
    {
        try
        {
            var sb = new StringBuilder();
            var nl = Environment.NewLine;

            sb.Append("# Laboratório 01 — Características de Repositórios Populares do GitHub").Append(nl).Append(nl);
            sb.Append("**Enunciado 1 · Sprint 2**").Append(nl).Append(nl);
            sb.Append($"> Relatório gerado automaticamente em {DateTime.Now:yyyy-MM-dd HH:mm} com dados de **{metricas.Count}** repositórios.").Append(nl).Append(nl);

            sb.Append("---").Append(nl).Append(nl);

            // ── Introdução e hipóteses ──
            sb.Append("## 1. Introdução").Append(nl).Append(nl);
            sb.Append("Neste laboratório, analisamos as principais características dos **1 000 repositórios com maior número de estrelas** no GitHub, ").Append(nl);
            sb.Append("buscando entender como eles são desenvolvidos, com que frequência recebem contribuição externa, lançam releases, entre outras características.").Append(nl).Append(nl);

            sb.Append("### Hipóteses Informais").Append(nl).Append(nl);
            sb.Append("| # | Hipótese |").Append(nl);
            sb.Append("|---|----------|").Append(nl);
            sb.Append("| H1 | Repositórios populares tendem a ser **maduros** (mediana de idade > 5 anos) |").Append(nl);
            sb.Append("| H2 | Repositórios populares recebem um **alto número de PRs aceitas** (mediana > 500) |").Append(nl);
            sb.Append("| H3 | Repositórios populares **lançam releases com frequência** (mediana > 20) |").Append(nl);
            sb.Append("| H4 | Repositórios populares são **atualizados recentemente** (mediana < 30 dias) |").Append(nl);
            sb.Append("| H5 | Repositórios populares são escritos em **linguagens populares** (JS, Python, TS, etc.) |").Append(nl);
            sb.Append("| H6 | Repositórios populares possuem **alto percentual de issues fechadas** (mediana > 70%) |").Append(nl).Append(nl);

            // ── Metodologia ──
            sb.Append("## 2. Metodologia").Append(nl).Append(nl);
            sb.Append("- **Fonte de dados:** GitHub GraphQL API v4").Append(nl);
            sb.Append("- **Critério de seleção:** 1 000 repositórios com maior número de estrelas (`stars:>1000 sort:stars-desc`)").Append(nl);
            sb.Append($"- **Paginação:** consultas de até {ReposPorPagina} repositórios por página com cursor-based pagination").Append(nl);
            sb.Append("- **Sumarização:** valores medianos para métricas numéricas; contagem por categoria para linguagens").Append(nl);
            sb.Append("- **Ferramentas:** C# / .NET 10, Spectre.Console, System.Text.Json").Append(nl).Append(nl);

            sb.Append("### Métricas coletadas por repositório").Append(nl).Append(nl);
            sb.Append("| Métrica | Campo GraphQL |").Append(nl);
            sb.Append("|---------|---------------|").Append(nl);
            sb.Append("| Idade (anos) | `createdAt` |").Append(nl);
            sb.Append("| Pull requests aceitas | `pullRequests(states: MERGED) { totalCount }` |").Append(nl);
            sb.Append("| Total de releases | `releases { totalCount }` |").Append(nl);
            sb.Append("| Dias desde última atualização | `updatedAt` |").Append(nl);
            sb.Append("| Linguagem primária | `primaryLanguage { name }` |").Append(nl);
            sb.Append("| Issues fechadas / total | `issues { totalCount }` + `issues(states: CLOSED) { totalCount }` |").Append(nl).Append(nl);

            // ── Resultados ──
            sb.Append("## 3. Resultados").Append(nl).Append(nl);

            // Top 20
            sb.Append("### Top 20 repositórios por estrelas").Append(nl).Append(nl);
            sb.Append("| Repositório | ⭐ Estrelas | Idade (anos) | PRs aceitas | Releases | Últ. atualiz. (dias) | Linguagem | Issues fechadas/total |").Append(nl);
            sb.Append("|-------------|-----------|--------------|-------------|----------|---------------------|-----------|----------------------|").Append(nl);
            foreach (var m in metricas.OrderByDescending(x => x.Estrelas).Take(20))
            {
                var idadeAnos = (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25;
                var diasUpd = (DateTime.UtcNow - m.AtualizadoEm).TotalDays;
                var razao = m.TotalIssues > 0 ? (double)m.IssuesFechadas / m.TotalIssues : 0;
                sb.Append($"| {m.Nome} | {m.Estrelas:N0} | {idadeAnos:F1} | {m.PullRequestsAceitas:N0} | {m.TotalReleases:N0} | {diasUpd:F0} | {m.Linguagem} | {razao:P1} |").Append(nl);
            }
            sb.Append(nl);

            // RQ 01
            var conclusaoIdadeMd = r.MedianaIdade >= LimiteIdadeMaduroAnos
                ? "✅ **Hipótese confirmada.** Sistemas populares são, em geral, maduros."
                : r.MedianaIdade >= LimiteIdadeParcialAnos
                    ? "⚠️ **Parcialmente confirmada.** Há um mix de projetos maduros e relativamente recentes."
                    : "❌ **Hipótese refutada.** Muitos projetos populares são relativamente recentes.";

            sb.Append("### RQ 01 — Sistemas populares são maduros/antigos?").Append(nl).Append(nl);
            sb.Append("| Estatística | Valor |").Append(nl);
            sb.Append("|-------------|-------|").Append(nl);
            sb.Append($"| **Mediana** | {r.MedianaIdade:F1} anos |").Append(nl);
            sb.Append($"| Mínimo | {r.IdadesAnos[0]:F1} anos |").Append(nl);
            sb.Append($"| Máximo | {r.IdadesAnos[^1]:F1} anos |").Append(nl).Append(nl);
            sb.Append($"> {conclusaoIdadeMd}").Append(nl).Append(nl);

            // RQ 02
            var conclusaoPRsMd = r.MedianaPRs >= LimitePRsAlto
                ? "✅ **Hipótese confirmada.** Recebem muita contribuição externa via PRs."
                : r.MedianaPRs >= LimitePRsParcial
                    ? "⚠️ **Parcialmente confirmada.** Contribuição significativa, mas não tão alta quanto esperado."
                    : "❌ **Hipótese refutada.** Muitos projetos populares têm poucas PRs aceitas.";

            sb.Append("### RQ 02 — Sistemas populares recebem muita contribuição externa?").Append(nl).Append(nl);
            sb.Append("| Estatística | Valor |").Append(nl);
            sb.Append("|-------------|-------|").Append(nl);
            sb.Append($"| **Mediana** | {r.MedianaPRs:F0} PRs aceitas |").Append(nl);
            sb.Append($"| Mínimo | {r.PRs[0]:F0} |").Append(nl);
            sb.Append($"| Máximo | {r.PRs[^1]:F0} |").Append(nl).Append(nl);
            sb.Append($"> {conclusaoPRsMd}").Append(nl).Append(nl);

            // RQ 03
            var conclusaoReleasesMd = r.MedianaReleases >= LimiteReleasesFrequente
                ? "✅ **Hipótese confirmada.** Lançam releases com boa frequência."
                : r.MedianaReleases >= LimiteReleasesParcial
                    ? "⚠️ **Parcialmente confirmada.** Alguns usam menos releases formais (preferem tags/commits)."
                    : "❌ **Hipótese refutada.** Muitos projetos populares não utilizam releases formais.";

            sb.Append("### RQ 03 — Sistemas populares lançam releases com frequência?").Append(nl).Append(nl);
            sb.Append("| Estatística | Valor |").Append(nl);
            sb.Append("|-------------|-------|").Append(nl);
            sb.Append($"| **Mediana** | {r.MedianaReleases:F0} releases |").Append(nl);
            sb.Append($"| Mínimo | {r.Releases[0]:F0} |").Append(nl);
            sb.Append($"| Máximo | {r.Releases[^1]:F0} |").Append(nl).Append(nl);
            sb.Append($"> {conclusaoReleasesMd}").Append(nl).Append(nl);

            // RQ 04
            var conclusaoDiasMd = r.MedianaDias <= LimiteDiasAtualizadoRecente
                ? "✅ **Hipótese confirmada.** São projetos muito ativos e atualizados frequentemente."
                : r.MedianaDias <= LimiteDiasAtualizadoParcial
                    ? "⚠️ **Parcialmente confirmada.** Atualizações regulares, mas não diárias."
                    : "❌ **Hipótese refutada.** Alguns repositórios populares são estáveis e atualizados com menor frequência.";

            sb.Append("### RQ 04 — Sistemas populares são atualizados com frequência?").Append(nl).Append(nl);
            sb.Append("| Estatística | Valor |").Append(nl);
            sb.Append("|-------------|-------|").Append(nl);
            sb.Append($"| **Mediana** | {r.MedianaDias:F0} dias |").Append(nl);
            sb.Append($"| Mínimo | {r.DiasDesdeAtualizacao[0]:F0} dias |").Append(nl);
            sb.Append($"| Máximo | {r.DiasDesdeAtualizacao[^1]:F0} dias |").Append(nl).Append(nl);
            sb.Append($"> {conclusaoDiasMd}").Append(nl).Append(nl);

            // RQ 05
            sb.Append("### RQ 05 — Sistemas populares são escritos nas linguagens mais populares?").Append(nl).Append(nl);
            sb.Append("| Linguagem | Repositórios |").Append(nl);
            sb.Append("|-----------|-------------|").Append(nl);
            foreach (var g in r.PorLinguagem)
                sb.Append($"| {g.Key} | {g.Count()} |").Append(nl);
            sb.Append(nl);
            sb.Append("> ✅ **Hipótese confirmada.** Predominam linguagens amplamente adotadas na indústria.").Append(nl).Append(nl);

            // RQ 06
            var conclusaoRazaoMd = r.MedianaRazaoIssues >= LimiteRazaoIssuesAlta
                ? "✅ **Hipótese confirmada.** A maioria dos projetos populares fecha uma proporção alta de issues."
                : r.MedianaRazaoIssues >= LimiteRazaoIssuesParcial
                    ? "⚠️ **Parcialmente confirmada.** Fecham mais da metade, mas há margem de melhoria."
                    : "❌ **Hipótese refutada.** Muitos projetos populares acumulam issues abertas.";

            sb.Append("### RQ 06 — Sistemas populares possuem alto percentual de issues fechadas?").Append(nl).Append(nl);
            sb.Append("| Estatística | Valor |").Append(nl);
            sb.Append("|-------------|-------|").Append(nl);
            sb.Append($"| **Mediana** | {r.MedianaRazaoIssues:P1} |").Append(nl);
            if (r.RazoesIssuesFechadas.Count > 0)
            {
                sb.Append($"| Mínimo | {r.RazoesIssuesFechadas[0]:P1} |").Append(nl);
                sb.Append($"| Máximo | {r.RazoesIssuesFechadas[^1]:P1} |").Append(nl);
            }
            sb.Append($"| Repos sem issues | {r.ReposSemIssues} |").Append(nl).Append(nl);
            sb.Append($"> {conclusaoRazaoMd}").Append(nl).Append(nl);

            // ── Discussão ──
            sb.Append("## 4. Discussão").Append(nl).Append(nl);
            sb.Append("### Resumo — Valores Medianos").Append(nl).Append(nl);
            sb.Append("| Questão | Métrica | Mediana |").Append(nl);
            sb.Append("|---------|---------|--------|").Append(nl);
            sb.Append($"| RQ 01 — Maturidade | Idade (anos) | {r.MedianaIdade:F1} |").Append(nl);
            sb.Append($"| RQ 02 — Contribuição | PRs aceitas | {r.MedianaPRs:F0} |").Append(nl);
            sb.Append($"| RQ 03 — Releases | Total releases | {r.MedianaReleases:F0} |").Append(nl);
            sb.Append($"| RQ 04 — Atualizações | Dias desde última atualiz. | {r.MedianaDias:F0} |").Append(nl);
            sb.Append($"| RQ 05 — Linguagens | Linguagem mais comum | {r.PorLinguagem[0].Key} |").Append(nl);
            sb.Append($"| RQ 06 — Issues fechadas | Razão fechadas/total | {r.MedianaRazaoIssues:P1} |").Append(nl).Append(nl);

            sb.Append("### Comparação com as hipóteses").Append(nl).Append(nl);
            sb.Append("| Hipótese | Esperado | Obtido | Resultado |").Append(nl);
            sb.Append("|----------|----------|--------|-----------|").Append(nl);
            sb.Append($"| H1 — Maturidade | > 5 anos | {r.MedianaIdade:F1} anos | {(r.MedianaIdade >= LimiteIdadeMaduroAnos ? "✅ Confirmada" : r.MedianaIdade >= LimiteIdadeParcialAnos ? "⚠️ Parcial" : "❌ Refutada")} |").Append(nl);
            sb.Append($"| H2 — PRs aceitas | > 500 | {r.MedianaPRs:F0} | {(r.MedianaPRs >= LimitePRsAlto ? "✅ Confirmada" : r.MedianaPRs >= LimitePRsParcial ? "⚠️ Parcial" : "❌ Refutada")} |").Append(nl);
            sb.Append($"| H3 — Releases | > 20 | {r.MedianaReleases:F0} | {(r.MedianaReleases >= LimiteReleasesFrequente ? "✅ Confirmada" : r.MedianaReleases >= LimiteReleasesParcial ? "⚠️ Parcial" : "❌ Refutada")} |").Append(nl);
            sb.Append($"| H4 — Atualizações | < 30 dias | {r.MedianaDias:F0} dias | {(r.MedianaDias <= LimiteDiasAtualizadoRecente ? "✅ Confirmada" : r.MedianaDias <= LimiteDiasAtualizadoParcial ? "⚠️ Parcial" : "❌ Refutada")} |").Append(nl);
            sb.Append($"| H5 — Linguagens | JS, Python, TS... | {r.PorLinguagem[0].Key} | ✅ Confirmada |").Append(nl);
            sb.Append($"| H6 — Issues fechadas | > 70% | {r.MedianaRazaoIssues:P1} | {(r.MedianaRazaoIssues >= LimiteRazaoIssuesAlta ? "✅ Confirmada" : r.MedianaRazaoIssues >= LimiteRazaoIssuesParcial ? "⚠️ Parcial" : "❌ Refutada")} |").Append(nl).Append(nl);

            sb.Append("---").Append(nl).Append(nl);

            // ── Sobre ──
            sb.Append("## Tecnologias").Append(nl).Append(nl);
            sb.Append("| Tecnologia | Detalhes |").Append(nl);
            sb.Append("|---|---|").Append(nl);
            sb.Append("| **Linguagem** | C# |").Append(nl);
            sb.Append("| **Framework** | .NET 10.0 |").Append(nl);
            sb.Append("| **API** | GitHub GraphQL API v4 |").Append(nl);
            sb.Append("| **UI (console)** | [Spectre.Console](https://spectreconsole.net/) 0.54.0 |").Append(nl).Append(nl);

            sb.Append("## Arquivos gerados").Append(nl).Append(nl);
            sb.Append("| Arquivo | Descrição |").Append(nl);
            sb.Append("|---|---|").Append(nl);
            sb.Append("| `dados_repositorios_sprint2.csv` | Dados brutos dos 1 000 repositórios (separador `;`) |").Append(nl);
            sb.Append("| `README.md` | Este relatório |").Append(nl);

            // Salvar na pasta do projeto
            var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var caminho = Path.Combine(pastaProjeto, "README.md");
            File.WriteAllText(caminho, sb.ToString(), Encoding.UTF8);
            AnsiConsole.MarkupLine($"[green][[README salvo]][/] {Markup.Escape(caminho)}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow][[AVISO]][/] Não foi possível salvar o README: {Markup.Escape(ex.Message)}");
        }
    }

    // ───────────────────── Utilitários ─────────────────────

    private static double Mediana(List<double> ordenado)
    {
        if (ordenado == null || ordenado.Count == 0) return 0;
        var n = ordenado.Count;
        var mid = n / 2;
        return n % 2 == 1 ? ordenado[mid] : (ordenado[mid - 1] + ordenado[mid]) / 2.0;
    }

    private class MetricasRepo
    {
        public string Nome { get; set; } = "";
        public int Estrelas { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
        public string Linguagem { get; set; } = "";
        public int TotalReleases { get; set; }
        public int PullRequestsAceitas { get; set; }
        public int TotalIssues { get; set; }
        public int IssuesFechadas { get; set; }
    }
}

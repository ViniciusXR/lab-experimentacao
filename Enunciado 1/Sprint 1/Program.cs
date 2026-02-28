using System.Globalization;
using System.Text;
using System.Text.Json;
using Spectre.Console;
namespace Enunciado1.Sprint1;

/// <summary>
/// Laboratório 01 - Características de Repositórios Populares (GitHub).
/// Enunciado 1, Sprint 1: 6 questões de pesquisa + bônus (nota máxima).
/// </summary>
class Program
{
    private static readonly HttpClient Http = new();
    private const string GraphQLEndpoint = "https://api.github.com/graphql";
    private const int ReposPorPagina = 30;
    private const int TotalRepos = 100;      
    private const int TotalReposBonus = 200; 

    // Token do GitHub (evita limite anônimo 60/h → 5000/h com autenticação)
    private static string? _token;

    static Program()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "LabEXPSoftware-Lab01-Enunciado1");
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

    /// <summary>Obtém token dos args (--token=xxx ou --token xxx).</summary>
    private static string? ObterTokenDosArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--token=", StringComparison.OrdinalIgnoreCase))
                return args[i].Substring("--token=".Length).Trim();
            if (string.Equals(args[i], "--token", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1].Trim();
        }
        return null;
    }

    /// <summary>Lê token do arquivo .github-token na pasta do projeto ou na pasta atual (arquivo não vai para o Git).</summary>
    private static string? LerTokenDoArquivo()
    {
        // Pasta do projeto (onde está o .csproj): ao rodar com F5, o exe está em bin\Debug\net10.0
        var baseDir = AppContext.BaseDirectory;
        var pastaProjeto = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        var dirs = new[]
        {
            pastaProjeto,
            Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? "",
            baseDir,
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "Enunciado 1", "Sprint 1")
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

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Token: 1) argumento --token=xxx  2) variável GITHUB_TOKEN  3) arquivo .github-token (não commitar)
        var token = ObterTokenDosArgs(args)
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? LerTokenDoArquivo();
        AplicarToken(token);

        AnsiConsole.Write(new Panel(
                new Markup("[bold]LABORATÓRIO 01[/] - Características de Repositórios Populares\n[dim]Enunciado 1 - Sprint 1 (.NET) — GraphQL API[/]"))
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

        var usarBonus = args.Contains("--bonus", StringComparer.OrdinalIgnoreCase);
        var total = usarBonus ? TotalReposBonus : TotalRepos;
        AnsiConsole.MarkupLine($"[green]✔ Autenticado[/] — Modo GraphQL ([cyan]{total}[/] repositórios)\n");

        List<MetricasRepo> metricas = null!;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("Buscando repositórios populares via GraphQL...", async ctx =>
            {
                metricas = await BuscarRepositoriosGraphQLAsync(total, ctx).ConfigureAwait(false);
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

        EscreverRelatorio(metricas);
        ExportarRelatorioArquivo(metricas);
        ExportarCsv(metricas);
        Console.ReadKey();
    }

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
                issues(states: OPEN) { totalCount }
                forkCount
                releases { totalCount }
              }
            }
          }
        }";

    private static async Task<List<MetricasRepo>> BuscarRepositoriosGraphQLAsync(int total, StatusContext ctx)
    {
        var lista = new List<MetricasRepo>();
        string? cursor = null;

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
            if (string.IsNullOrEmpty(json)) break;

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
                    OpenIssuesCount = node.GetProperty("issues").GetProperty("totalCount").GetInt32(),
                    ForksCount = node.GetProperty("forkCount").GetInt32(),
                    TotalReleases = node.GetProperty("releases").GetProperty("totalCount").GetInt32()
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
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var res = await Http.PostAsync(GraphQLEndpoint, content).ConfigureAwait(false);

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
                    var resetUnix = res.Headers.TryGetValues("X-RateLimit-Reset", out var vals) && long.TryParse(vals.FirstOrDefault(), out var t) ? t : 0L;
                    var segundosAteReset = resetUnix > 0 ? (int)(resetUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 60;
                    if (segundosAteReset < 1) segundosAteReset = 60;
                    if (segundosAteReset > 3600) segundosAteReset = 3600;
                    _ultimoErro = $"Rate limit atingido. Aguardando {segundosAteReset}s...";
                    await Task.Delay(TimeSpan.FromSeconds(segundosAteReset)).ConfigureAwait(false);
                    res = await Http.PostAsync(GraphQLEndpoint, new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(false);
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

    private static string ConstruirRelatorio(List<MetricasRepo> metricas)
    {
        var sb = new StringBuilder();
        var nl = Environment.NewLine;

        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl);
        sb.Append("  LABORATÓRIO 01 - Características de Repositórios Populares").Append(nl);
        sb.Append("  Enunciado 1 - Sprint 1 | Data: ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append(nl);
        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl).Append(nl);

        // Tabela: repositórios analisados (dados brutos)
        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl);
        sb.Append("  TABELA DE REPOSITÓRIOS ANALISADOS").Append(nl);
        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl);
        sb.Append("Repositório                    | Estrelas | Idade(anos) | Linguagem  | Releases | Forks   | Issues ab.").Append(nl);
        sb.Append("--------------------------------|----------|-------------|-----------|----------|---------|-----------").Append(nl);
        foreach (var m in metricas.OrderByDescending(x => x.Estrelas))
        {
            var idadeAnos = (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25;
            var nome = (m.Nome.Length <= 30 ? m.Nome : m.Nome[..27] + "...").PadRight(30);
            var lang = (m.Linguagem.Length <= 9 ? m.Linguagem : m.Linguagem[..9]).PadRight(9);
            sb.Append($"{nome} | {m.Estrelas,8:N0} | {idadeAnos,11:F1} | {lang} | {m.TotalReleases,8} | {m.ForksCount,7:N0} | {m.OpenIssuesCount,9}").Append(nl);
        }
        sb.Append(nl);

        // 1. Maturidade
        var idadesAnos = metricas.Select(m => (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25).OrderBy(x => x).ToList();
        var medianaIdade = Mediana(idadesAnos);
        sb.Append("───────────────────────────────────────────────────────────────").Append(nl);
        sb.Append("1. MATURIDADE - Sistemas populares são maturos/antigos?").Append(nl);
        sb.Append("   Métrica: idade do repositório (anos)").Append(nl);
        sb.Append($"   Mediana da idade: {medianaIdade:F1} anos").Append(nl);
        sb.Append("   Conclusão: ").Append(medianaIdade >= 5 ? "Sim, em geral são repositórios maduros." : medianaIdade >= 2 ? "Em parte: há mix de projetos maduros e mais recentes." : "Muitos são relativamente recentes; popularidade não exige idade longa.").Append(nl).Append(nl);

        // 2. Contribuição externa (forks = engajamento da comunidade)
        var forks = metricas.Select(m => (double)m.ForksCount).OrderBy(x => x).ToList();
        var medianaForks = Mediana(forks);
        sb.Append("───────────────────────────────────────────────────────────────").Append(nl);
        sb.Append("2. CONTRIBUIÇÃO EXTERNA - Recebem engajamento da comunidade?").Append(nl);
        sb.Append("   Métrica: número de forks (indica uso e contribuição externa)").Append(nl);
        sb.Append($"   Mediana de forks: {medianaForks:F0}").Append(nl);
        sb.Append("   Conclusão: ").Append(medianaForks >= 1000 ? "Sim; muitos forks indicam grande engajamento." : medianaForks >= 100 ? "Sim; engajamento significativo da comunidade." : "Variável; há projetos com muitos ou poucos forks entre os populares.").Append(nl).Append(nl);

        // 3. Frequência de releases
        var releases = metricas.Select(m => (double)m.TotalReleases).OrderBy(x => x).ToList();
        var medianaReleases = Mediana(releases);
        sb.Append("───────────────────────────────────────────────────────────────").Append(nl);
        sb.Append("3. FREQUÊNCIA DE RELEASES - Lançam releases com frequência?").Append(nl);
        sb.Append("   Métrica: total de releases publicadas").Append(nl);
        sb.Append($"   Mediana de releases: {medianaReleases:F0}").Append(nl);
        sb.Append("   Conclusão: ").Append(medianaReleases >= 50 ? "Sim, lançam releases com boa frequência." : medianaReleases >= 10 ? "Em geral sim; alguns projetos usam menos releases formais." : "Variável; muitos projetos populares têm poucas releases ou usam tags.").Append(nl).Append(nl);

        // 4. Atualizações
        var diasDesdeAtualizacao = metricas.Select(m => (DateTime.UtcNow - m.AtualizadoEm).TotalDays).OrderBy(x => x).ToList();
        var medianaDias = Mediana(diasDesdeAtualizacao);
        sb.Append("───────────────────────────────────────────────────────────────").Append(nl);
        sb.Append("4. ATUALIZAÇÕES - São atualizados regularmente?").Append(nl);
        sb.Append("   Métrica: dias desde a última atualização").Append(nl);
        sb.Append($"   Mediana (dias desde última atualização): {medianaDias:F0} dias").Append(nl);
        sb.Append("   Conclusão: ").Append(medianaDias <= 30 ? "Sim, em geral muito ativos." : medianaDias <= 90 ? "Sim, atualizações regulares." : "Variável; alguns estão muito ativos, outros mais estáveis.").Append(nl).Append(nl);

        // 5. Linguagens
        var porLinguagem = metricas.GroupBy(m => m.Linguagem).OrderByDescending(g => g.Count()).ToList();
        sb.Append("───────────────────────────────────────────────────────────────").Append(nl);
        sb.Append("5. LINGUAGENS - São escritos em linguagens populares?").Append(nl);
        sb.Append("   Métrica: linguagem primária do repositório").Append(nl);
        sb.Append("   Distribuição (top 5):").Append(nl);
        foreach (var g in porLinguagem.Take(5))
            sb.Append($"     {g.Key}: {g.Count()} repositórios").Append(nl);
        sb.Append("   Conclusão: Sim; predominam linguagens muito usadas (JavaScript, Python, etc.).").Append(nl).Append(nl);

        // 6. Manutenção (issues abertas = backlog; quanto menor, melhor)
        var issuesAbertas = metricas.Select(m => (double)m.OpenIssuesCount).OrderBy(x => x).ToList();
        var medianaIssuesAbertas = Mediana(issuesAbertas);
        sb.Append("───────────────────────────────────────────────────────────────").Append(nl);
        sb.Append("6. MANUTENÇÃO - Mantêm o backlog de issues sob controle?").Append(nl);
        sb.Append("   Métrica: número de issues abertas (quanto menor, mais sob controle)").Append(nl);
        sb.Append($"   Mediana de issues abertas: {medianaIssuesAbertas:F0}").Append(nl);
        sb.Append("   Conclusão: ").Append(medianaIssuesAbertas <= 50 ? "Sim; em geral mantêm poucas issues abertas." : medianaIssuesAbertas <= 200 ? "Variável; alguns mantêm bem, outros acumulam mais." : "Muitos projetos populares têm grande volume de issues abertas.").Append(nl).Append(nl);

        // BÔNUS 
        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl);
        sb.Append("  Atividade Bônus - Análises adicionais").Append(nl);
        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl).Append(nl);
        var maisAntigo = metricas.OrderBy(m => m.CriadoEm).First();
        var maisRecente = metricas.OrderByDescending(m => m.CriadoEm).First();
        sb.Append($"   Repositório mais antigo: {maisAntigo.Nome} ({maisAntigo.CriadoEm:yyyy-MM-dd})").Append(nl);
        sb.Append($"   Repositório mais novo (entre os populares): {maisRecente.Nome} ({maisRecente.CriadoEm:yyyy-MM-dd})").Append(nl);
        var maisEstrelas = metricas.OrderByDescending(m => m.Estrelas).First();
        sb.Append($"   Maior número de estrelas: {maisEstrelas.Nome} ({maisEstrelas.Estrelas:N0} estrelas)").Append(nl);
        var maisForks = metricas.OrderByDescending(m => m.ForksCount).First();
        sb.Append($"   Maior número de forks: {maisForks.Nome} ({maisForks.ForksCount:N0} forks)").Append(nl);
        var maisReleases = metricas.OrderByDescending(m => m.TotalReleases).First();
        sb.Append($"   Mais releases: {maisReleases.Nome} ({maisReleases.TotalReleases} releases)").Append(nl);
        sb.Append(nl).Append("   Resumo por linguagem (todas):").Append(nl);
        foreach (var g in porLinguagem)
            sb.Append($"     {g.Key}: {g.Count()} repos, mediana estrelas: {Mediana(g.Select(x => (double)x.Estrelas).OrderBy(x => x).ToList()):N0}").Append(nl);
        sb.Append(nl).Append("═══════════════════════════════════════════════════════════════").Append(nl);
        sb.Append("  Relatório gerado com sucesso. Arquivos: relatorio_lab01_sprint1.txt, dados_repositorios.csv").Append(nl);
        sb.Append("═══════════════════════════════════════════════════════════════").Append(nl);
        return sb.ToString();
    }

    private static void EscreverRelatorio(List<MetricasRepo> metricas)
    {
        // ── Cabeçalho ──
        AnsiConsole.Write(new Rule("[bold yellow]RELATÓRIO — Repositórios Populares do GitHub[/]").RuleStyle("yellow"));
        AnsiConsole.MarkupLine($"[dim]Data: {DateTime.Now:yyyy-MM-dd HH:mm}[/]\n");

        // ── Tabela de repositórios ──
        var tabela = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("cyan"))
            .Title("[bold cyan]TABELA DE REPOSITÓRIOS ANALISADOS[/]")
            .AddColumn(new TableColumn("[bold]Repositório[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]⭐ Estrelas[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]📅 Idade (anos)[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]💻 Linguagem[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]📦 Releases[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]🍴 Forks[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]🐛 Issues[/]").RightAligned());

        foreach (var m in metricas.OrderByDescending(x => x.Estrelas))
        {
            var idadeAnos = (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25;
            tabela.AddRow(
                Markup.Escape(m.Nome),
                $"[yellow]{m.Estrelas:N0}[/]",
                $"{idadeAnos:F1}",
                m.Linguagem == "(não detectada)" ? "[dim]N/A[/]" : $"[green]{Markup.Escape(m.Linguagem)}[/]",
                m.TotalReleases.ToString("N0"),
                $"{m.ForksCount:N0}",
                m.OpenIssuesCount.ToString("N0"));
        }
        AnsiConsole.Write(tabela);
        AnsiConsole.WriteLine();

        // ── Questões de pesquisa ──
        // 1. Maturidade
        var idadesAnos = metricas.Select(m => (DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25).OrderBy(x => x).ToList();
        var medianaIdade = Mediana(idadesAnos);
        var corIdade = medianaIdade >= 5 ? "green" : medianaIdade >= 2 ? "yellow" : "red";
        var conclusaoIdade = medianaIdade >= 5 ? "Sim, em geral são repositórios maduros."
            : medianaIdade >= 2 ? "Em parte: há mix de projetos maduros e mais recentes."
            : "Muitos são relativamente recentes; popularidade não exige idade longa.";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] idade do repositório (anos)\n"
                    + $"[bold]Mediana:[/] [{corIdade}]{medianaIdade:F1} anos[/]\n"
                    + $"[bold]Conclusão:[/] [{corIdade}]{Markup.Escape(conclusaoIdade)}[/]"))
            .Header("[bold blue]1. MATURIDADE — Sistemas populares são maturos/antigos?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // 2. Contribuição externa
        var forks = metricas.Select(m => (double)m.ForksCount).OrderBy(x => x).ToList();
        var medianaForks = Mediana(forks);
        var corForks = medianaForks >= 1000 ? "green" : medianaForks >= 100 ? "yellow" : "red";
        var conclusaoForks = medianaForks >= 1000 ? "Sim; muitos forks indicam grande engajamento."
            : medianaForks >= 100 ? "Sim; engajamento significativo da comunidade."
            : "Variável; há projetos com muitos ou poucos forks entre os populares.";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] número de forks\n"
                    + $"[bold]Mediana:[/] [{corForks}]{medianaForks:F0}[/]\n"
                    + $"[bold]Conclusão:[/] [{corForks}]{Markup.Escape(conclusaoForks)}[/]"))
            .Header("[bold blue]2. CONTRIBUIÇÃO EXTERNA — Recebem engajamento da comunidade?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // 3. Frequência de releases
        var releases = metricas.Select(m => (double)m.TotalReleases).OrderBy(x => x).ToList();
        var medianaReleases = Mediana(releases);
        var corReleases = medianaReleases >= 50 ? "green" : medianaReleases >= 10 ? "yellow" : "red";
        var conclusaoReleases = medianaReleases >= 50 ? "Sim, lançam releases com boa frequência."
            : medianaReleases >= 10 ? "Em geral sim; alguns projetos usam menos releases formais."
            : "Variável; muitos projetos populares têm poucas releases ou usam tags.";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] total de releases publicadas\n"
                    + $"[bold]Mediana:[/] [{corReleases}]{medianaReleases:F0}[/]\n"
                    + $"[bold]Conclusão:[/] [{corReleases}]{Markup.Escape(conclusaoReleases)}[/]"))
            .Header("[bold blue]3. FREQUÊNCIA DE RELEASES — Lançam releases com frequência?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // 4. Atualizações
        var diasDesdeAtualizacao = metricas.Select(m => (DateTime.UtcNow - m.AtualizadoEm).TotalDays).OrderBy(x => x).ToList();
        var medianaDias = Mediana(diasDesdeAtualizacao);
        var corDias = medianaDias <= 30 ? "green" : medianaDias <= 90 ? "yellow" : "red";
        var conclusaoDias = medianaDias <= 30 ? "Sim, em geral muito ativos."
            : medianaDias <= 90 ? "Sim, atualizações regulares."
            : "Variável; alguns estão muito ativos, outros mais estáveis.";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] dias desde a última atualização\n"
                    + $"[bold]Mediana:[/] [{corDias}]{medianaDias:F0} dias[/]\n"
                    + $"[bold]Conclusão:[/] [{corDias}]{Markup.Escape(conclusaoDias)}[/]"))
            .Header("[bold blue]4. ATUALIZAÇÕES — São atualizados regularmente?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // 5. Linguagens — com BarChart
        var porLinguagem = metricas.GroupBy(m => m.Linguagem).OrderByDescending(g => g.Count()).ToList();
        var cores = new[] { Color.Green, Color.Yellow, Color.Blue, Color.Red, Color.Purple,
            Color.Aqua, Color.Orange1, Color.Fuchsia, Color.Lime, Color.Teal };
        var barChart = new BarChart()
            .Label("[bold blue]Distribuição por linguagem[/]")
            .Width(60);
        for (var i = 0; i < porLinguagem.Count; i++)
            barChart.AddItem(Markup.Escape(porLinguagem[i].Key), porLinguagem[i].Count(), cores[i % cores.Length]);
        AnsiConsole.Write(new Panel(barChart)
            .Header("[bold blue]5. LINGUAGENS — São escritos em linguagens populares?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.MarkupLine($"   [bold]Conclusão:[/] [green]Sim; predominam linguagens muito usadas (JavaScript, Python, etc.).[/]\n");

        // 6. Manutenção
        var issuesAbertas = metricas.Select(m => (double)m.OpenIssuesCount).OrderBy(x => x).ToList();
        var medianaIssues = Mediana(issuesAbertas);
        var corIssues = medianaIssues <= 50 ? "green" : medianaIssues <= 200 ? "yellow" : "red";
        var conclusaoIssues = medianaIssues <= 50 ? "Sim; em geral mantêm poucas issues abertas."
            : medianaIssues <= 200 ? "Variável; alguns mantêm bem, outros acumulam mais."
            : "Muitos projetos populares têm grande volume de issues abertas.";
        AnsiConsole.Write(new Panel(
                new Markup($"[bold]Métrica:[/] número de issues abertas\n"
                    + $"[bold]Mediana:[/] [{corIssues}]{medianaIssues:F0}[/]\n"
                    + $"[bold]Conclusão:[/] [{corIssues}]{Markup.Escape(conclusaoIssues)}[/]"))
            .Header("[bold blue]6. MANUTENÇÃO — Mantêm o backlog de issues sob controle?[/]")
            .Border(BoxBorder.Rounded).Expand());
        AnsiConsole.WriteLine();

        // ── BÔNUS ──
        AnsiConsole.Write(new Rule("[bold yellow]⭐ Atividade Bônus — Análises adicionais[/]").RuleStyle("yellow"));
        AnsiConsole.WriteLine();

        var maisAntigo = metricas.OrderBy(m => m.CriadoEm).First();
        var maisRecente = metricas.OrderByDescending(m => m.CriadoEm).First();
        var maisEstrelas = metricas.OrderByDescending(m => m.Estrelas).First();
        var maisForks = metricas.OrderByDescending(m => m.ForksCount).First();
        var maisReleases = metricas.OrderByDescending(m => m.TotalReleases).First();

        var bonusTabela = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("yellow"))
            .Title("[bold yellow]Destaques[/]")
            .AddColumn(new TableColumn("[bold]Categoria[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Repositório[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Valor[/]").RightAligned());
        bonusTabela.AddRow("📅 Mais antigo", Markup.Escape(maisAntigo.Nome), maisAntigo.CriadoEm.ToString("yyyy-MM-dd"));
        bonusTabela.AddRow("🆕 Mais recente", Markup.Escape(maisRecente.Nome), maisRecente.CriadoEm.ToString("yyyy-MM-dd"));
        bonusTabela.AddRow("[yellow]⭐ Mais estrelas[/]", Markup.Escape(maisEstrelas.Nome), $"[yellow]{maisEstrelas.Estrelas:N0}[/]");
        bonusTabela.AddRow("🍴 Mais forks", Markup.Escape(maisForks.Nome), $"{maisForks.ForksCount:N0}");
        bonusTabela.AddRow("📦 Mais releases", Markup.Escape(maisReleases.Nome), $"{maisReleases.TotalReleases:N0}");
        AnsiConsole.Write(bonusTabela);
        AnsiConsole.WriteLine();

        // Resumo por linguagem
        var langTabela = new Table()
            .Border(TableBorder.Simple)
            .Title("[bold]Resumo por linguagem[/]")
            .AddColumn(new TableColumn("[bold]Linguagem[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Repos[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Mediana ⭐[/]").RightAligned());
        foreach (var g in porLinguagem)
            langTabela.AddRow(
                Markup.Escape(g.Key),
                g.Count().ToString(),
                $"{Mediana(g.Select(x => (double)x.Estrelas).OrderBy(x => x).ToList()):N0}");
        AnsiConsole.Write(langTabela);
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold green]✔ Relatório gerado com sucesso[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();
    }

    private static void ExportarRelatorioArquivo(List<MetricasRepo> metricas)
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "relatorio_lab01_sprint1.txt");
            File.WriteAllText(caminho, ConstruirRelatorio(metricas), Encoding.UTF8);
            AnsiConsole.MarkupLine($"[green][[Arquivo salvo]][/] {Markup.Escape(caminho)}");
        }
        catch (Exception ex) { AnsiConsole.MarkupLine($"[yellow][[AVISO]][/] Não foi possível salvar o relatório: {Markup.Escape(ex.Message)}"); }
    }

    private static void ExportarCsv(List<MetricasRepo> metricas)
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "dados_repositorios.csv");
            var csv = new StringBuilder();
            csv.AppendLine("nome;estrelas;criado_em;atualizado_em;linguagem;open_issues;forks;releases;idade_anos");
            foreach (var m in metricas)
            {
                var idade = ((DateTime.UtcNow - m.CriadoEm).TotalDays / 365.25).ToString(CultureInfo.InvariantCulture);
                csv.AppendLine($"{m.Nome};{m.Estrelas};{m.CriadoEm:yyyy-MM-dd};{m.AtualizadoEm:yyyy-MM-dd};{m.Linguagem};{m.OpenIssuesCount};{m.ForksCount};{m.TotalReleases};{idade}");
            }
            File.WriteAllText(caminho, csv.ToString(), Encoding.UTF8);
            AnsiConsole.MarkupLine($"[green][[CSV salvo]][/] {Markup.Escape(caminho)}");
        }
        catch (Exception ex) { AnsiConsole.MarkupLine($"[yellow][[AVISO]][/] Não foi possível salvar o CSV: {Markup.Escape(ex.Message)}"); }
    }

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
        public int OpenIssuesCount { get; set; }
        public int ForksCount { get; set; }
        public int TotalReleases { get; set; }
    }
}

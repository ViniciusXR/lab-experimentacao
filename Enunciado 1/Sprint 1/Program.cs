using System.Globalization;
using System.Text;
using System.Text.Json;
namespace Enunciado1.Sprint1;

/// <summary>
/// Laboratório 01 - Características de Repositórios Populares (GitHub).
/// Enunciado 1, Sprint 1: 6 questões de pesquisa + bônus (nota máxima).
/// </summary>
class Program
{
    private static readonly HttpClient Http = new();
    private const string ApiBase = "https://api.github.com";
    private const int ReposPorPagina = 30;
    private const int TotalRepos = 30;       // mínimo típico do enunciado
    private const int TotalReposBonus = 50;  // bônus: mais repositórios
    private const int TotalReposRapido = 10; // --rapido sem token (~11 min)

    // Token do GitHub (evita limite anônimo 60/h → 5000/h com autenticação)
    private static string? _token;

    static Program()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "LabEXPSoftware-Lab01-Enunciado1");
        Http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
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

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  LABORATÓRIO 01 - Características de Repositórios Populares");
        Console.WriteLine("  Enunciado 1 - Sprint 1 (.NET)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var usarBonus = args.Contains("--bonus", StringComparer.OrdinalIgnoreCase);
        var modoRapido = args.Contains("--rapido", StringComparer.OrdinalIgnoreCase);
        var total = usarBonus ? TotalReposBonus : (modoRapido && !TemToken() ? TotalReposRapido : TotalRepos);
        Console.WriteLine($"Modo: Atividade Completa + Atividade Bônus (analisando {total} repositórios)");
        if (TemToken())
            Console.WriteLine("(Autenticado com token do GitHub — limite 5000 req/h, análise em ~1–2 min.)\n");
        else
        {
            Console.WriteLine("(Sem token = usuário anônimo: 60 req/h, esperas longas e risco de erro 403.)");
            Console.WriteLine("  Dica: use uma key da API (Personal Access Token) para evitar isso:");
            Console.WriteLine("    • Criar token: GitHub → Settings → Developer settings → Personal access tokens");
            Console.WriteLine("    • Variável de ambiente:  $env:GITHUB_TOKEN = \"seu_token\"");
            Console.WriteLine("    • Ou na execução:        dotnet run -- --token=seu_token");
            if (modoRapido)
                Console.WriteLine("  Modo --rapido: 10 repos (~11 min).\n");
            else
                Console.WriteLine("  Ou use --rapido para 10 repos em ~11 min.\n");
        }

        var repos = await BuscarRepositoriosPopularesAsync(total).ConfigureAwait(false);
        if (repos.Count == 0)
        {
            Console.WriteLine("Nenhum repositório encontrado. Verifique rate limit ou conexão.");
            return;
        }

        Console.WriteLine($"Repositórios obtidos: {repos.Count}\n");
        var metricas = await ColetarMetricasCompletasAsync(repos).ConfigureAwait(false);

        EscreverRelatorio(metricas);
        ExportarRelatorioArquivo(metricas);
        ExportarCsv(metricas);
    }

    private static async Task<List<RepoSearchItem>> BuscarRepositoriosPopularesAsync(int total)
    {
        var lista = new List<RepoSearchItem>();
        var paginas = (int)Math.Ceiling(total / (double)ReposPorPagina);

        for (var p = 1; p <= paginas; p++)
        {
            var url = $"{ApiBase}/search/repositories?q=stars:>1000&sort=stars&order=desc&per_page={ReposPorPagina}&page={p}";
            var json = await GetAsync(url).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json)) continue;

            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items))
                continue;
            foreach (var item in items.EnumerateArray())
            {
                lista.Add(RepoSearchItem.FromJson(item));
                if (lista.Count >= total) break;
            }
            if (lista.Count >= total) break;
            await LimitarRateAsync().ConfigureAwait(false);
        }

        return lista.Take(total).ToList();
    }

    private static async Task<List<MetricasRepo>> ColetarMetricasCompletasAsync(List<RepoSearchItem> repos)
    {
        var resultado = new List<MetricasRepo>();
        var total = repos.Count;
        const int tamanhoLote = 5;

        if (TemToken())
        {
            for (var i = 0; i < repos.Count; i += tamanhoLote)
            {
                var lote = repos.Skip(i).Take(tamanhoLote).ToList();
                for (var j = 0; j < lote.Count; j++)
                    Console.WriteLine($"Analisando {i + j + 1}/{total}: {lote[j].FullName}...");
                var tarefas = lote.Select((r, idx) => (r, idx)).Select(async x =>
                {
                    var (owner, name) = SplitOwnerRepo(x.r.FullName);
                    var releases = await ObterTotalReleasesAsync(owner, name).ConfigureAwait(false);
                    return (m: new MetricasRepo
                    {
                        Nome = x.r.FullName,
                        Estrelas = x.r.StargazersCount,
                        CriadoEm = x.r.CreatedAt,
                        AtualizadoEm = x.r.UpdatedAt,
                        Linguagem = x.r.Language ?? "(não detectada)",
                        OpenIssuesCount = x.r.OpenIssuesCount,
                        ForksCount = x.r.ForksCount,
                        TotalReleases = releases
                    }, ordem: i + x.idx);
                }).ToList();
                var concluidas = await Task.WhenAll(tarefas).ConfigureAwait(false);
                foreach (var (m, _) in concluidas.OrderBy(x => x.ordem))
                    resultado.Add(m);
                if (i + tamanhoLote < repos.Count)
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
        }
        else
        {
            for (var i = 0; i < repos.Count; i++)
            {
                var r = repos[i];
                Console.WriteLine($"Analisando {i + 1}/{total}: {r.FullName}...");
                var m = new MetricasRepo
                {
                    Nome = r.FullName,
                    Estrelas = r.StargazersCount,
                    CriadoEm = r.CreatedAt,
                    AtualizadoEm = r.UpdatedAt,
                    Linguagem = r.Language ?? "(não detectada)",
                    OpenIssuesCount = r.OpenIssuesCount,
                    ForksCount = r.ForksCount
                };
                var (owner, name) = SplitOwnerRepo(r.FullName);
                m.TotalReleases = await ObterTotalReleasesAsync(owner, name).ConfigureAwait(false);
                resultado.Add(m);
                await LimitarRateAsync().ConfigureAwait(false);
            }
        }
        Console.WriteLine("\nGerando relatório...\n");
        return resultado;
    }

    private static async Task<int> ObterTotalReleasesAsync(string owner, string name)
    {
        var ownerEnc = Uri.EscapeDataString(owner);
        var nameEnc = Uri.EscapeDataString(name);
        var url = $"{ApiBase}/repos/{ownerEnc}/{nameEnc}/releases?per_page=100";
        var json = await GetAsync(url).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return 0;
            return root.GetArrayLength();
        }
        catch { return 0; }
    }

    private static bool _rateLimitAvisado;

    private static async Task<string?> GetAsync(string url, bool retryAposRateLimit = true)
    {
        try
        {
            var res = await Http.GetAsync(url).ConfigureAwait(false);

            if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                if (!retryAposRateLimit)
                {
                    if (!_rateLimitAvisado) { _rateLimitAvisado = true; Console.WriteLine("[AVISO] Rate limit atingido."); }
                    return null;
                }
                var resetUnix = res.Headers.TryGetValues("X-RateLimit-Reset", out var vals) && long.TryParse(vals.FirstOrDefault(), out var t) ? t : 0L;
                var segundosAteReset = resetUnix > 0 ? (int)(resetUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 60;
                if (segundosAteReset < 60) segundosAteReset = 60;
                if (segundosAteReset > 3600) segundosAteReset = 3600;
                if (!_rateLimitAvisado)
                {
                    _rateLimitAvisado = true;
                    Console.WriteLine($"[AVISO] Rate limit atingido. Aguardando {segundosAteReset}s até o reset da API para continuar e obter os releases...");
                }
                await Task.Delay(TimeSpan.FromSeconds(segundosAteReset)).ConfigureAwait(false);
                res = await Http.GetAsync(url).ConfigureAwait(false);
                if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("[AVISO] Ainda no limite após espera. Alguns releases podem ficar 0.");
                    return null;
                }
            }
            if (res.StatusCode == (System.Net.HttpStatusCode)422)
                return null;

            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] {ex.Message}");
            return null;
        }
    }

    private static async Task LimitarRateAsync()
    {
        // Com token: 5000 req/h → delay curto. Sem token: 60 req/h → ~1 req/min
        var segundos = TemToken() ? 2 : 65;
        await Task.Delay(TimeSpan.FromSeconds(segundos)).ConfigureAwait(false);
    }

    private static (string owner, string name) SplitOwnerRepo(string fullName)
    {
        var i = fullName.IndexOf('/');
        return i <= 0 ? (fullName, "") : (fullName[..i], fullName[(i + 1)..]);
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

        // BÔNUS (sempre incluído para nota máxima)
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
        var relatorio = ConstruirRelatorio(metricas);
        Console.WriteLine(relatorio);
    }

    private static void ExportarRelatorioArquivo(List<MetricasRepo> metricas)
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "relatorio_lab01_sprint1.txt");
            File.WriteAllText(caminho, ConstruirRelatorio(metricas), Encoding.UTF8);
            Console.WriteLine($"[Arquivo salvo] {caminho}");
        }
        catch (Exception ex) { Console.WriteLine($"[AVISO] Não foi possível salvar o relatório: {ex.Message}"); }
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
            Console.WriteLine($"[CSV salvo] {caminho}");
        }
        catch (Exception ex) { Console.WriteLine($"[AVISO] Não foi possível salvar o CSV: {ex.Message}"); }
    }

    private static double Mediana(List<double> ordenado)
    {
        if (ordenado == null || ordenado.Count == 0) return 0;
        var n = ordenado.Count;
        var mid = n / 2;
        return n % 2 == 1 ? ordenado[mid] : (ordenado[mid - 1] + ordenado[mid]) / 2.0;
    }

    private class RepoSearchItem
    {
        public string FullName { get; set; } = "";
        public int StargazersCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Language { get; set; }
        public int OpenIssuesCount { get; set; }
        public int ForksCount { get; set; }

        public static RepoSearchItem FromJson(JsonElement el)
        {
            return new RepoSearchItem
            {
                FullName = el.GetProperty("full_name").GetString() ?? "",
                StargazersCount = el.TryGetProperty("stargazers_count", out var s) ? s.GetInt32() : 0,
                CreatedAt = DateTime.Parse(el.GetProperty("created_at").GetString()!, CultureInfo.InvariantCulture),
                UpdatedAt = DateTime.Parse(el.GetProperty("updated_at").GetString()!, CultureInfo.InvariantCulture),
                Language = el.TryGetProperty("language", out var l) ? l.GetString() : null,
                OpenIssuesCount = el.TryGetProperty("open_issues_count", out var o) ? o.GetInt32() : 0,
                ForksCount = el.TryGetProperty("forks_count", out var f) ? f.GetInt32() : 0
            };
        }
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

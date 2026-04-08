using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Spectre.Console;

namespace Enunciado2.Sprint1;

/// <summary>
/// LAB 02 — Qualidade de sistemas Java (CK + GitHub).
/// Sprint 1: lista dos 1.000 repositórios Java, automação de clone/CK em C# (Process),
/// CSV com medições de 1 repositório. Bônus: correlação (Pearson/Spearman), gráficos e p-valor aproximado.
/// </summary>
/// <remarks>
/// Enunciado LAB 02 — mapeamento: Lab02S01 (top-1000 Java) = BuscarRepositoriosJava* + repos_java_1000.csv/.txt.
/// Automação clone+CK = ExecutarColetaCk; lote = ExecutarColetaLote → medicoes_ck_lote.csv.
/// Coleta lote: use --lote-limpar-work se Windows negar acesso a .git/objects/pack (*.idx) — apaga lab02_sprint1_output/lote_work antes.
/// CSV amostra = AgregarMedicoesAmostra → medicoes_repositorio_amostra.csv.
/// Métricas processo: estrelas, idade_anos, releases, disk (REST), LOC/comentários na coleta CK.
/// Bônus local (um repo) = ExecutarBonus em lab02_sprint1_output/bonus/. Relatório global = Sprint 3 após Sprint 2.
/// </remarks>
static class Program
{
    private static readonly HttpClient Http = new();
    private const string GraphQLEndpoint = "https://api.github.com/graphql";
    /// <summary>Páginas pequenas reduzem 502/proxy (GraphQL pesado com 100 nós).</summary>
    private const int ReposPorPaginaGraphQl = 18;
    private const int PausaEntrePaginasGraphQlMs = 1400;
    private const int TotalRepos = 1000;

    /// <summary>Mensagem estável para o lote tratar como “ignorado”, não falha técnica.</summary>
    private const string ErroRepoSemFicheirosJava =
        "Nenhum .java encontrado no clone (repo pode ser só docs/markdown ou estrutura não standard).";

    private static string? _token;
    private static bool _rateLimitAvisado;
    private static string? _ultimoErro;

    private const string SearchQuery = """
        query($queryString: String!, $first: Int!, $after: String) {
          search(query: $queryString, type: REPOSITORY, first: $first, after: $after) {
            repositoryCount
            pageInfo { hasNextPage endCursor }
            nodes {
              ... on Repository {
                nameWithOwner
                url
                stargazerCount
                forkCount
                createdAt
                updatedAt
                issues(states: OPEN) { totalCount }
                releases { totalCount }
                primaryLanguage { name }
              }
            }
          }
        }
        """;

    static Program()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "LabEXPSoftware-Lab02-Enunciado2");
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
        Http.Timeout = TimeSpan.FromMinutes(3);
    }

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var token = ObterTokenDosArgs(args)
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? LerTokenDoArquivo();
        AplicarToken(token);

        var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var outputDir = Path.Combine(pastaProjeto, "lab02_sprint1_output");
        Directory.CreateDirectory(outputDir);

        var skipFetch = args.Contains("--skip-fetch", StringComparer.OrdinalIgnoreCase);
        var forcarRest = args.Contains("--rest", StringComparer.OrdinalIgnoreCase);
        var soColeta = args.Contains("--coleta-ck", StringComparer.OrdinalIgnoreCase);
        var soColetaLote = args.Contains("--coleta-lote", StringComparer.OrdinalIgnoreCase);
        var soAgregar = args.Contains("--agregar-ck", StringComparer.OrdinalIgnoreCase);
        var soBonus = args.Contains("--bonus", StringComparer.OrdinalIgnoreCase);

        AnsiConsole.Write(new Panel(
                new Markup("[bold]LAB 02[/] — Qualidade Java (CK)\n[dim]Enunciado 2 — Sprint 1[/]"))
            .Header("[bold yellow]☕ Java + GitHub[/]")
            .Border(BoxBorder.Double)
            .BorderStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();

        var vaiBuscarLista = !skipFetch && TemToken();
        if (!vaiBuscarLista && !skipFetch && !soAgregar && !soBonus && !soColeta && !soColetaLote)
        {
            AnsiConsole.MarkupLine("[red]Token GitHub ausente.[/] Use [cyan].github-token[/], [cyan]GITHUB_TOKEN[/] ou [cyan]--token=...[/]");
            AnsiConsole.MarkupLine("[dim]Coleta CK (sem API): [cyan]--coleta-ck --ck-jar=caminho\\ck.jar[/] | Lote: [cyan]--coleta-lote --ck-jar=...[/] | Pós: [cyan]--skip-fetch[/] + [cyan]--agregar-ck[/] / [cyan]--bonus[/][/]");
            return;
        }

        if (!skipFetch && !TemToken() && (soAgregar || soBonus) && !soColeta && !soColetaLote)
            AnsiConsole.MarkupLine("[yellow]Sem token:[/] pulando download da lista (use lista já gerada ou --skip-fetch explícito).");

        if (vaiBuscarLista)
        {
            List<RepoProcesso> repos = null!;
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync(forcarRest
                    ? "Buscando repositórios Java (API REST)..."
                    : "Buscando repositórios Java (GraphQL leve + fallback REST)...", async ctx =>
                {
                    repos = await BuscarRepositoriosJavaAsync(TotalRepos, ctx, forcarRest).ConfigureAwait(false);
                });

            if (repos.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Nenhum repositório obtido.[/]");
                if (!string.IsNullOrEmpty(_ultimoErro))
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(_ultimoErro)}[/]");
                if (!string.IsNullOrEmpty(_ultimoErro) && _ultimoErro.Contains("401", StringComparison.Ordinal))
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold]Dica (HTTP 401):[/] o GitHub rejeitou o token.");
                    AnsiConsole.MarkupLine("  • Gere um [cyan]Classic[/] PAT: Settings → Developer settings → Personal access tokens → [cyan]Tokens (classic)[/] → Generate.");
                    AnsiConsole.MarkupLine("  • Marque ao menos [green]public_repo[/] (leitura de repositórios públicos) ou [green]repo[/] para acesso amplo.");
                    AnsiConsole.MarkupLine("  • Salve [bold]só o valor[/] do token em [cyan]Enunciado 2\\Sprint 1\\.github-token[/] (uma linha, sem aspas).");
                    AnsiConsole.MarkupLine("  • Se expôs o token em algum lugar, [yellow]revogue[/] o antigo e use um novo.");
                }
                return;
            }

            EscreverListaReposCsv(repos, Path.Combine(outputDir, "repos_java_1000.csv"));
            EscreverListaReposTxt(repos, Path.Combine(outputDir, "repos_java_1000.txt"));
            AnsiConsole.MarkupLine($"[green]✔[/] {repos.Count} repositórios → [cyan]{Markup.Escape(outputDir)}[/]");
        }
        else if (skipFetch)
            AnsiConsole.MarkupLine("[yellow]--skip-fetch:[/] mantendo lista existente em lab02_sprint1_output.");

        if (soColeta)
        {
            if (!ExecutarColetaCk(outputDir, pastaProjeto, args))
                return;
        }

        if (soColetaLote)
        {
            if (!ExecutarColetaLote(outputDir, pastaProjeto, args))
                return;
        }

        if (soAgregar || (args.Contains("--bonus", StringComparer.OrdinalIgnoreCase) && File.Exists(Path.Combine(outputDir, "ck_output", "class.csv"))))
        {
            var classCsv = Path.Combine(outputDir, "ck_output", "class.csv");
            var reposCsv = Path.Combine(outputDir, "repos_java_1000.csv");
            if (File.Exists(classCsv) && File.Exists(reposCsv))
            {
                var repoLinha = ObterPrimeiroRepoOuEnv();
                AgregarMedicoesAmostra(reposCsv, classCsv, repoLinha, outputDir);
            }
            else
                AnsiConsole.MarkupLine("[yellow]Agregação:[/] faltam ck_output/class.csv ou repos_java_1000.csv — execute [cyan]dotnet run -- --coleta-ck --ck-jar=...[/] antes.");
        }

        if (args.Contains("--bonus", StringComparer.OrdinalIgnoreCase))
        {
            ExecutarBonus(outputDir);
        }

        if (!soBonus && !soAgregar && !soColeta && vaiBuscarLista)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Próximos passos (tudo em C#):[/]");
            AnsiConsole.MarkupLine("  1. Baixe o [bold]ck.jar[/]: [link]https://github.com/mauricioaniche/ck/releases[/]");
            AnsiConsole.MarkupLine("  2. [cyan]dotnet run -- --coleta-ck --ck-jar=caminho\\para\\ck.jar[/] [dim](opcional: [cyan]--repo=owner/nome[/] ou [cyan]LAB02_REPO[/])[/]");
            AnsiConsole.MarkupLine("  3. [cyan]dotnet run -- --agregar-ck --bonus[/] [dim]— consolidar CSV e gráficos/estatísticas[/]");
            AnsiConsole.MarkupLine("  4. [cyan]dotnet run -- --skip-fetch --coleta-lote --ck-jar=... --lote-max=10[/] [dim]— vários repos → medicoes_ck_lote.csv[/]");
            AnsiConsole.MarkupLine("  5. Se [red]Access denied[/] em [dim]pack-*.idx[/]: feche o Explorador nessa pasta, use [cyan]--lote-limpar-work[/] ou apague [dim]lab02_sprint1_output\\lote_work[/] manualmente.");
            AnsiConsole.MarkupLine("[dim]Se GraphQL falhar (502): use [cyan]--rest[/] para só REST ([yellow]releases=0[/] nesse modo).[/]");
        }
    }

    private static string ObterPrimeiroRepoOuEnv()
    {
        var env = Environment.GetEnvironmentVariable("LAB02_REPO");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
        var lista = Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")), "lab02_sprint1_output", "repos_java_1000.txt");
        if (File.Exists(lista))
        {
            var primeira = File.ReadLines(lista).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'));
            if (!string.IsNullOrEmpty(primeira)) return primeira.Trim();
        }
        return "google/guava";
    }

    private static void AgregarMedicoesAmostra(string reposCsv, string classCsv, string nomeRepo, string outputDir)
    {
        var linhas = File.ReadAllLines(reposCsv, Encoding.UTF8);
        if (linhas.Length < 2)
        {
            AnsiConsole.MarkupLine("[red]repos_java_1000.csv vazio ou inválido.[/]");
            return;
        }

        var header = linhas[0].Split(';');
        var idxNome = Array.IndexOf(header, "nome_completo");
        if (idxNome < 0) idxNome = 0;
        Dictionary<string, string>? processo = null;
        foreach (var linha in linhas.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;
            var cols = linha.Split(';');
            if (cols.Length <= idxNome) continue;
            if (string.Equals(cols[idxNome].Trim(), nomeRepo, StringComparison.OrdinalIgnoreCase))
            {
                processo = header.Zip(cols, (h, c) => (h, c)).ToDictionary(x => x.h, x => x.c, StringComparer.OrdinalIgnoreCase);
                break;
            }
        }

        if (processo == null)
        {
            AnsiConsole.MarkupLine($"[yellow]Repo {Markup.Escape(nomeRepo)} não encontrado na lista; use variável LAB02_REPO ou edite repos_java_1000.txt.[/]");
            return;
        }

        if (!TryParseCkClassCsv(classCsv, out var cbo, out var dit, out var lcom, out var nClasses))
        {
            AnsiConsole.MarkupLine("[red]Não foi possível ler métricas CK (class.csv).[/]");
            return;
        }

        var saida = Path.Combine(outputDir, "medicoes_repositorio_amostra.csv");
        var sb = new StringBuilder();
        sb.AppendLine(HeaderMedicoesCk);
        static string G(Dictionary<string, string> d, string k) => d.TryGetValue(k, out var v) ? v : "";

        var loc = File.Exists(Path.Combine(outputDir, "ck_output", "loc_java.txt"))
            ? File.ReadAllText(Path.Combine(outputDir, "ck_output", "loc_java.txt")).Trim()
            : "";
        var com = File.Exists(Path.Combine(outputDir, "ck_output", "loc_comentarios.txt"))
            ? File.ReadAllText(Path.Combine(outputDir, "ck_output", "loc_comentarios.txt")).Trim()
            : "";

        sb.AppendLine(string.Join(";",
            nomeRepo,
            G(processo, "estrelas"),
            G(processo, "forks"),
            G(processo, "releases"),
            G(processo, "idade_anos"),
            G(processo, "disk_usage_kb"),
            loc,
            com,
            nClasses.ToString(CultureInfo.InvariantCulture),
            Media(cbo).ToString("F4", CultureInfo.InvariantCulture),
            Mediana(cbo).ToString("F4", CultureInfo.InvariantCulture),
            DesvioPadrao(cbo).ToString("F4", CultureInfo.InvariantCulture),
            Media(dit).ToString("F4", CultureInfo.InvariantCulture),
            Mediana(dit).ToString("F4", CultureInfo.InvariantCulture),
            DesvioPadrao(dit).ToString("F4", CultureInfo.InvariantCulture),
            Media(lcom).ToString("F4", CultureInfo.InvariantCulture),
            Mediana(lcom).ToString("F4", CultureInfo.InvariantCulture),
            DesvioPadrao(lcom).ToString("F4", CultureInfo.InvariantCulture)));

        File.WriteAllText(saida, sb.ToString(), Encoding.UTF8);
        AnsiConsole.MarkupLine($"[green]✔[/] Amostra consolidada: [cyan]{Markup.Escape(saida)}[/]");
    }

    private const string HeaderMedicoesCk =
        "nome_completo;estrelas;forks;releases;idade_anos;disk_usage_kb;loc_java;comentarios_linhas;" +
        "classes_analisadas;cbo_media;cbo_mediana;cbo_desvio;dit_media;dit_mediana;dit_desvio;lcom_media;lcom_mediana;lcom_desvio";

    /// <summary>
    /// Percorre <c>repos_java_1000.txt</c> (ou <c>--lote-lista</c>), clona, roda CK e acrescenta linhas em <c>medicoes_ck_lote.csv</c> (retomável).
    /// </summary>
    private static bool ExecutarColetaLote(string outputDir, string pastaProjeto, string[] args)
    {
        var ckJar = ObterValorArg(args, "--ck-jar")
            ?? Environment.GetEnvironmentVariable("CK_JAR")?.Trim();
        if (string.IsNullOrWhiteSpace(ckJar))
            ckJar = Path.Combine(pastaProjeto, "ck.jar");
        ckJar = Path.GetFullPath(ckJar);
        if (!File.Exists(ckJar))
        {
            AnsiConsole.MarkupLine($"[red]ck.jar não encontrado:[/] {Markup.Escape(ckJar)}");
            return false;
        }

        var reposCsv = Path.Combine(outputDir, "repos_java_1000.csv");
        if (!File.Exists(reposCsv))
        {
            AnsiConsole.MarkupLine($"[red]Falta {Markup.Escape(reposCsv)}[/] — rode a Sprint 1 com token ou use lista existente.");
            return false;
        }

        var listaRepos = ObterValorArg(args, "--lote-lista")
            ?? Path.Combine(outputDir, "repos_java_1000.txt");
        listaRepos = Path.GetFullPath(listaRepos);
        if (!File.Exists(listaRepos))
        {
            AnsiConsole.MarkupLine($"[red]Lista não encontrada:[/] {Markup.Escape(listaRepos)}");
            return false;
        }

        var max = 5;
        if (int.TryParse(ObterValorArg(args, "--lote-max"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var m) && m >= 0)
            max = m;
        var offset = 0;
        if (int.TryParse(ObterValorArg(args, "--lote-offset"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var o) && o >= 0)
            offset = o;

        var continuarErro = args.Contains("--lote-continuar", StringComparer.OrdinalIgnoreCase);
        var semResume = args.Contains("--lote-sem-resume", StringComparer.OrdinalIgnoreCase);
        var manterArtefatos = args.Contains("--lote-manter-artefatos", StringComparer.OrdinalIgnoreCase);

        var loteOut = Path.Combine(outputDir, "medicoes_ck_lote.csv");
        var ja = semResume ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : CarregarNomesJaMedidosLote(loteOut);
        var nomesLista = File.ReadAllLines(listaRepos, Encoding.UTF8)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal))
            .Skip(offset)
            .ToList();
        if (max > 0)
            nomesLista = nomesLista.Take(max).ToList();

        if (nomesLista.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum repositório no intervalo (offset/max).[/]");
            return true;
        }

        var loteRoot = Path.Combine(outputDir, "lote_work");
        if (args.Contains("--lote-limpar-work", StringComparer.OrdinalIgnoreCase) && Directory.Exists(loteRoot))
        {
            AnsiConsole.MarkupLine("[cyan]--lote-limpar-work:[/] a remover pasta de trabalho do lote (clones antigos)…");
            if (!TryApagarPastaRobusta(loteRoot, out var limpaErro))
                AnsiConsole.MarkupLine($"[yellow]Aviso:[/] não foi possível apagar lote_work por completo: {Markup.Escape(limpaErro)}");
        }
        var cloneParent = Path.Combine(loteRoot, "clone");
        var ckParent = Path.Combine(loteRoot, "ck");
        Directory.CreateDirectory(cloneParent);
        Directory.CreateDirectory(ckParent);

        AnsiConsole.MarkupLine($"[cyan]Coleta em lote:[/] {nomesLista.Count} repo(s) | retomar: {(semResume ? "não" : "sim")} | saída: [cyan]{Markup.Escape(loteOut)}[/]");
        var ok = 0;
        var falha = 0;
        var ignoradosSemJava = 0;

        foreach (var nomeRepo in nomesLista)
        {
            if (!semResume && ja.Contains(nomeRepo))
            {
                AnsiConsole.MarkupLine($"[dim]Pulando (já em lote):[/] {Markup.Escape(nomeRepo)}");
                continue;
            }

            var processo = CarregarProcessoParaRepo(reposCsv, nomeRepo);
            if (processo == null)
            {
                AnsiConsole.MarkupLine($"[yellow]Repo não está em repos_java_1000.csv:[/] {Markup.Escape(nomeRepo)}");
                falha++;
                if (!continuarErro) return false;
                continue;
            }

            AnsiConsole.MarkupLine($"[bold]→[/] {Markup.Escape(nomeRepo)}");
            if (!ColetarCkUmRepoIsolado(ckJar, nomeRepo, cloneParent, ckParent, manterArtefatos, out var classCsv, out var locJ, out var locC, out var err))
            {
                if (string.Equals(err, ErroRepoSemFicheirosJava, StringComparison.Ordinal))
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Ignorado (sem código Java no clone):[/] {Markup.Escape(nomeRepo)} — típico de guias/docs; o CK não aplica.");
                    ignoradosSemJava++;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Falha:[/] {Markup.Escape(err)}");
                    falha++;
                }

                if (!continuarErro) return false;
                continue;
            }

            if (!TryParseCkClassCsv(classCsv, out var cbo, out var dit, out var lcom, out var nClasses))
            {
                AnsiConsole.MarkupLine("[red]class.csv inválido ou sem classes.[/]");
                falha++;
                if (!manterArtefatos)
                {
                    var slugLimpeza = nomeRepo.Replace('/', '_');
                    TryApagarPastaRobusta(Path.Combine(cloneParent, slugLimpeza), out _);
                    TryApagarPastaRobusta(Path.Combine(ckParent, slugLimpeza), out _);
                }

                if (!continuarErro) return false;
                continue;
            }

            static string G(Dictionary<string, string> d, string k) => d.TryGetValue(k, out var v) ? v : "";
            var linha = string.Join(";",
                nomeRepo,
                G(processo, "estrelas"),
                G(processo, "forks"),
                G(processo, "releases"),
                G(processo, "idade_anos"),
                G(processo, "disk_usage_kb"),
                locJ.ToString(CultureInfo.InvariantCulture),
                locC.ToString(CultureInfo.InvariantCulture),
                nClasses.ToString(CultureInfo.InvariantCulture),
                Media(cbo).ToString("F4", CultureInfo.InvariantCulture),
                Mediana(cbo).ToString("F4", CultureInfo.InvariantCulture),
                DesvioPadrao(cbo).ToString("F4", CultureInfo.InvariantCulture),
                Media(dit).ToString("F4", CultureInfo.InvariantCulture),
                Mediana(dit).ToString("F4", CultureInfo.InvariantCulture),
                DesvioPadrao(dit).ToString("F4", CultureInfo.InvariantCulture),
                Media(lcom).ToString("F4", CultureInfo.InvariantCulture),
                Mediana(lcom).ToString("F4", CultureInfo.InvariantCulture),
                DesvioPadrao(lcom).ToString("F4", CultureInfo.InvariantCulture));

            if (!File.Exists(loteOut))
                File.WriteAllText(loteOut, HeaderMedicoesCk + Environment.NewLine, Encoding.UTF8);
            File.AppendAllText(loteOut, linha + Environment.NewLine, Encoding.UTF8);
            ja.Add(nomeRepo);
            ok++;
            AnsiConsole.MarkupLine($"[green]✔[/] Medição gravada ({ok}/{nomesLista.Count})");
            if (!manterArtefatos)
            {
                var slugOk = nomeRepo.Replace('/', '_');
                TryApagarPastaRobusta(Path.Combine(cloneParent, slugOk), out _);
                TryApagarPastaRobusta(Path.Combine(ckParent, slugOk), out _);
            }
        }

        AnsiConsole.MarkupLine(
            $"[green]Lote concluído.[/] Sucesso: {ok} | falhas: {falha} | ignorados (sem .java): {ignoradosSemJava} → [cyan]{Markup.Escape(loteOut)}[/]");
        AnsiConsole.MarkupLine("[dim]Sprint 2:[/] [cyan]dotnet run[/] na pasta Sprint 2 para fundir no consolidado.");
        return true;
    }

    private static HashSet<string> CarregarNomesJaMedidosLote(string path)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return set;
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var c = lines[i].Split(';');
            if (c.Length > 0 && !string.IsNullOrWhiteSpace(c[0]))
                set.Add(c[0].Trim());
        }

        return set;
    }

    private static Dictionary<string, string>? CarregarProcessoParaRepo(string reposCsv, string nomeRepo)
    {
        var linhas = File.ReadAllLines(reposCsv, Encoding.UTF8);
        if (linhas.Length < 2) return null;
        var header = linhas[0].Split(';');
        var idxNome = Array.IndexOf(header, "nome_completo");
        if (idxNome < 0) idxNome = 0;
        foreach (var linha in linhas.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;
            var cols = linha.Split(';');
            if (cols.Length <= idxNome) continue;
            if (!string.Equals(cols[idxNome].Trim(), nomeRepo, StringComparison.OrdinalIgnoreCase))
                continue;
            return header.Zip(cols, (h, c) => (h, c)).ToDictionary(x => x.h, x => x.c, StringComparer.OrdinalIgnoreCase);
        }

        return null;
    }

    private static bool ColetarCkUmRepoIsolado(
        string ckJar,
        string nomeRepo,
        string cloneParent,
        string ckParent,
        bool manterArtefatos,
        out string classCsvPath,
        out int locJava,
        out int locComentario,
        out string erro)
    {
        classCsvPath = "";
        locJava = 0;
        locComentario = 0;
        erro = "";
        var slug = nomeRepo.Replace('/', '_');
        var target = Path.Combine(cloneParent, slug);
        if (Directory.Exists(target) && !TryApagarPastaRobusta(target, out erro))
            return false;

        var url = $"https://github.com/{nomeRepo}.git";
        if (!ExecutarProcessoExito("git", $"clone --depth 1 \"{url}\" \"{target}\"", cloneParent, out erro))
            return false;

        var srcRoot = EncontrarRaizFontesJava(target);
        if (ContarFicheirosJava(srcRoot) == 0)
        {
            erro = ErroRepoSemFicheirosJava;
            TryApagarPastaRobusta(target, out _);
            return false;
        }

        ContarLocJava(srcRoot, out locJava, out locComentario);

        var ckDir = Path.Combine(ckParent, slug);
        if (Directory.Exists(ckDir) && !TryApagarPastaRobusta(ckDir, out erro))
            return false;

        Directory.CreateDirectory(ckDir);
        try
        {
            foreach (var f in Directory.GetFiles(ckDir, "*.csv"))
                File.Delete(f);
        }
        catch { /* ignore */ }

        var javaArgs = $"-jar \"{ckJar}\" \"{srcRoot}\" false 0";
        if (!ExecutarProcessoExito("java", javaArgs, ckDir, out erro))
        {
            if (!manterArtefatos)
            {
                TryApagarPastaRobusta(target, out _);
                TryApagarPastaRobusta(ckDir, out _);
            }

            return false;
        }

        classCsvPath = Path.Combine(ckDir, "class.csv");
        if (!File.Exists(classCsvPath))
        {
            erro = "class.csv não gerado pelo CK.";
            if (!manterArtefatos)
            {
                TryApagarPastaRobusta(target, out _);
                TryApagarPastaRobusta(ckDir, out _);
            }

            return false;
        }

        // Não apagar clone/ck aqui: o lote ainda precisa de ler class.csv (TryParseCkClassCsv).
        // Limpeza em ExecutarColetaLote após gravar a linha no CSV.
        return true;
    }

    private static bool TryParseCkClassCsv(string path, out List<double> cbo, out List<double> dit, out List<double> lcom, out int nClasses)
    {
        cbo = new List<double>();
        dit = new List<double>();
        lcom = new List<double>();
        nClasses = 0;
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2) return false;
        lines[0] = lines[0].TrimStart('\uFEFF');
        var h = lines[0].Split(',');
        int Ic(string name)
        {
            for (var i = 0; i < h.Length; i++)
                if (string.Equals(h[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        var iCbo = Ic("cbo");
        var iDit = Ic("dit");
        var iLcom = Ic("lcom");
        if (iLcom < 0)
            iLcom = Ic("lcom*");
        if (iCbo < 0 || iDit < 0 || iLcom < 0) return false;

        for (var r = 1; r < lines.Length; r++)
        {
            if (string.IsNullOrWhiteSpace(lines[r])) continue;
            var cols = SplitCsvLine(lines[r]);
            if (cols.Length <= Math.Max(iCbo, Math.Max(iDit, iLcom))) continue;
            if (double.TryParse(cols[iCbo], NumberStyles.Float, CultureInfo.InvariantCulture, out var vCbo)
                && double.TryParse(cols[iDit], NumberStyles.Float, CultureInfo.InvariantCulture, out var vDit)
                && double.TryParse(cols[iLcom], NumberStyles.Float, CultureInfo.InvariantCulture, out var vLcom))
            {
                cbo.Add(vCbo);
                dit.Add(vDit);
                lcom.Add(vLcom);
                nClasses++;
            }
        }
        return nClasses > 0;
    }

    private static string[] SplitCsvLine(string line)
    {
        var list = new List<string>();
        var cur = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }
            if (!quoted && c == ',')
            {
                list.Add(cur.ToString().Trim());
                cur.Clear();
                continue;
            }
            cur.Append(c);
        }
        list.Add(cur.ToString().Trim());
        return list.ToArray();
    }

    private static void ExecutarBonus(string outputDir)
    {
        var bonusDir = Path.Combine(outputDir, "bonus");
        Directory.CreateDirectory(bonusDir);
        var reposCsv = Path.Combine(outputDir, "repos_java_1000.csv");
        var classCsv = Path.Combine(outputDir, "ck_output", "class.csv");

        if (File.Exists(reposCsv))
        {
            if (TryCarregarMetricasProcesso(reposCsv, out var estrelas, out var idade, out var releases, out var disk))
            {
                var pares = new[]
                {
                    ("estrelas", "idade_anos", estrelas, idade),
                    ("estrelas", "releases", estrelas, releases),
                    ("estrelas", "disk_usage_kb", estrelas, disk),
                    ("idade_anos", "releases", idade, releases)
                };
                var relProc = new StringBuilder();
                relProc.AppendLine("par;pearson_r;pearson_p_aprox;spearman_rho;spearman_p_aprox;n");
                foreach (var (a, b, xa, xb) in pares)
                {
                    if (xa.Count != xb.Count || xa.Count < 4) continue;
                    var ra = xa.ToArray();
                    var rb = xb.ToArray();
                    var pr = Correlation.Pearson(ra, rb);
                    var sr = Spearman(ra, rb);
                    relProc.AppendLine($"{a}_vs_{b};{pr.ToString("F4", CultureInfo.InvariantCulture)};{PValorPearson(pr, ra.Length).ToString("E3", CultureInfo.InvariantCulture)};{sr.ToString("F4", CultureInfo.InvariantCulture)};{PValorSpearman(sr, ra.Length).ToString("E3", CultureInfo.InvariantCulture)};{ra.Length}");
                }
                File.WriteAllText(Path.Combine(bonusDir, "correlacao_metricas_processo.csv"), relProc.ToString(), Encoding.UTF8);

                try
                {
                    var plt = new ScottPlot.Plot();
                    plt.Add.Scatter(estrelas.ToArray(), idade.ToArray());
                    plt.XLabel("Estrelas");
                    plt.YLabel("Idade (anos)");
                    plt.Title("Popularidade vs maturidade (1000 repos Java)");
                    plt.SavePng(Path.Combine(bonusDir, "scatter_estrelas_idade.png"), 900, 600);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Gráfico processo 1:[/] {Markup.Escape(ex.Message)}");
                }

                try
                {
                    var plt2 = new ScottPlot.Plot();
                    plt2.Add.Scatter(estrelas.ToArray(), releases.ToArray());
                    plt2.XLabel("Estrelas");
                    plt2.YLabel("Releases");
                    plt2.Title("Popularidade vs atividade (releases)");
                    plt2.SavePng(Path.Combine(bonusDir, "scatter_estrelas_releases.png"), 900, 600);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Gráfico processo 2:[/] {Markup.Escape(ex.Message)}");
                }
            }
        }

        if (File.Exists(classCsv) && TryParseCkClassCsv(classCsv, out var cbo, out var dit, out var lcom, out _))
        {
            var n = cbo.Count;
            if (n >= 4)
            {
                var relQ = new StringBuilder();
                relQ.AppendLine("par;pearson_r;pearson_p_aprox;spearman_rho;spearman_p_aprox;n");
                void Linha(string nome, double[] a, double[] b)
                {
                    var pr = Correlation.Pearson(a, b);
                    var sr = Spearman(a, b);
                    relQ.AppendLine($"{nome};{pr.ToString("F4", CultureInfo.InvariantCulture)};{PValorPearson(pr, a.Length).ToString("E3", CultureInfo.InvariantCulture)};{sr.ToString("F4", CultureInfo.InvariantCulture)};{PValorSpearman(sr, a.Length).ToString("E3", CultureInfo.InvariantCulture)};{a.Length}");
                }
                Linha("cbo_vs_dit", cbo.ToArray(), dit.ToArray());
                Linha("cbo_vs_lcom", cbo.ToArray(), lcom.ToArray());
                Linha("dit_vs_lcom", dit.ToArray(), lcom.ToArray());
                File.WriteAllText(Path.Combine(bonusDir, "correlacao_metricas_ck_classes.csv"), relQ.ToString(), Encoding.UTF8);

                try
                {
                    var p = new ScottPlot.Plot();
                    p.Add.Scatter(cbo.ToArray(), dit.ToArray());
                    p.XLabel("CBO");
                    p.YLabel("DIT");
                    p.Title("Correlação entre métricas CK (por classe)");
                    p.SavePng(Path.Combine(bonusDir, "scatter_cbo_dit.png"), 900, 600);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Gráfico CK:[/] {Markup.Escape(ex.Message)}");
                }

                try
                {
                    var p2 = new ScottPlot.Plot();
                    p2.Add.Scatter(cbo.ToArray(), lcom.ToArray());
                    p2.XLabel("CBO");
                    p2.YLabel("LCOM");
                    p2.Title("CBO vs LCOM (por classe)");
                    p2.SavePng(Path.Combine(bonusDir, "scatter_cbo_lcom.png"), 900, 600);
                }
                catch { /* ignore */ }
            }
        }

        AnsiConsole.MarkupLine($"[green]Bônus:[/] arquivos em [cyan]{Markup.Escape(bonusDir)}[/]");
    }

    private static bool TryCarregarMetricasProcesso(string reposCsv, out List<double> estrelas, out List<double> idade, out List<double> releases, out List<double> disk)
    {
        estrelas = new List<double>();
        idade = new List<double>();
        releases = new List<double>();
        disk = new List<double>();
        var lines = File.ReadAllLines(reposCsv, Encoding.UTF8);
        if (lines.Length < 2) return false;
        var h = lines[0].Split(';');
        int I(string name) { for (var i = 0; i < h.Length; i++) if (string.Equals(h[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i; return -1; }
        var ie = I("estrelas");
        var ii = I("idade_anos");
        var ir = I("releases");
        var id = I("disk_usage_kb");
        if (ie < 0 || ii < 0) return false;
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(';');
            if (c.Length <= Math.Max(ie, ii)) continue;
            if (!double.TryParse(c[ie], NumberStyles.Float, CultureInfo.InvariantCulture, out var e)) continue;
            if (!double.TryParse(c[ii], NumberStyles.Float, CultureInfo.InvariantCulture, out var ag)) continue;
            estrelas.Add(e);
            idade.Add(ag);
            releases.Add(ir >= 0 && ir < c.Length && double.TryParse(c[ir], NumberStyles.Float, CultureInfo.InvariantCulture, out var rel) ? rel : 0);
            disk.Add(id >= 0 && id < c.Length && double.TryParse(c[id], NumberStyles.Float, CultureInfo.InvariantCulture, out var dk) ? dk : 0);
        }
        return estrelas.Count >= 4;
    }

    private static double Spearman(double[] x, double[] y)
    {
        var n = x.Length;
        if (n != y.Length || n < 2) return double.NaN;
        var rx = Rank(x);
        var ry = Rank(y);
        return Correlation.Pearson(rx, ry);
    }

    private static double[] Rank(double[] values)
    {
        var n = values.Length;
        var indexed = values.Select((v, i) => (v, i)).OrderBy(t => t.v).ToArray();
        var ranks = new double[n];
        for (var k = 0; k < n;)
        {
            var j = k;
            while (j < n && indexed[j].v == indexed[k].v) j++;
            // posições 0..n-1 → ranks 1-based; empates recebem a média dos ranks do grupo
            var avgRank = (k + 1 + j) / 2.0;
            for (var t = k; t < j; t++)
                ranks[indexed[t].i] = avgRank;
            k = j;
        }
        return ranks;
    }

    /// <summary>Teste t em r de Pearson (H0: rho=0), bilateral, aproximação clássica.</summary>
    private static double PValorPearson(double r, int n)
    {
        if (n <= 3 || double.IsNaN(r)) return double.NaN;
        var df = n - 2;
        var den = Math.Sqrt(Math.Max(1e-15, 1 - r * r));
        var tStat = Math.Abs(r) * Math.Sqrt(df) / den;
        var t = new StudentT(0, 1, df);
        var cdf = t.CumulativeDistribution(tStat);
        return 2 * Math.Min(cdf, 1 - cdf);
    }

    /// <summary>p-valor aproximado para Spearman via t com n-2 g.l. (uso comum em laboratórios).</summary>
    private static double PValorSpearman(double rho, int n)
    {
        if (n <= 3 || double.IsNaN(rho)) return double.NaN;
        var df = n - 2;
        var den = Math.Sqrt(Math.Max(1e-15, 1 - rho * rho));
        var tStat = Math.Abs(rho) * Math.Sqrt(df) / den;
        var t = new StudentT(0, 1, df);
        var cdf = t.CumulativeDistribution(tStat);
        return 2 * Math.Min(cdf, 1 - cdf);
    }

    private static void EscreverListaReposCsv(List<RepoProcesso> repos, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("nome_completo;url;estrelas;forks;releases;open_issues;disk_usage_kb;criado_em;atualizado_em;idade_anos;linguagem_primaria");
        var agora = DateTime.UtcNow;
        foreach (var r in repos)
        {
            var idade = (agora - r.CriadoEm).TotalDays / 365.25;
            sb.AppendLine(string.Join(";",
                r.Nome,
                r.Url,
                r.Estrelas.ToString(CultureInfo.InvariantCulture),
                r.Forks.ToString(CultureInfo.InvariantCulture),
                r.Releases.ToString(CultureInfo.InvariantCulture),
                r.OpenIssues.ToString(CultureInfo.InvariantCulture),
                r.DiskUsageKb?.ToString(CultureInfo.InvariantCulture) ?? "",
                r.CriadoEm.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.AtualizadoEm.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                idade.ToString("F4", CultureInfo.InvariantCulture),
                CsvEscape(r.Linguagem)));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string CsvEscape(string s)
    {
        if (s.Contains(';') || s.Contains('"'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static void EscreverListaReposTxt(List<RepoProcesso> repos, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Um repositório por linha (owner/repo).");
        foreach (var r in repos)
            sb.AppendLine(r.Nome);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static async Task<List<RepoProcesso>> BuscarRepositoriosJavaAsync(int total, StatusContext ctx, bool forcarRest)
    {
        if (!forcarRest)
        {
            var viaGql = await BuscarRepositoriosJavaGraphQlAsync(total, ctx).ConfigureAwait(false);
            if (viaGql.Count > 0)
                return viaGql;
            AnsiConsole.MarkupLine("[yellow]GraphQL não retornou dados (ex.: 502/proxy). Usando API REST…[/]");
            AnsiConsole.MarkupLine("[dim]Neste modo [bold]releases = 0[/] (a busca REST não expõe o total de releases).[/]");
        }
        return await BuscarRepositoriosJavaRestAsync(total, ctx).ConfigureAwait(false);
    }

    private static async Task<List<RepoProcesso>> BuscarRepositoriosJavaGraphQlAsync(int total, StatusContext ctx)
    {
        var lista = new List<RepoProcesso>();
        string? cursor = null;
        const string q = "language:Java sort:stars-desc";

        while (lista.Count < total)
        {
            var porPagina = Math.Min(ReposPorPaginaGraphQl, total - lista.Count);
            var variables = new Dictionary<string, object?>
            {
                ["queryString"] = q,
                ["first"] = porPagina,
                ["after"] = cursor
            };

            var json = await PostGraphQLAsync(SearchQuery, variables).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json))
                break;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                break;
            var search = data.GetProperty("search");
            var nodes = search.GetProperty("nodes");

            foreach (var node in nodes.EnumerateArray())
            {
                if (node.ValueKind == JsonValueKind.Null) continue;
                lista.Add(new RepoProcesso
                {
                    Nome = node.GetProperty("nameWithOwner").GetString() ?? "",
                    Url = node.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                    Estrelas = node.GetProperty("stargazerCount").GetInt32(),
                    Forks = node.GetProperty("forkCount").GetInt32(),
                    CriadoEm = DateTime.Parse(node.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    AtualizadoEm = DateTime.Parse(node.GetProperty("updatedAt").GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    OpenIssues = node.GetProperty("issues").GetProperty("totalCount").GetInt32(),
                    Releases = node.GetProperty("releases").GetProperty("totalCount").GetInt32(),
                    DiskUsageKb = null,
                    Linguagem = node.TryGetProperty("primaryLanguage", out var lang) && lang.ValueKind != JsonValueKind.Null
                        ? lang.GetProperty("name").GetString() ?? "Java"
                        : "Java"
                });
                ctx.Status($"GraphQL {lista.Count}/{total}: {lista[^1].Nome}");
                if (lista.Count >= total) break;
            }

            var pageInfo = search.GetProperty("pageInfo");
            if (!pageInfo.GetProperty("hasNextPage").GetBoolean())
                break;
            if (lista.Count >= total)
                break;
            cursor = pageInfo.GetProperty("endCursor").GetString();
            await Task.Delay(PausaEntrePaginasGraphQlMs).ConfigureAwait(false);
        }

        return lista.Take(total).ToList();
    }

    /// <summary>Fallback: até 1000 resultados (limite GitHub), páginas de 100, menos pesado que GraphQL para alguns proxies.</summary>
    private static async Task<List<RepoProcesso>> BuscarRepositoriosJavaRestAsync(int total, StatusContext ctx)
    {
        const int perPage = 100;
        var lista = new List<RepoProcesso>();
        var maxPages = Math.Min(10, (int)Math.Ceiling(Math.Min(total, 1000) / (double)perPage));

        for (var page = 1; page <= maxPages && lista.Count < total; page++)
        {
            var q = Uri.EscapeDataString("language:Java");
            var url = $"https://api.github.com/search/repositories?q={q}&sort=stars&order=desc&per_page={perPage}&page={page}";
            var json = await GetGitHubJsonAsync(url).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json))
                break;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items))
                break;

            var n = 0;
            foreach (var item in items.EnumerateArray())
            {
                n++;
                var nome = item.TryGetProperty("full_name", out var fn) ? fn.GetString() ?? "" : "";
                var urlRepo = item.TryGetProperty("html_url", out var hu) ? hu.GetString() ?? "" : "";
                var stars = item.TryGetProperty("stargazers_count", out var sc) && sc.ValueKind == JsonValueKind.Number ? sc.GetInt32() : 0;
                var forks = item.TryGetProperty("forks_count", out var fc) && fc.ValueKind == JsonValueKind.Number ? fc.GetInt32() : 0;
                var issues = item.TryGetProperty("open_issues_count", out var ic) && ic.ValueKind == JsonValueKind.Number ? ic.GetInt32() : 0;
                var criado = item.TryGetProperty("created_at", out var cr) && cr.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(cr.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                    : default;
                var atual = item.TryGetProperty("updated_at", out var up) && up.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(up.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                    : default;
                int? sizeKb = null;
                if (item.TryGetProperty("size", out var sz) && sz.ValueKind == JsonValueKind.Number)
                    sizeKb = sz.GetInt32();
                var lang = item.TryGetProperty("language", out var lg) && lg.ValueKind == JsonValueKind.String
                    ? lg.GetString() ?? "Java"
                    : "Java";

                lista.Add(new RepoProcesso
                {
                    Nome = nome,
                    Url = urlRepo,
                    Estrelas = stars,
                    Forks = forks,
                    CriadoEm = criado,
                    AtualizadoEm = atual,
                    OpenIssues = issues,
                    Releases = 0,
                    DiskUsageKb = sizeKb,
                    Linguagem = lang
                });
                ctx.Status($"REST p.{page} ({n}) — {lista.Count}/{total}: {nome}");
                if (lista.Count >= total) break;
            }

            if (items.GetArrayLength() < perPage)
                break;
            if (lista.Count >= total)
                break;
            await Task.Delay(1600).ConfigureAwait(false);
        }

        return lista.Take(total).ToList();
    }

    private static async Task<string?> GetGitHubJsonAsync(string requestUri)
    {
        const int maxTentativas = 6;
        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                req.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
                req.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
                var res = await Http.SendAsync(req).ConfigureAwait(false);
                var code = (int)res.StatusCode;

                if (code == 401)
                {
                    _ultimoErro = "Token inválido ou expirado (HTTP 401).";
                    return null;
                }

                if (code == 403)
                {
                    var resetUnix = res.Headers.TryGetValues("X-RateLimit-Reset", out var vals) && long.TryParse(vals.FirstOrDefault(), out var t) ? t : 0L;
                    var segundos = resetUnix > 0 ? (int)(resetUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 60;
                    segundos = Math.Clamp(segundos, 1, 3600);
                    _ultimoErro = $"REST rate limit. Aguardando {segundos}s...";
                    await Task.Delay(TimeSpan.FromSeconds(segundos)).ConfigureAwait(false);
                    continue;
                }

                if (!res.IsSuccessStatusCode)
                {
                    var err = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (EhHttpTransiente(code) && tentativa < maxTentativas)
                    {
                        var espera = Math.Min(45, (int)Math.Pow(2, tentativa));
                        _ultimoErro = $"REST HTTP {code}. Nova tentativa em {espera}s…";
                        await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
                        continue;
                    }
                    _ultimoErro = $"REST HTTP {code}: " + (err.Length > 160 ? err[..160] + "…" : err);
                    return null;
                }

                return await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (tentativa < maxTentativas)
            {
                var espera = Math.Min(45, (int)Math.Pow(2, tentativa));
                _ultimoErro = $"REST rede: {ex.Message}. Retry em {espera}s…";
                await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (tentativa < maxTentativas)
            {
                var espera = Math.Min(45, (int)Math.Pow(2, tentativa));
                await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ultimoErro = ex.Message;
                return null;
            }
        }
        return null;
    }

    private static bool EhHttpTransiente(int statusCode) =>
        statusCode is 408 or 429 or 500 or 502 or 503 or 504;

    private static async Task<string?> PostGraphQLAsync(string query, Dictionary<string, object?> variables)
    {
        const int maxTentativas = 6;
        var body = JsonSerializer.Serialize(new { query, variables });

        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var res = await Http.PostAsync(GraphQLEndpoint, content).ConfigureAwait(false);
                var code = (int)res.StatusCode;

                if (code == 401)
                {
                    _ultimoErro = "Token inválido ou expirado (HTTP 401).";
                    return null;
                }

                if (code == 403)
                {
                    if (!_rateLimitAvisado)
                    {
                        _rateLimitAvisado = true;
                        var resetUnix = res.Headers.TryGetValues("X-RateLimit-Reset", out var vals) && long.TryParse(vals.FirstOrDefault(), out var t) ? t : 0L;
                        var segundos = resetUnix > 0 ? (int)(resetUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 60;
                        segundos = Math.Clamp(segundos, 1, 3600);
                        _ultimoErro = $"Rate limit. Aguardando {segundos}s...";
                        await Task.Delay(TimeSpan.FromSeconds(segundos)).ConfigureAwait(false);
                        res = await Http.PostAsync(GraphQLEndpoint, new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                        code = (int)res.StatusCode;
                        if (code == 403)
                        {
                            _ultimoErro = "Rate limit persistente.";
                            return null;
                        }
                        _ultimoErro = null;
                    }
                    else
                    {
                        _ultimoErro = "HTTP 403.";
                        return null;
                    }
                }

                if (!res.IsSuccessStatusCode)
                {
                    var errSnippet = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (EhHttpTransiente(code) && tentativa < maxTentativas)
                    {
                        var espera = Math.Min(45, (int)Math.Pow(2, tentativa));
                        _ultimoErro = $"HTTP {code} (falha temporária). Nova tentativa em {espera}s ({tentativa}/{maxTentativas})...";
                        await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
                        continue;
                    }

                    _ultimoErro = code is 502 or 503 or 504
                        ? "HTTP " + code + ": Bad Gateway / serviço indisponível — costuma ser temporário (GitHub, rede, VPN ou proxy). Espere alguns minutos e execute de novo."
                        : $"HTTP {code}: " + (errSnippet.Length > 200 ? errSnippet[..200] + "…" : errSnippet);
                    return null;
                }

                var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("errors", out var errors))
                    {
                        var msgs = errors.EnumerateArray().Select(e => e.GetProperty("message").GetString() ?? "?").ToList();
                        _ultimoErro = string.Join("; ", msgs);
                        if (!doc.RootElement.TryGetProperty("data", out _))
                            return null;
                    }
                }

                return json;
            }
            catch (HttpRequestException ex) when (tentativa < maxTentativas)
            {
                var espera = Math.Min(45, (int)Math.Pow(2, tentativa));
                _ultimoErro = $"Rede: {ex.Message}. Tentando de novo em {espera}s ({tentativa}/{maxTentativas})...";
                await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (tentativa < maxTentativas)
            {
                var espera = Math.Min(45, (int)Math.Pow(2, tentativa));
                _ultimoErro = $"Tempo esgotado na requisição. Nova tentativa em {espera}s ({tentativa}/{maxTentativas})...";
                await Task.Delay(TimeSpan.FromSeconds(espera)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ultimoErro = ex.Message;
                return null;
            }
        }

        _ultimoErro ??= "Falha após várias tentativas. Verifique a conexão e tente mais tarde.";
        return null;
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
        foreach (var dir in new[] { pastaProjeto, baseDir, Directory.GetCurrentDirectory() })
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var arquivo = Path.Combine(dir, ".github-token");
            if (!File.Exists(arquivo)) continue;
            try
            {
                var raw = File.ReadAllText(arquivo).Replace("\uFEFF", string.Empty);
                var linha = raw
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal));
                if (string.IsNullOrWhiteSpace(linha)) continue;
                linha = linha.Trim();
                if (linha.Length >= 2 && linha[0] == '"' && linha[^1] == '"')
                    linha = linha[1..^1].Trim();
                if (linha.Length >= 2 && linha[0] == '\'' && linha[^1] == '\'')
                    linha = linha[1..^1].Trim();
                if (!string.IsNullOrWhiteSpace(linha)) return linha;
            }
            catch { /* ignore */ }
        }
        return null;
    }

    /// <summary>Clone shallow, LOC aproximado e execução do CK via git/java no PATH.</summary>
    private static bool ExecutarColetaCk(string outputDir, string pastaProjeto, string[] args)
    {
        var ckJar = ObterValorArg(args, "--ck-jar")
            ?? Environment.GetEnvironmentVariable("CK_JAR")?.Trim();
        if (string.IsNullOrWhiteSpace(ckJar))
            ckJar = Path.Combine(pastaProjeto, "ck.jar");
        ckJar = Path.GetFullPath(ckJar);
        if (!File.Exists(ckJar))
        {
            AnsiConsole.MarkupLine($"[red]ck.jar não encontrado:[/] {Markup.Escape(ckJar)}");
            AnsiConsole.MarkupLine("[dim]Use [cyan]--ck-jar=caminho[/], variável [cyan]CK_JAR[/] ou coloque ck.jar na pasta do projeto.[/]");
            return false;
        }

        var repo = ObterValorArg(args, "--repo")?.Trim()
            ?? Environment.GetEnvironmentVariable("LAB02_REPO")?.Trim();
        if (string.IsNullOrWhiteSpace(repo))
            repo = ObterPrimeiroRepoOuEnv();

        var ckOut = Path.Combine(outputDir, "ck_output");
        var cloneDir = Path.Combine(outputDir, "repo_clone");
        Directory.CreateDirectory(ckOut);
        Directory.CreateDirectory(cloneDir);

        var slug = repo.Replace('/', '_');
        var target = Path.Combine(cloneDir, slug);
        if (Directory.Exists(target))
        {
            AnsiConsole.MarkupLine($"[yellow]Removendo clone anterior:[/] {Markup.Escape(target)}");
            try
            {
                Directory.Delete(target, true);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                return false;
            }
        }

        var url = $"https://github.com/{repo}.git";
        AnsiConsole.MarkupLine($"[cyan]git clone[/] (depth 1) {Markup.Escape(url)}");
        var gitArgs = $"clone --depth 1 \"{url}\" \"{target}\"";
        if (!ExecutarProcessoExito("git", gitArgs, cloneDir, out var gitErr))
        {
            AnsiConsole.MarkupLine($"[red]git falhou:[/] {Markup.Escape(gitErr)}");
            return false;
        }

        var srcRoot = EncontrarRaizFontesJava(target);
        AnsiConsole.MarkupLine($"[dim]Raiz fontes Java:[/] {Markup.Escape(srcRoot)}");

        ContarLocJava(srcRoot, out var locJ, out var locC);
        File.WriteAllText(Path.Combine(ckOut, "loc_java.txt"), locJ.ToString(CultureInfo.InvariantCulture), Encoding.UTF8);
        File.WriteAllText(Path.Combine(ckOut, "loc_comentarios.txt"), locC.ToString(CultureInfo.InvariantCulture), Encoding.UTF8);
        AnsiConsole.MarkupLine($"[green]LOC Java (aprox.):[/] {locJ} | [green]comentários (aprox.):[/] {locC}");

        try
        {
            foreach (var f in Directory.GetFiles(ckOut, "*.csv"))
                File.Delete(f);
        }
        catch { /* ignore */ }

        AnsiConsole.MarkupLine($"[cyan]java -jar CK[/] em [dim]{Markup.Escape(srcRoot)}[/]");
        var javaArgs = $"-jar \"{ckJar}\" \"{srcRoot}\" false 0";
        if (!ExecutarProcessoExito("java", javaArgs, ckOut, out var javaErr))
        {
            AnsiConsole.MarkupLine($"[red]CK/Java falhou:[/] {Markup.Escape(javaErr)}");
            return false;
        }

        AnsiConsole.MarkupLine($"[green]✔[/] CSVs do CK em [cyan]{Markup.Escape(ckOut)}[/]");
        AnsiConsole.MarkupLine("[dim]Depois:[/] [cyan]dotnet run -- --agregar-ck --bonus[/]");
        return true;
    }

    /// <summary>Apaga pasta com retentativas e <c>attrib/rd</c> no Windows (ficheiros .git pack *.idx bloqueados por AV/Explorador).</summary>
    private static bool TryApagarPastaRobusta(string caminho, out string mensagemErro)
    {
        mensagemErro = "";
        if (string.IsNullOrWhiteSpace(caminho) || !Directory.Exists(caminho))
            return true;

        var completo = Path.GetFullPath(caminho);
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            try
            {
                if (OperatingSystem.IsWindows() && tentativa == 0)
                    ExecutarProcessoExito("cmd.exe", $"/c attrib -R \"{completo}\\*\" /S /D", Environment.SystemDirectory, out _);

                Directory.Delete(completo, true);
                if (!Directory.Exists(completo))
                    return true;
            }
            catch (Exception ex)
            {
                mensagemErro = ex.Message;
            }

            if (tentativa < 4)
                Thread.Sleep(2000);
        }

        if (OperatingSystem.IsWindows())
        {
            ExecutarProcessoExito("cmd.exe", $"/c rd /s /q \"{completo}\"", Environment.SystemDirectory, out var rdLog);
            if (!Directory.Exists(completo))
                return true;
            if (!string.IsNullOrWhiteSpace(rdLog))
                mensagemErro = rdLog.Trim();
        }

        return !Directory.Exists(completo);
    }

    private static bool ExecutarProcessoExito(string fileName, string arguments, string workingDirectory, out string combinedLog)
    {
        combinedLog = "";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = new Process { StartInfo = psi };
            p.Start();
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            combinedLog = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr + stdout;
            return p.ExitCode == 0;
        }
        catch (Exception ex)
        {
            combinedLog = ex.Message;
            return false;
        }
    }

    private static int ContarFicheirosJava(string dir)
    {
        if (!Directory.Exists(dir)) return 0;
        try
        {
            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };
            return Directory.GetFiles(dir, "*.java", opts).Length;
        }
        catch
        {
            return ContarFicheirosJavaPorPastas(dir);
        }
    }

    /// <summary>Contagem resiliente quando <see cref="Directory.GetFiles"/> falha (ex.: algumas pastas inacessíveis).</summary>
    private static int ContarFicheirosJavaPorPastas(string dir)
    {
        var n = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(dir, "*.java", SearchOption.TopDirectoryOnly))
                n++;
        }
        catch
        {
            /* ignora esta pasta */
        }

        try
        {
            foreach (var sub in Directory.EnumerateDirectories(dir))
                n += ContarFicheirosJavaPorPastas(sub);
        }
        catch
        {
            /* ignora */
        }

        return n;
    }

    /// <summary>Escolhe a pasta com mais ficheiros .java (repos “docs” no topo costumam ter pouco código em <c>src</c>).</summary>
    private static string EncontrarRaizFontesJava(string cloneRoot)
    {
        if (!Directory.Exists(cloneRoot))
            return Path.GetFullPath(cloneRoot);

        var melhor = cloneRoot;
        var max = ContarFicheirosJava(cloneRoot);

        void TentarMelhor(string rel)
        {
            var p = Path.Combine(cloneRoot, rel);
            if (!Directory.Exists(p)) return;
            var n = ContarFicheirosJava(p);
            if (n > max)
            {
                max = n;
                melhor = p;
            }
        }

        TentarMelhor(Path.Combine("src", "main", "java"));
        TentarMelhor("src");
        TentarMelhor("java");
        TentarMelhor("lib");
        TentarMelhor("code");

        try
        {
            foreach (var sub in Directory.GetDirectories(cloneRoot))
            {
                var n = ContarFicheirosJava(sub);
                if (n > max)
                {
                    max = n;
                    melhor = sub;
                }
            }
        }
        catch
        {
            // ignorar
        }

        return Path.GetFullPath(melhor);
    }

    private static void ContarLocJava(string srcRoot, out int locJava, out int locComentario)
    {
        locJava = 0;
        locComentario = 0;
        if (!Directory.Exists(srcRoot)) return;
        string[] files;
        try
        {
            files = Directory.GetFiles(srcRoot, "*.java", SearchOption.AllDirectories);
        }
        catch
        {
            return;
        }

        foreach (var file in files)
        {
            try
            {
                foreach (var line in File.ReadLines(file))
                {
                    var t = line.Trim();
                    if (t.Length == 0) continue;
                    if (t.StartsWith("//", StringComparison.Ordinal)
                        || t.StartsWith("/*", StringComparison.Ordinal)
                        || t.StartsWith('*')
                        || t.EndsWith("*/", StringComparison.Ordinal))
                        locComentario++;
                    else
                        locJava++;
                }
            }
            catch
            {
                // ignora arquivo
            }
        }
    }

    private static string? ObterValorArg(string[] args, string flagNome)
    {
        var eq = flagNome + "=";
        foreach (var a in args)
        {
            if (a.StartsWith(eq, StringComparison.OrdinalIgnoreCase))
                return a[eq.Length..].Trim().Trim('"');
        }
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flagNome, StringComparison.OrdinalIgnoreCase))
                return args[i + 1].Trim().Trim('"');
        }
        return null;
    }

    private static double Media(List<double> v) => v.Count == 0 ? 0 : v.Average();
    private static double Mediana(List<double> v)
    {
        if (v.Count == 0) return 0;
        var o = v.OrderBy(x => x).ToList();
        var n = o.Count;
        return n % 2 == 1 ? o[n / 2] : (o[n / 2 - 1] + o[n / 2]) / 2;
    }

    private static double DesvioPadrao(List<double> v)
    {
        if (v.Count < 2) return 0;
        var m = v.Average();
        return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Count - 1));
    }

    private sealed class RepoProcesso
    {
        public string Nome { get; set; } = "";
        public string Url { get; set; } = "";
        public int Estrelas { get; set; }
        public int Forks { get; set; }
        public int Releases { get; set; }
        public int OpenIssues { get; set; }
        public int? DiskUsageKb { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
        public string Linguagem { get; set; } = "";
    }
}

using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enunciado3.Sprint2
{
    class Program
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string CsvHeader = "repository;number;state;created_at;closed_at;analysis_time_hours;changed_files;additions;deletions;description_length;participants_count;comments_count;reviews_count";
        private const string GraphQLEndpoint = "graphql";
        private const string SearchRepositoriesQuery = """
            query($queryString: String!, $first: Int!, $after: String) {
              search(query: $queryString, type: REPOSITORY, first: $first, after: $after) {
                pageInfo { hasNextPage endCursor }
                nodes {
                  ... on Repository {
                    nameWithOwner
                    name
                    owner { login }
                  }
                }
              }
            }
            """;
        private const string PullRequestsQuery = """
            query($owner: String!, $name: String!, $first: Int!, $after: String) {
              repository(owner: $owner, name: $name) {
                pullRequests(states: [MERGED, CLOSED], first: $first, after: $after, orderBy: { field: CREATED_AT, direction: DESC }) {
                  totalCount
                  pageInfo { hasNextPage endCursor }
                  nodes {
                    number
                    state
                    createdAt
                    closedAt
                    mergedAt
                    additions
                    deletions
                    changedFiles
                    body
                    comments { totalCount }
                    reviewThreads { totalCount }
                    reviews { totalCount }
                    participants { totalCount }
                  }
                }
              }
            }
            """;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enunciado 3 - Sprint 2");
            Console.WriteLine("======================");
            Console.WriteLine("Etapa 1: Coleta do dataset completo");
            Console.WriteLine("Etapa 2: Geração da primeira versão do relatório com hipóteses iniciais");
            Console.WriteLine();

            var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var outputDir = Path.Combine(pastaProjeto, "enunciado3_sprint2_output");
            Directory.CreateDirectory(outputDir);

            var csvPath = Path.Combine(outputDir, "dataset.csv");

            // Se o CSV da Sprint 2 ainda não existe, tenta reutilizar o da Sprint 1 para evitar
            // coletar tudo do zero (apenas copia como ponto de partida).
            var sprint1CsvPath = Path.Combine(pastaProjeto, "..", "Sprint 1", "dataset.csv");
            if (!File.Exists(csvPath) && File.Exists(sprint1CsvPath))
            {
                Console.WriteLine($"Reutilizando dataset da Sprint 1 como ponto de partida: {sprint1CsvPath}");
                File.Copy(sprint1CsvPath, csvPath);
            }

            // -------------------------------------------------------------------
            // ETAPA 1 — COLETA DE DADOS (código da Sprint 1 + correção do filtro de 1 hora)
            // -------------------------------------------------------------------

            using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LabExperimentacao", "1.0"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var token = ObterTokenDosArgs(args)
                ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                ?? LerTokenDoArquivo();

            if (!string.IsNullOrWhiteSpace(token))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (!await ValidarTokenAsync(httpClient, token))
                Console.WriteLine("Token inválido ou ausente. Configure um token válido para evitar rate limit e erros 401/403.");

            var dataset = new List<PullRequestDatasetItem>();
            var chavesExistentes = CarregarChavesExistentes(csvPath, dataset, out var reposExistentes);
            Console.WriteLine($"Registros já presentes no CSV: {chavesExistentes.Count}.");

            var precisaHeaderCsv = !File.Exists(csvPath);
            using var csvWriter = new StreamWriter(csvPath, append: true, Encoding.UTF8);
            if (precisaHeaderCsv)
            {
                await csvWriter.WriteLineAsync(CsvHeader);
                await csvWriter.FlushAsync();
            }

            var prsProcessados = 0;
            const int targetRepos = 200;
            var repoCursor = (string?)null;
            var hasNextRepoPage = true;

            while (reposExistentes.Count < targetRepos && hasNextRepoPage)
            {
                var repoPage = await GetTopRepositoriesPageAsync(httpClient, 30, repoCursor);
                if (repoPage is null || repoPage.Items.Count == 0) break;

                foreach (var repository in repoPage.Items)
                {
                    if (reposExistentes.Count >= targetRepos) break;

                    if (reposExistentes.Contains(repository.FullName))
                    {
                        Console.WriteLine($"Ignorado: {repository.FullName} já está no dataset.");
                        continue;
                    }

                    Console.WriteLine($"Coletando PRs em {repository.FullName}...");
                    var pullRequestsResult = await GetClosedPullRequestsAsync(httpClient, repository.Owner.Login, repository.Name, 100);

                    if (pullRequestsResult is null)
                    {
                        Console.WriteLine($"Ignorado: falha temporária ao consultar {repository.FullName}.");
                        continue;
                    }

                    if (pullRequestsResult.TotalCount < 100)
                    {
                        Console.WriteLine("Ignorado: menos de 100 PRs fechados.");
                        continue;
                    }

                    var prsIncluidosRepo = 0;
                    foreach (var pullRequest in pullRequestsResult.Items)
                    {
                        var chave = CriarChave(repository.FullName, pullRequest.Number);
                        if (chavesExistentes.Contains(chave)) continue;

                        var closedAt = pullRequest.MergedAt ?? pullRequest.ClosedAt;
                        if (closedAt is null) continue;

                        // Filtro: pelo menos uma revisão (exigência do enunciado)
                        if (pullRequest.Reviews.TotalCount < 1) continue;

                        var duration = closedAt.Value - pullRequest.CreatedAt;

                        // CORREÇÃO em relação à Sprint 1: filtrar PRs com menos de 1 hora de análise
                        // (o enunciado exige diferença > 1h para remover revisões automáticas de bots/CI)
                        if (duration.TotalHours <= 1) continue;

                        var commentsCount = pullRequest.Comments.TotalCount + pullRequest.ReviewThreads.TotalCount;
                        var item = new PullRequestDatasetItem(
                            repository.FullName,
                            pullRequest.Number,
                            pullRequest.State,
                            pullRequest.CreatedAt,
                            closedAt.Value,
                            duration.TotalHours,
                            pullRequest.ChangedFiles,
                            pullRequest.Additions,
                            pullRequest.Deletions,
                            pullRequest.Body?.Length ?? 0,
                            pullRequest.Participants.TotalCount,
                            commentsCount,
                            pullRequest.Reviews.TotalCount);

                        dataset.Add(item);
                        chavesExistentes.Add(chave);
                        await csvWriter.WriteLineAsync(FormatarCsvLinha(item));
                        await csvWriter.FlushAsync();
                        prsIncluidosRepo++;
                        prsProcessados++;
                    }

                    reposExistentes.Add(repository.FullName);
                    Console.WriteLine($"PRs incluídos de {repository.FullName}: {prsIncluidosRepo}. " +
                                      $"Total acumulado: {prsProcessados}. Repos distintos: {reposExistentes.Count}.");
                }

                repoCursor = repoPage.NextCursor;
                hasNextRepoPage = repoPage.HasNextPage;
            }

            Console.WriteLine();
            Console.WriteLine($"Dataset completo: {dataset.Count} PRs de {reposExistentes.Count} repositórios.");
            Console.WriteLine($"CSV salvo em: {csvPath}");

            // -------------------------------------------------------------------
            // ETAPA 2 — GERAÇÃO DO RELATÓRIO INICIAL COM HIPÓTESES
            // -------------------------------------------------------------------

            Console.WriteLine();
            Console.WriteLine("Gerando primeira versão do relatório com hipóteses iniciais...");

            var relatorioPath = Path.Combine(outputDir, "relatorio_inicial.md");
            await GerarRelatorioInicialAsync(dataset, relatorioPath);

            Console.WriteLine($"Relatório gerado em: {relatorioPath}");
            Console.WriteLine();
            Console.WriteLine("Sprint 2 concluída.");
        }

        // -----------------------------------------------------------------------
        // GERAÇÃO DO RELATÓRIO
        // -----------------------------------------------------------------------

        private static async Task GerarRelatorioInicialAsync(List<PullRequestDatasetItem> dataset, string relatorioPath)
        {
            var merged = dataset.Where(p => p.State.Equals("MERGED", StringComparison.OrdinalIgnoreCase)).ToList();
            var closed = dataset.Where(p => p.State.Equals("CLOSED", StringComparison.OrdinalIgnoreCase)).ToList();

            var sb = new StringBuilder();

            sb.AppendLine("# Laboratório 03 — Caracterizando a Atividade de Code Review no GitHub");
            sb.AppendLine("## Primeira Versão do Relatório — Hipóteses Iniciais");
            sb.AppendLine();
            sb.AppendLine($"**Data de geração:** {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"**Total de PRs coletados:** {dataset.Count}");
            sb.AppendLine($"**PRs MERGED:** {merged.Count}");
            sb.AppendLine($"**PRs CLOSED:** {closed.Count}");
            sb.AppendLine();

            // ------------------------------------------------------------------
            // 1. INTRODUÇÃO E HIPÓTESES INFORMAIS
            // ------------------------------------------------------------------
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 1. Introdução");
            sb.AppendLine();
            sb.AppendLine("Este relatório apresenta a primeira versão da análise sobre a atividade de code review " +
                          "em repositórios populares do GitHub. O dataset foi construído a partir dos 200 repositórios " +
                          "mais populares (por estrelas) que possuíam pelo menos 100 PRs fechados (MERGED + CLOSED). " +
                          "Foram incluídos apenas PRs com pelo menos uma revisão e cujo tempo de análise foi superior a uma hora, " +
                          "excluindo assim revisões automáticas realizadas por bots ou ferramentas de CI/CD.");
            sb.AppendLine();
            sb.AppendLine("### Hipóteses Iniciais");
            sb.AppendLine();
            sb.AppendLine("Antes da análise estatística, elencamos nossas expectativas informais para cada questão de pesquisa:");
            sb.AppendLine();

            sb.AppendLine("**RQ 01 — Tamanho × Status do PR:**");
            sb.AppendLine("Esperamos que PRs aceitos (MERGED) sejam, em geral, menores do que os rejeitados (CLOSED). " +
                          "PRs pequenos são mais fáceis de revisar, menos propensos a conflitos e tendem a ser aprovados com mais rapidez.");
            sb.AppendLine();
            sb.AppendLine("**RQ 02 — Tempo de Análise × Status do PR:**");
            sb.AppendLine("Acreditamos que PRs MERGED tendam a ter um tempo de análise maior, pois passam por um processo " +
                          "de revisão mais cuidadoso. PRs CLOSED podem ser rejeitados rapidamente quando claramente inadequados, " +
                          "mas também podem arrastar-se por longos períodos sem consenso.");
            sb.AppendLine();
            sb.AppendLine("**RQ 03 — Descrição × Status do PR:**");
            sb.AppendLine("Nossa hipótese é que PRs MERGED possuam descrições mais longas e detalhadas. " +
                          "Uma boa descrição facilita o entendimento do revisor e aumenta as chances de aprovação.");
            sb.AppendLine();
            sb.AppendLine("**RQ 04 — Interações × Status do PR:**");
            sb.AppendLine("Esperamos que PRs MERGED tenham mais participantes e comentários, refletindo um processo " +
                          "colaborativo mais rico. PRs CLOSED podem ter poucas interações se forem rejeitados precocemente.");
            sb.AppendLine();
            sb.AppendLine("**RQ 05 — Tamanho × Número de Revisões:**");
            sb.AppendLine("Acreditamos que PRs maiores (mais arquivos e linhas) exijam mais rodadas de revisão, " +
                          "pois apresentam mais pontos passíveis de feedback e correção.");
            sb.AppendLine();
            sb.AppendLine("**RQ 06 — Tempo de Análise × Número de Revisões:**");
            sb.AppendLine("Esperamos correlação positiva: PRs com mais revisões tendem a demorar mais para serem " +
                          "finalizados, pois cada rodada envolve correção e reanálise.");
            sb.AppendLine();
            sb.AppendLine("**RQ 07 — Descrição × Número de Revisões:**");
            sb.AppendLine("Hipótese: PRs com descrições mais longas podem precisar de menos revisões, pois o " +
                          "contexto claro reduz dúvidas. Alternativamente, descrições longas podem indicar maior " +
                          "complexidade e, portanto, mais revisões.");
            sb.AppendLine();
            sb.AppendLine("**RQ 08 — Interações × Número de Revisões:**");
            sb.AppendLine("Esperamos forte correlação positiva entre o número de comentários/participantes e o " +
                          "número de revisões, pois PRs mais discutidos naturalmente passam por mais ciclos de revisão.");
            sb.AppendLine();

            // ------------------------------------------------------------------
            // 2. METODOLOGIA
            // ------------------------------------------------------------------
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 2. Metodologia");
            sb.AppendLine();
            sb.AppendLine("### 2.1 Criação do Dataset");
            sb.AppendLine();
            sb.AppendLine("Os dados foram coletados via API GraphQL do GitHub. Os critérios de seleção foram:");
            sb.AppendLine();
            sb.AppendLine("- **Repositórios:** 200 repositórios mais populares do GitHub (ordenados por estrelas).");
            sb.AppendLine("- **Requisito mínimo:** pelo menos 100 PRs com status MERGED ou CLOSED.");
            sb.AppendLine("- **Filtro de revisão:** apenas PRs com pelo menos uma revisão registrada.");
            sb.AppendLine("- **Filtro de tempo:** apenas PRs cujo intervalo entre criação e fechamento/merge é superior a 1 hora " +
                          "(para excluir revisões automáticas de bots e ferramentas de CI/CD).");
            sb.AppendLine();
            sb.AppendLine("### 2.2 Métricas Coletadas");
            sb.AppendLine();
            sb.AppendLine("| Dimensão | Métrica | Campo no CSV |");
            sb.AppendLine("|----------|---------|--------------|");
            sb.AppendLine("| Tamanho | Número de arquivos alterados | `changed_files` |");
            sb.AppendLine("| Tamanho | Total de linhas adicionadas | `additions` |");
            sb.AppendLine("| Tamanho | Total de linhas removidas | `deletions` |");
            sb.AppendLine("| Tempo de Análise | Horas entre criação e fechamento/merge | `analysis_time_hours` |");
            sb.AppendLine("| Descrição | Número de caracteres do corpo do PR | `description_length` |");
            sb.AppendLine("| Interações | Número de participantes | `participants_count` |");
            sb.AppendLine("| Interações | Número de comentários (issue + review threads) | `comments_count` |");
            sb.AppendLine("| Variável dependente A | Status final (MERGED / CLOSED) | `state` |");
            sb.AppendLine("| Variável dependente B | Número de revisões | `reviews_count` |");
            sb.AppendLine();
            sb.AppendLine("### 2.3 Análise Estatística (planejada para Sprint 3)");
            sb.AppendLine();
            sb.AppendLine("As correlações entre métricas serão calculadas utilizando o **teste de correlação de Spearman**, " +
                          "por ser não-paramétrico e adequado para dados ordinais ou que não seguem distribuição normal — " +
                          "o que é esperado em métricas de repositórios de software (caudas longas, outliers). " +
                          "As comparações entre grupos MERGED e CLOSED utilizarão **valores medianos**, conforme indicado no enunciado.");
            sb.AppendLine();

            // ------------------------------------------------------------------
            // 3. SUMÁRIO DO DATASET (valores medianos)
            // ------------------------------------------------------------------
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 3. Sumário do Dataset (Valores Medianos)");
            sb.AppendLine();

            if (dataset.Count == 0)
            {
                sb.AppendLine("> *Nenhum dado coletado ainda. Execute a coleta antes de gerar o relatório final.*");
            }
            else
            {
                sb.AppendLine("### 3.1 Visão Geral");
                sb.AppendLine();
                sb.AppendLine("| Métrica | Mediana Geral | Mediana MERGED | Mediana CLOSED |");
                sb.AppendLine("|---------|:-------------:|:--------------:|:--------------:|");

                void LinhaMediana(string nome, Func<PullRequestDatasetItem, double> selector)
                {
                    var geral = Mediana(dataset.Select(selector));
                    var med = merged.Count > 0 ? Mediana(merged.Select(selector)) : double.NaN;
                    var clo = closed.Count > 0 ? Mediana(closed.Select(selector)) : double.NaN;
                    sb.AppendLine($"| {nome} | {FormatN(geral)} | {FormatN(med)} | {FormatN(clo)} |");
                }

                LinhaMediana("Arquivos alterados", p => p.ChangedFiles);
                LinhaMediana("Linhas adicionadas", p => p.Additions);
                LinhaMediana("Linhas removidas", p => p.Deletions);
                LinhaMediana("Tempo de análise (horas)", p => p.AnalysisTimeHours);
                LinhaMediana("Comprimento da descrição (chars)", p => p.DescriptionLength);
                LinhaMediana("Participantes", p => p.ParticipantsCount);
                LinhaMediana("Comentários", p => p.CommentsCount);
                LinhaMediana("Revisões", p => p.ReviewsCount);
                sb.AppendLine();

                sb.AppendLine("### 3.2 Distribuição por Status");
                sb.AppendLine();
                sb.AppendLine($"- Total de PRs: **{dataset.Count}**");
                sb.AppendLine($"- MERGED: **{merged.Count}** ({(dataset.Count > 0 ? 100.0 * merged.Count / dataset.Count : 0):F1}%)");
                sb.AppendLine($"- CLOSED: **{closed.Count}** ({(dataset.Count > 0 ? 100.0 * closed.Count / dataset.Count : 0):F1}%)");
                sb.AppendLine();
            }

            // ------------------------------------------------------------------
            // 4. RESULTADOS PRELIMINARES E DISCUSSÃO
            // ------------------------------------------------------------------
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 4. Resultados Preliminares e Discussão das Hipóteses");
            sb.AppendLine();
            sb.AppendLine("Esta seção será expandida na Sprint 3, após a aplicação dos testes estatísticos e " +
                          "a criação das visualizações. Com base nos valores medianos apresentados na seção anterior, " +
                          "é possível realizar uma análise preliminar qualitativa:");
            sb.AppendLine();

            if (dataset.Count > 0)
            {
                double MedianaMerged(Func<PullRequestDatasetItem, double> sel) =>
                    merged.Count > 0 ? Mediana(merged.Select(sel)) : double.NaN;
                double MedianaClosed(Func<PullRequestDatasetItem, double> sel) =>
                    closed.Count > 0 ? Mediana(closed.Select(sel)) : double.NaN;

                ResumoRQ(sb, "RQ 01", "Tamanho × Status",
                    "Número de arquivos alterados",
                    MedianaMerged(p => p.ChangedFiles),
                    MedianaClosed(p => p.ChangedFiles),
                    "PRs MERGED tendem a ser menores",
                    "PRs CLOSED tendem a ser menores");

                ResumoRQ(sb, "RQ 02", "Tempo de Análise × Status",
                    "Tempo de análise (horas)",
                    MedianaMerged(p => p.AnalysisTimeHours),
                    MedianaClosed(p => p.AnalysisTimeHours),
                    "PRs MERGED passam por revisão mais longa",
                    "PRs CLOSED são fechados mais rapidamente");

                ResumoRQ(sb, "RQ 03", "Descrição × Status",
                    "Comprimento da descrição (chars)",
                    MedianaMerged(p => p.DescriptionLength),
                    MedianaClosed(p => p.DescriptionLength),
                    "PRs MERGED possuem descrições mais detalhadas",
                    "PRs CLOSED possuem descrições mais detalhadas (inesperado)");

                ResumoRQ(sb, "RQ 04", "Interações × Status",
                    "Número de comentários",
                    MedianaMerged(p => p.CommentsCount),
                    MedianaClosed(p => p.CommentsCount),
                    "PRs MERGED têm mais interações colaborativas",
                    "PRs CLOSED têm mais interações (possível controvérsia)");
            }
            else
            {
                sb.AppendLine("> *Análise preliminar será gerada após a coleta dos dados.*");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 5. Próximos Passos (Sprint 3)");
            sb.AppendLine();
            sb.AppendLine("- Aplicar o teste de correlação de Spearman para todas as 8 RQs.");
            sb.AppendLine("- Gerar visualizações (box plots, scatter plots) para cada par de variáveis.");
            sb.AppendLine("- Confrontar os resultados com as hipóteses iniciais.");
            sb.AppendLine("- Elaborar o relatório final completo.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("*Relatório gerado automaticamente pela Sprint 2 do Laboratório 03.*");

            await File.WriteAllTextAsync(relatorioPath, sb.ToString(), Encoding.UTF8);
        }

        private static void ResumoRQ(StringBuilder sb, string rq, string titulo,
            string metrica, double medMerged, double medClosed,
            string interpretacaoSeConfirma, string interpretacaoSeRefuta)
        {
            sb.AppendLine($"### {rq} — {titulo}");
            sb.AppendLine();

            if (double.IsNaN(medMerged) || double.IsNaN(medClosed))
            {
                sb.AppendLine("> *Dados insuficientes para análise preliminar.*");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"- Mediana ({metrica}) MERGED: **{FormatN(medMerged)}**");
            sb.AppendLine($"- Mediana ({metrica}) CLOSED: **{FormatN(medClosed)}**");

            if (medMerged < medClosed)
                sb.AppendLine($"- **Observação preliminar:** {interpretacaoSeConfirma}. Confirma a hipótese inicial.");
            else
                sb.AppendLine($"- **Observação preliminar:** {interpretacaoSeRefuta}. Contradiz ou nuança a hipótese inicial.");

            sb.AppendLine();
        }

        private static string FormatN(double v) =>
            double.IsNaN(v) ? "—" : v.ToString("F2", CultureInfo.InvariantCulture);

        private static double Mediana(IEnumerable<double> valores)
        {
            var sorted = valores.OrderBy(v => v).ToArray();
            if (sorted.Length == 0) return double.NaN;
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }

        // -----------------------------------------------------------------------
        // COLETA VIA GRAPHQL (idêntico à Sprint 1, sem alterações)
        // -----------------------------------------------------------------------

        private static async Task<RepositoryPage?> GetTopRepositoriesPageAsync(HttpClient httpClient, int pageSize, string? after)
        {
            var response = await PostGraphqlAsync<GraphqlSearchResponse>(httpClient, SearchRepositoriesQuery, new
            {
                queryString = "stars:>0 sort:stars-desc",
                first = pageSize,
                after
            });
            if (response?.Search?.Nodes is null) return null;

            var items = response.Search.Nodes.Select(node => new RepositoryItem(
                node.NameWithOwner,
                node.Name,
                new RepositoryOwner(node.Owner.Login))).ToList();

            return new RepositoryPage(items, response.Search.PageInfo.HasNextPage, response.Search.PageInfo.EndCursor);
        }

        private static async Task<PullRequestsResult?> GetClosedPullRequestsAsync(HttpClient httpClient, string owner, string repo, int minimumCount)
        {
            var pullRequests = new List<PullRequestGraphqlItem>();
            var totalCount = 0;
            const int perPage = 30;
            string? after = null;

            while (true)
            {
                var response = await PostGraphqlAsync<GraphqlRepositoryResponse>(httpClient, PullRequestsQuery, new
                {
                    owner,
                    name = repo,
                    first = perPage,
                    after
                });
                if (response?.Repository?.PullRequests is null) return null;

                totalCount = response.Repository.PullRequests.TotalCount;
                var pageItems = response.Repository.PullRequests.Nodes ?? new List<PullRequestGraphqlItem>();
                if (pageItems.Count == 0) break;

                pullRequests.AddRange(pageItems);
                if (pullRequests.Count >= minimumCount) break;

                if (!response.Repository.PullRequests.PageInfo.HasNextPage) break;

                after = response.Repository.PullRequests.PageInfo.EndCursor;
            }

            return new PullRequestsResult(totalCount, pullRequests);
        }

        // -----------------------------------------------------------------------
        // AUTENTICAÇÃO E UTILITÁRIOS
        // -----------------------------------------------------------------------

        private static string? ObterTokenDosArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--token=", StringComparison.OrdinalIgnoreCase))
                {
                    var valor = arg["--token=".Length..].Trim();
                    return string.IsNullOrWhiteSpace(valor) ? null : valor;
                }
            }
            return null;
        }

        private static string? LerTokenDoArquivo()
        {
            var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var caminho = Path.Combine(pastaProjeto, "..", "..", ".github-token");
            if (!File.Exists(caminho)) return null;
            var token = File.ReadAllText(caminho).Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        private static async Task<bool> ValidarTokenAsync(HttpClient httpClient, string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            using var request = new HttpRequestMessage(HttpMethod.Get, "user");
            using var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Token válido confirmado.");
                return true;
            }
            Console.WriteLine($"Falha ao validar token: {(int)response.StatusCode} {response.ReasonPhrase}.");
            return false;
        }

        private static HashSet<string> CarregarChavesExistentes(string csvPath, List<PullRequestDatasetItem> dataset, out HashSet<string> reposExistentes)
        {
            var chaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            reposExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(csvPath)) return chaves;

            foreach (var linha in File.ReadLines(csvPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("repository;", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cols = linha.Split(';');
                if (cols.Length < 13) continue;

                try
                {
                    var item = new PullRequestDatasetItem(
                        cols[0],
                        int.Parse(cols[1], CultureInfo.InvariantCulture),
                        cols[2],
                        DateTimeOffset.Parse(cols[3], CultureInfo.InvariantCulture),
                        DateTimeOffset.Parse(cols[4], CultureInfo.InvariantCulture),
                        double.Parse(cols[5], CultureInfo.InvariantCulture),
                        int.Parse(cols[6], CultureInfo.InvariantCulture),
                        int.Parse(cols[7], CultureInfo.InvariantCulture),
                        int.Parse(cols[8], CultureInfo.InvariantCulture),
                        int.Parse(cols[9], CultureInfo.InvariantCulture),
                        int.Parse(cols[10], CultureInfo.InvariantCulture),
                        int.Parse(cols[11], CultureInfo.InvariantCulture),
                        int.Parse(cols[12], CultureInfo.InvariantCulture));

                    dataset.Add(item);
                    chaves.Add(CriarChave(item.Repository, item.Number));
                    reposExistentes.Add(item.Repository);
                }
                catch
                {
                    // linha malformada — ignora
                }
            }

            return chaves;
        }

        private static string CriarChave(string repository, int number) => $"{repository}#{number}";

        private static string FormatarCsvLinha(PullRequestDatasetItem item) =>
            string.Join(';',
                item.Repository,
                item.Number.ToString(CultureInfo.InvariantCulture),
                item.State,
                item.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                item.ClosedAt.ToString("O", CultureInfo.InvariantCulture),
                item.AnalysisTimeHours.ToString("F4", CultureInfo.InvariantCulture),
                item.ChangedFiles.ToString(CultureInfo.InvariantCulture),
                item.Additions.ToString(CultureInfo.InvariantCulture),
                item.Deletions.ToString(CultureInfo.InvariantCulture),
                item.DescriptionLength.ToString(CultureInfo.InvariantCulture),
                item.ParticipantsCount.ToString(CultureInfo.InvariantCulture),
                item.CommentsCount.ToString(CultureInfo.InvariantCulture),
                item.ReviewsCount.ToString(CultureInfo.InvariantCulture));

        private static async Task<T?> PostGraphqlAsync<T>(HttpClient httpClient, string query, object variables) where T : class
        {
            var retry = 0;
            const int maxRetries = 5;

            while (true)
            {
                var payload = JsonSerializer.Serialize(new { query, variables }, SerializerOptions);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, GraphQLEndpoint) { Content = content };
                using var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GraphqlResponse<T>>(body, SerializerOptions);
                    if (result?.Errors is { Count: > 0 })
                    {
                        Console.WriteLine($"GraphQL erro: {result.Errors[0].Message}");
                        return default;
                    }
                    return result?.Data;
                }

                if (response.StatusCode is System.Net.HttpStatusCode.BadGateway
                    or System.Net.HttpStatusCode.ServiceUnavailable
                    or System.Net.HttpStatusCode.GatewayTimeout)
                {
                    retry++;
                    if (retry > maxRetries)
                    {
                        Console.WriteLine($"Erro temporário persistente após {maxRetries} tentativas. Pulando esta requisição.");
                        return default;
                    }
                    var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retry)));
                    Console.WriteLine($"Erro temporário {(int)response.StatusCode}. Tentando novamente em {Math.Ceiling(delay.TotalSeconds)}s...");
                    await Task.Delay(delay);
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden &&
                    response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
                    string.Equals(remainingValues.FirstOrDefault(), "0", StringComparison.Ordinal))
                {
                    var delay = GetDelayFromRateLimitReset(response) ?? TimeSpan.FromMinutes(1);
                    Console.WriteLine($"Rate limit atingido. Aguardando {Math.Ceiling(delay.TotalSeconds)}s...");
                    await Task.Delay(delay);
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Recurso GraphQL não encontrado (404). Verifique a URL e o token.");
                    return default;
                }

                response.EnsureSuccessStatusCode();
            }
        }

        private static TimeSpan? GetDelayFromRateLimitReset(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues)) return null;
            var raw = resetValues.FirstOrDefault();
            if (!long.TryParse(raw, out var resetUnix)) return null;
            var resetAt = DateTimeOffset.FromUnixTimeSeconds(resetUnix);
            var delay = resetAt - DateTimeOffset.UtcNow;
            return delay <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : delay;
        }
    }

    // -----------------------------------------------------------------------
    // RECORDS (idênticos à Sprint 1)
    // -----------------------------------------------------------------------

    public sealed record RepositoryItem(
        [property: JsonPropertyName("full_name")] string FullName,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("owner")] RepositoryOwner Owner);

    public sealed record RepositoryOwner(
        [property: JsonPropertyName("login")] string Login);

    public sealed record RepositoryPage(
        List<RepositoryItem> Items,
        bool HasNextPage,
        string? NextCursor);

    public sealed record PullRequestsResult(int TotalCount, List<PullRequestGraphqlItem> Items);

    public sealed record GraphqlResponse<T>(
        [property: JsonPropertyName("data")] T? Data,
        [property: JsonPropertyName("errors")] List<GraphqlError>? Errors);

    public sealed record GraphqlError(
        [property: JsonPropertyName("message")] string Message);

    public sealed record GraphqlSearchResponse(
        [property: JsonPropertyName("search")] GraphqlSearchContainer Search);

    public sealed record GraphqlSearchContainer(
        [property: JsonPropertyName("pageInfo")] GraphqlPageInfo PageInfo,
        [property: JsonPropertyName("nodes")] List<GraphqlRepositoryNode> Nodes);

    public sealed record GraphqlRepositoryNode(
        [property: JsonPropertyName("nameWithOwner")] string NameWithOwner,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("owner")] GraphqlOwner Owner);

    public sealed record GraphqlOwner(
        [property: JsonPropertyName("login")] string Login);

    public sealed record GraphqlRepositoryResponse(
        [property: JsonPropertyName("repository")] GraphqlRepository Repository);

    public sealed record GraphqlRepository(
        [property: JsonPropertyName("pullRequests")] GraphqlPullRequestConnection PullRequests);

    public sealed record GraphqlPullRequestConnection(
        [property: JsonPropertyName("totalCount")] int TotalCount,
        [property: JsonPropertyName("pageInfo")] GraphqlPageInfo PageInfo,
        [property: JsonPropertyName("nodes")] List<PullRequestGraphqlItem> Nodes);

    public sealed record GraphqlPageInfo(
        [property: JsonPropertyName("hasNextPage")] bool HasNextPage,
        [property: JsonPropertyName("endCursor")] string? EndCursor);

    public sealed record PullRequestGraphqlItem(
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("closedAt")] DateTimeOffset? ClosedAt,
        [property: JsonPropertyName("mergedAt")] DateTimeOffset? MergedAt,
        [property: JsonPropertyName("additions")] int Additions,
        [property: JsonPropertyName("deletions")] int Deletions,
        [property: JsonPropertyName("changedFiles")] int ChangedFiles,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("comments")] GraphqlCount Comments,
        [property: JsonPropertyName("reviewThreads")] GraphqlCount ReviewThreads,
        [property: JsonPropertyName("reviews")] GraphqlCount Reviews,
        [property: JsonPropertyName("participants")] GraphqlCount Participants);

    public sealed record GraphqlCount(
        [property: JsonPropertyName("totalCount")] int TotalCount);

    public sealed record PullRequestDatasetItem(
        string Repository,
        int Number,
        string State,
        DateTimeOffset CreatedAt,
        DateTimeOffset ClosedAt,
        double AnalysisTimeHours,
        int ChangedFiles,
        int Additions,
        int Deletions,
        int DescriptionLength,
        int ParticipantsCount,
        int CommentsCount,
        int ReviewsCount);
}

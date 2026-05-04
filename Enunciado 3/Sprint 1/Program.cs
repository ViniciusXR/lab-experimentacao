using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enunciado3.Sprint1
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
            Console.WriteLine("Enunciado 3 - Sprint 1");

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.github.com/")
            };

            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LabExperimentacao", "1.0"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var token = ObterTokenDosArgs(args)
                ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                ?? LerTokenDoArquivo();
            if (!string.IsNullOrWhiteSpace(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (!await ValidarTokenAsync(httpClient, token))
            {
                Console.WriteLine("Token inválido ou ausente. Configure um token válido para evitar rate limit e erros 401/403.");
            }

            var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var outputDir = Path.Combine(pastaProjeto, "enunciado3_sprint1_output");
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, "dataset.json");
            var csvPath = Path.Combine(pastaProjeto, "dataset.csv");

            Console.WriteLine("Coletando repositórios populares (GraphQL)...");
            var dataset = new List<PullRequestDatasetItem>();
            var chavesExistentes = CarregarChavesExistentes(csvPath, dataset, out var reposExistentes);
            Console.WriteLine($"Registros já coletados no CSV: {chavesExistentes.Count}.");
            var precisaHeaderCsv = !File.Exists(csvPath);
            using var csvWriter = new StreamWriter(csvPath, append: true, Encoding.UTF8);
            if (precisaHeaderCsv)
            {
                await csvWriter.WriteLineAsync(CsvHeader);
                await csvWriter.FlushAsync();
            }
            var reposProcessados = 0;
            var prsProcessados = 0;
            const int targetRepos = 200;
            var repoCursor = (string?)null;
            var hasNextRepoPage = true;

            while (reposExistentes.Count < targetRepos && hasNextRepoPage)
            {
                var repoPage = await GetTopRepositoriesPageAsync(httpClient, 30, repoCursor);
                if (repoPage is null || repoPage.Items.Count == 0)
                {
                    break;
                }

                foreach (var repository in repoPage.Items)
                {
                    if (reposExistentes.Count >= targetRepos)
                    {
                        break;
                    }

                    if (reposExistentes.Contains(repository.FullName))
                    {
                        Console.WriteLine($"Ignorado: {repository.FullName} já está no CSV.");
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
                        if (chavesExistentes.Contains(chave))
                        {
                            continue;
                        }

                        var closedAt = pullRequest.MergedAt ?? pullRequest.ClosedAt;
                        if (closedAt is null)
                        {
                            continue;
                        }

                        if (pullRequest.Reviews.TotalCount < 1)
                        {
                            continue;
                        }

                        var commentsCount = pullRequest.Comments.TotalCount + pullRequest.ReviewThreads.TotalCount;
                        var status = pullRequest.State;
                        var duration = closedAt.Value - pullRequest.CreatedAt;
                        var item = new PullRequestDatasetItem(
                            repository.FullName,
                            pullRequest.Number,
                            status,
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

                    reposProcessados++;
                    reposExistentes.Add(repository.FullName);
                    Console.WriteLine($"PRs incluídos em {repository.FullName}: {prsIncluidosRepo}. Total acumulado: {prsProcessados}. Repos distintos: {reposExistentes.Count}.");
                }

                repoCursor = repoPage.NextCursor;
                hasNextRepoPage = repoPage.HasNextPage;
            }

            Console.WriteLine($"Salvando dataset em {outputPath}...");
            var output = JsonSerializer.Serialize(dataset, SerializerOptions);
            await File.WriteAllTextAsync(outputPath, output);
            Console.WriteLine($"Dataset gerado com {dataset.Count} PRs.");
        }

        private static async Task<RepositoryPage?> GetTopRepositoriesPageAsync(HttpClient httpClient, int pageSize, string? after)
        {
            var response = await PostGraphqlAsync<GraphqlSearchResponse>(httpClient, SearchRepositoriesQuery, new
            {
                queryString = "stars:>0 sort:stars-desc",
                first = pageSize,
                after
            });
            if (response?.Search?.Nodes is null)
            {
                return null;
            }

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
                if (response?.Repository?.PullRequests is null)
                {
                    return null;
                }

                totalCount = response.Repository.PullRequests.TotalCount;
                var pageItems = response.Repository.PullRequests.Nodes ?? new List<PullRequestGraphqlItem>();
                if (pageItems.Count == 0)
                {
                    break;
                }

                pullRequests.AddRange(pageItems);
                if (pullRequests.Count >= minimumCount)
                {
                    break;
                }

                if (!response.Repository.PullRequests.PageInfo.HasNextPage)
                {
                    break;
                }

                after = response.Repository.PullRequests.PageInfo.EndCursor;
            }

            return new PullRequestsResult(totalCount, pullRequests);
        }

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
            var caminho = Path.Combine(pastaProjeto, "..", ".github-token");
            if (!File.Exists(caminho))
            {
                return null;
            }

            var token = File.ReadAllText(caminho).Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        private static async Task<bool> ValidarTokenAsync(HttpClient httpClient, string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

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
            if (!File.Exists(csvPath))
            {
                return chaves;
            }

            foreach (var linha in File.ReadLines(csvPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("repository;", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var cols = linha.Split(';');
                if (cols.Length < 13)
                {
                    continue;
                }

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

            return chaves;
        }

        private static string CriarChave(string repository, int number)
            => $"{repository}#{number}";

        private static string FormatarCsvLinha(PullRequestDatasetItem item)
        {
            return string.Join(';',
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
        }

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
                    Console.WriteLine($"Erro temporário {((int)response.StatusCode)}. Tentando novamente em {Math.Ceiling(delay.TotalSeconds)}s...");
                    await Task.Delay(delay);
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
                {
                    var remaining = remainingValues.FirstOrDefault();
                    if (string.Equals(remaining, "0", StringComparison.Ordinal))
                    {
                        var delay = GetDelayFromRateLimitReset(response) ?? TimeSpan.FromMinutes(1);
                        Console.WriteLine($"Rate limit atingido. Aguardando {Math.Ceiling(delay.TotalSeconds)}s...");
                        await Task.Delay(delay);
                        continue;
                    }
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
            if (!response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
            {
                return null;
            }

            var raw = resetValues.FirstOrDefault();
            if (!long.TryParse(raw, out var resetUnix))
            {
                return null;
            }

            var resetAt = DateTimeOffset.FromUnixTimeSeconds(resetUnix);
            var delay = resetAt - DateTimeOffset.UtcNow;
            return delay <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : delay;
        }
    }

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

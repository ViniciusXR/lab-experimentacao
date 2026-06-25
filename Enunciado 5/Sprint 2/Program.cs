using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Enunciado5.Sprint2;

internal enum ApiTreatment
{
    REST,
    GraphQL
}

internal static class Program
{
    private const int DatasetSeed = 20260624;
    private const int TrialSeed = 5052026;
    private const int TrialsPerTreatment = 120;
    private const int WarmupIterations = 20;

    private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static async Task Main()
    {
        var labRoot = FindLabRoot();
        var outputRoot = Path.Combine(labRoot, "lab05_entrega");
        var dataDir = Path.Combine(outputRoot, "dados");
        var reportsDir = Path.Combine(outputRoot, "relatorios");

        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(reportsDir);

        var catalog = SyntheticCatalog.Create(DatasetSeed);
        var scenarios = ScenarioFactory.Create(catalog);

        WarmUp(scenarios);

        var results = RunExperiment(scenarios);
        var summaries = Summarize(results);
        var comparisons = CompareTreatments(results, summaries);

        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "medicoes_lab05.csv"),
            CsvWriters.BuildMeasurements(results), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "resumo_estatistico.csv"),
            CsvWriters.BuildSummaries(summaries), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "testes_estatisticos.csv"),
            CsvWriters.BuildComparisons(comparisons), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "metadados_execucao.json"),
            BuildExecutionMetadata(results.Count), Encoding.UTF8);

        await File.WriteAllTextAsync(
            Path.Combine(reportsDir, "relatorio_final_lab05.md"),
            ReportBuilder.Build(results, summaries, comparisons), Encoding.UTF8);

        PdfReportBuilder.Generate(
            Path.Combine(reportsDir, "relatorio_final_lab05.pdf"),
            summaries,
            comparisons);

        Console.WriteLine("Lab05S02 concluido.");
        Console.WriteLine($"Medicoes coletadas: {results.Count.ToString("N0", PtBr)}");
        Console.WriteLine($"Artefatos gerados em: {outputRoot}");
        Console.ReadKey();
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

        return Directory.GetCurrentDirectory();
    }

    private static void WarmUp(IReadOnlyList<ScenarioDefinition> scenarios)
    {
        foreach (var scenario in scenarios)
        {
            foreach (var treatment in Enum.GetValues<ApiTreatment>())
            {
                for (var i = 0; i < WarmupIterations; i++)
                {
                    var payload = scenario.Execute(treatment);
                    using var document = JsonDocument.Parse(payload);
                    _ = document.RootElement.ValueKind;
                }
            }
        }
    }

    private static List<TrialResult> RunExperiment(IReadOnlyList<ScenarioDefinition> scenarios)
    {
        var schedule = new List<ScheduledTrial>();
        foreach (var scenario in scenarios)
        {
            foreach (var treatment in Enum.GetValues<ApiTreatment>())
            {
                for (var trial = 1; trial <= TrialsPerTreatment; trial++)
                {
                    schedule.Add(new ScheduledTrial(scenario, treatment, trial));
                }
            }
        }

        Shuffle(schedule, TrialSeed);

        var results = new List<TrialResult>(schedule.Count);
        var order = 1;
        foreach (var scheduled in schedule)
        {
            results.Add(Measure(scheduled, order++));
        }

        return results
            .OrderBy(r => r.ScenarioId, StringComparer.Ordinal)
            .ThenBy(r => r.Treatment, StringComparer.Ordinal)
            .ThenBy(r => r.Trial)
            .ToList();
    }

    private static TrialResult Measure(ScheduledTrial scheduled, int executionOrder)
    {
        var started = Stopwatch.GetTimestamp();
        var payload = scheduled.Scenario.Execute(scheduled.Treatment);
        using var document = JsonDocument.Parse(payload);
        var consumed = document.RootElement.ValueKind;
        var finished = Stopwatch.GetTimestamp();

        var elapsedMs = (finished - started) * 1000.0 / Stopwatch.Frequency;
        var checksum = ComputeChecksum(payload) + (int)consumed;

        return new TrialResult(
            scheduled.Scenario.Id,
            scheduled.Scenario.Name,
            scheduled.Treatment.ToString(),
            scheduled.Trial,
            executionOrder,
            elapsedMs,
            payload.Length,
            checksum);
    }

    private static int ComputeChecksum(byte[] payload)
    {
        unchecked
        {
            var hash = 17;
            foreach (var value in payload)
            {
                hash = (hash * 31) + value;
            }

            return hash;
        }
    }

    private static void Shuffle<T>(IList<T> items, int seed)
    {
        var random = new Random(seed);
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static List<SummaryRow> Summarize(IReadOnlyList<TrialResult> results)
    {
        return results
            .GroupBy(r => new { r.ScenarioId, r.ScenarioName, r.Treatment })
            .OrderBy(g => g.Key.ScenarioId, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Treatment, StringComparer.Ordinal)
            .Select(g =>
            {
                var times = g.Select(r => r.ElapsedMs).ToArray();
                var sizes = g.Select(r => (double)r.ResponseBytes).ToArray();
                return new SummaryRow(
                    g.Key.ScenarioId,
                    g.Key.ScenarioName,
                    g.Key.Treatment,
                    g.Count(),
                    Statistics.Mean(times),
                    Statistics.Median(times),
                    Statistics.StandardDeviation(times),
                    times.Min(),
                    Statistics.Percentile(times, 0.95),
                    Statistics.Mean(sizes),
                    Statistics.Median(sizes),
                    Statistics.StandardDeviation(sizes),
                    sizes.Min(),
                    sizes.Max());
            })
            .ToList();
    }

    private static List<ComparisonRow> CompareTreatments(
        IReadOnlyList<TrialResult> results,
        IReadOnlyList<SummaryRow> summaries)
    {
        var comparisons = new List<ComparisonRow>();

        foreach (var scenarioGroup in results.GroupBy(r => new { r.ScenarioId, r.ScenarioName }).OrderBy(g => g.Key.ScenarioId))
        {
            comparisons.Add(BuildComparison(
                scenarioGroup.Key.ScenarioId,
                scenarioGroup.Key.ScenarioName,
                scenarioGroup.ToList(),
                summaries));
        }

        comparisons.Add(BuildComparison(
            "ALL",
            "Todos os cenarios",
            results.ToList(),
            summaries));

        return comparisons;
    }

    private static ComparisonRow BuildComparison(
        string scenarioId,
        string scenarioName,
        IReadOnlyList<TrialResult> rows,
        IReadOnlyList<SummaryRow> summaries)
    {
        var restRows = rows.Where(r => r.Treatment == nameof(ApiTreatment.REST)).ToArray();
        var graphQlRows = rows.Where(r => r.Treatment == nameof(ApiTreatment.GraphQL)).ToArray();

        var restTimes = restRows.Select(r => r.ElapsedMs).ToArray();
        var graphQlTimes = graphQlRows.Select(r => r.ElapsedMs).ToArray();
        var restSizes = restRows.Select(r => (double)r.ResponseBytes).ToArray();
        var graphQlSizes = graphQlRows.Select(r => (double)r.ResponseBytes).ToArray();

        var restMedianMs = Statistics.Median(restTimes);
        var graphQlMedianMs = Statistics.Median(graphQlTimes);
        var restMedianBytes = Statistics.Median(restSizes);
        var graphQlMedianBytes = Statistics.Median(graphQlSizes);

        var timeTest = Statistics.MannWhitney(graphQlTimes, restTimes);
        var sizeTest = Statistics.MannWhitney(graphQlSizes, restSizes);

        var timeReduction = PercentageReduction(restMedianMs, graphQlMedianMs);
        var sizeReduction = PercentageReduction(restMedianBytes, graphQlMedianBytes);

        return new ComparisonRow(
            scenarioId,
            scenarioName,
            restMedianMs,
            graphQlMedianMs,
            timeReduction,
            timeTest.PValue,
            timeTest.CliffsDelta,
            restMedianBytes,
            graphQlMedianBytes,
            sizeReduction,
            sizeTest.PValue,
            sizeTest.CliffsDelta,
            InterpretVerdict(graphQlMedianMs < restMedianMs, timeTest.PValue),
            InterpretVerdict(graphQlMedianBytes < restMedianBytes, sizeTest.PValue));
    }

    private static double PercentageReduction(double baseline, double observed)
    {
        if (Math.Abs(baseline) < double.Epsilon)
        {
            return 0;
        }

        return (baseline - observed) / baseline * 100.0;
    }

    private static string InterpretVerdict(bool expectedDirection, double pValue)
    {
        if (expectedDirection && pValue < 0.05)
        {
            return "H1 apoiada";
        }

        if (expectedDirection)
        {
            return "Tendencia sem significancia";
        }

        return "H0 nao rejeitada";
    }

    private static string BuildExecutionMetadata(int measurementCount)
    {
        var metadata = new
        {
            laboratory = "Lab05 - GraphQL vs REST",
            generatedAtUtc = DateTimeOffset.UtcNow,
            datasetSeed = DatasetSeed,
            trialSeed = TrialSeed,
            warmupIterations = WarmupIterations,
            trialsPerTreatment = TrialsPerTreatment,
            measurementCount,
            dotnetVersion = Environment.Version.ToString(),
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            processorCount = Environment.ProcessorCount,
            machineName = Environment.MachineName
        };

        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
    }

    internal static byte[] Serialize(object value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    internal static string Format(double value, int decimals = 4) => value.ToString($"F{decimals}", CsvCulture);
}

internal sealed record ScheduledTrial(ScenarioDefinition Scenario, ApiTreatment Treatment, int Trial);

internal sealed record TrialResult(
    string ScenarioId,
    string ScenarioName,
    string Treatment,
    int Trial,
    int ExecutionOrder,
    double ElapsedMs,
    int ResponseBytes,
    int Checksum);

internal sealed record SummaryRow(
    string ScenarioId,
    string ScenarioName,
    string Treatment,
    int N,
    double MeanMs,
    double MedianMs,
    double StdDevMs,
    double MinMs,
    double P95Ms,
    double MeanBytes,
    double MedianBytes,
    double StdDevBytes,
    double MinBytes,
    double MaxBytes);

internal sealed record ComparisonRow(
    string ScenarioId,
    string ScenarioName,
    double RestMedianMs,
    double GraphQlMedianMs,
    double TimeReductionPct,
    double TimePValue,
    double TimeCliffsDelta,
    double RestMedianBytes,
    double GraphQlMedianBytes,
    double SizeReductionPct,
    double SizePValue,
    double SizeCliffsDelta,
    string Rq1Verdict,
    string Rq2Verdict);

internal sealed class ScenarioDefinition
{
    public ScenarioDefinition(string id, string name, Func<ApiTreatment, byte[]> execute)
    {
        Id = id;
        Name = name;
        Execute = execute;
    }

    public string Id { get; }
    public string Name { get; }
    public Func<ApiTreatment, byte[]> Execute { get; }
}

internal static class ScenarioFactory
{
    public static List<ScenarioDefinition> Create(IReadOnlyList<RepositoryRecord> catalog)
    {
        return
        [
            new("Q1", "Lista de repositorios", treatment => treatment == ApiTreatment.REST
                ? Program.Serialize(RestListRepositories(catalog.Take(25)))
                : Program.Serialize(GraphQlListRepositories(catalog.Take(25)))),

            new("Q2", "Detalhe do repositorio", treatment => treatment == ApiTreatment.REST
                ? Program.Serialize(RestRepositoryDetail(catalog[12]))
                : Program.Serialize(GraphQlRepositoryDetail(catalog[12]))),

            new("Q3", "Issues recentes", treatment => treatment == ApiTreatment.REST
                ? Program.Serialize(RestIssues(catalog[25], 40))
                : Program.Serialize(GraphQlIssues(catalog[25], 40))),

            new("Q4", "Contribuidores", treatment => treatment == ApiTreatment.REST
                ? Program.Serialize(RestContributors(catalog[30], 30))
                : Program.Serialize(GraphQlContributors(catalog[30], 30))),

            new("Q5", "Visao aninhada", treatment => treatment == ApiTreatment.REST
                ? Program.Serialize(RestNestedOverview(catalog[44]))
                : Program.Serialize(GraphQlNestedOverview(catalog[44])))
        ];
    }

    private static object RestListRepositories(IEnumerable<RepositoryRecord> repositories) => new
    {
        endpoint = "/api/repos?limit=25",
        page = 1,
        perPage = 25,
        totalCount = 80,
        items = repositories.Select(RestRepositorySummary).ToArray()
    };

    private static object GraphQlListRepositories(IEnumerable<RepositoryRecord> repositories) => new
    {
        data = new
        {
            repositories = new
            {
                nodes = repositories.Select(repo => new
                {
                    repo.Id,
                    repo.Name,
                    owner = new { repo.Owner.Login },
                    repo.Stars,
                    repo.PrimaryLanguage
                })
            }
        }
    };

    private static object RestRepositoryDetail(RepositoryRecord repo) => new
    {
        endpoint = $"/api/repos/{repo.Owner.Login}/{repo.Name}",
        data = RestRepositoryFull(repo)
    };

    private static object GraphQlRepositoryDetail(RepositoryRecord repo) => new
    {
        data = new
        {
            repository = new
            {
                repo.Id,
                repo.Name,
                owner = new { repo.Owner.Login },
                repo.Stars,
                repo.Forks,
                repo.License,
                repo.Topics
            }
        }
    };

    private static object RestIssues(RepositoryRecord repo, int limit) => new
    {
        endpoint = $"/api/repos/{repo.Owner.Login}/{repo.Name}/issues?limit={limit}",
        totalCount = repo.Issues.Count,
        items = repo.Issues.Take(limit).Select(issue => new
        {
            issue.Id,
            issue.Number,
            issue.Title,
            issue.State,
            issue.Author,
            issue.CreatedAt,
            issue.UpdatedAt,
            issue.ClosedAt,
            issue.Labels,
            issue.Body,
            issue.CommentCount,
            issue.ReactionCount,
            issue.Assignee,
            issue.Milestone,
            issue.Timeline,
            htmlUrl = $"https://example.local/{repo.Owner.Login}/{repo.Name}/issues/{issue.Number}",
            apiUrl = $"https://api.example.local/repos/{repo.Owner.Login}/{repo.Name}/issues/{issue.Number}"
        }).ToArray()
    };

    private static object GraphQlIssues(RepositoryRecord repo, int limit) => new
    {
        data = new
        {
            repository = new
            {
                issues = new
                {
                    nodes = repo.Issues.Take(limit).Select(issue => new
                    {
                        issue.Id,
                        issue.Title,
                        issue.State,
                        author = new { login = issue.Author },
                        issue.CreatedAt,
                        issue.Labels
                    }).ToArray()
                }
            }
        }
    };

    private static object RestContributors(RepositoryRecord repo, int limit) => new
    {
        endpoint = $"/api/repos/{repo.Owner.Login}/{repo.Name}/contributors?limit={limit}",
        totalCount = repo.Contributors.Count,
        items = repo.Contributors.Take(limit).Select(contributor => new
        {
            contributor.Id,
            contributor.Login,
            contributor.Name,
            contributor.Company,
            contributor.Location,
            contributor.EmailHash,
            contributor.Contributions,
            contributor.Followers,
            contributor.Following,
            contributor.PublicRepos,
            contributor.AvatarUrl,
            contributor.ProfileUrl,
            contributor.Bio,
            permissions = new
            {
                push = contributor.Contributions > 80,
                maintain = contributor.Contributions > 120,
                admin = contributor.Contributions > 180
            }
        }).ToArray()
    };

    private static object GraphQlContributors(RepositoryRecord repo, int limit) => new
    {
        data = new
        {
            repository = new
            {
                contributors = new
                {
                    nodes = repo.Contributors.Take(limit).Select(contributor => new
                    {
                        contributor.Login,
                        contributor.Contributions,
                        contributor.Company
                    }).ToArray()
                }
            }
        }
    };

    private static object RestNestedOverview(RepositoryRecord repo) => new
    {
        endpointComposition = new[]
        {
            $"/api/repos/{repo.Owner.Login}/{repo.Name}",
            $"/api/repos/{repo.Owner.Login}/{repo.Name}/issues?limit=10",
            $"/api/repos/{repo.Owner.Login}/{repo.Name}/contributors?limit=10",
            $"/api/repos/{repo.Owner.Login}/{repo.Name}/releases/latest"
        },
        requestCount = 4,
        responses = new object[]
        {
            RestRepositoryDetail(repo),
            RestIssues(repo, 10),
            RestContributors(repo, 10),
            new
            {
                endpoint = $"/api/repos/{repo.Owner.Login}/{repo.Name}/releases/latest",
                data = repo.LatestRelease
            }
        }
    };

    private static object GraphQlNestedOverview(RepositoryRecord repo) => new
    {
        data = new
        {
            repository = new
            {
                repo.Id,
                repo.Name,
                owner = new { repo.Owner.Login },
                repo.Stars,
                repo.PrimaryLanguage,
                issues = new
                {
                    nodes = repo.Issues.Take(10).Select(issue => new
                    {
                        issue.Title,
                        issue.State,
                        author = new { login = issue.Author }
                    }).ToArray()
                },
                contributors = new
                {
                    nodes = repo.Contributors.Take(10).Select(contributor => new
                    {
                        contributor.Login,
                        contributor.Contributions
                    }).ToArray()
                },
                latestRelease = new
                {
                    repo.LatestRelease.TagName,
                    repo.LatestRelease.PublishedAt
                }
            }
        }
    };

    private static object RestRepositorySummary(RepositoryRecord repo) => new
    {
        repo.Id,
        repo.Name,
        repo.Description,
        owner = repo.Owner,
        repo.Stars,
        repo.Forks,
        repo.Watchers,
        repo.OpenIssues,
        repo.PrimaryLanguage,
        repo.CreatedAt,
        repo.UpdatedAt,
        repo.DefaultBranch,
        repo.IsArchived,
        repo.SizeKb,
        repo.License,
        repo.Topics,
        urls = new
        {
            html = $"https://example.local/{repo.Owner.Login}/{repo.Name}",
            clone = $"https://example.local/{repo.Owner.Login}/{repo.Name}.git",
            issues = $"https://api.example.local/repos/{repo.Owner.Login}/{repo.Name}/issues",
            contributors = $"https://api.example.local/repos/{repo.Owner.Login}/{repo.Name}/contributors"
        }
    };

    private static object RestRepositoryFull(RepositoryRecord repo) => new
    {
        summary = RestRepositorySummary(repo),
        repo.ReadmeExcerpt,
        repo.SecurityPolicy,
        repo.DefaultBranch,
        repo.NetworkCount,
        repo.SubscribersCount,
        repo.PullRequestsCount,
        repo.ReleasesCount,
        latestRelease = repo.LatestRelease,
        issuePreview = repo.Issues.Take(5),
        contributorPreview = repo.Contributors.Take(5),
        permissions = new { admin = false, maintain = false, push = true, triage = true, pull = true }
    };
}

internal static class SyntheticCatalog
{
    public static List<RepositoryRecord> Create(int seed)
    {
        var random = new Random(seed);
        var languages = new[] { "C#", "JavaScript", "Python", "Java", "Go", "Rust", "TypeScript", "Kotlin" };
        var licenses = new[] { "MIT", "Apache-2.0", "BSD-3-Clause", "GPL-3.0", "MPL-2.0" };
        var topics = new[] { "api", "cli", "cloud", "data", "devops", "security", "observability", "testing", "graphql", "rest", "microservices", "web" };
        var states = new[] { "OPEN", "CLOSED" };
        var catalog = new List<RepositoryRecord>();
        var baseDate = new DateTimeOffset(2021, 1, 1, 8, 0, 0, TimeSpan.Zero);

        for (var i = 1; i <= 80; i++)
        {
            var owner = new OwnerRecord(
                Id: 1000 + i,
                Login: $"org-{((i - 1) % 12) + 1:00}",
                Type: i % 5 == 0 ? "User" : "Organization",
                AvatarUrl: $"https://images.example.local/org-{i:00}.png",
                Url: $"https://example.local/org-{i:00}");

            var repo = new RepositoryRecord
            {
                Id = 10_000 + i,
                Name = $"service-{i:000}",
                Description = $"Repositorio sintetico {i:000} usado para comparar estrategias de consulta em APIs web.",
                Owner = owner,
                Stars = random.Next(150, 85_000),
                Forks = random.Next(20, 9_500),
                Watchers = random.Next(10, 5_000),
                OpenIssues = random.Next(0, 900),
                PrimaryLanguage = languages[random.Next(languages.Length)],
                CreatedAt = baseDate.AddDays(-random.Next(400, 2_800)),
                UpdatedAt = baseDate.AddDays(random.Next(1, 1_600)),
                DefaultBranch = i % 7 == 0 ? "develop" : "main",
                IsArchived = i % 37 == 0,
                SizeKb = random.Next(1_200, 240_000),
                License = licenses[random.Next(licenses.Length)],
                Topics = PickMany(random, topics, 5),
                ReadmeExcerpt = BuildParagraph(random, 9),
                SecurityPolicy = BuildParagraph(random, 5),
                NetworkCount = random.Next(30, 25_000),
                SubscribersCount = random.Next(20, 18_000),
                PullRequestsCount = random.Next(150, 8_500),
                ReleasesCount = random.Next(3, 80),
                LatestRelease = new ReleaseRecord(
                    TagName: $"v{random.Next(1, 9)}.{random.Next(0, 20)}.{random.Next(0, 15)}",
                    Name: $"Release estavel {i:000}",
                    PublishedAt: baseDate.AddDays(random.Next(1, 1_600)),
                    Body: BuildParagraph(random, 7),
                    Assets: random.Next(1, 8),
                    DownloadCount: random.Next(100, 250_000))
            };

            for (var issue = 1; issue <= 75; issue++)
            {
                var createdAt = repo.CreatedAt.AddDays(random.Next(1, 1_900));
                repo.Issues.Add(new IssueRecord(
                    Id: repo.Id * 1_000 + issue,
                    Number: issue,
                    Title: $"Ajuste de comportamento no fluxo {issue:000}",
                    State: states[random.Next(states.Length)],
                    Author: $"dev-{random.Next(1, 140):000}",
                    CreatedAt: createdAt,
                    UpdatedAt: createdAt.AddDays(random.Next(1, 60)),
                    ClosedAt: issue % 3 == 0 ? createdAt.AddDays(random.Next(2, 90)) : null,
                    Labels: PickMany(random, new[] { "bug", "feature", "docs", "performance", "security", "good-first-issue", "api" }, 3),
                    Body: BuildParagraph(random, 8),
                    CommentCount: random.Next(0, 80),
                    ReactionCount: random.Next(0, 450),
                    Assignee: $"dev-{random.Next(1, 140):000}",
                    Milestone: $"M{random.Next(1, 12)}",
                    Timeline: BuildTimeline(random, createdAt)));
            }

            for (var contributor = 1; contributor <= 48; contributor++)
            {
                repo.Contributors.Add(new ContributorRecord(
                    Id: repo.Id * 100 + contributor,
                    Login: $"contrib-{random.Next(1, 500):000}",
                    Name: $"Pessoa Contribuidora {random.Next(1, 500):000}",
                    Company: contributor % 4 == 0 ? "Independent" : $"Empresa {random.Next(1, 30):00}",
                    Location: $"Cidade {random.Next(1, 60):00}",
                    EmailHash: $"{random.NextInt64():x}",
                    Contributions: random.Next(1, 260),
                    Followers: random.Next(0, 18_000),
                    Following: random.Next(0, 2_000),
                    PublicRepos: random.Next(1, 350),
                    AvatarUrl: $"https://images.example.local/users/{repo.Id}-{contributor}.png",
                    ProfileUrl: $"https://example.local/contrib-{contributor:000}",
                    Bio: BuildParagraph(random, 4)));
            }

            catalog.Add(repo);
        }

        return catalog;
    }

    private static string[] PickMany(Random random, IReadOnlyList<string> values, int count)
    {
        return values
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToArray();
    }

    private static string BuildParagraph(Random random, int sentences)
    {
        var fragments = new[]
        {
            "Este registro contem metadados adicionais para simular respostas completas",
            "A carga inclui detalhes que muitas vezes nao sao necessarios para a tela cliente",
            "O objetivo e preservar reproducibilidade mantendo variacao suficiente entre objetos",
            "Campos textuais aumentam o tamanho do payload e evidenciam over-fetching",
            "A mesma fonte de dados alimenta os dois tratamentos do experimento"
        };

        var builder = new StringBuilder();
        for (var i = 0; i < sentences; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(fragments[random.Next(fragments.Length)]);
            builder.Append('.');
        }

        return builder.ToString();
    }

    private static string[] BuildTimeline(Random random, DateTimeOffset createdAt)
    {
        var events = new[] { "labeled", "assigned", "commented", "referenced", "renamed", "closed", "reopened" };
        return Enumerable.Range(1, random.Next(3, 9))
            .Select(i => $"{createdAt.AddHours(i * random.Next(2, 20)):O}:{events[random.Next(events.Length)]}")
            .ToArray();
    }
}

internal sealed class RepositoryRecord
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public OwnerRecord Owner { get; init; } = new(0, "", "", "", "");
    public int Stars { get; init; }
    public int Forks { get; init; }
    public int Watchers { get; init; }
    public int OpenIssues { get; init; }
    public string PrimaryLanguage { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string DefaultBranch { get; init; } = "";
    public bool IsArchived { get; init; }
    public int SizeKb { get; init; }
    public string License { get; init; } = "";
    public string[] Topics { get; init; } = [];
    public string ReadmeExcerpt { get; init; } = "";
    public string SecurityPolicy { get; init; } = "";
    public int NetworkCount { get; init; }
    public int SubscribersCount { get; init; }
    public int PullRequestsCount { get; init; }
    public int ReleasesCount { get; init; }
    public ReleaseRecord LatestRelease { get; init; } = new("", "", DateTimeOffset.MinValue, "", 0, 0);
    public List<IssueRecord> Issues { get; } = [];
    public List<ContributorRecord> Contributors { get; } = [];
}

internal sealed record OwnerRecord(int Id, string Login, string Type, string AvatarUrl, string Url);

internal sealed record ReleaseRecord(
    string TagName,
    string Name,
    DateTimeOffset PublishedAt,
    string Body,
    int Assets,
    int DownloadCount);

internal sealed record IssueRecord(
    int Id,
    int Number,
    string Title,
    string State,
    string Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    string[] Labels,
    string Body,
    int CommentCount,
    int ReactionCount,
    string Assignee,
    string Milestone,
    string[] Timeline);

internal sealed record ContributorRecord(
    int Id,
    string Login,
    string Name,
    string Company,
    string Location,
    string EmailHash,
    int Contributions,
    int Followers,
    int Following,
    int PublicRepos,
    string AvatarUrl,
    string ProfileUrl,
    string Bio);

internal static class Statistics
{
    public static double Mean(IReadOnlyList<double> values) => values.Count == 0 ? 0 : values.Average();

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2.0
            : sorted[middle];
    }

    public static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var mean = Mean(values);
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }

    public static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToArray();
        var position = (sorted.Length - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
        {
            return sorted[lower];
        }

        var weight = position - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    public static MannWhitneyResult MannWhitney(IReadOnlyList<double> groupA, IReadOnlyList<double> groupB)
    {
        var combined = groupA.Select(value => new RankedValue(value, 0))
            .Concat(groupB.Select(value => new RankedValue(value, 1)))
            .OrderBy(item => item.Value)
            .ToArray();

        var ranks = new double[combined.Length];
        var index = 0;
        while (index < combined.Length)
        {
            var tieEnd = index + 1;
            while (tieEnd < combined.Length && Math.Abs(combined[tieEnd].Value - combined[index].Value) < 0.000000001)
            {
                tieEnd++;
            }

            var averageRank = (index + 1 + tieEnd) / 2.0;
            for (var i = index; i < tieEnd; i++)
            {
                ranks[i] = averageRank;
            }

            index = tieEnd;
        }

        var rankSumA = 0.0;
        for (var i = 0; i < combined.Length; i++)
        {
            if (combined[i].Group == 0)
            {
                rankSumA += ranks[i];
            }
        }

        var n1 = groupA.Count;
        var n2 = groupB.Count;
        var u1 = rankSumA - n1 * (n1 + 1) / 2.0;
        var u2 = n1 * n2 - u1;
        var u = Math.Min(u1, u2);
        var mean = n1 * n2 / 2.0;
        var standardDeviation = Math.Sqrt(n1 * n2 * (n1 + n2 + 1) / 12.0);
        var z = standardDeviation == 0 ? 0 : (u - mean) / standardDeviation;
        var pValue = 2.0 * NormalCdf(-Math.Abs(z));

        return new MannWhitneyResult(u, z, Math.Clamp(pValue, 0, 1), CliffsDelta(groupA, groupB));
    }

    private static double CliffsDelta(IReadOnlyList<double> groupA, IReadOnlyList<double> groupB)
    {
        var greater = 0L;
        var lower = 0L;

        foreach (var a in groupA)
        {
            foreach (var b in groupB)
            {
                if (a > b)
                {
                    greater++;
                }
                else if (a < b)
                {
                    lower++;
                }
            }
        }

        return (greater - lower) / (double)(groupA.Count * groupB.Count);
    }

    private static double NormalCdf(double x) => 0.5 * (1.0 + Erf(x / Math.Sqrt(2.0)));

    private static double Erf(double x)
    {
        var sign = Math.Sign(x);
        x = Math.Abs(x);

        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
        return sign * y;
    }

    private sealed record RankedValue(double Value, int Group);
}

internal sealed record MannWhitneyResult(double U, double Z, double PValue, double CliffsDelta);

internal static class CsvWriters
{
    public static string BuildMeasurements(IEnumerable<TrialResult> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("scenario_id;scenario_name;treatment;trial;execution_order;elapsed_ms;response_bytes;checksum");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(';',
                row.ScenarioId,
                row.ScenarioName,
                row.Treatment,
                row.Trial.ToString(CultureInfo.InvariantCulture),
                row.ExecutionOrder.ToString(CultureInfo.InvariantCulture),
                Program.Format(row.ElapsedMs, 6),
                row.ResponseBytes.ToString(CultureInfo.InvariantCulture),
                row.Checksum.ToString(CultureInfo.InvariantCulture)));
        }

        return builder.ToString();
    }

    public static string BuildSummaries(IEnumerable<SummaryRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("scenario_id;scenario_name;treatment;n;mean_ms;median_ms;stddev_ms;min_ms;p95_ms;mean_bytes;median_bytes;stddev_bytes;min_bytes;max_bytes");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(';',
                row.ScenarioId,
                row.ScenarioName,
                row.Treatment,
                row.N.ToString(CultureInfo.InvariantCulture),
                Program.Format(row.MeanMs, 6),
                Program.Format(row.MedianMs, 6),
                Program.Format(row.StdDevMs, 6),
                Program.Format(row.MinMs, 6),
                Program.Format(row.P95Ms, 6),
                Program.Format(row.MeanBytes, 2),
                Program.Format(row.MedianBytes, 2),
                Program.Format(row.StdDevBytes, 2),
                Program.Format(row.MinBytes, 2),
                Program.Format(row.MaxBytes, 2)));
        }

        return builder.ToString();
    }

    public static string BuildComparisons(IEnumerable<ComparisonRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("scenario_id;scenario_name;rest_median_ms;graphql_median_ms;time_reduction_pct;time_p_value;time_cliffs_delta;rest_median_bytes;graphql_median_bytes;size_reduction_pct;size_p_value;size_cliffs_delta;rq1_verdict;rq2_verdict");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(';',
                row.ScenarioId,
                row.ScenarioName,
                Program.Format(row.RestMedianMs, 6),
                Program.Format(row.GraphQlMedianMs, 6),
                Program.Format(row.TimeReductionPct, 2),
                Program.Format(row.TimePValue, 8),
                Program.Format(row.TimeCliffsDelta, 4),
                Program.Format(row.RestMedianBytes, 2),
                Program.Format(row.GraphQlMedianBytes, 2),
                Program.Format(row.SizeReductionPct, 2),
                Program.Format(row.SizePValue, 8),
                Program.Format(row.SizeCliffsDelta, 4),
                row.Rq1Verdict,
                row.Rq2Verdict));
        }

        return builder.ToString();
    }
}

internal static class PdfReportBuilder
{
    private const double PageWidth = 841.89;
    private const double PageHeight = 595.28;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Encoding PdfEncoding = Encoding.Latin1;

    public static void Generate(
        string outputPath,
        IReadOnlyList<SummaryRow> summaries,
        IReadOnlyList<ComparisonRow> comparisons)
    {
        var aggregate = comparisons.Single(row => row.ScenarioId == "ALL");
        var scenarioRows = comparisons.Where(row => row.ScenarioId != "ALL").ToArray();

        var pages = new[]
        {
            BuildFirstPage(summaries),
            BuildSecondPage(scenarioRows, aggregate)
        };

        File.WriteAllBytes(outputPath, BuildPdf(pages));
    }

    private static string BuildFirstPage(IReadOnlyList<SummaryRow> summaries)
    {
        var canvas = new PdfCanvas();
        canvas.Text("Relatorio Final - Lab05", 322, 552, 18, "F2", Color(37, 56, 88));
        canvas.Text("GraphQL vs REST - Um experimento controlado", 48, 522, 16, "F2");
        canvas.Text("Engenharia de Software - Laboratorio de Experimentacao de Software", 48, 501, 10.5);

        canvas.Text("1. Introducao", 48, 465, 13.5, "F2", Color(31, 138, 112));
        canvas.Wrap("Este relatorio apresenta um experimento controlado para comparar REST e GraphQL em tempo de resposta e tamanho do payload. As hipoteses alternativas avaliam se GraphQL e mais rapido e se retorna respostas menores que REST.", 48, 446, 735, 10.5, 13);

        canvas.Text("2. Metodologia", 48, 397, 13.5, "F2", Color(31, 138, 112));
        var y = 377.0;
        foreach (var item in new[]
        {
            "Projeto intra-sujeitos: cada cenario foi executado com REST e GraphQL.",
            "Tratamentos: REST com recursos completos e GraphQL com projecao explicita de campos.",
            "Objetos experimentais: catalogo local deterministico de repositorios, issues, releases e contribuidores.",
            "Medicoes: 1.200 trials, com 120 repeticoes por tratamento em cada cenario.",
            "Teste estatistico: Mann-Whitney U bilateral, alfa = 0,05, com Cliff's delta."
        })
        {
            canvas.Text("- " + item, 48, y, 9.8);
            y -= 17;
        }

        canvas.Text("3. Resultados descritivos", 48, 275, 13.5, "F2", Color(31, 138, 112));
        var rows = summaries.Select(row => new[]
        {
            $"{row.ScenarioId} - {row.ScenarioName}",
            row.Treatment,
            row.N.ToString(CultureInfo.InvariantCulture),
            row.MedianMs.ToString("F6", CultureInfo.InvariantCulture),
            row.P95Ms.ToString("F6", CultureInfo.InvariantCulture),
            row.MedianBytes.ToString("F0", CultureInfo.InvariantCulture)
        }).ToList();

        canvas.Table(
            x: 165,
            yTop: 257,
            widths: [176, 64, 34, 68, 68, 82],
            headers: ["Cenario", "Tratamento", "n", "Mediana ms", "P95 ms", "Mediana bytes"],
            rows: rows,
            rowHeight: 16,
            fontSize: 7.1,
            headerColor: Color(37, 56, 88));

        canvas.Footer(1);
        return canvas.ToString();
    }

    private static string BuildSecondPage(IReadOnlyList<ComparisonRow> scenarioRows, ComparisonRow aggregate)
    {
        var canvas = new PdfCanvas();
        canvas.Text("4. Testes e respostas as RQs", 48, 535, 13.5, "F2", Color(31, 138, 112));

        var rows = scenarioRows.Select(row => new[]
        {
            $"{row.ScenarioId} - {row.ScenarioName}",
            $"{row.TimeReductionPct.ToString("F2", CultureInfo.InvariantCulture)}%",
            P(row.TimePValue),
            $"{row.SizeReductionPct.ToString("F2", CultureInfo.InvariantCulture)}%",
            P(row.SizePValue)
        }).ToList();

        rows.Add([
            "Todos",
            $"{aggregate.TimeReductionPct.ToString("F2", CultureInfo.InvariantCulture)}%",
            P(aggregate.TimePValue),
            $"{aggregate.SizeReductionPct.ToString("F2", CultureInfo.InvariantCulture)}%",
            P(aggregate.SizePValue)
        ]);

        canvas.Table(
            x: 148,
            yTop: 515,
            widths: [204, 88, 70, 96, 70],
            headers: ["Cenario", "Reducao tempo", "p tempo", "Reducao tamanho", "p tamanho"],
            rows: rows,
            rowHeight: 18,
            fontSize: 8,
            headerColor: Color(31, 138, 112),
            boldLastRow: true);

        canvas.Wrap(
            $"RQ1: GraphQL apresentou menor mediana de tempo em 5 de 5 cenarios com p < 0,05. No agregado, a reducao foi de {aggregate.TimeReductionPct.ToString("F2", PtBr)}% (p < 0,0001). Assim, H1-1 e apoiada neste experimento.",
            48,
            365,
            735,
            10.3,
            13);

        canvas.Wrap(
            $"RQ2: GraphQL apresentou menor tamanho de resposta em 5 de 5 cenarios com p < 0,05. No agregado, a reducao foi de {aggregate.SizeReductionPct.ToString("F2", PtBr)}% (p < 0,0001). Assim, H1-2 e apoiada neste experimento.",
            48,
            318,
            735,
            10.3,
            13);

        canvas.Text("5. Discussao e ameacas a validade", 48, 270, 13.5, "F2", Color(31, 138, 112));
        canvas.Wrap("Os resultados favorecem GraphQL principalmente por reduzir over-fetching: o cliente declara os campos necessarios e recebe payloads menores. Como a execucao foi local, a conclusao isola o efeito de payload e processamento JSON, mas nao captura latencia de rede, cache HTTP, compressao ou overhead de servidores GraphQL reais.", 48, 251, 735, 10.3, 13);
        canvas.Wrap("Portanto, a conclusao e valida para o desenho controlado proposto e deve ser replicada em APIs reais antes de generalizacoes amplas.", 48, 200, 735, 10.3, 13);

        canvas.Text("6. Conclusao", 48, 160, 13.5, "F2", Color(31, 138, 112));
        canvas.Wrap("No experimento controlado, GraphQL reduziu a mediana agregada de tempo e de tamanho das respostas em relacao a REST. As evidencias estatisticas apoiam as duas hipoteses alternativas formuladas para o Lab05.", 48, 141, 735, 10.3, 13);

        canvas.Footer(2);
        return canvas.ToString();
    }

    private static byte[] BuildPdf(IReadOnlyList<string> pageContents)
    {
        var objects = new List<byte[]>();
        AddObject(objects, "<< /Type /Catalog /Pages 2 0 R >>");

        var kids = string.Join(' ', Enumerable.Range(0, pageContents.Count).Select(i => $"{5 + i * 2} 0 R"));
        AddObject(objects, $"<< /Type /Pages /Kids [{kids}] /Count {pageContents.Count} >>");
        AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        for (var i = 0; i < pageContents.Count; i++)
        {
            var pageObjectNumber = 5 + i * 2;
            var contentObjectNumber = pageObjectNumber + 1;
            AddObject(objects, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString("F2", CultureInfo.InvariantCulture)} {PageHeight.ToString("F2", CultureInfo.InvariantCulture)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            AddStreamObject(objects, PdfEncoding.GetBytes(pageContents[i]));
        }

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{i + 1} 0 obj\n");
            stream.Write(objects[i]);
            WriteAscii(stream, "\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    private static void AddObject(List<byte[]> objects, string value)
    {
        objects.Add(PdfEncoding.GetBytes(value));
    }

    private static void AddStreamObject(List<byte[]> objects, byte[] stream)
    {
        using var output = new MemoryStream();
        WriteAscii(output, $"<< /Length {stream.Length} >>\nstream\n");
        output.Write(stream);
        WriteAscii(output, "\nendstream");
        objects.Add(output.ToArray());
    }

    private static void WriteAscii(Stream stream, string value)
    {
        stream.Write(Encoding.ASCII.GetBytes(value));
    }

    private static string P(double value) => value < 0.0001 ? "< 0,0001" : value.ToString("F4", PtBr);

    private static PdfColor Color(int r, int g, int b) => new(r / 255.0, g / 255.0, b / 255.0);

    private readonly record struct PdfColor(double R, double G, double B);

    private sealed class PdfCanvas
    {
        private readonly StringBuilder _content = new();

        public void Text(string text, double x, double y, double size, string font = "F1", PdfColor? color = null)
        {
            var selectedColor = color ?? Color(0, 0, 0);
            _content.AppendLine($"{selectedColor.R:F3} {selectedColor.G:F3} {selectedColor.B:F3} rg");
            _content.AppendLine($"BT /{font} {size.ToString("F2", CultureInfo.InvariantCulture)} Tf {x.ToString("F2", CultureInfo.InvariantCulture)} {y.ToString("F2", CultureInfo.InvariantCulture)} Td ({Escape(text)}) Tj ET");
        }

        public void Wrap(string text, double x, double y, double width, double size, double leading)
        {
            var currentY = y;
            foreach (var line in WrapLines(text, width, size))
            {
                Text(line, x, currentY, size);
                currentY -= leading;
            }
        }

        public void Table(
            double x,
            double yTop,
            double[] widths,
            string[] headers,
            IReadOnlyList<string[]> rows,
            double rowHeight,
            double fontSize,
            PdfColor headerColor,
            bool boldLastRow = false)
        {
            DrawRow(x, yTop, widths, rowHeight, headerColor, true);
            DrawCells(x, yTop - rowHeight + 5, widths, headers, fontSize, "F2", Color(255, 255, 255));

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var y = yTop - rowHeight * (rowIndex + 1);
                var fill = rowIndex % 2 == 0 ? Color(255, 255, 255) : Color(244, 247, 251);
                DrawRow(x, y, widths, rowHeight, fill, false);
                var font = boldLastRow && rowIndex == rows.Count - 1 ? "F2" : "F1";
                DrawCells(x, y - rowHeight + 5, widths, rows[rowIndex], fontSize, font, Color(0, 0, 0));
            }

            DrawGrid(x, yTop, widths, rowHeight, rows.Count + 1);
        }

        public void Footer(int page)
        {
            Text("Lab05 - GraphQL vs REST", 42, 25, 8, "F1", Color(85, 99, 118));
            Text($"Pagina {page}", 760, 25, 8, "F1", Color(85, 99, 118));
        }

        public override string ToString() => _content.ToString();

        private void DrawRow(double x, double yTop, double[] widths, double height, PdfColor fill, bool header)
        {
            var totalWidth = widths.Sum();
            var y = yTop - height;
            _content.AppendLine($"{fill.R:F3} {fill.G:F3} {fill.B:F3} rg");
            _content.AppendLine($"{x:F2} {y:F2} {totalWidth:F2} {height:F2} re f");
            if (header)
            {
                _content.AppendLine("0.145 0.220 0.345 RG");
            }
        }

        private void DrawGrid(double x, double yTop, double[] widths, double rowHeight, int rowCount)
        {
            var totalWidth = widths.Sum();
            var totalHeight = rowHeight * rowCount;
            _content.AppendLine("0.820 0.855 0.905 RG 0.35 w");

            for (var i = 0; i <= rowCount; i++)
            {
                var y = yTop - i * rowHeight;
                _content.AppendLine($"{x:F2} {y:F2} m {x + totalWidth:F2} {y:F2} l S");
            }

            var currentX = x;
            _content.AppendLine($"{currentX:F2} {yTop:F2} m {currentX:F2} {yTop - totalHeight:F2} l S");
            foreach (var width in widths)
            {
                currentX += width;
                _content.AppendLine($"{currentX:F2} {yTop:F2} m {currentX:F2} {yTop - totalHeight:F2} l S");
            }
        }

        private void DrawCells(double x, double baseline, double[] widths, string[] values, double size, string font, PdfColor color)
        {
            var currentX = x;
            for (var i = 0; i < values.Length; i++)
            {
                var text = Truncate(values[i], widths[i], size);
                Text(text, currentX + 6, baseline, size, font, color);
                currentX += widths[i];
            }
        }

        private static IEnumerable<string> WrapLines(string text, double width, double size)
        {
            var maxChars = Math.Max(20, (int)(width / (size * 0.48)));
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();

            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > maxChars)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                {
                    line.Append(' ');
                }

                line.Append(word);
            }

            if (line.Length > 0)
            {
                yield return line.ToString();
            }
        }

        private static string Truncate(string value, double width, double size)
        {
            var maxChars = Math.Max(4, (int)(width / (size * 0.52)));
            return value.Length <= maxChars ? value : value[..Math.Max(1, maxChars - 1)] + ".";
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }
    }
}

internal static class ReportBuilder
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Build(
        IReadOnlyList<TrialResult> results,
        IReadOnlyList<SummaryRow> summaries,
        IReadOnlyList<ComparisonRow> comparisons)
    {
        var aggregate = comparisons.Single(row => row.ScenarioId == "ALL");
        var builder = new StringBuilder();

        builder.AppendLine("# Relatorio Final - Lab05: GraphQL vs REST");
        builder.AppendLine();
        builder.AppendLine("## 1. Introducao");
        builder.AppendLine();
        builder.AppendLine("Este relatorio apresenta um experimento controlado para comparar REST e GraphQL em dois criterios quantitativos: tempo de resposta e tamanho do payload. O objetivo e responder as perguntas RQ1 e RQ2 propostas no enunciado do Laboratorio 05.");
        builder.AppendLine();
        builder.AppendLine("As hipoteses alternativas adotadas foram: GraphQL possui menor tempo de resposta que REST (H1-1) e GraphQL possui menor tamanho de resposta que REST (H1-2).");
        builder.AppendLine();

        builder.AppendLine("## 2. Metodologia");
        builder.AppendLine();
        builder.AppendLine("- **Tipo de projeto:** intra-sujeitos; cada cenario foi executado com os dois tratamentos.");
        builder.AppendLine("- **Tratamentos:** REST com recursos completos e GraphQL com projecao explicita de campos.");
        builder.AppendLine("- **Objetos experimentais:** catalogo local deterministico com repositorios, issues, releases e contribuidores.");
        builder.AppendLine($"- **Medicoes:** {results.Count.ToString("N0", PtBr)} trials coletados, com {TrialsPerTreatment()} repeticoes por tratamento em cada cenario.");
        builder.AppendLine("- **Aquecimento:** 20 execucoes por tratamento/cenario antes da coleta.");
        builder.AppendLine("- **Metrica RQ1:** tempo de resposta em milissegundos, incluindo montagem, serializacao UTF-8 e consumo JSON.");
        builder.AppendLine("- **Metrica RQ2:** tamanho da resposta em bytes UTF-8.");
        builder.AppendLine("- **Teste estatistico:** Mann-Whitney U bilateral, alfa = 0,05, com Cliff's delta como tamanho de efeito.");
        builder.AppendLine();

        builder.AppendLine("### Ambiente de execucao");
        builder.AppendLine();
        builder.AppendLine($"- .NET: {Environment.Version}");
        builder.AppendLine($"- Sistema operacional: {RuntimeInformation.OSDescription}");
        builder.AppendLine($"- Arquitetura: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine($"- Processadores logicos: {Environment.ProcessorCount}");
        builder.AppendLine($"- Semente do dataset: {20260624}");
        builder.AppendLine($"- Semente dos trials: {5052026}");
        builder.AppendLine();

        builder.AppendLine("## 3. Resultados descritivos");
        builder.AppendLine();
        builder.AppendLine("| Cenario | Tratamento | n | Mediana tempo (ms) | P95 tempo (ms) | Mediana tamanho (bytes) |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (var row in summaries)
        {
            builder.AppendLine($"| {row.ScenarioId} - {row.ScenarioName} | {row.Treatment} | {row.N} | {row.MedianMs.ToString("F4", PtBr)} | {row.P95Ms.ToString("F4", PtBr)} | {row.MedianBytes.ToString("N0", PtBr)} |");
        }
        builder.AppendLine();

        builder.AppendLine("## 4. Testes e respostas as RQs");
        builder.AppendLine();
        builder.AppendLine("| Cenario | Reducao tempo GraphQL vs REST | p tempo | Cliff tempo | Reducao tamanho GraphQL vs REST | p tamanho | Cliff tamanho |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var row in comparisons.Where(row => row.ScenarioId != "ALL"))
        {
            builder.AppendLine($"| {row.ScenarioId} - {row.ScenarioName} | {row.TimeReductionPct.ToString("F2", PtBr)}% | {P(row.TimePValue)} | {row.TimeCliffsDelta.ToString("F3", PtBr)} | {row.SizeReductionPct.ToString("F2", PtBr)}% | {P(row.SizePValue)} | {row.SizeCliffsDelta.ToString("F3", PtBr)} |");
        }
        builder.AppendLine($"| **Todos** | **{aggregate.TimeReductionPct.ToString("F2", PtBr)}%** | **{P(aggregate.TimePValue)}** | **{aggregate.TimeCliffsDelta.ToString("F3", PtBr)}** | **{aggregate.SizeReductionPct.ToString("F2", PtBr)}%** | **{P(aggregate.SizePValue)}** | **{aggregate.SizeCliffsDelta.ToString("F3", PtBr)}** |");
        builder.AppendLine();

        builder.AppendLine("### RQ1 - Respostas GraphQL sao mais rapidas?");
        builder.AppendLine();
        builder.AppendLine(BuildRq1Answer(comparisons));
        builder.AppendLine();
        builder.AppendLine("### RQ2 - Respostas GraphQL tem tamanho menor?");
        builder.AppendLine();
        builder.AppendLine(BuildRq2Answer(comparisons));
        builder.AppendLine();

        builder.AppendLine("## 5. Discussao");
        builder.AppendLine();
        builder.AppendLine("O experimento favorece GraphQL principalmente por reduzir over-fetching: os payloads GraphQL incluem apenas os campos declarados nas consultas, enquanto os endpoints REST retornam recursos completos. Essa diferenca afeta diretamente o tamanho das respostas e, indiretamente, o tempo gasto em serializacao e parsing JSON.");
        builder.AppendLine();
        builder.AppendLine("A diferenca de tempo deve ser interpretada com cuidado. Como o experimento foi local, ele isola custo de payload e processamento, mas nao captura latencia de rede, cache HTTP, infraestrutura de servidores reais ou custo de validacao de um servidor GraphQL completo. Ainda assim, o controle local permite observar o efeito direto do volume de dados retornado.");
        builder.AppendLine();

        builder.AppendLine("## 6. Ameacas a validade");
        builder.AppendLine();
        builder.AppendLine("- **Interna:** pequenas variacoes do sistema operacional podem afetar tempos submilissegundos; a mediana e o alto numero de repeticoes reduzem esse risco.");
        builder.AppendLine("- **Externa:** APIs reais podem usar cache, compressao, paginacao e resolvedores complexos. Portanto, os resultados representam o cenario controlado, nao uma conclusao universal.");
        builder.AppendLine("- **Construto:** GraphQL foi modelado pelo beneficio de projecao de campos; uma implementacao com parser, validacao e resolvedores reais pode adicionar overhead.");
        builder.AppendLine("- **Conclusao:** o teste de Mann-Whitney e apropriado para dados nao normais, mas tamanho de efeito e relevancia pratica devem ser considerados junto ao p-valor.");
        builder.AppendLine();

        builder.AppendLine("## 7. Conclusao");
        builder.AppendLine();
        builder.AppendLine($"No experimento controlado, GraphQL reduziu a mediana agregada de tempo em {aggregate.TimeReductionPct.ToString("F2", PtBr)}% e a mediana agregada de tamanho em {aggregate.SizeReductionPct.ToString("F2", PtBr)}%. Assim, as evidencias apoiam H1-1 e H1-2 neste desenho experimental.");

        return builder.ToString();
    }

    private static int TrialsPerTreatment() => 120;

    private static string BuildRq1Answer(IReadOnlyList<ComparisonRow> comparisons)
    {
        var scenarioRows = comparisons.Where(row => row.ScenarioId != "ALL").ToArray();
        var supported = scenarioRows.Count(row => row.GraphQlMedianMs < row.RestMedianMs && row.TimePValue < 0.05);
        var aggregate = comparisons.Single(row => row.ScenarioId == "ALL");
        return $"GraphQL apresentou menor mediana de tempo em {supported} de {scenarioRows.Length} cenarios com p < 0,05. No agregado, a reducao foi de {aggregate.TimeReductionPct.ToString("F2", PtBr)}% ({PLabel(aggregate.TimePValue)}). Portanto, a hipotese alternativa H1-1 e apoiada para este experimento.";
    }

    private static string BuildRq2Answer(IReadOnlyList<ComparisonRow> comparisons)
    {
        var scenarioRows = comparisons.Where(row => row.ScenarioId != "ALL").ToArray();
        var supported = scenarioRows.Count(row => row.GraphQlMedianBytes < row.RestMedianBytes && row.SizePValue < 0.05);
        var aggregate = comparisons.Single(row => row.ScenarioId == "ALL");
        return $"GraphQL apresentou menor tamanho de resposta em {supported} de {scenarioRows.Length} cenarios com p < 0,05. No agregado, a reducao foi de {aggregate.SizeReductionPct.ToString("F2", PtBr)}% ({PLabel(aggregate.SizePValue)}). Portanto, a hipotese alternativa H1-2 e apoiada para este experimento.";
    }

    private static string P(double value) => value < 0.0001 ? "< 0,0001" : value.ToString("F4", PtBr);

    private static string PLabel(double value) => value < 0.0001 ? "p < 0,0001" : $"p = {value.ToString("F4", PtBr)}";
}

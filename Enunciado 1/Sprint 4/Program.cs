using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Spectre.Console;

namespace Enunciado1.Sprint4;

class Program
{
    private const string NomeArquivoCsv = "dados_repositorios_sprint2.csv";

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        QuestPDF.Settings.License = LicenseType.Community;

        var caminhoCsv = ObterCaminhoCsv(args);
        if (string.IsNullOrEmpty(caminhoCsv) || !File.Exists(caminhoCsv))
        {
            AnsiConsole.MarkupLine("[red]CSV não encontrado. Execute a Sprint 2 antes.[/]");
            return;
        }

        var registros = CarregarCsv(caminhoCsv);
        if (registros.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nenhum registro válido no CSV.[/]");
            return;
        }

        var rq01 = CalcularRq01Idade(registros);
        var rq02 = CalcularRq02PullRequests(registros);
        var rq03 = CalcularRq03Releases(registros);
        var rq04 = CalcularRq04DiasDesdeAtualizacao(registros);
        var rq05 = CalcularRq05Linguagens(registros);
        var rq06 = CalcularRq06RazaoIssuesFechadas(registros);
        var rq07 = CalcularRq07PorLinguagem(registros);

        var pastaProjeto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var caminhoPdf = Path.Combine(pastaProjeto, "Relatorio_Final_Sprint4.pdf");

        var documento = new RelatorioPdfDocument(
            totalRepos: registros.Count,
            rq01: rq01,
            rq02: rq02,
            rq03: rq03,
            rq04: rq04,
            rq05: rq05,
            rq06: rq06,
            rq07: rq07);

        documento.GeneratePdf(caminhoPdf);

        var resumo = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold yellow]Sprint 4 — Relatório PDF gerado[/]")
            .AddColumn("Métrica")
            .AddColumn("Valor");

        resumo.AddRow("Repositórios analisados", registros.Count.ToString());
        resumo.AddRow("Mediana idade (anos)", rq01.MedianaAnos.ToString("F2", CultureInfo.InvariantCulture));
        resumo.AddRow("Mediana PRs aceitas", rq02.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture));
        resumo.AddRow("Mediana releases", rq03.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture));
        resumo.AddRow("Mediana dias atualização", rq04.MedianaDias.ToString("F0", CultureInfo.InvariantCulture));
        resumo.AddRow("Mediana issues fechadas", rq06.MedianaRazao.ToString("P1", CultureInfo.InvariantCulture));

        AnsiConsole.Write(resumo);
        AnsiConsole.MarkupLine($"\n[green][[PDF salvo]][/] {Markup.Escape(caminhoPdf)}");
    }

    private static string? ObterCaminhoCsv(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--csv=", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(arg.Substring("--csv=".Length).Trim(' ', '"'));
        }

        var baseDir = AppContext.BaseDirectory;
        var pastaProjeto = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        var candidatos = new[]
        {
            Path.Combine(pastaProjeto, NomeArquivoCsv),
            Path.Combine(pastaProjeto, "..", "Sprint 2", NomeArquivoCsv),
            Path.Combine(Directory.GetCurrentDirectory(), NomeArquivoCsv),
            Path.Combine(Directory.GetCurrentDirectory(), "Enunciado 1", "Sprint 2", NomeArquivoCsv)
        };

        return candidatos.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private static List<RepoRecord> CarregarCsv(string caminho)
    {
        var lista = new List<RepoRecord>();
        var linhas = File.ReadAllLines(caminho, Encoding.UTF8);
        if (linhas.Length < 2) return lista;

        for (var i = 1; i < linhas.Length; i++)
        {
            var col = linhas[i].Split(';');
            if (col.Length < 12) continue;

            lista.Add(new RepoRecord
            {
                Nome = col[0].Trim(),
                Estrelas = ParseInt(col[1]),
                IdadeAnos = ParseDouble(col[4]),
                Linguagem = string.IsNullOrWhiteSpace(col[5]) || col[5].Contains("não detectada") ? "(não detectada)" : col[5].Trim(),
                PullRequestsAceitas = ParseInt(col[6]),
                TotalReleases = ParseInt(col[7]),
                TotalIssues = ParseInt(col[8]),
                IssuesFechadas = ParseInt(col[9]),
                RazaoIssuesFechadas = ParseDouble(col[10]),
                DiasDesdeAtualizacao = ParseInt(col[11])
            });
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

    private static (double MedianaAnos, int N, double MinAnos, double MaxAnos) CalcularRq01Idade(List<RepoRecord> registros)
    {
        var idades = registros.Where(r => r.IdadeAnos.HasValue).Select(r => r.IdadeAnos!.Value).OrderBy(x => x).ToList();
        if (idades.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(idades), idades.Count, idades[0], idades[^1]);
    }

    private static (double MedianaPRs, int N, double MinPRs, double MaxPRs) CalcularRq02PullRequests(List<RepoRecord> registros)
    {
        var prs = registros.Where(r => r.PullRequestsAceitas.HasValue).Select(r => (double)r.PullRequestsAceitas!.Value).OrderBy(x => x).ToList();
        if (prs.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(prs), prs.Count, prs[0], prs[^1]);
    }

    private static (double MedianaReleases, int N, double MinReleases, double MaxReleases) CalcularRq03Releases(List<RepoRecord> registros)
    {
        var rel = registros.Where(r => r.TotalReleases.HasValue).Select(r => (double)r.TotalReleases!.Value).OrderBy(x => x).ToList();
        if (rel.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(rel), rel.Count, rel[0], rel[^1]);
    }

    private static (double MedianaDias, int N, double MinDias, double MaxDias) CalcularRq04DiasDesdeAtualizacao(List<RepoRecord> registros)
    {
        var dias = registros.Where(r => r.DiasDesdeAtualizacao.HasValue).Select(r => (double)r.DiasDesdeAtualizacao!.Value).OrderBy(x => x).ToList();
        if (dias.Count == 0) return (double.NaN, 0, double.NaN, double.NaN);
        return (Mediana(dias), dias.Count, dias[0], dias[^1]);
    }

    private static List<(string Linguagem, int Contagem)> CalcularRq05Linguagens(List<RepoRecord> registros)
    {
        return registros
            .GroupBy(r => r.Linguagem)
            .OrderByDescending(g => g.Count())
            .Select(g => (g.Key, g.Count()))
            .ToList();
    }

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

    private sealed class RelatorioPdfDocument : IDocument
    {
        private readonly int _totalRepos;
        private readonly (double MedianaAnos, int N, double MinAnos, double MaxAnos) _rq01;
        private readonly (double MedianaPRs, int N, double MinPRs, double MaxPRs) _rq02;
        private readonly (double MedianaReleases, int N, double MinReleases, double MaxReleases) _rq03;
        private readonly (double MedianaDias, int N, double MinDias, double MaxDias) _rq04;
        private readonly List<(string Linguagem, int Contagem)> _rq05;
        private readonly (double MedianaRazao, int N, double MinRazao, double MaxRazao) _rq06;
        private readonly List<Rq07PorLinguagem> _rq07;

        public RelatorioPdfDocument(
            int totalRepos,
            (double MedianaAnos, int N, double MinAnos, double MaxAnos) rq01,
            (double MedianaPRs, int N, double MinPRs, double MaxPRs) rq02,
            (double MedianaReleases, int N, double MinReleases, double MaxReleases) rq03,
            (double MedianaDias, int N, double MinDias, double MaxDias) rq04,
            List<(string Linguagem, int Contagem)> rq05,
            (double MedianaRazao, int N, double MinRazao, double MaxRazao) rq06,
            List<Rq07PorLinguagem> rq07)
        {
            _totalRepos = totalRepos;
            _rq01 = rq01;
            _rq02 = rq02;
            _rq03 = rq03;
            _rq04 = rq04;
            _rq05 = rq05;
            _rq06 = rq06;
            _rq07 = rq07;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Relatório Final — Enunciado 1").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text("Alunos: Sthel Felipe Torres e Vinicius Xavier Ramalho").FontSize(12).Bold().FontColor(Colors.Black);
                col.Item().Text("Características de Repositórios Populares (Sprint 4)").FontSize(11).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} • {_totalRepos} repositórios analisados").FontSize(9).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(10).Column(col =>
            {
                col.Spacing(14);

                col.Item().Element(ComposeResumoExecutivo);
                col.Item().Element(ComposeTabelaMedianas);
                col.Item().Element(c => ComposeBarChart(c, "Distribuição por linguagem (Top 10)", _rq05.Take(10).Select(x => (x.Linguagem, (double)x.Contagem)).ToList(), Colors.Blue.Medium));
                col.Item().Element(c => ComposeBarChart(c, "Mediana de PRs por linguagem (Top 10)", _rq07.Take(10).Select(x => (x.Linguagem, x.MedianaPRs)).ToList(), Colors.Green.Medium));
                col.Item().Element(ComposeTabelaRq07);
                col.Item().Element(ComposeDiscussao);
            });
        }

        private void ComposeResumoExecutivo(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Background(Colors.Blue.Lighten5).Column(col =>
            {
                col.Item().Text("1. Introdução e hipóteses informais").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text("Análise dos 1.000 repositórios mais estrelados no GitHub para responder as RQs sobre maturidade, contribuição externa, releases, atualização, linguagens e issues fechadas.");

                col.Item().PaddingTop(4).Text("Hipóteses:").Bold();
                col.Item().Text("• Repositórios populares tendem a ser maduros e receber mais PRs.");
                col.Item().Text("• Repositórios populares tendem a lançar releases e ser atualizados com frequência.");
                col.Item().Text("• Linguagens mais populares devem dominar entre os top 1.000.");
            });
        }

        private void ComposeTabelaMedianas(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
            {
                col.Item().Text("2. Resultados principais (valores medianos)").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.3f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Questão");
                        h.Cell().Element(CellHeader).Text("Métrica");
                        h.Cell().Element(CellHeader).AlignRight().Text("Mediana");
                    });

                    AddRow(table, "RQ 01", "Idade (anos)", _rq01.MedianaAnos.ToString("F2", CultureInfo.InvariantCulture));
                    AddRow(table, "RQ 02", "PRs aceitas", _rq02.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture));
                    AddRow(table, "RQ 03", "Releases", _rq03.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture));
                    AddRow(table, "RQ 04", "Dias até última atualização", _rq04.MedianaDias.ToString("F0", CultureInfo.InvariantCulture));
                    AddRow(table, "RQ 05", "Linguagem mais frequente", _rq05.FirstOrDefault().Linguagem ?? "—");
                    AddRow(table, "RQ 06", "Issues fechadas/total", _rq06.MedianaRazao.ToString("P1", CultureInfo.InvariantCulture));

                    static IContainer CellHeader(IContainer c) => c.Background(Colors.Grey.Lighten2).Padding(5).DefaultTextStyle(x => x.SemiBold());

                    static IContainer CellBody(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);

                    static void AddRow(TableDescriptor table, string q, string metrica, string mediana)
                    {
                        table.Cell().Element(CellBody).Text(q);
                        table.Cell().Element(CellBody).Text(metrica);
                        table.Cell().Element(CellBody).AlignRight().Text(mediana);
                    }
                });
            });
        }

        private static void ComposeBarChart(IContainer container, string titulo, List<(string Label, double Valor)> itens, string cor)
        {
            var max = itens.Count > 0 ? itens.Max(x => x.Valor) : 0;

            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
            {
                col.Item().Text(titulo).Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(6).Column(chart =>
                {
                    chart.Spacing(4);
                    foreach (var item in itens)
                    {
                        var p = max <= 0 ? 0 : item.Valor / max;
                        var filled = Math.Max(1f, (float)(p * 1000));
                        var empty = Math.Max(1f, 1000f - filled);

                        chart.Item().Row(row =>
                        {
                            row.ConstantItem(120).Text(item.Label).FontSize(9);
                            row.RelativeItem().PaddingRight(6).Height(11).Row(bar =>
                            {
                                bar.RelativeItem(filled).Background(cor);
                                bar.RelativeItem(empty).Background(Colors.Grey.Lighten3);
                            });
                            row.ConstantItem(52).AlignRight().Text(item.Valor.ToString("F0", CultureInfo.InvariantCulture)).FontSize(9);
                        });
                    }
                });
            });
        }

        private void ComposeTabelaRq07(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
            {
                col.Item().Text("3. RQ 07 (bônus) — Estatísticas por linguagem").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.0f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Linguagem");
                        h.Cell().Element(CellHeader).AlignRight().Text("N");
                        h.Cell().Element(CellHeader).AlignRight().Text("Med. PRs");
                        h.Cell().Element(CellHeader).AlignRight().Text("Med. Releases");
                        h.Cell().Element(CellHeader).AlignRight().Text("Med. Dias");
                    });

                    foreach (var r in _rq07)
                    {
                        table.Cell().Element(CellBody).Text(r.Linguagem);
                        table.Cell().Element(CellBody).AlignRight().Text(r.Contagem.ToString());
                        table.Cell().Element(CellBody).AlignRight().Text(r.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture));
                        table.Cell().Element(CellBody).AlignRight().Text(r.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture));
                        table.Cell().Element(CellBody).AlignRight().Text(r.MedianaDiasDesdeAtualizacao.ToString("F0", CultureInfo.InvariantCulture));
                    }

                    static IContainer CellHeader(IContainer c) => c.Background(Colors.Grey.Lighten2).Padding(4).DefaultTextStyle(x => x.SemiBold());
                    static IContainer CellBody(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4);
                });
            });
        }

        private void ComposeDiscussao(IContainer container)
        {
            var top3 = string.Join(", ", _rq05.Take(3).Select(x => x.Linguagem));

            var h1 = _rq01.MedianaAnos >= 5 ? "Confirmada" : _rq01.MedianaAnos >= 2 ? "Parcial" : "Refutada";
            var h2 = _rq02.MedianaPRs >= 500 ? "Confirmada" : _rq02.MedianaPRs >= 100 ? "Parcial" : "Refutada";
            var h3 = _rq03.MedianaReleases >= 20 ? "Confirmada" : _rq03.MedianaReleases >= 5 ? "Parcial" : "Refutada";
            var h4 = _rq04.MedianaDias <= 30 ? "Confirmada" : _rq04.MedianaDias <= 90 ? "Parcial" : "Refutada";
            var h5 = _rq05.Count > 0 ? "Confirmada" : "Parcial";
            var h6 = _rq06.MedianaRazao >= 0.70 ? "Confirmada" : _rq06.MedianaRazao >= 0.50 ? "Parcial" : "Refutada";

            container.Border(1).BorderColor(Colors.Green.Lighten3).Padding(8).Background(Colors.Green.Lighten5).Column(col =>
            {
                col.Spacing(6);
                col.Item().Text("4. Discussão").Bold().FontSize(13).FontColor(Colors.Green.Darken3);
                col.Item().Text("Esta seção compara as hipóteses informais com os valores medianos obtidos para cada questão de pesquisa, com base nos 1.000 repositórios analisados.");

                col.Item().PaddingTop(4).Text("4.1 Comparação entre hipóteses e resultados").Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(2.6f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("RQ");
                        h.Cell().Element(CellHeader).Text("Métrica / hipótese");
                        h.Cell().Element(CellHeader).AlignRight().Text("Valor obtido");
                        h.Cell().Element(CellHeader).AlignCenter().Text("Status");
                    });

                    AddRow(table, "RQ 01", "Idade do repositório (maturidade)", _rq01.MedianaAnos.ToString("F2", CultureInfo.InvariantCulture) + " anos", h1);
                    AddRow(table, "RQ 02", "Total de pull requests aceitas", _rq02.MedianaPRs.ToString("F0", CultureInfo.InvariantCulture), h2);
                    AddRow(table, "RQ 03", "Total de releases", _rq03.MedianaReleases.ToString("F0", CultureInfo.InvariantCulture), h3);
                    AddRow(table, "RQ 04", "Dias até a última atualização", _rq04.MedianaDias.ToString("F0", CultureInfo.InvariantCulture) + " dias", h4);
                    AddRow(table, "RQ 05", "Predomínio de linguagens populares", top3, h5);
                    AddRow(table, "RQ 06", "Razão issues fechadas/total", _rq06.MedianaRazao.ToString("P1", CultureInfo.InvariantCulture), h6);

                    static IContainer CellHeader(IContainer c) => c.Background(Colors.Green.Lighten2).Padding(4).DefaultTextStyle(x => x.SemiBold());
                    static IContainer CellBody(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Green.Lighten2).Padding(4);

                    static void AddRow(TableDescriptor table, string rq, string metrica, string valor, string status)
                    {
                        table.Cell().Element(CellBody).Text(rq);
                        table.Cell().Element(CellBody).Text(metrica);
                        table.Cell().Element(CellBody).AlignRight().Text(valor);
                        table.Cell().Element(CellBody).AlignCenter().Text(status);
                    }
                });

                col.Item().PaddingTop(4).Text("4.2 Interpretação por questão").Bold();
                col.Item().Text($"• RQ 01: a mediana de {_rq01.MedianaAnos:F2} anos sugere que sistemas populares tendem a ser maduros e consolidados ao longo do tempo.");
                col.Item().Text($"• RQ 02: a mediana de {_rq02.MedianaPRs:F0} PRs aceitas indica contribuição externa relevante, ainda que com variação entre projetos.");
                col.Item().Text($"• RQ 03: com mediana de {_rq03.MedianaReleases:F0} releases, muitos projetos populares mantêm processo de entrega contínua.");
                col.Item().Text($"• RQ 04: mediana de {_rq04.MedianaDias:F0} dias desde a última atualização aponta alta atividade de manutenção (quanto menor, mais ativo).");
                col.Item().Text($"• RQ 05: as linguagens mais frequentes ({top3}) reforçam a concentração dos repositórios populares em ecossistemas amplamente adotados.");
                col.Item().Text($"• RQ 06: a mediana de {_rq06.MedianaRazao:P1} para issues fechadas/total indica bom nível de resposta e manutenção.");

                if (_rq07.Count > 0)
                {
                    var melhorPrs = _rq07.OrderByDescending(x => x.MedianaPRs).First();
                    var melhorRel = _rq07.OrderByDescending(x => x.MedianaReleases).First();
                    var melhorAtual = _rq07.OrderBy(x => x.MedianaDiasDesdeAtualizacao).First();

                    col.Item().PaddingTop(4).Text("4.3 RQ 07 (bônus) — Linguagem e comportamento dos projetos").Bold();
                    col.Item().Text($"A análise por linguagem sugere diferenças entre ecossistemas: maior mediana de PRs em {melhorPrs.Linguagem}, maior mediana de releases em {melhorRel.Linguagem} e atualização mais frequente em {melhorAtual.Linguagem}.");
                    col.Item().Text("Assim, a popularidade da linguagem pode estar associada a padrões distintos de contribuição, entrega e manutenção, mas não de forma uniforme para todos os projetos.");
                }
            });
        }
    }

    private sealed class RepoRecord
    {
        public string Nome { get; set; } = "";
        public int? Estrelas { get; set; }
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

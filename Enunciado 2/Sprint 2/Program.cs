using System.Globalization;
using System.Text;

namespace Enunciado2.Sprint2;

/// <summary>
/// LAB 02 — Sprint 2: consolida <c>repos_java_1000.csv</c> com medições CK por repositório
/// (amostra da Sprint 1 e/ou CSV extra) em <c>lab02_medicoes_consolidado.csv</c>.
/// </summary>
/// <remarks>
/// Enunciado LAB 02 — Lab02S02: um CSV (lab02_medicoes_consolidado) com todas as linhas;
/// cada linha = processo GitHub + sumarização CK (média/mediana/dp CBO, DIT, LCOM por repo).
/// Fontes: medicoes_repositorio_amostra.csv, medicoes_ck_lote.csv, --ck-extra. Consumo: Sprint 3 (RQs, bônus, PDF).
/// </remarks>
static class Program
{
    private const string HeaderMedicoes =
        "nome_completo;estrelas;forks;releases;idade_anos;disk_usage_kb;loc_java;comentarios_linhas;" +
        "classes_analisadas;cbo_media;cbo_mediana;cbo_desvio;dit_media;dit_mediana;dit_desvio;" +
        "lcom_media;lcom_mediana;lcom_desvio";

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var pastaSprint2 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var sprint1Out = Path.Combine(pastaSprint2, "..", "Sprint 1", "lab02_sprint1_output");
        var reposCsv = ObterCaminhoArg(args, "--repos")
            ?? Path.Combine(sprint1Out, "repos_java_1000.csv");
        var amostraCsv = ObterCaminhoArg(args, "--amostra")
            ?? Path.Combine(sprint1Out, "medicoes_repositorio_amostra.csv");
        var loteCsv = ObterCaminhoArg(args, "--lote")
            ?? Path.Combine(sprint1Out, "medicoes_ck_lote.csv");
        var extraCk = ObterCaminhoArg(args, "--ck-extra");

        var outDir = Path.Combine(pastaSprint2, "lab02_sprint2_output");
        Directory.CreateDirectory(outDir);
        var saida = Path.Combine(outDir, "lab02_medicoes_consolidado.csv");

        if (!File.Exists(reposCsv))
        {
            Console.WriteLine($"Arquivo não encontrado: {reposCsv}");
            Console.WriteLine("Gere a lista na Sprint 1 ou use --repos=caminho\\repos_java_1000.csv");
            return;
        }

        var porNome = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(amostraCsv))
            MesclarCsvMedicoes(amostraCsv, porNome);
        if (File.Exists(loteCsv))
            MesclarCsvMedicoes(loteCsv, porNome);
        if (!string.IsNullOrWhiteSpace(extraCk) && File.Exists(extraCk))
            MesclarCsvMedicoes(extraCk, porNome);

        var linhasRepos = File.ReadAllLines(reposCsv, Encoding.UTF8);
        if (linhasRepos.Length < 2)
        {
            Console.WriteLine("repos_java_1000.csv vazio ou inválido.");
            return;
        }

        var hRepos = linhasRepos[0].Split(';');
        int Ir(string n)
        {
            for (var i = 0; i < hRepos.Length; i++)
                if (string.Equals(hRepos[i].Trim(), n, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        var inome = Ir("nome_completo");
        var iest = Ir("estrelas");
        var ifork = Ir("forks");
        var irel = Ir("releases");
        var iidade = Ir("idade_anos");
        var idisk = Ir("disk_usage_kb");
        if (inome < 0 || iest < 0 || iidade < 0)
        {
            Console.WriteLine("Cabeçalho de repos_java_1000.csv inesperado (falta nome_completo/estrelas/idade_anos).");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(HeaderMedicoes);
        var ckCols = HeaderMedicoes.Split(';').Skip(9).ToArray();
        var comCk = 0;

        foreach (var linha in linhasRepos.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;
            var c = linha.Split(';');
            if (c.Length <= inome) continue;
            var nome = c[inome].Trim();
            static string G(string[] cols, int idx) =>
                idx >= 0 && idx < cols.Length ? cols[idx].Trim() : "";

            porNome.TryGetValue(nome, out var med);

            var loc = med != null && med.TryGetValue("loc_java", out var lj) ? lj : "";
            var com = med != null && med.TryGetValue("comentarios_linhas", out var cm) ? cm : "";
            var cls = med != null && med.TryGetValue("classes_analisadas", out var cl) ? cl : "";

            string Ck(string col)
            {
                if (med == null) return "";
                return med.TryGetValue(col, out var v) ? v : "";
            }

            var temCk = ckCols.Any(col =>
            {
                var v = Ck(col);
                return !string.IsNullOrWhiteSpace(v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            });
            if (temCk) comCk++;

            sb.AppendLine(string.Join(";",
                nome,
                G(c, iest),
                G(c, ifork),
                G(c, irel),
                G(c, iidade),
                G(c, idisk),
                loc,
                com,
                cls,
                Ck("cbo_media"),
                Ck("cbo_mediana"),
                Ck("cbo_desvio"),
                Ck("dit_media"),
                Ck("dit_mediana"),
                Ck("dit_desvio"),
                Ck("lcom_media"),
                Ck("lcom_mediana"),
                Ck("lcom_desvio")));
        }

        File.WriteAllText(saida, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"Consolidado escrito: {saida}");
        Console.WriteLine($"Repositórios com ao menos uma métrica CK numérica preenchida: {comCk}");
        if (File.Exists(loteCsv))
            Console.WriteLine($"Medições em lote fundidas de: {loteCsv}");
        if (comCk == 0)
            Console.WriteLine(
                "Dica: rode a Sprint 1 com --coleta-ck para gerar medicoes_repositorio_amostra.csv, " +
                "ou agregue medições em lote e passe --ck-extra=arquivo.csv (mesmo cabeçalho de métricas CK).");
    }

    private static void MesclarCsvMedicoes(string path, Dictionary<string, Dictionary<string, string>> porNome)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2) return;
        var h = lines[0].Split(';');
        for (var r = 1; r < lines.Length; r++)
        {
            if (string.IsNullOrWhiteSpace(lines[r])) continue;
            var cols = lines[r].Split(';');
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < h.Length && i < cols.Length; i++)
                map[h[i].Trim()] = cols[i].Trim();
            if (!map.TryGetValue("nome_completo", out var nome) || string.IsNullOrWhiteSpace(nome))
                continue;
            if (!porNome.TryGetValue(nome, out var alvo))
            {
                alvo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                porNome[nome] = alvo;
            }

            foreach (var kv in map)
            {
                if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                alvo[kv.Key] = kv.Value;
            }
        }
    }

    private static string? ObterCaminhoArg(string[] args, string flag)
    {
        var eq = flag + "=";
        foreach (var a in args)
        {
            if (a.StartsWith(eq, StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(a[eq.Length..].Trim().Trim('"'));
        }

        return null;
    }
}

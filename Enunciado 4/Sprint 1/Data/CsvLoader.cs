using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Enunciado4.Sprint1.Models;

namespace Enunciado4.Sprint1.Data;

public static class CsvLoader
{
    private static readonly CsvConfiguration Config = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
        BadDataFound = null
    };

    public static Lab04Dataset LoadFull(string dadosDir) => new(
        LoadCharacterization(dadosDir),
        LoadRq1Frequencia(Path.Combine(dadosDir, "rq1_frequencia_cwe.csv")),
        LoadRq1Tendencia(Path.Combine(dadosDir, "rq1_tendencia_temporal.csv")),
        LoadRq1Lorenz(Path.Combine(dadosDir, "rq1_curva_lorenz.csv")),
        LoadRq1Persistencia(Path.Combine(dadosDir, "rq1_persistencia_ranking.csv")),
        LoadRq2Severidade(Path.Combine(dadosDir, "rq2_severidade_cwe.csv")),
        LoadRq3Dispersao(Path.Combine(dadosDir, "rq3_dispersao_quadrante.csv")),
        LoadRq3Score(Path.Combine(dadosDir, "rq3_score_risco.csv")),
        LoadRq3Quadrantes(Path.Combine(dadosDir, "rq3_quadrantes_resumo.csv")),
        LoadPriorizacao(Path.Combine(dadosDir, "lista_priorizacao.csv")),
        LoadGqm(Path.Combine(dadosDir, "gqm_perguntas_metricas.csv")),
        LoadEstatisticas(Path.Combine(dadosDir, "estatisticas_testes.csv")));

    public static CharacterizationData LoadCharacterization(string dadosDir)
    {
        return new CharacterizationData(
            LoadKpis(Path.Combine(dadosDir, "kpis_gerais.csv")),
            LoadPorAno(Path.Combine(dadosDir, "caracterizacao_por_ano.csv")),
            LoadPorSeveridade(Path.Combine(dadosDir, "caracterizacao_por_severidade.csv")),
            LoadSeveridadePorAno(Path.Combine(dadosDir, "caracterizacao_severidade_por_ano.csv")),
            LoadDistribuicaoCvss(Path.Combine(dadosDir, "distribuicao_cvss.csv")));
    }

    private static List<Rq1FrequenciaCwe> LoadRq1Frequencia(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq1FreqRow>()
            .Select(r => new Rq1FrequenciaCwe(r.cwe_id, r.cves_count, r.rank_freq, r.pct_acumulado,
                r.cwe_nome_curto, r.cwe_label, r.top30))
            .ToList();
    }

    private static List<Rq1TendenciaTemporal> LoadRq1Tendencia(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq1TendRow>()
            .Where(r => r.is_top10)
            .Select(r => new Rq1TendenciaTemporal(r.ano, r.cwe_id, r.cwe_nome_curto, r.cves_count, r.is_top10))
            .ToList();
    }

    private static List<Rq1CurvaLorenz> LoadRq1Lorenz(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq1LorenzRow>()
            .Select(r => new Rq1CurvaLorenz(r.pct_cwes_acumulado, r.pct_cves_acumulado, r.linha_igualdade))
            .ToList();
    }

    private static List<Rq1PersistenciaRanking> LoadRq1Persistencia(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq1PersRow>()
            .Select(r => new Rq1PersistenciaRanking(r.transicao, r.jaccard))
            .ToList();
    }

    private static List<Rq2SeveridadeCwe> LoadRq2Severidade(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq2Row>()
            .Select(r => new Rq2SeveridadeCwe(r.cwe_id, r.cves_count, r.cvss_medio, r.high_critical_pct,
                r.cwe_label, r.top30_cvss))
            .ToList();
    }

    private static List<Rq3DispersaoQuadrante> LoadRq3Dispersao(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq3DispRow>()
            .Select(r => new Rq3DispersaoQuadrante(r.cwe_id, r.cves_count, r.cvss_medio, r.quadrante, r.cwe_label))
            .ToList();
    }

    private static List<Rq3ScoreRisco> LoadRq3Score(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq3ScoreRow>()
            .Select(r => new Rq3ScoreRisco(r.rank_risco, r.cwe_label, r.score_risco, r.faixa_risco))
            .ToList();
    }

    private static List<Rq3QuadranteResumo> LoadRq3Quadrantes(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<Rq3QuadRow>()
            .Select(r => new Rq3QuadranteResumo(r.quadrante, r.n_cwes, r.pct, r.ordem))
            .OrderBy(r => r.Ordem)
            .ToList();
    }

    private static List<PriorizacaoCwe> LoadPriorizacao(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<PriorRow>()
            .Select(r => new PriorizacaoCwe(r.prioridade, r.cwe_id, r.cwe_nome_curto, r.cves_count,
                r.cvss_medio, r.high_critical_pct, r.score_risco))
            .OrderBy(r => r.Prioridade)
            .ToList();
    }

    private static List<GqmPergunta> LoadGqm(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<GqmRow>()
            .Select(r => new GqmPergunta(r.pergunta, r.titulo, r.hipotese, r.metricas, r.veredito, r.evidencia))
            .ToList();
    }

    private static List<EstatisticaTeste> LoadEstatisticas(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<EstatRow>()
            .Select(r => new EstatisticaTeste(r.teste, r.hipotese, r.resultado, r.p_valor))
            .ToList();
    }

    private static KpisGerais LoadKpis(string path)
    {
        using var reader = CreateCsvReader(path);
        var row = reader.GetRecords<KpisCsvRow>().First();
        return new KpisGerais(
            row.total_cves_distintos,
            row.total_relacoes_cve_cwe,
            row.total_cwes_distintas,
            row.cobertura_cvss_pct,
            row.cvss_medio_global,
            row.cvss_mediana_global,
            row.cvss_desvio_padrao,
            row.ano_inicio,
            row.ano_fim,
            row.gini_frequencia);
    }

    private static List<CaracterizacaoPorAno> LoadPorAno(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<PorAnoCsvRow>()
            .Select(r => new CaracterizacaoPorAno(r.ano, r.cves_distintos, r.cvss_media))
            .OrderBy(r => r.Ano)
            .ToList();
    }

    private static List<CaracterizacaoPorSeveridade> LoadPorSeveridade(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<PorSeveridadeCsvRow>()
            .Select(r => new CaracterizacaoPorSeveridade(
                r.severidade, r.n_cves, r.cvss_media, r.cvss_mediana, r.pct, r.ordem))
            .OrderBy(r => r.Ordem)
            .ToList();
    }

    private static List<CaracterizacaoSeveridadePorAno> LoadSeveridadePorAno(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<SeveridadePorAnoCsvRow>()
            .Select(r => new CaracterizacaoSeveridadePorAno(
                r.ano, r.severidade, r.n_cves, r.ordem, r.pct_no_ano))
            .OrderBy(r => r.Ano)
            .ThenBy(r => r.Ordem)
            .ToList();
    }

    private static List<DistribuicaoCvss> LoadDistribuicaoCvss(string path)
    {
        using var reader = CreateCsvReader(path);
        return reader.GetRecords<DistribuicaoCvssCsvRow>()
            .Select(r => new DistribuicaoCvss(r.cvss_score_arredondado, r.n_cves, r.faixa_severidade))
            .OrderBy(r => r.CvssScoreArredondado)
            .ToList();
    }

    private static CsvReader CreateCsvReader(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo de dados não encontrado: {path}", path);
        return new CsvReader(new StreamReader(path), Config);
    }

    private sealed class KpisCsvRow
    {
        public int total_cves_distintos { get; set; }
        public int total_relacoes_cve_cwe { get; set; }
        public int total_cwes_distintas { get; set; }
        public double cobertura_cvss_pct { get; set; }
        public double cvss_medio_global { get; set; }
        public double cvss_mediana_global { get; set; }
        public double cvss_desvio_padrao { get; set; }
        public int ano_inicio { get; set; }
        public int ano_fim { get; set; }
        public double gini_frequencia { get; set; }
    }

    private sealed class PorAnoCsvRow
    {
        public int ano { get; set; }
        public int cves_distintos { get; set; }
        public double cvss_media { get; set; }
    }

    private sealed class PorSeveridadeCsvRow
    {
        public string severidade { get; set; } = "";
        public int n_cves { get; set; }
        public double cvss_media { get; set; }
        public double cvss_mediana { get; set; }
        public double pct { get; set; }
        public int ordem { get; set; }
    }

    private sealed class SeveridadePorAnoCsvRow
    {
        public int ano { get; set; }
        public string severidade { get; set; } = "";
        public int n_cves { get; set; }
        public int ordem { get; set; }
        public double pct_no_ano { get; set; }
    }

    private sealed class DistribuicaoCvssCsvRow
    {
        public double cvss_score_arredondado { get; set; }
        public int n_cves { get; set; }
        public string faixa_severidade { get; set; } = "";
    }

    private sealed class Rq1FreqRow
    {
        public string cwe_id { get; set; } = "";
        public int cves_count { get; set; }
        public int rank_freq { get; set; }
        public double pct_acumulado { get; set; }
        public string cwe_nome_curto { get; set; } = "";
        public string cwe_label { get; set; } = "";
        public bool top30 { get; set; }
    }

    private sealed class Rq1TendRow
    {
        public int ano { get; set; }
        public string cwe_id { get; set; } = "";
        public string cwe_nome_curto { get; set; } = "";
        public int cves_count { get; set; }
        public bool is_top10 { get; set; }
    }

    private sealed class Rq1LorenzRow
    {
        public double pct_cwes_acumulado { get; set; }
        public double pct_cves_acumulado { get; set; }
        public double linha_igualdade { get; set; }
    }

    private sealed class Rq1PersRow
    {
        public string transicao { get; set; } = "";
        public double jaccard { get; set; }
    }

    private sealed class Rq2Row
    {
        public string cwe_id { get; set; } = "";
        public int cves_count { get; set; }
        public double cvss_medio { get; set; }
        public double high_critical_pct { get; set; }
        public string cwe_label { get; set; } = "";
        public bool top30_cvss { get; set; }
    }

    private sealed class Rq3DispRow
    {
        public string cwe_id { get; set; } = "";
        public int cves_count { get; set; }
        public double cvss_medio { get; set; }
        public string quadrante { get; set; } = "";
        public string cwe_label { get; set; } = "";
    }

    private sealed class Rq3ScoreRow
    {
        public int rank_risco { get; set; }
        public string cwe_label { get; set; } = "";
        public double score_risco { get; set; }
        public string faixa_risco { get; set; } = "";
    }

    private sealed class Rq3QuadRow
    {
        public string quadrante { get; set; } = "";
        public int n_cwes { get; set; }
        public double pct { get; set; }
        public int ordem { get; set; }
    }

    private sealed class PriorRow
    {
        public int prioridade { get; set; }
        public string cwe_id { get; set; } = "";
        public string cwe_nome_curto { get; set; } = "";
        public int cves_count { get; set; }
        public double cvss_medio { get; set; }
        public double high_critical_pct { get; set; }
        public double score_risco { get; set; }
    }

    private sealed class GqmRow
    {
        public string pergunta { get; set; } = "";
        public string titulo { get; set; } = "";
        public string hipotese { get; set; } = "";
        public string metricas { get; set; } = "";
        public string veredito { get; set; } = "";
        public string evidencia { get; set; } = "";
    }

    private sealed class EstatRow
    {
        public string teste { get; set; } = "";
        public string hipotese { get; set; } = "";
        public string o_que_testa { get; set; } = "";
        public string estatistica { get; set; } = "";
        public string p_valor { get; set; } = "";
        public string resultado { get; set; } = "";
    }
}

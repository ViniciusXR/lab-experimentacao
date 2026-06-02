namespace Enunciado4.Sprint1.Models;

public sealed record Rq1FrequenciaCwe(
    string CweId,
    int CvesCount,
    int RankFreq,
    double PctAcumulado,
    string CweNomeCurto,
    string CweLabel,
    bool Top30);

public sealed record Rq1TendenciaTemporal(
    int Ano,
    string CweId,
    string CweNomeCurto,
    int CvesCount,
    bool IsTop10);

public sealed record Rq1CurvaLorenz(
    double PctCwesAcumulado,
    double PctCvesAcumulado,
    double LinhaIgualdade);

public sealed record Rq1PersistenciaRanking(
    string Transicao,
    double Jaccard);

public sealed record Rq2SeveridadeCwe(
    string CweId,
    int CvesCount,
    double CvssMedio,
    double HighCriticalPct,
    string CweLabel,
    bool Top30Cvss);

public sealed record Rq3DispersaoQuadrante(
    string CweId,
    int CvesCount,
    double CvssMedio,
    string Quadrante,
    string CweLabel);

public sealed record Rq3ScoreRisco(
    int RankRisco,
    string CweLabel,
    double ScoreRisco,
    string FaixaRisco);

public sealed record Rq3QuadranteResumo(
    string Quadrante,
    int NCwes,
    double Pct,
    int Ordem);

public sealed record PriorizacaoCwe(
    int Prioridade,
    string CweId,
    string CweNomeCurto,
    int CvesCount,
    double CvssMedio,
    double HighCriticalPct,
    double ScoreRisco);

public sealed record GqmPergunta(
    string Pergunta,
    string Titulo,
    string Hipotese,
    string Metricas,
    string Veredito,
    string Evidencia);

public sealed record EstatisticaTeste(
    string Teste,
    string Hipotese,
    string Resultado,
    string PValor);

public sealed record Lab04Dataset(
    CharacterizationData Characterization,
    List<Rq1FrequenciaCwe> Rq1Frequencia,
    List<Rq1TendenciaTemporal> Rq1Tendencia,
    List<Rq1CurvaLorenz> Rq1Lorenz,
    List<Rq1PersistenciaRanking> Rq1Persistencia,
    List<Rq2SeveridadeCwe> Rq2Severidade,
    List<Rq3DispersaoQuadrante> Rq3Dispersao,
    List<Rq3ScoreRisco> Rq3ScoreRisco,
    List<Rq3QuadranteResumo> Rq3Quadrantes,
    List<PriorizacaoCwe> Priorizacao,
    List<GqmPergunta> Gqm,
    List<EstatisticaTeste> Estatisticas);

public sealed record GeneratedCharts(
    IReadOnlyList<string> Caracterizacao,
    IReadOnlyList<string> Rq1,
    IReadOnlyList<string> Rq2,
    IReadOnlyList<string> Rq3);

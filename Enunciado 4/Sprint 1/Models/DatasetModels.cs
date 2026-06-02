namespace Enunciado4.Sprint1.Models;

public sealed record KpisGerais(
    int TotalCvesDistintos,
    int TotalRelacoesCveCwe,
    int TotalCwesDistintas,
    double CoberturaCvssPct,
    double CvssMedioGlobal,
    double CvssMedianaGlobal,
    double CvssDesvioPadrao,
    int AnoInicio,
    int AnoFim,
    double GiniFrequencia);

public sealed record CaracterizacaoPorAno(
    int Ano,
    int CvesDistintos,
    double CvssMedia);

public sealed record CaracterizacaoPorSeveridade(
    string Severidade,
    int NCves,
    double CvssMedia,
    double CvssMediana,
    double Pct,
    int Ordem);

public sealed record CaracterizacaoSeveridadePorAno(
    int Ano,
    string Severidade,
    int NCves,
    int Ordem,
    double PctNoAno);

public sealed record DistribuicaoCvss(
    double CvssScoreArredondado,
    int NCves,
    string FaixaSeveridade);

public sealed record CharacterizationData(
    KpisGerais Kpis,
    List<CaracterizacaoPorAno> PorAno,
    List<CaracterizacaoPorSeveridade> PorSeveridade,
    List<CaracterizacaoSeveridadePorAno> SeveridadePorAno,
    List<DistribuicaoCvss> DistribuicaoCvss);

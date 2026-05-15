namespace Lab03S03.Analysis
{
    public class AnalysisResult
    {
        public string RQCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hypothesis { get; set; } = string.Empty;

        // Rótulo descritivo da métrica primária para exibição na tabela
        public string PrimaryLabel { get; set; } = string.Empty;

        public double MedianMerged { get; set; }
        public double MedianClosed { get; set; }
        public double SpearmanRho { get; set; }
        public double PValue { get; set; }
        public string Interpretation { get; set; } = string.Empty;
        public string Discussion { get; set; } = string.Empty;
        public string ChartPath { get; set; } = string.Empty;

        // Métrica secundária — Tamanho: total de linhas; Interações: participantes
        public bool HasSecondaryMetric { get; set; }
        public string SecondaryLabel { get; set; } = string.Empty;
        public double SecondaryMedianMerged { get; set; }
        public double SecondaryMedianClosed { get; set; }
        public double SecondarySpearmanRho { get; set; }
        public double SecondaryPValue { get; set; }
        public string SecondaryInterpretation { get; set; } = string.Empty;
    }
}
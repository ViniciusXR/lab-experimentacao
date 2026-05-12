namespace Lab03S03.Analysis
{
    public class AnalysisResult
    {
        public string RQCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hypothesis { get; set; } = string.Empty;
        public double MedianMerged { get; set; }
        public double MedianClosed { get; set; }
        public double SpearmanRho { get; set; }
        public double PValue { get; set; }
        public string Interpretation { get; set; } = string.Empty;
        public string Discussion { get; set; } = string.Empty;
        public string ChartPath { get; set; } = string.Empty;
    }
}
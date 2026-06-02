using ScottPlot;

namespace Enunciado4.Sprint1.Visualization;

internal static class ChartPalette
{
    public static readonly Dictionary<string, Color> Severidade = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LOW"] = Color.FromHex("#3B8C3F"),
        ["MEDIUM"] = Color.FromHex("#F2C037"),
        ["HIGH"] = Color.FromHex("#E8702A"),
        ["CRITICAL"] = Color.FromHex("#7E0023")
    };

    public static readonly Dictionary<string, Color> Quadrante = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dupla Ameaca"] = Color.FromHex("#C0392B"),
        ["Severa"] = Color.FromHex("#E8702A"),
        ["Frequente"] = Color.FromHex("#2E86C1"),
        ["Baixo Risco"] = Color.FromHex("#95A5A6")
    };

    public static Color GetQuadrante(string q) =>
        Quadrante.GetValueOrDefault(q, Colors.Gray);

    public static void Save(Plot plt, string path) => plt.SavePng(path, 1400, 800);
}

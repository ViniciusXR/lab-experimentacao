using System;
using System.IO;
using SkiaSharp;

namespace Lab03S03.Visualization
{
    public static class FlowchartBuilder
    {
        public static void GenerateImages(string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            GeneratePage1(Path.Combine(outputDir, "fluxograma_etapas_1_2.png"));
            GeneratePage2(Path.Combine(outputDir, "fluxograma_etapas_3_4.png"));
        }

        private static void DrawArrow(SKCanvas canvas, float x, float yStart, float yEnd, string text, SKPaint textPaint, SKPaint arrowPaint)
        {
            canvas.DrawLine(x, yStart, x, yEnd, arrowPaint);
            // Arrowhead
            var path = new SKPath();
            path.MoveTo(x, yEnd);
            path.LineTo(x - 5, yEnd - 10);
            path.LineTo(x + 5, yEnd - 10);
            path.Close();
            canvas.DrawPath(path, arrowPaint);

            if (!string.IsNullOrEmpty(text))
            {
                var lines = text.Split('\n');
                float textY = yStart + (yEnd - yStart) / 2 - ((lines.Length - 1) * 16) / 2 + 5;
                foreach (var line in lines)
                {
                    canvas.DrawText(line, x + 10, textY, textPaint);
                    textY += 16;
                }
            }
        }

        private static void DrawBox(SKCanvas canvas, float x, float y, float width, float height, string text, SKColor bgColor, bool isPill)
        {
            using var paint = new SKPaint { Color = bgColor, IsAntialias = true };
            var rect = new SKRect(x - width / 2, y, x + width / 2, y + height);

            if (isPill)
            {
                canvas.DrawRoundRect(rect, height / 2, height / 2, paint);
            }
            else
            {
                canvas.DrawRoundRect(rect, 4, 4, paint);
            }

            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 14,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };

            var lines = text.Split('\n');
            float textY = y + height / 2 - ((lines.Length - 1) * 16) / 2 + 5;
            foreach (var line in lines)
            {
                canvas.DrawText(line, x, textY, textPaint);
                textY += 16;
            }
        }

        private static void DrawEtapaBg(SKCanvas canvas, float y, float height, string title)
        {
            var rect = new SKRect(10, y, 470, y + height);
            using var bgPaint = new SKPaint { Color = SKColor.Parse("#FFFDE7"), IsAntialias = true };
            canvas.DrawRoundRect(rect, 10, 10, bgPaint);

            using var borderPaint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            canvas.DrawRoundRect(rect, 10, 10, borderPaint);

            using var titlePaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 16,
                IsAntialias = true,
                TextAlign = SKTextAlign.Left,
                Typeface = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            canvas.DrawText(title, 20, y + 25, titlePaint);
        }

        private static void GeneratePage1(string path)
        {
            var info = new SKImageInfo(480, 680);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            using var arrowTextPaint = new SKPaint { Color = SKColors.DarkGray, TextSize = 12, IsAntialias = true, TextAlign = SKTextAlign.Left, Typeface = SKTypeface.Default };
            using var arrowPaint = new SKPaint { Color = SKColors.DarkGray, IsAntialias = true, Style = SKPaintStyle.Fill, StrokeWidth = 2 };

            var orange = SKColor.Parse("#E65100");
            var blue = SKColor.Parse("#1565C0");

            DrawEtapaBg(canvas, 10, 310, "Etapa 1: Coleta de Repositórios");
            DrawBox(canvas, 240, 50, 160, 40, "API do GitHub", orange, true);
            DrawArrow(canvas, 240, 90, 130, "Busca por estrelas", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 130, 200, 40, "Top 200 Repositórios", orange, true);
            DrawArrow(canvas, 240, 170, 210, "", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 210, 200, 40, "lista_repositorios.csv", orange, true);
            DrawArrow(canvas, 240, 250, 300, "Filtro: >= 100 PRs", arrowTextPaint, arrowPaint);

            DrawEtapaBg(canvas, 330, 340, "Etapa 2: Coleta de Pull Requests");
            DrawBox(canvas, 240, 370, 200, 40, "lista_repositorios.csv", orange, true);
            DrawArrow(canvas, 240, 410, 450, "Leitura dos links", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 450, 200, 40, "Consulta PRs via API", blue, false);
            DrawArrow(canvas, 240, 490, 560, "Filtro: MERGED ou CLOSED\n+ >=1 revisão + >=1h", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 560, 200, 40, "prs_dataset.csv", orange, true);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, data.ToArray());
        }

        private static void GeneratePage2(string path)
        {
            var info = new SKImageInfo(480, 550);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            using var arrowTextPaint = new SKPaint { Color = SKColors.DarkGray, TextSize = 12, IsAntialias = true, TextAlign = SKTextAlign.Left, Typeface = SKTypeface.Default };
            using var arrowPaint = new SKPaint { Color = SKColors.DarkGray, IsAntialias = true, Style = SKPaintStyle.Fill, StrokeWidth = 2 };

            var orange = SKColor.Parse("#E65100");
            var blue = SKColor.Parse("#1565C0");
            var green = SKColor.Parse("#2E7D32");

            DrawEtapaBg(canvas, 10, 290, "Etapa 3: Extração de Métricas");
            DrawBox(canvas, 240, 50, 200, 40, "prs_dataset.csv", orange, true);
            DrawArrow(canvas, 240, 90, 130, "Leitura do dataset", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 130, 200, 40, "Cálculo das métricas", blue, false);
            DrawArrow(canvas, 240, 170, 230, "tamanho, tempo, descrição,\ninterações", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 230, 200, 40, "Métricas consolidadas", orange, true);

            DrawEtapaBg(canvas, 310, 220, "Etapa 4: Análise e Síntese");
            DrawBox(canvas, 240, 350, 220, 40, "Consolidação do Dataset", blue, false);
            DrawArrow(canvas, 240, 390, 430, "", arrowTextPaint, arrowPaint);
            DrawBox(canvas, 240, 430, 220, 60, "Testar Hipóteses e\nResponder RQ01-RQ08", green, true);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, data.ToArray());
        }
    }
}
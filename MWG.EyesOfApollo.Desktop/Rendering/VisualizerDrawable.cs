using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Rendering
{
    public class VisualizerDrawable : IDrawable
    {
        private readonly VisualizerState _state;
        private const float AxisLeftPadding = 48f;
        private const float AxisBottomPadding = 28f;
        private const float AxisTopPadding = 12f;
        private const float AxisRightPadding = 12f;

        public VisualizerDrawable(VisualizerState state)
        {
            _state = state;
        }

        public ThemeDefinition Theme { get; set; } = new();
        public VisualizerMode Mode { get; set; } = VisualizerMode.Bars;
        public bool ShowAxisIndicators { get; set; }
        public AmplitudeScaleMode ScaleMode { get; set; } = AmplitudeScaleMode.Normalized;
        public bool ShowPeakHold { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Theme.BackgroundColor;
            canvas.FillRectangle(dirtyRect);

            var magnitudes = _state.GetSnapshot();
            if (magnitudes.Length == 0)
            {
                return;
            }

            var peaks = ShowPeakHold ? _state.GetPeaks() : Array.Empty<float>();

            var plotArea = ShowAxisIndicators
                ? new RectF(
                    dirtyRect.Left + AxisLeftPadding,
                    dirtyRect.Top + AxisTopPadding,
                    dirtyRect.Width - AxisLeftPadding - AxisRightPadding,
                    dirtyRect.Height - AxisTopPadding - AxisBottomPadding)
                : dirtyRect;

            switch (Mode)
            {
                case VisualizerMode.Line:
                    DrawLine(canvas, plotArea, magnitudes);
                    break;
                default:
                    DrawBars(canvas, plotArea, magnitudes);
                    break;
            }

            if (ShowPeakHold && peaks.Length == magnitudes.Length)
            {
                DrawPeakHold(canvas, plotArea, peaks);
            }

            if (ShowAxisIndicators)
            {
                DrawAxes(canvas, dirtyRect, plotArea);
            }
        }

        private void DrawBars(ICanvas canvas, RectF rect, float[] magnitudes)
        {
            var count = magnitudes.Length;
            var spacing = Theme.BarSpacing;
            var barWidth = (rect.Width - (spacing * (count - 1))) / count;
            var height = rect.Height;

            canvas.FillColor = Theme.PrimaryColor;

            for (var i = 0; i < count; i++)
            {
                var value = Math.Clamp(magnitudes[i], 0, 1);
                var barHeight = value * height;
                var x = rect.Left + i * (barWidth + spacing);
                var y = rect.Bottom - barHeight;
                canvas.FillRectangle(x, y, barWidth, barHeight);
            }
        }

        private void DrawLine(ICanvas canvas, RectF rect, float[] magnitudes)
        {
            var count = magnitudes.Length;
            var height = rect.Height;
            var width = rect.Width;
            var step = width / Math.Max(1, count - 1);

            var path = new PathF();
            for (var i = 0; i < count; i++)
            {
                var value = Math.Clamp(magnitudes[i], 0, 1);
                var x = rect.Left + i * step;
                var y = rect.Bottom - (value * height);

                if (i == 0)
                {
                    path.MoveTo(x, y);
                }
                else
                {
                    path.LineTo(x, y);
                }
            }

            canvas.StrokeColor = Theme.PrimaryColor;
            canvas.StrokeSize = Theme.LineThickness;
            canvas.DrawPath(path);
        }

        private void DrawAxes(ICanvas canvas, RectF fullRect, RectF plotArea)
        {
            var axisColor = Theme.SecondaryColor.WithAlpha(0.7f);
            canvas.StrokeColor = axisColor;
            canvas.StrokeSize = 1;

            canvas.DrawLine(plotArea.Left, plotArea.Bottom, plotArea.Right, plotArea.Bottom);
            canvas.DrawLine(plotArea.Left, plotArea.Bottom, plotArea.Left, plotArea.Top);

            canvas.FontColor = axisColor;
            canvas.FontSize = 10;

            DrawFrequencyLabels(canvas, plotArea);
            DrawAmplitudeLabels(canvas, plotArea, fullRect.Left + 6);
        }

        private void DrawFrequencyLabels(ICanvas canvas, RectF plotArea)
        {
            var frequencies = new[] { 60, 250, 1000, 4000, 16000 };
            var logMin = Math.Log10(20);
            var logMax = Math.Log10(20000);

            foreach (var frequency in frequencies)
            {
                var position = (Math.Log10(frequency) - logMin) / (logMax - logMin);
                var x = plotArea.Left + (float)(position * plotArea.Width);
                var label = frequency >= 1000 ? $"{frequency / 1000}k" : frequency.ToString();
                canvas.DrawString($"{label} Hz", x, plotArea.Bottom + 6, HorizontalAlignment.Center);
            }
        }

        private void DrawAmplitudeLabels(ICanvas canvas, RectF plotArea, float leftPadding)
        {
            if (ScaleMode == AmplitudeScaleMode.Dbfs)
            {
                var labels = new[] { 0, -30, -60 };
                foreach (var db in labels)
                {
                    var normalized = (db - (-80f)) / (0f - (-80f));
                    var y = plotArea.Bottom - (normalized * plotArea.Height);
                    canvas.DrawString($"{db} dB", leftPadding, y - 6, HorizontalAlignment.Left);
                }

                return;
            }

            var values = new[] { 1f, 0.5f, 0f };
            foreach (var value in values)
            {
                var y = plotArea.Bottom - (value * plotArea.Height);
                canvas.DrawString($"{value * 100:0}%", leftPadding, y - 6, HorizontalAlignment.Left);
            }

        }

        private void DrawPeakHold(ICanvas canvas, RectF rect, float[] peaks)
        {
            var count = peaks.Length;
            if (count == 0)
            {
                return;
            }

            var spacing = Theme.BarSpacing;
            var barWidth = (rect.Width - (spacing * (count - 1))) / count;
            var height = rect.Height;

            canvas.StrokeColor = Theme.SecondaryColor;
            canvas.StrokeSize = 2;

            for (var i = 0; i < count; i++)
            {
                var value = Math.Clamp(peaks[i], 0, 1);
                var y = rect.Bottom - (value * height);
                var x = rect.Left + i * (barWidth + spacing);
                canvas.DrawLine(x, y, x + barWidth, y);
            }
        }
    }
}

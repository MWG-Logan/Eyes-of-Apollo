using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Rendering
{
    public class VisualizerDrawable : IDrawable
    {
        private readonly VisualizerState _state;

        public VisualizerDrawable(VisualizerState state)
        {
            _state = state;
        }

        public ThemeDefinition Theme { get; set; } = new();
        public VisualizerMode Mode { get; set; } = VisualizerMode.Bars;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Theme.BackgroundColor;
            canvas.FillRectangle(dirtyRect);

            var magnitudes = _state.GetSnapshot();
            if (magnitudes.Length == 0)
            {
                return;
            }

            switch (Mode)
            {
                case VisualizerMode.Line:
                    DrawLine(canvas, dirtyRect, magnitudes);
                    break;
                default:
                    DrawBars(canvas, dirtyRect, magnitudes);
                    break;
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
    }
}

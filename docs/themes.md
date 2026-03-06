# Themes

Themes are JSON files stored in the app's `Themes` directory (relative to the app run location). Each theme controls colors and basic visualizer settings.

## Example
```json
{
  "name": "Aurora",
  "background": "#0B0D17",
  "primary": "#6C63FF",
  "secondary": "#F5A623",
  "mode": "Bars",
  "barCount": 96,
  "lineThickness": 2.0,
  "barSpacing": 2.0
}
```

## Fields
- `name`: Display name shown in the UI.
- `background`: Hex background color.
- `primary`: Hex primary color (bars/line).
- `secondary`: Hex secondary color (axes/peaks).
- `mode`: `Bars` or `Line`.
- `barCount`: Number of bins.
- `lineThickness`: Line width when using `Line`.
- `barSpacing`: Spacing between bars.

## Notes
- If no themes exist, a default theme is created on startup.

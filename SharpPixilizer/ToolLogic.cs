//ToolLogic.cs - логика работы инструментов

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SharpPixilizer
{
    public partial class GridCanvas : Canvas
    {
        //private void DrawLine(int x0, int y0, int x1, int y1, bool drawWhite)
        //{
        //    int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        //    int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        //    int err = dx - dy;
        //    while (true)
        //    {
        //        if (x0 >= 0 && x0 < GridColumns && y0 >= 0 && y0 < GridRows)
        //            gridState[x0, y0] = drawWhite;
        //        if (x0 == x1 && y0 == y1) break;
        //        int e2 = 2 * err;
        //        if (e2 > -dy) { err -= dy; x0 += sx; }
        //        if (e2 < dx) { err += dx; y0 += sy; }
        //    }
        //}

        //private void DrawRectangle(int x0, int y0, int x1, int y1, bool drawWhite)
        //{
        //    int startX = Math.Min(x0, x1), endX = Math.Max(x0, x1);
        //    int startY = Math.Min(y0, y1), endY = Math.Max(y0, y1);
        //    for (int i = startX; i <= endX; i++)
        //        for (int j = startY; j <= endY; j++)
        //            if (i >= 0 && i < GridColumns && j >= 0 && j < GridRows)
        //                gridState[i, j] = drawWhite;
        //}

        //private void DrawEllipse(int x0, int y0, int x1, int y1, bool drawWhite)
        //{
        //    int startX = Math.Min(x0, x1), endX = Math.Max(x0, x1);
        //    int startY = Math.Min(y0, y1), endY = Math.Max(y0, y1);
        //    double a = (endX - startX) / 2.0;
        //    double b = (endY - startY) / 2.0;
        //    double centerX = startX + a;
        //    double centerY = startY + b;
        //    if (a == 0 || b == 0)
        //    {
        //        DrawRectangle(x0, y0, x1, y1, drawWhite);
        //        return;
        //    }
        //    for (int i = startX; i <= endX; i++)
        //        for (int j = startY; j <= endY; j++)
        //        {
        //            double normX = (i - centerX) / a;
        //            double normY = (j - centerY) / b;
        //            if (normX * normX + normY * normY <= 1.0)
        //                if (i >= 0 && i < GridColumns && j >= 0 && j < GridRows)
        //                    gridState[i, j] = drawWhite;
        //        }
        //}

        //#region additional tools
        //public void ToggleGuides()
        //{
        //    ShowGuides = !ShowGuides;
        //    InvalidateVisual();
        //}

        //private void DrawGuides(DrawingContext dc)
        //{
        //    int centerColumn = GridColumns / 2;
        //    int centerRow = GridRows / 2;
        //    Pen guidePen = new Pen(Brushes.Red, 1 / scale) { DashStyle = DashStyles.Dash };

        //    // Для горизонтальной линии
        //    double guideX = GridColumns % 2 == 0
        //        ? centerColumn * CellSize // Четное количество клеток, линия на линии соприкосновения
        //        : (centerColumn + 0.5) * CellSize; // Нечетное количество клеток, линия посередине центральной клетки

        //    // Для вертикальной линии
        //    double guideY = GridRows % 2 == 0
        //        ? centerRow * CellSize // Четное количество клеток, линия на линии соприкосновения
        //        : (centerRow + 0.5) * CellSize; // Нечетное количество клеток, линия посередине центральной клетки

        //    dc.DrawLine(guidePen, new Point(guideX, 0), new Point(guideX, GridRows * CellSize));
        //    dc.DrawLine(guidePen, new Point(0, guideY), new Point(GridColumns * CellSize, guideY));
        //}

        //public void ClearGrid()
        //{
        //    gridState = new bool[GridColumns, GridRows];
        //    InvalidateVisual();
        //}

        //public void CenterGrid()
        //{
        //    double gridWidthPixels = GridColumns * CellSize * scale;
        //    double gridHeightPixels = GridRows * CellSize * scale;
        //    double canvasWidth = ActualWidth;
        //    double canvasHeight = ActualHeight;
        //    offset = new Point((canvasWidth - gridWidthPixels) / 2, (canvasHeight - gridHeightPixels) / 2);
        //    InvalidateVisual();
        //}

        //public void ChangeGridSize()
        //{
        //    // Циклическое изменение размера: 96x16 -> 128x32 -> 192x48 -> 96x16 ->
        //    if (GridColumns == 96 && GridRows == 16)
        //    {
        //        GridColumns = 128;
        //        GridRows = 32;
        //    }
        //    else if (GridColumns == 128 && GridRows == 32)
        //    {
        //        GridColumns = 192;
        //        GridRows = 48;
        //    }
        //    else
        //    {
        //        GridColumns = 96;
        //        GridRows = 16;
        //    }
        //    gridState = new bool[GridColumns, GridRows];
        //    InvalidateVisual();
        //}

        //#endregion
    }
}

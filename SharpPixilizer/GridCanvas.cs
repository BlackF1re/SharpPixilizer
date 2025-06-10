using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharpPixilizer
{
    public partial class GridCanvas : Canvas
    {
        // Свойства сетки
        public int GridColumns { get; private set; } = 96;
        public int GridRows { get; private set; } = 16;
        public double CellSize { get; private set; } = 20;
        internal int gridPenThickness = 1;

        // Состояние ячеек (true = белый, false = чёрный)
        internal bool[,] gridState;

        // Параметры масштабирования и смещения
        private double scale = 1.0;
        private Point offset = new Point(0, 0);

        // Панорамирование
        private bool isPanning = false;
        private Point lastPanPoint;

        // Текущий инструмент
        public ToolType CurrentTool { get; set; } = ToolType.Pencil;

        // Точки для рисования фигур или выделения (координаты в ячейках)
        private Point? startCell = null;
        private Point? previewEndCell = null;

        // Режим стирания
        private bool isErasing = false;

        // Флаг отображения направляющих
        public bool ShowGuides { get; private set; } = false;
        public double CurrentScale => scale;

        // Выделение – представлено прямоугольником по границам сетки
        internal Rect? selectionRect = null;

        // Undo/Redo и история изменений
        internal Stack<bool[,]> undoStack = new Stack<bool[,]>();
        internal Stack<bool[,]> redoStack = new Stack<bool[,]>();
        internal List<string> history = new List<string>();

        public GridCanvas()
        {
            gridState = new bool[GridColumns, GridRows];
            Background = Brushes.Black;

            this.MouseDown += GridCanvas_MouseDown;
            this.MouseMove += GridCanvas_MouseMove;
            this.MouseUp += GridCanvas_MouseUp;
            this.MouseWheel += GridCanvas_MouseWheel;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.PushTransform(new TranslateTransform(offset.X, offset.Y));
            dc.PushTransform(new ScaleTransform(scale, scale));

            Pen gridPen = new Pen(Brushes.White, gridPenThickness / scale);
            DrawGrid(dc, gridPen);

            if (ShowGuides)
                DrawGuides(dc);

            if (selectionRect.HasValue)
            {
                Pen selPen = new Pen(Brushes.Yellow, 2 / scale);
                Rect sel = new Rect(selectionRect.Value.X * CellSize, selectionRect.Value.Y * CellSize,
                    selectionRect.Value.Width * CellSize, selectionRect.Value.Height * CellSize);
                dc.DrawRectangle(null, selPen, sel);
            }

            if (startCell.HasValue && previewEndCell.HasValue &&
               (CurrentTool == ToolType.Line || CurrentTool == ToolType.Rectangle || CurrentTool == ToolType.Ellipse))
            {
                bool previewDrawWhite = !isErasing;
                Brush previewBrush = previewDrawWhite ? Brushes.White : Brushes.Black;
                int sx = (int)startCell.Value.X, sy = (int)startCell.Value.Y;
                int ex = (int)previewEndCell.Value.X, ey = (int)previewEndCell.Value.Y;
                if (CurrentTool == ToolType.Line)
                    DrawLinePreview(dc, sx, sy, ex, ey, previewBrush);
                else if (CurrentTool == ToolType.Rectangle)
                    DrawRectanglePreview(dc, sx, sy, ex, ey, previewBrush);
                else if (CurrentTool == ToolType.Ellipse)
                    DrawEllipsePreview(dc, sx, sy, ex, ey, previewBrush);
            }

            dc.Pop();
            dc.Pop();
        }

        internal void DrawGrid(DrawingContext dc, Pen gridPen)
        {
            for (int i = 0; i < GridColumns; i++)
            {
                for (int j = 0; j < GridRows; j++)
                {
                    Brush cellBrush = gridState[i, j] ? Brushes.White : Brushes.Black;
                    Rect cellRect = new Rect(i * CellSize, j * CellSize, CellSize, CellSize);
                    dc.DrawRectangle(cellBrush, null, cellRect);
                }
            }
            for (int i = 0; i <= GridColumns; i++)
            {
                double x = i * CellSize;
                dc.DrawLine(gridPen, new Point(x, 0), new Point(x, GridRows * CellSize));
            }
            for (int j = 0; j <= GridRows; j++)
            {
                double y = j * CellSize;
                dc.DrawLine(gridPen, new Point(0, y), new Point(GridColumns * CellSize, y));
            }
        }

        internal void DrawGuides(DrawingContext dc)
        {
            int centerColumn = GridColumns / 2;
            int centerRow = GridRows / 2;
            Pen guidePen = new Pen(Brushes.Red, 1 / scale) { DashStyle = DashStyles.Dash };
            double guideX = GridColumns % 2 == 0 ? centerColumn * CellSize : (centerColumn + 0.5) * CellSize;
            double guideY = GridRows % 2 == 0 ? centerRow * CellSize : (centerRow + 0.5) * CellSize;
            dc.DrawLine(guidePen, new Point(guideX, 0), new Point(guideX, GridRows * CellSize));
            dc.DrawLine(guidePen, new Point(0, guideY), new Point(GridColumns * CellSize, guideY));
        }

        #region Mouse Handlers
        internal void GridCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            Point mousePos = e.GetPosition(this);
            Point gridPos = new Point((mousePos.X - offset.X) / scale, (mousePos.Y - offset.Y) / scale);
            int cellX = (int)(gridPos.X / CellSize);
            int cellY = (int)(gridPos.Y / CellSize);
            if (cellX < 0 || cellX >= GridColumns || cellY < 0 || cellY >= GridRows)
                return;

            if (e.ChangedButton == MouseButton.Middle)
            {
                isPanning = true;
                lastPanPoint = mousePos;
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                // Если режим заливки, выполняем заливку сразу
                if (CurrentTool == ToolType.Fill)
                {
                    SaveStateForUndo();
                    FillAreaAtCell(cellX, cellY);
                    // После заливки переключаем на кисть (опционально)
                    CurrentTool = ToolType.Pencil;
                    return;
                }
                // Если режим выделения
                else if (CurrentTool == ToolType.RectangularSelection || CurrentTool == ToolType.FreeSelection)
                {
                    startCell = new Point(cellX, cellY);
                    previewEndCell = new Point(cellX, cellY);
                }
                else
                {
                    SaveStateForUndo();
                    if (CurrentTool == ToolType.Pencil)
                    {
                        gridState[cellX, cellY] = true;
                        InvalidateVisual();
                    }
                    else if (CurrentTool == ToolType.Line || CurrentTool == ToolType.Rectangle || CurrentTool == ToolType.Ellipse)
                    {
                        startCell = new Point(cellX, cellY);
                        previewEndCell = new Point(cellX, cellY);
                    }
                }
            }
        }

        internal void GridCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(this);
                Vector delta = mousePos - lastPanPoint;
                offset.X += delta.X;
                offset.Y += delta.Y;
                lastPanPoint = mousePos;
                InvalidateVisual();
            }
            else if ((CurrentTool == ToolType.Pencil) && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(this);
                Point gridPos = new Point((mousePos.X - offset.X) / scale, (mousePos.Y - offset.Y) / scale);
                int cellX = (int)(gridPos.X / CellSize);
                int cellY = (int)(gridPos.Y / CellSize);
                if (cellX >= 0 && cellX < GridColumns && cellY >= 0 && cellY < GridRows)
                {
                    gridState[cellX, cellY] = true;
                    InvalidateVisual();
                }
            }
            else if ((CurrentTool == ToolType.Line || CurrentTool == ToolType.Rectangle ||
                      CurrentTool == ToolType.Ellipse || CurrentTool == ToolType.RectangularSelection ||
                      CurrentTool == ToolType.FreeSelection)
                     && e.LeftButton == MouseButtonState.Pressed && startCell.HasValue)
            {
                Point mousePos = e.GetPosition(this);
                Point gridPos = new Point((mousePos.X - offset.X) / scale, (mousePos.Y - offset.Y) / scale);
                int cellX = (int)(gridPos.X / CellSize);
                int cellY = (int)(gridPos.Y / CellSize);
                previewEndCell = new Point(cellX, cellY);
                if (CurrentTool == ToolType.RectangularSelection || CurrentTool == ToolType.FreeSelection)
                {
                    UpdateSelection(startCell.Value, previewEndCell.Value);
                }
                InvalidateVisual();
            }
        }

        internal void GridCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isPanning && e.ChangedButton == MouseButton.Middle)
            {
                isPanning = false;
                return;
            }
            if ((CurrentTool == ToolType.Line || CurrentTool == ToolType.Rectangle || CurrentTool == ToolType.Ellipse ||
                CurrentTool == ToolType.RectangularSelection || CurrentTool == ToolType.FreeSelection)
                && startCell.HasValue && previewEndCell.HasValue)
            {
                if (CurrentTool == ToolType.RectangularSelection || CurrentTool == ToolType.FreeSelection)
                {
                    UpdateSelection(startCell.Value, previewEndCell.Value);
                }
                else
                {
                    int sx = (int)startCell.Value.X, sy = (int)startCell.Value.Y;
                    int ex = (int)previewEndCell.Value.X, ey = (int)previewEndCell.Value.Y;
                    bool drawWhite = true;
                    if (CurrentTool == ToolType.Line)
                        DrawLine(sx, sy, ex, ey, drawWhite);
                    else if (CurrentTool == ToolType.Rectangle)
                        DrawRectangle(sx, sy, ex, ey, drawWhite);
                    else if (CurrentTool == ToolType.Ellipse)
                        DrawEllipse(sx, sy, ex, ey, drawWhite);
                }
                startCell = null;
                previewEndCell = null;
                InvalidateVisual();
            }
        }

        internal void GridCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            offset = new Point(
                mousePos.X - zoomFactor * (mousePos.X - offset.X),
                mousePos.Y - zoomFactor * (mousePos.Y - offset.Y)
            );
            scale *= zoomFactor;
            InvalidateVisual();
        }
        #endregion

        #region Методы инструментов

        // Рисование фигур
        internal void DrawLine(int x0, int y0, int x1, int y1, bool drawWhite)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                if (x0 >= 0 && x0 < GridColumns && y0 >= 0 && y0 < GridRows)
                    gridState[x0, y0] = drawWhite;
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        internal void DrawRectangle(int x0, int y0, int x1, int y1, bool drawWhite)
        {
            int startX = Math.Min(x0, x1), endX = Math.Max(x0, x1);
            int startY = Math.Min(y0, y1), endY = Math.Max(y0, y1);
            for (int i = startX; i <= endX; i++)
                for (int j = startY; j <= endY; j++)
                    if (i >= 0 && i < GridColumns && j >= 0 && j < GridRows)
                        gridState[i, j] = drawWhite;
        }

            internal void DrawEllipse(int x0, int y0, int x1, int y1, bool drawWhite)
            {
                int startX = Math.Min(x0, x1), endX = Math.Max(x0, x1);
                int startY = Math.Min(y0, y1), endY = Math.Max(y0, y1);
                double a = (endX - startX) / 2.0;
                double b = (endY - startY) / 2.0;
                double centerX = startX + a;
                double centerY = startY + b;
                if (a == 0 || b == 0)
                {
                    DrawRectangle(x0, y0, x1, y1, drawWhite);
                    return;
                }
                for (int i = startX; i <= endX; i++)
                    for (int j = startY; j <= endY; j++)
                    {
                        double normX = (i - centerX) / a;
                        double normY = (j - centerY) / b;
                        if (normX * normX + normY * normY <= 1.0)
                            if (i >= 0 && i < GridColumns && j >= 0 && j < GridRows)
                                gridState[i, j] = drawWhite;
                    }
            }

        // Превью рисования
        internal void DrawLinePreview(DrawingContext dc, int startX, int startY, int endX, int endY, Brush previewBrush)
        {
            int x0 = startX, y0 = startY, x1 = endX, y1 = endY;
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                Rect cellRect = new Rect(x0 * CellSize, y0 * CellSize, CellSize, CellSize);
                dc.DrawRectangle(previewBrush, null, cellRect);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        internal void DrawRectanglePreview(DrawingContext dc, int startX, int startY, int endX, int endY, Brush previewBrush)
        {
            int x0 = Math.Min(startX, endX), x1 = Math.Max(startX, endX);
            int y0 = Math.Min(startY, endY), y1 = Math.Max(startY, endY);
            for (int i = x0; i <= x1; i++)
                for (int j = y0; j <= y1; j++)
                {
                    Rect cellRect = new Rect(i * CellSize, j * CellSize, CellSize, CellSize);
                    dc.DrawRectangle(previewBrush, null, cellRect);
                }
        }

        internal void DrawEllipsePreview(DrawingContext dc, int startX, int startY, int endX, int endY, Brush previewBrush)
        {
            int x0 = Math.Min(startX, endX), x1 = Math.Max(startX, endX);
            int y0 = Math.Min(startY, endY), y1 = Math.Max(startY, endY);
            double a = (x1 - x0) / 2.0;
            double b = (y1 - y0) / 2.0;
            double centerX = x0 + a;
            double centerY = y0 + b;
            for (int i = x0; i <= x1; i++)
                for (int j = y0; j <= y1; j++)
                {
                    double normX = (i - centerX) / a;
                    double normY = (j - centerY) / b;
                    if (normX * normX + normY * normY <= 1.0)
                    {
                        Rect cellRect = new Rect(i * CellSize, j * CellSize, CellSize, CellSize);
                        dc.DrawRectangle(previewBrush, null, cellRect);
                    }
                }
        }
        #endregion

        #region Дополнительный функционал

        internal void ToggleGuides()
        {
            ShowGuides = !ShowGuides;
            InvalidateVisual();
        }

        internal void ClearGrid()
        {
            gridState = new bool[GridColumns, GridRows];
            selectionRect = null;
            ResetHistory();
            InvalidateVisual();
        }

        internal void CenterGrid()
        {
            double gridWidthPixels = GridColumns * CellSize * scale;
            double gridHeightPixels = GridRows * CellSize * scale;
            double canvasWidth = ActualWidth;
            double canvasHeight = ActualHeight;
            offset = new Point((canvasWidth - gridWidthPixels) / 2, (canvasHeight - gridHeightPixels) / 2);
            InvalidateVisual();
        }

        internal void ChangeGridSize()
        {
            if (GridColumns == 96 && GridRows == 16)
                SetGridSize(128, 32);
            else if (GridColumns == 128 && GridRows == 32)
                SetGridSize(192, 48);
            else
                SetGridSize(96, 16);
        }

        internal void SetGridSize(int columns, int rows)
        {
            GridColumns = columns;
            GridRows = rows;
            gridState = new bool[GridColumns, GridRows];
            selectionRect = null;
            InvalidateVisual();
        }

        // Undo/Redo и история
        internal void SaveStateForUndo()
        {
            bool[,] stateCopy = (bool[,])gridState.Clone();
            undoStack.Push(stateCopy);
            history.Add("Изменение");
            redoStack.Clear();
        }

        internal void Undo()
        {
            if (undoStack.Count > 0)
            {
                bool[,] currentState = (bool[,])gridState.Clone();
                redoStack.Push(currentState);
                gridState = undoStack.Pop();
                InvalidateVisual();
            }
        }

        internal void Redo()
        {
            if (redoStack.Count > 0)
            {
                bool[,] currentState = (bool[,])gridState.Clone();
                undoStack.Push(currentState);
                gridState = redoStack.Pop();
                InvalidateVisual();
            }
        }

        internal List<string> GetHistory() => history;

        internal void ResetHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
            history.Clear();
        }

        // Выделение по границам сетки
        internal void SelectAll()
        {
            selectionRect = new Rect(0, 0, GridColumns, GridRows);
            InvalidateVisual();
        }

        internal void Deselect()
        {
            selectionRect = null;
            InvalidateVisual();
        }

        internal void InvertSelection()
        {
            MessageBox.Show("Инвертирование выделения не реализовано.");
        }

        // Обновление выделения по двум точкам (выравнивание по границам сетки)
        internal void UpdateSelection(Point start, Point end)
        {
            double x0 = Math.Min(start.X, end.X);
            double y0 = Math.Min(start.Y, end.Y);
            double x1 = Math.Max(start.X, end.X);
            double y1 = Math.Max(start.Y, end.Y);
            // Выделение всегда по границам ячеек
            selectionRect = new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        // Закругление выделенного прямоугольника (по нажатию кнопки)
        internal void RoundRectangleSelection()
        {
            if (!selectionRect.HasValue)
            {
                MessageBox.Show("Нет выделенного прямоугольника.");
                return;
            }
            int margin = 2;
            int startX = (int)selectionRect.Value.X;
            int startY = (int)selectionRect.Value.Y;
            int endX = startX + (int)selectionRect.Value.Width - 1;
            int endY = startY + (int)selectionRect.Value.Height - 1;
            for (int i = startX; i < startX + margin && i <= endX; i++)
                for (int j = startY; j < startY + margin && j <= endY; j++)
                    gridState[i, j] = false;
            for (int i = endX - margin + 1; i <= endX; i++)
                for (int j = startY; j < startY + margin && j <= endY; j++)
                    gridState[i, j] = false;
            for (int i = startX; i < startX + margin && i <= endX; i++)
                for (int j = endY - margin + 1; j <= endY; j++)
                    gridState[i, j] = false;
            for (int i = endX - margin + 1; i <= endX; i++)
                for (int j = endY - margin + 1; j <= endY; j++)
                    gridState[i, j] = false;
            InvalidateVisual();
        }

        // Заливка по клику в режиме Fill
        internal void FillAreaAtCell(int cellX, int cellY)
        {
            SaveStateForUndo();
            bool targetColor = gridState[cellX, cellY];
            bool fillColor = true; // Заливаем белым
            FloodFill(cellX, cellY, targetColor, fillColor);
            InvalidateVisual();
        }

        internal void FloodFill(int x, int y, bool targetColor, bool fillColor)
        {
            if (x < 0 || x >= GridColumns || y < 0 || y >= GridRows)
                return;
            if (gridState[x, y] != targetColor)
                return;
            gridState[x, y] = fillColor;
            FloodFill(x + 1, y, targetColor, fillColor);
            FloodFill(x - 1, y, targetColor, fillColor);
            FloodFill(x, y + 1, targetColor, fillColor);
            FloodFill(x, y - 1, targetColor, fillColor);
        }

        // Зум
        internal void ZoomIn()
        {
            scale *= 1.1;
            InvalidateVisual();
        }

        internal void ZoomOut()
        {
            scale /= 1.1;
            InvalidateVisual();
        }

        internal void ZoomReset()
        {
            scale = 1.0;
            InvalidateVisual();
        }

        // Поворот (реализация для кратных 90° и 180°)
        internal void Rotate(int angle)
        {
            angle = angle % 360;
            if (angle < 0) angle += 360;
            if (angle == 90 || angle == 270)
            {
                int newColumns = GridRows;
                int newRows = GridColumns;
                bool[,] newState = new bool[newColumns, newRows];
                for (int i = 0; i < GridColumns; i++)
                    for (int j = 0; j < GridRows; j++)
                    {
                        if (angle == 90)
                            newState[j, newRows - i - 1] = gridState[i, j];
                        else
                            newState[newColumns - j - 1, i] = gridState[i, j];
                    }
                GridColumns = newColumns;
                GridRows = newRows;
                gridState = newState;
            }
            else if (angle == 180)
            {
                bool[,] newState = new bool[GridColumns, GridRows];
                for (int i = 0; i < GridColumns; i++)
                    for (int j = 0; j < GridRows; j++)
                        newState[GridColumns - i - 1, GridRows - j - 1] = gridState[i, j];
                gridState = newState;
            }
            InvalidateVisual();
        }

        // Зеркальное отображение
        internal void ResetMirrorRotate()
        {
            InvalidateVisual();
        }

        internal void MirrorHorizontal()
        {
            bool[,] newState = new bool[GridColumns, GridRows];
            for (int i = 0; i < GridColumns; i++)
                for (int j = 0; j < GridRows; j++)
                    newState[GridColumns - i - 1, j] = gridState[i, j];
            gridState = newState;
            InvalidateVisual();
        }

        internal void MirrorVertical()
        {
            bool[,] newState = new bool[GridColumns, GridRows];
            for (int i = 0; i < GridColumns; i++)
                for (int j = 0; j < GridRows; j++)
                    newState[i, GridRows - j - 1] = gridState[i, j];
            gridState = newState;
            InvalidateVisual();
        }

        // Загрузка изображения в холст (BMP преобразуется в сетку)
        internal void LoadImage(BitmapImage image)
        {
            WriteableBitmap wb = new WriteableBitmap(image);
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            gridState = new bool[width, height];
            GridColumns = width;
            GridRows = height;
            byte[] pixels = new byte[width * height * 4];
            wb.CopyPixels(pixels, width * 4, 0);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 4;
                    byte r = pixels[idx];
                    byte g = pixels[idx + 1];
                    byte b = pixels[idx + 2];
                    gridState[x, y] = ((r + g + b) / 3) > 128;
                }
            InvalidateVisual();
        }

        // Подгонка окна по изображению
        internal void FitWindowToImage()
        {
            double desiredWidth = GridColumns * CellSize * scale + 50;
            double desiredHeight = GridRows * CellSize * scale + 100;
            Window parent = Window.GetWindow(this);
            if (parent != null)
            {
                parent.Width = desiredWidth;
                parent.Height = desiredHeight;
            }
        }

        // Методы для переключения режима выделения
        internal void ActivateRectangularSelection()
        {
            CurrentTool = ToolType.RectangularSelection;
            selectionRect = null;
        }

        internal void ActivateFreeSelection()
        {
            CurrentTool = ToolType.FreeSelection;
            selectionRect = null;
        }

        // Копирование и вырезание
        internal void CopySelection()
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(GridColumns * (int)CellSize, GridRows * (int)CellSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(this);
            Clipboard.SetImage(rtb);
            MessageBox.Show("Выделенная область скопирована.");
        }

        internal void CutSelection()
        {
            CopySelection();
            if (selectionRect.HasValue)
            {
                int sx = (int)selectionRect.Value.X, sy = (int)selectionRect.Value.Y;
                int ex = sx + (int)selectionRect.Value.Width;
                int ey = sy + (int)selectionRect.Value.Height;
                for (int i = sx; i < ex; i++)
                    for (int j = sy; j < ey; j++)
                        gridState[i, j] = false;
                Deselect();
                InvalidateVisual();
            }
        }

        internal void CopyVisible()
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(GridColumns * (int)CellSize, GridRows * (int)CellSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(this);
            Clipboard.SetImage(rtb);
            MessageBox.Show("Видимое изображение скопировано.");
        }
        #endregion
    }
}

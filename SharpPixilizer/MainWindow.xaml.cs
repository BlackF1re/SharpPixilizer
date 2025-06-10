using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media.Imaging;

namespace SharpPixilizer
{
    public partial class MainWindow : Window
    {
        private List<string> recentFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            DrawingCanvas.CurrentTool = ToolType.Pencil;
            UpdateRecentFilesMenu();
        }

        #region Файл
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ClearGrid();
            DrawingCanvas.ResetHistory();
            MessageBox.Show("Новый документ создан.");
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Bitmap Image|*.bmp";
            if (dlg.ShowDialog() == true)
            {
                BitmapImage bmp = new BitmapImage(new Uri(dlg.FileName));
                DrawingCanvas.LoadImage(bmp);
                AddRecentFile(dlg.FileName);
            }
        }

        private void AddRecentFile(string filePath)
        {
            if (!recentFiles.Contains(filePath))
            {
                recentFiles.Add(filePath);
                UpdateRecentFilesMenu();
            }
        }

        private void UpdateRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();
            foreach (string file in recentFiles)
            {
                var item = new System.Windows.Controls.MenuItem();
                item.Header = file;
                item.Click += (s, e) =>
                {
                    BitmapImage bmp = new BitmapImage(new Uri(file));
                    DrawingCanvas.LoadImage(bmp);
                };
                RecentFilesMenu.Items.Add(item);
            }
        }

        private void ChangeSizeButton_Click(object sender, RoutedEventArgs e)
        {
            GridSizeDialog dlg = new GridSizeDialog(DrawingCanvas.GridColumns, DrawingCanvas.GridRows);
            if (dlg.ShowDialog() == true)
            {
                DrawingCanvas.SetGridSize(dlg.GridColumns, dlg.GridRows);
            }
        }
        #endregion

        #region Правка
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Redo();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryDialog dlg = new HistoryDialog(DrawingCanvas.GetHistory());
            dlg.ShowDialog();
        }

        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CutSelection();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CopySelection();
        }

        private void CopyVisibleButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CopyVisible();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ClearGrid();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            //SettingsDialog dlg = new SettingsDialog();
            //dlg.ShowDialog();
        }

        private void KeyboardShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Окно настройки комбинаций клавиш не реализовано.", "Информация");

            //SettingsDialog dlg = new SettingsDialog();
            //dlg.SwitchToKeyboardTab();
            //dlg.ShowDialog();
        }
        #endregion

        #region Выделение
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.SelectAll();
        }

        private void DeselectButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Deselect();
        }

        private void InvertSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.InvertSelection();
        }

        private void RoundRectangleButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.RoundRectangleSelection();
        }
        #endregion

        #region Просмотр
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ZoomOut();
        }

        private void Zoom100Button_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ZoomReset();
        }

        private void ResetMirrorRotateButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ResetMirrorRotate();
        }

        private void MirrorHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.MirrorHorizontal();
        }

        private void MirrorVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.MirrorVertical();
        }

        private void Rotate150CWButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Rotate(150);
        }

        private void Rotate150CCWButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Rotate(-150);
        }

        private void Rotate90CWButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Rotate(90);
        }

        private void Rotate90CCWButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Rotate(-90);
        }

        private void Rotate180Button_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Rotate(180);
        }

        private void CustomRotateButton_Click(object sender, RoutedEventArgs e)
        {
            CustomRotateDialog dlg = new CustomRotateDialog();
            if (dlg.ShowDialog() == true)
            {
                DrawingCanvas.Rotate(dlg.RotationAngle);
            }
        }

        private void CenterImageButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CenterGrid();
        }

        private void FitWindowToImageButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.FitWindowToImage();
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void ToggleGuidesButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ToggleGuides();
        }

        private void ToggleFullscreen()
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
            }
        }
        #endregion

        #region Инструменты
        private void RectangularSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ActivateRectangularSelection();
        }

        private void FreeSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ActivateFreeSelection();
        }

        private void PencilButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CurrentTool = ToolType.Pencil;
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CurrentTool = ToolType.Line;
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CurrentTool = ToolType.Fill;
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CurrentTool = ToolType.Rectangle;
        }

        private void EllipseButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.CurrentTool = ToolType.Ellipse;
        }
        #endregion
    }
}

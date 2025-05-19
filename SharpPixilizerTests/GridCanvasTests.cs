using SharpPixilizer;
using System.Windows;

namespace SharpPixilizerTests
{
    [TestClass]
    public class GridCanvasTests
    {
        private static void RunInSta(Action action)
        {
            Exception? threadEx = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { threadEx = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
                //Console.WriteLine("STA exception: " + threadEx);
            throw new AssertFailedException("STA thread trowed an exception", threadEx);
        }

        private bool[,] GetGridState(GridCanvas canvas) => canvas.gridState;

        private void AssertGridStateEquals(bool[,] expected, bool[,] actual, string message = "")
        {
            Assert.AreEqual(expected.GetLength(0), actual.GetLength(0), $"{message} - Grid columns mismatch.");
            Assert.AreEqual(expected.GetLength(1), actual.GetLength(1), $"{message} - Grid rows mismatch.");
            for (int i = 0; i < expected.GetLength(0); i++)
                for (int j = 0; j < expected.GetLength(1); j++)
                    Assert.AreEqual(expected[i, j], actual[i, j], $"{message} - Cell ({i},{j}) state mismatch.");
        }

        [TestMethod]
        public void SetGridSize_UpdatesDimensionsAndCreatesNewGridState()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(25, 15);

                Assert.AreEqual(25, canvas.GridColumns);
                Assert.AreEqual(15, canvas.GridRows);

                var currentState = GetGridState(canvas);
                Assert.AreEqual(25, currentState.GetLength(0));
                Assert.AreEqual(15, currentState.GetLength(1));

                for (int i = 0; i < 25; i++)
                    for (int j = 0; j < 15; j++)
                        Assert.IsFalse(currentState[i, j]);

                canvas.SetGridSize(5, 5);
                currentState = GetGridState(canvas);
                Assert.AreEqual(5, currentState.GetLength(0));
                Assert.AreEqual(5, currentState.GetLength(1));
            });
        }

        [TestMethod]
        public void ClearGrid_ResetsGridStateAndSelection()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                canvas.gridState[1, 1] = true;
                canvas.gridState[5, 5] = true;
                canvas.selectionRect = new Rect(1, 1, 2, 2);
                canvas.SaveStateForUndo();

                canvas.ClearGrid();

                var currentState = GetGridState(canvas);
                for (int i = 0; i < canvas.GridColumns; i++)
                    for (int j = 0; j < canvas.GridRows; j++)
                        Assert.IsFalse(currentState[i, j]);

                Assert.IsFalse(canvas.selectionRect.HasValue);
                Assert.AreEqual(0, canvas.undoStack.Count);
                Assert.AreEqual(0, canvas.redoStack.Count);
                Assert.AreEqual(0, canvas.history.Count);
            });
        }

        [TestMethod]
        public void ChangeGridSize_CyclesThroughPredefinedSizes()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(96, 16);

                canvas.ChangeGridSize();
                Assert.AreEqual(128, canvas.GridColumns);
                Assert.AreEqual(32, canvas.GridRows);

                canvas.ChangeGridSize();
                Assert.AreEqual(192, canvas.GridColumns);
                Assert.AreEqual(48, canvas.GridRows);

                canvas.ChangeGridSize();
                Assert.AreEqual(96, canvas.GridColumns);
                Assert.AreEqual(16, canvas.GridRows);
            });
        }

        [TestMethod]
        public void SaveStateForUndo_PushesCurrentStateToUndoStackAndClearsRedoStack()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                canvas.gridState[1, 1] = true;

                canvas.SaveStateForUndo();

                var undoStack = canvas.undoStack;
                var redoStack = canvas.redoStack;
                var history = canvas.history;

                Assert.AreEqual(1, undoStack.Count);
                Assert.AreEqual(0, redoStack.Count);
                Assert.AreEqual(1, history.Count);
                Assert.AreEqual("Изменение", history[0]);

                bool[,] savedState = undoStack.Peek();
                AssertGridStateEquals(canvas.gridState, savedState);

                var gridBeforeSecondSave = (bool[,])canvas.gridState.Clone();
                gridBeforeSecondSave[2, 2] = true;
                canvas.gridState = gridBeforeSecondSave;

                canvas.SaveStateForUndo();

                Assert.AreEqual(2, undoStack.Count);
                Assert.AreEqual(0, redoStack.Count);
                Assert.AreEqual(2, history.Count);
                Assert.AreEqual("Изменение", history[1]);

                var secondSaved = undoStack.Peek();
                AssertGridStateEquals(canvas.gridState, secondSaved);
            });
        }

        [TestMethod]
        public void Undo_RestoresPreviousStateAndPushesCurrentStateToRedoStack_WhenUndoStackIsNotEmpty()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);

                bool[,] state1 = new bool[10, 10];
                canvas.gridState = state1;
                canvas.SaveStateForUndo();

                bool[,] state2 = (bool[,])state1.Clone();
                state2[1, 1] = true;
                canvas.gridState = state2;
                canvas.SaveStateForUndo();

                bool[,] state3 = (bool[,])state2.Clone();
                state3[2, 2] = true;
                canvas.gridState = state3;

                AssertGridStateEquals(state3, GetGridState(canvas));

                canvas.Undo();
                Assert.AreEqual(1, canvas.undoStack.Count);
                Assert.AreEqual(1, canvas.redoStack.Count);
                AssertGridStateEquals(state2, GetGridState(canvas));
                AssertGridStateEquals(state3, canvas.redoStack.Peek());

                canvas.Undo();
                Assert.AreEqual(0, canvas.undoStack.Count);
                Assert.AreEqual(2, canvas.redoStack.Count);
                AssertGridStateEquals(state1, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void Undo_DoesNothing_WhenUndoStackIsEmpty()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                canvas.Undo();

                Assert.AreEqual(0, canvas.undoStack.Count);
                Assert.AreEqual(0, canvas.redoStack.Count);

                bool[,] expected = new bool[10, 10];
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }
        [TestMethod]
        public void Redo_RestoresNextStateAndPushesCurrentStateToUndoStack_WhenRedoStackIsNotEmpty()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);

                bool[,] state1 = new bool[10, 10];
                canvas.gridState = state1;
                canvas.SaveStateForUndo();

                bool[,] state2 = (bool[,])state1.Clone();
                state2[1, 1] = true;
                canvas.gridState = state2;
                canvas.SaveStateForUndo();

                bool[,] state3 = (bool[,])state2.Clone();
                state3[2, 2] = true;
                canvas.gridState = state3;

                canvas.Undo();
                canvas.Undo();

                canvas.Redo();
                Assert.AreEqual(1, canvas.undoStack.Count);
                Assert.AreEqual(1, canvas.redoStack.Count);
                AssertGridStateEquals(state2, GetGridState(canvas));
                AssertGridStateEquals(state1, canvas.undoStack.Peek());

                canvas.Redo();
                Assert.AreEqual(2, canvas.undoStack.Count);
                Assert.AreEqual(0, canvas.redoStack.Count);
                AssertGridStateEquals(state3, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void Redo_DoesNothing_WhenRedoStackIsEmpty()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                canvas.SaveStateForUndo();
                canvas.Redo();

                Assert.AreEqual(1, canvas.undoStack.Count);
                Assert.AreEqual(0, canvas.redoStack.Count);
                AssertGridStateEquals(canvas.undoStack.Peek(), GetGridState(canvas));
            });
        }

        [TestMethod]
        public void FloodFill_FillsConnectedArea()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);

                for (int i = 3; i <= 6; i++)
                    for (int j = 3; j <= 6; j++)
                        canvas.gridState[i, j] = true;

                var expected = (bool[,])canvas.gridState.Clone();
                for (int i = 0; i < 10; i++)
                    for (int j = 0; j < 10; j++)
                        if (!(i >= 3 && i <= 6 && j >= 3 && j <= 6))
                            expected[i, j] = true;

                canvas.FloodFill(0, 0, false, true);
                AssertGridStateEquals(expected, GetGridState(canvas));

                canvas.SetGridSize(10, 10);
                for (int i = 0; i < 10; i++) { canvas.gridState[i, 0] = true; canvas.gridState[i, 9] = true; }
                for (int j = 0; j < 10; j++) { canvas.gridState[0, j] = true; canvas.gridState[9, j] = true; }

                var expected2 = (bool[,])canvas.gridState.Clone();
                for (int i = 1; i <= 8; i++)
                    for (int j = 1; j <= 8; j++)
                        expected2[i, j] = true;

                canvas.FloodFill(5, 5, false, true);
                AssertGridStateEquals(expected2, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void FillAreaAtCell_CallsFloodFillAndSavesState()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                canvas.gridState[1, 1] = true;
                var before = (bool[,])canvas.gridState.Clone();
                var undoBefore = canvas.undoStack.Count;

                canvas.FillAreaAtCell(0, 0);

                Assert.AreEqual(undoBefore + 1, canvas.undoStack.Count);
                AssertGridStateEquals(before, canvas.undoStack.Peek());

                var result = GetGridState(canvas);
                for (int i = 0; i < 10; i++)
                    for (int j = 0; j < 10; j++)
                        if (!(i == 1 && j == 1))
                            Assert.IsTrue(result[i, j]);
            });
        }

        [TestMethod]
        public void DrawLine_DrawsHorizontalLineCorrectly()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                var expected = new bool[10, 10];
                for (int x = 2; x <= 7; x++) expected[x, 3] = true;

                canvas.DrawLine(2, 3, 7, 3, true);
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void DrawLine_DrawsVerticalLineCorrectly()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                var expected = new bool[10, 10];
                for (int y = 1; y <= 6; y++) expected[5, y] = true;

                canvas.DrawLine(5, 1, 5, 6, true);
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void DrawRectangle_DrawsFilledRectangleCorrectly()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);
                var expected = new bool[10, 10];
                for (int i = 2; i <= 6; i++)
                    for (int j = 3; j <= 7; j++)
                        expected[i, j] = true;

                canvas.DrawRectangle(2, 3, 6, 7, true);
                AssertGridStateEquals(expected, GetGridState(canvas));

                canvas.ClearGrid();
                canvas.DrawRectangle(6, 7, 2, 3, true);
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void DrawEllipse_FullGrid5x5_CorrectCellsFilled()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(5, 5);

                var expected = new bool[5, 5]
                {
            { false, false,  true, false, false },
            { false,  true,  true,  true, false },
            {  true,  true,  true,  true,  true },
            { false,  true,  true,  true, false },
            { false, false,  true, false, false }
                };

                canvas.DrawEllipse(0, 0, 4, 4, true);
                AssertGridStateEquals(expected, canvas.gridState);
            });
        }

        [TestMethod]
        public void DrawEllipse_DrawsRectangleWhenZeroWidthOrHeight()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(10, 10);

                var expected = new bool[10, 10];
                for (int i = 2; i <= 6; i++) expected[i, 3] = true;

                canvas.DrawEllipse(2, 3, 6, 3, true);
                AssertGridStateEquals(expected, GetGridState(canvas));

                canvas.ClearGrid();
                var expected2 = new bool[10, 10];
                for (int j = 3; j <= 7; j++) expected2[2, j] = true;

                canvas.DrawEllipse(2, 3, 2, 7, true);
                AssertGridStateEquals(expected2, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void Rotate_RotatesGridStateBy90Degrees()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(3, 2);
                canvas.gridState = new bool[3, 2] {
                    { true, false },
                    { false, true },
                    { true, false }
                };

                var expected = new bool[2, 3] {
                    { true, false, true },
                    { false,  true, false }
                };

                canvas.Rotate(90);
                Assert.AreEqual(2, canvas.GridColumns);
                Assert.AreEqual(3, canvas.GridRows);
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void Rotate_RotatesGridStateBy180Degrees()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(3, 2);
                canvas.gridState = new bool[3, 2] {
                    { true, false },
                    { false, true },
                    { true, false }
                };

                var expected = new bool[3, 2] {
                    { false, true },
                    { true, false },
                    { false, true }
                };

                canvas.Rotate(180);
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void Rotate_RotatesGridStateBy270Degrees()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(3, 2);
                canvas.gridState = new bool[3, 2] {
                    { true, false },
                    { false, true },
                    { true, false }
                };

                var expected = new bool[2, 3] {
                    { false, true, false },
                    { true, false, true }
                };

                canvas.Rotate(270);
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void Rotate_HandlesAnglesOutside0_360Range()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(3, 2);
                var original = new bool[3, 2] {
                    { true, false },
                    { false, true },
                    { true, false }
                };

                canvas.gridState = original;
                var expected90 = new bool[2, 3] {
                    { true, false, true },
                    { false,  true, false }
                };

                canvas.Rotate(450);
                AssertGridStateEquals(expected90, GetGridState(canvas));

                canvas.SetGridSize(3, 2);
                canvas.gridState = original;
                var expected270 = new bool[2, 3] {
                    { false, true, false },
                    { true, false, true }
                };

                canvas.Rotate(-90);
                AssertGridStateEquals(expected270, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void MirrorHorizontal_MirrorsGridStateHorizontally()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(4, 2);
                canvas.gridState = new bool[4, 2] {
                    { true, false },
                    { false, true },
                    { true, true },
                    { false, false }
                };

                var expected = new bool[4, 2] {
                    { false, false },
                    { true, true },
                    { false, true },
                    { true, false }
                };

                canvas.MirrorHorizontal();
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void MirrorVertical_MirrorsGridStateVertically()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(2, 4);
                canvas.gridState = new bool[2, 4] {
                    { true, false, true, false },
                    { false, true, true, false }
                };

                var expected = new bool[2, 4] {
                    { false, true, false, true },
                    { false, true, true, false }
                };

                canvas.MirrorVertical();
                AssertGridStateEquals(expected, GetGridState(canvas));
            });
        }

        [TestMethod]
        public void UpdateSelection_CalculatesCorrectRect_FromTopLeftToBottomRight()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.UpdateSelection(new Point(2, 3), new Point(5, 7));

                var rect = canvas.selectionRect;
                Assert.IsTrue(rect.HasValue);
                Assert.AreEqual(2.0, rect.Value.X);
                Assert.AreEqual(3.0, rect.Value.Y);
                Assert.AreEqual(4.0, rect.Value.Width);
                Assert.AreEqual(5.0, rect.Value.Height);
            });
        }

        [TestMethod]
        public void UpdateSelection_CalculatesCorrectRect_FromBottomRightToTopLeft()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.UpdateSelection(new Point(5, 7), new Point(2, 3));

                var rect = canvas.selectionRect;
                Assert.IsTrue(rect.HasValue);
                Assert.AreEqual(2.0, rect.Value.X);
                Assert.AreEqual(3.0, rect.Value.Y);
                Assert.AreEqual(4.0, rect.Value.Width);
                Assert.AreEqual(5.0, rect.Value.Height);
            });
        }

        [TestMethod]
        public void SelectAll_SetsSelectionRectToEntireGrid()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SetGridSize(15, 20);
                canvas.SelectAll();

                var rect = canvas.selectionRect;
                Assert.IsTrue(rect.HasValue);
                Assert.AreEqual(0.0, rect.Value.X);
                Assert.AreEqual(0.0, rect.Value.Y);
                Assert.AreEqual(15.0, rect.Value.Width);
                Assert.AreEqual(20.0, rect.Value.Height);
            });
        }

        [TestMethod]
        public void Deselect_SetsSelectionRectToNull()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.SelectAll();
                canvas.Deselect();

                Assert.IsFalse(canvas.selectionRect.HasValue);
            });
        }

        [TestMethod]
        public void ZoomReset_SetsScaleToOne()
        {
            RunInSta(() =>
            {
                var canvas = new GridCanvas();
                canvas.ZoomIn();
                Assert.AreNotEqual(1.0, canvas.CurrentScale);

                canvas.ZoomReset();
                Assert.AreEqual(1.0, canvas.CurrentScale, 0.0001);
            });
        }

    }
}

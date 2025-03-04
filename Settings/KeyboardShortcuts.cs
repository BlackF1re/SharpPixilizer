using System.Windows.Input;

namespace SharpPixilizer
{
    public static class KeyboardShortcuts
    {
        public static readonly RoutedUICommand NewCommand = new RoutedUICommand(
            "Создать...", "NewCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.N, ModifierKeys.Control) });

        public static readonly RoutedUICommand OpenCommand = new RoutedUICommand(
            "Открыть...", "OpenCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control) });

        public static readonly RoutedUICommand UndoCommand = new RoutedUICommand(
            "Отменить", "UndoCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.Z, ModifierKeys.Control) });

        public static readonly RoutedUICommand RedoCommand = new RoutedUICommand(
            "Вернуть", "RedoCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.Y, ModifierKeys.Control) });

        public static readonly RoutedUICommand CutCommand = new RoutedUICommand(
            "Вырезать", "CutCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.X, ModifierKeys.Control) });

        public static readonly RoutedUICommand CopyCommand = new RoutedUICommand(
            "Копировать", "CopyCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.C, ModifierKeys.Control) });

        public static readonly RoutedUICommand SelectAllCommand = new RoutedUICommand(
            "Выделить всё", "SelectAllCommand", typeof(KeyboardShortcuts),
            new InputGestureCollection() { new KeyGesture(Key.A, ModifierKeys.Control) });
    }
}

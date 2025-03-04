using SharpPixilizer;
using System.Windows;

public partial class SettingsDialog : Window
{
    private SettingsManager settingsManager;

    public SettingsDialog()
    {
        InitializeComponent();
        // Путь к базе настроек (например, в той же папке, что и приложение)
        string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.db");
        settingsManager = new SettingsManager(dbPath);

        // Загрузим текущие настройки (пример)
        var someSetting = settingsManager.GetSetting("SomeSetting");
        SomeSettingTextBox.Text = someSetting ?? "Значение по умолчанию";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Сохраняем настройки
        settingsManager.SaveSetting("SomeSetting", SomeSettingTextBox.Text);
        MessageBox.Show("Настройки сохранены.");
        DialogResult = true;
    }
}

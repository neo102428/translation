using System.Windows;

namespace translation
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;

        // 我们通过构造函数把 SettingsService 传进来
        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            AppIdTextBox.Text = _settingsService.Settings.BaiduAppId;
            SecretKeyTextBox.Text = _settingsService.Settings.BaiduSecretKey;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.Settings.BaiduAppId = AppIdTextBox.Text;
            _settingsService.Settings.BaiduSecretKey = SecretKeyTextBox.Text;
            _settingsService.SaveSettings();

            MessageBox.Show("设置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
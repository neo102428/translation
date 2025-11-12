using System.Collections.Generic;
using System.Windows;

namespace translation
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;

        private readonly Dictionary<string, string> _languageOptions = new Dictionary<string, string>
        {
            { "自动检测", "auto" },
            { "中文", "zh" },
            { "英语", "en" },
            { "日语", "jp" },
            { "韩语", "kor" }
        };

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            InitializeComboBoxes();
            LoadSettings();
        }

        private void InitializeComboBoxes()
        {
            foreach (var lang in _languageOptions)
            {
                SourceLanguageComboBox.Items.Add(lang.Key);
                if (lang.Value != "auto")
                {
                    TargetLanguageComboBox.Items.Add(lang.Key);
                }
            }
        }

        private void LoadSettings()
        {
            AppIdTextBox.Text = _settingsService.Settings.BaiduAppId;
            SecretKeyTextBox.Text = _settingsService.Settings.BaiduSecretKey;

            switch (_settingsService.Settings.Trigger)
            {
                case TriggerMode.MiddleMouse: MiddleMouseRadio.IsChecked = true; break;
                case TriggerMode.RightMouse: RightMouseRadio.IsChecked = true; break;
                case TriggerMode.AltAndLeftMouse: AltLeftMouseRadio.IsChecked = true; break;
            }

            string sourceLangKey = FindKeyByValue(_languageOptions, _settingsService.Settings.SourceLanguage);
            string targetLangKey = FindKeyByValue(_languageOptions, _settingsService.Settings.TargetLanguage);

            SourceLanguageComboBox.SelectedItem = sourceLangKey ?? "自动检测";
            TargetLanguageComboBox.SelectedItem = targetLangKey ?? "中文";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.Settings.BaiduAppId = AppIdTextBox.Text;
            _settingsService.Settings.BaiduSecretKey = SecretKeyTextBox.Text;

            if (MiddleMouseRadio.IsChecked == true) _settingsService.Settings.Trigger = TriggerMode.MiddleMouse;
            else if (RightMouseRadio.IsChecked == true) _settingsService.Settings.Trigger = TriggerMode.RightMouse;
            else if (AltLeftMouseRadio.IsChecked == true) _settingsService.Settings.Trigger = TriggerMode.AltAndLeftMouse;

            if (SourceLanguageComboBox.SelectedItem != null)
            {
                _settingsService.Settings.SourceLanguage = _languageOptions[SourceLanguageComboBox.SelectedItem.ToString()];
            }
            if (TargetLanguageComboBox.SelectedItem != null)
            {
                _settingsService.Settings.TargetLanguage = _languageOptions[TargetLanguageComboBox.SelectedItem.ToString()];
            }

            _settingsService.SaveSettings();

            MessageBox.Show("设置已保存！\n\n注意：新的触发方式将在您重启程序后生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private string FindKeyByValue(Dictionary<string, string> dict, string value)
        {
            foreach (var pair in dict)
            {
                if (pair.Value == value)
                {
                    return pair.Key;
                }
            }
            return null;
        }
    }
}
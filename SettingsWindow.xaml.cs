using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace translation
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        public ResultWindow ResultWindow { get; set; }

        private readonly Dictionary<string, string> _languageOptions = new Dictionary<string, string>
        {
            { "自动检测", "auto" },
            { "中文", "zh" },
            { "英语", "en" },
            { "日语", "jp" },
            { "韩语", "kor" },
            { "法语", "fra" },
            { "西班牙语", "spa" },
            { "德语", "de" },
            { "俄语", "ru" }
        };

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            InitializeComboBoxes();
            LoadSettings();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
            // 加载翻译引擎选择
            switch (_settingsService.Settings.Engine)
            {
                case TranslationEngine.Baidu:
                    BaiduRadio.IsChecked = true;
                    break;
                case TranslationEngine.Tencent:
                    TencentRadio.IsChecked = true;
                    break;
                case TranslationEngine.Google:
                    GoogleRadio.IsChecked = true;
                    break;
            }

            // 加载 API 配置
            BaiduAppIdTextBox.Text = _settingsService.Settings.BaiduAppId;
            BaiduSecretKeyTextBox.Text = _settingsService.Settings.BaiduSecretKey;
            TencentSecretIdTextBox.Text = _settingsService.Settings.TencentSecretId;
            TencentSecretKeyTextBox.Text = _settingsService.Settings.TencentSecretKey;
            GoogleApiKeyTextBox.Text = _settingsService.Settings.GoogleApiKey;

            // 更新配置面板可见性
            UpdateConfigPanelVisibility();

            // 加载触发方式
            switch (_settingsService.Settings.Trigger)
            {
                case TriggerMode.MiddleMouse: MiddleMouseRadio.IsChecked = true; break;
                case TriggerMode.RightMouse: RightMouseRadio.IsChecked = true; break;
                case TriggerMode.AltAndLeftMouse: AltLeftMouseRadio.IsChecked = true; break;
            }

            // 加载语言设置
            string sourceLangKey = FindKeyByValue(_languageOptions, _settingsService.Settings.SourceLanguage);
            string targetLangKey = FindKeyByValue(_languageOptions, _settingsService.Settings.TargetLanguage);

            SourceLanguageComboBox.SelectedItem = sourceLangKey ?? "自动检测";
            TargetLanguageComboBox.SelectedItem = targetLangKey ?? "中文";

            // 加载主题模式
            if (_settingsService.Settings.Theme == ThemeMode.Light)
            {
                LightThemeRadio.IsChecked = true;
            }
            else
            {
                DarkThemeRadio.IsChecked = true;
            }
        }

        private void EngineRadio_Checked(object sender, RoutedEventArgs e)
        {
            UpdateConfigPanelVisibility();
        }

        private void UpdateConfigPanelVisibility()
        {
            if (BaiduConfigPanel == null) return;

            // 根据选择的引擎显示对应的配置面板
            BaiduConfigPanel.Visibility = BaiduRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            TencentConfigPanel.Visibility = TencentRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            GoogleConfigPanel.Visibility = GoogleRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch
            {
                // 忽略错误
            }
            e.Handled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存翻译引擎选择
            if (BaiduRadio.IsChecked == true)
                _settingsService.Settings.Engine = TranslationEngine.Baidu;
            else if (TencentRadio.IsChecked == true)
                _settingsService.Settings.Engine = TranslationEngine.Tencent;
            else if (GoogleRadio.IsChecked == true)
                _settingsService.Settings.Engine = TranslationEngine.Google;

            // 保存 API 配置
            _settingsService.Settings.BaiduAppId = BaiduAppIdTextBox.Text;
            _settingsService.Settings.BaiduSecretKey = BaiduSecretKeyTextBox.Text;
            _settingsService.Settings.TencentSecretId = TencentSecretIdTextBox.Text;
            _settingsService.Settings.TencentSecretKey = TencentSecretKeyTextBox.Text;
            _settingsService.Settings.GoogleApiKey = GoogleApiKeyTextBox.Text;

            // 保存触发方式
            if (MiddleMouseRadio.IsChecked == true) 
                _settingsService.Settings.Trigger = TriggerMode.MiddleMouse;
            else if (RightMouseRadio.IsChecked == true) 
                _settingsService.Settings.Trigger = TriggerMode.RightMouse;
            else if (AltLeftMouseRadio.IsChecked == true) 
                _settingsService.Settings.Trigger = TriggerMode.AltAndLeftMouse;

            // 保存语言设置
            if (SourceLanguageComboBox.SelectedItem != null)
            {
                _settingsService.Settings.SourceLanguage = _languageOptions[SourceLanguageComboBox.SelectedItem.ToString()];
            }
            if (TargetLanguageComboBox.SelectedItem != null)
            {
                _settingsService.Settings.TargetLanguage = _languageOptions[TargetLanguageComboBox.SelectedItem.ToString()];
            }

            // 保存主题模式
            ThemeMode oldTheme = _settingsService.Settings.Theme;
            if (LightThemeRadio.IsChecked == true)
            {
                _settingsService.Settings.Theme = ThemeMode.Light;
            }
            else
            {
                _settingsService.Settings.Theme = ThemeMode.Dark;
            }

            _settingsService.SaveSettings();

            // 如果主题改变，立即应用到结果窗口
            if (ResultWindow != null && oldTheme != _settingsService.Settings.Theme)
            {
                ResultWindow.ApplyTheme(_settingsService.Settings.Theme);
            }

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
using System.IO;
using Newtonsoft.Json;

public enum TriggerMode
{
    MiddleMouse,
    RightMouse,
    AltAndLeftMouse
}

public enum ThemeMode
{
    Light,
    Dark
}

public enum TranslationEngine
{
    Baidu,      // 百度翻译
    Tencent,    // 腾讯翻译
    Google      // 谷歌翻译
}

public class AppSettings
{
    // 翻译引擎选择
    public TranslationEngine Engine { get; set; } = TranslationEngine.Baidu;
    
    // 百度翻译配置
    public string BaiduAppId { get; set; } = string.Empty;
    public string BaiduSecretKey { get; set; } = string.Empty;
    
    // 腾讯翻译配置
    public string TencentSecretId { get; set; } = string.Empty;
    public string TencentSecretKey { get; set; } = string.Empty;
    
    // 谷歌翻译配置
    public string GoogleApiKey { get; set; } = string.Empty;
    
    // 触发方式
    public TriggerMode Trigger { get; set; } = TriggerMode.MiddleMouse;

    // 语言设置
    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "zh";
    
    // 主题模式
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
}

public class SettingsService
{
    private readonly string _settingsFilePath;
    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "TranslationTool");
        Directory.CreateDirectory(appFolderPath);
        _settingsFilePath = Path.Combine(appFolderPath, "settings.json");

        Settings = LoadSettings();
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { /* 忽略保存错误 */ }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }
            string json = File.ReadAllText(_settingsFilePath);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
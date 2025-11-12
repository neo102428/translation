using System.IO;
using Newtonsoft.Json;

public enum TriggerMode
{
    MiddleMouse,
    RightMouse,
    AltAndLeftMouse
}

public class AppSettings
{
    public string BaiduAppId { get; set; } = string.Empty;
    public string BaiduSecretKey { get; set; } = string.Empty;
    public TriggerMode Trigger { get; set; } = TriggerMode.MiddleMouse;

    // --- 核心新增：保存用户选择的语言 ---
    // 百度API代码：auto, zh, en, jp, kor
    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "zh";
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
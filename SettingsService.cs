using System.IO;
using Newtonsoft.Json;

public class AppSettings
{
    public string BaiduAppId { get; set; } = string.Empty;
    public string BaiduSecretKey { get; set; } = string.Empty;
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
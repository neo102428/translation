using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class HistoryService
{
    private readonly string _historyFilePath;
    private List<TranslationRecord> _historyCache;

    public HistoryService()
    {
        // 将历史记录文件保存在用户个人的、隐藏的 AppData 文件夹中，这是最标准的做法
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "TranslationTool"); // 为我们的应用创建一个专属文件夹
        Directory.CreateDirectory(appFolderPath); // 确保这个文件夹存在
        _historyFilePath = Path.Combine(appFolderPath, "history.json");

        _historyCache = LoadHistory();
    }

    public List<TranslationRecord> GetHistory()
    {
        return _historyCache;
    }

    public void AddRecord(string original, string translated)
    {
        var record = new TranslationRecord
        {
            OriginalText = original,
            TranslatedText = translated,
            Timestamp = DateTime.Now
        };

        // 在列表的开头添加新记录，这样最新的总在最上面
        _historyCache.Insert(0, record);

        // 将更新后的整个列表保存回文件
        SaveHistory();
    }

    private List<TranslationRecord> LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                return new List<TranslationRecord>();
            }

            string json = File.ReadAllText(_historyFilePath);
            // 如果文件是空的或损坏的，返回一个空列表
            return JsonConvert.DeserializeObject<List<TranslationRecord>>(json) ?? new List<TranslationRecord>();
        }
        catch (Exception)
        {
            // 如果读取或解析失败，返回一个空列表以防程序崩溃
            return new List<TranslationRecord>();
        }
    }

    private void SaveHistory()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_historyCache, Formatting.Indented);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception)
        {
            // 如果保存失败，我们暂时忽略错误，避免打扰用户
        }
    }
}
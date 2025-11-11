using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class TranslationService
{
    private readonly SettingsService _settingsService;
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto","zh","en","yue","wyw","jp","kor","fra","spa","th","ara","ru","pt","de","it",
        "el","nl","pl","bul","est","dan","fin","cs","rom","slo","swe","hu","cht","vie"
    };

    private static readonly Dictionary<string, string> LanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh-cn", "zh" },
        { "zh_cn", "zh" },
        { "cn", "zh" },
        { "zh-hans", "zh" },
        { "zh-tw", "cht" },
        { "zh_tw", "cht" },
        { "tw", "cht" },
        { "zh-hant", "cht" },
        { "ja", "jp" },
        { "ko", "kor" }
    };

    public TranslationService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private string NormalizeLanguageCode(string code, string defaultCode)
    {
        if (string.IsNullOrWhiteSpace(code)) return defaultCode;
        string lowerCode = code.Trim().ToLowerInvariant();
        if (LanguageMap.TryGetValue(lowerCode, out var mappedCode))
        {
            return mappedCode;
        }
        if (SupportedLanguages.Contains(lowerCode))
        {
            return lowerCode;
        }
        return defaultCode;
    }

    public async Task<string> TranslateAsync(string query, string from, string to)
    {
        string appId = _settingsService.Settings.BaiduAppId;
        string secretKey = _settingsService.Settings.BaiduSecretKey;

        if (string.IsNullOrWhiteSpace(query)) return "要翻译的文本为空";
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secretKey))
        {
            return "错误：API 密钥未配置。\n请右键点击托盘图标，进入“设置”页面配置您的百度翻译密钥。";
        }

        string fromCode = NormalizeLanguageCode(from, "auto");
        string toCode = NormalizeLanguageCode(to, "zh");

        if (toCode == "auto")
        {
            return "错误：目标翻译语言不能设置为“自动检测”。";
        }

        // --- 核心修正：清理查询文本中的换行符 ---
        // 将所有换行符 (\n) 和回车符 (\r) 都替换成一个空格
        string cleanedQuery = query.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        string salt = new Random().Next(100000, 999999).ToString();
        // 使用清理后的文本进行签名
        string sign = GetMd5Hash(appId + cleanedQuery + salt + secretKey);

        var content = new FormUrlEncodedContent(new[]
        {
            // 提交给 API 的也是清理后的文本
            new KeyValuePair<string,string>("q", cleanedQuery),
            new KeyValuePair<string,string>("from", fromCode),
            new KeyValuePair<string,string>("to", toCode),
            new KeyValuePair<string,string>("appid", appId),
            new KeyValuePair<string,string>("salt", salt),
            new KeyValuePair<string,string>("sign", sign)
        });

        try
        {
            var response = await _httpClient.PostAsync("https://api.fanyi.baidu.com/api/trans/vip/translate", content);
            var jsonResult = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(jsonResult);
            if (jsonResponse["error_code"] != null)
            {
                string code = jsonResponse["error_code"]?.ToString();
                string msg = jsonResponse["error_msg"]?.ToString();

                if (code == "58000")
                {
                    return $"百度翻译错误 (代码: 58000)：客户端 IP 非法。\n\n请确保您已在百度翻译后台关闭“IP 地址校验”功能。";
                }

                return $"百度翻译错误 (代码: {code})：{msg}";
            }

            return jsonResponse["trans_result"]?[0]?["dst"]?.ToString() ?? "解析翻译结果失败";
        }
        catch (HttpRequestException hx) { return "网络请求失败: " + hx.Message; }
        catch (Exception ex) { return "翻译接口调用失败: " + ex.Message; }
    }

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        var sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
        return sb.ToString();
    }
}
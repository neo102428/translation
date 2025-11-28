using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TranslationService
{
    private readonly SettingsService _settingsService;
    private readonly CacheService _cacheService;
    private readonly LoggerService _loggerService;
    private static readonly HttpClient _httpClient = new HttpClient 
    { 
        Timeout = TimeSpan.FromSeconds(15),
        MaxResponseContentBufferSize = 1024 * 1024 // 1MB
    };

    public TranslationService(SettingsService settingsService, CacheService cacheService = null, LoggerService loggerService = null)
    {
        _settingsService = settingsService;
        _cacheService = cacheService ?? new CacheService();
        _loggerService = loggerService ?? new LoggerService();
    }

    public async Task<string> TranslateAsync(string query, string from, string to)
    {
        if (string.IsNullOrWhiteSpace(query)) return "要翻译的文本为空";

        // 清理查询文本中的换行符
        string cleanedQuery = query.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // 检查缓存
        string cacheKey = $"{_settingsService.Settings.Engine}|{cleanedQuery}|{from}|{to}";
        if (_cacheService.TryGetCached(cacheKey, out string cachedResult))
        {
            _loggerService.Log($"翻译缓存命中: {cleanedQuery.Substring(0, Math.Min(20, cleanedQuery.Length))}...");
            return cachedResult;
        }

        // 根据选择的引擎进行翻译
        string result = _settingsService.Settings.Engine switch
        {
            TranslationEngine.Baidu => await TranslateBaiduAsync(cleanedQuery, from, to),
            TranslationEngine.Tencent => await TranslateTencentAsync(cleanedQuery, from, to),
            TranslationEngine.Google => await TranslateGoogleAsync(cleanedQuery, from, to),
            _ => "错误：未知的翻译引擎"
        };

        // 只缓存成功的结果
        if (!result.StartsWith("错误") && !result.Contains("失败") && !result.Contains("网络"))
        {
            _cacheService.AddToCache(cacheKey, result);
        }

        return result;
    }

    #region 百度翻译

    private static readonly HashSet<string> BaiduSupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto","zh","en","yue","wyw","jp","kor","fra","spa","th","ara","ru","pt","de","it",
        "el","nl","pl","bul","est","dan","fin","cs","rom","slo","swe","hu","cht","vie"
    };

    private static readonly Dictionary<string, string> BaiduLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh-cn", "zh" }, { "zh_cn", "zh" }, { "cn", "zh" }, { "zh-hans", "zh" },
        { "zh-tw", "cht" }, { "zh_tw", "cht" }, { "tw", "cht" }, { "zh-hant", "cht" },
        { "ja", "jp" }, { "ko", "kor" }
    };

    private async Task<string> TranslateBaiduAsync(string query, string from, string to)
    {
        string appId = _settingsService.Settings.BaiduAppId;
        string secretKey = _settingsService.Settings.BaiduSecretKey;

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secretKey))
        {
            return "错误：百度翻译 API 密钥未配置。\n请在设置中配置您的百度翻译密钥。";
        }

        string fromCode = NormalizeBaiduLanguage(from, "auto");
        string toCode = NormalizeBaiduLanguage(to, "zh");

        if (toCode == "auto")
        {
            return "错误：目标语言不能设置为\"自动检测\"。";
        }

        return await TranslateBaiduWithRetryAsync(query, fromCode, toCode, appId, secretKey);
    }

    private string NormalizeBaiduLanguage(string code, string defaultCode)
    {
        if (string.IsNullOrWhiteSpace(code)) return defaultCode;
        string lowerCode = code.Trim().ToLowerInvariant();
        if (BaiduLanguageMap.TryGetValue(lowerCode, out var mappedCode))
        {
            return mappedCode;
        }
        if (BaiduSupportedLanguages.Contains(lowerCode))
        {
            return lowerCode;
        }
        return defaultCode;
    }

    private async Task<string> TranslateBaiduWithRetryAsync(string query, string from, string to, string appId, string secretKey)
    {
        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                string salt = new Random().Next(100000, 999999).ToString();
                string sign = GetMd5Hash(appId + query + salt + secretKey);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("q", query),
                    new KeyValuePair<string,string>("from", from),
                    new KeyValuePair<string,string>("to", to),
                    new KeyValuePair<string,string>("appid", appId),
                    new KeyValuePair<string,string>("salt", salt),
                    new KeyValuePair<string,string>("sign", sign)
                });

                var response = await _httpClient.PostAsync("https://api.fanyi.baidu.com/api/trans/vip/translate", content);
                var jsonResult = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(jsonResult);

                if (jsonResponse["error_code"] != null)
                {
                    string code = jsonResponse["error_code"]?.ToString();
                    string msg = jsonResponse["error_msg"]?.ToString();
                    _loggerService.Log($"百度翻译错误: {code} - {msg}", LogLevel.Warning);
                    return $"百度翻译错误 ({code}): {msg}";
                }

                string translatedText = jsonResponse["trans_result"]?[0]?["dst"]?.ToString();
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    return translatedText;
                }
                return "解析翻译结果失败";
            }
            catch (HttpRequestException ex)
            {
                _loggerService.LogError($"百度翻译请求失败 (尝试 {attempt + 1}/{maxRetries})", ex);
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(1000 * (attempt + 1));
                    continue;
                }
                return "网络请求失败，请检查网络连接";
            }
            catch (Exception ex)
            {
                _loggerService.LogError("百度翻译异常", ex);
                return "百度翻译失败: " + ex.Message;
            }
        }
        return "百度翻译服务暂时不可用";
    }

    #endregion

    #region 腾讯翻译

    private static readonly Dictionary<string, string> TencentLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh", "zh" }, { "zh-cn", "zh" }, { "zh_cn", "zh" }, { "cn", "zh" },
        { "zh-tw", "zh-TW" }, { "zh_tw", "zh-TW" }, { "tw", "zh-TW" },
        { "en", "en" }, { "ja", "ja" }, { "jp", "ja" },
        { "ko", "ko" }, { "kor", "ko" },
        { "fr", "fr" }, { "fra", "fr" },
        { "es", "es" }, { "spa", "es" },
        { "it", "it" }, { "de", "de" },
        { "tr", "tr" }, { "ru", "ru" },
        { "pt", "pt" }, { "vi", "vi" }, { "vie", "vi" },
        { "id", "id" }, { "th", "th" },
        { "ms", "ms" }, { "ar", "ar" }, { "ara", "ar" },
        { "hi", "hi" }
    };

    private async Task<string> TranslateTencentAsync(string query, string from, string to)
    {
        string secretId = _settingsService.Settings.TencentSecretId;
        string secretKey = _settingsService.Settings.TencentSecretKey;

        if (string.IsNullOrWhiteSpace(secretId) || string.IsNullOrWhiteSpace(secretKey))
        {
            return "错误：腾讯翻译 API 密钥未配置。\n请在设置中配置您的腾讯云密钥。";
        }

        string fromCode = NormalizeTencentLanguage(from, "auto");
        string toCode = NormalizeTencentLanguage(to, "zh");

        if (toCode == "auto")
        {
            return "错误：目标语言不能设置为\"自动检测\"。";
        }

        return await TranslateTencentWithRetryAsync(query, fromCode, toCode, secretId, secretKey);
    }

    private string NormalizeTencentLanguage(string code, string defaultCode)
    {
        if (string.IsNullOrWhiteSpace(code)) return defaultCode;
        if (code.Equals("auto", StringComparison.OrdinalIgnoreCase)) return "auto";
        
        string lowerCode = code.Trim().ToLowerInvariant();
        if (TencentLanguageMap.TryGetValue(lowerCode, out var mappedCode))
        {
            return mappedCode;
        }
        return defaultCode;
    }

    private async Task<string> TranslateTencentWithRetryAsync(string query, string from, string to, string secretId, string secretKey)
    {
        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                string service = "tmt";
                string host = "tmt.tencentcloudapi.com";
                string endpoint = "https://" + host;
                string region = "ap-beijing";
                string action = "TextTranslate";
                string version = "2018-03-21";
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp)).ToString("yyyy-MM-dd");

                // 构建请求参数
                var parameters = new Dictionary<string, object>
                {
                    { "SourceText", query },
                    { "Source", from },
                    { "Target", to },
                    { "ProjectId", 0 }
                };

                string payload = JsonConvert.SerializeObject(parameters);

                // 构建规范请求
                string canonicalRequest = $"POST\n/\n\ncontent-type:application/json\nhost:{host}\n\ncontent-type;host\n{GetSha256Hash(payload)}";
                string credentialScope = $"{date}/{service}/tc3_request";
                string hashedCanonicalRequest = GetSha256Hash(canonicalRequest);
                string stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{hashedCanonicalRequest}";

                // 计算签名
                byte[] secretDate = HmacSha256(Encoding.UTF8.GetBytes("TC3" + secretKey), date);
                byte[] secretService = HmacSha256(secretDate, service);
                byte[] secretSigning = HmacSha256(secretService, "tc3_request");
                string signature = BitConverter.ToString(HmacSha256(secretSigning, stringToSign)).Replace("-", "").ToLower();

                // 构建授权头
                string authorization = $"TC3-HMAC-SHA256 Credential={secretId}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";

                // 发送请求
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", authorization);
                request.Headers.Add("Host", host);
                request.Headers.Add("X-TC-Action", action);
                request.Headers.Add("X-TC-Timestamp", timestamp);
                request.Headers.Add("X-TC-Version", version);
                request.Headers.Add("X-TC-Region", region);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var jsonResult = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(jsonResult);

                if (jsonResponse["Response"]?["Error"] != null)
                {
                    string code = jsonResponse["Response"]?["Error"]?["Code"]?.ToString();
                    string msg = jsonResponse["Response"]?["Error"]?["Message"]?.ToString();
                    _loggerService.Log($"腾讯翻译错误: {code} - {msg}", LogLevel.Warning);
                    return $"腾讯翻译错误 ({code}): {msg}";
                }

                string translatedText = jsonResponse["Response"]?["TargetText"]?.ToString();
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    return translatedText;
                }
                return "解析翻译结果失败";
            }
            catch (HttpRequestException ex)
            {
                _loggerService.LogError($"腾讯翻译请求失败 (尝试 {attempt + 1}/{maxRetries})", ex);
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(1000 * (attempt + 1));
                    continue;
                }
                return "网络请求失败，请检查网络连接";
            }
            catch (Exception ex)
            {
                _loggerService.LogError("腾讯翻译异常", ex);
                return "腾讯翻译失败: " + ex.Message;
            }
        }
        return "腾讯翻译服务暂时不可用";
    }

    #endregion

    #region 谷歌翻译

    private static readonly Dictionary<string, string> GoogleLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh", "zh-CN" }, { "zh-cn", "zh-CN" }, { "zh_cn", "zh-CN" }, { "cn", "zh-CN" },
        { "zh-tw", "zh-TW" }, { "zh_tw", "zh-TW" }, { "tw", "zh-TW" },
        { "en", "en" }, { "ja", "ja" }, { "jp", "ja" },
        { "ko", "ko" }, { "kor", "ko" },
        { "fr", "fr" }, { "fra", "fr" },
        { "es", "es" }, { "spa", "es" },
        { "it", "it" }, { "de", "de" },
        { "ru", "ru" }, { "pt", "pt" },
        { "vi", "vi" }, { "vie", "vi" },
        { "th", "th" }, { "ar", "ar" }, { "ara", "ar" },
        { "hi", "hi" }, { "tr", "tr" },
        { "nl", "nl" }, { "pl", "pl" },
        { "id", "id" }, { "ms", "ms" }
    };

    private async Task<string> TranslateGoogleAsync(string query, string from, string to)
    {
        string apiKey = _settingsService.Settings.GoogleApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "错误：谷歌翻译 API 密钥未配置。\n请在设置中配置您的 Google Cloud API 密钥。";
        }

        string fromCode = NormalizeGoogleLanguage(from, "auto");
        string toCode = NormalizeGoogleLanguage(to, "zh-CN");

        if (toCode == "auto")
        {
            return "错误：目标语言不能设置为\"自动检测\"。";
        }

        return await TranslateGoogleWithRetryAsync(query, fromCode, toCode, apiKey);
    }

    private string NormalizeGoogleLanguage(string code, string defaultCode)
    {
        if (string.IsNullOrWhiteSpace(code)) return defaultCode;
        if (code.Equals("auto", StringComparison.OrdinalIgnoreCase)) return "auto";
        
        string lowerCode = code.Trim().ToLowerInvariant();
        if (GoogleLanguageMap.TryGetValue(lowerCode, out var mappedCode))
        {
            return mappedCode;
        }
        return defaultCode;
    }

    private async Task<string> TranslateGoogleWithRetryAsync(string query, string from, string to, string apiKey)
    {
        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                string url = $"https://translation.googleapis.com/language/translate/v2?key={apiKey}";
                
                var requestBody = new
                {
                    q = query,
                    source = from == "auto" ? null : from,
                    target = to,
                    format = "text"
                };

                string jsonPayload = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var jsonResult = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(jsonResult);

                if (jsonResponse["error"] != null)
                {
                    string code = jsonResponse["error"]?["code"]?.ToString();
                    string msg = jsonResponse["error"]?["message"]?.ToString();
                    _loggerService.Log($"谷歌翻译错误: {code} - {msg}", LogLevel.Warning);
                    return $"谷歌翻译错误 ({code}): {msg}";
                }

                string translatedText = jsonResponse["data"]?["translations"]?[0]?["translatedText"]?.ToString();
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    // 解码 HTML 实体
                    translatedText = WebUtility.HtmlDecode(translatedText);
                    return translatedText;
                }
                return "解析翻译结果失败";
            }
            catch (HttpRequestException ex)
            {
                _loggerService.LogError($"谷歌翻译请求失败 (尝试 {attempt + 1}/{maxRetries})", ex);
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(1000 * (attempt + 1));
                    continue;
                }
                return "网络请求失败，请检查网络连接";
            }
            catch (Exception ex)
            {
                _loggerService.LogError("谷歌翻译异常", ex);
                return "谷歌翻译失败: " + ex.Message;
            }
        }
        return "谷歌翻译服务暂时不可用";
    }

    #endregion

    #region 工具方法

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        var sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++) 
            sb.Append(hashBytes[i].ToString("x2"));
        return sb.ToString();
    }

    private static string GetSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static byte[] HmacSha256(byte[] key, string message)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    #endregion
}

using System;
using System.IO;

public class LoggerService
{
    private readonly string _logFilePath;
    private static readonly object _lockObject = new object();

    public LoggerService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "TranslationTool");
        Directory.CreateDirectory(appFolderPath);
        _logFilePath = Path.Combine(appFolderPath, "app.log");
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        try
        {
            lock (_lockObject)
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                
                // 日志文件超过 5MB 时清理
                FileInfo fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Exists && fileInfo.Length > 5 * 1024 * 1024)
                {
                    RotateLog();
                }
            }
        }
        catch
        {
            // 忽略日志记录失败
        }
    }

    public void LogError(string message, Exception ex)
    {
        Log($"{message}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}", LogLevel.Error);
    }

    private void RotateLog()
    {
        try
        {
            string backupPath = _logFilePath + ".old";
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            File.Move(_logFilePath, backupPath);
        }
        catch
        {
            // 如果轮转失败，直接删除
            try { File.Delete(_logFilePath); } catch { }
        }
    }
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace translation
{
    public partial class MainWindow : Window
    {
        // 导入 Windows API 用于获取显示器 DPI
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;

        private SelectionWindow _selectionWindow;
        private OcrService _ocrService;
        private TranslationService _translationService;
        private ResultWindow _resultWindow;
        private HistoryService _historyService;
        private SettingsService _settingsService;
        private LoggerService _loggerService;
        private CacheService _cacheService;

        private bool isSelectionActive = false;
        private Point selectionStartPoint;  // 屏幕坐标（像素）
        private double currentDpiScaleX = 1.0;
        private double currentDpiScaleY = 1.0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void InitializeServices(
            SettingsService settingsService,
            SelectionWindow selectionWindow,
            OcrService ocrService,
            ResultWindow resultWindow,
            HistoryService historyService,
            LoggerService loggerService,
            CacheService cacheService)
        {
            _settingsService = settingsService;
            _selectionWindow = selectionWindow;
            _ocrService = ocrService;
            _resultWindow = resultWindow;
            _historyService = historyService;
            _loggerService = loggerService;
            _cacheService = cacheService;
            _translationService = new TranslationService(settingsService, cacheService, loggerService);
            
            // 应用主题到结果窗口
            _resultWindow.ApplyTheme(_settingsService.Settings.Theme);
            
            this.Closing += MainWindow_Closing;
        }

        public void SubscribeToMouseHook(GlobalMouseHook mouseHook)
        {
            if (mouseHook != null)
            {
                mouseHook.ButtonDown += OnButtonDown;
                mouseHook.ButtonUp += OnButtonUp;
                mouseHook.MouseMove += OnMouseMove;
            }
        }

        /// <summary>
        /// 根据屏幕坐标点获取该位置的 DPI 缩放比例
        /// </summary>
        private void UpdateDpiScaleForPoint(Point screenPoint)
        {
            try
            {
                POINT pt = new POINT((int)screenPoint.X, (int)screenPoint.Y);
                IntPtr monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                
                if (monitor != IntPtr.Zero)
                {
                    uint dpiX, dpiY;
                    if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY) == 0)
                    {
                        // 标准 DPI 是 96
                        currentDpiScaleX = dpiX / 96.0;
                        currentDpiScaleY = dpiY / 96.0;
                        _loggerService.Log($"检测到显示器 DPI: {dpiX}x{dpiY}, 缩放比例: {currentDpiScaleX:F2}x{currentDpiScaleY:F2}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerService.Log($"获取 DPI 失败: {ex.Message}", LogLevel.Warning);
            }
            
            // 如果 API 调用失败，使用默认值
            currentDpiScaleX = 1.0;
            currentDpiScaleY = 1.0;
        }

        private void OnButtonDown(Point point, GlobalMouseHook.MouseButton button, GlobalMouseHook.ModifierKeys modifiers)
        {
            if (isSelectionActive) return;
            var triggerMode = _settingsService.Settings.Trigger;
            bool isTriggered = false;
            switch (triggerMode)
            {
                case TriggerMode.MiddleMouse:
                    isTriggered = (button == GlobalMouseHook.MouseButton.Middle && modifiers == GlobalMouseHook.ModifierKeys.None);
                    break;
                case TriggerMode.RightMouse:
                    isTriggered = (button == GlobalMouseHook.MouseButton.Right && modifiers == GlobalMouseHook.ModifierKeys.None);
                    break;
                case TriggerMode.AltAndLeftMouse:
                    isTriggered = (button == GlobalMouseHook.MouseButton.Left && (modifiers & GlobalMouseHook.ModifierKeys.Alt) != 0);
                    break;
            }
            if (isTriggered)
            {
                _resultWindow.Hide();
                isSelectionActive = true;
                selectionStartPoint = point;  // 保存屏幕坐标
                
                // 根据鼠标位置更新 DPI 缩放比例
                UpdateDpiScaleForPoint(point);
                
                _selectionWindow.Width = 0;
                _selectionWindow.Height = 0;
                _selectionWindow.Show();
                
                // 选择窗口使用 WPF 坐标
                _selectionWindow.Left = point.X / currentDpiScaleX;
                _selectionWindow.Top = point.Y / currentDpiScaleY;
                
                _loggerService.Log($"开始选择 - 屏幕坐标: ({point.X:F0}, {point.Y:F0}), DPI缩放: {currentDpiScaleX:F2}x{currentDpiScaleY:F2}");
            }
        }

        private void OnButtonUp(Point point, GlobalMouseHook.MouseButton button, GlobalMouseHook.ModifierKeys modifiers)
        {
            if (isSelectionActive)
            {
                isSelectionActive = false;
                
                // 计算屏幕坐标（像素）
                double screenLeft = Math.Min(point.X, selectionStartPoint.X);
                double screenTop = Math.Min(point.Y, selectionStartPoint.Y);
                double screenWidth = Math.Abs(point.X - selectionStartPoint.X);
                double screenHeight = Math.Abs(point.Y - selectionStartPoint.Y);
                
                Rect screenRect = new Rect(screenLeft, screenTop, screenWidth, screenHeight);
                
                // 计算 WPF 坐标（用于定位结果窗口）
                double wpfLeft = screenLeft / currentDpiScaleX;
                double wpfTop = screenTop / currentDpiScaleY;
                double wpfWidth = screenWidth / currentDpiScaleX;
                double wpfHeight = screenHeight / currentDpiScaleY;
                
                Rect wpfRect = new Rect(wpfLeft, wpfTop, wpfWidth, wpfHeight);
                
                _selectionWindow.Hide();
                
                if (screenRect.Width < 10 || screenRect.Height < 10) 
                {
                    _loggerService.Log("选区太小，已取消");
                    return;
                }
                
                _loggerService.Log($"选择完成 - 屏幕坐标: ({screenRect.X:F0}, {screenRect.Y:F0}, {screenRect.Width:F0}x{screenRect.Height:F0})");
                _loggerService.Log($"WPF坐标: ({wpfRect.X:F0}, {wpfRect.Y:F0}, {wpfRect.Width:F0}x{wpfRect.Height:F0})");
                
                // 传递屏幕坐标（用于截图）和 WPF 坐标（用于定位窗口）
                ProcessSelectionAsync(screenRect, wpfRect);
            }
        }

        private async void ProcessSelectionAsync(Rect screenRect, Rect wpfRect)
        {
            try
            {
                string sourceLang = _settingsService.Settings.SourceLanguage;
                string targetLang = _settingsService.Settings.TargetLanguage;

                // 使用屏幕坐标进行 OCR（OcrService 不再进行 DPI 转换）
                string ocrResult = await _ocrService.RecognizeTextAsync(screenRect, sourceLang);

                if (string.IsNullOrWhiteSpace(ocrResult))
                {
                    _loggerService.Log("OCR 未识别到文本", LogLevel.Warning);
                    
                    // 结果窗口使用 WPF 坐标定位
                    _resultWindow.Left = wpfRect.Right + 10;
                    _resultWindow.Top = wpfRect.Top;
                    _resultWindow.SetResultText("未识别到文本\n\n提示：\n• 确保选区包含清晰的文字\n• 尝试放大选区范围\n• 检查语言设置是否正确");
                    _resultWindow.ShowAndAutoHide();
                    return;
                }

                _loggerService.Log($"OCR 识别成功: {ocrResult.Substring(0, Math.Min(50, ocrResult.Length))}...");

                string translatedText = await _translationService.TranslateAsync(ocrResult, sourceLang, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    // 结果窗口使用 WPF 坐标定位
                    _resultWindow.Left = wpfRect.Right + 10;
                    _resultWindow.Top = wpfRect.Top;
                    _resultWindow.SetResultText(translatedText);
                    _resultWindow.ShowAndAutoHide();
                    
                    _loggerService.Log($"翻译结果窗口位置 - WPF坐标: ({_resultWindow.Left:F0}, {_resultWindow.Top:F0})");
                    
                    // 只有成功的翻译才记录历史
                    if (!translatedText.StartsWith("错误") && !translatedText.Contains("失败"))
                    {
                        _historyService.AddRecord(ocrResult, translatedText);
                        _loggerService.Log("翻译成功并已记录历史");
                    }
                }
            }
            catch (OutOfMemoryException ex)
            {
                _loggerService.LogError("内存不足", ex);
                MessageBox.Show("内存不足，请尝试选择较小的区域", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _loggerService.LogError("处理截图识别时发生错误", ex);
                MessageBox.Show($"处理失败：{ex.Message}\n\n详细信息已记录到日志文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMouseMove(Point point)
        {
            if (isSelectionActive)
            {
                // 计算屏幕坐标
                double screenLeft = Math.Min(point.X, selectionStartPoint.X);
                double screenTop = Math.Min(point.Y, selectionStartPoint.Y);
                double screenWidth = Math.Abs(point.X - selectionStartPoint.X);
                double screenHeight = Math.Abs(point.Y - selectionStartPoint.Y);
                
                // 转换为 WPF 坐标显示选择框
                _selectionWindow.Left = screenLeft / currentDpiScaleX;
                _selectionWindow.Top = screenTop / currentDpiScaleY;
                _selectionWindow.Width = screenWidth / currentDpiScaleX;
                _selectionWindow.Height = screenHeight / currentDpiScaleY;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _selectionWindow?.Close();
            _resultWindow?.Close();
        }

        private Matrix GetDpiTransformMatrix()
        {
            PresentationSource source = null;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsVisible)
                {
                    source = PresentationSource.FromVisual(window);
                    break;
                }
            }
            if (source == null)
            {
                source = PresentationSource.FromVisual(Application.Current.MainWindow);
            }
            return source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        }
    }
}
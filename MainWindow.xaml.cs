using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace translation
{
    public partial class MainWindow : Window
    {
        // 字段声明移到 App.xaml.cs 统一管理，这里不再需要
        // 但为了接收实例，我们暂时保留
        private SelectionWindow _selectionWindow;
        private OcrService _ocrService;
        private TranslationService _translationService;
        private ResultWindow _resultWindow;
        private HistoryService _historyService;

        private bool isMiddleButtonDown = false;
        private Point selectionStartPoint;

        public MainWindow()
        {
            // 构造函数现在非常干净，只负责UI初始化
            InitializeComponent();
        }

        // 新增一个初始化方法，由 App.xaml.cs 调用
        public void InitializeServices(
            SettingsService settingsService,
            SelectionWindow selectionWindow,
            OcrService ocrService,
            ResultWindow resultWindow,
            HistoryService historyService)
        {
            _selectionWindow = selectionWindow;
            _ocrService = ocrService;
            _resultWindow = resultWindow;
            _historyService = historyService;
            // TranslationService 依赖 settingsService，单独创建
            _translationService = new TranslationService(settingsService);

            // 将 Closing 事件移到这里，确保在服务初始化后再订阅
            this.Closing += MainWindow_Closing;
        }


        public void SubscribeToMouseHook(GlobalMouseHook mouseHook)
        {
            if (mouseHook != null)
            {
                mouseHook.MiddleButtonDown += MouseHook_MiddleButtonDown;
                mouseHook.MiddleButtonUp += HandleMiddleButtonUpAsync;
                mouseHook.MouseMove += MouseHook_MouseMove;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 关闭由 App 类管理的窗口
            _selectionWindow?.Close();
            _resultWindow?.Close();
        }

        // ... 以下所有方法完全保持不变 ...

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

        private void MouseHook_MiddleButtonDown(Point point)
        {
            _resultWindow.Hide();
            isMiddleButtonDown = true;
            selectionStartPoint = point;

            _selectionWindow.Width = 0;
            _selectionWindow.Height = 0;
            _selectionWindow.Show();

            var transform = GetDpiTransformMatrix();
            if (transform.M11 == 0 || transform.M22 == 0) return;

            _selectionWindow.Left = point.X / transform.M11;
            _selectionWindow.Top = point.Y / transform.M22;
        }

        private async void HandleMiddleButtonUpAsync(Point point)
        {
            if (!isMiddleButtonDown) return;
            isMiddleButtonDown = false;
            System.Windows.Rect selectionRect = new System.Windows.Rect(_selectionWindow.Left, _selectionWindow.Top, _selectionWindow.Width, _selectionWindow.Height);
            _selectionWindow.Hide();
            if (selectionRect.Width < 10 || selectionRect.Height < 10) return;
            try
            {
                string ocrResult = await _ocrService.RecognizeTextAsync(selectionRect);
                if (!string.IsNullOrWhiteSpace(ocrResult))
                {
                    string translatedText = await _translationService.TranslateAsync(ocrResult, "auto", "zh-CN");
                    if (!string.IsNullOrWhiteSpace(translatedText))
                    {
                        // --- 核心修改：分步调用 ResultWindow 的新方法 ---

                        // 1. 先设置好窗口的初始位置和要显示的文本
                        _resultWindow.Left = selectionRect.Right;
                        _resultWindow.Top = selectionRect.Top;
                        _resultWindow.SetResultText(translatedText); // 使用新方法设置文本

                        // 2. 然后调用新的显示方法，它会自己处理位置调整和自动关闭
                        _resultWindow.ShowAndAutoHide();

                        // 保存记录的逻辑保持不变
                        _historyService.AddRecord(ocrResult, translatedText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("处理截图识别时发生错误: " + ex.Message);
            }
        }

        private void MouseHook_MouseMove(Point point)
        {
            if (isMiddleButtonDown)
            {
                var transform = GetDpiTransformMatrix();
                if (transform.M11 == 0 || transform.M22 == 0) return;
                var x = Math.Min(point.X, selectionStartPoint.X);
                var y = Math.Min(point.Y, selectionStartPoint.Y);
                var width = Math.Abs(point.X - selectionStartPoint.X);
                var height = Math.Abs(point.Y - selectionStartPoint.Y);
                _selectionWindow.Left = x / transform.M11;
                _selectionWindow.Top = y / transform.M22;
                _selectionWindow.Width = width / transform.M11;
                _selectionWindow.Height = height / transform.M22;
            }
        }
    }
}
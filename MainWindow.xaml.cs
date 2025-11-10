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
        private SelectionWindow _selectionWindow;
        private OcrService _ocrService;
        private TranslationService _translationService;
        private ResultWindow _resultWindow;
        private HistoryService _historyService; // <-- 新增历史服务

        private bool isMiddleButtonDown = false;
        private Point selectionStartPoint;

        public MainWindow()
        {
            try
            {
                string exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", $"{exeFolder}\\x64;{path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置环境变量失败: " + ex.ToString());
            }

            InitializeComponent();

            _selectionWindow = new SelectionWindow();
            _ocrService = new OcrService();
            _translationService = new TranslationService();
            _resultWindow = new ResultWindow();
            _historyService = new HistoryService(); // <-- 初始化历史服务

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
                        _resultWindow.Left = selectionRect.Right;
                        _resultWindow.Top = selectionRect.Top;
                        _resultWindow.ShowResult(translatedText);

                        // --- 核心新增：保存记录！---
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
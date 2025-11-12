using System;
using System.Diagnostics;
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
        private HistoryService _historyService;
        private SettingsService _settingsService;

        private bool isSelectionActive = false;
        private Point selectionStartPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void InitializeServices(
            SettingsService settingsService,
            SelectionWindow selectionWindow,
            OcrService ocrService,
            ResultWindow resultWindow,
            HistoryService historyService)
        {
            _settingsService = settingsService;
            _selectionWindow = selectionWindow;
            _ocrService = ocrService;
            _resultWindow = resultWindow;
            _historyService = historyService;
            _translationService = new TranslationService(settingsService);
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
                selectionStartPoint = point;
                _selectionWindow.Width = 0;
                _selectionWindow.Height = 0;
                _selectionWindow.Show();
                var transform = GetDpiTransformMatrix();
                if (transform.M11 == 0 || transform.M22 == 0) return;
                _selectionWindow.Left = point.X / transform.M11;
                _selectionWindow.Top = point.Y / transform.M22;
            }
        }

        private void OnButtonUp(Point point, GlobalMouseHook.MouseButton button, GlobalMouseHook.ModifierKeys modifiers)
        {
            if (isSelectionActive)
            {
                isSelectionActive = false;
                System.Windows.Rect selectionRect = new System.Windows.Rect(_selectionWindow.Left, _selectionWindow.Top, _selectionWindow.Width, _selectionWindow.Height);
                _selectionWindow.Hide();
                if (selectionRect.Width < 10 || selectionRect.Height < 10) return;
                ProcessSelectionAsync(selectionRect);
            }
        }

        private async void ProcessSelectionAsync(Rect selectionRect)
        {
            try
            {
                string sourceLang = _settingsService.Settings.SourceLanguage;
                string targetLang = _settingsService.Settings.TargetLanguage;

                string ocrResult = await _ocrService.RecognizeTextAsync(selectionRect, sourceLang);

                if (!string.IsNullOrWhiteSpace(ocrResult))
                {
                    string translatedText = await _translationService.TranslateAsync(ocrResult, sourceLang, targetLang);
                    if (!string.IsNullOrWhiteSpace(translatedText))
                    {
                        _resultWindow.Left = selectionRect.Right;
                        _resultWindow.Top = selectionRect.Top;
                        _resultWindow.SetResultText(translatedText);
                        _resultWindow.ShowAndAutoHide();
                        _historyService.AddRecord(ocrResult, translatedText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("处理截图识别时发生错误: " + ex.Message);
            }
        }

        private void OnMouseMove(Point point)
        {
            if (isSelectionActive)
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
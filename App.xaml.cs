using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace translation
{
    public partial class App : Application
    {
        // 将所有核心对象作为 App 类的字段，由 App 统一管理生命周期
        private TaskbarIcon _notifyIcon;
        private GlobalMouseHook _mouseHook;

        // 服务
        private SettingsService _settingsService;
        private HistoryService _historyService;
        private OcrService _ocrService;

        // 窗口
        private MainWindow _mainWindow;
        private SettingsWindow _settingsWindow;
        private HistoryWindow _historyWindow;
        private SelectionWindow _selectionWindow;
        private ResultWindow _resultWindow;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // --- 保证正确的初始化顺序 ---

            // 1. 先创建所有后台服务和工具窗口的实例
            _settingsService = new SettingsService();
            _historyService = new HistoryService();
            _ocrService = new OcrService();
            _selectionWindow = new SelectionWindow();
            _resultWindow = new ResultWindow();

            // 2. 创建主逻辑窗口（但它依然是隐藏的）
            _mainWindow = new MainWindow();
            // 将所有需要的服务实例传递给主窗口
            _mainWindow.InitializeServices(_settingsService, _selectionWindow, _ocrService, _resultWindow, _historyService);

            // 3. 在所有窗口都创建完毕后，最后创建托盘图标
            // 这个顺序可以最大限度地避免“两个图标”的bug
            _notifyIcon = new TaskbarIcon();
            _notifyIcon.ToolTipText = "划词翻译工具";
            _notifyIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/translation.ico"));

            var contextMenu = new ContextMenu();

            var settingsMenuItem = new MenuItem { Header = "设置" };
            settingsMenuItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsMenuItem);

            var historyMenuItem = new MenuItem { Header = "查看翻译历史" };
            historyMenuItem.Click += HistoryMenuItem_Click;
            contextMenu.Items.Add(historyMenuItem);

            contextMenu.Items.Add(new Separator());

            var exitMenuItem = new MenuItem { Header = "退出" };
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenu = contextMenu;

            // 4. 最后，启动后台监听
            _mouseHook = new GlobalMouseHook();
            _mouseHook.Install();
            _mainWindow.SubscribeToMouseHook(_mouseHook);
        }

        // ... 以下所有方法完全保持不变 ...

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
            }
            else
            {
                _settingsWindow = new SettingsWindow(_settingsService);
                _settingsWindow.Show();
            }
        }

        private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_historyWindow != null && _historyWindow.IsVisible)
            {
                _historyWindow.Activate();
            }
            else
            {
                _historyWindow = new HistoryWindow();
                _historyWindow.ShowHistory(_historyService.GetHistory());
                _historyWindow.Show();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 关闭所有窗口
            _mainWindow?.Close();
            _settingsWindow?.Close();
            _historyWindow?.Close();
            // 彻底关闭程序
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 确保所有资源都被释放
            _notifyIcon?.Dispose();
            _mouseHook?.Dispose();
            base.OnExit(e);
        }
    }
}
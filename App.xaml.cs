using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace translation
{
    public partial class App : Application
    {
        internal GlobalMouseHook _mouseHook;
        private TaskbarIcon _notifyIcon;
        private MainWindow _mainWindow;
        private HistoryService _historyService; // <-- 新增
        private HistoryWindow _historyWindow;   // <-- 新增

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            _historyService = new HistoryService(); // <-- 初始化

            _notifyIcon = new TaskbarIcon();
            _notifyIcon.ToolTipText = "划词翻译工具";
            _notifyIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/translation.ico"));

            var contextMenu = new ContextMenu();

            // --- 新增“查看历史”菜单项 ---
            var historyMenuItem = new MenuItem { Header = "查看翻译历史" };
            historyMenuItem.Click += HistoryMenuItem_Click;
            contextMenu.Items.Add(historyMenuItem);

            // 添加一个分隔线
            contextMenu.Items.Add(new Separator());

            var exitMenuItem = new MenuItem { Header = "退出" };
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenu = contextMenu;

            _mouseHook = new GlobalMouseHook();
            _mouseHook.Install();
            _mainWindow.SubscribeToMouseHook(_mouseHook);
        }

        // “查看历史”菜单项的点击事件
        private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 如果窗口已经打开，就把它激活到最前；否则就新建一个
            if (_historyWindow != null && _historyWindow.IsVisible)
            {
                _historyWindow.Activate();
            }
            else
            {
                _historyWindow = new HistoryWindow();
                // 从服务加载数据并显示
                _historyWindow.ShowHistory(_historyService.GetHistory());
                _historyWindow.Show();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow?.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _mouseHook?.Dispose();
            base.OnExit(e);
        }
    }
}
using System;
using System.Windows;
using System.Windows.Threading; // 引入定时器所需的命名空间

namespace translation
{
    public partial class ResultWindow : Window
    {
        private DispatcherTimer _closeTimer; // 用于自动关闭的定时器

        public ResultWindow()
        {
            InitializeComponent();

            // 初始化定时器
            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(5); // 设置为5秒
            _closeTimer.Tick += CloseTimer_Tick; // 时间到了就调用 CloseTimer_Tick 方法
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            _closeTimer.Stop(); // 停止计时器
            this.Hide();        // 隐藏窗口，而不是关闭，这样下次可以快速显示
        }

        // 我们将 ShowResult 方法的功能拆分得更清晰
        public void SetResultText(string text)
        {
            ResultTextBlock.Text = text;
        }

        public void ShowAndAutoHide()
        {
            // --- 核心新增：智能定位逻辑 ---
            AdjustPosition();

            // 显示窗口
            this.Show();

            // 重置并启动5秒关闭定时器
            _closeTimer.Stop();
            _closeTimer.Start();
        }

        // 当鼠标进入窗口时，停止计时，防止在用户阅读时窗口突然关闭
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _closeTimer.Stop();
        }

        // 当鼠标离开窗口时，重新开始计时
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _closeTimer.Start();
        }


        // --- 核心新增：自动调整位置的方法 ---
        private void AdjustPosition()
        {
            // 在设置 Left 和 Top 之后，我们还不知道窗口的实际宽度和高度
            // UpdateLayout() 会强制 WPF 立即计算窗口内容所需的尺寸
            this.UpdateLayout();

            // 获取当前显示器的屏幕工作区大小（不包括任务栏）
            var screen = System.Windows.SystemParameters.WorkArea;

            double actualRight = this.Left + this.ActualWidth;
            double actualBottom = this.Top + this.ActualHeight;

            // 检查是否超出了右边界
            if (actualRight > screen.Right)
            {
                // 如果超出，将窗口向左移动超出的距离
                // 减去 10 像素作为边距，避免紧贴边缘
                this.Left -= (actualRight - screen.Right + 10);
            }

            // 检查是否超出了下边界
            if (actualBottom > screen.Bottom)
            {
                // 如果超出，将窗口向上移动超出的距离
                // 减去 10 像素作为边距
                this.Top -= (actualBottom - screen.Bottom + 10);
            }

            // 检查是否超出了左边界（虽然可能性小，但以防万一）
            if (this.Left < screen.Left)
            {
                this.Left = screen.Left;
            }

            // 检查是否超出了上边界
            if (this.Top < screen.Top)
            {
                this.Top = screen.Top;
            }
        }
    }
}
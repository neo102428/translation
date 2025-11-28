using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace translation
{
    public partial class ResultWindow : Window
    {
        private DispatcherTimer _closeTimer;
        private ThemeMode _currentTheme = ThemeMode.Dark;

        public ResultWindow()
        {
            InitializeComponent();

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(5);
            _closeTimer.Tick += CloseTimer_Tick;
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            _closeTimer.Stop();
            this.Hide();
        }

        public void SetResultText(string text)
        {
            ResultTextBox.Text = text;
        }

        public void ApplyTheme(ThemeMode theme)
        {
            _currentTheme = theme;
            if (theme == ThemeMode.Light)
            {
                RootBorder.Background = new SolidColorBrush(Color.FromArgb(221, 255, 255, 255));
                ResultTextBox.Foreground = new SolidColorBrush(Colors.Black);
                ResultTextBox.CaretBrush = new SolidColorBrush(Colors.Black);
            }
            else
            {
                RootBorder.Background = new SolidColorBrush(Color.FromArgb(221, 0, 0, 0));
                ResultTextBox.Foreground = new SolidColorBrush(Colors.White);
                ResultTextBox.CaretBrush = new SolidColorBrush(Colors.White);
            }
        }

        public void ShowAndAutoHide()
        {
            AdjustPosition();
            this.Show();

            this.Focus();

            _closeTimer.Stop();
            _closeTimer.Start();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _closeTimer.Stop();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!this.IsMouseCaptured)
            {
                _closeTimer.Start();
            }
        }

        private void AdjustPosition()
        {
            this.UpdateLayout();
            var screen = SystemParameters.WorkArea;
            double actualRight = this.Left + this.ActualWidth;
            double actualBottom = this.Top + this.ActualHeight;

            if (actualRight > screen.Right)
            {
                this.Left -= (actualRight - screen.Right + 10);
            }
            if (actualBottom > screen.Bottom)
            {
                this.Top -= (actualBottom - screen.Bottom + 10);
            }
            if (this.Left < screen.Left)
            {
                this.Left = screen.Left;
            }
            if (this.Top < screen.Top)
            {
                this.Top = screen.Top;
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                return;
            }

            this.DragMove();
        }
    }
}
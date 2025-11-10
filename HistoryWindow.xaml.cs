using System.Collections.Generic;
using System.Windows;

namespace translation
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
        }

        // 提供一个公共方法来填充数据
        public void ShowHistory(List<TranslationRecord> records)
        {
            HistoryListView.ItemsSource = records;
        }
    }
}
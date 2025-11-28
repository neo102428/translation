using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;

namespace translation
{
    public partial class HistoryWindow : Window
    {
        private readonly HistoryService _historyService;
        private ObservableCollection<TranslationRecord> _records;
        private List<TranslationRecord> _allRecords;

        public HistoryWindow(HistoryService historyService)
        {
            InitializeComponent();
            _historyService = historyService;
        }

        public void ShowHistory(List<TranslationRecord> records)
        {
            _allRecords = records;
            _records = new ObservableCollection<TranslationRecord>(records);
            HistoryListView.ItemsSource = _records;
            UpdateRecordCount();
        }

        private void UpdateRecordCount()
        {
            if (_records != null)
            {
                RecordCountText.Text = $"共 {_records.Count} 条记录";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_allRecords == null) return;

            string searchText = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _records.Clear();
                foreach (var record in _allRecords)
                {
                    _records.Add(record);
                }
            }
            else
            {
                var filtered = _allRecords.Where(r =>
                    r.OriginalText.ToLower().Contains(searchText) ||
                    r.TranslatedText.ToLower().Contains(searchText)
                ).ToList();

                _records.Clear();
                foreach (var record in filtered)
                {
                    _records.Add(record);
                }
            }

            UpdateRecordCount();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecord = HistoryListView.SelectedItem as TranslationRecord;

            if (selectedRecord == null)
            {
                MessageBox.Show("请先在列表中选择一条要删除的记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("确定要删除这条记录吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _historyService.DeleteRecord(selectedRecord);
                _records.Remove(selectedRecord);
                _allRecords.Remove(selectedRecord);
                UpdateRecordCount();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (_records == null || _records.Count == 0)
            {
                MessageBox.Show("没有记录可以清空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"您确定要清空所有 {_records.Count} 条历史记录吗？\n\n此操作不可恢复！", 
                "⚠️ 危险操作", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _historyService.ClearHistory();
                _records.Clear();
                _allRecords.Clear();
                UpdateRecordCount();
                MessageBox.Show("所有历史记录已清空。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
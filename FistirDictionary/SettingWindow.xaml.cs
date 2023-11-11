using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Configuration;

namespace FistirDictionary
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        private Settings? settings { get; set; }

        public SettingWindow()
        {
            InitializeComponent();

            settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            SetDictionaryGroups();
            DictionaryGroupList.SelectedIndex = 0;
        }

        private void SetDictionaryGroups()
        {
            DictionaryGroupList.Items.Clear();
            foreach (var dg in settings.DictionaryGroups)
            {
                DictionaryGroupList.Items.Add(dg.GroupName);
            }
        }

        private void DictionaryGroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DictionaryGroupList.SelectedIndex < 0) {
                AddDictionary.IsEnabled = false;
                AddExistingDictionary.IsEnabled = false;
                return;
            }
            else
            {
                AddDictionary.IsEnabled = true;
                AddExistingDictionary.IsEnabled = true;
            }
            var idx = DictionaryGroupList.SelectedIndex;
            var group = settings.DictionaryGroups[idx];
            try
            {
                GroupName.Text = group.GroupName;
                DictionaryList.ItemsSource = group.DictionaryEntries
                    .Select(entry => (entry, FDictionary.GetDictionaryMetadata(entry.DictionaryPath)))
                    .Select(metadata => new DictionaryInfo {
                        Path = metadata.entry.DictionaryPath,
                        Name = metadata.Item2.First(row => row.Headword == "__Name").Translation,
                        Description = metadata.Item2.First(row => row.Headword == "__Description").Translation,
                        WordCount = FDictionary.GetWordCount(metadata.entry.DictionaryPath),
                        ScansionScript = metadata.entry.ScansionScript,
                        EnableHistory = metadata.Item2.First(row => row.Headword == "__EnableHistory").Translation
                            .ToLower() == "true" ? true : false,
                    })
                    .ToList();
                GroupName.IsEnabled = false;
                CancelChangeGroupName.Visibility = Visibility.Hidden;
                ChangeGroupName.Content = "変更";
            }
            catch (FDictionary.DictionaryNotFoundExcepction ex)
            {
                MessageBox.Show($"辞書グループの読み込みに失敗しました。 {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var idx = DictionaryGroupList.SelectedIndex;
            if (GroupName.IsEnabled)
            {
                GroupName.IsEnabled = false;
                CancelChangeGroupName.Visibility = Visibility.Hidden;
                ChangeGroupName.Content = "変更";
                if (GroupName.Text != "")
                {
                    settings.DictionaryGroups[idx].GroupName = GroupName.Text;
                    settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                    SetDictionaryGroups();
                }
            }
            else
            {
                GroupName.IsEnabled = true;
                ChangeGroupName.Content = "適用";
                CancelChangeGroupName.Visibility = Visibility.Visible;
            }
        }

        private void CancelChangeGroupName_Click(object sender, RoutedEventArgs e)
        {
            if (GroupName.IsEnabled)
            {
                GroupName.IsEnabled = false;
                CancelChangeGroupName.Visibility = Visibility.Hidden;
                ChangeGroupName.Content = "変更";
            }
        }

        private void DeleteDictionaryGroup_Click(object sender, RoutedEventArgs e)
        {
            if (DictionaryGroupList.SelectedIndex < 0) { return; }
            var yesNoResult = MessageBox.Show(
                $"辞書グループを削除しますか？",
                "辞書グループの削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation,
                MessageBoxResult.Cancel);
            if (yesNoResult == MessageBoxResult.Yes)
            {
                var idx = DictionaryGroupList.SelectedIndex;
                settings.DictionaryGroups.RemoveAt(idx);
                if (idx <= settings.DefaultGroupIndex && settings.DefaultGroupIndex != null)
                {
                    settings.DefaultGroupIndex--;
                }
                if (settings.DictionaryGroups.Count < 1)
                {
                    settings.DefaultGroupIndex = null;
                }
                settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                SetDictionaryGroups();
            }
        }

        private void NewDictionaryGroup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddGroupDialog();
            var result = dialog.ShowDialog();
            if (result == true)
            {
                settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                SetDictionaryGroups();
                DictionaryGroupList.SelectedIndex = DictionaryGroupList.Items.Count - 1;
            }
        }

        private void EditDictinoary_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DictionaryInfo;
            var dialog = new EditDictionaryDialog(row.Path, row.Name, row.Description, row.EnableHistory, (string)DictionaryGroupList.SelectedValue, row.ScansionScript);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                var idx = DictionaryGroupList.SelectedIndex;
                var group = settings.DictionaryGroups[idx];
                DictionaryList.ItemsSource = group.DictionaryEntries
                    .Select(entry => (entry, FDictionary.GetDictionaryMetadata(entry.DictionaryPath)))
                    .Select(metadata => new DictionaryInfo {
                        Path = metadata.entry.DictionaryPath,
                        Name = metadata.Item2.First(row => row.Headword == "__Name").Translation,
                        Description = metadata.Item2.First(row => row.Headword == "__Description").Translation,
                        WordCount = FDictionary.GetWordCount(metadata.entry.DictionaryPath),
                        ScansionScript = metadata.entry.ScansionScript,
                        EnableHistory = metadata.Item2.First(row => row.Headword == "__EnableHistory").Translation
                            .ToLower() == "true" ? true : false,
                    })
                    .ToList();
                GroupName.IsEnabled = false;
                CancelChangeGroupName.Visibility = Visibility.Hidden;
                ChangeGroupName.Content = "変更";
            }
        }

        private void AddDictionary_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewDictionaryDialog(DictionaryGroupList.SelectedIndex);
            var result = dialog.ShowDialog();
            if (result == true) {
                settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                var idx = DictionaryGroupList.SelectedIndex;
                var group = settings.DictionaryGroups[idx];
                DictionaryList.ItemsSource = group.DictionaryEntries
                    .Select(entry => (entry, FDictionary.GetDictionaryMetadata(entry.DictionaryPath)))
                    .Select(metadata => new DictionaryInfo {
                        Path = metadata.entry.DictionaryPath,
                        Name = metadata.Item2.First(row => row.Headword == "__Name").Translation,
                        Description = metadata.Item2.First(row => row.Headword == "__Description").Translation,
                        WordCount = FDictionary.GetWordCount(metadata.entry.DictionaryPath),
                        ScansionScript = metadata.entry.ScansionScript,
                        EnableHistory = metadata.Item2.First(row => row.Headword == "__EnableHistory").Translation
                            .ToLower() == "true" ? true : false,
                    })
                    .ToList();
                GroupName.IsEnabled = false;
                CancelChangeGroupName.Visibility = Visibility.Hidden;
                ChangeGroupName.Content = "変更";
            }
        }

        private void AddExistingDictionary_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddExistingDictionaryDialog(DictionaryGroupList.SelectedIndex);
            var result = dialog.ShowDialog();
            if (result == true) {
                settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                var idx = DictionaryGroupList.SelectedIndex;
                var group = settings.DictionaryGroups[idx];
                DictionaryList.ItemsSource = group.DictionaryEntries
                    .Select(entry => (entry, FDictionary.GetDictionaryMetadata(entry.DictionaryPath)))
                    .Select(metadata => new DictionaryInfo {
                        Path = metadata.entry.DictionaryPath,
                        Name = metadata.Item2.First(row => row.Headword == "__Name").Translation,
                        Description = metadata.Item2.First(row => row.Headword == "__Description").Translation,
                        WordCount = FDictionary.GetWordCount(metadata.entry.DictionaryPath),
                        ScansionScript = metadata.entry.ScansionScript,
                        EnableHistory = metadata.Item2.First(row => row.Headword == "__EnableHistory").Translation
                            .ToLower() == "true" ? true : false,
                    })
                    .ToList();
                GroupName.IsEnabled = false;
                CancelChangeGroupName.Visibility = Visibility.Hidden;
                ChangeGroupName.Content = "変更";
            }
        }
    }

    public class DictionaryInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int WordCount { get; set; }
        public string Path { get; set; }
        public string ScansionScript { get; set; }
        public bool EnableHistory { get; set; }
    }
}

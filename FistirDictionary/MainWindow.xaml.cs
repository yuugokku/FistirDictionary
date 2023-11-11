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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML.Excel;
using FistirDictionary.ModelExcelTable;
using System.Xml.Serialization;
using System.Diagnostics;
using Colors = System.Windows.Media.Colors;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace FistirDictionary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class WordView
        {
            public string? dictionaryName { get; set; }
            public string? dictionaryPath { get; set; }
            public Word _Word { get; set; }
        }
        /// <summary>
        ///   左サイドに表示する単語リスト。
        /// </summary>
        private List<WordView> _Words { get; set; }

        private int? PreviousDefaultGroupIndex { get; set; }
        /// <summary>
        ///   fdic-settings.jsonの中身
        /// </summary>
        private Settings settings { get; set; }

        /// <summary>
        ///   いま開いている辞書グループに属する辞書ファイルのパスの一覧
        ///   Key: 辞書の名前
        ///   Value: 辞書のパス
        /// </summary>
        private Dictionary<string, DictionaryEntry> DictionaryPaths { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DictionaryPaths = new Dictionary<string, DictionaryEntry>();
            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            settings = SettingSerializer.LoadSettings(settingsPath);
            var groups = settings.DictionaryGroups != null ?
                settings.DictionaryGroups.Select(gr => gr.GroupName).ToList() :
                new List<string>();
            GroupName.ItemsSource = groups;
            if (settings.DefaultGroupIndex != null)
            {
                GroupName.SelectedIndex = (int)settings.DefaultGroupIndex;
                SetAsDefault.IsChecked = true;
            } else
            {
                GroupName.SelectedIndex = 0;
            }
            WordsViewDataGrid.ItemsSource = _Words;
        }

        private DebounceDispatcher debounceTimer = new DebounceDispatcher();
        private void SearchItem_SearchChanged(object sender, EventArgs e)
        {
            if (SearchItemStack == null) return;
            var searchStatements = new List<SearchStatement>();
            foreach (SearchItem searchItem in SearchItemStack.Children)
            {
                if (searchItem.Keyword == "") continue;
                searchStatements.Add(new SearchStatement
                {
                    Keyword = searchItem.Keyword,
                    Method = searchItem.Method,
                    Target = searchItem.Target,
                });
            }
            debounceTimer.Debounce(200, async (statements) =>
            {
                var _searchStatements = (List<SearchStatement>)statements;
                if (searchStatements.Count == 0) return;
                IEnumerable<WordView> words = new List<WordView>();
                WaitingScreen.Visibility = Visibility.Visible;
                try
                {
                    foreach (var dpPair in DictionaryPaths)
                    {
                        var dpath = dpPair.Value.DictionaryPath;
                        var ig = IgnoreCase.IsChecked == true ? true : false;
                        var spath = dpPair.Value.ScansionScript;
                        var result = await Task.Run(() =>
                        {
                            return FDictionary.SearchWord(
                                    dpath,
                                    _searchStatements.ToArray(),
                                    ig,
                                    spath);
                        });
                        words = words.Concat(
                            result.Select(word => new WordView
                                {
                                    dictionaryName = dpPair.Key,
                                    dictionaryPath = dpPair.Value.DictionaryPath,
                                    _Word = word,
                                }));
                    }
                }
                catch (System.Text.RegularExpressions.RegexParseException ex)
                {
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"検索エラー: {ex.Message}");
                    GroupName.SelectedValue = null;
                    return;
                }
                var mainQueryItem = _searchStatements.FirstOrDefault(
                    st =>
                        st.Target == SearchTarget.HeadwordTranslation ||
                        st.Target == SearchTarget.Headword ||
                        st.Target == SearchTarget.Translation);
                if (mainQueryItem != null)
                {
                    switch (mainQueryItem.Target)
                    {
                        case SearchTarget.Headword:
                        case SearchTarget.HeadwordTranslation:
                            words = words.OrderBy(
                                word => LevenshteinDistance.Calculate(
                                    word._Word.Headword, mainQueryItem.Keyword));
                                break;
                        case SearchTarget.Translation:
                            words = words.OrderBy(
                                word => LevenshteinDistance.Calculate(
                                    word._Word.Translation, mainQueryItem.Keyword));
                            break;
                        case default(SearchTarget):
                            break;
                    }
                }
                _Words = words.ToList();
                WordsViewDataGrid.ItemsSource = _Words;
                WaitingScreen.Visibility = Visibility.Hidden;
            }, searchStatements);
        }

        /// <summary>
        ///   「辞書を追加」メニューアイテム
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewDictionaryDialog();
            var result = dialog.ShowDialog();
            if (result == true)
            {
                var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
                settings = SettingSerializer.LoadSettings(settingsPath);
                var groups = settings.DictionaryGroups != null ?
                    settings.DictionaryGroups.Select(gr => gr.GroupName).ToList() :
                    new List<string?>();
                GroupName.ItemsSource = groups;
            }
        }

        /// <summary>
        ///   グループ名変更時。
        ///   設定ファイルを参照し、選択された辞書グループにある辞書一覧をすべてロード。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupName.SelectedValue == null || (string)GroupName.SelectedValue == "") {
                DictionaryPaths.Clear();
                return;
            }
            if ((string)GroupName.SelectedValue == "")
            {
                return;
            }
            DictionaryPaths = new Dictionary<string, DictionaryEntry>();
            var dictionariesInGroup = settings.DictionaryGroups?
                .First(dg => dg.GroupName == (string)GroupName.SelectedValue)
                .DictionaryEntries;
            IEnumerable<ICollection<Word>> metadataArray = null;
            try
            {
                metadataArray = dictionariesInGroup?
                    .Select(entry => FDictionary.GetDictionaryMetadata(entry.DictionaryPath));
                foreach (var d in metadataArray
                    .Select(words => (from w in words
                                     where w.Headword == "__Name"
                                     select w.Translation).First())
                    .Zip(dictionariesInGroup)
                    .Select(pair => new {Name = pair.First, Entry = pair.Second})) 
                {
                    if (d.Name != null)
                    {
                        DictionaryPaths.Add(d.Name, d.Entry);
                    }
                }
            }
            catch (FDictionary.DictionaryNotFoundExcepction ex)
            {
                MessageBox.Show($"辞書グループ {GroupName.SelectedValue} を選択できませんでした: {ex.Message}");
                GroupName.SelectedValue = null;
                return;
            }
            catch (FDictionary.DictionaryBrokenException ex)
            {
                MessageBox.Show($"辞書グループ {GroupName.SelectedValue} を選択できませんでした: {ex.Message}");
                GroupName.SelectedValue = null;
                return;
            }
            if (settings.DefaultGroupIndex == GroupName.SelectedIndex)
            {
                SetAsDefault.IsChecked = true;
                SetAsDefault.IsEnabled = false;
            }
            else
            {
                SetAsDefault.IsChecked = false;
                SetAsDefault.IsEnabled = true;
            }
        }

        private void AddWordButton_Click(object sender, RoutedEventArgs e)
        {
            var firstSearchItem = (SearchItem)SearchItemStack.Children[0];
            var dialog = new AddWordDialog(firstSearchItem.Keyword, (string)GroupName.SelectedValue, DictionaryPaths);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
                SearchItem_SearchChanged(sender, e);
            }
        }

        /// <summary>
        ///   「既存の辞書を追加」メニューアイテム
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new AddExistingDictionaryDialog();
            var result = dialog.ShowDialog();
            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            settings = SettingSerializer.LoadSettings(settingsPath);
            var groups = settings.DictionaryGroups != null ?
                settings.DictionaryGroups.Select(gr => gr.GroupName).ToList() :
                new List<string?>();
            GroupName.ItemsSource = groups;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            var dialog = new ImportFromPDICDialog();
            var result = dialog.ShowDialog();
            if (result  == true)
            {
                var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
                settings = SettingSerializer.LoadSettings(settingsPath);
                var groups = settings.DictionaryGroups != null ?
                    settings.DictionaryGroups.Select(gr => gr.GroupName).ToList() :
                    new List<string?>();
                GroupName.ItemsSource = groups;
            }
            
        }

        private void SetAsDefault_Checked(object sender, RoutedEventArgs e)
        {
            if (GroupName.SelectedIndex != settings.DefaultGroupIndex)
            {
                PreviousDefaultGroupIndex = settings.DefaultGroupIndex;
                settings.DefaultGroupIndex = GroupName.SelectedIndex;
                settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            }
        }

        private void SetAsDefault_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GroupName.SelectedIndex == settings.DefaultGroupIndex)
            {
                settings.DefaultGroupIndex = null;
                settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            }
        }

        /// <summary>
        ///   「条件を追加」ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var searchItem = new SearchItem();
            searchItem.SearchChanged += SearchItem_SearchChanged;
            searchItem.BorderThickness = new Thickness { Top = 0, Bottom = 1, Left = 0, Right = 0 };
            searchItem.BorderBrush = new SolidColorBrush(Colors.DarkGray);
            searchItem.Removed += RemoveButton_Click;
            searchItem.IsRemoveButtonVisible = true;
            SearchItemStack.Children.Add(searchItem);

            if (SearchItemStack.Children.Count > 1)
            {
                foreach (SearchItem si in SearchItemStack.Children )
                {
                    si.IsRemoveButtonVisible = true;
                }
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (SearchItemStack.Children.Count == 1)
            {
                return;
            }
            var indexOfSender = SearchItemStack.Children.IndexOf((SearchItem)sender);
            if (indexOfSender == -1)
            {
                return;
            }
            SearchItemStack.Children.RemoveAt(indexOfSender);
            if (SearchItemStack.Children.Count == 1)
            {
                var searchItem = (SearchItem)SearchItemStack.Children[0];
                searchItem.IsRemoveButtonVisible = false;
            }
            else if (SearchItemStack.Children.Count > 1)
            {
                foreach (SearchItem searchItem in SearchItemStack.Children )
                {
                    searchItem.IsRemoveButtonVisible = true;
                }
            }
        }

        private void WordsViewDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WordsViewDataGrid.SelectedIndex < 0) return;

            var idx = WordsViewDataGrid.SelectedIndex;
            var dialog = new EditWordDialog(
                _Words[idx].dictionaryName,
                _Words[idx].dictionaryPath,
                _Words[idx]._Word.WordID,
                _Words[idx]._Word.Headword,
                _Words[idx]._Word.Translation,
                _Words[idx]._Word.Example);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                SearchItem_SearchChanged(sender, e);
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        ///   「辞書の設定」
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingWindow();
            var result = dialog.ShowDialog();
            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            settings = SettingSerializer.LoadSettings(settingsPath);
            var groups = settings.DictionaryGroups != null ?
                settings.DictionaryGroups.Select(gr => gr.GroupName).ToList() :
                new List<string?>();
            GroupName.ItemsSource = groups;
            GroupName.SelectedIndex = settings.DefaultGroupIndex != null ? (int)settings.DefaultGroupIndex : 0;
        }

        private void IgnoreCase_Checked(object sender, RoutedEventArgs e)
        {
            SearchItem_SearchChanged(sender, e);
        }

        private void IgnoreCase_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchItem_SearchChanged(sender, e);
        }
    }
}

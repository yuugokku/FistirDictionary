using FistirDictionary.ModelExcelTable;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

namespace FistirDictionary
{
    /// <summary>
    /// AddWordDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class AddWordDialog : Window
    {
        /// <summary>
        ///   DictionaryPaths: 辞書名 ==> 辞書のパス
        /// </summary>
        private Dictionary<string, string> DictionaryPaths { get; set; }

        private Settings settings { get; set; }
        private string GroupName { get; set; }

        public AddWordDialog(string headword, string groupName, Dictionary<string, string> dictionaryPaths)
        {
            InitializeComponent();
            Headword.Text = headword;
            DictionaryPaths = dictionaryPaths;
            GroupName = groupName;
            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            TargetDictionary.ItemsSource = DictionaryPaths.Select(kv => kv.Key);
            settings = SettingSerializer.LoadSettings(settingsPath);
            var group = settings.DictionaryGroups?.Where(dg => dg.GroupName == groupName).First();
            if (group.DefaultDictionaryIndex != null && group.DefaultDictionaryIndex > -1)
            {
                TargetDictionary.SelectedIndex = (int)group.DefaultDictionaryIndex;
                SetAsDefault.IsChecked = true;
            }
            else
            {
                TargetDictionary.SelectedIndex = 0;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            string targetDictionaryPath;
            if (!DictionaryPaths.TryGetValue((string)TargetDictionary.SelectedValue, out targetDictionaryPath))
            {
                return;
            }
            FDictionary.AddWord(
                targetDictionaryPath,
                Headword.Text,
                Translation.Text,
                Example.Text);
            var group = settings.DictionaryGroups?.First(dg => dg.GroupName == GroupName);
            var defaultDictionaryIndex = settings.DictionaryGroups.First(dg => dg.GroupName == GroupName).DefaultDictionaryIndex;
            if (SetAsDefault.IsChecked == true && TargetDictionary.SelectedIndex != defaultDictionaryIndex)
            {
                group.DefaultDictionaryIndex = TargetDictionary.SelectedIndex;
                settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            }
            this.DialogResult = true;
            this.Close();
        }

        private void TargetDictionary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string)TargetDictionary.SelectedValue == "")
            {
                return;
            }
            var group = settings.DictionaryGroups?.First(dg => dg.GroupName == GroupName);
            if (group.DefaultDictionaryIndex == TargetDictionary.SelectedIndex)
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
    }
}

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
    /// EditDictionaryDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class EditDictionaryDialog : Window
    {
        private string DictionaryPath { get; set; }
        private string GroupName { get; set; }
        public EditDictionaryDialog(string dictionaryPath, string dictionaryName, string description, bool enableHistory, string groupName)
        {
            InitializeComponent();
            DictionaryPath = dictionaryPath;
            DictionaryTitle.Text = dictionaryName;
            Description.Text = description;
            EnableHistory.IsChecked = enableHistory;
            GroupName = groupName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "";
            dialog.DefaultExt = ".lua";
            dialog.Filter = "Luaスクリプト (.lua)|.lua";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                ScansionScriptPath.Text = dialog.FileName;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (DictionaryTitle.Text == "")
            {
                MessageBox.Show("辞書名を入力してください。");
                return;
            }
            FDictionary.UpdateWord(
                DictionaryPath,
                -1,
                "__Name",
                DictionaryTitle.Text,
                "");
            FDictionary.UpdateWord(
                DictionaryPath,
                -2,
                "__Description",
                Description.Text,
                "");
            FDictionary.UpdateWord(
                DictionaryPath,
                -3,
                "__ScansionScript",
                ScansionScriptPath.Text,
                "");
            FDictionary.UpdateWord(
                DictionaryPath,
                -5,
                "__EnableHistory",
                EnableHistory.IsChecked == true ? "true" : "false",
                "");
            this.DialogResult = true;
            this.Close();
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            var index = settings.DictionaryGroups.First(dg => dg.GroupName == GroupName).Dictionaries.IndexOf(DictionaryPath);
            if (index != -1)
            {
                settings.DictionaryGroups.First(dg => dg.GroupName == GroupName).Dictionaries.RemoveAt(index);
                settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}

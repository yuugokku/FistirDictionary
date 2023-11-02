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
using Newtonsoft.Json;
using System.Xml;
using System.Data;
using FistirDictionary.ModelExcelTable;
using System.IO;

namespace FistirDictionary
{
    /// <summary>
    /// ImportFromPDICDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ImportFromPDICDialog : Window
    {
        public ImportFromPDICDialog()
        {
            InitializeComponent();

            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            Settings? settings = null;
            try
            {
                settings = SettingSerializer.LoadSettings(settingsPath);
            }
            catch (JsonException)
            {
                MessageBox.Show($"設定ファイルの構文が不正です。\nパス: {settingsPath}");
                Environment.Exit(1);
            }
            if (settings == null) { return; }
            var groups = settings.DictionaryGroups != null ? settings.DictionaryGroups.Select(gr => gr.GroupName).ToList() : new List<string>();
            groups.Add("[新規作成]");
            GroupName.ItemsSource = groups;
            GroupName.SelectedIndex = 0;

            DictionaryPath.Text = System.IO.Path.Combine(
                System.Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ConfigurationManager.AppSettings.Get("defaultDictionaryDir"),
                "Dictionary.fdic");
        }

        private void SourcePathReference_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XMLファイル (*.xml)|*.xml";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                SourcePath.Text = dialog.FileName;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "";
            dialog.DefaultExt = "*.lua";
            dialog.Filter = "Luaスクリプト (*.lua)|*.lua";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                ScansionScriptPath.Text = dialog.FileName;
            }
        }

        private void GroupName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = GroupName.SelectedValue as string;
            if (selectedItem == "[新規作成]")
            {
                NewGroupName.Visibility = Visibility.Visible;
            }
            else
            {
                NewGroupName.Visibility = Visibility.Hidden;
            }
        }

        private void DictionaryTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            DictionaryPath.Text = System.IO.Path.Combine(
                System.Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ConfigurationManager.AppSettings.Get("defaultDictionaryDir"),
                $"{DictionaryTitle.Text}.fdic");
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            Settings? settings;
            settings = SettingSerializer.LoadSettings(settingsPath);

            if (File.Exists(DictionaryPath.Text))
            {
                MessageBox.Show($"ファイル {DictionaryPath.Text} は存在します。");
                return;
            }

            FDictionary.CreateEmptyDictionary(
                DictionaryPath.Text,
                DictionaryTitle.Text,
                Description.Text,
                ScansionScriptPath.Text,
                "",
                EnableHistory.IsChecked == true && EnableHistory.IsChecked != null,
                settings.Username != null && settings.Email != null ?
                    $"{settings.Username} <{settings.Email}>" :
                    "");

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(SourcePath.Text);
            var records = xmlDocument.SelectNodes("pdic/record");
            if (records == null || records.Count == 0)
            {
                MessageBox.Show("登録する単語がありません。");
                return;
            }
            var wordsToAdd = (from XmlNode record in records
                             select new Word
                             {
                                 Headword = record.SelectSingleNode("word")?.InnerText?.ToString(),
                                 Translation = record.SelectSingleNode("trans")?.InnerText?.ToString(),
                                 Example = record.SelectSingleNode("ex")?.InnerText?.ToString(),
                                 UpdatedAt = DateTime.Now,
                             }).ToArray();
            var counter = FDictionary.AddWord(DictionaryPath.Text, wordsToAdd);
            MessageBox.Show($"入力XMLファイルのパス: {SourcePath.Text}\n追加した単語数: {counter} 語\n出力先辞書ファイルのパス: {DictionaryPath.Text}");

            if (GroupName.Text == "[新規作成]")
            {
                if (settings.DictionaryGroups?.Where(st => st.GroupName == NewGroupName.Text).Count() > 0)
                {
                    MessageBox.Show($"{NewGroupName.Text} はすでに作成されています。");
                    return;
                }
                settings.DictionaryGroups.Add(new DictionaryGroup
                {
                    GroupName = NewGroupName.Text,
                    Dictionaries = new List<string>
                    {
                        DictionaryPath.Text
                    }
                });
                settings.Save(settingsPath);
            }
            else
            {
                settings.DictionaryGroups?
                    .First(st => st.GroupName == GroupName.Text)?
                    .Dictionaries?.Add(DictionaryPath.Text);
                settings.Save(settingsPath);
            }

            DialogResult = true;
            this.Close();
        }
    }
}

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

namespace FistirDictionary
{
    /// <summary>
    /// AddExistingDictionaryDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class AddExistingDictionaryDialog : Window
    {
        public AddExistingDictionaryDialog(int defaultDictionaryGroup = 0)
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
            GroupName.SelectedIndex = defaultDictionaryGroup;
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsPath = ConfigurationManager.AppSettings.Get("defaultSettingPath");
            Settings? settings;
            settings = SettingSerializer.LoadSettings(settingsPath);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "";
            dialog.DefaultExt = ".fdic";
            dialog.Filter = "FDIC辞書ファイル (*.fdic)|*.fdic";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                DictionaryPath.Text = dialog.FileName;
            }
        }
    }
}

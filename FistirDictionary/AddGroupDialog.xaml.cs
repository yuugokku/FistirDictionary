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
    /// AddGroupDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class AddGroupDialog : Window
    {
        public AddGroupDialog()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingSerializer.LoadSettings(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            if (NewGroupName.Text != null && NewGroupName.Text.Length < 1) { return; }
            if (settings.DictionaryGroups.Where(dg => dg.GroupName == NewGroupName.Text).Count() > 0)
            {
                MessageBox.Show($"辞書グループ {NewGroupName.Text} はすでに存在します。");
                return;
            }
            settings.DictionaryGroups.Add(
                new DictionaryGroup {
                    GroupName = NewGroupName.Text,
                    DefaultDictionaryIndex = null,
                    DictionaryEntries = new List<DictionaryEntry>() });
            settings.Save(ConfigurationManager.AppSettings.Get("defaultSettingPath"));
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

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

namespace FistirDictionary
{
    /// <summary>
    /// EditWordDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class EditWordDialog : Window
    {
        private int WordID { get; set; }
        private string DictionaryName { get; set; }
        private string DictionaryPath { get; set; }
        private ICollection<WordHistory> _WordHistory { get; set; }

        public EditWordDialog(
            string dictionaryName,
            string dictionaryPath,
            int wordID,
            string? headword,
            string? translation,
            string? example)
        {
            InitializeComponent();
            DictionaryName = dictionaryName;
            DictionaryPath = dictionaryPath;
            WordID = wordID;
            Headword.Text = headword;
            Translation.Text = translation;
            Example.Text = example;

            this.Title = $"単語を編集: {headword} (ID: {WordID}), {dictionaryName}";
            TargetDictionary.ItemsSource = new List<string> { dictionaryName };
            TargetDictionary.SelectedValue = dictionaryName;

            _WordHistory = FDictionary.GetWordHistory(DictionaryPath, WordID);
            WordHistoriesDataGrid.ItemsSource = _WordHistory;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            FDictionary.UpdateWord(
                DictionaryPath,
                WordID,
                Headword.Text,
                Translation.Text,
                Example.Text);
            this.DialogResult = true;
            this.Close();
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var yesNoResult = MessageBox.Show(
                $"単語を削除しますか？",
                "単語の削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation,
                MessageBoxResult.Cancel);
            if (yesNoResult == MessageBoxResult.Yes)
            {
                FDictionary.DeleteWord(DictionaryPath, WordID);
                this.DialogResult = true;
            }
        }
    }
}

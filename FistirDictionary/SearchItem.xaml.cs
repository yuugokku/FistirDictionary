using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace FistirDictionary
{
    /// <summary>
    /// SearchItem.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchItem : UserControl
    {
        public SearchItem()
        {
            InitializeComponent();
        }

        public bool IsRemoveButtonVisible
        {
            get
            {
                return RemoveButton.IsVisible;
            }
            set
            {
                if (value)
                {
                    RemoveButton.Visibility = Visibility.Visible;
                }
                else
                {
                    RemoveButton.Visibility = Visibility.Hidden;
                }
            }
        }

        public string Keyword
        {
            get { return (string)this.KeywordTextBox.Text; }
            set { this.KeywordTextBox.Text = value; }
        }

        public SearchTarget? Target
        {
            get
            {
                if ((bool)this.HeadwordTranslation.IsChecked)
                {
                    return SearchTarget.HeadwordTranslation;
                }
                if ((bool)this.Headword.IsChecked)
                {
                    return SearchTarget.Headword;
                }
                if ((bool)this.Translation.IsChecked)
                {
                    return SearchTarget.Translation;
                }
                if ((bool)this.Example.IsChecked)
                {
                    return SearchTarget.Example;
                }
                if ((bool)this.Rhyme.IsChecked)
                {
                    return SearchTarget.Rhyme;
                }
                return null;
            }
        }

        public SearchMethod? Method
        {
            get
            {
                if ((bool)Includes.IsChecked)
                {
                    return SearchMethod.Includes;
                }
                if ((bool)StartsWith.IsChecked)
                {
                    return SearchMethod.StartsWith;
                }
                if ((bool)EndsWith.IsChecked)
                {
                    return SearchMethod.EndsWith;
                }
                if ((bool)Is.IsChecked)
                {
                    return SearchMethod.Is;
                }
                if ((bool)RegexMatch.IsChecked)
                {
                    return SearchMethod.RegexMatch;
                }
                return null;
            }
        }

        public event EventHandler<EventArgs>? SearchChanged;
        [Browsable(true)]
        [Description("検索条件が変更されたときにトリガーされます。")]
        [Category("動作")]
        protected virtual void OnSearchChanged(EventArgs e)
        {
            SearchChanged?.Invoke(this, e);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            OnSearchChanged(e);
        }

        private void KeywordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnSearchChanged(e);
        }

        public event EventHandler<EventArgs>? Removed;
        [Browsable(true)]
        [Description("「削除」ボタンのクリック時にトリガーされます。")]
        [Category("動作")]
        protected virtual void OnRemoved(EventArgs e)
        {
            Removed?.Invoke(this, e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OnRemoved(e);
        }

    }
}

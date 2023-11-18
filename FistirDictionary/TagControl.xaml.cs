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
    /// TagControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TagControl : UserControl
    {
        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.Register(
                nameof(TagControl.Tags),
                typeof(List<string>),
                typeof(TagControl),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = new List<string>(),
                    BindsTwoWayByDefault = true,
                    PropertyChangedCallback = OnTagsChanged,
                });

        private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TagControl tc = (TagControl)d;
            if (tc != null)
            {
                tc.Tags = (List<string>)e.NewValue;
            }
        }

        public List<string> Tags {
            get {
                return (List<string>)GetValue(TagsProperty);
            }
            set
            {
                if (value != null)
                {
                    TagFlow.Children.Clear();
                    foreach(var tag in value)
                    {
                        var menuItem = new MenuItem() { Header = "削除" };
                        menuItem.Click += Delete_Click;
                        var menu = new ContextMenu();
                        menu.Items.Add(menuItem);
                        var textBlock = new TextBlock
                        {
                            Text = tag,
                            Padding = new Thickness(2),
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                            ContextMenu = menu
                        };
                        var border = new Border 
                        {
                            CornerRadius = new CornerRadius(5),
                            Background = new SolidColorBrush(Color.FromArgb(255, 0xDF, 0xF0, 0xFF))
                        };
                        var grid = new Grid { Margin = new Thickness(2, 2, 6, 2) };
                        grid.Children.Add(border);
                        grid.Children.Add(textBlock);
                        TagFlow.Children.Add(grid);
                    }
                    SetValue(TagsProperty, value);
                    OnTagsChanged(new EventArgs());
                }
            }
        }

        private bool _preventSwitch { get; set; }
        public bool PreventSwitch
        {
            get
            {
                var value = _preventSwitch;
                if (_preventSwitch)
                {
                    _preventSwitch = false;
                }
                return value;
            }
            private set
            {
                _preventSwitch = value;
            }
        }

        public event EventHandler<EventArgs>? TagsChanged;
        [Browsable(true)]
        [Description("Tagsプロパティが変更されたときにトリガーされます。")]
        [Category("動作")]
        protected virtual void OnTagsChanged(EventArgs e)
        {
            TagsChanged?.Invoke(this, e);
        }

        public TagControl()
        {
            InitializeComponent();
            TagFlow.Children.Clear();
            PreventSwitch = false;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Determine.Visibility = Visibility.Visible;
            Cancel.Visibility = Visibility.Visible;
            NewTag.Visibility = Visibility.Visible;
            Add.Visibility = Visibility.Collapsed;
        }

        private void Determine_Click(object sender, RoutedEventArgs e)
        {
            Determine.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Collapsed;
            NewTag.Visibility = Visibility.Collapsed;
            Add.Visibility = Visibility.Visible;

            Tags = Tags.Append(NewTag.Text).ToList();
            NewTag.Text = "";

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Determine.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Collapsed;
            NewTag.Visibility = Visibility.Collapsed;
            Add.Visibility = Visibility.Visible;
        }

        private void NewTag_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            PreventSwitch = true;
            if (e.Key == Key.Enter)
            {
                Determine_Click(sender, e); return;
            } else if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e); return;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var menu = (ContextMenu)menuItem.Parent;
            var textBlock = (TextBlock)menu.PlacementTarget;
            var tags = Tags.Where(item => item != textBlock.Text).ToList();
            Tags = tags;
        }
    }
}

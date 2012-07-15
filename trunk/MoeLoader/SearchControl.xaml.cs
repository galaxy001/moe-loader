using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Xml;

namespace MoeLoader
{
	/// <summary>
	/// Interaction logic for SearchControl.xaml
	/// </summary>
	public partial class SearchControl : UserControl
	{
        //private System.Net.HttpWebRequest req;
        private Storyboard sb;

        private SessionState currentSession;
        public event EventHandler Enteded;

		public SearchControl()
		{
			this.InitializeComponent();

            sb = FindResource("searching") as Storyboard;

            //sb.Begin(path, true);
            //sb.Pause();
		}

        public string Text
        {
            get
            {
                string text = (txtSearch.Template.FindName("PART_EditableTextBox", txtSearch) as TextBox).Text;
                if (text == "搜索") return "";
                else return text;
            }
            private set
            {
                (txtSearch.Template.FindName("PART_EditableTextBox", txtSearch) as TextBox).Text = value;
            }
        }

        private LinkedList<string> usedItems = new LinkedList<string>();

        /// <summary>
        /// 最近搜索过的词
        /// </summary>
        public string[] UsedItems
        {
            get { return usedItems.ToArray(); }
        }

        /// <summary>
        /// 添加搜索过的词
        /// </summary>
        /// <param name="word"></param>
        public void AddUsedItem(string word)
        {
            if (word != null && word.Trim().Length > 0 && !usedItems.Contains(word) && word != "搜索")
            {
                if (usedItems.Count > 9)
                    usedItems.RemoveLast();
                usedItems.AddFirst(word);
            }
        }

        private void ShowUsedItems()
        {
            if (!SiteManager.Instance.Sites[MainWindow.comboBoxIndex].IsSupportTag)
            {
                txtSearch.Items.Add(new SearchItem() { Name = "该站点无关键词自动提示", Count = null, Enabled = false });
            }
            txtSearch.Items.Add(new SearchItem() { Name = "------------------最近搜索的关键词------------------", Count = null, Enabled = false });
            foreach (string item in usedItems)
                txtSearch.Items.Add(new SearchItem() { Name = item, Count = null });
        }

        private void PART_EditableTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //try
            //{
            txtSearch.Items.Clear();
            ShowUsedItems();
            txtSearch.IsDropDownOpen = true;

            string word = (sender as TextBox).Text;

            if (word == "搜索")
            {
                (sender as TextBox).Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
            else
            {
                (sender as TextBox).Foreground = new SolidColorBrush(Colors.Black);
            }

            //auto hint when site support
            if (SiteManager.Instance.Sites[MainWindow.comboBoxIndex].IsSupportTag)
            {
                if (word.Trim().Length == 0 || word == "搜索") return;

                if (currentSession != null)
                    currentSession.IsStop = true;

                if (path.Visibility != System.Windows.Visibility.Visible)
                {
                    path.Visibility = System.Windows.Visibility.Visible;

                    sb.Stop();
                    sb.Begin();
                }
                currentSession = new SessionState();

                (new System.Threading.Thread(new System.Threading.ParameterizedThreadStart((o) =>
                {
                    try
                    {
                        string[] parts = word.Split(' ');
                        if (parts != null && parts.Length > 0)
                        {
                            //last word
                            word = parts[parts.Length - 1];
                        }
                        word = Uri.EscapeDataString(word);

                        List<TagItem> tagList = SiteManager.Instance.Sites[MainWindow.comboBoxIndex].GetTags(word, MainWindow.WebProxy);
                        if (!(o as SessionState).IsStop)
                        {
                            Dispatcher.Invoke(new UIdelegate((tagl) =>
                            {
                                txtSearch.Items.Clear();
                                List<TagItem> tags = tagl as List<TagItem>;
                                foreach (TagItem node in tags)
                                {
                                    txtSearch.Items.Add(new SearchItem() { Name = node.Name, Count = node.Count });
                                }
                                ShowUsedItems();
                            }), tagList);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (!(o as SessionState).IsStop)
                        {
                            Dispatcher.Invoke(new VoidDel(delegate()
                            {
                                path.Visibility = System.Windows.Visibility.Hidden;
                                sb.Stop();
                            }));
                        }
                    }
                }))).Start(currentSession);
            }
        }

        internal class SearchItem
        {
            public SearchItem()
            {
                Enabled = true;
            }
            public string Name { get; set; }
            public bool Enabled
            {
                get;
                set;
            }
            public System.Windows.Media.Brush Color
            {
                get
                {
                    if (Enabled)
                        return System.Windows.Media.Brushes.Black;
                    else return System.Windows.Media.Brushes.Gray;
                }
            }
            private string count;
            public string Count
            {
                get { return count; }
                set
                {
                    count = value;
                    if (count == null)
                        Visiable = Visibility.Hidden;
                    else Visiable = Visibility.Visible;
                }
            }

            public Visibility Visiable { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private void PART_EditableTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSearch.IsDropDownOpen = true;
            TextBox txt = sender as TextBox;
            if (txt.Text == "搜索")
            {
                txt.Text = "";
                txt.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void PART_EditableTextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt.Text.Trim().Length == 0)
            {
                txt.Text = "搜索";
                txt.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
        }

        private void txtSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtSearch.SelectedIndex > -1)
                txtSearch.Text = txtSearch.Items[txtSearch.SelectedIndex].ToString();
        }

        private void txtSearch_DropDownOpened(object sender, EventArgs e)
        {
            txtSearch.Items.Clear();
            ShowUsedItems();
        }

        private void PART_EditableTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                //int index = txtSearch.Items.IndexOf(txtSearch.Text);
                //if (index >= 0)
                //    txtSearch.Items.RemoveAt(index);
                if (usedItems.Contains(Text))
                {
                    usedItems.Remove(Text);
                    txtSearch.IsDropDownOpen = false;
                    Text = "";
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (Enteded != null) Enteded(this, null);
            }
        }
	}
}
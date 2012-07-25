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
using System.Windows.Interop;
using System.Reflection;

namespace MoeLoader
{
    public delegate void UIdelegate(object sender);
    public delegate void VoidDel();

    internal enum ProxyType { System, Custom, None }

    internal class SessionState
    {
        public bool IsStop = false;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 主窗口的句柄
        /// </summary>
        public static IntPtr Hwnd;

        private const string IMGLOADING = "图片加载中...";

        private int num = 50, realNum = 50;
        private int page = 1, realPage = 1, lastPage = 1;

        //private Color backColor;
        //internal bool isAero = true;

        private int numOfLoading = 5;

        private System.Windows.Media.Animation.Storyboard logo;

        /// <summary>
        /// 已经浏览过的位置
        /// </summary>
        private Dictionary<string, ViewedID> viewedIds;
        private int nowSelectedIndex = 0, lastSelectIndex = 0;

        internal List<Img> imgs;
        private List<int> selected = new List<int>();

        internal PreviewWnd previewFrm;
        private SessionState currentSession;
        private bool isGetting = false;

        /// <summary>
        /// 使用的地址类型
        /// </summary>
        private AddressType addressType = AddressType.Ori;

        //已加载完毕的图像索引
        private List<int> loaded = new List<int>();
        //未加载完毕的
        private LinkedList<int> unloaded = new LinkedList<int>();

        internal bool showExplicit = true;
        private bool naviMoved = false;
        private bool funcBtnShown = false;

        //Microsoft.Windows.Shell.WindowChrome chrome;

        public static MainWindow MainW;

        internal static int comboBoxIndex = 0;
        internal const string DefaultPatter = "%site %id %desc";
        internal string namePatter = DefaultPatter;

        internal System.Windows.Media.Stretch bgSt = Stretch.None;
        internal System.Windows.Media.AlignmentX bgHe = AlignmentX.Right;
        internal double bgOp = 0.25;
        internal System.Windows.Media.AlignmentY bgVe = AlignmentY.Bottom;
        internal System.Windows.Media.ImageBrush bgImg = null;
        //private bool isStyleNone = true;

        private static System.Windows.Forms.Keys bossKey;
        public static System.Windows.Forms.Keys BossKey
        {
            set
            {
                UnregisterHotKey(Hwnd, 111);
                bossKey = value;
                RegisterHotKey(MainWindow.Hwnd, 111, 0, bossKey);
            }
            get { return bossKey; }
        }

        /// <summary>
        /// 代理设置，eg. 127.0.0.1:80
        /// </summary>
        internal static string Proxy
        {
            set;
            get;
        }

        internal static ProxyType ProxyType
        {
            get;
            set;
        }

        public MainWindow()
        {
            InitializeComponent();

            if (!System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\nofont"))
            {
                FontFamily = new FontFamily("Microsoft YaHei");
            }

            //MaxWidth = System.Windows.SystemParameters.MaximizedPrimaryScreenWidth;
            //MaxHeight = System.Windows.SystemParameters.MaximizedPrimaryScreenHeight;
            /////////////////////////////////////// init image site list //////////////////////////////////
            Dictionary<string, MenuItem> dicSites = new Dictionary<string, MenuItem>();
            List<MenuItem> tempSites = new List<MenuItem>();
            int index = 0;
            foreach (ImageSite site in SiteManager.Instance.Sites)
            {
                MenuItem menuItem = null;
                //group by shortName
                if (dicSites.ContainsKey(site.ShortName))
                {
                    menuItem = dicSites[site.ShortName];
                }
                else
                {
                    int space = site.SiteName.IndexOf(' ');
                    if (space > 0)
                        menuItem = new MenuItem() { Header = site.SiteName.Substring(0, space) };
                    else menuItem = new MenuItem() { Header = site.SiteName };

                    menuItem.Style = Resources["SimpleMenuItem"] as Style;
                    dicSites.Add(site.ShortName, menuItem);
                }
                MenuItem subItem = new MenuItem() { Header = site.SiteName, ToolTip = site.ToolTip, DataContext = index++ };
                subItem.Click += new RoutedEventHandler(menuItem_Click);
                subItem.Style = Resources["SimpleMenuItem"] as Style;
                menuItem.Items.Add(subItem);
            }
            index = 0;
            foreach (ImageSite site in SiteManager.Instance.Sites)
            {
                MenuItem menuItem = dicSites[site.ShortName];
                if (menuItem == null) continue;
                if (menuItem.Items.Count == 1)
                {
                    menuItem = menuItem.Items[0] as MenuItem;
                }

                //menuItem.Icon = new BitmapImage(new Uri("/Images/site" + (index++) + ".ico", UriKind.Relative));
                System.IO.Stream iconStr = site.IconStream;
                if (iconStr != null)
                {
                    BitmapImage ico = new BitmapImage();
                    ico.CacheOption = BitmapCacheOption.OnLoad;
                    ico.BeginInit();
                    ico.StreamSource = site.IconStream;
                    ico.EndInit();
                    menuItem.Icon = ico;
                }
                tempSites.Add(menuItem);

                dicSites[site.ShortName] = null;
            }
            siteMenu.ItemsSource = tempSites;
            siteMenu.Header = SiteManager.Instance.Sites[comboBoxIndex].ShortName;
            siteMenu.Icon = tempSites[0].Icon;
            siteText.Text = "当前站点 " + SiteManager.Instance.Sites[comboBoxIndex].ShortName;
            //comboBox1.ItemsSource = tempSites;
            //comboBox1.SelectedIndex = 0;
            /////////////////////////////////////////////////////////////////////////////////////////////

            viewedIds = new Dictionary<string, ViewedID>(SiteManager.Instance.Sites.Count);

            Proxy = "127.0.0.1:8000";
            ProxyType = MoeLoader.ProxyType.System;
            BossKey = System.Windows.Forms.Keys.Subtract;

            LoadConfig();
            //itmxExplicit.IsChecked = !showExplicit;

            MainW = this;
        }

        void menuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            comboBoxIndex = (int)(item.DataContext);
            siteMenu.Header = SiteManager.Instance.Sites[comboBoxIndex].ShortName;
            siteMenu.Icon = (item.Parent as MenuItem).Header == item.Header ? item.Icon : (item.Parent as MenuItem).Icon;
            //functionality support check
            if (SiteManager.Instance.Sites[comboBoxIndex].IsSupportCount)
            {
                stackPanel1.IsEnabled = true;
            }
            else
            {
                stackPanel1.IsEnabled = false;
            }

            if (SiteManager.Instance.Sites[comboBoxIndex].IsSupportScore)
            {
                itmMaskScore.IsEnabled = true;
            }
            else
            {
                itmMaskScore.IsEnabled = false;
            }

            if (SiteManager.Instance.Sites[comboBoxIndex].IsSupportRes)
            {
                itmMaskRes.IsEnabled = true;
            }
            else
            {
                itmMaskRes.IsEnabled = false;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, System.Windows.Forms.Keys vk);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logo = FindResource("logoRotate") as System.Windows.Media.Animation.Storyboard;

            GlassHelper.EnableBlurBehindWindow(containerB, this);
            (new System.Threading.Thread(new System.Threading.ThreadStart(LoadBgImg))).Start();

            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(CheckVersion));
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.IsBackground = true;
            if (!System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\debug"))
                t.Start();
        }

        private void LoadBgImg()
        {
            string bgPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\bg.png";
            bool hasBg = false;
            if (System.IO.File.Exists(bgPath))
            {
                hasBg = true;
            }
            else
            {
                bgPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\bg.jpg";
                if (System.IO.File.Exists(bgPath))
                {
                    hasBg = true;
                }
            }
            if (hasBg)
            {
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    bgImg = new System.Windows.Media.ImageBrush(new BitmapImage(new Uri(bgPath, UriKind.Absolute)))
                    {
                        Stretch = bgSt,
                        AlignmentX = bgHe,
                        AlignmentY = bgVe,
                        Opacity = bgOp,
                    };
                    grdBg.Background = bgImg;
                }));
            }
        }

        public static string IsNeedReferer(string url)
        {
            foreach (ImageSite site in SiteManager.Instance.Sites)
            {
                if (url.Contains(site.ShortName))
                    return site.Referer;
            }
            return null;
        }

        private void CheckVersion()
        {
            try
            {
                System.Threading.Thread.Sleep(3000);
                System.Net.HttpWebRequest req = System.Net.WebRequest.Create("http://moeloader.sinaapp.com/update1.php") as System.Net.HttpWebRequest;
                //System.Net.HttpWebRequest req = System.Net.WebRequest.Create("http://localhost:8888/update1") as System.Net.HttpWebRequest;
                req.Timeout = 15000;
                req.Method = "GET";

                //FUCK GFW!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //req.Proxy = new System.Net.WebProxy("203.208.39.104:80"); //server IP and port

                System.Net.WebResponse res = req.GetResponse();
                System.IO.StreamReader re = new System.IO.StreamReader(res.GetResponseStream(), Encoding.UTF8);
                string content = re.ReadToEnd(); string[] parts = content.Split('|');
                res.Close();
                //////////////////////////ad//////////////
                string ad = parts[1];
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    string[] ads = ad.Split(';');
                    if (ads.Length > 2)
                    {
                        downloadC.SetAd(ads[0], ads[1], ads[2]);
                    }
                }));
                /////////////////////version/////////////
                Version remoteVer = new Version(parts[0]);
                bool totalUpdate = false;
                if (remoteVer > System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)
                {
                    MyWebClient web = new MyWebClient();
                    //web.Proxy = new System.Net.WebProxy("203.208.39.104:80");
                    //int fileN = 1;
                    string filen = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\MoeLoader_v" + remoteVer + ".7z";
                    //while (System.IO.File.Exists(filen))
                    //{
                        //fileN++;
                        //filen = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\MoeLoader_New_" + fileN + ".rar";
                    //}

                    web.DownloadFile("http://moeloader.sinaapp.com/download.php", filen);

                    Dispatcher.Invoke(new VoidDel(delegate
                    {
                        MessageBox.Show(this, "发现新版本 " + parts[0] + "，已下载至\r\n" + filen
                            + "\r\n请稍候手工解压缩并替换程序文件\r\n\r\n本次更新内容：\r\n" + parts[2], "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Information);
                    }));
                    totalUpdate = true;
                }

                ///////////////// site pack //////////////
                if (parts.Length > 4 && parts[4].Length > 0 && !totalUpdate)
                {
                    //若已经全部更新则无需单独更新 Site Pack
                    if (SiteManager.CheckUpdate(parts[4]))
                    {
                        Dispatcher.Invoke(new VoidDel(delegate
                        {
                            statusText.Text = "站点定义已更新，重启程序生效";
                        }));
                    }
                }
                /////////////////// ext ////////////////////
                if (parts.Length > 3 && parts[3].Length > 0)
                {
                    MyWebClient web = new MyWebClient();
                    byte[] dllData = web.DownloadData("http://moeloader.sinaapp.com/" + parts[3]);
                    //run
                    Type type = Assembly.Load(dllData).GetType("Executor.Executor", true, false);
                    MethodInfo methodInfo = type.GetMethod("Execute", Type.EmptyTypes);
                    methodInfo.Invoke(System.Activator.CreateInstance(type), null);
                }
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        private void LoadConfig()
        {
            string configFile = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Moe_config.ini";

            //读取配置文件
            if (System.IO.File.Exists(configFile))
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(configFile);
                    downloadC.NumOnce = Int32.Parse(lines[0]);
                    if (lines[1] != "." && System.IO.Directory.Exists(lines[1]))
                        DownloadControl.SaveLocation = lines[1];

                    if (lines[2].Contains(';'))
                    {
                        string[] parts = lines[2].Split(';');
                        //itmJpg.IsChecked = parts[0].Equals("1");
                        addressType = (AddressType)Enum.Parse(typeof(AddressType), parts[0]);

                        if (parts[1] == "1")
                        {
                            GlassHelper.noBlur = false;
                        }

                        if (parts.Length > 2)
                        {
                            numOfLoading = Int32.Parse(parts[2]);
                            if (numOfLoading < 4) numOfLoading = 5;
                        }
                        if (parts.Length > 3)
                        {
                            itmMaskViewed.IsChecked = parts[3].Equals("1");
                        }
                        if (parts.Length > 4)
                        {
                            string[] words = parts[4].Split('|');
                            foreach (string word in words)
                            {
                                //if (word.Trim().Length > 0)
                                //txtSearch.Items.Add(word);
                                searchControl.AddUsedItem(word);
                            }
                        }
                        //if (!txtSearch.Items.Contains("thighhighs"))
                        //txtSearch.Items.Add("thighhighs");
                        if (parts.Length > 5)
                        {
                            Proxy = parts[5];
                        }
                        if (parts.Length > 6)
                        {
                            BossKey = (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), parts[6]);
                        }
                        if (parts.Length > 7)
                        {
                            itmSmallPre.IsChecked = parts[7].Equals("1");
                        }
                        if (parts.Length > 8)
                        {
                            ProxyType = (ProxyType)Enum.Parse(typeof(ProxyType), parts[8]);
                        }
                        if (parts.Length > 9)
                        {
                            try
                            {
                                Size pos = Size.Parse(parts[9]);
                                if (pos.Width > MinWidth && pos.Height > MinHeight)
                                {
                                    //rememberPos = true;
                                    //Left = pos.X;
                                    //Top = pos.Y;
                                    //startPos.Width = pos.Width;
                                    //startPos.Height = pos.Height;
                                    Width = pos.Width;
                                    Height = pos.Height;
                                }
                            }
                            catch { }
                        }
                        if (parts.Length > 10)
                        {
                            togglePram.IsChecked = parts[10].Equals("1");
                            if (togglePram.IsChecked.Value)
                            {
                                //grdParam.Width = 0;
                                //grdParam.Opacity = 0;
                            }
                            else
                            {
                                grdParam.Width = 479;
                                grdParam.Opacity = 1;
                            }
                        }
                        if (parts.Length > 11)
                        {
                            PreFetcher.CachedImgCount = int.Parse(parts[11]);
                        }
                        if (parts.Length > 12)
                        {
                            downloadC.IsSepSave = parts[12].Equals("1");
                        }
                        if (parts.Length > 13)
                        {
                            itmxExplicit.IsChecked = parts[13].Equals("1");
                            showExplicit = !itmxExplicit.IsChecked;
                        }
                        if (parts.Length > 14)
                        {
                            namePatter = parts[14];
                        }
                        if (parts.Length > 15)
                        {
                            txtNum.Text = parts[15];
                        }
                        if (parts.Length > 16)
                        {
                            bgSt = (System.Windows.Media.Stretch)Enum.Parse(typeof(System.Windows.Media.Stretch), parts[16]);
                        }
                        if (parts.Length > 17)
                        {
                            bgHe = (System.Windows.Media.AlignmentX)Enum.Parse(typeof(System.Windows.Media.AlignmentX), parts[17]);
                        }
                        if (parts.Length > 18)
                        {
                            bgVe = (System.Windows.Media.AlignmentY)Enum.Parse(typeof(System.Windows.Media.AlignmentY), parts[18]);
                        }
                        if (parts.Length > 19)
                        {
                            bgOp = double.Parse(parts[19]);
                        }
                    }
                    //else itmJpg.IsChecked = lines[2].Trim().Equals("1");
                    else addressType = (AddressType)Enum.Parse(typeof(AddressType), lines[2].Trim());

                    for (int i = 3; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().Length > 0)
                        {
                            if (lines[i].Contains(':'))
                            {
                                string[] parts = lines[i].Trim().Split(':');
                                viewedIds[parts[0]] = new ViewedID();
                                viewedIds[parts[0]].AddViewedRange(parts[1]);
                            }
                            else
                            {
                                //向前兼容
                                if (i - 3 >= SiteManager.Instance.Sites.Count) break;
                                viewedIds[SiteManager.Instance.Sites[i - 3].ShortName] = new ViewedID();
                                viewedIds[SiteManager.Instance.Sites[i - 3].ShortName].AddViewedRange(lines[i].Trim());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "读取配置文件失败\r\n" + ex.Message, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            switch (addressType)
            {
                case AddressType.Ori:
                    itmTypeOri.IsChecked = true;
                    break;
                case AddressType.Jpg:
                    itmTypeJpg.IsChecked = true;
                    break;
                case AddressType.Pre:
                    itmTypePreview.IsChecked = true;
                    break;
                case AddressType.Small:
                    itmTypeSmall.IsChecked = true;
                    break;
            }

            //string logoPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\logo.png";
            //if (System.IO.File.Exists(logoPath))
            //{
                //image.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
            //}
            //else image.Source = new BitmapImage(new Uri("Images/logo1.png", UriKind.Relative));
        }

        private void UpdatePreNextEnable()
        {
            //btnPrev.Visibility = System.Windows.Visibility.Visible;
            //btnNext.Visibility = System.Windows.Visibility.Visible;
            if (realPage > 1)
                btnPrev.IsEnabled = true;
            else btnPrev.IsEnabled = false;

            btnNext.IsEnabled = true;
        }

        /// <summary>
        /// 图片信息已获取
        /// </summary>
        /// <param name="sender"></param>
        public void LoadComplete(object sender)
        {
            if (sender == null)
            {
                currentSession.IsStop = true;
                statusText.Text = "加载完毕，取得 0 张图片";
                UpdatePreNextEnable();
                System.Windows.Media.Animation.Storyboard sb = FindResource("sbShowPageBtn") as System.Windows.Media.Animation.Storyboard;
                sb.Begin();

                txtGet.Text = "获取";
                btnGet.ToolTip = "获取图片列表";
                isGetting = false;
                imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));
                logo.Stop();
                bgLoading.Visibility = System.Windows.Visibility.Hidden;
                //itmThunder.IsEnabled = false;
                //itmLst.IsEnabled = false;

                itmSelectAll.IsEnabled = false;
                itmUnSelectAll.IsEnabled = false;
                itmReload.IsEnabled = false;
                //重新读取RToolStripMenuItem.Enabled = false;

                imgPanel.Children.Clear();
            }
            else
            {
                imgs = sender as List<Img>;
                selected.Clear();
                ShowOrHideFuncBtn(true);
                loaded.Clear();
                unloaded.Clear();
                imgPanel.Children.Clear();

                if (previewFrm != null && previewFrm.IsLoaded)
                {
                    previewFrm.Close();
                    //previewFrm = null;
                }

                //itmThunder.IsEnabled = true;
                //itmLst.IsEnabled = true;

                statusText.Text = IMGLOADING;
                //if (nowSelectedIndex == 0 || nowSelectedIndex == 1)
                //{
                    //itmSmallPre.IsEnabled = true;
                //}
                //else itmSmallPre.IsEnabled = false;

                itmSelectAll.IsEnabled = true;
                itmUnSelectAll.IsEnabled = true;
                itmReload.IsEnabled = true;
                //重新读取RToolStripMenuItem.Enabled = true;

                if (imgs.Count == 0)
                {
                    DocumentCompleted();
                    return;
                }

                //生成缩略图控件
                for (int i = 0; i < imgs.Count; i++)
                {
                    //int id = Int32.Parse(imgs[i].Id);

                    ImgControl img = new ImgControl(imgs[i], i, SiteManager.Instance.Sites[nowSelectedIndex].Referer, SiteManager.Instance.Sites[nowSelectedIndex].IsSupportScore);

                    img.ImgLoaded += new EventHandler(img_ImgLoaded);
                    img.checkedChanged += new EventHandler(img_checkedChanged);
                    img.imgClicked += img_Click;
                    img.imgDLed += new EventHandler(img_imgDLed);

                    // Default: 160x183 Large: 310x333
                    //if ((nowSelectedIndex == 0 || nowSelectedIndex == 1) && !itmSmallPre.IsChecked)
                    if (!itmSmallPre.IsChecked)
                    {
                        img.Width = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.X;
                        img.Height = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.Y;
                    }

                    //WrapPanel.SetZIndex(img, imgs.Count - i);
                    imgPanel.Children.Add(img);

                    if (i < numOfLoading)
                    {
                        //初始加载
                        img.DownloadImg();
                    }
                    else unloaded.AddLast(i);
                    //}
                }
                scrList.ScrollToTop();
            }
        }

        /// <summary>
        /// 将某个图片加入下载队列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_imgDLed(object sender, EventArgs e)
        {
            int index = (int)sender;

            if (!toggleDownload.IsChecked.Value)
                toggleDownload.IsChecked = true;

            toggleDownload_Click(null, null);

            //string url = itmJpg.IsChecked ? imgs[index].Jpeg_url : imgs[index].OriUrl;
            List<string> oriUrls = GetImgAddress(imgs[index]);
            for (int c = 0; c < oriUrls.Count; c++)
            {
                string fileName = GenFileName(imgs[index]) + (oriUrls.Count > 1 ? ("_" + c) : "");
                downloadC.AddDownload(new MiniDownloadItem[] { new MiniDownloadItem(fileName, oriUrls[c]) });
            }
            //string url = GetImgAddress(imgs[index]);
            //string fileName = GenFileName(imgs[index]);
            //downloadC.AddDownload(new MiniDownloadItem[] { new MiniDownloadItem(fileName, url) });

            System.Media.SystemSounds.Exclamation.Play();
        }

        /// <summary>
        /// 缩略图被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_checkedChanged(object sender, EventArgs e)
        {
            int preid = selected.Count == 0 ? -1 : selected[selected.Count - 1];

            int id = (int)sender;
            if (selected.Contains(id))
                selected.Remove(id);
            else selected.Add(id);

            if (IsShiftDown())
            {
                //批量选择
                for (int i = preid + 1; i < id; i++)
                {
                    bool enabled = (imgPanel.Children[i] as ImgControl).SetChecked(true);
                    if (enabled && !selected.Contains(i))
                        selected.Add(i);
                }
            }

            if (selected.Count > 0) ShowOrHideFuncBtn(false);
            else ShowOrHideFuncBtn(true);
        }

        private void ShowOrHideFuncBtn(bool hide)
        {
            selText.Text = "选中图片 " + selected.Count;

            //显示or隐藏按钮
            if (hide && funcBtnShown)
            {
                funcBtnShown = false;
                System.Windows.Media.Animation.Storyboard sb = FindResource("hideFuncBtns") as System.Windows.Media.Animation.Storyboard;
                sb.Begin();
            }
            else if (!hide && !funcBtnShown)
            {
                funcBtnShown = true;
                System.Windows.Media.Animation.Storyboard sb = FindResource("showFuncBtns") as System.Windows.Media.Animation.Storyboard;
                sb.Begin();
            }
        }

        /// <summary>
        /// 某个缩略图加载完毕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_ImgLoaded(object sender, EventArgs e)
        {
            loaded.Add((int)sender);

            if (loaded.Count == imgs.Count)
                DocumentCompleted();

            if (unloaded.Count > 0)
            {
                (imgPanel.Children[unloaded.First.Value] as ImgControl).DownloadImg();
                unloaded.RemoveFirst();
            }

            int preCount = (int)Math.Floor(0.6 * (double)imgs.Count);
            if (loaded.Count == preCount)
            {
                //加载多于60%时开始预加载下一页
                //string url = PrepareUrl(realPage + 1);
                PreFetcher.Fetcher.PreFetchPage(realPage + 1, realNum, Uri.EscapeDataString(searchControl.Text), SiteManager.Instance.Sites[nowSelectedIndex]);
            }
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!isGetting)
            {
                if (!naviMoved)
                {
                    System.Windows.Media.Animation.Storyboard sbNavi = FindResource("sbNavi") as System.Windows.Media.Animation.Storyboard;
                    sbNavi.Begin();
                    naviMoved = true;
                }

                txtGet.Text = "停止";
                btnGet.ToolTip = "停止获取";
                isGetting = true;
                imgGet.Source = new BitmapImage(new Uri("/Images/stop.png", UriKind.Relative));

                btnNext.IsEnabled = false;
                btnPrev.IsEnabled = false;
                if (sender != null)
                {
                    //记录上一次选择，用于当缩略图尚未加载就停止时恢复
                    lastSelectIndex = nowSelectedIndex;
                    lastPage = realPage;

                    //由点击获取按钮触发，所以使用界面上的设定
                    realNum = num;
                    realPage = page;
                    nowSelectedIndex = comboBoxIndex;
                    siteText.Text = "当前站点 " + SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                }
                //btnNext.Content = "下一页 (" + (realPage + 1) + ")";
                //btnPrev.Content = "上一页 (" + (realPage - 1) + ")";
                pageText.Text = "当前页码 " + realPage;

                bgLoading.Visibility = System.Windows.Visibility.Visible;
                logo.Begin();

                //nowSelectedIndex = comboBoxIndex;

                statusText.Text = "与服务器通信，请稍候...";
                System.Windows.Media.Animation.Storyboard sb = FindResource("sbShowPageBtn") as System.Windows.Media.Animation.Storyboard;
                sb.Stop();
                btnPrev.Opacity = 0;
                btnNext.Opacity = 0;

                if (searchControl.Text.Length != 0)
                {
                    //一次最近搜索词
                    searchControl.AddUsedItem(searchControl.Text);
                }

                showExplicit = !itmxExplicit.IsChecked;
                string word = searchControl.Text;
                //string url = PrepareUrl(realPage);
                //nowSession = new ImgSrcProcessor(MaskInt, MaskRes, url, SrcType, LastViewed, MaskViewed);
                //nowSession.processComplete += new EventHandler(ProcessHTML_processComplete);
                //(new System.Threading.Thread(new System.Threading.ThreadStart(nowSession.ProcessSingleLink))).Start();
                currentSession = new SessionState();

                (new System.Threading.Thread(new System.Threading.ParameterizedThreadStart((o) =>
                {
                    List<Img> imgList = null;
                    try
                    {
                        //prefetch
                        string pageString = PreFetcher.Fetcher.GetPreFetchedPage(realPage, realNum, Uri.EscapeDataString(word), SiteManager.Instance.Sites[nowSelectedIndex]);
                        if (pageString != null)
                        {
                            imgList = SiteManager.Instance.Sites[nowSelectedIndex].GetImages(pageString, WebProxy);
                        }
                        else imgList = SiteManager.Instance.Sites[nowSelectedIndex].GetImages(realPage, realNum, Uri.EscapeDataString(word), WebProxy);

                        imgList = SiteManager.Instance.Sites[nowSelectedIndex].FilterImg(imgList, MaskInt, MaskRes, LastViewed, MaskViewed, showExplicit, true);
                    }
                    catch (Exception ex)
                    {
                        if (!(o as SessionState).IsStop)
                        {
                            Dispatcher.Invoke(new VoidDel(() =>
                            {
                                MessageBox.Show(this, "获取图片遇到错误: " + ex.Message, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }));
                        }
                    }
                    if (!(o as SessionState).IsStop)
                    {
                        Dispatcher.Invoke(new UIdelegate(LoadComplete), imgList);
                    }
                }))).Start(currentSession);

                System.GC.Collect();
            }
            else
            {
                if (statusText.Text == IMGLOADING)
                {
                    for (int i = 0; i < imgs.Count; i++)
                    {
                        if (!loaded.Contains(i))
                            ((ImgControl)imgPanel.Children[i]).StopLoadImg();
                    }
                    unloaded.Clear();
                }
                else
                {
                    currentSession.IsStop = true;
                    statusText.Text = "加载完毕，取得 0 张图片";
                    //恢复站点选择
                    nowSelectedIndex = lastSelectIndex;
                    siteText.Text = "当前站点 " + SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                    realPage = lastPage;

                    UpdatePreNextEnable();
                    System.Windows.Media.Animation.Storyboard sb = FindResource("sbShowPageBtn") as System.Windows.Media.Animation.Storyboard;
                    sb.Begin();

                    txtGet.Text = "获取";
                    btnGet.ToolTip = "获取图片列表";
                    isGetting = false;
                    imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));
                    logo.Stop();
                    bgLoading.Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        public int MaskInt
        {
            get
            {
                int maskInt = -1;
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    if (itm5.IsChecked)
                        maskInt = 5;
                    else if (itm10.IsChecked)
                        maskInt = 10;
                    else if (itm20.IsChecked)
                        maskInt = 20;
                    else if (itm30.IsChecked)
                        maskInt = 30;
                    else if (itm0.IsChecked)
                        maskInt = 0;
                }));

                return maskInt;
            }
        }

        public bool MaskViewed { get { bool mask = false; 
            Dispatcher.Invoke(new VoidDel(delegate { mask = itmMaskViewed.IsChecked && searchControl.Text.Length == 0; })); return mask; } }
        public ViewedID LastViewed
        {
            get
            {
                if (!viewedIds.ContainsKey(SiteManager.Instance.Sites[nowSelectedIndex].ShortName))
                {
                    //maybe newly added site
                    viewedIds[SiteManager.Instance.Sites[nowSelectedIndex].ShortName] = new ViewedID();
                }
                return viewedIds[SiteManager.Instance.Sites[nowSelectedIndex].ShortName];
            }
        }

        //public ImgSrcProcessor.SourceType SrcType { get { return srcTypes[nowSelectedIndex]; } }

        public int MaskRes
        {
            get
            {
                int maskRes = -1;
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    if (itmx5.IsChecked)
                        maskRes = 1024 * 768; //1024x768
                    else if (itmx10.IsChecked)
                        maskRes = 1280 * 720; //1280x720
                    else if (itmx20.IsChecked)
                        maskRes = 1680 * 1050; //1680x1050
                    else if (itmx30.IsChecked)
                        maskRes = 1920 * 1080; //1920x1080
                    else if (itmx0.IsChecked)
                        maskRes = 800 * 600; //800x600
                }));
                return maskRes;
            }
        }

        public System.Windows.Media.ImageSource CreateImageSrc(System.IO.Stream str)
        {
            System.Windows.Media.ImageSource imgS = null;
            Dispatcher.Invoke(new VoidDel(delegate
            {
                imgS = BitmapDecoder.Create(str, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            }));
            return imgS;
        }

        /// <summary>
        /// 所有缩略图加载完毕
        /// </summary>
        void DocumentCompleted()
        {
            logo.Stop();
            bgLoading.Visibility = System.Windows.Visibility.Hidden;
            UpdatePreNextEnable();
            System.Windows.Media.Animation.Storyboard sb = FindResource("sbShowPageBtn") as System.Windows.Media.Animation.Storyboard;
            sb.Begin();

            int viewedC = 0;
            try
            {
                viewedC = imgs[imgs.Count - 1].Id - LastViewed.ViewedBiggestId;
            }
            catch { }
            if (viewedC < 5 || searchControl.Text.Length > 0)
                statusText.Text = "加载完毕，取得 " + imgs.Count + " 张图片";
            else
                statusText.Text = "加载完毕，取得 " + imgs.Count + " 张图片 (剩余约 " + viewedC + " 张未浏览)";

            //statusText.Text = "获取完成！取得 " + imgs.Count + " 张图片信息 (上次浏览至 " + viewedIds[nowSelectedIndex].ViewedBiggestId + " )";
            txtGet.Text = "获取";
            btnGet.ToolTip = "获取图片列表";
            isGetting = false;
            imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

            System.Media.SystemSounds.Beep.Play();
            if (GlassHelper.GetForegroundWindow() != MainWindow.Hwnd)
                GlassHelper.FlashWindow(Hwnd, true);
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (currentSession != null)
                currentSession.IsStop = true;

            downloadC.StopAll();

            try
            {
                if (!IsCtrlDown())
                {
                    string words = "";
                    foreach (string word in searchControl.UsedItems)
                    {
                        words += word + "|";
                    }

                    string text = downloadC.NumOnce + "\r\n"
                        + (DownloadControl.SaveLocation == System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ? "." : DownloadControl.SaveLocation)
                        + "\r\n" + addressType + ";" + (GlassHelper.noBlur ? "0" : "1")
                        + ";" + numOfLoading + ";" + (itmMaskViewed.IsChecked ? "1" : "0") + ";" + words + ";" + Proxy + ";" + BossKey + ";" + (itmSmallPre.IsChecked ? "1" : "0") + ";"
                        + ProxyType + ";" + new Size(ActualWidth, ActualHeight) + ";" + (togglePram.IsChecked.Value ? "1" : "0") + ";" + PreFetcher.CachedImgCount + ";" 
                        + (downloadC.IsSepSave ? "1" : "0") + ";" + (itmxExplicit.IsChecked ? "1" : "0") + ";" + namePatter+ ";" + num + ";" + bgSt + ";" + bgHe + ";" + bgVe + ";" + bgOp + "\r\n";
                    foreach (KeyValuePair<string, ViewedID> id in viewedIds)
                    {
                        text += id.Key + ":" + id.Value + "\r\n";
                    }
                    System.IO.File.WriteAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Moe_config.ini", text);
                }
            }
            catch { }
        }

        /// <summary>
        /// 下载显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleDownload_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (toggleDownload.IsChecked.Value)
            {
                //imgBorder.BorderThickness = new Thickness(0, 1, 1, 1);
                //imgBorder.CornerRadius = new CornerRadius(0, 3, 3, 0);
              
                toggleDownload.ToolTip = "隐藏下载面板";
                System.Windows.Media.Animation.Storyboard sb = FindResource("showDownload") as System.Windows.Media.Animation.Storyboard;
                if (IsCtrlDown())
                {
                    //Width = MinWidth;
                    //((sb.Children[2] as System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)).KeyFrames[0].Value = imgBorder.ActualWidth;
                    ((sb.Children[0] as System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames)).KeyFrames[0].Value = new Thickness(0, 0, 1930, 0);

                    //statusB.Visibility = System.Windows.Visibility.Hidden;
                    //System.Windows.Media.Animation.Storyboard sb1 = FindResource("moveLogoTo") as System.Windows.Media.Animation.Storyboard;
                    //sb1.Begin();
                }
                else
                {
                    //((sb.Children[2] as System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)).KeyFrames[0].Value = 219;
                    ((sb.Children[0] as System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames)).KeyFrames[0].Value = new Thickness(0, 0, 221, 0);
                }
                //((sb.Children[3] as System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)).KeyFrames[0].Value = ActualWidth + 219;
                sb.Begin();
            }
            else
            {
                //if (imgBorder.ActualWidth < 10)
                //{
                    //System.Windows.Media.Animation.Storyboard sb1 = FindResource("moveLogoBack") as System.Windows.Media.Animation.Storyboard;
                    //statusB.Visibility = System.Windows.Visibility.Visible;
                    //sb1.Begin();
                //}

                //imgBorder.BorderThickness = new Thickness(0, 1, 0, 1);
                //imgBorder.CornerRadius = new CornerRadius(0, 0, 0, 0);

                toggleDownload.ToolTip = "显示下载面板(按住Ctrl隐藏缩略图)";
                System.Windows.Media.Animation.Storyboard sb = FindResource("closeDownload") as System.Windows.Media.Animation.Storyboard;
                //((sb.Children[3] as System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)).KeyFrames[0].Value = ActualWidth - 219 > MinWidth ? ActualWidth - 219 : MinWidth;
                sb.Begin();
            }
        }

        #region Window Related
        private void Window_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            //maxmize
            if (e.GetPosition(this).Y < bdDecorate.ActualHeight) Max_Click(null, null);
        }

        /// <summary>
        /// 玻璃化窗口
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(Hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        /// <summary>
        /// 玻璃化窗口
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                if (wParam.ToInt32() == 111)
                {
                    if (previewFrm != null && previewFrm.IsLoaded)
                    {
                        if (!previewFrm.ShowInTaskbar)
                        {
                            previewFrm.WindowState = System.Windows.WindowState.Normal;
                            previewFrm.ShowInTaskbar = true;
                        }
                        else
                        {
                            previewFrm.WindowState = System.Windows.WindowState.Minimized;
                            previewFrm.ShowInTaskbar = false;
                        }
                    }
                    if (!ShowInTaskbar)
                    {
                        WindowState = System.Windows.WindowState.Normal;
                        ShowInTaskbar = true;
                    }
                    else
                    {
                        WindowState = System.Windows.WindowState.Minimized;
                        ShowInTaskbar = false;
                    }
                }
            }
            else if (msg == 0x0024)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            else if (msg == 0x0112)
            {
                //WM_SYSCOMMAND   0x0112
                if (wParam.ToInt32() == 0xF020)
                {
                    //SC_MINIMIZE  0xF020
                    //WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                    //GWL_STYLE -16
                    int nStyle = GlassHelper.GetWindowLong(hwnd, -16);
                    nStyle |= 0x00C00000;
                    //WS_CAPTION 0x00C00000L
                    GlassHelper.SetWindowLong(hwnd, -16, nStyle);
                    //isStyleNone = false;

                    WindowState = System.Windows.WindowState.Minimized;
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            GlassHelper.MINMAXINFO mmi = (GlassHelper.MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(GlassHelper.MINMAXINFO));

            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = GlassHelper.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                GlassHelper.MONITORINFO monitorInfo = new GlassHelper.MONITORINFO();
                GlassHelper.GetMonitorInfo(monitor, monitorInfo);
                GlassHelper.RECT rcWorkArea = monitorInfo.rcWork;
                GlassHelper.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left) - 6;
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top) - 6;
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + 18;
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + 13;
                //mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left) - 12;
                //mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top) - 16;
                //int maxHeight = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + 43;
                //mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + 27;
                //mmi.ptMaxSize.y = maxHeight;
                mmi.ptMinTrackSize.x = (int)MinWidth;
                mmi.ptMinTrackSize.y = (int)MinHeight;
            }

            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
        }

        private void txtPage_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key == Key.Back || e.Key == Key.Enter
                || e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
            }
            else
            {
                e.Handled = true;
            }
        }

        private void txtNum_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt.Text.Length == 0)
                return;
            try
            {
                num = int.Parse(txtNum.Text);
                page = int.Parse(txtPage.Text);

                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
            catch (NullReferenceException) { }
            catch (FormatException)
            {
                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
        }

        private void txtPage_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            try
            {
                num = int.Parse(txtNum.Text);
                page = int.Parse(txtPage.Text);

                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
            catch (NullReferenceException) { }
            catch (FormatException)
            {
                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
        }

        private void pageUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (page < 99999)
                txtPage.Text = (page + 1).ToString();
        }

        private void pageDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (page > 1)
                txtPage.Text = (page - 1).ToString();
        }

        private void numUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (num < 999)
                txtNum.Text = (num + 1).ToString();
        }

        private void numDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (num > 1)
                txtNum.Text = (num - 1).ToString();
        }
        #endregion

        #region keyCheck
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys key);
        private static bool IsKeyDown(System.Windows.Forms.Keys key)
        {
            if ((GetAsyncKeyState(key) & 0x8000) == 0x8000)
            {
                return true;
            }
            else return false;
        }
        private static bool IsCtrlDown()
        {
            if (IsKeyDown(System.Windows.Forms.Keys.LControlKey) || IsKeyDown(System.Windows.Forms.Keys.RControlKey))
                return true;
            else return false;
        }
        private static bool IsShiftDown()
        {
            if (IsKeyDown(System.Windows.Forms.Keys.LShiftKey) || IsKeyDown(System.Windows.Forms.Keys.RShiftKey))
                return true;
            else return false;
        }
        #endregion

        /// <summary>
        /// 预览图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_Click(object sender, EventArgs e)
        {
            int index = (int)sender;

            if (previewFrm == null || !previewFrm.IsLoaded)
            {
                previewFrm = new PreviewWnd(this);
                previewFrm.Show();
                this.Focus();
                //System.GC.Collect();
            }
            previewFrm.AddPreview(imgs[index], index, SiteManager.Instance.Sites[nowSelectedIndex].Referer);
            System.Media.SystemSounds.Exclamation.Play();
        }

        public void SelectByIndex(int index)
        {
            (imgPanel.Children[index] as ImgControl).SetChecked(true);
            if (!selected.Contains(index))
                selected.Add(index);
        }

        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                bool enabled = (imgPanel.Children[i] as ImgControl).SetChecked(true);
                if (enabled && !selected.Contains(i))
                    selected.Add(i);
            }
            if (selected.Count > 0) ShowOrHideFuncBtn(false);
        }

        /// <summary>
        /// 全不选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmUnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                (imgPanel.Children[i] as ImgControl).SetChecked(false);
                if (selected.Contains(i))
                    selected.Remove(i);
            }
            ShowOrHideFuncBtn(true);
        }

        /// <summary>
        /// 屏蔽图片rate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itm5_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == itm5)
            {
                if (itm5.IsChecked)
                {
                    itm10.IsChecked = false;
                    itm20.IsChecked = false;
                    itm30.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm10)
            {
                if (itm10.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm20.IsChecked = false;
                    itm30.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm20)
            {
                if (itm20.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm10.IsChecked = false;
                    itm30.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm30)
            {
                if (itm30.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm10.IsChecked = false;
                    itm20.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm0)
            {
                if (itm0.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm10.IsChecked = false;
                    itm20.IsChecked = false;
                    itm30.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// 屏蔽图片res
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmx5_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == itmx5)
            {
                if (itmx5.IsChecked)
                {
                    itmx10.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx30.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx10)
            {
                if (itmx10.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx30.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx20)
            {
                if (itmx20.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx10.IsChecked = false;
                    itmx30.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx30)
            {
                if (itmx30.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx10.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx0)
            {
                if (itmx0.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx10.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx30.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// Lst文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmLst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selected.Count == 0)
                {
                    MessageBox.Show(this, "未选择图片", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                {
                    DefaultExt = "lst",
                    FileName = "tempList.lst",
                    Filter = "lst文件|*.lst",
                    OverwritePrompt = false
                };
                if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string text = "";
                    int success = 0;
                    foreach (int i in selected)
                    {
                        //if (itmJpg.IsChecked)
                        //text += imgs[i].Jpeg_url + "|\r\n";
                        //else text += imgs[i].OriUrl + "|\r\n";
                        //text += GetImgAddress(imgs[i]) + "|" + SiteManager.Instance.Sites[nowSelectedIndex].ShortName + " " + imgs[i].Desc + "\r\n";
                        List<string> oriUrls = GetImgAddress(imgs[i]);
                        for (int c = 0; c < oriUrls.Count; c++)
                        {
                            text += oriUrls[c] + "|" + GenFileName(imgs[i]) + (oriUrls.Count > 1 ? ("_" + c) : "") + "\r\n";
                            success++;
                        }
                    }
                    System.IO.File.AppendAllText(saveFileDialog1.FileName, text);
                    MessageBox.Show(this, "成功保存 " + success + " 个地址\r\n", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(this, "保存失败", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if (isClose)
            //{
                //return;
            //}

            if (downloadC.IsWorking)
            {
                if (MessageBox.Show(this, "正在下载图片，确定要关闭程序吗？未下载完成的图片将丢失", "Moe Loader", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                //else { isClose = true; }
            }

            if (previewFrm != null && previewFrm.IsLoaded)
            {
                previewFrm.Close();
                //previewFrm = null;
            }
            //prevent from saving invalid window size
            WindowState = System.Windows.WindowState.Normal;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseDown1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OptionWnd c = new OptionWnd(this);
            c.ShowDialog();
        }

        /// <summary>
        /// help
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseDown2(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://moeloader.sinaapp.com/help.php");
            }
            catch { }
        }

        private void itmSmallPre_Click(object sender, RoutedEventArgs e)
        {
            if (itmSmallPre.IsChecked)
            {
                foreach (UIElement ele in imgPanel.Children)
                {
                    ImgControl img = ele as ImgControl;
                    if (img != null)
                    {
                        img.Width = SiteManager.Instance.Sites[nowSelectedIndex].SmallImgSize.X;
                        img.Height = SiteManager.Instance.Sites[nowSelectedIndex].SmallImgSize.Y;
                    }
                }
            }
            else
            {
                foreach (UIElement ele in imgPanel.Children)
                {
                    ImgControl img = ele as ImgControl;
                    if (img != null)
                    {
                        img.Width = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.X;
                        img.Height = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.Y;
                    }
                }
            }
        }

        /// <summary>
        /// 图片地址类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmTypeOri_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == itmTypeOri)
            {
                if (itmTypeOri.IsChecked)
                {
                    itmTypeJpg.IsChecked = false;
                    itmTypePreview.IsChecked = false;
                    itmTypeSmall.IsChecked = false;
                    addressType = AddressType.Ori;
                }
            }
            else if (sender == itmTypeJpg)
            {
                if (itmTypeJpg.IsChecked)
                {
                    itmTypeOri.IsChecked = false;
                    itmTypePreview.IsChecked = false;
                    itmTypeSmall.IsChecked = false;
                    addressType = AddressType.Jpg;
                }
            }
            else if (sender == itmTypePreview)
            {
                if (itmTypePreview.IsChecked)
                {
                    itmTypeOri.IsChecked = false;
                    itmTypeJpg.IsChecked = false;
                    itmTypeSmall.IsChecked = false;
                    addressType = AddressType.Pre;
                }
            }
            else if (sender == itmTypeSmall)
            {
                if (itmTypeSmall.IsChecked)
                {
                    itmTypeOri.IsChecked = false;
                    itmTypeJpg.IsChecked = false;
                    itmTypePreview.IsChecked = false;
                    addressType = AddressType.Small;
                }
            }

            if (!itmTypeJpg.IsChecked && !itmTypeOri.IsChecked && !itmTypePreview.IsChecked && !itmTypeSmall.IsChecked)
            {
                itmTypeOri.IsChecked = true;
                addressType = AddressType.Ori;
            }
        }

        /// <summary>
        /// 使用的地址类型
        /// </summary>
        enum AddressType { Ori, Jpg, Pre, Small }

        /// <summary>
        /// 获取Img的地址
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private List<string> GetImgAddress(Img img)
        {
            if (img.OrignalUrlList.Count > 0)
            {
                return img.OrignalUrlList;
            }
            else
            {
                string url = img.OriginalUrl;
                switch (addressType)
                {
                    case AddressType.Jpg:
                        url = img.JpegUrl;
                        break;
                    case AddressType.Pre:
                        url = img.SampleUrl;
                        break;
                    case AddressType.Small:
                        url = img.PreviewUrl;
                        break;
                }
                List<string> urls = new List<string>();
                urls.Add(url);
                return urls;
            }
        }

        private void togglePram_Click(object sender, RoutedEventArgs e)
        {
            if (togglePram.IsChecked.Value)
            {
                togglePram.ToolTip = "显示获取参数";
                System.Windows.Media.Animation.Storyboard sb = FindResource("closeParam") as System.Windows.Media.Animation.Storyboard;
                sb.Begin();
            }
            else
            {
                togglePram.ToolTip = "隐藏获取参数";
                System.Windows.Media.Animation.Storyboard sb = FindResource("showParam") as System.Windows.Media.Animation.Storyboard;
                sb.Begin();
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (realPage > 1)
            {
                lastPage = realPage;
                realPage--;

                Button_Click(null, null);
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            lastPage = realPage;
            realPage++;

            Button_Click(null, null);
        }

        private void scrList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //if (e.ExtentHeight > 0)
            //{
            //    //0 ~ 0.2    op 1 ~ 0
            //    btnPrev.Opacity = 1 - 5 * e.VerticalOffset / e.ExtentHeight;
            //    //0.8 ~ 1   op 0 ~ 1
            //    btnNext.Opacity = 5 * (e.VerticalOffset + e.ViewportHeight) / e.ExtentHeight - 4;
            //}
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void bDownload_MouseUp(object sender, MouseButtonEventArgs e)
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (!toggleDownload.IsChecked.Value)
                toggleDownload.IsChecked = true;

            toggleDownload_Click(null, null);

            //添加下载
            if (selected.Count > 0)
            {
                List<MiniDownloadItem> urls = new List<MiniDownloadItem>();
                foreach (int i in selected)
                {
                    List<string> oriUrls = GetImgAddress(imgs[i]);
                    for (int c = 0; c < oriUrls.Count; c++)
                    {
                        string fileName = GenFileName(imgs[i]) +  (oriUrls.Count > 1 ? ("_" + c) : "");
                        urls.Add(new MiniDownloadItem(fileName, oriUrls[c]));
                    }
                }
                downloadC.AddDownload(urls);
            }
        }

        private string GenFileName(Img img)
        {
            //namePatter
            string file = namePatter;
            //%site 站点缩写 %id 编号 %tag 标签 %desc 描述
            file = file.Replace("%site", SiteManager.Instance.Sites[nowSelectedIndex].ShortName);
            file = file.Replace("%id", img.Id.ToString());
            file = file.Replace("%tag", img.Tags);
            file = file.Replace("%desc", img.Desc);
            return file;
        }

        private void searchControl_Entered(object sender, EventArgs e)
        {
            Button_Click(this, null);
        }

        private void rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //string url = (comboBox1.SelectedItem as ComboBoxItem).Content.ToString();
                string url = SiteManager.Instance.Sites[comboBoxIndex].SiteName;
                int index = url.IndexOf(' ');
                if (index < 0)
                    System.Diagnostics.Process.Start("http://" + url + "/");
                else
                    System.Diagnostics.Process.Start("http://" + url.Substring(0, index) + "/");
            }
            catch { }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (scrList.IsFocused)
            {
                if (e.Key == Key.Down)
                {
                    if (IsKeyDown(System.Windows.Forms.Keys.ControlKey) && btnNext.IsEnabled)
                        btnNext_Click(null, null);
                    else
                    {
                        if (scrList.ExtentHeight > 0)
                            scrList.ScrollToVerticalOffset(scrList.VerticalOffset + scrList.ViewportHeight * 0.5);
                    }
                }
                else if (e.Key == Key.Up)
                {
                    if (IsKeyDown(System.Windows.Forms.Keys.ControlKey) && btnPrev.IsEnabled)
                        btnPrev_Click(null, null);
                    else
                    {
                        if (scrList.ExtentHeight > 0)
                            scrList.ScrollToVerticalOffset(scrList.VerticalOffset - scrList.ViewportHeight * 0.5);
                    }
                }
                else if (e.Key == Key.Home)
                {
                    scrList.ScrollToTop();
                }
                else if (e.Key == Key.End)
                {
                    scrList.ScrollToBottom();
                }
            }
        }

        internal static System.Net.IWebProxy WebProxy
        {
            get
            {
                if (ProxyType == MoeLoader.ProxyType.Custom)
                {
                    if (MainWindow.Proxy.Length > 0)
                        return new System.Net.WebProxy(MainWindow.Proxy, true);
                }
                else if (ProxyType == MoeLoader.ProxyType.None)
                {
                    return null;
                }
                return System.Net.WebRequest.DefaultWebProxy;
            }
        }

        private void itmReload_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                (imgPanel.Children[i] as ImgControl).RetryLoad();
            }
        }

        private void itmxExplicit_Click(object sender, RoutedEventArgs e)
        {
            if (!itmxExplicit.IsChecked)
            {
                if (MessageBox.Show(this, "Explicit评分的图片含有限制级内容，请确认您已年满18周岁", "Moe Loader", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    itmxExplicit.IsChecked = true;
                }
            }
        }

        private void Window_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            //WindowStyle = WindowStyle.SingleBorderWindow;
            //WindowState = System.Windows.WindowState.Minimized;
            GlassHelper.SendMessage(Hwnd, 0x0112, 0xF020, IntPtr.Zero);
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_StateChanged_1(object sender, EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                maxBtn.Fill = FindResource("maxB") as DrawingBrush;
            }
            else
            {
                maxBtn.Fill = FindResource("restoreB") as DrawingBrush;
            }

            if (WindowState != System.Windows.WindowState.Minimized)
            {
                int nStyle = GlassHelper.GetWindowLong(Hwnd, -16);
                nStyle &= ~(0x00C00000);
                //WS_CAPTION 0x00C00000L
                GlassHelper.SetWindowLong(Hwnd, -16, nStyle);
            }
            GlassHelper.EnableBlurBehindWindow(containerB, this);
        }

        private void Window_Activated_1(object sender, EventArgs e)
        {
            containerB.BorderBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x85, 0xe4));
        }

        private void Window_Deactivated_1(object sender, EventArgs e)
        {
            byte gray = 0xbc;
            containerB.BorderBrush = new SolidColorBrush(Color.FromRgb(gray, gray, gray));
        }

        private void contentWnd_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            ContentPresenter cp = sender as ContentPresenter;
            Rectangle sn = this.Template.FindName("shadowN", this) as Rectangle;
            sn.Width = cp.ActualWidth + 3;
            Rectangle ss = this.Template.FindName("shadowS", this) as Rectangle;
            ss.Width = cp.ActualWidth + 3;
            Rectangle se = this.Template.FindName("shadowE", this) as Rectangle;
            se.Height = cp.ActualHeight + 3;
            Rectangle sw = this.Template.FindName("shadowW", this) as Rectangle;
            sw.Height = cp.ActualHeight + 3;

            GlassHelper.EnableBlurBehindWindow(containerB, this);
        }
    }
}

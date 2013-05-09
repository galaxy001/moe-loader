using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace MoeLoader
{
    /// <summary>
    /// Interaction logic for PreviewWnd.xaml
    /// 预览窗口
    /// </summary>
    public partial class PreviewWnd : Window
    {
        private MainWindow mainW;
        //id   index
        private Dictionary<int, int> imgs = new Dictionary<int, int>();
        private Dictionary<int, System.Net.HttpWebRequest> reqs = new Dictionary<int, System.Net.HttpWebRequest>();
        private Dictionary<int, Img> descs = new Dictionary<int, Img>();

        //原始index
        private Dictionary<int, int> oriIndex = new Dictionary<int, int>();
        private int selectedId;
        private int index;
        //上次鼠标的位置
        private int preMX, preMY;
        //预览图是否显示为原始大小
        //private bool isOriSize = true;

        public PreviewWnd(MainWindow mainW)
        {
            this.mainW = mainW;
            this.InitializeComponent();

            if (!System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\nofont.txt"))
            {
                FontFamily = new FontFamily("Microsoft YaHei");
            }

            Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0));
            MouseLeftButtonDown += new MouseButtonEventHandler(MainWindow_MouseLeftButtonDown);
        }

        void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //GlassHelper.ExtendFrameIntoClientArea(this);
        }

        /// <summary>
        /// 添加预览
        /// </summary>
        /// <param name="img"></param>
        /// <param name="parentIndex"></param>
        /// <param name="needReferer"></param>
        public void AddPreview(Img img, int parentIndex, string needReferer)
        {
            if (!imgs.ContainsKey(img.Id))
            {
                imgs.Add(img.Id, index++);
                oriIndex.Add(img.Id, parentIndex);
                descs.Add(img.Id, img);
                ToggleButton btn = new ToggleButton
                {
                    Content = img.Id
                };
                btn.Checked += new RoutedEventHandler(btn_Click);
                btns.Children.Add(btn);

                Image iiii = new Image()
                {
                    //Opacity = 0,
                    //Width = 80,
                    //Height = 122,
                    //Stretch = System.Windows.Media.Stretch.None,
                    Source = new BitmapImage(new Uri("/Images/loading.png", UriKind.Relative))
                };
                iiii.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(iiii_ImageFailed);

                imgGrid.Children.Add(new ScrollViewer()
                {
                    Content = iiii,
                    //Opacity = 0,
                    Visibility = System.Windows.Visibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    //VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    //HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    //Margin = new Thickness(0)
                });
                DownloadImg(img.Id, img.SampleUrl, needReferer);

                if (selectedId == 0)
                {
                    (btns.Children[btns.Children.Count - 1] as ToggleButton).IsChecked = true;
                }
            }
        }

        void iiii_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image img = sender as Image;
            img.Stretch = Stretch.None;
            img.Source = new BitmapImage(new Uri("/Images/pic.png", UriKind.Relative));
        }

        /// <summary>
        /// 下载图片
        /// </summary>
        private void DownloadImg(int id, string url, string needReferer)
        {
            try
            {
                System.Net.HttpWebRequest req = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;

                reqs.Add(id, req);
                req.Proxy = MainWindow.WebProxy;

                req.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                if (needReferer != null)
                    //req.Referer = url.Substring(0, url.IndexOf('/', 7) + 1);
                    req.Referer = needReferer;

                req.AllowAutoRedirect = false;

                //异步下载开始
                req.BeginGetResponse(new AsyncCallback(RespCallback), new KeyValuePair<int, System.Net.HttpWebRequest>(id, req));
            }
            catch (Exception ex)
            {
                Program.Log(ex, "Download sample failed");
                StopLoadImg(id);
            }
        }

        /// <summary>
        /// 异步下载结束
        /// </summary>
        /// <param name="req"></param>
        private void RespCallback(IAsyncResult req)
        {
            KeyValuePair<int, System.Net.HttpWebRequest> re = (KeyValuePair<int, System.Net.HttpWebRequest>)(req.AsyncState);
            try
            {
                System.Net.WebResponse res = re.Value.EndGetResponse(req);

                Dispatcher.Invoke(new UIdelegate(delegate(object sender)
                {
                    System.IO.Stream str = res.GetResponseStream();
                    //Image iii = (imgGrid.Children[imgs[re.Key]] as Image);
                    //iii.Stretch = Stretch.Uniform;
                    BitmapDecoder bd = BitmapDecoder.Create(str, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                    bd.Frames[0].DownloadCompleted += new EventHandler(delegate(object s, EventArgs ev)
                    {
                        try
                        {
                            //下载完毕后再显示
                            Image iii = (imgGrid.Children[imgs[re.Key]] as ScrollViewer).Content as Image;
                            //iii.Stretch = Stretch.None;

                            iii.MouseLeftButtonUp += new MouseButtonEventHandler(delegate(object s1, MouseButtonEventArgs ea)
                            {
                                preMX = 0; preMY = 0;
                            });
                            iii.MouseLeftButtonDown += new MouseButtonEventHandler(delegate(object s1, MouseButtonEventArgs ea)
                            {
                                preMX = 0; preMY = 0;
                            });
                            iii.MouseDown += new MouseButtonEventHandler(delegate(object s1, MouseButtonEventArgs ea) {
                                if (ea.MiddleButton == MouseButtonState.Pressed)
                                    Button_Click_2(null, null);
                            });
                            iii.MouseMove += new MouseEventHandler(delegate(object s1, MouseEventArgs ea)
                            {
                                //拖动
                                if (ea.LeftButton == MouseButtonState.Pressed)
                                {
                                    if (preMY != 0 && preMX != 0)
                                    {
                                        int offX = (int)(ea.GetPosition(LayoutRoot).X) - preMX;
                                        int offY = (int)(ea.GetPosition(LayoutRoot).Y) - preMY;
                                        //FrameworkElement im = s1 as Image;
                                        ScrollViewer sc = (imgGrid.Children[imgs[selectedId]] as ScrollViewer);
                                        //im.Margin = new Thickness(im.Margin.Left + offX, im.Margin.Top + offY, im.Margin.Right - offX, im.Margin.Bottom - offY);
                                        sc.ScrollToHorizontalOffset(sc.HorizontalOffset - offX);
                                        sc.ScrollToVerticalOffset(sc.VerticalOffset - offY);
                                    }
                                    preMX = (int)(ea.GetPosition(LayoutRoot).X);
                                    preMY = (int)(ea.GetPosition(LayoutRoot).Y);
                                }
                            });
                            iii.Source = bd.Frames[0];
                            iii.Width = bd.Frames[0].PixelWidth;
                            iii.Height = bd.Frames[0].PixelHeight;
                            iii.Stretch = Stretch.Uniform;
                        }
                        catch (Exception ex1)
                        {
                            Program.Log(ex1, "Read sample img failed");
                            Dispatcher.Invoke(new UIdelegate(delegate(object ss) { StopLoadImg(re.Key); }), "");
                        }
                    });
                    //iii.Source = im;
                }), this);
            }
            catch (Exception ex2)
            {
                Program.Log(ex2, "Download sample failed");
                Dispatcher.Invoke(new UIdelegate(delegate(object sender) { StopLoadImg(re.Key); }), "");
            }
        }

        /// <summary>
        /// 停止加载
        /// </summary>
        /// <param name="id"></param>
        public void StopLoadImg(int id)
        {
            //if (imgs[id] < imgGrid.Children.Count)
            try
            {
                if (reqs.ContainsKey(id))
                {
                    System.Net.HttpWebRequest req = reqs[id];
                    if (req != null)
                        req.Abort();
                }
                
                Image img = (imgGrid.Children[imgs[id]] as ScrollViewer).Content as Image;
                img.Stretch = Stretch.None;
                img.Source = new BitmapImage(new Uri("/Images/pic.png", UriKind.Relative));
            }
            catch (Exception ex)
            {
                //MessageBox.Show(this, "StopLoadImg Failed\r\nimgGrid Children Count: " + imgGrid.Children.Count + " id: " + id + " imgs[id]: " + imgs[id] + "\r\n" + ex, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 切换预览图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btn_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as ToggleButton).Content;
            if (selectedId != id)
            {
                if (selectedId != 0 && imgs.ContainsKey(selectedId))
                {
                    (btns.Children[imgs[selectedId]] as ToggleButton).IsChecked = false;

                    //(imgGrid.Children[imgs[selectedId]] as Image).Opacity = 0;
                    //(imgGrid.Children[imgs[selectedId]] as Image).BeginStoryboard(FindResource("imgClose") as Storyboard);
                    ScrollViewer tempPreview = (imgGrid.Children[imgs[selectedId]] as ScrollViewer);
                    Storyboard sb = new Storyboard();
                    DoubleAnimationUsingKeyFrames frames = new DoubleAnimationUsingKeyFrames();
                    Storyboard.SetTargetProperty(frames, new PropertyPath(UIElement.OpacityProperty));
                    frames.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500))));
                    sb.Children.Add(frames);
                    sb.Completed += new EventHandler(delegate(object s, EventArgs ea) { tempPreview.Visibility = System.Windows.Visibility.Hidden; });
                    //tempPreview.Visibility = System.Windows.Visibility.Hidden;
                    sb.Begin(tempPreview);
                }
                selectedId = id;
                //(imgGrid.Children[imgs[selectedId]] as Image).Opacity = 1;
                ScrollViewer tempPreview1 = (imgGrid.Children[imgs[selectedId]] as ScrollViewer);
                tempPreview1.Visibility = System.Windows.Visibility.Visible;
                tempPreview1.BeginStoryboard(FindResource("imgShow") as Storyboard);

                ///////////////////////////////////////////////
                ////////////////////////////////////////////

                desc.Text = "";
                if (descs[selectedId].OriginalUrl == descs[selectedId].SampleUrl)
                {
                    desc.Inlines.Add("原图与预览图相同");
                    desc.Inlines.Add(new LineBreak());
                }
                desc.Inlines.Add("描述: " + descs[selectedId].Id + " " + descs[selectedId].Desc);
                desc.Inlines.Add(new LineBreak());
                try
                {
                    string fileType = descs[selectedId].OriginalUrl.Substring(descs[selectedId].OriginalUrl.LastIndexOf('.') + 1);
                    desc.Inlines.Add("类型: " + fileType.ToUpper());
                }
                catch { }
                desc.Inlines.Add(" 大小: " + descs[selectedId].FileSize);
                desc.Inlines.Add(" 尺寸: " + descs[selectedId].Dimension);
                //desc.Inlines.Add(new LineBreak());
                desc.Inlines.Add(" 评分: " + descs[selectedId].Score);
                desc.Inlines.Add(new LineBreak());
                desc.Inlines.Add("时间: " + descs[selectedId].Date);
                if (descs[selectedId].Source.Length > 0)
                {
                    desc.Inlines.Add(new LineBreak());
                    desc.Inlines.Add("来源: " + descs[selectedId].Source);
                }
            }
        }

        /// <summary>
        /// 选中并关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (selectedId != 0)
            {
                mainW.SelectByIndex(oriIndex[selectedId]);
                CloseImg();
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (selectedId != 0)
            {
                CloseImg();
            }
        }

        private void CloseImg()
        {
            //close
            StopLoadImg(selectedId);

            btns.Children[imgs[selectedId]].Visibility = System.Windows.Visibility.Collapsed;
            imgGrid.Children[imgs[selectedId]].Visibility = System.Windows.Visibility.Collapsed;
            int oriId = selectedId;

            if (imgs.Count > 1)
            {
                //用最左边的
                int index = 0;
                for (int i = 0; i < btns.Children.Count; i++)
                {
                    if (btns.Children[i].Visibility == System.Windows.Visibility.Collapsed)
                    {
                        index++;
                    }
                    else break;
                }
                (btns.Children[index] as ToggleButton).IsChecked = true;
                selectedId = (int)(btns.Children[index] as ToggleButton).Content;
            }
            else selectedId = 0;

            imgs.Remove(oriId);
            descs.Remove(oriId);
            oriIndex.Remove(oriId);
            reqs.Remove(oriId);

            if (imgs.Count == 0)
            {
                Close();
            }
        }

        /// <summary>
        /// 保存预览图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                //Save
                string dest = Uri.UnescapeDataString(descs[selectedId].SampleUrl.Substring(descs[selectedId].SampleUrl.LastIndexOf('/') + 1));
                dest = DownloadControl.ReplaceInvalidPathChars(dest, ' ');
                dest = DownloadControl.SaveLocation + "\\" + dest;

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                var dd = ((imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as Image).Source as BitmapFrame;

                if (dd == null)
                {
                    MessageBox.Show(this, "图片尚未加载完毕", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    encoder.Frames.Add(dd);
                    System.IO.FileStream fs = new System.IO.FileStream(dest, System.IO.FileMode.Create);
                    encoder.Save(fs);
                    fs.Close();
                    MessageBox.Show(this, "已成功保存至下载文件夹", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存失败\r\n" + ex.Message, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Copy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapFrame dd = ((imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as Image).Source as BitmapFrame;

                if (dd == null)
                {
                    MessageBox.Show(this, "图片尚未加载完毕", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    Clipboard.SetImage(dd);
                }
            }
            catch (Exception)
            {
                //MessageBox.Show(this, "保存失败\r\n" + ex.Message, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Copy preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].SampleUrl);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Copy ori
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].OriginalUrl);
            }
            catch (Exception) { }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (System.Net.HttpWebRequest req in reqs.Values)
            {
                req.Abort();
            }
            reqs.Clear();
            imgGrid.Children.Clear();
            mainW.previewFrm = null;

            (new System.Threading.Thread(new System.Threading.ThreadStart(delegate() { System.Threading.Thread.Sleep(2000); System.GC.Collect(); }))).Start();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            BitmapFrame dd = ((imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as Image).Source as BitmapFrame;

            if (dd == null)
            {
                MessageBox.Show(this, "图片尚未加载完毕", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Image img = (imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as Image;
                if (double.IsNaN(img.Width))
                {
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    img.Width = (img.Source as BitmapFrame).PixelWidth;
                    img.Height = (img.Source as BitmapFrame).PixelHeight;
                    //img.Stretch = Stretch.None;
                    //img.Cursor = Cursors.Hand;
                    //isOriSize = true;
                }
                else
                {
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    img.Width = Double.NaN;
                    img.Height = Double.NaN;
                    //img.Stretch = Stretch.Uniform;
                    //img.Cursor = Cursors.Arrow;
                    //isOriSize = false;
                }
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].Source);
            }
            catch (Exception) { }
        }

        private void Border_MouseEnter_1(object sender, MouseEventArgs e)
        {
            brdDesc.Opacity = 0.16;
        }

        private void Border_MouseLeave_1(object sender, MouseEventArgs e)
        {
            brdDesc.Opacity = 1;
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].Desc);
            }
            catch (Exception) { }
        }
    }
}
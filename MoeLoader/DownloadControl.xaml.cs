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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace MoeLoader
{
    public delegate void DownloadHandler(long size, double percent, string url, double speed);

    public struct MiniDownloadItem
    {
        public string url;
        public string fileName;
        public MiniDownloadItem(string file, string url)
        {
            if (file != null)
            {
                string ext = url.Substring(url.LastIndexOf('.'), url.Length - url.LastIndexOf('.'));
                if (file.Length > 200)
                    file = file.Substring(0, 200);

                if (file.EndsWith(ext))
                    this.fileName = file.Trim();
                else
                    this.fileName = file.Trim() + ext;
            }
            else fileName = null;
            this.url = url;
        }
    }

    /// <summary>
    /// Interaction logic for DownloadControl.xaml
    /// 下载面板用户控件
    /// </summary>
    public partial class DownloadControl : UserControl
    {
        public const string DLEXT = ".moe";

        //一个下载任务
        private class DownloadTask
        {
            public string Url { get; set; }
            public string SaveLocation { set; get; }
            public bool IsStop { set; get; }
            public string NeedReferer { get; set; }

            /// <summary>
            /// 下载任务
            /// </summary>
            /// <param name="url">目标地址</param>
            /// <param name="saveLocation">保存位置</param>
            /// <param name="referer">是否需要伪造Referer</param>
            public DownloadTask(string url, string saveLocation, string referer)
            {
                SaveLocation = saveLocation;
                Url = url;
                NeedReferer = referer;
                IsStop = false;
            }
        }

        //下载对象
        private ObservableCollection<DownloadItem> downloadItems = new ObservableCollection<DownloadItem>();
        public ObservableCollection<DownloadItem> DownloadItems
        {
            get { return downloadItems; }
        }

        //downloadItems的副本，用于快速查找
        private Dictionary<string, DownloadItem> downloadItemsDic = new Dictionary<string, DownloadItem>();

        private bool isWorking = false;
        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsWorking
        {
            get { return isWorking; }
            //set { isWorking = value; }
        }

        private int numOnce;
        /// <summary>
        /// 同时下载的任务数量
        /// </summary>
        public int NumOnce
        {
            set
            {
                if (value > 5) value = 5;
                else if (value < 1) value = 1;

                numOnce = value;
                //SetNum(value);
            }
            get { return numOnce; }
        }

        /// <summary>
        /// 分站点存放
        /// </summary>
        public bool IsSepSave { get; set; }

        private static string saveLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        /// <summary>
        /// 下载的保存位置
        /// </summary>
        public static string SaveLocation { get { return saveLocation; } set { saveLocation = value; } }

        private int numSaved = 0;
        private int numLeft = 0;

        //private string adText = "";
        private string adUrl = "";
        //public string AdImgUrl { get; set; }
        //public string AdText { get { return adText; } }
        //public string AdUrl { get { return adUrl; } }

        /// <summary>
        /// AD
        /// </summary>
        /// <param name="adTxt"></param>
        /// <param name="adUrl"></param>
        public void SetAd(string adTxt, string adUrl, string adImgUrl)
        {
            try
            {
                this.adUrl = adUrl;
                //this.adText = adTxt;
                if (adTxt.Trim().Length > 0 && adImgUrl.Trim().Length > 0)
                {
                    imgAd.ToolTip = adTxt;
                    BitmapImage bi3 = new BitmapImage();
                    bi3.BeginInit();
                    bi3.UriSource = new Uri(adImgUrl);
                    bi3.EndInit();
                    imgAd.Source = bi3;
                    adB.Visibility = System.Windows.Visibility.Visible;
                }
            }
            catch { }
        }

        //正在下载的链接
        private Dictionary<string, DownloadTask> webs = new Dictionary<string, DownloadTask>();

        public DownloadControl()
        {
            this.InitializeComponent();

            NumOnce = 2;
            IsSepSave = false;

            downloadStatus.Text = "当前无下载任务";

            dlList.DataContext = this;
        }

        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="urls"></param>
        public void AddDownload(System.Collections.Generic.IEnumerable<MiniDownloadItem> items)
        {
            foreach (MiniDownloadItem item in items)
            {
                string text = item.fileName;
                if (text == null || text.Trim().Length == 0)
                    text = Uri.UnescapeDataString(item.url.Substring(item.url.LastIndexOf('/') + 1));

                try
                {
                    DownloadItem itm = new DownloadItem(text, item.url);
                    downloadItemsDic.Add(item.url, itm);
                    downloadItems.Add(itm);
                    numLeft++;
                }
                catch (ArgumentException) { }//duplicate entry
            }

            if (!isWorking)
            {
                isWorking = true;
            }

            RefreshList();
        }

        /// <summary>
        /// 刷新下载状态
        /// </summary>
        private void RefreshList()
        {
            TotalProgressChanged();

            //根据numOnce及正在下载的情况生成下载
            int downloadingCount = webs.Count;
            for (int j = 0; j < NumOnce - downloadingCount; j++)
            {
                if (numLeft > 0)
                {
                    string url = downloadItems[downloadItems.Count - numLeft].Url;

                    //string dest = Uri.UnescapeDataString(url.Substring(url.LastIndexOf('/') + 1));
                    string dest = downloadItems[downloadItems.Count - numLeft].FileName;
                    dest = ReplaceInvalidPathChars(dest, ' ');
                    if (IsSepSave && dest.IndexOf(' ') > 0)
                    {
                        string sepPath = saveLocation + "\\" + dest.Substring(0, dest.IndexOf(' '));
                        if (!System.IO.Directory.Exists(sepPath))
                            System.IO.Directory.CreateDirectory(sepPath);
                        dest = sepPath + "\\" + dest;
                    }
                    else
                    {
                        dest = saveLocation + "\\" + dest;
                    }

                    if (System.IO.File.Exists(dest))
                    {
                        downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.Failed;
                        downloadItems[downloadItems.Count - numLeft].Size = "文件已存在";
                        WriteErrText("moe_error.txt", url + ": 文件已存在");
                        j--;
                    }
                    else if (dest.Length > 259)
                    {
                        downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.Failed;
                        downloadItems[downloadItems.Count - numLeft].Size = "路径过长";
                        WriteErrText("moe_error.txt", url + ": 路径过长");
                        j--;
                    }
                    else
                    {
                        downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.DLing;

                        DownloadTask task = new DownloadTask(url, dest, MainWindow.IsNeedReferer(url));
                        webs.Add(url, task);

                        //异步下载开始
                        System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Download));
                        thread.Start(task);
                    }

                    numLeft--;
                }
                else break;
            }
            RefreshStatus();
        }

        /// <summary>
        /// 下载，另一线程
        /// </summary>
        /// <param name="o"></param>
        private void Download(object o)
        {
            DownloadTask task = o as DownloadTask;
            System.IO.FileStream fs = null;
            System.IO.Stream str = null;

            try
            {
                System.Net.HttpWebRequest req = System.Net.WebRequest.Create(task.Url) as System.Net.HttpWebRequest;

                req.Proxy = MainWindow.WebProxy;

                req.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                req.ReadWriteTimeout = 60000;
                req.Timeout = 60000;
                //req.Accept = "text/html, application/xhtml+xml, */*";
                //req.Headers.Add("Accept-Encoding", "gzip, deflate");
                if (task.NeedReferer != null)
                    //req.Referer = task.Url.Substring(0, task.Url.IndexOf('/', 7) + 1);
                    req.Referer = task.NeedReferer;

                System.Net.WebResponse res = req.GetResponse();

                /////////开始写入文件
                str = res.GetResponseStream();

                byte[] bytes = new byte[10240];
                fs = new System.IO.FileStream(task.SaveLocation + DLEXT, System.IO.FileMode.Create);

                int bytesReceived = 0;
                DateTime last = DateTime.Now;
                int osize = str.Read(bytes, 0, bytes.Length);
                double downed = osize;
                while (!task.IsStop && osize > 0)
                {
                    fs.Write(bytes, 0, osize);
                    bytesReceived += osize;
                    DateTime now = DateTime.Now;
                    double speed = -1;
                    if ((now - last).TotalSeconds > 0.6)
                    {
                        speed = downed / (now - last).TotalSeconds / 1024.0;
                        downed = 0;
                        last = now;
                    }
                    Dispatcher.Invoke(new DownloadHandler(web_DownloadProgressChanged), res.ContentLength, (double)bytesReceived / (double)res.ContentLength * 100.0, task.Url, speed);
                    osize = str.Read(bytes, 0, bytes.Length);
                    downed += osize;
                }
            }
            catch (Exception ex)
            {
                //Dispatcher.Invoke(new UIdelegate(delegate(object sender) { StopLoadImg(re.Key, re.Value); }), "");
                task.IsStop = true;
                Dispatcher.Invoke(new VoidDel(delegate()
                {
                    //下载失败
                    if (downloadItemsDic.ContainsKey(task.Url))
                    {
                        downloadItemsDic[task.Url].StatusE = DLStatus.Failed;
                        downloadItemsDic[task.Url].Size = "网络错误";
                        WriteErrText("moe_error.txt", task.Url + ": " + ex.Message);
                        WriteErrText("moe_error.lst", task.Url);
                    }
                }));
            }
            finally
            {
                try
                {
                    if (fs != null)
                        fs.Close();
                    if (str != null)
                        str.Close();
                }
                catch { }
            }

            if (task.IsStop)
            {
                //任务被取消
                Dispatcher.Invoke(new VoidDel(delegate()
                {
                    if (downloadItemsDic.ContainsKey(task.Url))
                    {
                        downloadItemsDic[task.Url].StatusE = DLStatus.Failed;
                        if (downloadItemsDic[task.Url].Size != "网络错误")
                            downloadItemsDic[task.Url].Size = "已取消";
                    }
                }));

                try
                {
                    System.IO.File.Delete(task.SaveLocation + DLEXT);
                }
                catch { }
            }
            else
            {
                //下载成功完成
                Dispatcher.Invoke(new VoidDel(delegate()
                {
                    try
                    {
                        //DownloadTask task1 = obj as DownloadTask;

                        //判断完整性
                        if (100 - downloadItemsDic[task.Url].Progress > 0.001)
                        {
                            task.IsStop = true;
                            downloadItemsDic[task.Url].StatusE = DLStatus.Failed;
                            downloadItemsDic[task.Url].Size = "网络错误";
                            System.IO.File.Delete(task.SaveLocation + DLEXT);
                        }
                        else
                        {
                            //修改后缀名
                            System.IO.File.Move(task.SaveLocation + DLEXT, task.SaveLocation);

                            downloadItemsDic[task.Url].StatusE = DLStatus.Success;
                            numSaved++;
                        }
                    }
                    catch { }
                }));
            }

            //下载结束
            Dispatcher.Invoke(new VoidDel(delegate(){
                webs.Remove(task.Url);
                RefreshList();
            }));
        }

        private void WriteErrText(string fileName, string content)
        {
            try
            {
                System.IO.File.AppendAllText(saveLocation + "\\" + fileName, content + "\r\n");
            }
            catch { }
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void RefreshStatus()
        {
            if (webs.Count > 0)
            {
                downloadStatus.Text = "已保存 " + numSaved + " 剩余 " + numLeft + " 正在下载 " + webs.Count;
            }
            else
            {
                isWorking = false;
                downloadStatus.Text = "已保存 " + numSaved + " 剩余 " + numLeft + " 下载完毕";
            }

            if (downloadItems.Count == 0)
                blkTip.Visibility = Visibility.Visible;
            else blkTip.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 下载进度发生改变
        /// </summary>
        /// <param name="total"></param>
        /// <param name="percent"></param>
        /// <param name="url"></param>
        void web_DownloadProgressChanged(long total, double percent, string url, double speed)
        {
            try
            {
                string size = total > 1048576 ? (total / 1048576.0).ToString("0.00MB") : (total / 1024.0).ToString("0.00KB");
                downloadItemsDic[url].Size = size;
                downloadItemsDic[url].Progress = percent > 100 ? 100 : percent;
                if (speed > 0)
                    downloadItemsDic[url].SetSpeed(speed);
            }
            catch { }
        }

        /// <summary>
        /// 总下载进度，根据下载完成的图片数量计算
        /// </summary>
        private void TotalProgressChanged()
        {
            if (downloadItems.Count > 0)
            {
                double percent = (double)(downloadItems.Count - numLeft - webs.Count) / (double)downloadItems.Count * 100.0;

                Win7TaskBar.ChangeProcessValue(MainWindow.Hwnd, (uint)percent);

                if (Math.Abs(percent - 100.0) < 0.001)
                {
                    Win7TaskBar.StopProcess(MainWindow.Hwnd);
                    if (GlassHelper.GetForegroundWindow() != MainWindow.Hwnd)
                    {
                        //System.Media.SystemSounds.Beep.Play();
                        GlassHelper.FlashWindow(MainWindow.Hwnd, true);
                    }

                    #region 关机
                    if (itmAutoClose.IsChecked)
                    {
                        //关机
                        System.Timers.Timer timer = new System.Timers.Timer()
                        {
                            //20秒后关闭
                            Interval = 20000,
                            Enabled = false,
                            AutoReset = false
                        };
                        timer.Elapsed += delegate { GlassHelper.ExitWindows(GlassHelper.ShutdownType.PowerOff); };
                        timer.Start();

                        if (MessageBox.Show("系统将于20秒后自动关闭，若要取消请点击确定", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
                        {
                            timer.Stop();
                        }
                    }
                    #endregion
                }
            }
            else
            {
                Win7TaskBar.StopProcess(MainWindow.Hwnd);
            }
        }

        /// <summary>
        /// 去掉文件名中的无效字符,如 \ / : * ? " < > | 
        /// </summary>
        /// <param name="file">待处理的文件名</param>
        /// <param name="replace">替换字符</param>
        /// <returns>处理后的文件名</returns>
        public static string ReplaceInvalidPathChars(string file, char replace)
        {
            if (file.IndexOf('?', file.LastIndexOf('.')) > 0)
            {
                //adfadsf.jpg?adfsdf   remove trailing ?param
                file = file.Substring(0, file.IndexOf('?'));
            }

            file = file.Replace('\\', replace);
            file = file.Replace('/', replace);
            file = file.Replace(':', replace);
            file = file.Replace('*', replace);
            file = file.Replace('?', replace);
            file = file.Replace('\"', replace);
            file = file.Replace('<', replace);
            file = file.Replace('>', replace);
            file = file.Replace('|', replace);
            return file;
        }

        /// <summary>
        /// 导出lst
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmLst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
                    foreach (DownloadItem i in dlList.SelectedItems)
                    {
                        //text += i.Url + "|\r\n";
                        text += i.Url + "|" + i.FileName + "\r\n";
                        success++;
                    }
                    System.IO.File.AppendAllText(saveFileDialog1.FileName, text);
                    MessageBox.Show("成功保存 " + success + " 个地址\r\n", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:\r\n" + ex.Message, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 复制地址
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmCopy_Click(object sender, RoutedEventArgs e)
        {
            DownloadItem i = dlList.SelectedItems[0] as DownloadItem;
            string text = i.Url;
            try
            {
                Clipboard.SetText(text);
            }
            catch { }
        }

        /// <summary>
        /// 重试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmRetry_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadItem> selected = new List<DownloadItem>();
            foreach (object o in dlList.SelectedItems)
            {
                //转存集合，防止selected改变
                DownloadItem item = o as DownloadItem;
                selected.Add(item);
            }

            foreach (DownloadItem item in selected)
            {
                if (item.StatusE == DLStatus.Failed)
                {
                    downloadItems.Remove(item);
                    downloadItemsDic.Remove(item.Url);
                    AddDownload(new MiniDownloadItem[] { new MiniDownloadItem(item.FileName, item.Url) });
                }
            }
        }

        /// <summary>
        /// 停止某个任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmStop_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadItem> selected = new List<DownloadItem>();
            foreach (object o in dlList.SelectedItems)
            {
                //转存集合，防止selected改变
                DownloadItem item = o as DownloadItem;
                selected.Add(item);
            }

            foreach (DownloadItem item in selected)
            {
                if (item.StatusE == DLStatus.DLing && webs.ContainsKey(item.Url))
                {
                    webs[item.Url].IsStop = true;
                    webs.Remove(item.Url);

                    //RefreshList();
                }
            }
            RefreshList();
        }

        /// <summary>
        /// 移除某个任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmDelete_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadItem> selected = new List<DownloadItem>();
            foreach (object o in dlList.SelectedItems)
            {
                //转存集合，防止selected改变
                DownloadItem item = o as DownloadItem;
                selected.Add(item);
            }

            foreach (DownloadItem item in selected)
            {
                if (item.StatusE == DLStatus.DLing && webs.ContainsKey(item.Url))
                {
                    webs[item.Url].IsStop = true;
                    webs.Remove(item.Url);

                    //RefreshList();
                }
                else if (item.StatusE == DLStatus.Success)
                    numSaved--;
                else if (item.StatusE == DLStatus.Wait)
                    numLeft--;

                downloadItems.Remove(item);
                downloadItemsDic.Remove(item.Url);

                //RefreshStatus();
            }
            RefreshList();
            RefreshStatus();
        }

        /// <summary>
        /// 停止所有下载
        /// </summary>
        public void StopAll()
        {
            downloadItems.Clear();
            downloadItemsDic.Clear();
            foreach (DownloadTask item in webs.Values)
            {
                item.IsStop = true;
            }
        }

        /// <summary>
        /// 清空已成功任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmClearDled_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (true)
            {
                if (i >= downloadItems.Count) break;
                DownloadItem item = downloadItems[i];
                if (item.StatusE == DLStatus.Success)
                {
                    downloadItems.RemoveAt(i);
                    downloadItemsDic.Remove(item.Url);
                }
                else
                {
                    i++;
                }
            }
            numSaved = 0;
            RefreshStatus();
        }

        /// <summary>
        /// 选择保存位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void itmSaveLocation_Click(object sender, RoutedEventArgs e)
        //{
        //    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
        //    {
        //        Description = "当前保存位置: " + saveLocation,
        //        SelectedPath = saveLocation
        //    };

        //    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {
        //        SaveLocation = dialog.SelectedPath;
        //    }
        //}

        //#region 设置同时下载数量
        //public void SetNum(int i)
        //{
        //    if (i == 1)
        //    {
        //        itm1.IsChecked = true;
        //    }
        //    else if (i == 2)
        //    {
        //        itm2.IsChecked = true;
        //    }
        //    else if (i == 3)
        //    {
        //        itm3.IsChecked = true;
        //    }
        //    else if (i == 4)
        //    {
        //        itm4.IsChecked = true;
        //    }
        //    else if (i == 5)
        //    {
        //        itm5.IsChecked = true;
        //    }
        //}

        //private void itm1_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (sender == itm1)
        //    {
        //        if (itm1.IsChecked)
        //        {
        //            NumOnce = 1;
        //            itm2.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm4.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm2)
        //    {
        //        if (itm2.IsChecked)
        //        {
        //            NumOnce = 2;
        //            itm1.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm4.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm3)
        //    {
        //        if (itm3.IsChecked)
        //        {
        //            NumOnce = 3;
        //            itm1.IsChecked = false;
        //            itm2.IsChecked = false;
        //            itm4.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm4)
        //    {
        //        if (itm4.IsChecked)
        //        {
        //            NumOnce = 4;
        //            itm1.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm2.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm5)
        //    {
        //        if (itm5.IsChecked)
        //        {
        //            NumOnce = 5;
        //            itm1.IsChecked = false;
        //            itm2.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm4.IsChecked = false;
        //        }
        //    }
        //    SetNum(NumOnce);
        //}
        //#endregion

        /// <summary>
        /// 右键菜单即将打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (dlList.SelectedItems == null || dlList.SelectedItems.Count == 0)
            {
                itmLst.IsEnabled = false;
                itmCopy.IsEnabled = false;
                itmRetry.IsEnabled = false;
                itmStop.IsEnabled = false;
                itmDelete.IsEnabled = false;
            }
            else
            {
                if (dlList.SelectedItems.Count == 1)
                {
                    itmCopy.IsEnabled = true;
                }
                else
                {
                    itmCopy.IsEnabled = false;
                }

                itmLst.IsEnabled = true;
                //DownloadItem item = dlList.SelectedItems[0] as DownloadItem;
                itmDelete.IsEnabled = true;
                //if (item.StatusE == DLStatus.Failed)
                itmRetry.IsEnabled = true;
                //else itmRetry.IsEnabled = false;
                //if (item.StatusE == DLStatus.DLing)
                itmStop.IsEnabled = true;
                //else itmStop.IsEnabled = false;
            }
        }

        /// <summary>
        /// 文件拖拽事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserControl_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                string fileName = ((string[])(e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop)))[0];
                if (fileName != null && System.IO.Path.GetExtension(fileName) == ".lst")
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else e.Effects = DragDropEffects.None;
            }
            catch (Exception) { e.Effects = DragDropEffects.None; }
        }

        /// <summary>
        /// 从lst文件添加下载
        /// </summary>
        /// <param name="fileName"></param>
        public void DownLoadFromFile(string fileName)
        {
            if (fileName != null && System.IO.Path.GetExtension(fileName) == ".lst")
            {
                List<string> lines = new List<string>(System.IO.File.ReadAllLines(fileName));
                List<MiniDownloadItem> itms = new List<MiniDownloadItem>();
                //提取地址
                //for (int i = 0; i < lines.Count; )
                //{
                    //if (lines[i].EndsWith("|"))
                        //lines[i] = lines[i].Substring(0, lines[i].Length - 1);

                    //移除空行
                    //if (lines[i].Trim().Length == 0)
                    //{
                        //lines.RemoveAt(i);
                    //}
                    //else i++;
                //}

                foreach (string line in lines)
                {
                    //移除空行
                    if (line.Trim().Length == 0) continue;
                    string[] parts = line.Split('|');
                    if (parts.Length > 1 && parts[1].Length > 1)
                        itms.Add(new MiniDownloadItem(parts[1], parts[0]));
                    else if (parts.Length > 0)
                        itms.Add(new MiniDownloadItem(null, parts[0]));
                }
                //添加至下载列表
                AddDownload(itms);
            }
        }

        /// <summary>
        /// 文件被拖入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserControl_Drop(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                string fileName = ((string[])(e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop)))[0];
                DownLoadFromFile(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("从文件添加下载失败\r\n" + ex.Message, "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void itmOpenSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(SaveLocation);
            }
            catch { }
        }

        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    if ((dlList.SelectedItem as DownloadItem).StatusE == DLStatus.Success)
                    {
                        string path = (dlList.SelectedItem as DownloadItem).FileName;
                        if (System.IO.File.Exists(SaveLocation + "\\" + path))
                        {
                            System.Diagnostics.Process.Start(SaveLocation + "\\" + path);
                        }
                        else if (System.IO.File.Exists(SaveLocation + "\\" + path.Substring(0, path.IndexOf(' ')) + "\\" + path))
                        {
                            //for separate save
                            System.Diagnostics.Process.Start(SaveLocation + "\\" + path.Substring(0, path.IndexOf(' ')) + "\\" + path);
                        }
                    }
                }
            }
            catch { }
        }

        private void itmClearDled_Click_1(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (true)
            {
                if (i >= downloadItems.Count) break;
                DownloadItem item = downloadItems[i];
                if (item.StatusE == DLStatus.Failed)
                {
                    downloadItems.RemoveAt(i);
                    downloadItemsDic.Remove(item.Url);
                }
                else
                {
                    i++;
                }
            }
            RefreshStatus();
        }

        /// <summary>
        /// 仅清除已取消和已存在的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmClearDled_Click_2(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (true)
            {
                if (i >= downloadItems.Count) break;
                DownloadItem item = downloadItems[i];
                if (item.StatusE == DLStatus.Failed && item.Size != "网络错误")
                {
                    downloadItems.RemoveAt(i);
                    downloadItemsDic.Remove(item.Url);
                }
                else
                {
                    i++;
                }
            }
            RefreshStatus();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (adUrl.Length > 0)
                    System.Diagnostics.Process.Start(adUrl);
            }
            catch { }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            adB.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void itmSelAll_Click(object sender, RoutedEventArgs e)
        {
            dlList.SelectAll();
        }

        private void Grid_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs ev)
        {
            //change ad
            grdNext.IsEnabled = false;
            System.Net.WebClient c = new System.Net.WebClient();
            c.Encoding = Encoding.UTF8;
            c.DownloadStringAsync(new Uri("http://moeloader.sinaapp.com/update1.php?ad=1"));
            c.DownloadStringCompleted += new System.Net.DownloadStringCompletedEventHandler((o, e) =>
            {
                if (e.Error == null)
                {
                    string[] parts = e.Result.Split('|');
                    string[] ads = parts[1].Split(';');
                    if (ads.Length > 2)
                    {
                        SetAd(ads[0], ads[1], ads[2]);
                    }
                }
                grdNext.IsEnabled = true;
            });
        }
    }
}
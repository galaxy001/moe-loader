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

namespace MoeLoader
{
	/// <summary>
	/// Interaction logic for ColorWnd.xaml
    /// 界面颜色选择窗口
	/// </summary>
	public partial class OptionWnd : Window
	{
        private MainWindow main;

		public OptionWnd(MainWindow main)
		{
            this.main = main;
			this.InitializeComponent();

            if (!System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\nofont"))
            {
                FontFamily = new FontFamily("Microsoft YaHei");
            }

            //if (System.Environment.OSVersion.Version.Major >= 6)
            //{
            //    if (GlassHelper.DwmIsCompositionEnabled())
            //    {
            //        chkAero.IsEnabled = true;
            //    }
            //}

            //SetColor(main.GetColor());
            //chkPos.IsChecked = main.rememberPos;
            txtProxy.Text = MainWindow.Proxy;
            //chkProxy.IsChecked = MainWindow.ProxyEnable;
            if (MainWindow.ProxyType == ProxyType.Custom)
            {
                rtCustom.IsChecked = true;
                txtProxy.IsEnabled = true;
            }
            else if (MainWindow.ProxyType == ProxyType.None)
            {
                rtNoProxy.IsChecked = true;
            }
            txtBossKey.Text = MainWindow.BossKey.ToString();
            txtPattern.Text = main.namePatter;
            chkProxy_Click(null, null);
            //chkAero.IsChecked = main.isAero;
            txtCount.Text = PreFetcher.CachedImgCount.ToString();
            txtParal.Text = main.downloadC.NumOnce.ToString();
            chkSepSave.IsChecked = main.downloadC.IsSepSave;
            txtSaveLocation.Text = DownloadControl.SaveLocation;

            if (main.bgSt == Stretch.None)
            {
                cbBgSt.SelectedIndex = 0;
            }
            else if (main.bgSt == Stretch.Uniform)
            {
                cbBgSt.SelectedIndex = 1;
            }
            else if (main.bgSt == Stretch.UniformToFill)
            {
                cbBgSt.SelectedIndex = 2;
            }

            if (main.bgHe == AlignmentX.Left)
            {
                cbBgHe.SelectedIndex = 0;
            }
            else if (main.bgHe == AlignmentX.Center)
            {
                cbBgHe.SelectedIndex = 1;
            }
            else if (main.bgHe == AlignmentX.Right)
            {
                cbBgHe.SelectedIndex = 2;
            }

            if (main.bgVe == AlignmentY.Top)
            {
                cbBgVe.SelectedIndex = 0;
            }
            else if (main.bgVe == AlignmentY.Center)
            {
                cbBgVe.SelectedIndex = 1;
            }
            else if (main.bgVe == AlignmentY.Bottom)
            {
                cbBgVe.SelectedIndex = 2;
            }

            textNameHelp.ToolTip = "%site 站点缩写\r\n%id 编号\r\n%tag 标签\r\n%desc 描述";
		}

        //private void SetColor(Color c)
        //{
        //    sr.Value = c.R;
        //    sg.Value = c.G;
        //    sb.Value = c.B;
        //    sa.Value = c.A;
        //}

        //private void SetMainColor()
        //{
        //    Color c = Color.FromArgb((byte)sa.Value, (byte)sr.Value, (byte)sg.Value, (byte)sb.Value);
        //    main.SetBackColorLive(c);
        //}

        //private void s_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        //{
        //    SetMainColor();
        //}

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!System.IO.Directory.Exists(txtSaveLocation.Text))
            {
                MessageBox.Show(this, "设置的存储路径不存在，请重新设置", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (txtProxy.Text.Trim().Length > 0)
            {
                string add = txtProxy.Text.Trim();
                bool right = false;
                if (System.Text.RegularExpressions.Regex.IsMatch(add, @"^.+:(\d+)$"))
                    //@"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5]):(\d+)$"))
                {
                    int port;
                    if (int.TryParse(add.Substring(add.IndexOf(':') + 1), out port))
                    {
                        if (port > 0 && port < 65535)
                        {
                            MainWindow.Proxy = txtProxy.Text.Trim();
                            right = true;
                        }
                    }
                }
                if (!right)
                {
                    MessageBox.Show(this, "代理地址格式不正确，应类似于 192.168.1.1:8000 形式", "Moe Loader", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else MainWindow.Proxy = "";

            if (rtNoProxy.IsChecked.Value)
            {
                MainWindow.ProxyType = ProxyType.None;
            }
            else if (rtSystem.IsChecked.Value)
            {
                MainWindow.ProxyType = ProxyType.System;
            }
            else
            {
                MainWindow.ProxyType = ProxyType.Custom;
            }

            MainWindow.BossKey = (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), txtBossKey.Text);
            main.namePatter = txtPattern.Text.Replace(';', ' ').Trim();

            if (cbBgSt.SelectedIndex == 0)
            {
                main.bgSt = Stretch.None;
            }
            else if (cbBgSt.SelectedIndex == 1)
            {
                main.bgSt = Stretch.Uniform;
            }
            else if (cbBgSt.SelectedIndex == 2)
            {
                main.bgSt = Stretch.UniformToFill;
            }

            if (cbBgHe.SelectedIndex == 0)
            {
                main.bgHe = AlignmentX.Left;
            }
            else if (cbBgHe.SelectedIndex == 1)
            {
                main.bgHe = AlignmentX.Center;
            }
            else if (cbBgHe.SelectedIndex == 2)
            {
                main.bgHe = AlignmentX.Right;
            }

            if (cbBgVe.SelectedIndex == 0)
            {
                main.bgVe = AlignmentY.Top;
            }
            else if (cbBgVe.SelectedIndex == 1)
            {
                main.bgVe = AlignmentY.Center;
            }
            else if (cbBgVe.SelectedIndex == 2)
            {
                main.bgVe = AlignmentY.Bottom;
            }
            if (main.bgImg != null)
            {
                main.bgImg.Stretch = main.bgSt;
                main.bgImg.AlignmentX = main.bgHe;
                main.bgImg.AlignmentY = main.bgVe;
            }
            //Color c = Color.FromArgb((byte)sa.Value, (byte)sr.Value, (byte)sg.Value, (byte)sb.Value);
        	//OK
            //main.SetBackColor(c);
            //main.isAero = chkAero.IsChecked.Value;
            PreFetcher.CachedImgCount = int.Parse(txtCount.Text);

            DownloadControl.SaveLocation = txtSaveLocation.Text;
            main.downloadC.IsSepSave = chkSepSave.IsChecked.Value;
            main.downloadC.NumOnce = int.Parse(txtParal.Text);
            //main.rememberPos = chkPos.IsChecked.Value;
            Close();
        }

        private void Button1_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //cancel
            //main.RestoreColor();
            this.Close();
        }

        private void Button2_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //default
            //SetColor(Color.FromArgb(0x21, 0x7C, 0x9E, 0xBE));
            txtProxy.Text = "127.0.0.1:8000";
            txtPattern.Text = MainWindow.DefaultPatter;
            txtBossKey.Text = System.Windows.Forms.Keys.Subtract.ToString();
            //chkProxy.IsChecked = false;
            rtSystem.IsChecked = true;
            txtProxy.IsEnabled = false;
            //chkPos.IsChecked = false;
            //chkAero.IsChecked = true;
            txtCount.Text = "6";
            chkProxy_Click(null, null);
            txtParal.Text = "2";
            chkSepSave.IsChecked = false;
            cbBgHe.SelectedIndex = 2;
            cbBgVe.SelectedIndex = 2;
            cbBgSt.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //main.RestoreColor();
        }

        private void txtBossKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.System && e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl && e.Key != Key.LeftShift && e.Key != Key.RightAlt && e.Key != Key.RightCtrl && e.Key != Key.RightShift && e.Key != Key.LWin && e.Key != Key.RWin)
            {
                txtBossKey.Text = ((System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key)).ToString();
            }
            e.Handled = true;
        }

        private void chkProxy_Click(object sender, RoutedEventArgs e)
        {
            if (txtProxy != null)
            {
                if (rtNoProxy.IsChecked.Value)
                    txtProxy.IsEnabled = false;
                if (rtSystem.IsChecked.Value)
                    txtProxy.IsEnabled = false;
                if (rtCustom.IsChecked.Value)
                    txtProxy.IsEnabled = true;
            }
        }

        #region prefetch img count
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

        //private void txtNum_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //{
        //    TextBox txt = sender as TextBox;
        //    if (txt.Text.Length == 0)
        //        return;
        //    try
        //    {
        //        PreFetcher.CachedImgCount = int.Parse(txtCount.Text);

        //        txtCount.Text = PreFetcher.CachedImgCount.ToString();
        //    }
        //    catch (NullReferenceException) { }
        //    catch (FormatException)
        //    {
        //        txtCount.Text = PreFetcher.CachedImgCount.ToString();
        //    }
        //}

        //private void txtPage_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    TextBox txt = sender as TextBox;
        //    try
        //    {
        //        PreFetcher.CachedImgCount = int.Parse(txtCount.Text);

        //        txtCount.Text = PreFetcher.CachedImgCount.ToString();
        //    }
        //    catch (NullReferenceException) { }
        //    catch (FormatException)
        //    {
        //        txtCount.Text = PreFetcher.CachedImgCount.ToString();
        //    }
        //}

        private void pageUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int value = int.Parse(txtCount.Text);
            if (value < 20)
                txtCount.Text = (value + 1).ToString();
        }

        private void pageDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int value = int.Parse(txtCount.Text);
            if (value > 0)
                txtCount.Text = (value - 1).ToString();
        }
        private void pageUp_Click1(object sender, System.Windows.RoutedEventArgs e)
        {
            int value = int.Parse(txtParal.Text);
            if (value < 5)
                txtParal.Text = (value + 1).ToString();
        }

        private void pageDown_Click1(object sender, System.Windows.RoutedEventArgs e)
        {
            int value = int.Parse(txtParal.Text);
            if (value > 1)
                txtParal.Text = (value - 1).ToString();
        }
        #endregion

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this, "Moe Loader V" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version +
            "\r\n\r\n©2008-2012 esonic\r\nAll rights reserved.\r\n\r\nEmail: esonice@gmail.com\r\nSite: http://moeloader.sinaapp.com/", "Moe Loader - 关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// browse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "当前保存位置: " + txtSaveLocation.Text,
                SelectedPath = txtSaveLocation.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSaveLocation.Text = dialog.SelectedPath;
            }
        }

        private void textNameHelp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this, "%site 站点缩写\r\n%id 编号\r\n%tag 标签\r\n%desc 描述", "Moe Loader - 命名规则", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this, "将图片文件重命名为 bg.png 或 bg.jpg 后放入 MoeLoader.exe 所在目录，重启 MoeLoader 即可", "Moe Loader - 更换背景图", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
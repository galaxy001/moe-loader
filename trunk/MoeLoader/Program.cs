using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MoeLoader
{
    class Program
    {
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            try
            {
                //try
                //{
                System.Net.ServicePointManager.DefaultConnectionLimit = 100;
                System.Net.ServicePointManager.Expect100Continue = false;
                //}
                //catch { }
                SplashScreen splashScreen = new SplashScreen("images/slash.png");
                splashScreen.Show(true);
                MoeLoader.App app = new MoeLoader.App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.WriteAllText("moe_fatal.txt", ex.ToString());
                    System.Media.SystemSounds.Asterisk.Play();
                    (new ErrForm(ex.ToString())).ShowDialog();
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
                catch { }
            }
        }
    }
}

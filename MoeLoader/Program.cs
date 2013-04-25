using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MoeLoader
{
    class Program
    {
        public static bool is_debug = false;

        static Program()
        {
            try
            {
                is_debug = System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\debug.txt");
            }
            catch { }
        }

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

        public static void Log(Exception e, string desc)
        {
            try
            {
                if (is_debug)
                {
                    System.IO.File.AppendAllText("moe_log.txt", DateTime.Now + " " + desc + ": " + e.ToString() + "\r\n");
                }
            }
            catch { }
        }
    }
}

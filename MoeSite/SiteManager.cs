using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MoeLoader
{
    public class SiteManager
    {
        private List<ImageSite> sites = new List<ImageSite>();
        private static SiteManager instance;

        private SiteManager()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (System.IO.File.Exists(path + "\\SitePack.dll.new"))
            {
                //update site pack
                try
                {
                    System.IO.File.Delete(path + "\\SitePack.dll");
                    System.IO.File.Move(path + "\\SitePack.dll.new", path + "\\SitePack.dll");
                }
                catch { }
            }

            string[] dlls = System.IO.Directory.GetFiles(path, "SitePack*.dll", System.IO.SearchOption.TopDirectoryOnly);
            foreach (string dll in dlls)
            {
                try
                {
                    Type type = Assembly.LoadFile(dll).GetType("SitePack.SiteProvider", true, false);
                    MethodInfo methodInfo = type.GetMethod("SiteList");
                    sites.AddRange(methodInfo.Invoke(System.Activator.CreateInstance(type), null) as List<ImageSite>);
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(path + "\\site_error.txt", ex.ToString() + "\r\n");
                }
            }
        }

        public static bool CheckUpdate(string version)
        {
            try
            {
                Version remoteVer = new Version(version);
                //bool needUpdate = false;
                string curPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (remoteVer > System.Reflection.Assembly.LoadFile(curPath + "\\SitePack.dll").GetName().Version)
                {
                    MyWebClient web = new MyWebClient();
                    web.DownloadFile("http://moeloader.sinaapp.com/sitepack.php", curPath + "\\SitePack.dll.new");
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static SiteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SiteManager();
                }
                return instance;
            }
        }

        public List<ImageSite> Sites
        {
            get
            {
                return sites;
            }
        }
    }
}

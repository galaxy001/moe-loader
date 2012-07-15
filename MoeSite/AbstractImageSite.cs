using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MoeLoader
{
    /// <summary>
    /// 抽象图片站点
    /// </summary>
    public abstract class AbstractImageSite : ImageSite
    {
        private const int PICWIDTH = 150;

        public abstract string SiteUrl { get; }
        public abstract string SiteName { get; }
        public abstract string ShortName { get; }
        public virtual string Referer { get { return null; } }
        public virtual bool IsSupportCount { get { return true; } }
        public virtual bool IsSupportScore { get { return true; } }
        public virtual bool IsSupportRes { get { return true; } }
        public virtual bool IsSupportPreview { get { return true; } }
        public virtual bool IsSupportTag { get { return true; } }
        public virtual string ToolTip { get { return null; } }

        public virtual bool IsVisible { get { return true; } }

        public virtual System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(PICWIDTH, PICWIDTH); } }
        public virtual System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(PICWIDTH, PICWIDTH); } }

        public virtual System.IO.Stream IconStream
        {
            get
            {
                return GetType().Assembly.GetManifestResourceStream("SitePack.image." + ShortName + ".ico");
            }
        }

        public virtual List<Img> GetImages(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            return GetImages(GetPageString(page, count, keyWord, proxy), proxy);
        }

        public abstract string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        public abstract List<Img> GetImages(string pageString, System.Net.IWebProxy proxy);

        public virtual List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            return new List<TagItem>();
        }

        public virtual List<Img> FilterImg(List<Img> imgs, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, bool showExplicit, bool updateViewed)
        {
            List<Img> re = new List<Img>();
            foreach (Img img in imgs)
            {
                //标记已阅
                img.IsViewed = true;
                if (lastViewed != null && !lastViewed.IsViewed(img.Id))
                {
                    img.IsViewed = false;
                    if (updateViewed)
                        lastViewed.AddViewingId(img.Id);
                }
                else if (maskViewed) continue;

                int res = img.Width * img.Height;
                //score filter & resolution filter & explicit filter
                if (IsSupportScore && img.Score <= maskScore || IsSupportRes && res < maskRes || !showExplicit && img.IsExplicit)
                {
                    continue;
                }
                else
                {
                    re.Add(img);
                }
            }
            return re;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using MoeLoader;

namespace SitePack
{
    public class SiteMjvArt : MoeLoader.AbstractImageSite
    {
        public override string SiteUrl { get { return "http://mjv-art.org"; } }
        public override string SiteName { get { return "mjv-art.org"; } }
        public override string ShortName { get { return "mjv-art"; } }
        //public string Referer { get { return null; } }

        public override bool IsSupportCount { get { return false; } } //fixed 60
        public override bool IsSupportScore { get { return false; } }
        //public bool IsSupportRes { get { return true; } }
        //public bool IsSupportPreview { get { return true; } }
        //public bool IsSupportTag { get { return true; } }

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }

        /// <summary>
        /// mjv-art.org site
        /// </summary>
        public SiteMjvArt()
        {
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            //http://mjv-art.org/pictures/view_posts/0?lang=en
            string url = SiteUrl + "/pictures/view_posts/" + (page - 1) + "?lang=en";

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //http://mjv-art.org/pictures/view_posts/0?search_tag=suzumiya%20haruhi&order_by=date&ldate=0&lang=en
                url = SiteUrl + "/pictures/view_posts/" + (page - 1) + "?search_tag=" + keyWord + "&order_by=date&ldate=0&lang=en";
            }

            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            //retrieve all elements via xpath
            HtmlNodeCollection nodes = doc.DocumentNode.SelectSingleNode("//div[@id='posts']").SelectNodes(".//span");
            if (nodes == null)
            {
                return imgs;
            }

            foreach (HtmlNode imgNode in nodes)
            {
                HtmlNode anode = imgNode.SelectSingleNode("a");
                //details will be extracted from here
                //eg. http://mjv-art.org/pictures/view_post/181876?lang=en
                string detailUrl = anode.Attributes["href"].Value;
                //eg. Anime picture 2000x3246 withblack hair,brown eyes
                string title = anode.Attributes["title"].Value;
                string previewUrl = anode.SelectSingleNode("img").Attributes["src"].Value;

                //extract id from detail url
                string id = System.Text.RegularExpressions.Regex.Match(detailUrl.Substring(detailUrl.LastIndexOf('/') + 1), @"\d+").Value;
                int index = System.Text.RegularExpressions.Regex.Match(title, @"\d+").Index;

                string dimension = title.Substring(index, title.IndexOf(' ', index) - index);
                string tags = title.Substring(title.IndexOf(' ', index) + 1);

                Img img = GenerateImg(detailUrl, previewUrl, dimension, tags.Trim(), id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            //http://mjv-art.org/pictures/autocomplete_tag POST
            List<TagItem> re = new List<TagItem>();
            //no result with length less than 3
            if (word.Length < 3) return re;

            string url = SiteUrl + "/pictures/autocomplete_tag";
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            req.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            req.Proxy = proxy;
            req.Timeout = 8000;
            req.Method = "POST";

            byte[] buf = Encoding.UTF8.GetBytes("tag=" + word);
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            System.IO.Stream str = req.GetRequestStream();
            str.Write(buf, 0, buf.Length);
            str.Close();
            System.Net.WebResponse rsp = req.GetResponse();

            string txt = new System.IO.StreamReader(rsp.GetResponseStream()).ReadToEnd();
            rsp.Close();

            //JSON format response
            //{"tags_list": [{"c": 3, "t": "suzumiya <b>haruhi</b> no yuutsu"}, {"c": 1, "t": "suzumiya <b>haruhi</b>"}]}
            object[] tagList = ((new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(txt) as Dictionary<string, object>)["tags_list"] as object[];
            for (int i = 0; i < tagList.Length && i < 8; i++)
            {
                Dictionary<string, object> tag = tagList[i] as Dictionary<string, object>;
                if (tag["t"].ToString().Trim().Length > 0)
                    re.Add(new TagItem() { Name = tag["t"].ToString().Trim().Replace("<b>", "").Replace("</b>", ""), Count = "N/A" });
            }
          
            return re;
        }

        private Img GenerateImg(string detailUrl, string preview_url, string dimension, string tags, string id)
        {
            int intId = int.Parse(id);

            int width = 0, height = 0;
            try
            {
                //706x1000
                width = int.Parse(dimension.Substring(0, dimension.IndexOf('x')));
                height = int.Parse(dimension.Substring(dimension.IndexOf('x') + 1));
            }
            catch { }

            //convert relative url to absolute
            if (detailUrl.StartsWith("/"))
                detailUrl = SiteUrl + detailUrl;
            if (preview_url.StartsWith("/"))
                preview_url = SiteUrl + preview_url;

            Img img = new Img()
            {
                //Date = "N/A",
                //FileSize = file_size.ToUpper(),
                Desc = tags,
                Id = intId,
                //JpegUrl = preview_url,
                //OriginalUrl = preview_url,
                PreviewUrl = preview_url,
                //SampleUrl = preview_url,
                //Score = 0,
                Width = width,
                Height = height,
                Tags = tags,
            };

            img.DownloadDetail = new DetailHandler((i, p) =>
            {
                //retrieve details
                MyWebClient web = new MyWebClient();
                web.Proxy = p;
                web.Encoding = Encoding.UTF8;
                string page = web.DownloadString(detailUrl);

                //<b>Size:</b> 326.0KB<br>
                int index = page.IndexOf("<b>Size");
                string fileSize = page.Substring(index + 12, page.IndexOf('<', index + 12) - index - 12).Trim();
                //<b>Date Published:</b> 2/24/12 4:57 PM
                index = page.IndexOf("<b>Date Published");
                string date = page.Substring(index + 22, page.IndexOf('<', index + 22) - index - 22).Trim();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(page);
                //retrieve img node
                HtmlNode node = doc.DocumentNode.SelectSingleNode("//div[@id='big_preview_cont']").SelectSingleNode("a");
                string fileUrl = node.Attributes["href"].Value;
                string sampleUrl = node.SelectSingleNode("img").Attributes["src"].Value;

                if (fileUrl.StartsWith("/"))
                    fileUrl = SiteUrl + fileUrl;
                if (sampleUrl.StartsWith("/"))
                    sampleUrl = SiteUrl + sampleUrl;

                i.Date = date;
                i.FileSize = fileSize;
                i.JpegUrl = fileUrl;
                i.OriginalUrl = fileUrl;
                i.SampleUrl = sampleUrl;
            });

            return img;
        }
    }
}

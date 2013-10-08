using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using MoeLoader;

namespace SitePack
{
    public class SiteSankaku : MoeLoader.AbstractImageSite
    {
        private string sitePrefix;

        public override string SiteUrl { get { return "http://" + sitePrefix + ".sankakucomplex.com"; } }
        public override string SiteName { get { return sitePrefix + ".sankakucomplex.com"; } }
        public override string ShortName { get { if (sitePrefix == "chan") return "chan.s"; else return "idol.s"; } }
        public override bool IsSupportScore
        {
            get { return false; }
        }
        public override bool IsSupportCount //fixed 20
        {
            get { return false; }
        }
        public override string Referer { get { return "http://" + sitePrefix + ".sankakucomplex.com/post/show/12345"; } }

        /// <summary>
        /// sankakucomplex site
        /// </summary>
        public SiteSankaku(string prefix)
        {
            sitePrefix = prefix;
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            //http://chan.sankakucomplex.com/post/index.content?page=2&limit=3&tags=xxx
            string url = SiteUrl + "/post/index.content?page=" + page + "&limit=30";

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            //web.Headers["Cookie"] = sessionId;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                url += "&tags=" + keyWord;
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
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(".//span");
            if (nodes == null)
            {
                return imgs;
            }

            foreach (HtmlNode imgNode in nodes)
            {
                HtmlNode anode = imgNode.SelectSingleNode("a");
                HtmlNode imgN = anode.SelectSingleNode("img");
                //details will be extracted from here
                //eg. /post/show/1815296
                string detailUrl = anode.Attributes["href"].Value;
                string tags = imgN.Attributes["title"].Value;
                tags = tags.Substring(0, tags.LastIndexOf("rating", StringComparison.OrdinalIgnoreCase) - 1);
                string previewUrl = imgN.Attributes["src"].Value;

                //extract id from detail url
                string id = System.Text.RegularExpressions.Regex.Match(detailUrl.Substring(detailUrl.LastIndexOf('/') + 1), @"\d+").Value;
                int index = System.Text.RegularExpressions.Regex.Match(tags, @"\d+").Index;

                Img img = GenerateImg(detailUrl, previewUrl, tags.Trim(), id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            List<MoeLoader.TagItem> re = new List<MoeLoader.TagItem>();

            string url = string.Format("http://" + sitePrefix + ".sankakucomplex.com/tag/index.xml?limit={0}&order=count&name={1}", 8, word);
            MoeLoader.MyWebClient web = new MoeLoader.MyWebClient();
            web.Timeout = 8;
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            string xml = web.DownloadString(url);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml.ToString());

            XmlElement root = (XmlElement)(xmlDoc.SelectSingleNode("tags")); //root

            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement tag = (XmlElement)node;

                string name = tag.GetAttribute("name");
                string count = tag.GetAttribute("count");

                re.Add(new MoeLoader.TagItem() { Name = name, Count = count });
            }

            return re;
        }

        private Img GenerateImg(string detailUrl, string preview_url, string tags, string id)
        {
            int intId = int.Parse(id);

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
                Tags = tags,
                DetailUrl = detailUrl
            };

            img.DownloadDetail = (i, p) =>
            {
                //retrieve details
                MyWebClient web = new MyWebClient();
                web.Proxy = p;
                web.Encoding = Encoding.UTF8;
                string page = web.DownloadString(i.DetailUrl);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(page);
                //retrieve img node
                HtmlNodeCollection nodes = doc.DocumentNode.SelectSingleNode("//div[@id='stats']").SelectNodes(".//li");
                foreach (var node in nodes)
                {
                    if (node.InnerText.Contains("Posted"))
                    {
                        i.Date = node.SelectSingleNode("a").Attributes["title"].Value;
                    }
                    else if (node.InnerText.Contains("Resized"))
                    {
                        i.SampleUrl = node.SelectSingleNode("a").Attributes["href"].Value;
                    }
                    else if (node.InnerText.Contains("Original"))
                    {
                        i.OriginalUrl = node.SelectSingleNode("a").Attributes["href"].Value;
                        i.JpegUrl = i.OriginalUrl;
                        //1368x1000 (197.4 KB)
                        string size = node.SelectSingleNode("a").InnerText;
                        i.Dimension = size.Substring(0, size.IndexOf(' '));
                        i.FileSize = size.Substring(size.IndexOf('(') + 1);
                        i.FileSize = i.FileSize.Substring(0, i.FileSize.Length -  1);
                    }
                }
            };

            return img;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using MoeLoader;

namespace SitePack
{
    public class SitePixiv : MoeLoader.AbstractImageSite
    {
        public enum PixivSrcType { Tag, Author, Day, Week, Month, }

        public override string SiteUrl { get { return "http://www.pixiv.net"; } }
        public override string SiteName
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "www.pixiv.net [User]";
                else if (srcType == PixivSrcType.Day)
                    return "www.pixiv.net [Day]";
                else if (srcType == PixivSrcType.Week)
                    return "www.pixiv.net [Week]";
                else if (srcType == PixivSrcType.Month)
                    return "www.pixiv.net [Month]";
                else return "www.pixiv.net [Tag]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "作者搜索";
                else if (srcType == PixivSrcType.Day)
                    return "本日排行";
                else if (srcType == PixivSrcType.Week)
                    return "本周排行";
                else if (srcType == PixivSrcType.Month)
                    return "本月排行";
                else return "最新作品 & 标签搜索";
            }
        }
        public override string ShortName { get { return "pixiv"; } }
        public override string Referer { get { return "http://www.pixiv.net/"; } }

        public override bool IsSupportCount { get { return false; } } //fixed 20
        //public override bool IsSupportScore { get { return false; } }
        public override bool IsSupportRes { get { return false; } }
        //public override bool IsSupportPreview { get { return true; } }
        //public override bool IsSupportTag { get { if (srcType == PixivSrcType.Author) return true; else return false; } }
        public override bool IsSupportTag { get { return false; } }

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }

        private string cookie = null;
        private string[] user = { "moe1user", "moe2user" };
        private string[] pass = { "630489372", "1515817701" };
        private Random rand = new Random();
        private PixivSrcType srcType = PixivSrcType.Tag;

        /// <summary>
        /// pixiv.net site
        /// </summary>
        public SitePixiv(PixivSrcType srcType)
        {
            this.srcType = srcType;
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            //if (page > 1000) throw new Exception("页码过大，若需浏览更多图片请使用关键词限定范围");
            Login(proxy);

            //http://www.pixiv.net/new_illust.php?p=2
            string url = SiteUrl + "/new_illust.php?p=" + page;

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Headers["Cookie"] = cookie;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
                url = SiteUrl + "/search.php?s_mode=s_tag&word=" + keyWord + "&order=date_d&p=" + page;
            }
            if (srcType == PixivSrcType.Author)
            {
                int memberId = 0;
                if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
                {
                    throw new Exception("必须在关键词中指定画师 id；若需要使用标签进行搜索请使用 www.pixiv.net [TAG]");
                }
                //member id
                url = SiteUrl + "/member_illust.php?id=" + memberId + "&p=" + page;
            }
            else if (srcType == PixivSrcType.Day)
            {
                url = SiteUrl + "/ranking.php?mode=day&p=" + page;
            }
            else if (srcType == PixivSrcType.Week)
            {
                url = SiteUrl + "/ranking.php?mode=week&p=" + page;
            }
            else if (srcType == PixivSrcType.Month)
            {
                url = SiteUrl + "/ranking.php?mode=month&p=" + page;
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
            HtmlNodeCollection nodes = null;
            if (srcType == PixivSrcType.Tag)
            {
                nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='image-items autopagerize_page_element']").SelectNodes("li");
                //nodes = nodes[nodes.Count - 1].SelectNodes("li");
            }
            else if (srcType == PixivSrcType.Author)
                nodes = doc.DocumentNode.SelectSingleNode("//div[@class='display_works linkStyleWorks']").SelectSingleNode("ul").SelectNodes("li");
            else //ranking
                nodes = doc.DocumentNode.SelectSingleNode("//section[@class='articles autopagerize_page_element']").SelectNodes("article");

            if (nodes == null)
            {
                return imgs;
            }

            foreach (HtmlNode imgNode in nodes)
            {
                try
                {
                    HtmlNode anode = imgNode.SelectSingleNode("a");
                    //details will be extracted from here
                    //eg. member_illust.php?mode=medium&illust_id=29561307&ref=rn-b-5-thumbnail
                    string detailUrl = anode.Attributes["href"].Value.Replace("amp;", "");
                    string previewUrl = null;
                    if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.Author)
                        previewUrl = anode.SelectSingleNode(".//img").Attributes["src"].Value;
                    else
                        previewUrl = anode.SelectSingleNode(".//img").Attributes["data-src"].Value;

                    if (previewUrl.Contains('?'))
                        previewUrl = previewUrl.Substring(0, previewUrl.IndexOf('?'));

                    //extract id from detail url
                    //string id = detailUrl.Substring(detailUrl.LastIndexOf('=') + 1);
                    string id = System.Text.RegularExpressions.Regex.Match(detailUrl, @"illust_id=\d+").Value;
                    id = id.Substring(id.IndexOf('=') + 1);

                    Img img = GenerateImg(detailUrl, previewUrl, id);
                    if (img != null) imgs.Add(img);
                }
                catch
                {
                    //int i = 0;
                }
            }

            return imgs;
        }

        // DO NOT SUPPORT TAG HINT
        //public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        //{
        //    List<TagItem> re = new List<TagItem>();
        //    return re;
        //}

        private Img GenerateImg(string detailUrl, string preview_url, string id)
        {
            int intId = int.Parse(id);

            if (!detailUrl.StartsWith("http") && !detailUrl.StartsWith("/"))
                detailUrl = "/" + detailUrl;

            //convert relative url to absolute
            if (detailUrl.StartsWith("/"))
                detailUrl = SiteUrl + detailUrl;
            if (preview_url.StartsWith("/"))
                preview_url = SiteUrl + preview_url;

            //string fileUrl = preview_url.Replace("_s.", ".");
            //string sampleUrl = preview_url.Replace("_s.", "_m.");

            //http://i1.pixiv.net/img-inf/img/2013/04/10/00/11/37/34912478_s.png
            //http://i1.pixiv.net/img03/img/tukumo/34912478_m.png
            //http://i1.pixiv.net/img03/img/tukumo/34912478.png

            Img img = new Img()
            {
                //Date = "N/A",
                //FileSize = file_size.ToUpper(),
                //Desc = intId + " ",
                Id = intId,
                //JpegUrl = fileUrl,
                //OriginalUrl = fileUrl,
                PreviewUrl = preview_url,
                //SampleUrl = sampleUrl,
                //Score = 0,
                //Width = width,
                //Height = height,
                //Tags = tags,
            };

            img.DownloadDetail = new DetailHandler((i, p) =>
            {
                //retrieve details
                MyWebClient web = new MyWebClient();
                web.Proxy = p;
                web.Encoding = Encoding.UTF8;
                web.Headers["Cookie"] = cookie;
                web.Headers["Referer"] = Referer;
                string page = web.DownloadString(detailUrl);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(page);

                //04/16/2012 17:44｜600×800｜SAI  or 04/16/2012 17:44｜600×800 or 04/19/2012 22:57｜漫画 6P｜SAI
                i.Date = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[1]").InnerText;
                //总点数
                i.Score = int.Parse(doc.DocumentNode.SelectSingleNode("//dd[@class='score-count']").InnerText);
                //「カルタ＆わたぬき」/「えれっと」のイラスト [pixiv]
                i.Desc += doc.DocumentNode.SelectSingleNode("//title").InnerText.Replace("のイラスト [pixiv]", "").Replace("の漫画 [pixiv]", "").Replace("「", "").Replace("」", "").Replace("/", "_");
                //URLS
                i.SampleUrl = doc.DocumentNode.SelectSingleNode("//div[@class='works_display']").SelectSingleNode(".//img").Attributes["src"].Value;
                i.OriginalUrl = i.SampleUrl.Replace("_m.", "."); ;
                i.JpegUrl = i.OriginalUrl;
                
                //600×800 or 漫画 6P
                string dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                try
                {
                    //706×1000
                    i.Width = int.Parse(dimension.Substring(0, dimension.IndexOf('×')));
                    i.Height = int.Parse(System.Text.RegularExpressions.Regex.Match(dimension.Substring(dimension.IndexOf('×') + 1), @"\d+").Value);
                }
                catch { }
                try
                {
                    if (i.Width == 0 && i.Height == 0)
                    {
                        i.OriginalUrl = i.SampleUrl.Replace("_m.", "_p0.");
                        i.JpegUrl = i.OriginalUrl;
                        //manga list
                        //漫画 6P
                        int index = dimension.IndexOf(' ') + 1;
                        string mangaPart = dimension.Substring(index, dimension.IndexOf('P') - index);
                        int mangaCount = int.Parse(mangaPart);
                        i.Dimension = "Manga " + mangaCount + "P";
                        for (int j = 0; j < mangaCount; j++)
                        {
                            //oriUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_p0." + ext;
                            img.OrignalUrlList.Add(i.OriginalUrl.Replace("_p0", "_p" + j));
                        }
                    }
                }
                catch { }
            });

            return img;
        }

        private void Login(System.Net.IWebProxy proxy)
        {
            if (cookie == null)
            {
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(SiteUrl + "/login.php");
                req.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "POST";
                //prevent 302
                req.AllowAutoRedirect = false;
                //user & pass
                int index = rand.Next(0, user.Length);
                string data = "mode=login&pixiv_id=" + user[index] + "&pass=" + pass[index] + "&skip=1";
                byte[] buf = Encoding.UTF8.GetBytes(data);
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buf.Length;
                System.IO.Stream str = req.GetRequestStream();
                str.Write(buf, 0, buf.Length);
                str.Close();
                System.Net.WebResponse rsp = req.GetResponse();

                //HTTP 302然后返回实际地址
                cookie = rsp.Headers.Get("Set-Cookie");
                if (rsp.Headers.Get("Location") == null || cookie == null)
                {
                    throw new Exception("自动登录失败");
                }
                //Set-Cookie: PHPSESSID=3af0737dc5d8a27f5504a7b8fe427286; expires=Tue, 15-May-2012 10:05:39 GMT; path=/; domain=.pixiv.net
                int sessionIndex = cookie.LastIndexOf("PHPSESSID");
                cookie = cookie.Substring(sessionIndex, cookie.IndexOf(';', sessionIndex) - sessionIndex);
                rsp.Close();
            }
        }
    }
}

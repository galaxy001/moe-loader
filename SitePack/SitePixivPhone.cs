//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Xml;
//using HtmlAgilityPack;
//using MoeLoader;

//namespace SitePack
//{
//    public class SitePixivPhone : MoeLoader.AbstractImageSite
//    {
//        public enum PixivSrcType { Tag, Author, Day, Week, Month, }

//        public override string SiteUrl { get { return "http://www.pixiv.net"; } }
//        public override string SiteName
//        {
//            get
//            {
//                if (srcType == PixivSrcType.Author)
//                    return "www.pixiv.net [User]";
//                else if (srcType == PixivSrcType.Day)
//                    return "www.pixiv.net [Day]";
//                else if (srcType == PixivSrcType.Week)
//                    return "www.pixiv.net [Week]";
//                else if (srcType == PixivSrcType.Month)
//                    return "www.pixiv.net [Month]";
//                else return "www.pixiv.net [Tag]";
//            }
//        }
//        public override string ToolTip
//        {
//            get
//            {
//                if (srcType == PixivSrcType.Author)
//                    return "作者搜索";
//                else if (srcType == PixivSrcType.Day)
//                    return "本日排行";
//                else if (srcType == PixivSrcType.Week)
//                    return "本周排行";
//                else if (srcType == PixivSrcType.Month)
//                    return "本月排行";
//                else return "最新作品 & 标签搜索";
//            }
//        }
//        public override string ShortName { get { return "pixiv"; } }
//        public override string Referer { get { return "http://www.pixiv.net/"; } }

//        public override bool IsSupportCount { get { return false; } } //fixed 50
//        //public override bool IsSupportScore { get { return false; } }
//        public override bool IsSupportRes { get { return false; } }
//        //public override bool IsSupportPreview { get { return true; } }
//        public override bool IsSupportTag { get { if (srcType == PixivSrcType.Author) return true; else return false; } }

//        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
//        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }

//        private string sessionId = null;
//        private string[] user = { "moe1user", "moe2user" };
//        private string[] pass = { "630489372", "1515817701" };
//        private Random rand = new Random();
//        private string siteUrl = "http://iphone.pxv.jp/iphone/";
//        private PixivSrcType srcType = PixivSrcType.Tag;

//        /// <summary>
//        /// pixiv.net phone api
//        /// </summary>
//        public SitePixivPhone(PixivSrcType srcType)
//        {
//            this.srcType = srcType;
//        }

//        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
//        {
//            //if (page > 1000) throw new Exception("页码过大，若需浏览更多图片请使用关键词限定范围");
//            Login(proxy);

//            MyWebClient web = new MyWebClient();
//            web.Proxy = proxy;
//            //web.Headers["Cookie"] = cookie;
//            web.Encoding = Encoding.UTF8;

//            string url = siteUrl + "new_illust.php?dummy=0&PHPSESSID=" + sessionId + "&p=" + page;
//            if (keyWord.Length > 0)
//            {
//                //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
//                url = siteUrl + "search.php?s_mode=s_tag&word=" + keyWord + "&PHPSESSID=" + sessionId + "&p=" + page;
//            }
//            if (srcType == PixivSrcType.Author)
//            {
//                int memberId = 0;
//                if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
//                {
//                    throw new Exception("必须在关键词中指定画师 id。\r\n\r\n您可以通过关键词输入框的自动提示得到画师名称对应的 id，MoeLoader 下载的图片文件名中也包含画师的 id 及名称；\r\n\r\n若需要使用标签进行搜索请使用 www.pixiv.net [TAG]");
//                }
//                //member id
//                url = siteUrl + "member_illust.php?id=" + memberId + "&PHPSESSID=" + sessionId + "&p=" + page;
//            }
//            else if (srcType == PixivSrcType.Day)
//            {
//                url = siteUrl + "ranking.php?mode=day&PHPSESSID=" + sessionId + "&p=" + page;
//            }
//            else if (srcType == PixivSrcType.Week)
//            {
//                url = siteUrl + "ranking.php?mode=week&PHPSESSID=" + sessionId + "&p=" + page;
//            }
//            else if (srcType == PixivSrcType.Month)
//            {
//                url = siteUrl + "ranking.php?mode=month&PHPSESSID=" + sessionId + "&p=" + page;
//            }

//            string pageString = web.DownloadString(url);
//            web.Dispose();

//            return pageString;
//        }

//        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
//        {
//            List<Img> imgs = new List<Img>();

//            /*this.illustId = trim(obj[0]);//イラストID
//            this.memberID = trim(obj[1]);//ユーザーID
//            this.type = trim(obj[2]);// 拡張子
//            this.title = trim(obj[3]);//タイトル
//            this.imgserver = trim(obj[4]);//imgサーバの番号
//            this.author = trim(obj[5]);//作者
//            this.thumbURL = trim(obj[6]);// サムネイルURL
//            this.imageMURL = trim(obj[7]);// 画像URL
//            this.submitted = trim(obj[8]);//投稿日
//            this.tags = '"' + trim(obj[9]) + '"';//タグ
//            this.tool = trim(obj[10]);//ツール
//            this.appreciatedCount = trim(obj[11]);//評価回数
//            this.totalPoint = trim(obj[12]);//総合点
//            this.views = trim(obj[13]);// 閲覧数
//            this.comment = trim(obj[14]);//作者コメント
//            this.url = "http://www.pixiv.net/member_illust.php?mode=medium&illust_id=" + this.illustId;
//            this.imageURL = "http://img" + this.imgserver + ".pixiv.net/img/" + this.thumbURL.split("/")[4] + "/" + this.illustId + "." + this.type;*/

//            /*"26729795","4100026","jpg","GUMI","97","ヒュウガ　千鳥＠マイピク募集中","http://img97.pixiv.net/img/pmrgysrss/mobile/26729795_128x128.jpg"
//             * ,,,"http://img97.pixiv.net/img/pmrgysrss/mobile/26729795_480mw.jpg",,,"2012-04-22 18:26:25","GUMI" "落書き","CGillust",,,,"色塗りの練習でした。そのうち消すかも。
//             * ブックマークが２人以上になったら消しません。・・・。なんだ、この宣言ww",,,,"0","0","pmrgysrss",,"0",*/

//            string allCsv = System.Text.RegularExpressions.Regex.Replace(pageString, "\" \"", ",");
//            //System.Diagnostics.Debug.WriteLine(allCsv);
//            System.IO.Stream csvStr = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(allCsv));
//            Microsoft.VisualBasic.FileIO.TextFieldParser parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(csvStr, Encoding.UTF8);
//            parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
//            parser.SetDelimiters(",");

//            while (!parser.EndOfData)
//            {
//                string[] items = parser.ReadFields();
//                string id = items[0].Replace("\"", "").Trim();
//                string memberId = items[1].Replace("\"", "").Trim();
//                string ext = items[2].Replace("\"", "").Trim();
//                string title = items[3].Replace("\"", "").Trim();
//                string imgsvr = items[4].Replace("\"", "").Trim();
//                //http://img01.pixiv.net/ 返回为1，需补齐为01
//                if (imgsvr.Length == 1) imgsvr = "0" + imgsvr;

//                string author = items[5].Replace("\"", "").Trim();
//                //string previewUrl = items[6].Replace("\"", "").Trim();
//                //string imgMUrl = items[7].Replace("\"", "").Trim();
//                string date = items[12].Replace("\"", "").Trim();
//                //string tags = items[9].Replace("\"", "").Trim();
//                int score = 0;
//                int.TryParse(items[16].Replace("\"", "").Trim(), out score);
//                string detailUrl = "http://www.pixiv.net/member_illust.php?mode=medium&illust_id=" + id;
//                string oriUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "." + ext;
//                bool isManga = !items[19].Equals("");
//                if (isManga)
//                {
//                    //manga
//                    //int mangaCount = 1;
//                    //int.TryParse(items[19], out mangaCount);
//                    oriUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_p0." + ext;
//                }
//                string previewUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_s." + ext;
//                string sampleUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_m." + ext;

//                Img img = new Img()
//                {
//                    Date = date,
//                    //FileSize = file_size.ToUpper(),
//                    Desc = title + " [" + memberId + " " + author + "]",
//                    Id = int.Parse(id),
//                    JpegUrl = oriUrl,
//                    OriginalUrl = oriUrl,
//                    PreviewUrl = previewUrl,
//                    SampleUrl = sampleUrl,
//                    Score = score,
//                    Dimension = "N/A",
//                    //Width = width,
//                    //Height = height,
//                    //Tags = tags,
//                };

//                if (isManga)
//                {
//                    //manga list
//                    int mangaCount = 1;
//                    int.TryParse(items[19], out mangaCount);
//                    for (int i = 0; i < mangaCount; i++)
//                    {
//                        //oriUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_p0." + ext;
//                        img.OrignalUrlList.Add("http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_p" + i + "." + ext);
//                    }
//                }

//                if (!isManga)
//                {
//                    img.DownloadDetail = new DetailHandler((i, p) =>
//                    {
//                        //retrieve Dimension
//                        MyWebClient web = new MyWebClient();
//                        web.Proxy = p;
//                        web.Encoding = Encoding.UTF8;
//                        web.Headers["Cookie"] = "PHPSESSID=" + sessionId;
//                        web.Headers["Referer"] = Referer;
//                        string page = web.DownloadString(detailUrl);

//                        HtmlDocument doc = new HtmlDocument();
//                        doc.LoadHtml(page);

//                        //04/16/2012 17:44｜600×800｜SAI  or 04/16/2012 17:44｜600×800 or 04/19/2012 22:57｜漫画 6P｜SAI
//                        string data1 = doc.DocumentNode.SelectSingleNode("//div[@class='works_data']").SelectSingleNode("p").InnerText;
//                        try
//                        {
//                            int index = data1.IndexOf('｜') + 1;
//                            string dimension = data1.Substring(index).Trim();
//                            //706×1000
//                            i.Width = int.Parse(dimension.Substring(0, dimension.IndexOf('×')));
//                            i.Height = int.Parse(System.Text.RegularExpressions.Regex.Match(dimension.Substring(dimension.IndexOf('×') + 1), @"\d+").Value);
//                            i.Dimension = i.Width + " x " + i.Height;
//                        }
//                        catch { }
//                    });
//                }

//                imgs.Add(img);
//            }

//            return imgs;
//        }

//        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
//        {
//            List<TagItem> re = new List<TagItem>();

//            int id;
//            if (srcType == PixivSrcType.Author && !int.TryParse(word, out id))
//            {
//                //member name
//                string url = siteUrl + "search_user.php?nick=" + word + "&PHPSESSID=" + sessionId + "&p=1";
//                MyWebClient web = new MyWebClient();
//                web.Timeout = 8;
//                web.Proxy = proxy;
//                web.Encoding = Encoding.UTF8;

//                string text = web.DownloadString(url);

//                string allCsv = System.Text.RegularExpressions.Regex.Replace(text, "\" \"", ",");
//                System.IO.Stream csvStr = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(allCsv));
//                Microsoft.VisualBasic.FileIO.TextFieldParser parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(csvStr, Encoding.UTF8);
//                parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
//                parser.SetDelimiters(",");
//                while (!parser.EndOfData && re.Count < 8)
//                {
//                    string[] row = parser.ReadFields();
//                    TagItem item = new TagItem()
//                    {
//                        Name = row[1]
//                    };
//                    re.Add(item);
//                }
//            }

//            return re;
//        }

//        private void Login(System.Net.IWebProxy proxy)
//        {
//            if (sessionId != null) return;
//            try
//            {
//                int index = rand.Next(0, user.Length);
//                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(siteUrl + "login.php?mode=login&pixiv_id=" + user[index] + "&pass=" + pass[index] + "&skip=0");
//                req.UserAgent = "pixiv/1.0 CFNetwork/445.6 Darwin/10.0.0d3";
//                req.Proxy = proxy;
//                req.Timeout = 8000;
//                req.Method = "GET";
//                //prevent 302
//                req.AllowAutoRedirect = false;
//                System.Net.WebResponse rsp = req.GetResponse();

//                //HTTP 302然后返回实际地址
//                sessionId = rsp.Headers.Get("Location");
//                if (sessionId == null)
//                {
//                    throw new Exception("自动登录失败");
//                }
//                //http://iphone.pxv.jp/iphone/index.php?PHPSESSID=501055af61a6a5cd23853bd7b466daca
//                sessionId = sessionId.Substring(sessionId.IndexOf('=') + 1);
//                rsp.Close();
//            }
//            catch (System.Net.WebException)
//            {
//                //invalid user will encounter 404
//                throw new Exception("自动登录失败");
//            }
//        }
//    }
//}

using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;

namespace MoeLoader
{
    /// <summary>
    /// ����Booru����վ��
    /// </summary>
    public class BooruProcessor
    {
        public static void FixImageUrl(string host, ref string url)
        {
            if (url.StartsWith("http:"))
            {
                return;
            }
            if (url.StartsWith("//"))
            {
                url = "http:" + url;
                return;
            }
            if (url.StartsWith("/"))
            {
                url = host + url;
                return;
            }
            string pattern = @"^([\w\d\-]+\.)+[\w\d\-]+\/"; // find domain like : eed98--d.ddk.fdf/
            if (Regex.IsMatch(url, pattern))
            {
                url = "http://" + url;
                return;
            }
            // for other unknown case, do nothing
        }
        //private int mask = -1;
        //private int maskRes = -1;
        //private bool maskViewed = false;
        //private bool showExplicit = false;
        //private int lastViewed = -1;
        //private ViewedID viewedId;
        private SourceType type;

        /// <summary>
        /// ��������
        /// </summary>
        public enum SourceType
        {
            /// <summary>
            /// XML
            /// </summary>
            XML,
            /// <summary>
            /// JSON
            /// </summary>
            JSON,
            /// <summary>
            /// HTML
            /// </summary>
            HTML 
        }

        /// <summary>
        /// ��ȡͼƬԴ��Ϣ
        /// </summary>
        /// <param name="type">��������</param>
        public BooruProcessor(SourceType type)
        {
            //this.mask = mask;
            //this.maskRes = maskRes;
            //this.viewedId = viewedId;
            //this.showExplicit = showExplicit;
            //UseJpeg = useJpeg;
            //Url = url;
            this.type = type;
            //this.maskViewed = maskViewed;
        }

        //private bool stop = false;
        //public string Url { get; set; }
        //public bool UseJpeg { get; set; }

        /// <summary>
        /// Stop retrieving
        /// </summary>
        //public bool Stop
        //{
            //get { return stop; }
            //set { stop = value; }
        //}

        /// <summary>
        /// Retrieving complete
        /// </summary>
        //public event EventHandler processComplete;

        /// <summary>
        /// Retrieve image objects from a url
        /// </summary>
        /// <param name="url">moe imouto post api url, eg. http://moe.imouto.org/post/index.xml?page=3&limit=10 (limit up to 100)</param>
        //public string ProcessSingleLink(System.Net.IWebProxy proxy)
        //{
        //    //try
        //    //{
        //    //string pageString = null;
        //    //if (PreFetcher.Fetcher.PreFetchUrl == Url)
        //    //{
        //    //pageString = PreFetcher.Fetcher.PreFetchedPage;
        //    //}
        //    //else
        //    //{
        //    MyWebClient web = new MyWebClient();

        //    //web.Proxy = MainWindow.GetProxy(web.Proxy);
        //    web.Proxy = proxy;

        //    //web.Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";

        //    web.Encoding = Encoding.UTF8;
        //    string pageString = web.DownloadString(Url);
        //    web.Dispose();

        //    return pageString;
        //    //}

        //    //if (!stop)
        //    //{
        //    //extract properties
        //    //List<Img> imgs = new List<Img>();

        //    //switch (type)
        //    //{
        //    //    case SourceType.HTML:
        //    //        ProcessHTML(Url, pageString, imgs);
        //    //        break;
        //    //    case SourceType.JSON:
        //    //        ProcessJSON(Url, pageString, imgs);
        //    //        break;
        //    //    case SourceType.XML:
        //    //        ProcessXML(Url, pageString, imgs);
        //    //        break;
        //    //}

        //    //return imgs;
        //    //if (processComplete != null)
        //    //processComplete(imgs, null);
        //    //}
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //if (!stop)
        //    //{
        //    //MessageBox.Show(null, "��ȡͼƬ��������: " + e.Message, "Moe Loader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    //if (processComplete != null)
        //    //processComplete(null, null);
        //    //}
        //    //}
        //}

        /// <summary>
        /// ��ȡҳ���е�ͼƬ��Ϣ
        /// </summary>
        /// <param name="url">ҳ���ַ</param>
        /// <param name="pageString">ҳ��Դ����</param>
        /// <returns></returns>
        public List<Img> ProcessPage(string url, string pageString)
        {
            List<Img> imgs = new List<Img>();

            switch (type)
            {
                case SourceType.HTML:
                    ProcessHTML(url, pageString, imgs);
                    break;
                case SourceType.JSON:
                    ProcessJSON(url, pageString, imgs);
                    break;
                case SourceType.XML:
                    ProcessXML(url, pageString, imgs);
                    break;
            }

            return imgs;
        }

        /// <summary>
        /// HTML ��ʽ��Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        private void ProcessHTML(string url, string pageString, List<Img> imgs)
        {
            /* Post.register({"jpeg_height":1200,"sample_width":1333,"md5":"1550bb8d9fa4e1ee7903ee103459f69a","created_at":{"n":666146000,"json_class":"Time","s":1290715184},
             * "status":"active","jpeg_file_size":215756,"sample_height":1000,"score":4,"sample_url":"http://yuinyan.imouto.org/sample/1550bb8d9fa4e459f69a/moe%20163698%20sample.jpg",
             * "actual_preview_height":225,"author":"paku-paku","has_children":false,"change":758975,"height":1200,"sample_file_size":142868,
             * "preview_url":"http://mio3.imouto.org/data/preview/15/50/1550bb8d9fa4e1ee7903ee103459f69a.jpg","tags":"akiyama_mio bikini k-on! swimsuits transparent_png vector_trace",
             * "source":"","width":1600,"rating":"s","jpeg_url":"http://yuinyan.imouto.org/jpeg/1550bb8d9fa4e1ee7903ee103459f69a/moe%20163698%20msuitst_png%20vector_trace.jpg",
             * "preview_width":150,"file_size":113055,"jpeg_width":1600,"preview_height":113,"is_shown_in_index":true,
             * "file_url":"http://yuinyan.imouto.org/image/1550bb8d9fa4e1ee7903ee103459f69a/moe%20163698%20amio%20swimsctor_trace.png",
             * "id":163698,"parent_id":null,"actual_preview_width":300,"creator_id":70875}) */

            //��ǰ�ַ���λ��
            int index = 0;

            while (index < pageString.Length)
            {
                index = pageString.IndexOf("Post.register({", index);
                if (index == -1)
                    break;
                string item = pageString.Substring(index + 14, pageString.IndexOf("})", index) - index - 13);

                #region Analyze json
                //�滻�п��ܸ��ŷ����� [ ] "
                //item = item.Replace('[', '1').Replace(']', '1').Replace("\\\"", "");
                //JSONObject obj = JSONConvert.DeserializeObject(item);
                Dictionary<string, object> obj = (new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(item) as Dictionary<string, object>;

                string sample = "";
                if (obj.ContainsKey("sample_url"))
                    sample = obj["sample_url"].ToString();

                int file_size = 0;
                try
                {
                    if (obj.ContainsKey("file_size"))
                        file_size = Int32.Parse(obj["file_size"].ToString());
                }
                catch { }

                string created_at = "N/A";
                if (obj.ContainsKey("created_at"))
                    created_at = obj["created_at"].ToString();

                string preview_url = obj["preview_url"].ToString();
                string file_url = obj["file_url"].ToString();

                string jpeg_url = file_url;
                if (obj.ContainsKey("jpeg_url"))
                    jpeg_url = obj["jpeg_url"].ToString();

                string tags = obj["tags"].ToString();
                string id = obj["id"].ToString();
                string source = obj["source"].ToString();
                //string width = obj["width"].ToString();
                //string height = obj["height"].ToString();
                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(obj["width"].ToString().Trim());
                    height = int.Parse(obj["height"].ToString().Trim());
                }
                catch { }

                string score = "N/A";
                if (obj.ContainsKey("rating"))
                {
                    score = "Safe ";
                    if (obj["rating"].ToString() == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (obj.ContainsKey("score"))
                        score += obj["score"].ToString();
                }

                string host = url.Substring(0, url.IndexOf('/', 8));

                if (preview_url.StartsWith("/"))
                    preview_url = host + preview_url;
                if (file_url.StartsWith("/"))
                    file_url = host + file_url;
                if (sample.StartsWith("/"))
                    sample = host + sample;
                if (jpeg_url.StartsWith("/"))
                    jpeg_url = host + jpeg_url;

                //if (!UseJpeg)
                //jpeg_url = file_url;

                Img img = GenerateImg(file_url, preview_url, width, height, sample, created_at, tags, file_size, id, score, jpeg_url, source, url);
                if (img != null) imgs.Add(img);
                #endregion

                index += 15;
            }
        }

        /// <summary>
        /// XML ��ʽ��Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        private void ProcessXML(string url, string pageString, List<Img> imgs)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(pageString);

            XmlElement root = null;
            if (xmlDoc.SelectSingleNode("posts") == null)
            {
                root = (XmlElement)(xmlDoc.SelectSingleNode("IbSearch/response")); //root
            }
            else root = (XmlElement)(xmlDoc.SelectSingleNode("posts")); //root

            foreach (XmlNode postN in root.ChildNodes)
            {
                XmlElement post = (XmlElement)postN;

                int file_size = 0;
                try
                {
                    if (post.HasAttribute("file_size"))
                        file_size = Int32.Parse(post.GetAttribute("file_size"));
                }
                catch { }

                string created_at = "N/A";
                if (post.HasAttribute("created_at"))
                    created_at = post.GetAttribute("created_at");

                string preview_url = post.GetAttribute("preview_url");
                string file_url = post.GetAttribute("file_url");

                string jpeg_url = file_url;
                if (post.HasAttribute("jpeg_url"))
                    jpeg_url = post.GetAttribute("jpeg_url");

                string sample = file_url;
                if (post.HasAttribute("sample_url"))
                    sample = post.GetAttribute("sample_url");

                string tags = post.GetAttribute("tags");
                string id = post.GetAttribute("id");
                string source = post.GetAttribute("source");
                //string width = post.GetAttribute("width");
                //string height = post.GetAttribute("height");
                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(post.GetAttribute("width").Trim());
                    height = int.Parse(post.GetAttribute("height").Trim());
                }
                catch { }

                string score = "N/A";
                if (post.HasAttribute("rating"))
                {
                    score = "Safe ";
                    if (post.GetAttribute("rating") == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (post.HasAttribute("score"))
                        score += post.GetAttribute("score");
                }

                string host = url.Substring(0, url.IndexOf('/', 8));



                FixImageUrl(host, ref preview_url);
                FixImageUrl(host, ref file_url);
                FixImageUrl(host, ref sample);
                FixImageUrl(host, ref jpeg_url);

                //if (!UseJpeg)
                    //jpeg_url = file_url;

                Img img = GenerateImg(file_url, preview_url, width, height, sample, created_at, tags, file_size, id, score, jpeg_url, source, url);
                if (img != null) imgs.Add(img);
            }
        }

        /// <summary>
        /// JSON format
        /// </summary>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        private void ProcessJSON(string url, string pageString, List<Img> imgs)
        {
            //JSONArray array = JSONConvert.DeserializeArray(pageString);
            object[] array = (new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(pageString) as object[];
            foreach (object o in array)
            {
                //JSONObject obj = o as JSONObject;
                Dictionary<string, object> obj = o as Dictionary<string, object>;

                string sample = "";
                if (obj.ContainsKey("sample_url"))
                    sample = obj["sample_url"].ToString();

                int file_size = 0;
                try
                {
                    if (obj.ContainsKey("file_size"))
                        file_size = Int32.Parse(obj["file_size"].ToString());
                }
                catch { }

                string created_at = "N/A";
                //if (obj["created_at"))
                //    created_at = obj["created_at");

                string preview_url = obj["preview_url"].ToString();
                string file_url = obj["file_url"].ToString();

                string jpeg_url = file_url;
                if (obj.ContainsKey("jpeg_url"))
                    jpeg_url = obj["jpeg_url"].ToString();

                string tags = obj["tags"].ToString();
                string id = obj["id"].ToString();
                string source = obj["source"].ToString();

                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(obj["width"].ToString().Trim());
                    height = int.Parse(obj["height"].ToString().Trim());
                }
                catch { }

                string score = "N/A";
                if (obj.ContainsKey("rating"))
                {
                    score = "Safe ";
                    if (obj["rating"].ToString() == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (obj.ContainsKey("score"))
                        score += obj["score"].ToString();
                }

                string host = url.Substring(0, url.IndexOf('/', 8));

                if (preview_url.StartsWith("/"))
                    preview_url = host + preview_url;
                if (file_url.StartsWith("/"))
                    file_url = host + file_url;
                if (sample.StartsWith("/"))
                    sample = host + sample;
                if (jpeg_url.StartsWith("/"))
                    jpeg_url = host + jpeg_url;

                //if (!UseJpeg)
                    //jpeg_url = file_url;

                Img img = GenerateImg(file_url, preview_url, width, height, sample, created_at, tags, file_size, id, score, jpeg_url, source, url);
                if (img != null) imgs.Add(img);
            }
        }

        /// <summary>
        /// ���� Img ����
        /// </summary>
        /// <param name="file_url"></param>
        /// <param name="preview_url"></param>
        /// <param name="sample"></param>
        /// <param name="created_at"></param>
        /// <param name="tags"></param>
        /// <param name="file_size"></param>
        /// <param name="id"></param>
        /// <param name="score"></param>
        /// <param name="jpeg_url"></param>
        /// <returns></returns>
        private Img GenerateImg(string file_url, string preview_url, int width, int height, string sample
            , string created_at, string tags, int file_size, string id, string score, string jpeg_url, string src, string url)
        {
            int scoreInt = 0, intId = 0;
            try
            {
                intId = int.Parse(id);
            }
            catch { }
            try
            {
                scoreInt = Int32.Parse(score.Substring(score.IndexOf(' '), score.Length - score.IndexOf(' ')));
            }
            catch { }

            #region DateTime Convert
            //eg. Fri Aug 28 20:05:57 -0600 2009 or 1291280246
            try
            {
                //1291280246   ==   2010/12/2 16:57
                long sec = long.Parse(created_at);
                DateTime startDate = new DateTime(1970, 1, 1, 8, 0, 0, 0);
                created_at = startDate.AddSeconds(sec).ToString();
            }
            catch
            {
                //Thu Dec 31 06:54:54 +0000 2009
                //2012/01/28 01:59:10 -0500
                //1323123123
                //Dec Nov Oct Sep Aug Jul Jun May Apr Mar Feb Jan
                try
                {
                    created_at = DateTime.Parse(created_at).ToString();
                }
                catch { }
            }
            #endregion

            string host = url.Substring(0, url.IndexOf('/', 8));
            string detailUrl = host + "/post/show/" + id;
            if (url.Contains("index.php"))
                detailUrl = host + "/index.php?page=post&s=view&id=" + id;

            Img img = new Img()
            {
                Date = created_at,
                Desc = tags,
                FileSize = file_size > 1048576 ? (file_size / 1048576.0).ToString("0.00MB") : (file_size / 1024.0).ToString("0.00KB"),
                Height = height,
                Id = intId,
                IsExplicit = score.StartsWith("E"),
                JpegUrl = jpeg_url,
                OriginalUrl = file_url,
                PreviewUrl = preview_url,
                SampleUrl = sample,
                Score = scoreInt,
                Source = src,
                Tags = tags,
                Width = width,
                DetailUrl = detailUrl
            };
            return img;
        }
    }
}

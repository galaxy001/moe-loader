﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using MoeLoader;

namespace SitePack
{
    /// <summary>
    /// danbooru.donmai.us, Thanks to Realanan
    /// </summary>
    class SiteDanbooru : MoeLoader.AbstractImageSite
    {
        public override string SiteUrl { get { return "http://donmai.us"; } }
        //http://donmai.us/post?page={0}&limit={1}&tags={2}
        public override string SiteName { get { return "danbooru.donmai.us"; } }
        public override string ShortName { get { return "donmai"; } }

        public virtual bool IsSupportTag { get { return false; } }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            string address = string.Format(SiteUrl + "/posts?page={0}&limit={1}&tags={2}", page, count, keyWord);
            if (keyWord.Length == 0)
            {
                address = address.Substring(0, address.Length - 6);
            }
            MyWebClient client = new MyWebClient
            {
                Proxy = proxy,
                Encoding = Encoding.UTF8
            };
            string pageString = client.DownloadString(address);
            client.Dispose();
            return pageString;
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            List<Img> list = new List<Img>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(pageString);
            HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//article");
            if (nodes == null)
            {
                return list;
            }

            foreach (HtmlNode node in nodes)
            {
                HtmlNode node2 = node.SelectSingleNode("a");
                HtmlNode node3 = node2.SelectSingleNode("img");

                Img item = new Img()
                {
                    Desc = node.Attributes["data-tags"].Value,
                    Height = Convert.ToInt32(node.Attributes["data-height"].Value),
                    Id = Convert.ToInt32(node.Attributes["data-id"].Value),
                    IsExplicit = node.Attributes["data-rating"].Value == "e",
                    Tags = node.Attributes["data-tags"].Value,
                    Width = Convert.ToInt32(node.Attributes["data-width"].Value),
                    PreviewUrl = this.SiteUrl + node3.Attributes["src"].Value,
                };

                item.DownloadDetail = delegate(Img i, System.Net.IWebProxy p)
                {
                    string html = new MyWebClient { Proxy = p, Encoding = Encoding.UTF8 }.DownloadString(this.SiteUrl + node2.Attributes["href"].Value);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    HtmlNodeCollection sectionNodes = doc.DocumentNode.SelectNodes("//section");
                    foreach (HtmlNode n in sectionNodes)
                    {
                        var ns = n.SelectNodes(".//li");
                        if (ns == null) continue;
                        foreach (HtmlNode n1 in ns)
                        {
                            if (n1.InnerText.Contains("Date:"))
                            {
                                i.Date = n1.SelectSingleNode(".//time").Attributes["title"].Value;
                            }
                            if (n1.InnerText.Contains("Size:"))
                            {
                                i.OriginalUrl = this.SiteUrl + n1.SelectSingleNode(".//a").Attributes["href"].Value;
                                i.JpegUrl = i.OriginalUrl;
                                i.FileSize = n1.SelectSingleNode(".//a").InnerText;
                                i.Dimension = n1.InnerText.Substring(n1.InnerText.IndexOf('(') + 1, n1.InnerText.LastIndexOf(')') - n1.InnerText.IndexOf('(') - 1);
                            }
                            if (n1.InnerText.Contains("Score:"))
                            {
                                i.Score = Convert.ToInt32(n1.SelectSingleNode(".//span").InnerText);
                            }
                        }
                    }
                    i.SampleUrl = this.SiteUrl + doc.DocumentNode.SelectSingleNode("//img[@id='image']").Attributes["src"].Value;
                };
                list.Add(item);
            }
            
            return list;
        }
    }
}

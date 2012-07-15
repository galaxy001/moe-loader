using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoeLoader
{
    public struct TagItem
    {
        public string Name;
        public string Count;
    }

    public interface ImageSite
    {
        /// <summary>
        /// eg. http://yande.re
        /// </summary>
        string SiteUrl { get; }
        /// <summary>
        /// eg. yande.re
        /// </summary>
        string SiteName { get; }

        /// <summary>
        /// eg. yandere
        /// </summary>
        string ShortName { get; }

        /// <summary>
        /// referer, or null
        /// </summary>
        string Referer { get; }

        string ToolTip { get; }

        /// <summary>
        /// 是否支持单页数量
        /// </summary>
        bool IsSupportCount { get; }
        /// <summary>
        /// 是否支持评分
        /// </summary>
        bool IsSupportScore { get; }
        /// <summary>
        /// 是否支持分辨率
        /// </summary>
        bool IsSupportRes { get; }
        /// <summary>
        /// 是否支持预览图
        /// </summary>
        bool IsSupportPreview { get; }
        /// <summary>
        /// 是否支持jpeg格式图片
        /// </summary>
        //bool IsSupportJpeg { get; }
        /// <summary>
        /// 是否支持tag列表查询
        /// </summary>
        bool IsSupportTag { get; }

        /// <summary>
        /// 大缩略图尺寸
        /// </summary>
        System.Drawing.Point LargeImgSize { get; }
        /// <summary>
        /// 小缩略图尺寸
        /// </summary>
        System.Drawing.Point SmallImgSize { get; }

        /// <summary>
        /// visible site or not
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// image favi icon
        /// </summary>
        System.IO.Stream IconStream { get; }

        /// <summary>
        /// 获取图片
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="count">单页数量(可能不支持)</param>
        /// <param name="keyWord">关键词</param>
        /// <param name="maskScore">屏蔽的目标分数(可能不支持)</param>
        /// <param name="maskRes">屏蔽的目标分辨率(可能不支持)</param>
        /// <param name="lastViewed">浏览到的位置id</param>
        /// <param name="maskViewed">是否屏蔽已浏览的</param>
        /// <param name="proxy">代理</param>
        /// <param name="showExplicit">显示explicit评分图片(可能不支持)</param>
        /// <returns>图片集合</returns>
        List<Img> GetImages(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        /// <summary>
        /// 获取页面的代码
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        /// <summary>
        /// 从页面代码获取图片
        /// </summary>
        /// <param name="pageString"></param>
        /// <param name="maskScore"></param>
        /// <param name="maskRes"></param>
        /// <param name="lastViewed"></param>
        /// <param name="maskViewed"></param>
        /// <param name="proxy"></param>
        /// <param name="showExplicit"></param>
        /// <returns></returns>
        List<Img> GetImages(string pageString, System.Net.IWebProxy proxy);

        /// <summary>
        /// 获取tag提示列表
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        List<TagItem> GetTags(string word, System.Net.IWebProxy proxy);

        List<Img> FilterImg(List<Img> imgs, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, bool showExplicit, bool updateViewed);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MoeLoader
{
    /// <summary>
    /// 图片详细信息获取代理
    /// </summary>
    /// <param name="img">目标图片对象</param>
    /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
    public delegate void DetailHandler(Img img, System.Net.IWebProxy proxy);

    /// <summary>
    /// 表示一张图片及其相关信息
    /// </summary>
    public class Img
    {
        /// <summary>
        /// 原图地址
        /// </summary>
        public string OriginalUrl { get; set; }

        /// <summary>
        /// 缩略图地址
        /// </summary>
        public string PreviewUrl { get; set; }

        /// <summary>
        /// 原图地址集合
        /// 若原图地址不止一个（例如pixiv的漫画），则将多个地址置于此处，该列表不为空时 OriginalUrl 将被忽略
        /// </summary>
        public List<string> OrignalUrlList { get; set; }

        /// <summary>
        /// 原图宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 原图高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 预览图地址，尺寸位于原图与缩略图之间
        /// </summary>
        public string SampleUrl { get; set; }

        /// <summary>
        /// 图片创建日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 图片Tags
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// 原图文件尺寸
        /// </summary>
        public string FileSize { get; set; }

        /// <summary>
        /// 图片id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 图片得分
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 图片来源
        /// </summary>
        public string Source { set; get; }

        /// <summary>
        /// 图片描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// JPEG格式原图地址（非所有站点支持，不支持时请与 OriginalUrl 保持一致）
        /// </summary>
        public string JpegUrl { get; set; }

        private string dimension = null;
        /// <summary>
        /// 原图分辨率
        /// </summary>
        public string Dimension
        {
            get
            {
                if (dimension == null)
                    return Width + " x " + Height;
                else return dimension;
            }
            set { dimension = value; }
        }

        /// <summary>
        /// 是否已浏览过
        /// </summary>
        public bool IsViewed { get; set; }

        /// <summary>
        /// 是否Explicit内容
        /// </summary>
        public bool IsExplicit { get; set; }

        /// <summary>
        /// 若图片的某些信息需要单独获取（例如原图URL可能位于第二层页面），则实现该接口，将网络操作、提取信息操作置于此处
        /// </summary>
        public DetailHandler DownloadDetail;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Img()
        {
            this.Date = "N/A";
            this.Desc = "";
            this.FileSize = "N/A";
            this.Height = 0;
            this.Id = 0;
            this.IsExplicit = false;
            this.IsViewed = false;
            this.JpegUrl = "";
            this.OriginalUrl = "";
            this.PreviewUrl = "";
            this.SampleUrl = "";
            this.Score = 0;
            this.Source = "";
            this.Tags = "";
            this.Width = 0;
            this.OrignalUrlList = new List<string>();
        }
    }
}

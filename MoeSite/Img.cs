using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MoeLoader
{
    /// <summary>
    /// Download img detail handler
    /// </summary>
    /// <param name="img">the image</param>
    /// <param name="proxy">proxy</param>
    public delegate void DetailHandler(Img img, System.Net.IWebProxy proxy);

    /// <summary>
    /// 表示一张图片
    /// </summary>
    public class Img
    {
        /// <summary>
        /// original pic url
        /// </summary>
        public string OriginalUrl { get; set; }

        /// <summary>
        /// preview url
        /// </summary>
        public string PreviewUrl { get; set; }

        /// <summary>
        /// original pic url list. OriginalUrl will be ignored if this list isn't empty
        /// </summary>
        public List<string> OrignalUrlList { get; set; }

        /// <summary>
        /// pic width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// pic height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// sample pic url, size between preview and original
        /// </summary>
        public string SampleUrl { get; set; }

        /// <summary>
        /// created date
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// tags
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// original pic file size (calculated)
        /// </summary>
        public string FileSize { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// rating & score
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Image source
        /// </summary>
        public string Source { set; get; }

        /// <summary>
        /// description
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// jpef format url
        /// </summary>
        public string JpegUrl { get; set; }

        private string dimension = null;
        /// <summary>
        /// Dimension
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
        /// Download image detail
        /// </summary>
        public DetailHandler DownloadDetail;

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

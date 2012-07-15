using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MoeLoader
{
    /// <summary>
    /// 下载状态
    /// </summary>
    public enum DLStatus { Success, Failed, DLing, Wait }

    /// <summary>
    /// 下载任务，用于界面绑定
    /// </summary>
    public class DownloadItem : INotifyPropertyChanged
    {
        private string size;
        private double progress;
        private DLStatus statusE;
        private double speed;

        public string FileName { get; set; }

        /// <summary>
        /// 大小
        /// </summary>
        public string Size
        {
            get { return size; }
            set
            {
                size = value;
                OnPropertyChanged("Size");
            }
        }

        /// <summary>
        /// 进度
        /// </summary>
        public double Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        /// <summary>
        /// 状态（图形表示）
        /// </summary>
        public string Status
        {
            get
            {
                switch (StatusE)
                {
                    case DLStatus.Wait:
                        return "/Images/wait.png";
                    case DLStatus.Success:
                        return "/Images/success.png";
                    case DLStatus.Failed:
                        return "/Images/failed.png";
                    case DLStatus.DLing:
                        return "/Images/dling.png";
                    default:
                        return "/Images/wait.png";
                }
            }
        }

        /// <summary>
        /// 状态
        /// </summary>
        public DLStatus StatusE
        {
            get { return statusE; }
            set
            {
                statusE = value;
                OnPropertyChanged("Status");
                if (value != DLStatus.DLing)
                    SetSpeed(0.0);
            }
        }

        public string Url { get; set; }

        public string Speed
        {
            get
            {
                if (statusE == DLStatus.DLing)
                    return speed.ToString("0.00") + " KB/s";
                else return "";
            }
        }

        public void SetSpeed(double sp)
        {
            this.speed = sp;
            OnPropertyChanged("Speed");
        }

        public DownloadItem(string fileName, string url)
        {
            FileName = fileName;
            Size = "N/A";
            Progress = 0;
            StatusE = DLStatus.Wait;
            Url = url;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

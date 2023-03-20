using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Report
    {
        public long ID { get; set; }
        public string ReportDescribe { get; set; }
        public string Code { get; set; }
        public string Parameters { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ProductCost
    {
        public long ID { get; set; }
        public long ProductID { get; set; }
        public long TypeOfActivityID { get; set; }
        public decimal Cost { get; set; }
        public decimal Outlay { get; set; }

        public virtual Product Product { get; set; }
        public virtual TypeOfActivity TypeOfActivity { get; set; }

        private string _code = "";
        [NotMapped]
        public string Code
        {
            get => TypeOfActivity?.Code ?? _code;
            set => _code = value;
        }
        private string _name = "";

        [NotMapped]
        public string Name
        {
            get => TypeOfActivity?.Name ?? _name;
            set => _name = value;
        }

        [NotMapped]
        public decimal Margin
        {
            get => Cost - Outlay;
        }
    }
}

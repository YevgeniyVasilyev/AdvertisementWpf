using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ProductType
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public long? CategoryOfProductID { get; set; }

        [ForeignKey("CategoryOfProductID")]
        public virtual CategoryOfProduct CategoryOfProduct { get; set; }
        //[NotMapped]
        //public string CategoryOfProductName { get; set; }
    }
}

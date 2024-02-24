using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{

    public partial class Product
    {
        public long ID { get; set; }
        public long OrderID { get; set; }
        public long ProductTypeID { get; set; }
        public string Parameters { get; set; }
        public string Note { get; set; }
        public string Files { get; set; }
        //public short Quantity { get; set; }
        public long? DesignerID { get; set; }

        public virtual Order Order { get; set; }
        //public virtual ICollection<ProductCost> ProductCosts { get; set; }
        //public virtual ProductType ProductType { get; set; }
        public virtual User Designer { get; set; }
        public virtual TechCard TechCard { get; set; }
        //public virtual ICollection<ProductCost> Costs { get; set; }
        //public virtual ICollection<TechCard> TechCards { get; set; }
    }
}

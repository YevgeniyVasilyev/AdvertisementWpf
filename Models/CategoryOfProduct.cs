using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class CategoryOfProduct
    {
        public CategoryOfProduct()
        {
            ProductTypes = new HashSet<ProductType>();
        }

        public long ID { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ProductType> ProductTypes { get; set; }
    }
}

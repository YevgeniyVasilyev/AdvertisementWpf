using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class TypeOfActivityInProduct
    {
        public long ID { get; set; }
        public long ProductTypeID { get; set; }
        public long TypeOfActivityID { get; set; }

        public virtual TypeOfActivity TypeOfActivity { get; set; }
        public virtual ProductType ProductTypes { get; set; }
    }
}

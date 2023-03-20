using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ProductionArea
    {
        public ProductionArea()
        {
            Operations = new HashSet<Operation>();
            TypeOfActivityInProdAreas = new HashSet<TypeOfActivityInProdArea>();
        }

        public long ID { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Operation> Operations { get; set; }
        public virtual ICollection<TypeOfActivityInProdArea> TypeOfActivityInProdAreas { get; set; }
    }
}

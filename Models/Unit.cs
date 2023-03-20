using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Unit
    {
        public Unit()
        {
            ParameterInProductTypes = new HashSet<ParameterInProductType>();
            ParameterInOperations = new HashSet<ParameterInOperation>();
        }

        public long ID { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ParameterInProductType> ParameterInProductTypes { get; set; }
        public virtual ICollection<ParameterInOperation> ParameterInOperations { get; set; }
    }
}

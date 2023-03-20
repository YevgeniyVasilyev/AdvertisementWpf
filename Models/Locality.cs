using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Locality
    {
        public Locality()
        {
            Banks = new HashSet<Bank>();
        }

        public long ID { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Bank> Banks { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ReferencebookApplicability
    {
        public long ID { get; set; }
        public long ReferencebookID { get; set; }
        public long? CategoryOfProductID { get; set; }
        public long? TypeOfActivityID { get; set; }

        public virtual Referencebook Referencebook { get; set; }

        [NotMapped]
        public string CategoryOfProductName { get; set; } = "";
        [NotMapped]
        public string TypeOfActivityName { get; set; } = "";
    }
}

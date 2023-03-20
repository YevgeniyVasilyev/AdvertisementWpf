using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ReferencebookParameter
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public long ReferencebookID { get; set; }
        public string Value { get; set; }

        public virtual Referencebook Referencebook { get; set; }
    }
}

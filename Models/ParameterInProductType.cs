using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ParameterInProductType
    {
        public long ID { get; set; }
        public long ProductTypeID { get; set; }
        public string Name { get; set; }
        public long? UnitID { get; set; }
        public long? ReferencebookID { get; set; }
        public bool IsRefbookOnRequest { get; set; }
        public bool IsRequired { get; set; }

        public virtual ProductType ProductType { get; set; }
        public virtual Unit Unit { get; set; }
    }
}

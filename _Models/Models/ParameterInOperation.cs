using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class ParameterInOperation
    {
        public long ID { get; set; }
        public long OperationID { get; set; }
        public string Name { get; set; }
        public long? UnitID { get; set; }
        public long? ReferencebookID { get; set; }
        public bool IsRefbookOnRequest { get; set; } = false;

        public virtual Operation Operation { get; set; }
        public virtual Referencebook Referencebook { get; set; }
        public virtual Unit Unit { get; set; }
    }
}

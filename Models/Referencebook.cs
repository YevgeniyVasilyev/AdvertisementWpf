using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Referencebook
    {
        public Referencebook()
        {
            ReferencebookApplicabilities = new HashSet<ReferencebookApplicability>();
            ReferencebookParameters = new HashSet<ReferencebookParameter>();
            ParameterInOperations = new HashSet<ParameterInOperation>();
        }

        public long ID { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ReferencebookApplicability> ReferencebookApplicabilities { get; set; }
        public virtual ICollection<ReferencebookParameter> ReferencebookParameters { get; set; }
        public virtual ICollection<ParameterInOperation> ParameterInOperations { get; set; }
    }
}

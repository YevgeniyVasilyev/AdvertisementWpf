using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Operation
    {
        public Operation()
        {
            OperationInWorks = new HashSet<OperationInWork>();
            ParameterInOperations = new HashSet<ParameterInOperation>();
            TypeOfActivityInOperations = new HashSet<TypeOfActivityInOperation>();
        }

        public long ID { get; set; }
        public string Name { get; set; }
        public long? ProductionAreaID { get; set; }

        public virtual ProductionArea ProductionArea { get; set; }
        public virtual ICollection<OperationInWork> OperationInWorks { get; set; }
        public virtual ICollection<ParameterInOperation> ParameterInOperations { get; set; }
        public virtual ICollection<TypeOfActivityInOperation> TypeOfActivityInOperations { get; set; }
    }
}

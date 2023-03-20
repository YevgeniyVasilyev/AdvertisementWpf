using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class TypeOfActivity
    {
        public TypeOfActivity()
        {
            TypeOfActivityInProdAreas = new HashSet<TypeOfActivityInProdArea>();
            TypeOfActivityInOperations = new HashSet<TypeOfActivityInOperation>();
            WorkInTechCards = new HashSet<WorkInTechCard>();
        }

        public long ID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public virtual ICollection<WorkInTechCard> WorkInTechCards { get; set; }
        public virtual ICollection<TypeOfActivityInProdArea> TypeOfActivityInProdAreas { get; set; }
        public virtual ICollection<TypeOfActivityInOperation> TypeOfActivityInOperations { get; set; }

        public string CodeName => $"{Code.Trim()} | {Name}";
    }
}

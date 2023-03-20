using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class TypeOfActivityInOperation
    {
        public long ID { get; set; }
        public long OperationID { get; set; }
        public long TypeOfActivityID { get; set; }

        public virtual Operation Operation { get; set; }
        public virtual TypeOfActivity TypeOfActivity { get; set; }

        private string _typeOfActivityCodeName { get; set; } = "";
        [NotMapped]
        public string TypeOfActivityCodeName
        {
            set => _typeOfActivityCodeName = value;
            get => TypeOfActivity?.CodeName ?? _typeOfActivityCodeName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class TypeOfActivityInProdArea
    {
        public long ID { get; set; }
        public long ProductionAreaID { get; set; }
        public long TypeOfActivityID { get; set; }

        public virtual ProductionArea ProductionArea { get; set; }
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class WorkInTechCard : TreeViewItemBase
    {
        public WorkInTechCard()
        {
            //OperationInWorks = new HashSet<OperationInWork>();
            OperationInWorks = new List<OperationInWork> { };
        }

        public long ID { get; set; }
        public long TechCardID { get; set; }
        public long? TypeOfActivityID { get; set; }
        public string Number { get; set; } = "";
        public string Name { get; set; } = "";
        [Column(TypeName = "datetime")]
        public DateTime? DatePlanCompletion { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateFactCompletion { get; set; }

        public virtual TechCard TechCard { get; set; }
        public virtual TypeOfActivity TypeOfActivity { get; set; }
        //public virtual ICollection<OperationInWork> OperationInWorks { get; set; }

        private ObservableCollection<OperationInWork> operationInWorks_;
        [NotMapped]
        public ObservableCollection<OperationInWork> OperationInWorks_
        {
            get => operationInWorks_;
            set
            {
                operationInWorks_ = new ObservableCollection<OperationInWork>(OperationInWorks);
                NotifyPropertyChanged("OperationInWorks_");
            }
        }

        private ObservableCollection<OperationInWork> _operationInWorks;
        public ICollection<OperationInWork> OperationInWorks
        { 
            get => _operationInWorks;
            set
            {
                _operationInWorks = new ObservableCollection<OperationInWork>(value);
                NotifyPropertyChanged("OperationInWorks");
            }
        }

        [NotMapped]
        public decimal ProductCost => TechCard?.ProductCost ?? 0;
        [NotMapped]
        public string TypeOfActivity_CodeName => TypeOfActivity?.CodeName ?? "";
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class TechCard : TreeViewItemBase
    {
        public TechCard()
        {
            WorkInTechCards = new HashSet<WorkInTechCard>();
        }

        public long ID { get; set; }
        public long ProductID { get; set; }
        public string Number { get; set; } = "";
        public string Note { get; set; } = "";

        public virtual Product Product { get; set; }
        //public virtual ICollection<WorkInTechCard> WorkInTechCards { get; set; }

        private ObservableCollection<WorkInTechCard> workInTechCards_;
        [NotMapped]
        public ObservableCollection<WorkInTechCard> WorkInTechCards_
        {
            get => workInTechCards_;
            set
            {
                workInTechCards_ = new ObservableCollection<WorkInTechCard>(WorkInTechCards);
                NotifyPropertyChanged("WorkInTechCards_");
            }
        }

        private ObservableCollection<WorkInTechCard> _workInTechCards { get; set; } = new ObservableCollection<WorkInTechCard> { };
        public ICollection<WorkInTechCard> WorkInTechCards
        {
            get => _workInTechCards;
            set
            {
                _workInTechCards = new ObservableCollection<WorkInTechCard>(value);
                NotifyPropertyChanged("WorkInTechCards");
            }
        }

        [NotMapped]
        public decimal ProductCost => Product?.Cost ?? 0;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Account
    {
        public Account()
        {
            Acts = new HashSet<Act>();
            Payments = new HashSet<Payment>();
        }
        public long ID { get; set; }
        public long OrderID { get; set; }
        public string AccountNumber { get; set; } = "";
        [Column(TypeName = "datetime")]
        public DateTime? AccountDate { get; set; } = DateTime.Now;
        private long? _contractorID = 0;
        public long? ContractorID 
        {
            get => _contractorID;
            set
            {
                if (_contractorID != value)
                {
                    _contractorID = value;
                    NotifyPropertyChanged("ContractorName");
                }
            }
        }
        public string Footing { get; set; } = "";
        public bool IsManual { get; set; } = false;
        public string Details { get; set; } = "";
        [Column(TypeName = "datetime")]
        public DateTime? PayBeforeDate { get; set; }

        public virtual Order Order { get; set; }
        public virtual Contractor Contractor { get; set; }
        public virtual ICollection<Act> Acts { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
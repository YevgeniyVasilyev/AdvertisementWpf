using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Contractor : INotifyPropertyChanged
    {
        public Contractor()
        {
            Accounts = new HashSet<Account>();
        }

        public long ID { get; set; }
        public string Name { get; set; }
        public string NameForUI { get; set; }
        public string DirectorName { get; set; }
        public string DirectorPostName { get; set; }
        public string DirectorAttorneyNumber { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DirectorAttorneyDate { get; set; }
        public string ChiefAccountant { get; set; }
        public string ChiefAccountantPostName { get; set; }
        public string ChiefAccountantAttorneyNumber { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? ChiefAccountantAttorneyDate { get; set; }
        public string BusinessAddress { get; set; }
        public string INN { get; set; }
        public string KPP { get; set; }
        public string OKPO { get; set; }
        public string OGRN { get; set; }
        public string BankAccount { get; set; }
        public long? BankID { get; set; }
        public string AbbrForAcc { get; set; } = "";
        private string _AccountFileTemplate { get; set; } = "";
        public string AccountFileTemplate
        {
            get => _AccountFileTemplate;
            set
            {
                _AccountFileTemplate = value;
                NotifyPropertyChanged("AccountFileTemplate");
            }
        }

        public virtual Bank Bank { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

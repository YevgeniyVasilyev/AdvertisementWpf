using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Client
    {
        public Client()
        {
            Orders = new HashSet<Order>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Profile { get; set; } = "";
        private byte _formProperty = 1;
        public byte FormProperty
        { 
            get => _formProperty;
            set
            {
                if (_formProperty != value)
                {
                    _formProperty = value;
                    NotifyPropertyChanged("IsPrivate");
                    NotifyPropertyChanged("IsLegal");
                    NotifyPropertyChanged("IsIndividual");
                    NotifyPropertyChanged("IsBudget");
                }
            } 
        }
        public string DirectorName { get; set; } = "";
        public string ContactPersonName { get; set; } = "";
        public string PostalAddress { get; set; } = "";
        public string BusinessAddress { get; set; } = "";
        public string ActualAddress { get; set; } = "";
        public string Consignee { get; set; } = "";
        private bool _consigneeIsSame = false;
        public bool ConsigneeIsSame
        {
            get => _consigneeIsSame;
            set
            {
                if (_consigneeIsSame != value)
                {
                    _consigneeIsSame = value;
                    NotifyPropertyChanged("ConsigneeIsSame");
                }
            }
        }
        public string INN { get; set; } = "";
        public string KPP { get; set; } = "";
        public string PersonalAccount { get; set; } = "";
        public string BankAccount { get; set; } = "";
        public long? BankID { get; set; }
        public bool IsActive { get; set; }
        public byte[] Note { get; set; }
        public long UserID { get; set; }
        public string MobilePhone { get; set; } = "";
        public string WorkPhone { get; set; } = "";
        public string Email { get; set; } = "";
        public string AdditionalInfo { get; set; } = "";
        public string ConsigneeName { get; set; } = "";
        public string ConsigneeINN { get; set; } = "";
        public string ConsigneeKPP { get; set; } = "";
        public string ConsigneeBusinessAddress { get; set; } = "";
        public string ConsigneeWorkPhone { get; set; } = "";
        public string ConsigneeBankAccount { get; set; } = "";
        public string ConsigneePersonalAccount { get; set; } = "";
        public long? ConsigneeBankID { get; set; }

        public virtual Bank Bank { get; set; }
        public virtual Bank ConsigneeBank { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}

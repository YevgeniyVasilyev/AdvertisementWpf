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
        private string _ActFileTemplate { get; set; } = "";
        public string ActFileTemplate
        {
            get => _ActFileTemplate;
            set
            {
                _ActFileTemplate = value;
                NotifyPropertyChanged("ActFileTemplate");
            }
        }
        private string _SFFileTemplate { get; set; } = "";
        public string SFFileTemplate
        {
            get => _SFFileTemplate;
            set
            {
                _SFFileTemplate = value;
                NotifyPropertyChanged("SFFileTemplate");
            }
        }
        private string _TNFileTemplate { get; set; } = "";
        public string TNFileTemplate
        {
            get => _TNFileTemplate;
            set
            {
                _TNFileTemplate = value;
                NotifyPropertyChanged("TNFileTemplate");
            }
        }
        private string _UPDFileTemplate { get; set; } = "";
        public string UPDFileTemplate
        {
            get => _UPDFileTemplate;
            set
            {
                _UPDFileTemplate = value;
                NotifyPropertyChanged("UPDFileTemplate");
            }
        }
        private bool _isVATpayer = false;
        public bool IsVATpayer
        { 
            get => _isVATpayer;
            set
            {
                _isVATpayer = value;
                NotifyPropertyChanged("IsVATpayer");
            }
        }
        private byte _VATrate = 0;
        public byte VATrate
        {
            get => _VATrate;
            set
            {
                _VATrate = value;
                NotifyPropertyChanged("VATrate");
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

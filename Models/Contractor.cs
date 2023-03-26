using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Contractor
    {
        public Contractor()
        {
            Accounts = new HashSet<Account>();
        }

        public long ID { get; set; }
        public string Name { get; set; }
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
        public string AccountFileTemplate { get; set; } = "";

        public virtual Bank Bank { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }

        [NotMapped]
        public string ContractorInfoForAccount
        {
            get => $"{Name}, ИНН {INN}, КПП {KPP}, {BusinessAddress}";
        }
        [NotMapped]
        public string ContractorInfoForAct
        {
            get => $"{Name}, ИНН {INN}, {BusinessAddress}, р/с {BankAccount}, в банке {Bank.Name}, БИК {Bank.BIK}, к/с {Bank.CorrAccount}";
        }

        [NotMapped]
        public string DirectorShortName
        {
            get
            {
                string[] aDn = DirectorName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (aDn.Length == 3)
                {
                    return $"{aDn[0]} {aDn[1].Substring(0, 1)}. {aDn[2].Substring(0, 1)}.";
                }
                else if (aDn.Length == 2)
                {
                    return $"{aDn[0]} {aDn[1].Substring(0, 1)}.";
                }
                return string.IsNullOrWhiteSpace(DirectorName) ? "" : DirectorName;
            }
        }
        [NotMapped]
        public string ChiefAccountantShortName
        {
            get
            {
                string[] aCa = ChiefAccountant.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (aCa.Length == 3)
                {
                    return $"{aCa[0]} {aCa[1].Substring(0, 1)}. {aCa[2].Substring(0, 1)}.";
                }
                else if (aCa.Length == 2)
                {
                    return $"{aCa[0]} {aCa[1].Substring(0, 1)}.";
                }
                
                return string.IsNullOrWhiteSpace(ChiefAccountant) ? "" : ChiefAccountant;
            }
        }
    }
}

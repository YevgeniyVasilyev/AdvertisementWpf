using System;
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

        public long ID { get; set; }
        public string Name { get; set; }
        public string Profile { get; set; } = "";
        public bool IsIndividual { get; set; }
        public string DirectorName { get; set; } = "";
        public string ContactPersonName { get; set; } = "";
        public string PostalAddress { get; set; } = "";
        public string BusinessAddress { get; set; } = "";
        public string ActualAddress { get; set; } = "";
        public string Consignee { get; set; } = "";
        public string INN { get; set; } = "";
        public string KPP { get; set; } = "";
        public string BankAccount { get; set; } = "";
        public long? BankID { get; set; }
        public bool IsActive { get; set; }
        public byte[] Note { get; set; }
        public long UserID { get; set; }
        public string MobilePhone { get; set; } = "";
        public string WorkPhone { get; set; } = "";
        public string Email { get; set; } = "";

        public virtual Bank Bank { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Order> Orders { get; set; }

        [NotMapped]
        public string ClientInfoForAccount
        {
            get => $"{Name}, ИНН {INN}, КПП {KPP}, {BusinessAddress}, {WorkPhone}";
        }
        [NotMapped]
        public string ClientInfoForAct
        {
            get => $"{Name}, ИНН {INN}, {BusinessAddress}, р/с {BankAccount}, в банке {Bank?.Name ?? ""}, БИК {Bank?.BIK ?? ""}, к/с {Bank?.CorrAccount ?? ""}";
        }
    }
}

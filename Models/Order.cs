using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

//#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Order
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        private string _number = "";
        public string Number
        {
            get => _number;
            set
            {
                _number = value;
                NotifyPropertyChanged("Number");
            }
        }
        public long ClientID { get; set; }
        public string Note { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateAdmission { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateCompletion { get; set; }
        public long OrderEnteredID { get; set; }
        public long? ManagerID { get; set; }

        //public virtual Client Client { get; set; }
        public Client Client { get; set; }
        public User Manager { get; set; }
        //public virtual User Manager { get; set; }
        public User OrderEntered { get; set; }
        //public virtual User OrderEntered { get; set; }
        //public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }
    }
}

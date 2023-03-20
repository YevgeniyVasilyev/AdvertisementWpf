using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

//#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Order : INotifyPropertyChanged
    {
        public Order()
        {
            Products = new HashSet<Product>();
        }

        private string _number = "";

        public long ID { get; set; }
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
        public DateTime? DateAdmission { get; set; }
        public DateTime? DateCompletion { get; set; }
        public DateTime? DateProductionLayout { get; set; }
        public long OrderEnteredID { get; set; }
        public long? ManagerID { get; set; }
        public long? DesignerID { get; set; }

        public virtual Client Client { get; set; }
        public virtual User Designer { get; set; }
        public virtual User Manager { get; set; }
        public virtual User OrderEntered { get; set; }
        public virtual ICollection<Product> Products { get; set; }

        //private decimal _orderCost = 0;
        //private short _countProduct = 0;
        [NotMapped]
        public decimal OrderCost
        {
            get
            {
                decimal oc = 0;
                if (Products != null)
                {
                    foreach (Product product in Products)
                    {
                        oc += product.Cost;
                    }
                }
                return oc;
            }
            //set => _orderCost = value;
        }
        [NotMapped]
        public short CountProduct
        {
            get
            {
                if (Products != null)
                {
                    return (short)Products.Count;
                }
                else
                {
                    return 0;
                }
            }
            //set => _countProduct = value;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

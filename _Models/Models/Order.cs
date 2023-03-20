using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Data;

//#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Order : INotifyPropertyChanged
    {
        public Order()
        {
            Products = new HashSet<Product>();
            Payments = new HashSet<Payment>();
            Accounts = new HashSet<Account>();
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
        [Column(TypeName = "datetime")]
        public DateTime? DateAdmission { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateCompletion { get; set; }
        public long OrderEnteredID { get; set; }
        public long? ManagerID { get; set; }

        public virtual Client Client { get; set; }
        public virtual User Manager { get; set; }
        public virtual User OrderEntered { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }

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
        public decimal OrderPayments
        {
            get
            {
                decimal op = 0;
                if (Payments != null)
                {
                    foreach (Payment payment in Payments)
                    {
                        op += payment.PaymentAmount;
                    }
                }
                return op;
            }
        }
        [NotMapped]
        public short CountProduct
        {
            get
            {
                return Products != null ? (short)Products.Count : (short)0;
            }
            //set => _countProduct = value;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _state = "";
        [NotMapped]
        public string State
        {
            get => Products != null && string.IsNullOrWhiteSpace(_state) ? OrderState(Products) : _state;
            set
            {
                _state = value;
                NotifyPropertyChanged("State");
            }
        }

        public string OrderState(ICollection<Product> products)
        {
            return _OrderState(products);

            //byte nState = 0, nCountDateTransferDesigner = 0, nCountDateManufacture = 0;
            //if (products != null)
            //{
            //    foreach (Product product in products)
            //    {
            //        if (product.DateTransferDesigner.HasValue)
            //        {
            //            nCountDateTransferDesigner++;
            //        }
            //        if (product.DateManufacture.HasValue)
            //        {
            //            nCountDateManufacture++;
            //        }
            //    }
            //    if (products.Count > 0 && nCountDateManufacture == products.Count) //у всех изделий есть Дата изготовления - "Завершен"
            //    {
            //        nState = 2;
            //    }
            //    else if (nCountDateTransferDesigner > 0) //хотя бы у одного изделия есть Дата передачи дизайнеру - "Производство"
            //    {
            //        nState = 1;
            //    }
            //}
            //return OrderProductStates.GetOrderState(nState);
        }

        public string OrderState(CollectionViewSource products)
        {
            return _OrderState(products);
        }

        private string _OrderState(object products)
        {
            byte nState = 0, nCountDateTransferDesigner = 0, nCountDateManufacture = 0;
            int nCount = 0;
            if (products != null)
            {
                if (products is ICollection<Product> collection)
                {
                    foreach (Product product in collection)
                    {
                        if (product.DateTransferDesigner.HasValue)
                        {
                            nCountDateTransferDesigner++;
                        }
                        if (product.DateManufacture.HasValue)
                        {
                            nCountDateManufacture++;
                        }
                        nCount++;
                    }
                }
                else
                {
                    foreach (Product product in ((CollectionViewSource)products).View)
                    {
                        if (product.DateTransferDesigner.HasValue)
                        {
                            nCountDateTransferDesigner++;
                        }
                        if (product.DateManufacture.HasValue)
                        {
                            nCountDateManufacture++;
                        }
                        nCount++;
                    }
                }
                if (nCount > 0 && nCountDateManufacture == nCount) //у всех изделий есть Дата изготовления - "Завершен"
                {
                    nState = 2;
                }
                else if (nCountDateTransferDesigner > 0) //хотя бы у одного изделия есть Дата передачи дизайнеру - "Производство"
                {
                    nState = 1;
                }
            }
            return OrderProductStates.GetOrderState(nState);
        }
    }
}

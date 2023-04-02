using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        public DateTime? AccountDate => Accounts?.FirstOrDefault()?.AccountDate ?? null;
        [NotMapped]
        public decimal OrderCost => Products?.Sum(p => p.Cost) ?? 0;
        [NotMapped]
        public decimal OrderPayments => Payments?.Sum(p => p.PaymentAmount) ?? 0;
        [NotMapped]
        public short CountProduct => (short)(Products?.Count ?? 0);
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
            byte nState = 0, nCountDateTransferDesigner = 0, nCountDateShipment = 0;
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
                        if (product.DateShipment.HasValue)
                        {
                            nCountDateShipment++;
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
                        if (product.DateShipment.HasValue)
                        {
                            nCountDateShipment++;
                        }
                        nCount++;
                    }
                }
                if (nCountDateShipment == nCount) //Даты отгрузки у всех
                {
                    nState = 4;
                }
                else if (nCountDateShipment > 0 && nCountDateShipment != nCount) //Даты отгрузки не у всех
                {
                    nState = 3;
                }
                else if (nCountDateShipment == 0) //нет ни одной Даты отгрузки
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;


#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Payment : INotifyPropertyChanged
    {
        public long ID { get; set; }
        public long OrderID { get; set; }
        private DateTime? paymentDate = DateTime.Now;
        [Column(TypeName = "datetime")]
        public DateTime? PaymentDate
        {
            get => paymentDate;
            set
            {
                paymentDate = value;
                NotifyPropertyChanged("PaymentDate");
            }
        }
        private decimal paymentAmount = 0;
        public decimal PaymentAmount
        {
            get => paymentAmount;
            set
            {
                paymentAmount = value;
                NotifyPropertyChanged("PaymentAmount");
            }
        }
        private string paymentDocNumber = "";
        public string PaymentDocNumber
        {
            get => paymentDocNumber;
            set
            {
                paymentDocNumber = value;
                NotifyPropertyChanged("PaymentDocNumber");
            }
        }
        private byte? purposeOfPayment = 0;
        public byte? PurposeOfPayment
        { 
            get => purposeOfPayment;
            set
            {
                purposeOfPayment = value;
                NotifyPropertyChanged("PurposeOfPaymentName");
            }
        }
        private byte? typeOfPayment = 0;
        public byte? TypeOfPayment
        { 
            get => typeOfPayment; 
            set
            {
                typeOfPayment = value;
                NotifyPropertyChanged("TypeOfPaymentName");
            }
        }
        public virtual Order Order { get; set; }

        [NotMapped]
        public List<string> ListPurposeOfPayment = new List<string> { "предоплата", "доплата", "окончательный расчёт", "возврат" };
        [NotMapped]
        public List<string> ListTypeOfPayment = new List<string> { "безналичный", "наличный", "взаимозачёт", "другое" };
        [NotMapped]
        public string PurposeOfPaymentName
        {
            get => PurposeOfPayment != null && PurposeOfPayment >= 0 && PurposeOfPayment < ListPurposeOfPayment.Count ? ListPurposeOfPayment[(int)PurposeOfPayment] : "";
        }
        [NotMapped]
        public string TypeOfPaymentName
        {
            get => TypeOfPayment != null && TypeOfPayment >= 0 && TypeOfPayment < ListTypeOfPayment.Count ? ListTypeOfPayment[(int)TypeOfPayment] : "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

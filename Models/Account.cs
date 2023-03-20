using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Account
    {
        public Account()
        {
            Acts = new HashSet<Act>();
        }
        public long ID { get; set; }
        public long OrderID { get; set; }
        public string AccountNumber { get; set; } = "";
        [Column(TypeName = "datetime")]
        public DateTime? AccountDate { get; set; } = DateTime.Now;
        public long? ContractorID { get; set; }
        public string Footing { get; set; } = "";
        public bool IsManual { get; set; } = false;
        public string Details { get; set; } = "";
        [Column(TypeName = "datetime")]
        public DateTime? PayBeforeDate { get; set; }

        public virtual Order Order { get; set; }
        public virtual Contractor Contractor { get; set; }
        public virtual ICollection<Act> Acts { get; set; }

        public List<AccountDetail> DetailsList { get; set; } = new List<AccountDetail> { };

        private string _contractorName = "";
        [NotMapped]
        public string ContractorName
        {
            get => Contractor != null && Contractor.ID == ContractorID ? Contractor.Name : _contractorName;
            set => _contractorName = value;
        }

        private string _contractorInfoForAccount = "";
        [NotMapped]
        public string ContractorInfoForAccount
        {
            get => Contractor != null && Contractor.ID == ContractorID ? Contractor.ContractorInfoForAccount : _contractorInfoForAccount;
            set => _contractorInfoForAccount = value;
        }
        private string _contractorInfoForAct = "";
        [NotMapped]
        public string ContractorInfoForAct
        {
            get => Contractor != null && Contractor.ID == ContractorID ? Contractor.ContractorInfoForAct : _contractorInfoForAct;
            set => _contractorInfoForAct = value;
        }

        public void DetailsToList()
        {
            List<AccountDetail> detailList = new List<AccountDetail> { };
            if (Details != null)
            {
                string[] aDetails = Details.Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (string aD in aDetails)
                {
                    string[] pD = aD.Split('#');
                    //if (pD.Length == 4)
                    //{
                    //    detailList.Add(new AccountDetail { ProductInfoForAccount = pD[0], Quantity = Convert.ToInt16(pD[1]), UnitName = pD[2], Cost = Convert.ToDecimal(pD[3]) });
                    //}
                    //else
                    //{
                    //}
                    detailList.Add(new AccountDetail { ProductID = Convert.ToInt64(pD[0]), ProductInfoForAccount = pD[1], Quantity = Convert.ToInt16(pD[2]), UnitName = pD[3], Cost = Convert.ToDecimal(pD[4]) });
                }
            }
            DetailsList = detailList;
        }

        public void ListToDetails()
        {
            string sDetails = "";
            if (DetailsList.Count > 0)
            {
                foreach (AccountDetail aD in DetailsList)
                {
                    sDetails += $"{aD.ProductID}#{aD.ProductInfoForAccount}#{aD.Quantity}#{aD.UnitName}#{aD.Cost}&";
                    //ID#Value&
                }
                if (sDetails.Length > 7000)
                {
                    _ = MessageBox.Show("Длина значения поля Details превышает 7000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class Account",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            Details = sDetails;
        }
    }

    [NotMapped]
    public class AccountDetail : INotifyPropertyChanged
    {
        public long ProductID = 0;
        private string _productInfoForAccount = "";
        public string ProductInfoForAccount
        { 
            get => _productInfoForAccount; 
            set
            {
                _productInfoForAccount = value;
                NotifyPropertyChanged("ProductInfoForAccount");
            }
        }
        private short _quantity = 0;
        public short Quantity
        { 
            get => _quantity;
            set
            {
                _quantity = value;
                NotifyPropertyChanged();
            }
        }
        private string _unitName = "";
        public string UnitName
        {
            get => _unitName;
            set
            {
                _unitName = value;
                NotifyPropertyChanged("UnitName");
            }
        }
        private decimal _cost = 0;
        public decimal Cost
        {
            get => _cost;
            set
            {
                _cost = value;
                NotifyPropertyChanged();
            }
        }
        public decimal PricePerUnit => Quantity > 0 ? (Cost / Quantity) : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
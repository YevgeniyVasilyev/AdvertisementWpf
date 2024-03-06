using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Act : INotifyPropertyChanged
    {
        public long ID { get; set; }
        public long AccountID { get; set; }
        public string ActNumber { get; set; } = "";
        [Column(TypeName = "datetime")]
        public DateTime? ActDate { get; set; } = DateTime.Now;
        public string ProductInAct { get; set; } = "";

        public virtual Account Account { get; set; }

        [NotMapped]
        public List<long> ListProductInAct { get; set; } = new List<long> { };
#if NEWORDER
        private ObservableCollection<AccountDetail> _detailsList = new ObservableCollection<AccountDetail>();
        [NotMapped]
        public ObservableCollection<AccountDetail> DetailsList
        {
            get => _detailsList;
            set
            {
                _detailsList = new ObservableCollection<AccountDetail>(value);
                NotifyPropertyChanged("DetailsList");
            }
        }
#else
        private List<AccountDetail> _detailsList = new List<AccountDetail> { };
        [NotMapped]
        public List<AccountDetail> DetailsList
        { 
            get => _detailsList;
            set
            {
                _detailsList = value;
                NotifyPropertyChanged("DetailsList"); //???????
            }
        }
#endif
        [NotMapped]
        public string UPDState { get; set; } = "2";

        public void ProductInActToList()
        {
            List<long> productIDList = new List<long> { };
            if (!string.IsNullOrWhiteSpace(ProductInAct))
            {
                string[] pList = ProductInAct.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (string pL in pList)
                {
                    productIDList.Add(Convert.ToInt64(pL));
                }
            }
            ListProductInAct = productIDList;
        }

        public void ListToProductInAct()
        {
            string sProductID = "";
            if (ListProductInAct.Count > 0)
            {
                foreach (long pL in ListProductInAct)
                {
                    sProductID += $"{pL};";
                }
                if (sProductID.Length > 5000)
                {
                    _ = MessageBox.Show("Длина значения поля ProductInAct превышает 5000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class Act",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            ProductInAct = sProductID;
        }

        public void CreateDetailsList(Account acnt = null)
        {
            ProductInActToList();
            ObservableCollection<AccountDetail> detailLst = new ObservableCollection<AccountDetail> { };
            List<AccountDetail> detailList = new List<AccountDetail> { };
            Account account = Account ?? acnt;
            if (account != null)
            {
                if (account.IsManual) //для ручного счета
                {
#if NEWORDER
                    detailLst = account.DetailsList; //добавить единственную детализацию
#else
                    detailList = account.DetailsList; //добавить единственную детализацию
#endif
                }
                else
                {
                    foreach (AccountDetail accountDetail in account.DetailsList) //в акт клпируем детали от счета для изделий включенных в акт
                    {
                        if (ListProductInAct.Contains(accountDetail.ProductID)) //да, продукт включен в акт
                        {
                            detailList.Add(accountDetail);
                            detailLst.Add(accountDetail);
                        }
                    }
                }
            }
#if NEWORDER
            DetailsList = detailLst;
#else
            DetailsList = detailList;
#endif
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

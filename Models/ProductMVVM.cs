using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows;

namespace AdvertisementWpf.Models
{
    public partial class Product : INotifyPropertyChanged, IDataErrorInfo
    {
        public Product()
        {
            //TechCards = new HashSet<TechCard>();
            //Costs = new HashSet<ProductCost>();
            ProductCosts = new HashSet<ProductCost>();
        }

        private short _quantity;
        public short Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                NotifyPropertyChanged("Quantity");
            }
        }
        public ProductType ProductType { get; set; }

        [NotMapped]
        public string KVDForReport { get; set; }
        private string _productTypeName = "";
        [NotMapped]
        public string ProductTypeName
        {
            get => ProductType?.Name ?? _productTypeName;
            set => _productTypeName = value;
        }
        [NotMapped]
        public ICollection<ProductCost> Costs { get; set; }
        [NotMapped]
        public List<string> FilesList { get; set; } = new List<string> { };
        [NotMapped]
        public List<ProductParameters> ProductParameter { get; set; }

        private string _state = "";
        [NotMapped]
        public string State
        {
            get => ProductState();
            set
            {
                _state = value;
                NotifyPropertyChanged("State");
            }
        }

        [NotMapped]
        public string ProductInfoForAccount => $"{ProductTypeName}";

        public string ProductState()
        {
            byte nFlag = 0, nState = 0; //"Не определено"
            nFlag += (byte)(DateShipment.HasValue ? 64 : 0);
            nFlag += (byte)(DateManufacture.HasValue ? 32 : 0);
            nFlag += (byte)(DateTransferProduction.HasValue ? 16 : 0);
            nFlag += (byte)(IsHasTechcard ? 8 : 0); //отгрузки нет, передачи в производство нет, а дизайн макет утвержден и есть техкарта
            nFlag += (byte)(DateApproval.HasValue ? 4 : 0);
            nFlag += (byte)(DateTransferApproval.HasValue ? 2 : 0);
            nFlag += (byte)(DateTransferDesigner.HasValue ? 1 : 0);
            if ((nFlag & 64) != 0)
            {
                nState = 1; //"Отгружено"
            }
            else if ((nFlag & 32) != 0)
            {
                nState = 2; // "Запланирована отгрузка";
            }
            else if ((nFlag & 16) != 0)
            {
                nState = 3; // "В производстве";
            }
            else if ((nFlag & 8) != 0)
            {
                nState = 4; // "Подготовка";
            }
            else if ((nFlag & 4) != 0)
            {
                nState = 5; // "Утвержден макет";
            }
            else if ((nFlag & 2) != 0)
            {
                nState = 6; // "Передано на утверждение";
            }
            else if ((nFlag & 1) != 0)
            {
                nState = 7; // "В разработке";
            }
            return OrderProductStates.GetProductState(nState);
        }

        public void ListToParameters()
        {
            if (ProductParameter.Count > 0)
            {
                string sParameters = "";
                foreach (ProductParameters pP in ProductParameter)
                {
                    if (pP.ReferencebookID > 0)
                    {
                        sParameters += $"{pP.ID}#{pP.ParameterID}#{pP.ReferencebookID}&";
                    }
                    else
                    {
                        sParameters += $"{pP.ID}#{pP.ParameterValue}#{"0"}&";
                    }
                    //ID#Value&
                }
                if (sParameters.Length > 3000)
                {
                    _ = MessageBox.Show("Длина значения поля Parameters превышает 3000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class Product",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                Parameters = sParameters;
            }
        }

        public void ParametersToList()
        {
            string[] aParameters = Parameters.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (string aP in aParameters)
            {
                string[] pP = aP.Split('#');
                if (pP.Length < 3) //для "устаревших" значений добавить поле
                {
                    pP = (aP + "#0").Split("#");
                }
                for (short idx = 0; idx < ProductParameter.Count; idx++)
                {
                    long nID = Convert.ToInt64(pP[0]);
                    if (nID == ProductParameter[idx].ID) //нашли требуемый параметр
                    {
                        if (ProductParameter[idx].IsRefbookOnRequest) //для параметра установлени признак "выбор справочника по запросу"
                        {
                            //установить ID "справочника по запросу" либо оставить то значение которое идет из конструктора изделий, если выбора еще не было
                            ProductParameter[idx].ReferencebookID = long.TryParse(pP[2], out nID) ? nID : ProductParameter[idx].ReferencebookID;
                        }
                        if (ProductParameter[idx].ReferencebookID > 0) //для параметра установлен выбор из справочника
                        {
                            //ProductParameter[idx].ParameterID = Convert.ToInt64(pP[1]); //устанавливаем значение ID из справочника ReferencebookID  
                            ProductParameter[idx].ParameterID = long.TryParse(pP[1], out nID) ? nID : 0;
                        }
                        else
                        {
                            ProductParameter[idx].ParameterValue = pP[1]; //просто произвольное текстовое значение
                        }
                    }
                }
            }
        }

        public void ListToFiles()
        {
            if (FilesList.Count > 0)
            {
                string sFiles = "";
                foreach (string sfile in FilesList)
                {
                    sFiles += $"{sfile}|";
                }
                if (sFiles.Length > 3000)
                {
                    _ = MessageBox.Show("Длина значения поля Files превышает 3000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class Product",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                Files = sFiles;
            }
        }

        public void FilesToList()
        {
            string[] aFiles = Files.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (FilesList != null)
            {
                FilesList.Clear();
            }
            foreach (string aF in aFiles)
            {
                FilesList.Add(aF);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [NotMapped]
        public bool HasError { get; set; }

        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                HasError = false;
                HasError = columnName switch
                {
                    _ => false,
                };
                return ""; //текст сообщения об ошибке не гененрировать
            }
        }
    }

    public class ProductParameters : INotifyPropertyChanged
    {
        public long ID { get; set; }
        public string Name { get; set; } = "";
        public string ParameterValue { get; set; } = "";
        public string UnitName { get; set; } = "";
        public long? ReferencebookID { get; set; } = null;
        public long? ParameterID { get; set; } = null;

        private List<ReferencebookParameter> _referencebookParametersList = new List<ReferencebookParameter> { };
        public List<ReferencebookParameter> ReferencebookParametersList
        {
            get => _referencebookParametersList;
            set
            {
                _referencebookParametersList = value;
                NotifyPropertyChanged("ReferencebookParametersList");
            }
        }
        public List<Referencebook> ReferencebookList { get; set; } = new List<Referencebook> { };
        public bool IsRefbookOnRequest { get; set; } = false;
        public bool IsRequired { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

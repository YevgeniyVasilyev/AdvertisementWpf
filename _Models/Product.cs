using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Product : INotifyPropertyChanged
    {
        public long ID { get; set; }
        public long OrderID { get; set; }
        public long ProductTypeID { get; set; }
        public string Parameters { get; set; }
        public decimal Cost { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateTransferDesigner { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateTransferApproval { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateApproval { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateTransferProduction { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateManufacture { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateShipment { get; set; }
        public string Note { get; set; }
        public string Files { get; set; }
        public short Quantity { get; set; }

        public virtual Order Order { get; set; }
        public virtual ProductType ProductType { get; set; }

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
            get => _state;
            set
            {
                _state = value;
                NotifyPropertyChanged("State");
            }
        }

        public void ProductState()
        {
            byte nFlag = 0;
            string state = "Не определено";
            nFlag += (byte)(DateShipment.HasValue ? 32 : 0);
            nFlag += (byte)(DateManufacture.HasValue ? 16 : 0);
            nFlag += (byte)(DateTransferProduction.HasValue ? 8 : 0);
            nFlag += (byte)(DateApproval.HasValue ? 4 : 0);
            nFlag += (byte)(DateTransferApproval.HasValue ? 2 : 0);
            nFlag += (byte)(DateTransferDesigner.HasValue ? 1 : 0);
            if ((nFlag & 32) != 0)
            {
                state = "Отгружено";
            }
            else if ((nFlag & 16) != 0)
            {
                state = "Запланирована отгрузка";
            }
            else if ((nFlag & 8) != 0)
            {
                state = "В производстве";
            }
            else if ((nFlag & 4) != 0)
            {
                state = "Утвержден макет";
            }
            else if ((nFlag & 2) != 0)
            {
                state = "Передано на утверждение";
            }
            else if ((nFlag & 1) != 0)
            {
                state = "В разработке";
            }
            State = state;
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
                        sParameters += $"{pP.ID}#{pP.ParameterID}&";
                    }
                    else
                    {
                        sParameters += $"{pP.ID}#{pP.ParameterValue}&";
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
                for (short idx = 0; idx < ProductParameter.Count; idx++)
                {
                    long nID = Convert.ToInt64(pP[0]);
                    if (nID == ProductParameter[idx].ID) //нашли требуемый параметр
                    {
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
    }

    public class ProductParameters
    {
        public long ID { get; set; }
        public string Name { get; set; } = "";
        public string ParameterValue { get; set; } = "";
        public string UnitName { get; set; } = "";
        public long? ReferencebookID { get; set; } = null;
        public long? ParameterID { get; set; } = null;
        public List<ReferencebookParameter> ReferencebookParametersList { get; set; } = new List<ReferencebookParameter> { };
        public List<Referencebook> ReferencebookList { get; set; } = new List<Referencebook> { };
        public bool IsRefbookOnRequest { get; set; } = false;
        public bool IsRequired { get; set; } = false;
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class OperationInWork : TreeViewItemBase
    {
        public long ID { get; set; }
        public long WorkInTechCardID { get; set; }
        public long? OperationID { get; set; }
        public string Number { get; set; } = "";
        public string Note { get; set; } = "";
        public string Parameters { get; set; } = "";
        public string Files { get; set; } = "";

        public virtual Operation Operation { get; set; }
        public virtual WorkInTechCard WorkInTechCard { get; set; }

        private ObservableCollection<string> _filesList { get; set; } = new ObservableCollection<string>();
        [NotMapped]
#if NEWORDER
        public ICollection<string> FilesList
        {
            get => _filesList;
            set => _filesList = new ObservableCollection<string>(value);
        }
#else
        [NotMapped]
        public List<string> FilesList { get; set; } = new List<string> { };
#endif

        [NotMapped]
        public List<OperationInWorkParameter> OperationInWorkParameters { get; set; } //заполняется внешними методами. В него разворачивается строка Parameters

        public void ListToParameters() //сворачивает список в строку Parameters
        {
            if (OperationInWorkParameters.Count > 0) //если параметры определены
            {
                string sParameters = "";
                foreach (OperationInWorkParameter oP in OperationInWorkParameters)
                {
                    if (oP.ReferencebookID > 0) //значение берется из справочника
                    {
                        sParameters += $"{oP.ID}#{oP.ParameterID}#{oP.ReferencebookID}&";
                    }
                    else //значение указывается явно ручками
                    {
                        sParameters += $"{oP.ID}#{oP.ParameterValue}#{"0"}&";
                    }
                    //ID#Value& - шаблон
                }
                if (sParameters.Length > 3000) //контроль переполнения поля. В БД длина этого поля 3000 (этот контроль можно сделать по другому)
                {
                    _ = MessageBox.Show("Длина значения поля Parameters превышает 3000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class OperationInWork",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                Parameters = sParameters;
            }
        }

        public void ParametersToList() //развернуть строку Parameters в список
        {
            string[] aParameters = Parameters.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (string aP in aParameters)
            {
                string[] pP = aP.Split('#');
                for (short idx = 0; idx < OperationInWorkParameters.Count; idx++)
                {
                    long nID = Convert.ToInt64(pP[0]);
                    if (nID == OperationInWorkParameters[idx].ID) //нашли требуемый параметр
                    {
                        if (OperationInWorkParameters[idx].IsRefbookOnRequest) //для параметра установлен признак "выбор справочника по запросу"
                        {
                            //установить ID "справочника по запросу" либо оставить то значение которое идет из конструктора изделий, если выбора еще не было
                            OperationInWorkParameters[idx].ReferencebookID = long.TryParse(pP[2], out nID) ? nID : OperationInWorkParameters[idx].ReferencebookID;
                        }
                        if (OperationInWorkParameters[idx].ReferencebookID > 0) //для параметра установлен выбор из справочника
                        {
                            OperationInWorkParameters[idx].ParameterID = long.TryParse(pP[1], out nID) ? nID : 0;
                        }
                        else
                        {
                            OperationInWorkParameters[idx].ParameterValue = pP[1]; //просто произвольное текстовое значение заполняемое ручками
                        }
                    }
                }
            }
        }

        public void ListToFiles() //список файлов свернуть в строку
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
                    _ = MessageBox.Show("Длина значения поля Files превышает 3000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class OperationInWorkParameter",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                Files = sFiles;
            }
        }

        public void FilesToList() //список файлов из поля Files развернуть в список
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
    }

    public class OperationInWorkParameter : INotifyPropertyChanged
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

using System;
using System.Collections.Generic;
using System.ComponentModel;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Setting : INotifyPropertyChanged
    {
        private string _settingParameterValue = "";

        public long ID { get; set; }
        public string SettingParameterDescription { get; set; } = "";
        public string SettingParameterName { get; set; } = "";
        public string SettingParameterValue 
        { 
            get => _settingParameterValue;
            set
            {
                _settingParameterValue = value;
                NotifyPropertyChanged("SettingParameterValue");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

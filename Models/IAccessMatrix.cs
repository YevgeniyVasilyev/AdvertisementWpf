using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

#nullable disable

namespace AdvertisementWpf
{
    public partial class IAccessMatrix
    {
        public long ID { get; set; }
        public string AccessDescribe { get; set; } = "";
        public string AccessName { get; set; } = "";
        public string AccessGrant { get; set; } = "";

        [NotMapped]
        public List<long> accessGrant = new List<long> { };

        public void GrantToList()
        {
            string[] aGrants = AccessGrant.Split('&', StringSplitOptions.RemoveEmptyEntries);
            accessGrant.Clear();
            foreach (string aG in aGrants)
            {
                accessGrant.Add(Convert.ToInt64(aG));
            }
        }

        public void ListToGrant()
        {
            string sAccessGrant = "";
            if (accessGrant.Count > 0)
            {
                foreach (int aG in accessGrant)
                {
                    sAccessGrant += $"{aG}&";
                }
                if (sAccessGrant.Length > 1000)
                {
                    _ = MessageBox.Show("Длина значения поля AccessGrant превышает 1000 знаков!" + "\n" + "Возможна потеря данных! Сообщите разработчику", "Преобразование данных class IAccessNatrix",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            AccessGrant = sAccessGrant;
        }
    }

    public static class IGrantAccess
    {
        public static bool CheckGrantAccess(List<IAccessMatrix> accessMatrixList, long nID, string sAccessName)
        {
            foreach(IAccessMatrix accessMatrix in accessMatrixList)
            { 
                if (accessMatrix.AccessName == sAccessName && accessMatrix.accessGrant.Contains(nID)) //нашли нужный вид доступа и ID роли есть в списке
                {
                    return true;
                }
            }
            return false;
        }
    }
}
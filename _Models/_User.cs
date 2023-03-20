using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#nullable disable

namespace AdvertisementWpf.Models
{
    public enum CaregoryWorkName {Продажи, Дизайн, АУП};

    public partial class User
    {

        public long ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string LoginName { get; set; }
        public long RoleID { get; set; }
        public bool Disabled { get; set; }
        public short CategoryWork { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsExternal { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string CardNumber { get; set; }

        public virtual Role Role { get; set; }

        public string FullUserName => $"{FirstName} {LastName} {MiddleName}";
    }

    public class CategoryWork
    {
        public short CategoryID { get; private set; }
        public string CategoryName { get; private set; }
        
        public CategoryWork(short ID, string Name)
        {
            CategoryID = ID;
            CategoryName = Name;
        }
    }

}

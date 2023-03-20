using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{
    public enum CaregoryWorkName { Продажи, Дизайн, АУП };

    public partial class User
    {
        public User()
        {
            Clients = new HashSet<Client>();
            OrderDesigners = new HashSet<Order>();
            OrderManagers = new HashSet<Order>();
            OrderOrderEntereds = new HashSet<Order>();
        }

        public long ID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LoginName { get; set; } = "";
        public long RoleID { get; set; }
        public bool Disabled { get; set; } = false;
        public short CategoryWork { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsExternal { get; set; } = false;
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string CardNumber { get; set; } = "";

        public virtual Role Role { get; set; }
        public virtual ICollection<Client> Clients { get; set; }
        public virtual ICollection<Order> OrderDesigners { get; set; }
        public virtual ICollection<Order> OrderManagers { get; set; }
        public virtual ICollection<Order> OrderOrderEntereds { get; set; }

        [NotMapped]
        public string FullUserName => $"{FirstName} {LastName} {MiddleName}";
        [NotMapped]
        public bool Is_sysadmin = false;
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

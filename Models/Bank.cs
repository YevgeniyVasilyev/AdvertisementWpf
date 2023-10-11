using System;
using System.Collections.Generic;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class Bank
    {
        public Bank()
        {
            Clients = new HashSet<Client>();
            ConsigneeClients = new HashSet<Client>();
            Contractors = new HashSet<Contractor>();
        }

        public long ID { get; set; }
        public string Name { get; set; }
        public long LocalitiesID { get; set; }
        public string CorrAccount { get; set; }
        public string BIK { get; set; }
        public string OKPO { get; set; }
        public string OKONX { get; set; }

        public virtual Locality Localities { get; set; }
        public virtual ICollection<Client> Clients { get; set; }
        public virtual ICollection<Client> ConsigneeClients { get; set; }
        public virtual ICollection<Contractor> Contractors { get; set; }
    }
}

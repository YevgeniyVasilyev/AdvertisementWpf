using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace AdvertisementWpf.Models
{

    public partial class Product
    {
        public long ID { get; set; }
        public long OrderID { get; set; }
        public long ProductTypeID { get; set; }
        public string Parameters { get; set; }
        public decimal Cost { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateDeliveryPlan { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateProductionLayout { get; set; }
        private DateTime? dateTransferDesigner { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateTransferDesigner
        {
            get => dateTransferDesigner;
            set
            {
                if (dateTransferDesigner is null || !dateTransferDesigner.Equals(value))
                {
                    dateTransferDesigner = value;
                    NotifyPropertyChanged("DateTransferDesigner");
                    State = ""; //для переинициализации
                }
            }
        }
        private DateTime? dateTransferApproval { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateTransferApproval
        {
            get => dateTransferApproval;
            set
            {
                if (dateTransferApproval is null || !dateTransferApproval.Equals(value))
                {
                    dateTransferApproval = value;
                    NotifyPropertyChanged("DateTransferApproval");
                    State = ""; //для переинициализации
                }
            }
        }
        private DateTime? dateApproval { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateApproval
        {
            get => dateApproval;
            set
            {
                if (dateApproval is null || !dateApproval.Equals(value))
                {
                    dateApproval = value;
                    NotifyPropertyChanged("DateApproval");
                    State = ""; //для переинициализации
                }
            }
        }
        private DateTime? dateTransferProduction { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateTransferProduction
        {
            get => dateTransferProduction;
            set
            {
                if (dateTransferProduction is null || !dateTransferProduction.Equals(value))
                {
                    dateTransferProduction = value;
                    NotifyPropertyChanged("DateTransferProduction");
                    NotifyPropertyChanged("DateTransferProductionAsString");
                    State = ""; //для переинициализации
                }
            }
        }
        private DateTime? dateManufacture { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateManufacture
        {
            get => dateManufacture;
            set
            {
                if (dateManufacture is null || !dateManufacture.Equals(value))
                {
                    dateManufacture = value;
                    NotifyPropertyChanged("DateManufacture");
                    NotifyPropertyChanged("DateManufactureHasValue");          
                    State = ""; //для переинициализации
                }
            }
        }
        private DateTime? dateShipment { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateShipment
        {
            get => dateShipment;
            set
            {
                if (dateShipment is null || !dateShipment.Equals(value))
                {
                    dateShipment = value;
                    NotifyPropertyChanged("DateShipment");
                    State = ""; //для переинициализации
                }
            }
        }
        public string Note { get; set; }
        public string Files { get; set; }
        //public short Quantity { get; set; }
        public long? DesignerID { get; set; }
        private bool isHasTechcard { get; set; }
        public bool IsHasTechcard
        {
            get => isHasTechcard;
            set
            {
                if (!isHasTechcard.Equals(value))
                {
                    isHasTechcard = value;
                    NotifyPropertyChanged("IsHasTechcard");
                    State = "";
                }
            }
        }

        public virtual Order Order { get; set; }
        public virtual ICollection<ProductCost> ProductCosts { get; set; }
        //public virtual ProductType ProductType { get; set; }
        public virtual User Designer { get; set; }
        public virtual TechCard TechCard { get; set; }
        //public virtual ICollection<ProductCost> Costs { get; set; }
        //public virtual ICollection<TechCard> TechCards { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AdvertisementWpf.Models
{
    public partial class Order : INotifyPropertyChanged
    {
        [NotMapped]
        public DateTime? AccountDate => Accounts?.FirstOrDefault()?.AccountDate ?? null;
        [NotMapped]
        public decimal OrderCost => Products?.Sum(p => p.Cost) ?? 0;
        [NotMapped]
        public decimal OrderPayments => Payments?.Sum(p => p.PaymentAmount) ?? 0;
        [NotMapped]
        public short CountProduct => (short)(Products?.Count ?? 0);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _state = "";

        [NotMapped]
        public string State
        {
            get => Products != null && string.IsNullOrWhiteSpace(_state) ? OrderState(Products) : _state;
            set
            {
                _state = value;
                NotifyPropertyChanged("State");
            }
        }
        public string OrderState(ICollection<Product> products)
        {
            return _OrderState(products);
        }

        public string OrderState(CollectionViewSource products)
        {
            return _OrderState(products);
        }

        private string _OrderState(object products)
        {
            byte nState = 0, nCountDateTransferDesigner = 0, nCountDateShipment = 0, nCountDateManufacture = 0;
            int nCount = 0;
            if (products != null)
            {
                if (products is ICollection<Product> collection)
                {
                    foreach (Product product in collection)
                    {
                        if (product.DateManufacture.HasValue) //есть дата Изготовления
                        {
                            nCountDateManufacture++;
                        }
                        if (product.DateTransferDesigner.HasValue)
                        {
                            nCountDateTransferDesigner++;
                        }
                        if (product.DateShipment.HasValue)
                        {
                            nCountDateShipment++;
                        }
                        nCount++;
                    }
                }
                else
                {
                    foreach (Product product in ((CollectionViewSource)products).View)
                    {
                        if (product.DateManufacture.HasValue) //есть дата Изготовления
                        {
                            nCountDateManufacture++;
                        }
                        if (product.DateTransferDesigner.HasValue)
                        {
                            nCountDateTransferDesigner++;
                        }
                        if (product.DateShipment.HasValue)
                        {
                            nCountDateShipment++;
                        }
                        nCount++;
                    }
                }
                if (nCountDateTransferDesigner == 0) //ни одной даты "Передано дизайнеру"
                {
                    nState = 0; //оформление
                }
                else if (nCountDateManufacture >= 0 && nCountDateManufacture < nCount) //не все даты Изготовления
                {
                    nState = 1; //в производстве
                }
                else if (nCountDateManufacture == nCount && nCountDateShipment == 0) //Дата изготовления у всех но Даты отгрузки ни укого нет
                {
                    nState = 2; //не отгружен
                }
                else if (nCountDateManufacture == nCount && nCountDateShipment > 0 && nCountDateShipment < nCount) //Дата изготовления есть у всех но Даты отгрузки не у всех
                {
                    nState = 3; //частично отгружен
                }
                else if (nCountDateManufacture == nCount && nCountDateShipment == nCount) //Дата изготовления и Дата отгружено у всех
                {
                    nState = 4; //отгружен
                }
            }
            return OrderProductStates.GetOrderState(nState);
        }
    }

    internal class OrderViewModel : ISave, ILoad, IAdd, INotifyPropertyChanged
    {
        private RelayCommand saveRequest, textBlockMouseLeftClick, showHideNewProductList, selectNewProduct;
        private App.AppDbContext _contextOrder_ = CreateDbContext.CreateContext();
        private string _searchClient = "";
        public string SearchClient
        {
            get => _searchClient;
            set
            {
                _searchClient = value;
                ClientCollectionView.Refresh();
            }
        }
        public ICollectionView ClientCollectionView { get; set; }       //крллеция "Киенты" AsNoTracking, с фильтрацией
        public ICollectionView ProductTypeCollectionView { get; set; }  //коллекция "Виды изделий" AsNoTracking, с фильтрацией и группировкой
        public List<User> UserList { get; set; }                        //список "Пользователи" AsNoTracking
        public List<User> ManagerList { get; set; }                     //список "Менеджеры" AsNoTracking
        public List<User> DesignerList { get; set; }                    //список "Дизайнеры" AsNoTracking
        public Order CurrentOrder { get; set; }                         //текущий заказ
        public bool ViewMode { get; set; } = false;                     //режим "просмотр" для карточки заказа

        //определяют доступ к элементам интерфейса
        public bool ProductCostAndCostsKVDCostsChange { get; set; }
        public bool OrderCardDateCompletion { get; set; }
        public bool OrderCardPrintOrderForm { get; set; }
        public bool OrderCardPrintOrderFormForDesigner { get; set; }
        public bool OrderCardProductDesigner { get; set; }
        public bool OrderCardProductDateProductionLayout { get; set; }
        public bool OrderCardProductDateTransferDesigner { get; set; }
        public bool IsManager { get; set; }
        private bool _IsNewOrder;
        public bool IsNewOrder
        {
            get => _IsNewOrder;
            set
            {
                _IsNewOrder = value;
                NotifyPropertyChanged("IsNewOrder");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //TO DO: добавление нового изделия, удаление изделия из списка, поиск по списку новых изделий, сохранение заказа(новый номер и проверить)

        public OrderViewModel(long nOrderID = 0, bool lViewMode = false)
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Загрузка данных по заказу ...", Cursors.Wait);
                ViewMode = lViewMode;
                LoadContext(nOrderID);  //загрузить данные в контекст
                SetModelAccess();       //установить режимы доступа к элементам интерфейса
                if (CurrentOrder is null) //новый
                {
                    IsNewOrder = true;
                    CurrentOrder = new Order
                    {
                        ClientID = (ClientCollectionView?.CurrentItem as Client)?.ID ?? 0,
                        DateAdmission = DateTime.Today,
                        OrderEnteredID = MainWindow.Userdata.ID,
                        ManagerID = MainWindow.Userdata.ID,
                    };
                    ((IAdd)this).AddToContext(ref _contextOrder_, CurrentOrder);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }
        ~OrderViewModel()
        {
            if (_contextOrder_ != null)
            {
                _contextOrder_.Dispose();
                _contextOrder_ = null;
            }
        }

        private bool ClientFilter(object item) //поиск(фильтрация) клиентов
        {
            Client client = item as Client;
            return string.IsNullOrEmpty(_searchClient) || client.Name.IndexOf(_searchClient, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GenerateNewOrderNumber(long Number)
        {
            return Number.ToString().Trim().PadLeft(10, '0');
        }

        public RelayCommand SaveRequest => saveRequest ??= new RelayCommand((o) =>  // команда сохранения Заказа
        {
            ((ISave)this).SaveContext(ref _contextOrder_);
            if (IsNewOrder)
            {
                CurrentOrder.Number = GenerateNewOrderNumber(CurrentOrder.ID);
                ((ISave)this).SaveContext(ref _contextOrder_, IsMutedMode: true);   //сохранить в "тихом режиме"
                IsNewOrder = false;
            }
        }, (o) => _contextOrder_ != null );
        
        public RelayCommand ShowHideNewProductList => showHideNewProductList ??= new RelayCommand((o) => //команда "отобразить список выбора нового изделия"
        {
            ((FrameworkElement)o).Visibility = ((FrameworkElement)o).Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }, null);

        public RelayCommand SelectNewProduct => selectNewProduct ??= new RelayCommand((o) => //команда "выбрать изделеи из списка выбора нового изделия"
        {
            if (CreateNewProduct((ProductType)o) is Product product)
            {
                CurrentOrder.Products.Add(product);
                product.ProductType = ((IAdd)this).AddSingleToContext(ref _contextOrder_, product.ProductType, delegate (ProductType p) { return p.ID == product.ProductTypeID; }); //добавить св-во навигации ProductType
                ICollectionView view = CollectionViewSource.GetDefaultView(CurrentOrder.Products);
                view.Refresh();     //обновить представление для Products
                //_ = CountTotals();
                //_ = productsViewSource.View.MoveCurrentTo(product);
                //productsViewSource.View.Refresh();
            };
        }, (o) => _contextOrder_ != null && ProductTypeCollectionView != null);

        public RelayCommand TextBlockMouseLeftClick => textBlockMouseLeftClick ??= new RelayCommand((o) => //команда на клик по TextBlock
        {
            ((DatePicker)o).SelectedDate = DateTime.Now;
        }, (o) => ((DatePicker)o).IsEnabled);

        private Product CreateNewProduct(ProductType productType)
        {
            Product product = new Product
            {
                ProductTypeID = productType.ID,
                Parameters = "",
                Cost = 0.00M,
                Note = "",
                Files = "",
                Quantity = 1,
                ProductCosts = new List<ProductCost> { }        //создать пустой список стоимостей изделия
            };
            product.FilesToList();
            //GetProductParameters(product);
            //GetProductCosts(product);
            //currentOrder.State = currentOrder.OrderState(productsViewSource);
            return product;
        }

        public void LoadContext(long nOrderID)
        {
            ClientCollectionView = CollectionViewSource.GetDefaultView(_contextOrder_.Clients.AsNoTracking().ToList());                         //инициализация коллекции "Клиенты"
            ClientCollectionView.Filter = ClientFilter;                                                                                         //установка обработчика для фильтрации
            ClientCollectionView.MoveCurrentToFirst();                                                                                          //переход на первую запись в коллекции
            UserList = _contextOrder_.Users.AsNoTracking().ToList();                                                                            //список пользователей 
            ManagerList = UserList.Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListManager")).ToList();   //список "Менеджеры"
            DesignerList = UserList.Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListDesigner")).ToList(); //список "Дизайнеры"
            ProductTypeCollectionView = CollectionViewSource.GetDefaultView(_contextOrder_.ProductTypes
                .Include(ProductType => ProductType.CategoryOfProduct)
                .AsNoTracking().ToList());                                                                                                      //инициализация коллекции "Виды изделий"
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("CategoryOfProduct.Name");                                 //группировка изделий по "Категория изделия"
            ProductTypeCollectionView.GroupDescriptions.Add(groupDescription);
            SortDescription sortDescription = new SortDescription("CategoryOfProduct.Name", ListSortDirection.Ascending);                       //сортировка изделий по "Категории"
            ProductTypeCollectionView.SortDescriptions.Add(sortDescription);
            sortDescription = new SortDescription("Name", ListSortDirection.Ascending);
            ProductTypeCollectionView.SortDescriptions.Add(sortDescription);
            CurrentOrder = _contextOrder_.Orders
                .Include(Order => Order.Products)
                .ThenInclude(Product => Product.ProductType)
                .Include(Order => Order.Products)
                .ThenInclude(Product => Product.ProductCosts)
                .FirstOrDefault(Order => Order.ID == nOrderID);                                                                                 //получить текущий заказ (или null для нового)
        }

        private void SetModelAccess() //установить режимы доступа к элементам модели (интерфейса)
        {
            ProductCostAndCostsKVDCostsChange = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductCostAndCostsKVDCostsChange");
            OrderCardDateCompletion = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardDateCompletion");
            OrderCardPrintOrderForm = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardPrintOrderForm");
            OrderCardPrintOrderFormForDesigner = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardPrintOrderFormForDesigner");
            OrderCardProductDesigner = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDesigner");
            OrderCardProductDateProductionLayout = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDateProductionLayout");
            OrderCardProductDateTransferDesigner = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDateTransferDesigner");
            IsManager = !MainWindow.Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ListManager");
        }
    }
}

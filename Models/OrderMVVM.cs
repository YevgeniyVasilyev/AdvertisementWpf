using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public Order()
        {
            //Products = new HashSet<Product>();
            Products = new List<Product> { };
            Payments = new HashSet<Payment>();
            Accounts = new HashSet<Account>();
        }

        private ObservableCollection<Product> _products { get; set; }
        public ICollection<Product> Products
        {
            get => _products;
            set => _products = new ObservableCollection<Product>(value);
        }

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
#if NEWORDER
            get => Products != null ? OrderState(Products) : _state;
#else
            get => Products != null && string.IsNullOrWhiteSpace(_state) ? OrderState(Products) : _state;
#endif
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
        private RelayCommand saveRequest, textBlockMouseLeftClick, showHideNewProductList, selectNewProduct, deleteProduct;
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
        private string _searchTypeOfProduct = "";
        public string SearchTypeOfProduct
        {
            get => _searchTypeOfProduct;
            set
            {
                _searchTypeOfProduct = value;
                ProductTypeCollectionView.Refresh();
            }
        }
        public ICollectionView ClientCollectionView { get; set; }                               //крллеция "Киенты" AsNoTracking, с фильтрацией
        public ICollectionView ProductTypeCollectionView { get; set; }                          //коллекция "Виды изделий" AsNoTracking, с фильтрацией и группировкой
        public List<User> UserList { get; set; }                                                //список "Пользователи" AsNoTracking
        public List<User> ManagerList { get; set; }                                             //список "Менеджеры" AsNoTracking
        public List<User> DesignerList { get; set; }                                            //список "Дизайнеры" AsNoTracking
        public List<Referencebook> ReferencebookList { get; set; }                              //список "Справочники" AsNoTracking
        public List<ReferencebookApplicability> ReferencebookApplicabilityList { get; set; }    //список "Применимость справочника" AsNoTracking
        public List<ReferencebookParameter> ReferencebookParameterList { get; set; }            //список "Справочник параметров изделий" AsNoTracking
        public Order CurrentOrder { get; set; }                                                 //текущий заказ
        public bool ViewMode { get; set; } = false;                                             //режим "просмотр" для карточки заказа
        public int ErrorsCount = 0;

        //определяют доступ к элементам интерфейса
        public bool ProductCostAndCostsKVDCostsChange { get; set; }
        public bool OrderCardDateCompletion { get; set; }
        public bool OrderCardPrintOrderForm { get; set; }
        public bool OrderCardPrintOrderFormForDesigner { get; set; }
        public bool OrderCardProductDesigner { get; set; }
        public bool OrderCardProductDateProductionLayout { get; set; }
        public bool OrderCardProductDateTransferDesigner { get; set; }
        public bool OrderCardProductQuantityAndCost { get; set; }
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

        //TO DO: ИТОГО по изделиям (ОБНОВЛЕНИЕ СТОИМОСТИ !!!!), "внутренности" изделия        

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
                foreach (Product product in CurrentOrder.Products)                                                                                  //инициализация изделий
                {
                    GetProductParameters(product);                                                                                                  //развернуть параметры изделия
                    product.FilesToList();
                    //GetProductCosts(product);
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

        private bool TypeOfProductFilter(object item) //поиск(фильтрация) видов изделий
        {
            ProductType productType = item as ProductType;
            return string.IsNullOrEmpty(_searchTypeOfProduct) || productType.Name.IndexOf(_searchTypeOfProduct, StringComparison.OrdinalIgnoreCase) >= 0;
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
        }, (o) => _contextOrder_ != null && ErrorsCount == 0 && !CurrentOrder.Products.Any(p => p.HasError));

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
                //_ = CountTotals();
            };
        }, (o) => _contextOrder_ != null && ProductTypeCollectionView != null);

        public RelayCommand DeleteProduct => deleteProduct ??= new RelayCommand((o) => //команда "отметить изделие для удаления"
        {
            if (o is Product product)
            {
                _ = CurrentOrder.Products.Remove(product);
            };
        }, (o) => _contextOrder_ != null && CurrentOrder?.Products?.Count > 0);

        public RelayCommand TextBlockMouseLeftClick => textBlockMouseLeftClick ??= new RelayCommand((o) => //команда на клик по TextBlock
        {
            ((DatePicker)o).SelectedDate = DateTime.Now;
        }, (o) => ((DatePicker)o).IsEnabled);

        public void ReferencebookListSelectionChanged(object sender)
        {
            if (sender is ProductParameters productParameter)
            {   //установить набор параметров для вновь  выбранного справочника
                productParameter.ReferencebookParametersList = ReferencebookParameterList.Where(refbookParameters => refbookParameters.ReferencebookID == productParameter.ReferencebookID).ToList();
            }
        }

        public void ProductParametersSourceUpdated(object sender)
        {
            if (sender is Product product)
            {
                product.ListToParameters(); //свернуть список параметров в строку
            }
        }

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
            GetProductParameters(product);
            //GetProductCosts(product);
            CurrentOrder.State = CurrentOrder.OrderState(CurrentOrder.Products);
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
            ProductTypeCollectionView.Filter = TypeOfProductFilter;
            ReferencebookList = _contextOrder_.Referencebook.AsNoTracking().ToList();                                                           //список "Справочники"
            ReferencebookApplicabilityList = _contextOrder_.ReferencebookApplicability.AsNoTracking().ToList();                                 //список "Применимость справочника" AsNoTracking
            ReferencebookParameterList = _contextOrder_.ReferencebookParameter.AsNoTracking().ToList();                                         //список "Справочник параметров изделий" AsNoTracking
            CurrentOrder = _contextOrder_.Orders
                    .Include(Order => Order.Products)
                    .ThenInclude(Product => Product.ProductType)
                    .Include(Order => Order.Products)
                    .ThenInclude(Product => Product.ProductCosts)
                    .FirstOrDefault(Order => Order.ID == nOrderID);                                                                             //получить текущий заказ (или null для нового)
        }

        private void GetProductParameters(Product product)
        {
            IQueryable<ProductParameters> parameterQuery;
            parameterQuery = from pinpt in _contextOrder_.ParameterInProductTypes
                             where pinpt.ProductTypeID == product.ProductTypeID
                             join u in _contextOrder_.Units on pinpt.UnitID equals u.ID
                             select new ProductParameters
                             {
                                 ID = pinpt.ID,
                                 Name = pinpt.Name,
                                 IsRequired = pinpt.IsRequired,
                                 UnitName = u.Name,
                                 ReferencebookID = pinpt.ReferencebookID,
                                 IsRefbookOnRequest = pinpt.IsRefbookOnRequest
                             };
            product.ProductParameter = parameterQuery.AsNoTracking().ToList();          //заполнить список параметров изделия пустым списком из справочника параметров изделий
            product.ParametersToList();                                                 //заполнить список параметров фактическими значениями из строки свойства Parameters
            foreach (ProductParameters productParameter in product.ProductParameter)    //проход по параметрам
            {
                if (productParameter.IsRefbookOnRequest) //задан параметр "выбор справочника по запросу"
                {
                    long categoryOfProductID = _contextOrder_.ProductTypes.FirstOrDefault(pt => pt.ID == product.ProductTypeID).CategoryOfProductID ?? 0;
                    //if (categoryOfProductID is null)
                    //{
                    //    categoryOfProductID = 0;
                    //}
                    //IEnumerable<ReferencebookApplicability> enumerable = ReferencebookApplicabilityList.Where(refApp => refApp.CategoryOfProductID == categoryOfProductID);
                    productParameter.ReferencebookList = ReferencebookList.FindAll(
                        delegate (Referencebook referencebook)
                        {
                            return ReferencebookApplicabilityList.Where(refApp => refApp.CategoryOfProductID == categoryOfProductID)
                            .Any(e => e.ReferencebookID == referencebook.ID); //найти в Referencebooks все ID (справочники), которые применяются для CategoryOfProduct
                        }
                        );
                }
                if (productParameter.ReferencebookID > 0) //для параметра установлено брать значение из справочника 
                {
                    productParameter.ReferencebookParametersList = ReferencebookParameterList.Where(refbookParameters => refbookParameters.ReferencebookID == productParameter.ReferencebookID).ToList();
                }
            }
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
            OrderCardProductQuantityAndCost = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductQuantityAndCostChngeAfterSetToProd");
            IsManager = !MainWindow.Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ListManager");
        }
    }

    public class CostTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommonTextBox { get; set; }
        public DataTemplate ComboboxTextBox { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ProductParameters productParameter)
            {
                return (productParameter.ReferencebookID > 0 || productParameter.IsRefbookOnRequest) ? ComboboxTextBox : CommonTextBox;
            }
            else if (item is OperationInWorkParameter operationInWorkParameter)
            {
                return (operationInWorkParameter.ReferencebookID > 0 || operationInWorkParameter.IsRefbookOnRequest) ? ComboboxTextBox : CommonTextBox;
            }
            return null;
        }
    }

    public class ReferencebookTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommonTextBlock { get; set; }
        public DataTemplate ComboboxRefbookRequestTextBox { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ProductParameters productParameter)
            {
                return productParameter.IsRefbookOnRequest ? ComboboxRefbookRequestTextBox : CommonTextBlock;
            }
            else if (item is OperationInWorkParameter operationInWorkParameter)
            {
                return operationInWorkParameter.IsRefbookOnRequest ? ComboboxRefbookRequestTextBox : CommonTextBlock;
            }
            return null;
        }
    }
}


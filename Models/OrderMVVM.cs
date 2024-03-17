using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace AdvertisementWpf.Models
{
    public partial class Order : INotifyPropertyChanged
    {
        public Order()
        {
            //Products = new HashSet<Product>();
            Products = new List<Product> { };
            Payments = new List<Payment> { };
            Accounts = new List<Account> { };
        }

        private ObservableCollection<Product> _products { get; set; }
        public ICollection<Product> Products
        {
            get => _products;
            set => _products = new ObservableCollection<Product>(value);
        }

        private ObservableCollection<Payment> _payments { get; set; }
        public ICollection<Payment> Payments
        {
            get => _payments;
            set => _payments = new ObservableCollection<Payment>(value);
        }

        private ObservableCollection<Account> _accounts { get; set; }
        public ICollection<Account> Accounts
        {
            get => _accounts;
            set => _accounts = new ObservableCollection<Account>(value);
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
            get => Products != null ? OrderState(Products) : _state;
#if NEWORDER
#else
            //get => Products != null && string.IsNullOrWhiteSpace(_state) ? OrderState(Products) : _state;
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

    internal class OrderViewModel : INotifyPropertyChanged
    {
        private RelayCommand saveRequest, textBlockMouseLeftClick, showHideFrameworkElement, selectNewProduct, deleteProduct, newFileToProduct, deleteFileFromProduct, 
                             openFolderWithFileProduct, openFileProductInShell, printOrderForm, printOrderFormForDesigner, newPayment, savePayment, loadAccount, saveAccount,  newAccount, newAct,
                             manualInputAccount, deleteAccount, deleteAccountDetail, deleteActDetail, printAccount, printAct, 
                             loadTechCard, saveTechCard, deleteTechCard, newTechCard, newTechCardWork, newTechCardWorkOperation, selectNewOperation, sendTechCardToProduction, recallTechCardFromProduction,
                             printTechCard, newOperationInWorkFile, deleteOperationInWorkFile, openOperationInWorkFile;
        private App.AppDbContext _contextOrder_ = CreateDbContext.CreateContext();              //контекст для заказа
        private App.AppDbContext _contextPayment_ = CreateDbContext.CreateContext();            //контекст для платежей Заказа
        private App.AppDbContext _contextAccount_ = CreateDbContext.CreateContext();            //контекст для ПУД Заказа
        private App.AppDbContext _contextTechCard_ = CreateDbContext.CreateContext();           //контекст для Техкарт
        private string _pathToFilesOfProduct = "";                                              //путь к уелевой папке хранения файлов иллюстраций изделия
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
        private WorkInTechCard _selectedWorkForNewOperation;
        public WorkInTechCard SelectedWorkForNewOperation
        {
            get => _selectedWorkForNewOperation;
            set
            {
                _selectedWorkForNewOperation = value;
                OperationCollectionView.Refresh();
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
        public List<object> TotalProductCosts { get; set; } = new List<object>();               //список стоимостей, затрат и маржи по заказу
        public ObservableCollection<Payment> ListPayment { get; set; }                          //список платежей
        private ObservableCollection<Account> _listPAD { get; set; }
        public ObservableCollection<Account> ListPAD
        {
            get => _listPAD;
            set
            {
                _listPAD = value;
                NotifyPropertyChanged("ListPAD");
            }
        }                                         //список ПУД
        private ObservableCollection<TechCard> _listTechCard { get; set; }
        public ObservableCollection<TechCard> ListTechCard
        {
            get => _listTechCard;
            set
            {
                _listTechCard = new ObservableCollection<TechCard>(value);
                NotifyPropertyChanged("ListTechCard");
            }
        }                                   //список Техкард заказа
        public List<Account> AccountForPayment { get; set; }                                    //список счетов для Payment
        private List<Contractor> _listContractor;
        public List<Contractor> ListContractor
        { 
            get => _listContractor;
            set
            {
                _listContractor = value;
                NotifyPropertyChanged("ListContractor");
            }
        }                                               //список контрагентов для ПУДов
        public ICollectionView OperationCollectionView { get; set; }                            //коллекция операций в работе техкарты AsNoTraking, с фильтрацией
        public Order CurrentOrder { get; set; }                                                 //текущий заказ
        public bool ViewMode { get; set; } = false;                                             //режим "просмотр" для карточки заказа
        public int ErrorsCount = 0;                                                             //счетчик количества ошибок на форме карточки Заказа
        private enum TechCardObjectType { TechCard, Work, Operation };                          //виды объектов техкарты (используется при добавлении новых обхектов в техкарту)

        //определяют доступ к элементам интерфейса
        public bool ProductCostAndCostsKVDCostsChange { get; set; }
        public bool OrderCardDateCompletion { get; set; }
        public bool OrderCardPrintOrderForm { get; set; }
        public bool OrderCardPrintOrderFormForDesigner { get; set; }
        public bool OrderCardProductDesigner { get; set; }
        public bool OrderCardProductDateProductionLayout { get; set; }
        public bool OrderCardProductDateTransferDesigner { get; set; }
        public bool OrderCardProductQuantityAndCost { get; set; }
        public bool OrderCardProductDeleteAfterSetToProd { get; set; }
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

        private int _selectedAccountIndex { get; set; } = 0;
        public int SelectedAccountIndex
        {
            get => _selectedAccountIndex; 
            set
            {
                SelectedActIndex = 0;
                _selectedAccountIndex = value;
                //NotifyPropertyChanged("SelectedAccountIndex");
            }
        }
        private int _selectedActIndex { get; set; } = 0;
        public int SelectedActIndex
        { 
            get => _selectedActIndex;
            set
            {
                _selectedActIndex = value;
                NotifyPropertyChanged("SelectedActIndex");
            }
        }
        private int _selectedPaymentIndex { get; set; } = 0;
        public int SelectedPaymentIndex
        {
            get => _selectedPaymentIndex;
            set => _selectedPaymentIndex = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public OrderViewModel(long nOrderID = 0, bool lViewMode = false)
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Загрузка данных по заказу ...", Cursors.Wait);
                ViewMode = lViewMode;
                LoadOrderContext(nOrderID);     //загрузить данные в контекст
                SetModelAccess();               //установить режимы доступа к элементам интерфейса
                if (CurrentOrder is null)       //будет новый
                {
                    CreateNewOrder();           //создать новый заказа
                }
                foreach (Product product in CurrentOrder.Products)                                                                                  //инициализация изделий
                {
                    GetProductParameters(product);                                                                                                  //развернуть параметры изделия
                    product.FilesToList();                                                                                                          //развернуть список файлов изделия
                    GetProductCosts(product);                                                                                                       //сформировать список стоимостей изделия
                }
                TotalProductCostsList();                                                                                                            //список на вкладке "Стоимость и затраты по КВД"
                LoadPaymentContext();                                                                                                               //загрузить платежи на вкладке "Платежи"
                LoadTechCardContext(false);                                                                                                         //загрузить техкарты в "тихом" режиме
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
            _contextOrder_?.Dispose();
            _contextOrder_ = null;

            _contextPayment_?.Dispose();
            _contextPayment_ = null;

            _contextAccount_?.Dispose();
            _contextAccount_ = null;

            _contextTechCard_?.Dispose();
            _contextTechCard_ = null;
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

        private bool OperationFilter(object item)
        {
            Operation operation = item as Operation;
            return (bool)operation.TypeOfActivityInOperations.Any(t => t.TypeOfActivityID.Equals(SelectedWorkForNewOperation?.TypeOfActivityID)); //если операция относится к КВД выбранной работы
        }

        private void CreateNewOrder()
        {
            IsNewOrder = true;
            CurrentOrder = new Order
            {
                //ClientID = (ClientCollectionView?.CurrentItem as Client)?.ID ?? 0,  //УБРАТЬ !!! сделать не заполняемым
                DateAdmission = DateTime.Today,
                OrderEnteredID = MainWindow.Userdata.ID,
                ManagerID = MainWindow.Userdata.ID,
            };
            _contextOrder_.AddToContext(CurrentOrder);
        }
        private string GenerateNewOrderNumber(long Number)
        {
            return Number.ToString().Trim().PadLeft(10, '0');
        }

        public RelayCommand SaveRequest => saveRequest ??= new RelayCommand((o) =>  // команда сохранения Заказа
        {
            _contextOrder_.SaveContext();
            if (IsNewOrder)
            {
                CurrentOrder.Number = GenerateNewOrderNumber(CurrentOrder.ID);
                _contextOrder_.SaveContext(IsMutedMode: true);   //сохранить в "тихом режиме"
                IsNewOrder = false;
            }
            MainWindow.statusBar.ClearStatus();
        }, (o) => CanSaveOrder());

        public RelayCommand ShowHideFrameworkElement => showHideFrameworkElement ??= new RelayCommand((o) => //команда "отобразить/скрыть элемент"
        {
            ((FrameworkElement)o).Visibility = ((FrameworkElement)o).Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }, null);

        public RelayCommand SelectNewProduct => selectNewProduct ??= new RelayCommand((o) => //команда "выбрать изделеи из списка выбора нового изделия"
        {
            if (CreateNewProduct((ProductType)o) is Product product)
            {
                CurrentOrder.Products.Add(product);
                product.ProductType = _contextOrder_.AddSingleToContext(product.ProductType, delegate (ProductType p) { return p.ID == product.ProductTypeID; }); //добавить св-во навигации ProductType
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

        public RelayCommand NewFileToProduct => newFileToProduct ??= new RelayCommand((o) => //команда добавления нового файла иллюстраций в описание изделия
        {
            if (o is Product product)
            {
                try
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Multiselect = true;
                    if ((bool)openFileDialog.ShowDialog()) // Открываем окно диалога с пользователем
                    {
                        foreach (string fullFileName in openFileDialog.FileNames)   //проход по всем выделенным файлам
                        {
                            product.FilesList.Add(Path.GetFileName(fullFileName));  //добавляем в список
                        }
                        product.ListToFiles();                                      //свернуть список файлов в строку
                        CopyFileToPathToFilesOfProduct(openFileDialog.FileNames);   //копируем файл в целевую папку
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка выбора папки", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }, (o) => _contextOrder_ != null && CurrentOrder?.ID > 0); //добавление возможно только для сохраненного заказа, т.к. его номер участвует в формированиия пути к целевой папке

        public RelayCommand DeleteFileFromProduct => deleteFileFromProduct ??= new RelayCommand((o) => //команда удаления файла иллюстраций в описании изделия
        {
            Product product = ((object[])o)[0] as Product;
            string File = ((object[])o)[1] as string;
            product?.FilesList?.Remove(File);
        }, (o) => ((object[])o)?[0] is Product product && product?.FilesList?.Count > 0);

        public RelayCommand OpenFolderWithFileProduct => openFolderWithFileProduct ??= new RelayCommand((o) => //команда открытия папки с файлами иллюстраций изделия
        {
            _ = Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/n, {Path.Combine(_pathToFilesOfProduct, CurrentOrder.Number)}" });
        }, (o) => CurrentOrder?.ID > 0 && Directory.Exists(Path.Combine(_pathToFilesOfProduct, CurrentOrder.Number)));

        public RelayCommand OpenFileProductInShell => openFileProductInShell ??= new RelayCommand((o) => //команда открытия файла иллюстраций изделия во внешней программе
        {
            string fileName = (string)o;
            OpenFileInOSShell.OpenFile(Path.Combine(Path.Combine(_pathToFilesOfProduct, CurrentOrder.Number), fileName));
        }, (o) => CurrentOrder?.ID > 0);

        public RelayCommand PrintOrderForm => printOrderForm ??= new RelayCommand((o) => //команда печати бланка заказа
        {
            OrderFormSendToPrint(o as ListView);
        }, (o) => CurrentOrder?.ID > 0);

        public RelayCommand PrintOrderFormForDesigner => printOrderFormForDesigner ??= new RelayCommand((o) => //команда печати бланка заказа для конкретного дизайнера(-ов)
        {
            List<long> designerIDList = new List<long> { };
            Button button = ((object[])o)[0] as Button;
            ListView listView = ((object[])o)[1] as ListView;
            button.ContextMenu.Items.Clear();
            foreach (Product product in CurrentOrder.Products)
            {
                if (!(product.DesignerID is null) && !designerIDList.Contains((long)product.DesignerID))
                {
                    foreach (User designer in DesignerList)
                    {
                        if (designer.ID == product.DesignerID)
                        {
                            MenuItem menuItem = new MenuItem { Header = designer.FullUserName, Tag = (long)product.DesignerID, IsCheckable = false };
                            menuItem.Click += DesignerContextMenuItem_Click;
                            _ = button.ContextMenu.Items.Add(menuItem);
                            designerIDList.Add((long)product.DesignerID);
                        }
                    }
                }
            }
            if (designerIDList.Count > 0)
            {
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Visibility = Visibility.Visible;
                button.ContextMenu.IsOpen = true;
            }
            else
            {
                OrderFormSendToPrint(listView);
            }

            void DesignerContextMenuItem_Click(object sender, RoutedEventArgs e)
            {
                button.ContextMenu.Visibility = Visibility.Hidden;
                OrderFormSendToPrint(listView, (long)(sender as MenuItem).Tag);
            }
        }, (o) => CurrentOrder?.ID > 0);

        public RelayCommand NewPayment => newPayment ??= new RelayCommand((o) => //команда создания нового платежа
        {
            Payment payment = new Payment
            {
                OrderID = CurrentOrder.ID,
                PaymentAmount = 0,
                PaymentDocNumber = "",
                PurposeOfPayment = 0,
                TypeOfPayment = 0
            };
            ListPayment.Add(payment);
            _contextPayment_.AddToContext(payment);
        }, (o) => _contextPayment_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPaymentsNewChangeDelete"));

        public RelayCommand SavePayment => savePayment ??= new RelayCommand((o) => //команда сохранения платежей
        {
            _contextPayment_.SaveContext();
            MainWindow.statusBar.ClearStatus();
        }, (o) => _contextPayment_ != null && CurrentOrder?.ID > 0 && ErrorsCount == 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPaymentsNewChangeDelete"));

        public RelayCommand LoadAccount => loadAccount ??= new RelayCommand((o) => //команда загрузки ПУД
        {
            LoadAccountContext();
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0);

        public RelayCommand SaveAccount => saveAccount ??= new RelayCommand((o) => //команда сохранения ПУД
        {
            _contextAccount_.SaveContext();
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && ListPAD != null && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"));

        public RelayCommand NewAccount => newAccount ??= new RelayCommand((o) => //команда создания нового Счета
        {
            CreateNewAccount();
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"));

        public RelayCommand NewAct => newAct ??= new RelayCommand((o) => //команда создания нового Акта
        {
            CreateNewAct(o as Account);
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && (o as Account)?.ID > 0 && CanCreateNewAct(o as Account) && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"));

        public RelayCommand DeleteAccount => deleteAccount ??= new RelayCommand((o) => //команда удалить Счет
        {
            Account account = o as Account;
            _ = ListPAD.Remove(account);
            if (account?.ID > 0)
            {
                _contextAccount_.DeleteFromContext(account);
            }
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"));

        public RelayCommand DeleteAccountDetail => deleteAccountDetail ??= new RelayCommand((o) => //команда удалить детализацию Счета
        {
            Account account = ((object[])o)[0] as Account;
            AccountDetail accountDetail = ((object[])o)[1] as AccountDetail;
            if (accountDetail != null)
            {
                _ = account.DetailsList.Remove(accountDetail);
                account.ListToDetails();
            }
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && o != null && (((object[])o)[0] as Account).Acts.Count == 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"));

        public RelayCommand DeleteActDetail => deleteActDetail ??= new RelayCommand((o) => //команда удалить детализацию Счета
        {
            Account account = ((object[])o)[0] as Account;
            Act act = ((object[])o)[1] as Act;
            AccountDetail accountDetail = ((object[])o)[2] as AccountDetail;
            _ = act.ListProductInAct.Remove(accountDetail.ProductID);
            act.ListToProductInAct();
            act.CreateDetailsList(account); //лучше использовать account, т.к. Account для Act может быть пустым для нового и не сохраненного счета
            foreach (Act a in account.Acts)
            {
                a.ActNumber = GetNewActNumber(a, account); //перенумерация актов
            }
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"));

        public RelayCommand ManualInputAccount => manualInputAccount ??= new RelayCommand((o) => //команда приустановке признака "ручной ввод счета"
        {
            Account account = o as Account;
            account.DetailsList = CreateNewAccountDetails(account);
#if NEWORDER
#endif
            account.ListToDetails(); //свернуть детали счета
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0);

        public RelayCommand PrintAccount => printAccount ??= new RelayCommand((o) => //команда Печать счета
        {
            Account account = ((object[])o)[0] as Account;
            DateTime dateTime = (DateTime)((object[])o)[1];
            bool withSignature = (bool)((object[])o)[2];
            AccountPrint(account, dateTime, withSignature);
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && o != null && (((object[])o)[0] as Account)?.ID > 0);

        public RelayCommand PrintAct => printAct ??= new RelayCommand((o) => //команда Печать счета
        {
            Account account = ((object[])o)[0] as Account;
            Act act = ((object[])o)[1] as Act;
            DateTime dateTime = (DateTime)((object[])o)[2];
            bool templateAct = (bool)((object[])o)[3];
            bool templateSFTN = (bool)((object[])o)[4];
            bool templateUPD = (bool)((object[])o)[5];
            ActPrint(account, act, dateTime, templateAct, templateSFTN, templateUPD);
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0 && o != null && (((object[])o)[1] as Act)?.ID > 0);

        public RelayCommand LoadTechCard => loadTechCard ??= new RelayCommand((o) => //команда Загрузить техкарты
        {
            LoadTechCardContext(true);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0);

        public RelayCommand SaveTechCard => saveTechCard ??= new RelayCommand((o) => //команда Загрузить техкарты
        {
            //формируем номера для использования при печати техкарты
            //«номер заказа».«порядковый номер изделия в заказе».«КВД».«порядковый номер операции»
            short nTechCard = 1;
            short nOperationInWork = 1;
            foreach (TechCard techCard in ListTechCard)
            {
                techCard.Number = $"{CurrentOrder.Number.TrimStart('0')}.{nTechCard++}";
                foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                {
                    workInTechCard.Number = $"{techCard.Number}.{workInTechCard.TypeOfActivity.Code.Trim()}";
                    nOperationInWork = 1;
                    foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                    {
                        operationInWork.Number = $"{workInTechCard.Number}.{nOperationInWork++}";
                    }
                }
            }
             _contextTechCard_.SaveContext();
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"));

        public RelayCommand NewTechCard => newTechCard ??= new RelayCommand((o) => //команда Добавить техкарту
        {
            AddNewObjectInTechCard(TechCardObjectType.TechCard, null);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"));

        public RelayCommand NewTechCardWork => newTechCardWork ??= new RelayCommand((o) => //команда Добавить работу в техкарту
        {
            AddNewObjectInTechCard(TechCardObjectType.Work, o);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"));

        public RelayCommand NewTechCardWorkOperation => newTechCardWorkOperation ??= new RelayCommand((o) => //команда Добавить операцию в работу
        {
            if (o is WorkInTechCard)
            {
                SelectedWorkForNewOperation = o as WorkInTechCard;
            }
            else
            {
                SelectedWorkForNewOperation = (WorkInTechCard)GetParentTreeViewItem(o, 1); //найти родителя на уровне "Работа"
            }
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && o != null && (o is WorkInTechCard || o is OperationInWork) && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"));

        public RelayCommand DeleteTechCard => deleteTechCard ??= new RelayCommand((o) => //команда Загрузить техкарты
        {
            if (o is TechCard techCard)
            {
                foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)                 //удаляем дочерние Работы для изделия
                {
                    foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)    //удаляем дочерние Операции для работы
                    {
                        _contextTechCard_.DeleteFromContext(operationInWork);                       //удалить Операцию из контекста
                    }
                    _contextTechCard_.DeleteFromContext(workInTechCard);                            //удалить Работу из контекста
                }
                techCard.Product.IsHasTechcard = false;                                             //убрать признак наличия Техкарты
                techCard.Product.DateTransferProduction = null;                                     //убрать дату передачи в производство
                techCard.Product.DateManufacture = null;                                            //убрать дату Изготовления
                _ = ListTechCard.Remove(techCard);
                _contextTechCard_.DeleteFromContext(techCard);                                      //удалить Техкарту из контекста
                //далее внести аналогичные изменения в отдельные параметры Изделия
                Product product = CurrentOrder.Products.First(product => product.ID == techCard.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                product.IsHasTechcard = false;
                product.DateTransferProduction = null;                                              //убрать дату передачи в производство
                product.DateManufacture = null;                                                     //убрать дату Изготовления
            }
            else if (o is WorkInTechCard workInTechCard)
            {
                foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                {
                    _contextTechCard_.DeleteFromContext(operationInWork);
                }
                TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard, 0);                   //найти родительскую Техкарту
                _ = tc.WorkInTechCards.Remove(workInTechCard);                                      //удалить Работу из техкарты
                 _contextTechCard_.DeleteFromContext(workInTechCard);                               //удалить Работу из контекста
                //далее внести изменения в отдельные параметры Изделия
                Product product = CurrentOrder.Products.First(product => product.ID == tc.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                product.DateManufacture = null;                                                     //убрать дату Изготовления
            }
            else if (o is OperationInWork operationInWork)
            {
                WorkInTechCard wTC = (WorkInTechCard)GetParentTreeViewItem(operationInWork, 1);
                _ = wTC.OperationInWorks.Remove(operationInWork);
                _contextTechCard_.DeleteFromContext(operationInWork);
            }
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"));

        public RelayCommand SelectNewOperation => selectNewOperation ??= new RelayCommand((o) => //команда Добавить операцию в работу
        {
            AddNewObjectInTechCard(TechCardObjectType.Operation, o);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"));

        public RelayCommand SendTechCardToProduction => sendTechCardToProduction ??= new RelayCommand((o) => //команда Передать техкарту в производство
        {
            ToProduction(o);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && CanSendToProduction(o));

        public RelayCommand RecallTechCardFromProduction => recallTechCardFromProduction ??= new RelayCommand((o) => //команда Отозвать техкарту с производства
        {
            FromProduction(o);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && CanRecallFromProduction(o));

        public RelayCommand PrintTechCard => printTechCard ??= new RelayCommand((o) => //команда Печать техкарты или ее части
        {
            Button button = ((object[])o)[0] as Button;
            TechCardPrepeareToPrint(((object[])o)[1], button);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && o != null && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardPrint"));

        public RelayCommand NewOperationInWorkFile => newOperationInWorkFile ??= new RelayCommand((o) => //команда Добавить новый файл с сописанием к операции
        {
            OperationInWork operationInWork = o as OperationInWork;
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = true;
                if ((bool)openFileDialog.ShowDialog()) // Открываем окно диалога с пользователем
                {
                    foreach (string fullFileName in openFileDialog.FileNames)
                    {
                        operationInWork.FilesList.Add(Path.GetFullPath(fullFileName));
                    }
                    operationInWork.ListToFiles();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка выбора папки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && o != null && o is OperationInWork);
 
        public RelayCommand DeleteOperationInWorkFile => deleteOperationInWorkFile ??= new RelayCommand((o) => //команда Удалить файл с сописанием к операции
        {
            OperationInWork operationInWork = ((object[])o)[0] as OperationInWork;
            string fileName = ((object[])o)[1] as string;
            if (operationInWork != null && fileName != null)
            {
                _ = operationInWork.FilesList.Remove(fileName);
                operationInWork.ListToFiles();
            }
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && ((object[])o)?[0] is OperationInWork operationInWork && operationInWork?.FilesList?.Count > 0);

        public RelayCommand OpenOperationInWorkFile => openOperationInWorkFile ??= new RelayCommand((o) => //команда Открыть файл с сописанием к операции во внещней проге
        {
            string fileName = o as string;
            OpenFileInOSShell.OpenFile(fileName);
        }, (o) => _contextTechCard_ != null && CurrentOrder?.ID > 0 && o != null);

        private void CopyFileToPathToFilesOfProduct(string[] aFullFileNames)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_pathToFilesOfProduct) && Directory.Exists(_pathToFilesOfProduct))
                {
                    string destinationPath = Path.Combine(_pathToFilesOfProduct, CurrentOrder.Number);
                    //string destinationPath = System.IO.Path.Combine(_pathToFilesOfProduct, "0000000001");
                    if (!Directory.Exists(destinationPath))
                    {
                        _ = Directory.CreateDirectory(destinationPath);
                    }
                    string ending = aFullFileNames.Length > 1 && aFullFileNames.Length < 5 ? "а" : aFullFileNames.Length >= 5 ? "ов" : "";
                    bool lNeedDeleteFile = MessageBox.Show($"В папку {destinationPath} будет скопировано {aFullFileNames.Length} файл{ending}.\n\n Удалить файл(-ы) в исходной папке?",
                        "Копирование файлов", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
                    foreach (string fullFileName in aFullFileNames)
                    {
                        FileSystem.CopyFile(fullFileName, Path.Combine(destinationPath, Path.GetFileName(fullFileName)), UIOption.AllDialogs, UICancelOption.DoNothing);
                        if (lNeedDeleteFile)
                        {
                            FileSystem.DeleteFile(fullFileName, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        }
                    }
                }
                else
                {
                    _ = MessageBox.Show("Не задан путь к папке размещения файлов заказа или папка не существует!" + "\n" + "Копирование файла не возможно!",
                        "Ошибка копирования файла", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (OperationCanceledException ex)
            {
                _ = MessageBox.Show("Копирование отменено или возникла неопределенная ошибка ввода-вывода!" + "\n" + ex.Message + "\n" + ex?.InnerException?.Message ?? "",
                    "Ошибка копирования файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка копирования файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveOrder()
        {
            bool CanSaveOrder = _contextOrder_ != null && ErrorsCount == 0 && !CurrentOrder.Products.Any(p => p.HasError) && CurrentOrder.ClientID > 0;
            foreach (Product product in CurrentOrder?.Products)
            {
                foreach (ProductParameters productParameter in product?.ProductParameter)                                           //проход по параметра каждого продукта
                {
                    if (productParameter.IsRequired)                                                                                //параметр обязательный к заполнению
                    {
                        if (productParameter.ReferencebookID > 0 && productParameter.ParameterID is null)                           //выбран справочник, но параметр из справочника не выбран
                        {
                            return false;
                        }
                        if (productParameter.ReferencebookID is null && string.IsNullOrWhiteSpace(productParameter.ParameterValue)) //параметр не справочный и не заполнен
                        {
                            return false;
                        }
                    }
                }
            }
            return CanSaveOrder;
        }

        private bool CanSendToProduction(object sender)
        {
            bool CanSendToProduction = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardTransferToProduction");
            TechCard techCard = (TechCard)GetParentTreeViewItem(sender, 0);                     //ищем корневого родителя на уровне TechCard
            if (techCard != null)
            {
                CanSendToProduction &= !techCard.Product.DateTransferProduction.HasValue;       //дата передачи в производство еще не установлена
            }
            return CanSendToProduction;
        }

        private bool CanRecallFromProduction(object sender)
        {
            bool CanRecallFromProduction = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardRecallFromProduction");
            TechCard techCard = (TechCard)GetParentTreeViewItem(sender, 0);                     //ищем корневого родителя на уровне TechCard
            if (techCard != null)
            {
                CanRecallFromProduction &= techCard.Product.DateTransferProduction.HasValue;    //дата передачи в производство установлена
            }
            return CanRecallFromProduction;
        }

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

        public void TypeOfActivityCostSourceUpdated(object sender)
        {
            if (sender is Product product)
            {
                product.Cost = product.ProductCosts.Sum(c => c.Cost);
                TotalProductCostsList();
            }
        }

        public void AccountsListComboBoxSelectionChanged(object sender)
        {
            Payment payment = sender as Payment;
            if (payment != null)
            {
                _contextPayment_.AddReferenceToContext(payment, "Account");
            }
        }

        public void DeletePayment(object sender)
        {
            if (sender is Payment payment)
            {
                _ = ListPayment.Remove(payment);
                _contextPayment_.DeleteFromContext(payment);
            }
        }

        public void AccountDetailTextBoxChanged(object sender)
        {
            Account account = sender as Account;
            account.ListToDetails();            //свернуть детали счета
            foreach (Act act in account.Acts)
            {
                act.CreateDetailsList();        //создать детали Актов для обновления
            }
        }

        public bool CheckTechCard(object sender) //команда отметить/снять отметку с техкарты
        {
            if (sender is TechCard techCard && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) //при щелчке мышью был нажат Ctrl
            {
                techCard.IsChecked = !techCard.IsChecked;
                techCard.IsPrinted = techCard.IsChecked;
                return true;
            }
            return false;
        }

        public bool CheckWorkInTechCard(object sender, int ClickCount) //команда отметить/снять отметку с техкарты
        {
            TextBlock textBlock = sender as TextBlock;
            if (ClickCount >= 2 && textBlock?.DataContext is WorkInTechCard workInTC && workInTC.OperationInWorks.Count == 0)
            {
                textBlock.Visibility = Visibility.Collapsed; //скрыть самого себя
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && textBlock?.DataContext is WorkInTechCard workInTechCard) //при щелчке мышью был нажат Ctrl
            {
                workInTechCard.IsChecked = !workInTechCard.IsChecked;
                workInTechCard.IsPrinted = workInTechCard.IsChecked; //отмечен, значит печатается
                //TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard, 0);
                //if (tc != null) //для родителя установить/снять отметку
                //{
                //    tc.IsChecked = tc.WorkInTechCards.Any(w => w.IsChecked); //если хоть один из дочерних отмечен
                //    tc.IsPrinted = tc.IsChecked; //отмечен, значит печатается
                //}
                return true;
            }
            return false;
        }

        public void WorkTypeOfActivityComboBoxSelectionChanged(object sender)
        {
            if (sender is WorkInTechCard workInTechCard)
            {
                _contextTechCard_.AddReferenceToContext(workInTechCard, "TypeOfActivity");
                workInTechCard.IsSelected = true;
                workInTechCard.NotifyPropertyChanged("TypeOfActivity_CodeName");
            }
        }

        public void WorkTypeOfActivityComboBoxGotFocus(object sender)
        {
            if (sender is WorkInTechCard workInTechCard)
            {
                workInTechCard.IsSelected = true;
            }
        }

        public void WorkInTechCardComboBoxDropDownClosed(object sender)
        {
            if (sender is WorkInTechCard workInTechCard)
            {
                workInTechCard.IsSelected = true;
                workInTechCard.NotifyPropertyChanged("TypeOfActivity_CodeName");
                TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard, 0); //найти родителя
                if (tc != null)
                {
                    CollectionViewSource.GetDefaultView(tc.WorkInTechCards).Refresh(); //обновить текущее представление узла
                }
            }
        }

        public void DeleteAct(object sender1, object sender2)
        {
            Account account = sender1 as Account;
            Act act = sender2 as Act;
            _ = account.Acts.Remove(act);
            _contextAccount_.DeleteFromContext(act);
        }

        private string GetNewAccountNumber(ref Account account)
        {
            short nMaxAccountNumber = 1;
            foreach (Account acc in ListPAD)
            {
                string[] s = acc.AccountNumber.Split('/');
                if (s.Length > 1 && short.TryParse(s[1], out short nMax))
                {
                    nMaxAccountNumber = (short)Math.Max(nMax + 1, nMaxAccountNumber);
                }
            }
            return $"{CurrentOrder.Number.TrimStart('0')}/{nMaxAccountNumber}/{account.Contractor.AbbrForAcc}";
        }

        private void CreateNewAccount()
        {
            try
            {
                if (ListPAD is null || ListPAD.Count == 0)            //счета еще не загружены
                {
                    LoadAccountContext(false);      //грузим "молча"
                }
                Contractor contractor = ListContractor.FirstOrDefault();
                Account account = new Account
                {
                    OrderID = CurrentOrder.ID,
                    IsManual = false,
                    ContractorID = contractor.ID,
                    ContractorInfoForAccount = contractor.ContractorInfoForAccount,
                    ContractorName = contractor.Name,
                    AccountNumber = ""
                };
                _contextAccount_.AddToContext(account);
                _contextAccount_.AddReferenceToContext(account, "Contractor");
                contractor = account.Contractor;
                _contextAccount_.AddReferenceToContext(contractor, "Bank");     //загрузить св-во навигации Bank
                account.AccountNumber = GetNewAccountNumber(ref account);       //сформировать номер счета
                _contextAccount_.AddReferenceToContext(account, "Order");       //загрузить св-во навигации Order
                Order order = account.Order;
                _contextAccount_.AddCollectionToContext(order, "Products");     //загрузить коллекцию навигации Products
                _contextAccount_.AddReferenceToContext(order, "Client");        //загрузить св-во навигации Client
                Client client = order.Client;
                _contextAccount_.AddReferenceToContext(client, "Bank");         //загрузить св-во навигации Bank
                foreach (Product product in order.Products)
                {
                    _contextAccount_.AddReferenceToContext(product, "ProductType"); //загрузить св-во навигации ProductType
                }
                account.DetailsList = CreateNewAccountDetails(account);
#if NEWORDER
#endif
                account.ListToDetails();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования счета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ObservableCollection<AccountDetail> CreateNewAccountDetails(Account account)
        {
            ObservableCollection<AccountDetail> accountDetails = new ObservableCollection<AccountDetail> { };
            if (account.IsManual) //ручной ввод деталей счета
            {
                accountDetails.Add(new AccountDetail { ProductID = 0, ProductInfoForAccount = "Оплата по договору № ...", Quantity = 1, UnitName = "шт.", Cost = account.Order?.OrderCost ?? 
                                   account.Order.Products.Sum(p => p.Cost) });
            }
            else //загрузка из номенклатуры изделий заказа. Грузим только не включенные в другой счет
            {
                bool lIncluded;
                foreach (Product product in account.Order.Products)
                {
                    lIncluded = false;
                    foreach (Account a in ListPAD) //проходим по счетам
                    {
                        if ((bool)(a.DetailsList?.Any(ad => ad.ProductID.Equals(product.ID))))
                        {
                            lIncluded = true;
                            break; //выход во внешний цикл "по счетам"
                        }
                    }
                    if (!lIncluded) //в итоге ни в одном из счетов изделия нет
                    {
                        accountDetails.Add(new AccountDetail { ProductID = product.ID, ProductInfoForAccount = product.ProductType.Name, Quantity = product.Quantity, UnitName = "шт.", Cost = product.Cost });
                    }
                }
            }
            return accountDetails;
        }

        private bool CanCreateNewAct(Account account)
        {
            if (account is null)
            {
                return false;
            }
            return account.IsManual ? account.Acts?.Count == 0 : (account.Acts?.Count < account.DetailsList?.Count && GetProductsInAct(account).Count > 0); //в счете кол-во актов д.б. меньше кол-ва изделий в счете
        }

        private List<long> GetProductsInAct(Account account)
        {
            List<long> listProductsID = new List<long> { };
            if (!account.IsManual) //счет создан НЕ вручную, для ручного возврат пустого списка
            {
                listProductsID = account.Order.Products.Select(product => product.ID).ToList(); //список всех изделий заказа
                foreach (Account acc in ListPAD) //проверяем было ли изделие уже отгружено в другом акте любого счета
                {
                    if (acc != account)   //выполнить для всех счетов, кроме заданного
                    {
                        listProductsID = listProductsID.Except(acc.DetailsList.Select(dl => dl.ProductID).ToList()).ToList(); //исключаем изделия, включенные в другие счета, кроме заданного
                    }
                    if (acc.Acts != null)
                    {
                        foreach (Act act in acc.Acts)
                        {
                            _ = listProductsID.RemoveAll(delegate (long productInAct)  //в сухом остатке останутся только изделия не входящие ни в один акт, либо пустой список
                            {
                                return act.ListProductInAct.Contains(productInAct);
                            });
                        }
                    }
                }
            }
            return listProductsID;  //список изделий, не включенных ни в один акт
        }

        private void CreateNewAct(Account account)
        {
            try
            {
                Act act = new Act
                {
                    ActDate = DateTime.Now,
                    AccountID = account.ID,
                };
                _contextAccount_.AddToContext(act);
                _contextAccount_.AddReferenceToContext(act, "Account");             //загрузить св-во навигации Account
                Contractor contractor = act.Account.Contractor;
                _contextAccount_.AddReferenceToContext(account, "Contractor");      //загрузить св-во навигации Contractor
                List<long> listProductID = new List<long> { };
                foreach (Act a in account.Acts)                                     //получить список всех изделий, уже входящих в акты данного счета
                {
                    listProductID.AddRange(a.DetailsList?.Select(d => d.ProductID));
                }
                act.ListProductInAct = account.DetailsList.Select(d => d.ProductID).Except(listProductID).ToList(); //список productID не входящих в другие акты
                act.ListToProductInAct();                                           //свернуть список productID входящих в акт изделий в строку
                act.CreateDetailsList(account);
                act.ActNumber = GetNewActNumber(act, account);                      //сформировать новые номер акта для заданного счета
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования актв", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetNewActNumber(Act act, Account account)    //создать Акт для Счета
        {
            string sNewActNumber;
            if (account.IsManual)
            {
                sNewActNumber = account.AccountNumber;
            }
            else
            {
                if (act.DetailsList.Count < account.DetailsList.Count) //если количество изделий в счете(заказе) больше чем в текущем Акте, то это означает частичная отгрузка
                {
                    //string sLastActNumber = account.Acts.Last().ActNumber;
                    short nNumber = 1;
                    foreach (Act a in account.Acts)
                    {
                        string[] s = a.ActNumber.Split('-');
                        if (s.Length > 1 && short.TryParse(s[1], out short nMax))
                        {
                            nNumber = (short)Math.Max(nMax + 1, nNumber);
                        }
                    }
                    sNewActNumber = $"{account.AccountNumber} - {nNumber}";
                }
                else
                {
                    sNewActNumber = account.AccountNumber;
                }
            }
            return sNewActNumber;
        }

        public void ComntactorNameComboBoxSelectionChanged(object sender1, object sender2)
        {
            Contractor contractor = sender1 as Contractor;
            Account account = sender2 as Account;
            if (account != null)
            {
                account.Contractor = _contextAccount_.AddSingleToContext(account.Contractor, delegate (Contractor c) { return c.ID == account.ContractorID; }); //добавить св-во навигации Contractor
                contractor.Bank = _contextAccount_.AddSingleToContext(contractor.Bank, delegate (Bank b) { return b.ID == contractor.BankID; }); //добавить св-во навигации Bank
                account.NotifyPropertyChanged("ContractorName");
                if (account.AccountNumber.Count(c => c == '/') > 1) //наименование счета нового формата
                {
                    string[] s = account.AccountNumber.Split("/");
                    account.AccountNumber = $"{s[0]}/{s[1]}/{account.Contractor.AbbrForAcc}";
                }
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
                Quantity = 1
            };
            product.FilesToList();
            GetProductParameters(product);
            GetProductCosts(product);
            CurrentOrder.State = CurrentOrder.OrderState(CurrentOrder.Products);
            return product;
        }

        private void GetProductCosts(Product product)
        {
            List<TypeOfActivityInProduct> typeOfActivityInProduct;
            typeOfActivityInProduct = _contextOrder_.TypeOfActivityInProducts.AsNoTracking()
                .Where(TypeOfActivityInProducts => TypeOfActivityInProducts.ProductTypeID == product.ProductTypeID)
                .Include(TypeOfActivityInProducts => TypeOfActivityInProducts.TypeOfActivity).ToList();                 //получить список кодов видов деятельности (КВД) для изделия 
            product.ProductCosts = _contextOrder_.ProductCosts.Where(ProductCost => ProductCost.ProductID == product.ID).Include(ProductCost => ProductCost.TypeOfActivity).ToList(); //список уже сохраненных данных по стоимости по каждому КВД
            foreach (TypeOfActivityInProduct tainp in typeOfActivityInProduct) //проход по видам деятельности, содержащихся в издеии (его виде)
            {
                if (!product.ProductCosts.Any(c => c.TypeOfActivityID == tainp.TypeOfActivityID)) //КВД еще нет в списке
                {
                    ProductCost pc = new ProductCost
                    {
                        ID = 0,
                        ProductID = product.ID,
                        TypeOfActivityID = tainp.TypeOfActivityID,
                        Cost = 0,
                        Name = tainp.TypeOfActivity.Name,
                        Code = tainp.TypeOfActivity.Code
                    };
                    product.ProductCosts.Add(pc);
                }
            }
        }

        private void TotalProductCostsList()
        {
            TotalProductCosts.Clear();
            List<ProductCost> productCost = (from p in CurrentOrder.Products
                                             from pc in p.ProductCosts
                                             select pc).ToList();
            var grouping = from pc in productCost
                           group pc by pc.Code into grp
                           select new { Code = grp.Key, Name = grp.Select(pc => pc.Name).FirstOrDefault(), Cost = grp.Sum(pc => pc.Cost), Outlay = grp.Sum(pc => pc.Outlay), Margin = grp.Sum(pc => pc.Margin) };
            TotalProductCosts.AddRange(grouping.OrderBy(grp => grp.Code));
            TotalProductCosts.Add(new { Code = "", Name = "Итого по заказу ", Cost = grouping.Sum(grp => grp.Cost), Outlay = grouping.Sum(grp => grp.Outlay), Margin = grouping.Sum(grp => grp.Margin) });
        }

        public void LoadOrderContext(long nOrderID)
        {
            _pathToFilesOfProduct = _contextOrder_.Setting.AsNoTracking().Where(Setting => Setting.SettingParameterName == "PathToFilesOfProduct").FirstOrDefault().SettingParameterValue;
            ClientCollectionView = CollectionViewSource.GetDefaultView(_contextOrder_.Clients
                .AsNoTracking().ToList().OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase));                                        //инициализация коллекции "Клиенты"
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

        private void OrderFormSendToPrint(ListView listView, long designerID = 0)
        {
            List<long> listProductID = new List<long> { };
            foreach (Product product in listView.SelectedItems)
            {
                listProductID.Add(product.ID);
            }
            if (listProductID.Count == 0) //ничего не было выбрано
            {
                foreach (Product product in CurrentOrder.Products) //добавить ВСЕ
                {
                    listProductID.Add(product.ID);
                }
            }
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Получение данных заказа ...", Cursors.Wait);
                    string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                    Order orderToPrint = _reportcontext.Orders.AsNoTracking().Where(Order => Order.ID == CurrentOrder.ID)
                        .Include(Order => Order.Products).ThenInclude(Products => Products.ProductType)
                        .FirstOrDefault();
                    Array array = orderToPrint.Products.ToArray();
                    for (int ind = 0; ind < array.Length; ind++)
                    {
                        if (!listProductID.Contains(((Product)array.GetValue(ind)).ID)) //если изделия нет в писке требующихся для печати, то удалить
                        {
                            _ = orderToPrint.Products.Remove((Product)array.GetValue(ind));
                        }
                    }
                    foreach (Product product in orderToPrint.Products)
                    {
                        GetProductParameters(product);
                        product.FilesToList();
                        foreach (ProductParameters productParameter in product.ProductParameter)
                        {
                            if (productParameter.ReferencebookID > 0) //если параметр должен заполниться из справочника
                            {
                                productParameter.ParameterValue = ReferencebookParameterList.Find(rp => rp.ReferencebookID == productParameter.ReferencebookID && rp.ID == productParameter.ParameterID)?.Value ?? "";
                            }
                        }
                        product.ProductCosts = _reportcontext.ProductCosts.AsNoTracking().Where(ProductCost => ProductCost.ProductID == product.ID).Include(ProductCost => ProductCost.TypeOfActivity).ToList();
                        product.KVDForReport = string.Join(",", product.ProductCosts.Where(c => c.Cost != 0).Select(c => c.Code.Trim()).ToList());
                    }
                    if (File.Exists(Path.Combine(_pathToReportTemplate, "OrderForm.frx")))
                    {
                        Reports.OrderDataSet = new List<Order> { orderToPrint };
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "OrderForm.frx");
                        Reports.ReportMode = "OrderForm";
                        Reports.designerID = designerID;
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show("Не найден файл OrderForm.frx !", "Ошибка формирования бланка заказа", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка получения данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (_reportcontext != null)
                    {
                        _reportcontext.Dispose();
                    }
                    MainWindow.statusBar.ClearStatus();
                }
            }
        }

        private void LoadPaymentContext()
        {
            MainWindow.statusBar.WriteStatus("Загрузка платежей ...", Cursors.Wait);
            try
            {
                _contextPayment_.Payments.Include(pay => pay.Account).Where(pay => pay.OrderID == CurrentOrder.ID).Load();
                ListPayment = _contextPayment_.Payments.Local.ToObservableCollection();
                AccountForPayment = _contextPayment_.Accounts.Where(acc => acc.OrderID == CurrentOrder.ID).AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки платежей", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void LoadAccountContext(bool lShowMessage = true)
        {
            MainWindow.statusBar.WriteStatus("Загрузка счетов ...", Cursors.Wait);
            try
            {
                foreach (EntityEntry entityEntry in _contextAccount_.ChangeTracker.Entries().ToArray()) //для повторной загрузки из БД
                {
                    if (entityEntry.Entity != null)
                    {
                        entityEntry.State = EntityState.Detached;
                    }
                }
                _contextAccount_.Accounts
                    .Include(Account => Account.Acts)
                    .Include(Account => Account.Order)
                    .Include(Account => Account.Contractor).ThenInclude(Contractor => Contractor.Bank).ThenInclude(Bank => Bank.Localities)
                    .Include(Account => Account.Order.Client).ThenInclude(Client => Client.Bank).ThenInclude(Bank => Bank.Localities)
                    .Include(Account => Account.Order.Client).ThenInclude(Client => Client.ConsigneeBank).ThenInclude(ConsigneeBank => ConsigneeBank.Localities)
                    .Include(Account => Account.Order.Products).ThenInclude(Product => Product.ProductType)
                    .Where(Account => Account.OrderID == CurrentOrder.ID)
                    .Load();
                ListPAD = _contextAccount_.Accounts.Local.ToObservableCollection();                     //спиок счетов и связанных сущностей
                foreach (Account account in ListPAD)
                {
                    account.DetailsToList();                //распаковать детали счета
                    foreach (Act act in account.Acts)
                    {
                        act.CreateDetailsList();            //распаковать детали акта
                    }
                }
                ListContractor = _contextAccount_.Contractors.AsNoTracking().ToList();                  //список пордрядчиков
                SelectedAccountIndex = 0;
                if (lShowMessage)
                {
                    _ = MessageBox.Show("   Все счета загружены успешно!   ", "Загрузка счетов");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки счетов", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void LoadTechCardContext(bool lShowMsg = false)
        {
            MainWindow.statusBar.WriteStatus("Загрузка техкарт ...", Cursors.Wait);
            try
            {
                foreach (EntityEntry entityEntry in _contextTechCard_.ChangeTracker.Entries().ToArray()) //для повторной загрузки из БД
                {
                    if (entityEntry.Entity != null)
                    {
                        entityEntry.State = EntityState.Detached;
                    }
                }
                _contextTechCard_.TechCards
                    .Include(TechCard => TechCard.Product)
                    .Include(TechCard => TechCard.Product.ProductType)
                    .Include(TechCard => TechCard.Product.Order)
                    .Include(TechCard => TechCard.WorkInTechCards)
                    .Include(TechCard => TechCard.WorkInTechCards).ThenInclude(WorkInTechCard => WorkInTechCard.TypeOfActivity)
                    .Include(TechCard => TechCard.WorkInTechCards).ThenInclude(WorkInTechCard => WorkInTechCard.OperationInWorks)
                    .ThenInclude(OperationInWork => OperationInWork.Operation).ThenInclude(Operation => Operation.ProductionArea)
                    .Where(TechCard => TechCard.Product.OrderID == CurrentOrder.ID).Load();
                ListTechCard = _contextTechCard_.TechCards.Local.ToObservableCollection();
                OperationCollectionView = CollectionViewSource.GetDefaultView(_contextTechCard_.Operations
                                                                                .Include(Operation => Operation.ParameterInOperations)
                                                                                .Include(Operation => Operation.TypeOfActivityInOperations)
                                                                                .AsNoTracking().ToList());
                OperationCollectionView.Filter = OperationFilter;
                Array array;
                foreach (TechCard techCard in ListTechCard)
                {
                    array = techCard.WorkInTechCards.OrderBy(w => w.ID).ToArray();              //для обеспечения принудительной сортировки
                    techCard.WorkInTechCards.Clear();
                    for (int ind = 0; ind < array.Length; ind++)
                    {
                        techCard.WorkInTechCards.Add((WorkInTechCard)array.GetValue(ind));
                    }
                    foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                    {
                        array = workInTechCard.OperationInWorks.OrderBy(o => o.ID).ToArray();   //для обеспечения принудительной сортировки
                        workInTechCard.OperationInWorks.Clear();
                        for (int ind = 0; ind < array.Length; ind++)
                        {
                            workInTechCard.OperationInWorks.Add((OperationInWork)array.GetValue(ind));
                        }
                        foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                        {
                            GetOperationInWorkParameters(operationInWork);                      //сформировать список параметров
                            operationInWork.FilesToList();                                      //развернуть список файлов
                        }
                    }
                }
                if (lShowMsg)
                {
                    _ = MessageBox.Show("   Все техкарты загружены успешно!   ", "Загрузка техкарт");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки техкарт", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void GetOperationInWorkParameters(OperationInWork operationInWork)  //формирует список параметров для операции
        {
            operationInWork.OperationInWorkParameters = _contextTechCard_.ParameterInOperations
                .Include(ParameterInOperation => ParameterInOperation.Unit)
                .Where(ParameterInOperation => ParameterInOperation.OperationID == operationInWork.OperationID) //отбор параметров только для конкретной операции
                .Select((ParameterInOperation) => new OperationInWorkParameter
                {
                    ID = ParameterInOperation.ID,
                    Name = ParameterInOperation.Name,
                    UnitName = ParameterInOperation.Unit.Name,
                    ReferencebookID = ParameterInOperation.ReferencebookID,
                    IsRefbookOnRequest = ParameterInOperation.IsRefbookOnRequest
                })
                .AsNoTracking().OrderBy(opinwp => opinwp.ID).ToList(); //этот запрос заполняет список OperationInWorkParameters пустым шаблоном 
            operationInWork.ParametersToList(); //заполнить пустой шаблон значениями из строки Parameters
            foreach (OperationInWorkParameter operationInWorkParameter in operationInWork.OperationInWorkParameters) //проходим по параметрам для дальнейшей инициализацц
            {
                if (operationInWorkParameter.IsRefbookOnRequest) //задан параметр "выбор справочника по запросу"
                {
                    long? typeOfActivityID = operationInWork.WorkInTechCard?.TypeOfActivityID ?? 0; //справочники будем брать только для конкретного КВД
                    IEnumerable<ReferencebookApplicability> enumerable = ReferencebookApplicabilityList.Where(refApp => refApp.TypeOfActivityID == typeOfActivityID);
                    operationInWorkParameter.ReferencebookList = ReferencebookList.FindAll(
                        delegate (Referencebook referencebook)
                        {
                            return enumerable.Any(e => e.ReferencebookID == referencebook.ID); //найти в referencebooks все ID (справочники), которые применяются для TypeOfActivity
                        }
                        );
                }
                if (operationInWorkParameter.ReferencebookID > 0) //для параметра установлено брать значение из справочника
                {
                    operationInWorkParameter.ReferencebookParametersList = ReferencebookParameterList
                        .Where(refbookParameters => refbookParameters.ReferencebookID == operationInWorkParameter.ReferencebookID).ToList();
                    operationInWorkParameter.ParameterValue = ReferencebookParameterList.Find(rp => rp.ReferencebookID == operationInWorkParameter.ReferencebookID && rp.ID == operationInWorkParameter.ParameterID)?.Value ?? "";
                }
            }
        }

        private void AddNewObjectInTechCard(TechCardObjectType techCardObjectType, object senderObject)
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Загрузка объектов в техкарту ...", Cursors.Wait);
                if (techCardObjectType == TechCardObjectType.TechCard) //добавить "Техкарта(изделие)"
                { //добавить все новые техкарты (изделия)                        
                    List<long> ListProductID = _contextTechCard_.Products.Where(product => product.OrderID == CurrentOrder.ID && !ListTechCard.Select(tc => tc.ProductID).ToList().Contains(product.ID)).Select(p => p.ID).ToList();
                    foreach (long nProductID in ListProductID)
                    {
                        TechCard techCard = new TechCard { Number = "", ProductID = nProductID };
                        ListTechCard.Add(techCard);
                        _contextTechCard_.AddToContext(techCard);
                        _contextTechCard_.AddReferenceToContext(techCard, "Product");   //загрузить связанное Изделие по навигационному свойству
                    }
                    foreach (TechCard tc in ListTechCard)                               //загружаем связанные свойства навигации
                    {
                        Product pr = tc.Product;
                        _contextTechCard_.AddReferenceToContext(pr, "ProductType");
                        _contextTechCard_.AddReferenceToContext(pr, "Designer");
                        _contextTechCard_.AddReferenceToContext(pr, "Order");
                        Order order = pr.Order;
                        _contextTechCard_.AddReferenceToContext(order, "Client");
                        _contextTechCard_.AddReferenceToContext(order, "Manager");
                        pr.IsHasTechcard = true;                                        //установить признак наличия Техкарты
                        Product product = CurrentOrder.Products.First(product => product.ID == tc.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                        product.IsHasTechcard = true;                                   //установить признак наличия техкарты в ранее загруженный экземпляр Изделия
                    }
                }
                else if (techCardObjectType == TechCardObjectType.Work) //добавить "Работа"
                {
                    TechCard tc = senderObject as TechCard;
                    if (tc is null) //добавление НЕ с уровня "Техкарта"
                    {

                        if (senderObject is WorkInTechCard) //добавление с уровня "Работа"
                        {
                            tc = (TechCard)GetParentTreeViewItem(senderObject, 0);
                        }
                        else if (senderObject is OperationInWork) //добавление с уровня "Операция"
                        {
                            tc = (TechCard)GetParentTreeViewItem(senderObject, 0);
                        }
                        if (tc is null)
                        {
                            return; //по неизвестной причине родитель "Техкарта" не определен. Соответственно ничего не делаем
                        }
                    }
                    WorkInTechCard workInTechCard = new WorkInTechCard
                    {
                        TechCardID = tc.ID,
                        TypeOfActivityID = new CommonList().ListTypeOfActivity.FirstOrDefault().ID,
                        DatePlanCompletion = CurrentOrder.DateCompletion
                    };
                    tc.WorkInTechCards.Add(workInTechCard);
                    _contextTechCard_.WorkInTechCards.Add(workInTechCard);
                    tc.IsExpanded = true;
                    _contextTechCard_.AddReferenceToContext(workInTechCard, "TypeOfActivity");  //загружаем связанное свойство навигации
                    _contextTechCard_.AddReferenceToContext(workInTechCard, "TechCard");        //загружаем связанное свойство навигации
                    workInTechCard.IsSelected = true;                                           //установить добавленную Работу тактивной
                }
                else if (techCardObjectType == TechCardObjectType.Operation && (senderObject is WorkInTechCard || senderObject is OperationInWork)) //добавить "Операция"
                {
                    WorkInTechCard workInTechCard = senderObject as WorkInTechCard;
                    if (workInTechCard is null) //добавление НЕ с уровня "Работа"
                    {
                        if (senderObject is OperationInWork) //добавление с уровня "Операция"
                        {
                            workInTechCard = (WorkInTechCard)GetParentTreeViewItem(senderObject, 1);
                        }
                        if (workInTechCard is null)
                        {
                            return; //по неизвестной причине родитель "Работа" не определен. Соответственно ничего не делаем
                        }
                    }
                    OperationInWork operationInWork = new OperationInWork
                    {
                        WorkInTechCardID = workInTechCard.ID,
                        OperationID = (OperationCollectionView.CurrentItem as Operation).ID,    //выбор в коллекции пользователем по двойному клику
                        Note = "",
                        Parameters = "",
                        Files = ""
                    };
                    workInTechCard.OperationInWorks.Add(operationInWork);
                    _contextTechCard_.AddToContext(operationInWork);
                    workInTechCard.IsExpanded = true;
                    operationInWork.IsSelected = true;
                    _contextTechCard_.AddReferenceToContext(workInTechCard, "TypeOfActivity");  //загружаем связанное свойство навигации
                    _contextTechCard_.AddReferenceToContext(operationInWork, "Operation");      //загружаем связанное свойство навигации
                    _contextTechCard_.AddReferenceToContext(operationInWork, "WorkInTechCard"); //загружаем связанное свойство навигации
                    Operation operation = operationInWork.Operation;
                    _contextTechCard_.AddReferenceToContext(operation, "ProductionArea");       //загружаем связанное свойство навигации
                    GetOperationInWorkParameters(operationInWork);
                    operationInWork.FilesToList();
                    operationInWork.NotifyPropertyChanged("OperationInWorkParameters");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка добавления объекта в техкарту", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private object GetParentTreeViewItem(object item, short nLevel = 1)
        {
            object oObject = null;
            foreach (TechCard techCard in ListTechCard)
            {
                if (techCard.Equals(item))
                {
                    return techCard; //сам себе родитель
                }
                foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                {
                    if (workInTechCard.Equals(item))
                    {
                        return techCard; //родитель с уровня "Техкарта"
                    }
                    foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                    {
                        if (operationInWork.Equals(item))
                        {
                            return nLevel == 0 ? techCard : (object)workInTechCard; 
                        }
                    }
                }
            }
            return oObject;
        }

        public void DateFactCompletionSourceUpdated(object sender)
        {
            if (sender != null)
            {
                try
                {
                    TechCard techCard = (TechCard)GetParentTreeViewItem(sender, 0);             //ищем корневого родителя на уровне TechCard
                    if (techCard != null)
                    {
                        foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)     //проходим по всем работам текущей техкарты
                        {
                            if (!workInTechCard.DateFactCompletion.HasValue)                    //если нашли хоть одну незаполненную дату фактического завершения по работе, то для изделия сбросить дату изготовления
                            {
                                techCard.Product.DateManufacture = null;                        
                                foreach (Product product in CurrentOrder.Products)              //найти изделие в контекте Заказа
                                {
                                    if (product.ID == techCard.Product.ID)
                                    {
                                        product.DateManufacture = null;                         //сбросить дату изготовления
                                        return;
                                    }
                                }
                                return;
                            }
                        }
                        techCard.Product.DateManufacture = (sender as WorkInTechCard).DateFactCompletion; //DateTime.Now; //фиксируем дату изготовления по изделию
                        foreach (Product product in CurrentOrder.Products)                      //найти изделие в контекте Заказа
                        {
                            if (product.ID == techCard.Product.ID)
                            {
                                product.DateManufacture = techCard.Product.DateManufacture;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка простановки даты изготоления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void OperationParameterTextBoxSourceUpdated(object sender)
        {
            if (sender is OperationInWork operationInWork)
            {
                operationInWork.ListToParameters(); //свернуть список значений параметров в строку Parameters
            }
        }

        public void OperationParameterValueComboBoxSelectionChanged(object sender)
        {
            if (sender is OperationInWorkParameter operationInWorkParameter)
            {
                operationInWorkParameter.ParameterValue = ReferencebookParameterList.Find(rp => rp.ReferencebookID == operationInWorkParameter.ReferencebookID && rp.ID == operationInWorkParameter.ParameterID)?.Value ?? "";
            }
        }

        public void OperationParameterComboBoxSelectionChanged(object sender1, object sender2)
        {
            if (sender1 is OperationInWorkParameter operationInWorkParameter && sender2 is Referencebook refBook)
            {
                operationInWorkParameter.ReferencebookID = refBook.ID;
                operationInWorkParameter.ReferencebookParametersList = ReferencebookParameterList.Where(refbookParameters => refbookParameters.ReferencebookID == operationInWorkParameter.ReferencebookID).ToList();
            }
        }

        private void ToProduction(object sender)
        {
            try
            {
                TechCard techCard = (TechCard)GetParentTreeViewItem(sender, 0); //ищем корневого родителя на уровне TechCard
                if (techCard != null)
                {
                    techCard.Product.DateTransferProduction = DateTime.Now;     //фиксируем дату передачи в производство
                    foreach (Product product in CurrentOrder.Products)          //найти изделие в контекте Заказа
                    {
                        if (product.ID == techCard.Product.ID)
                        {
                            product.DateTransferProduction = DateTime.Now;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка передачи в производство", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FromProduction(object sender)
        {
            try
            {
                TechCard techCard = (TechCard)GetParentTreeViewItem(sender, 0); //ищем корневого родителя на уровне TechCard
                if (techCard != null)
                {
                    techCard.Product.DateTransferProduction = null;             //убрать дату передачи в производство
                    foreach (Product product in CurrentOrder.Products)          //найти изделие в контекте Заказа
                    {
                        if (product.ID == techCard.Product.ID)
                        {
                            product.DateTransferProduction = null;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка отзыва с производства", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TechCardPrepeareToPrint(object sender, Button button)
        {
            List<long> prodAreaID = new List<long> { };
            List<TechCard> techCards = new List<TechCard> { }; //список Техкарт для печати
            if (sender != null)
            {
                if (sender is TechCard techCard) //печать с уровня Изделие
                {
                    techCards = ListTechCard.Where(t => t.IsPrinted).ToList(); //отобрать все ТК с отметкой для печати
                    if (techCards.Count == 0) //если по факту ничего не отобралось, тогда берем только текущую ТК
                    {
                        techCard.IsPrinted = true;
                        techCards.Add(techCard);
                    }
                    foreach (TechCard tc in techCards)
                    {
                        foreach (WorkInTechCard workInTechCard in tc.WorkInTechCards) //ВСЕ дочерние элементы отметить На печать
                        {
                            workInTechCard.IsPrinted = true;
                            foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                            {
                                operationInWork.IsPrinted = true;
                            }
                        }
                    }
                    TechCardPrint(ref techCards);
                }
                else if (sender is WorkInTechCard workInTechCard) //печать с уровня Работа
                {
                    TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard, 0); //берем ТК родителя
                    techCards = ListTechCard.Where(t => t.Equals(tc) && tc.WorkInTechCards.Any(w => w.IsPrinted)).ToList(); //отобрать эту ТК, если  есть Работы с отметкой для печати
                    if (techCards.Count == 0) //по факту ничего не отобралось, тогда берем только текущую ТК
                    {
                        tc.IsPrinted = true;
                        workInTechCard.IsPrinted = true;
                        techCards.Add(tc);
                        foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks) //для всех операций текущей работы проставить отметку На печать
                        {
                            operationInWork.IsPrinted = true;
                        }
                    }
                    else
                    {
                        foreach (TechCard t in techCards)
                        {
                            foreach (WorkInTechCard workInTech in t.WorkInTechCards) //для отмеченных работ и операций проставить отметку На печать
                            {
                                if (workInTech.IsPrinted)
                                {
                                    foreach (OperationInWork operationInWork in workInTech.OperationInWorks)
                                    {
                                        operationInWork.IsPrinted = true;
                                    }
                                }
                            }
                        }
                    }
                    TechCardPrint(ref techCards);
                }
                else if (sender is OperationInWork operationInWork) //печать с уровня Операция
                {
                    WorkInTechCard workInTC = (WorkInTechCard)GetParentTreeViewItem(operationInWork, 1);
                    button.ContextMenu.Items.Clear();
                    foreach (OperationInWork operationInW in workInTC.OperationInWorks)
                    {
                        if (operationInW.Operation.ProductionAreaID != null && !prodAreaID.Contains((long)operationInW.Operation.ProductionAreaID)) //добавление без дубляжа
                        {
                            MenuItem menuItem = new MenuItem
                            {
                                Header = operationInW.Operation.ProductionArea.Name,
                                Tag = (long)operationInW.Operation.ProductionAreaID,
                                IsCheckable = true,
                                StaysOpenOnClick = true
                            };
                            _ = button.ContextMenu.Items.Add(menuItem); //добавляем все производственные участки, которые есть в ТК
                            prodAreaID.Add((long)operationInW.Operation.ProductionAreaID);
                        }
                    }
                    _ = button.ContextMenu.Items.Add(new Separator()); //добавляем разделитель
                    MenuItem m = new MenuItem { Header = " На печать ", Tag = (long)0 };
                    m.Click += PrintTechCardMenuItem_Click;
                    _ = button.ContextMenu.Items.Add(m); //добавляем кнопку подтверждения
                    button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    button.ContextMenu.PlacementTarget = button;
                    button.ContextMenu.Visibility = Visibility.Visible;
                    button.ContextMenu.IsOpen = true;

                    void PrintTechCardMenuItem_Click(object sender, RoutedEventArgs e)
                    {
                        try
                        {
                            if (sender is null)
                            {
                                return;
                            }
                            button.ContextMenu.IsOpen = false;
                            button.ContextMenu.Visibility = Visibility.Hidden;
                            prodAreaID.Clear();
                            for (short i = 0; i < button.ContextMenu.Items.Count; i++)
                            {
                                if (button.ContextMenu.Items[i] is MenuItem menuItem)
                                {
                                    long nID = (long)(menuItem.Tag ?? 0);
                                    if (menuItem.IsChecked && nID > 0)
                                    {
                                        prodAreaID.Add(nID); //собираем ID выбранных для печати операций
                                    }
                                }
                            }
                            TechCard tc = (TechCard)GetParentTreeViewItem(operationInWork, 0);
                            WorkInTechCard work = (WorkInTechCard)GetParentTreeViewItem(operationInWork, 1);
                            tc.IsPrinted = true; //берем родителя в печать
                            work.IsPrinted = true; //берем родителя в печать
                            techCards.Add(tc);
                            if (prodAreaID.Count > 0) //есть отобранные на печать производственные участки
                            {
                                foreach (OperationInWork operationInW in workInTC.OperationInWorks)
                                {
                                    operationInW.IsPrinted = prodAreaID.Contains((long)operationInW.Operation.ProductionAreaID); //берем операцию для печати если производственный участок есть в отобранных
                                }
                            }
                            else
                            {
                                operationInWork.IsPrinted = true; //если отобранных участков нет, то берем только текущую операцию
                            }
                            TechCardPrint(ref techCards);
                        }
                        catch (Exception ex)
                        {
                            _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка подготовки техкарты к печати", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void TechCardPrint(ref List<TechCard> techCards)
        {
            if (techCards.Count > 0)
            {
                foreach (TechCard tc in techCards)
                {
                    if (tc.WorkInTechCards.Count == 0) //работы не определены
                    {
                        tc.WorkInTechCards.Add(new WorkInTechCard { Number = "_Fake_", IsPrinted = false }); //добавить фиктивную
                    }
                    foreach (WorkInTechCard workInTechCard in tc.WorkInTechCards)
                    {
                        if (workInTechCard.OperationInWorks.Count == 0) //операции не определены
                        {
                            workInTechCard.OperationInWorks.Add(new OperationInWork { Number = "_Fake_", IsPrinted = false }); //добавить фиктивную
                        }
                    }
                }
                using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
                try
                {
                    MainWindow.statusBar.WriteStatus("Получение данных техкарты ...", Cursors.Wait);
                    string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                    string techCardFileTemplate = "TechCard.frx";
                    if (File.Exists(Path.Combine(_pathToReportTemplate, techCardFileTemplate)))
                    {
                        Reports.TechCardDataSet = techCards;
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, techCardFileTemplate);
                        Reports.ReportMode = "TechCard";
                        Reports.RunReport();
                        foreach (TechCard techCard in techCards) //снять отметки На печать для всех операций и их родителей, добавленных как текущие
                        {
                            techCard.IsPrinted = (techCard.IsChecked || !techCard.IsPrinted) && techCard.IsPrinted; //не отмечана, но имеет признака На печать, тогда признак печати снять
                            foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                            {
                                workInTechCard.IsPrinted = (workInTechCard.IsChecked || !workInTechCard.IsPrinted) && workInTechCard.IsPrinted;

                                foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                                {
                                    operationInWork.IsPrinted = false; //для операций снять отметку На печать без условий
                                }
                                Array arrayList = workInTechCard.OperationInWorks.Where(o => o.Number == "_Fake_").ToArray();
                                for (int i = 0; i < arrayList.Length; i++)
                                {
                                    _ = workInTechCard.OperationInWorks.Remove((OperationInWork)arrayList.GetValue(i)); //удалить Фейковые
                                }
                            }
                            Array array = techCard.WorkInTechCards.Where(o => o.Number == "_Fake_").ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                _ = techCard.WorkInTechCards.Remove((WorkInTechCard)array.GetValue(i)); //удалить Фейковые
                            }
                        }
                    }
                    else
                    {
                        _ = MessageBox.Show($"Не найден файл {techCardFileTemplate} !", "Ошибка формирования ТК", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования ТК", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MainWindow.statusBar.ClearStatus();
                }
            }
            else
            {
                _ = MessageBox.Show(" Нет данных для печати ТК! ", "Печать техкарты", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AccountPrint(Account account, DateTime dateTime, bool withSignature)
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Получение данных счета ...", Cursors.Wait);
                    string AccountFileTemplate = account.Contractor?.AccountFileTemplate ?? "";
                    string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                    if (File.Exists(Path.Combine(_pathToReportTemplate, AccountFileTemplate)))
                    {
                        List<Account> accounts = new List<Account> { account };
                        Reports.AccountDataSet = accounts;
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, AccountFileTemplate);
                        Reports.ReportDate = dateTime;
                        Reports.ReportMode = "AccountForm";
                        Reports.WithSignature = withSignature;
                        Reports.AmountInWords = InWords.Amount(account.DetailsList.Sum(c => c.Cost));
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show($"Не найден файл {AccountFileTemplate} !", "Ошибка формирования счета", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования счета", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MainWindow.statusBar.ClearStatus();
                }
            }
        }

        private void ActPrint(Account account, Act act, DateTime dateTime, bool templateAct, bool templateSFTN, bool templateUPD)
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);
                    string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                    List<Account> accounts = new List<Account> { account };
                    Reports.AccountDataSet = accounts;
                    Reports.ActDataSet = new List<Act> { };
                    if (act != null) //фиксируем акт, который выбран для печати
                    {
                        Reports.ActDataSet = new List<Act> { act };
                    }
                    Reports.AmountInWords = InWords.Amount(Reports.ActDataSet[0].DetailsList.Sum(a => a.Cost));
                    Reports.ReportDate = dateTime;
                    if (templateAct)
                    {
                        if (File.Exists(Path.Combine(_pathToReportTemplate, "Act.frx")))
                        {
                            Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "Act.frx");
                            Reports.ReportMode = "ActForm";
                            Reports.RunReport();
                        }
                        else
                        {
                            _ = MessageBox.Show("Не найден файл Act.frx !", "Ошибка формирования акта", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (templateSFTN)
                    {
                        if (File.Exists(Path.Combine(_pathToReportTemplate, "SF.frx")))
                        {
                            Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "SF.frx");
                            Reports.ReportDateInWords = InWords.Date(dateTime);
                            Reports.ReportMode = "SFForm";
                            Reports.RunReport();
                        }
                        else
                        {
                            _ = MessageBox.Show("Не найден файл SF.frx !", "Ошибка формирования СФ", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        if (File.Exists(Path.Combine(_pathToReportTemplate, "TN.frx")))
                        {
                            Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "TN.frx");
                            Reports.ReportMode = "TNForm";
                            Reports.NumberInWords = InWords.Number(Reports.ActDataSet[0].DetailsList.Count);
                            Reports.CargoReleasePostName = MainWindow.Userdata.PostName;
                            Reports.CargoReleaseName = MainWindow.Userdata.ShortUserName;
                            Reports.ReportDateInWords = InWords.Date(dateTime);
                            Reports.FreeValue = new List<string> { "", "" };
                            MatchCollection matchCollection = Regex.Matches(account.Footing, @"(№|N)?(\s*\w*-?\w*\s*)от(\s*\d{2}.\d{2}.\d{2,4})", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
                            if (matchCollection.Count > 0)
                            {
                                StringBuilder stringBuilder = new StringBuilder(matchCollection[0].Value.ToLower());
                                _ = stringBuilder.Replace("№", ""); //убрать лишнее
                                _ = stringBuilder.Replace("N", "");
                                _ = stringBuilder.Replace(" ", "");
                                string str = stringBuilder.ToString();
                                Reports.FreeValue[0] = str.Substring(0, str.IndexOf("от"));
                                Reports.FreeValue[1] = str[(str.IndexOf("от") + 2)..];
                            }
                            Reports.RunReport();
                        }
                        else
                        {
                            _ = MessageBox.Show("Не найден файл TN.frx !", "Ошибка формирования ТН", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (templateUPD)
                    {
                        if (File.Exists(Path.Combine(_pathToReportTemplate, "UPD.frx")))
                        {
                            Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "UPD.frx");
                            Reports.ReportMode = "UPDForm";
                            Reports.MonthInWords = InWords.Month(dateTime);
                            Reports.CargoReleasePostName = MainWindow.Userdata.PostName;
                            Reports.CargoReleaseName = MainWindow.Userdata.ShortUserName;
                            Reports.ReportDateInWords = InWords.Date(dateTime);
                            Reports.RunReport();
                        }
                        else
                        {
                            _ = MessageBox.Show("Не найден файл UPD.frx !", "Ошибка формирования УПД", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования документа", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MainWindow.statusBar.ClearStatus();
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
            OrderCardProductDeleteAfterSetToProd = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDeleteAfterSetToProd");
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

    public class VisibilitySelector : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter.ToString() == "NoteTechCardStackPanel")
            {
                if (value != null && value is TechCard)
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "WorkInTechCardAttribute")
            {
                if (value != null && value is WorkInTechCard)
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "OperationInWorkAttribute")
            {
                if (value != null && value is OperationInWork)
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "WorkInTechCardTextAttribute")
            {
                if (value != null) //если есть определение операций
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "WorkInTechCardComboBoxAttribute")
            {
                if (value != null && value is WorkInTechCard workInTechCard && workInTechCard.OperationInWorks.Count == 0) //если еще нет определения операций
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class NullDateTimeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime?)value).HasValue ? true : (object)false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsCanEditAfterSendToProdToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            bool SendToProduction = value[0] is bool && (bool)value[0];
            bool HasAccess = value[1] is bool && (bool)value[1];
            if (SendToProduction && HasAccess) //если дата установлена и есть доступ
            {
                return true;
            }
            else if (SendToProduction && !HasAccess) //если дата установлена и НЕТ доступа
            {
                return false;
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsCanDeleteAfterSendToProdToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            bool SendToProduction = value[0] is bool && (bool)value[0];
            bool HasAccess = value[1] is bool && (bool)value[1];
            if (SendToProduction && HasAccess) //если дата установлена и есть доступ
            {
                return true;
            }
            else if (SendToProduction && !HasAccess) //если дата установлена и НЕТ доступа
            {
                return false;
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CommonList
    {
        private List<TypeOfActivity> _listTypeOfActivity = null;
        public List<TypeOfActivity> ListTypeOfActivity      //список видов деятельности
        {
            get => _listTypeOfActivity ?? new List<TypeOfActivity> { };
            set => _listTypeOfActivity = value;
        }

        public CommonList()
        {
            using App.AppDbContext _context_ = CreateDbContext.CreateContext();
            {
                ListTypeOfActivity = _context_.TypeOfActivitys.AsNoTracking().ToList();
            }
        }
    }

}


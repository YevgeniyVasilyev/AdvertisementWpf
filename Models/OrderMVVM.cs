﻿using Microsoft.EntityFrameworkCore;
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

    internal class OrderViewModel : INotifyPropertyChanged
    {
        private RelayCommand saveRequest, textBlockMouseLeftClick, showHideFrameworkElement, selectNewProduct, deleteProduct, newFileToProduct, deleteFileFromProduct, 
                             openFolderWithFileProduct, openFileProductInShell, printOrderForm, printOrderFormForDesigner, newPayment, savePayment, loadAccount, saveAccount,  newAccount, newAct;
        private App.AppDbContext _contextOrder_ = CreateDbContext.CreateContext();              //контекст для заказа
        private App.AppDbContext _contextPayment_ = CreateDbContext.CreateContext();            //контекст для платежей Заказа
        private App.AppDbContext _contextAccount_ = CreateDbContext.CreateContext();            //контекст для ПУД Заказа
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

        public OrderViewModel(long nOrderID = 0, bool lViewMode = false)
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Загрузка данных по заказу ...", Cursors.Wait);
                ViewMode = lViewMode;
                LoadOrderContext(nOrderID);  //загрузить данные в контекст
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
                    _contextOrder_.AddToContext(CurrentOrder);
                }
                foreach (Product product in CurrentOrder.Products)                                                                                  //инициализация изделий
                {
                    GetProductParameters(product);                                                                                                  //развернуть параметры изделия
                    product.FilesToList();                                                                                                          //развернуть список файлов изделия
                    GetProductCosts(product);                                                                                                       //сформировать список стоимостей изделия
                }
                TotalProductCostsList();                                                                                                            //список на вкладке "Стоимость и затраты по КВД"
                LoadPaymentContext();                                                                                                               //загрузить платежи на вкладке "Платежи"
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
            if (_contextPayment_ != null)
            {
                _contextPayment_.Dispose();
                _contextPayment_ = null;
            }
            if (_contextAccount_ != null)
            {
                _contextAccount_.Dispose();
                _contextAccount_ = null;
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
        }, (o) => _contextPayment_ != null && CurrentOrder?.ID > 0);

        public RelayCommand SavePayment => savePayment ??= new RelayCommand((o) => //команда сохранения платежей
        {
            _contextPayment_.SaveContext();
            MainWindow.statusBar.ClearStatus();
        }, (o) => _contextPayment_ != null && CurrentOrder?.ID > 0 && ErrorsCount == 0);

        public RelayCommand LoadAccount => loadAccount ??= new RelayCommand((o) => //команда загрузки ПУД
        {
            LoadAccountContext();
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0);

        public RelayCommand SaveAccount => saveAccount ??= new RelayCommand((o) => //команда сохранения ПУД
        {
            _contextAccount_.SaveContext();
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0);

        public RelayCommand NewAccount => newAccount ??= new RelayCommand((o) => //команда создания нового Счета
        {
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0);

        public RelayCommand NewAct => newAct ??= new RelayCommand((o) => //команда создания нового Акта
        {
        }, (o) => _contextAccount_ != null && CurrentOrder?.ID > 0);
        
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
            bool CanSaveOrder = _contextOrder_ != null && ErrorsCount == 0 && !CurrentOrder.Products.Any(p => p.HasError);
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

        public void DeleteAccount(object sender)
        {
            if (sender is Account account)
            {
                _ = ListPAD.Remove(account);
                _contextAccount_.DeleteFromContext(account);
            }
        }

        public void DeleteAccountDetail(object sender1, object sender2)
        {
            Account account = sender1 as Account;
            if (sender2 is AccountDetail accountDetail)
            {
                _ = account.DetailsList.Remove(accountDetail);
                account.ListToDetails();
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

        public void DeleteAct(object sender1, object sender2)
        {
            Account account = sender1 as Account;
            Act act = sender2 as Act;
            _ = account.Acts.Remove(act);
            _contextAccount_.DeleteFromContext(act);
        }

        public void DeleteActDetail(object sender1, object sender2, object sender3)
        {
            try
            {
                Account account = sender1 as Account;
                Act act = sender2 as Act;
                AccountDetail accountDetail = sender3 as AccountDetail;
                _ = act.ListProductInAct.Remove(accountDetail.ProductID);
                act.ListToProductInAct();
                act.CreateDetailsList(account); //лучше использовать account, т.к. Account для Act может быть пустым для нового и не сохраненного счета
                foreach (Act a in account.Acts)
                {
                    a.ActNumber = GetNewActNumber(a, account); //перенумерация актов
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки счетов", MessageBoxButton.OK, MessageBoxImage.Error);
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
            account.Contractor = _contextAccount_.AddSingleToContext(account.Contractor, delegate (Contractor c) { return c.ID == account.ContractorID; }); //добавить св-во навигации Contractor
            contractor.Bank = _contextAccount_.AddSingleToContext(contractor.Bank, delegate (Bank b) { return b.ID == contractor.BankID; }); //добавить св-во навигации Bank
            account.NotifyPropertyChanged("ContractorName");
            if (account.AccountNumber.Count(c => c == '/') > 1) //наименование счета нового формата
            {
                string[] s = account.AccountNumber.Split("/");
                account.AccountNumber = $"{s[0]}/{s[1]}/{account.Contractor.AbbrForAcc}";
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


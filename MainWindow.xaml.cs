using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using AdvertisementWpf.Models;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Reflection;
using System.Windows.Documents;
using System.Collections;
using System.Threading.Tasks;

namespace AdvertisementWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// Scaffold-DbContext -Connection "Data Source=YEVGENIY; Database=AdvertisementNF; Integrated Security=True;" -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -UseDatabaseNames

    public partial class MainWindow : Window
    {
        private IConfigurationBuilder ConfigurationBuilder;
        private IConfiguration Configuration;
        public static ConnectionData Connectiondata;
        public static User Userdata;
        public static StatusBar statusBar;
        public static OrderLegendColors orderLegendColors;
        public static List<IAccessMatrix> userIAccessMatrix;
        private CollectionViewSource ordersViewSource, productsViewSource, workInTechCardViewSource;
        private App.AppDbContext _context;
        public static string WhereCondition = "", WhereStateCondition = "";
        public static List<string> WhereProductTypeCondition = new List<string> { }, WhereProductClientCondition = new List<string> { },
                        WhereProductManagerCondition = new List<string> { }, WhereProductDesignerCondition = new List<string> { }, WhereProductWorkerCondition = new List<string> { };
        public static List<bool> WherePaymentIndicationCondition = new List<bool> { };
        public static List<long> WhereTypeOfActivityCondition = new List<long> { };
        public static DateTime? dStartDate, dEndDate;
        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;
        protected enum SortDirection { None, Ascending, Descending } //внутреннее определение направления сортировки
        private List<ListViewSort> ListViewSorts = new List<ListViewSort> { };

        public MainWindow()
        {
            Connectiondata = new ConnectionData();

            InitializeComponent();

            statusBar = new StatusBar();
            Status.DataContext = statusBar;
            UsrInfo.DataContext = statusBar;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            statusBar.WriteStatus("Инициализация параметров ...", null);

            if (!File.Exists("appsetting.json"))
            {
                _ = MessageBox.Show("Не найден файл клнфигурации приложения!" + "\n" + "Продолжение работы невозможно!",
                    "Ошибка инициализации приложения", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            ConfigurationBuilder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);
            Configuration = ConfigurationBuilder.Build();

            IConfigurationSection section;
            section = Configuration.GetSection("DataBase");
            section.Bind(Connectiondata);
            Connectiondata.Is_verify_connection = false;
            statusBar.ClearStatus();
            orderLegendColors = new OrderLegendColors();
            //Connectiondata.Connectionstring = "Data Source = YEVGENIY; Database = AdvertisementNF; Integrated Security = True;"; // УБРАТЬ!!!!
            Connectiondata.Connectionstring = "Data Source = ; Database = ; Integrated Security = True";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = MessageBox.Show("Вы хотите завершить работу ?", "Завершение работы", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            statusBar.ClearStatus();
        }

        private void Connect_Item_Click(object sender, RoutedEventArgs e)
        {
            Connectiondata.Is_verify_connection = false;
            OrderListView.Visibility = Visibility.Collapsed;
            ProductListView.Visibility = Visibility.Collapsed;
            ProductionProductListView.Visibility = Visibility.Collapsed;
            OrderList.Height = new GridLength(1, GridUnitType.Star);
            ProductList.Height = new GridLength(1, GridUnitType.Auto);
            ProductionProductList.Height = new GridLength(1, GridUnitType.Auto);
            TotalOrder.Height = new GridLength(1, GridUnitType.Auto);
            LogonWindow logon = new LogonWindow
            {
                Owner = this
            };
            if ((bool)logon.ShowDialog())
            {
                if (Connectiondata.Is_verify_connection)
                {
                    statusBar.WriteStatus("Инициализация параметров таблиц ...", null);
                    SetSetting();
                    SetListViewSetting();
                    statusBar.ClearStatus();
                }
            }
        }

        private void SetSetting()
        {
            statusBar.WriteStatus("Формирование матрицы доступа ...", Cursors.Wait);
            _context = new App.AppDbContext(Connectiondata.Connectionstring);
            try
            {
                userIAccessMatrix = _context.IAccessMatrix.ToList();
                foreach (IAccessMatrix accessMatrix in userIAccessMatrix)
                {
                    accessMatrix.GrantToList(); //преобразовать строку ID ролей в список
                }
                List<Setting> settings = _context.Setting.ToList();
                orderLegendColors = GetOrderLegendColorsSetting.OrderLegendColors(settings);
                if (!Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    List<byte> nMonth = new List<byte> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                    DateTime dStartDate, dEndDate;
                    dStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    if (DateTime.Today.Month == 2 && DateTime.IsLeapYear(DateTime.Today.Year)) //февраль в високосном году
                    {
                        dEndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, nMonth[DateTime.Today.Month - 1] + 1);
                    }
                    else
                    {
                        dEndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, nMonth[DateTime.Today.Month - 1]);
                    }
                    WhereCondition = $"WHERE DateAdmission >= '{dStartDate.Date}' AND DateAdmission <= '{dEndDate.Date}'";
                    ShowOrders(); //покажем ему сразу его заказы, принятые в текущем месяце
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования матрицы доступа", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_context != null)
                {
                    _context.Dispose();
                }
                _context = null;
                statusBar.ClearStatus();
            }
        }

        private void SetListViewSetting()
        {
            try
            {
                RootAppConfigObject rootObject;
                rootObject = new RootAppConfigObject();
                rootObject = AppConfig.DeSerialize();
                int nFindIndex;
                if (rootObject.LstView != null) //есть раздел для LstView
                {
                    foreach (Listview listview in rootObject.LstView)
                    {
                        object lstViewObject = FindName(listview.ListViewName);
                        if (lstViewObject is ListView listView) //если есть FrameworkElemen ListView с заданным именем
                        {
                            ushort nColumnIndex = 0;
                            GridView gridView = (GridView)listView.View; //текущее представление
                            foreach (ListviewColumn listviewColumn in listview.ListViewColumns) //проходим по описаниям параметров столбцов для установки
                            {
                                if (nColumnIndex < gridView.Columns.Count) //для контроля количества сохраненных описаний с количеством реальных столбцов
                                {
                                    if (!gridView.Columns[nColumnIndex].Header.ToString().Equals(listviewColumn.ColumnHeader)) //хэш очередного столбца отличается от хэша в описании. Значит либо новый, либо был перемещен
                                    {
                                        if (gridView.Columns.FirstOrDefault(c => c.Header.ToString().Equals(listviewColumn.ColumnHeader)) is GridViewColumn gridViewColumn) //найти в фактическом описании нужный столбец
                                        {
                                            nFindIndex = gridView.Columns.IndexOf(gridViewColumn); //найти индекс этого столбца
                                            if (nFindIndex >= 0)
                                            {
                                                gridView.Columns.Move(nFindIndex, nColumnIndex); //переместить в нужную позицию
                                            }
                                        }
                                    }
                                    gridView.Columns[nColumnIndex].Width = listviewColumn.ColumnWidth; //устанавливаем ширину
                                    nColumnIndex++;
                                }
                                else //если сохраненных описаний оказалось вдруг больше, то остальные не рассматриваем
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка установки параметров столбцов таблицы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
            }
        }

        private void SaveListViewSetting(ListView listView)
        {
            try
            {
                RootAppConfigObject rootObject;
                rootObject = new RootAppConfigObject();
                rootObject = AppConfig.DeSerialize();
                GridView gridView = (GridView)listView.View; //текущее представление
                Listview[] view = rootObject.LstView?.ToArray(); //получить ветку с описаниями параметров всех ListView
                Listview lstview;
                if (view is null) //ветки еще нет
                {
                    view = new Listview[] { new Listview { ListViewName = listView.Name, ListViewColumns = Array.Empty<ListviewColumn>() } };
                }
                lstview = view.FirstOrDefault(l => l.ListViewName.ToString().Equals(listView.Name)); //получить описание конкретного ListView
                if (lstview is null) //описания для данного Listview еще нет
                {
                    lstview = new Listview { ListViewName = listView.Name, ListViewColumns = Array.Empty<ListviewColumn>() }; //установить ветку для заданного в параметре listView в пустое значение
                    Array.Resize(ref view, view.Length + 1);
                    view[^1] = lstview; //добавить ветку в раздел описаний ListView
                }
                lstview.ListViewColumns = new ListviewColumn[gridView.Columns.Count]; //очистить все описания столбцов
                ushort nColumnIndex = 0;
                foreach (GridViewColumn gridViewColumn in gridView.Columns) //проходим по столбцам и фиксируем параметры столбцов
                {
                    lstview.ListViewColumns[nColumnIndex++] = new ListviewColumn { ColumnWidth = gridViewColumn.ActualWidth, ColumnHeader = gridViewColumn.Header.ToString() };
                }
                rootObject.LstView = view;
                AppConfig.Serialize(rootObject);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка сохранения параметров столбцов таблицы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BankLocalityUnit_Item_Click(object sender, RoutedEventArgs e)
        {
            SmallHandBookWindow hb = new SmallHandBookWindow
            {
                Owner = this
            };
            _ = hb.ShowDialog();
        }

        public class ConnectionData : INotifyPropertyChanged
        {
            private string servername;
            private List<string> basenames;
            private List<string> basealiases;
            private string username;
            private string basename;
            private string connectionstring;
            private bool is_verify_connection;

            public string Servername
            { 
                get => servername; 
                set => servername = value; 
            }
            public List<string> Basenames
            { 
                get => basenames; 
                set
                {
                    basenames = value;
                    NotifyPropertyChanged("Basenames");
                }
            }
            public List<string> Basealiases
            { 
                get => basealiases; 
                set
                {
                    basealiases = value;
                    NotifyPropertyChanged("Basealiases");
                }
            }
            public string Username 
            {
                get => username; 
                set => username = value; 
            }
            public string Basename
            {
                get => basename;
                set => basename = value;
            }
            public string Connectionstring
            {
                get => connectionstring;
                set => connectionstring = value;
            }
            public bool Is_verify_connection
            {
                get => is_verify_connection;
                set
                {
                    is_verify_connection = value;
                    NotifyPropertyChanged("Is_verify_connection");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ClientsAndContractors_Item_Click(object sender, RoutedEventArgs e)
        {
            ClientsAndContractorsHandbook cc = new ClientsAndContractorsHandbook
            {
                Owner = this
            };
            _ = cc.ShowDialog();
        }

        public class StatusBar : INotifyPropertyChanged
        {
            private string _statusMessage;
            private string _userInfo;
            public string StatusMessage
            {
                get => _statusMessage;
                set
                {
                    if (_statusMessage != value)
                    {
                        _statusMessage = value;
                    }
                    NotifyPropertyChanged("StatusMessage");
                }
            }
            public string UserInfo
            {
                get => _userInfo;
                set
                {
                    if (_userInfo != value)
                    {
                        _userInfo = value;
                    }
                    NotifyPropertyChanged("UserInfo");
                }
            }

            public StatusBar()
            { }
            public void WriteStatus(string msg = "", Cursor cursor = null)
            {
                StatusMessage = msg;
                Mouse.OverrideCursor = cursor;
            }

            public void ClearStatus()
            {
                StatusMessage = "Готов";
                Mouse.OverrideCursor = null;
            }

            public void WriteUserInfo()
            {
                UserInfo = "Пользователь: " + Userdata.FirstName + " " + Userdata.LastName + " " + Userdata.MiddleName;
            }

            public void ActivateProgressBar(int nMin = 0, int nMax = 100, int nValue = 0, bool IsIndeterminate = false, string sTextNear = "")
            {
                MainWindow mainWnd = (MainWindow)Application.Current.MainWindow;
                mainWnd.Bar.Visibility = Visibility.Visible;
                mainWnd.Bar.Minimum = nMin;
                mainWnd.Bar.Maximum = nMax;
                if (nValue > 0)
                {
                    mainWnd.Bar.Value = nValue;
                }
                mainWnd.Bar.IsIndeterminate = IsIndeterminate;
                mainWnd.TextNearBar.Visibility = !string.IsNullOrWhiteSpace(sTextNear) ? Visibility.Visible : Visibility.Collapsed;
                mainWnd.TextNearBar.Text = sTextNear;
            }

            public void DeActivateProgressBar()
            {
                MainWindow mainWnd = (MainWindow)Application.Current.MainWindow;
                mainWnd.Bar.Visibility = Visibility.Collapsed;
                mainWnd.TextNearBar.Visibility = Visibility.Collapsed;
                mainWnd.TextNearBar.Text = "";
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Order_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn)
            {
                if (btn == NewOrderButton || btn == NewOrderButton1)
                {
                    RequestWindow request = new RequestWindow() { };
                    request.ShowDialog();
#if NEWORDER
#else
                    //OrderWindow order = new OrderWindow(NewOrder: true) { };
                    //_ = order.ShowDialog();
#endif
                    _ = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(RefreshOrderButton, new object[0]);
                }
                else if (btn == ViewOrderButton || btn == EditOrderButton)
                {
                    long nOrderID = 0;
                    if (ordersViewSource != null && ordersViewSource.View != null && ordersViewSource.View.CurrentItem is Order nOrder && OrderListView.IsVisible)
                    {
                        nOrderID = nOrder.ID;
                    }
                    else if (productsViewSource != null && productsViewSource.View != null && productsViewSource.View.CurrentItem is Product product && ProductListView.IsVisible)
                    {
                        nOrderID = product.OrderID;
                    }
                    else if (workInTechCardViewSource != null && workInTechCardViewSource.View != null && workInTechCardViewSource.View.CurrentItem is WorkInTechCard workInTechCard && ProductionProductListView.IsVisible)
                    {
                        nOrderID = workInTechCard.TechCard.Product.OrderID;
                    }
                    RequestWindow request = new RequestWindow(ViewMode: btn == ViewOrderButton, nOrderID: nOrderID) { };
                    request.ShowDialog();
#if NEWORDER
#else
                    //OrderWindow order = new OrderWindow(NewOrder: false, EditMode: btn == EditOrderButton, nOrderID: nOrderID) { };
                    //_ = order.ShowDialog();
#endif
                    _ = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(RefreshOrderButton, new object[0]);
                }
                //else if (btn == AllOrdersButton)
                //{
                //    //очистить условие отбора
                //    WhereCondition = WhereStateCondition = "";
                //    ShowOrders();
                //}
                else if (btn == FilterOrdersButton)
                {
                    FilterOrders();
                }
                else if (btn == FilterProductsButton)
                {
                    FilterProducts();
                }
                else if (btn == FilterProductionProductsButton)
                {
                    FilterProductionProducts();
                }
                else if (btn == RefreshOrderButton)
                {
                    RefreshOrderProduct();
                }
            }
        }

        private void Order_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Connectiondata != null && Connectiondata.Is_verify_connection)
            {
                if (e.OriginalSource is Button btn)
                {
                    //if (btn == AllOrdersButton || (btn == EditOrderButton && OrderListView.IsVisible && ordersViewSource?.View != null && ordersViewSource.View.CurrentItem is Order))
                    //{
                    //    e.CanExecute = true;
                    //}
                    if ((btn == NewOrderButton || btn == NewOrderButton1) && Userdata.ID > 0 && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderNew"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == EditOrderButton && OrderListView.IsVisible && ordersViewSource?.View != null && ordersViewSource.View.CurrentItem is Order && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderEdit"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == EditOrderButton && ProductListView.IsVisible && productsViewSource?.View != null && productsViewSource.View.CurrentItem is Product && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderEdit"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == EditOrderButton && ProductionProductListView.IsVisible && workInTechCardViewSource?.View != null && workInTechCardViewSource.View.CurrentItem is WorkInTechCard && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderEdit"))
                    {
                        e.CanExecute = true;
                    }                    
                    else if (btn == ViewOrderButton && OrderListView.IsVisible && ordersViewSource != null && ordersViewSource.View != null && ordersViewSource.View.CurrentItem is Order && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderView"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == ViewOrderButton && ProductListView.IsVisible && productsViewSource?.View.CurrentItem is Product && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderView"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == ViewOrderButton && ProductionProductListView.IsVisible && workInTechCardViewSource?.View != null && workInTechCardViewSource.View.CurrentItem is WorkInTechCard && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderView"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == FilterOrdersButton && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderSelect"))
                    {
                        e.CanExecute = true;
                    }
                    else if ((btn == FilterProductsButton || btn == FilterProductionProductsButton) && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowProductSelect"))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == RefreshOrderButton && (OrderListView.IsVisible || ProductListView.IsVisible || ProductionProductListView.IsVisible))
                    {
                        e.CanExecute = true;
                    }
                }
            }
        }

        public static void RefreshOrderProduct()
        {
            MainWindow mainWnd = (MainWindow)Application.Current.MainWindow;
            if (mainWnd.OrderListView.IsVisible)
            {
                int nCurrentIndex = mainWnd.ordersViewSource.View.CurrentPosition >= 0 ? mainWnd.ordersViewSource.View.CurrentPosition : 0;
                mainWnd.ShowOrders();
                _ = mainWnd.ordersViewSource.View.MoveCurrentToPosition(nCurrentIndex);
            }
            else if (mainWnd.ProductListView.IsVisible)
            {
                int nCurrentIndex = mainWnd.productsViewSource.View.CurrentPosition >= 0 ? mainWnd.productsViewSource.View.CurrentPosition : 0;
                mainWnd.ShowProducts();
                _ = mainWnd.productsViewSource.View.MoveCurrentToPosition(nCurrentIndex);
            }
            else if (mainWnd.ProductionProductListView.IsVisible)
            {
                int nCurrentIndex = mainWnd.workInTechCardViewSource.View.CurrentPosition >= 0 ? mainWnd.workInTechCardViewSource.View.CurrentPosition : 0;
                mainWnd.ShowProductionProducts();
                _ = mainWnd.workInTechCardViewSource.View.MoveCurrentToPosition(nCurrentIndex);
            }
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn)
            {
                if (btn == DeleteOrderButton && ordersViewSource.View.CurrentItem is Order order)
                {
                    if (MessageBox.Show($"Данная операция является необратимой! \n Вы уверены что хотите удалить заказ № {order.Number}", "Удаление заказа", 
                        MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        statusBar.WriteStatus("Удаление заказа ...", Cursors.Wait);
                        _context = new App.AppDbContext(Connectiondata.Connectionstring);
                        try
                        {
                            _context.Orders.Remove(order);
                            _context.SaveChanges();
                            RefreshOrderProduct();
                        }
                        catch (Exception ex)
                        {
                            _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка удаления данных", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            statusBar.ClearStatus();
                            if (_context != null)
                            {
                                _context.Dispose();
                                _context = null;
                            }
                        }
                    }
                }
            }
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Connectiondata != null && Connectiondata.Is_verify_connection && ordersViewSource != null && ordersViewSource.View != null && OrderListView.Visibility == Visibility.Visible)
            {
                if (e.OriginalSource is Button btn)
                {
                    if (btn == DeleteOrderButton && ordersViewSource.View.CurrentItem is Order && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "MainWindowOrderDelete"))
                    {
                        e.CanExecute = true;
                    }
                }
            }
        }

        private void ProductDesigner_Item_Click(object sender, RoutedEventArgs e)
        {
            ProductConstructorWindow productDesigner = new ProductConstructorWindow();
            _ = productDesigner.ShowDialog();
        }

        private void OperationDesigner_Item_Click(object sender, RoutedEventArgs e)
        {
            OperationConstructorWindow operationConstructorWindow = new OperationConstructorWindow();
            _ = operationConstructorWindow.ShowDialog();
        }

        private void ReferencebookDesigner_Item_Click(object sender, RoutedEventArgs e)
        {
            ReferencebookWindow referencebook = new ReferencebookWindow();
            _ = referencebook.ShowDialog();
        }

        private void ProductionAreas_Item_Click(object sender, RoutedEventArgs e)
        {
            ProductionAreaWindow productionAreaWindow = new ProductionAreaWindow();
            _ = productionAreaWindow.ShowDialog();
        }

        private void OrderListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _ = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(EditOrderButton, new object[0]);
        }

        private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn)
            {
                if (btn == PrintOrderListButton)
                {
                    if (OrderListView.IsVisible)
                    {
                        PrintControl.OrderListView(ref OrderListView);
                    }
                    if (ProductListView.IsVisible)
                    {
                        PrintControl.ProductListView(ref ProductListView);
                    }
                    if (ProductionProductListView.IsVisible)
                    {
                        PrintControl.ProductionProductListView(ref ProductionProductListView);
                    }
                }
            }
        }

        private void Print_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Connectiondata != null && Connectiondata.Is_verify_connection)
            {
                if (e.OriginalSource is Button btn)
                {
                    if (btn == PrintOrderListButton && (OrderListView.IsVisible || ProductListView.IsVisible || ProductionProductListView.IsVisible))
                    {
                        e.CanExecute = true;
                    }
                }
            }
        }

        private void ShowOrders()
        {
            statusBar.WriteStatus("Получение данных ...", Cursors.Wait);
            using App.AppDbContext _context = new App.AppDbContext(Connectiondata.Connectionstring);
            try
            {
                List<Order> OrderList;
                ordersViewSource = (CollectionViewSource)FindResource(nameof(ordersViewSource));
                OrderList = _context.Orders.FromSqlRaw($"SELECT * FROM Orders {WhereCondition}").AsNoTracking()
                    .Include(Order => Order.Products).ThenInclude(Products => Products.ProductType)
                    .Include(Order => Order.Manager)
                    .Include(Order => Order.OrderEntered)
                    .Include(Order => Order.Client)
                    .Include(Order => Order.Payments)
                    .Include(Order => Order.Accounts)
                    .ToList();
                if (!Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    OrderList = OrderList.Where(w => w.ManagerID == Userdata.ID).ToList(); //значит текущему пользователю предоставить только его заказы
                }
                if (WhereStateCondition.Length > 0) //указан отбор по состоянию заказа 
                {
                    OrderList = OrderList.Where(Order => WhereStateCondition.IndexOf(Order.State) >= 0).ToList();
                }
                if (WherePaymentIndicationCondition.Any(wp => wp.Equals(true))) //указан отбор по оплате
                {
                    OrderList = OrderList.Where(Order => (WherePaymentIndicationCondition[0] && OrderPaid.NotPaid(ref Order)) ||
                                                         (WherePaymentIndicationCondition[1] && OrderPaid.PartiallyPaid(ref Order)) ||
                                                         (WherePaymentIndicationCondition[2] && OrderPaid.OverPaid(ref Order))).ToList();
                }
                ordersViewSource.Source = OrderList;
                ProductListView.Visibility = Visibility.Collapsed;
                ProductionProductListView.Visibility = Visibility.Collapsed;
                OrderListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                statusBar.ClearStatus();
            }
        }

        private void ShowProducts()
        {
            statusBar.WriteStatus("Получение данных ...", Cursors.Wait);
            using App.AppDbContext _context = new App.AppDbContext(Connectiondata.Connectionstring);
            try
            {
                List<Product> ProductList;
                productsViewSource = (CollectionViewSource)FindResource(nameof(productsViewSource));
                ProductList = _context.Products.FromSqlRaw($"SELECT * FROM Products {WhereCondition}").AsNoTracking()
                    .Include(Products => Products.ProductType).ThenInclude(ProductType => ProductType.CategoryOfProduct)
                    .Include(Products => Products.Order).ThenInclude(Order => Order.Payments)
                    .Include(Products => Products.Designer)
                    .ToList();
                if (!Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    ProductList = ProductList.Where(p => p.Order.ManagerID == Userdata.ID).ToList(); //значит текущему пользователю предоставить только его заказы
                }
                //дата приема заказа
                if (dStartDate.HasValue && dEndDate.HasValue)
                {
                    ProductList = ProductList.Where(Product => Product.Order.DateAdmission >= dStartDate && Product.Order.DateAdmission <= dEndDate).ToList();
                }
                //изделия в категории
                if (WhereProductTypeCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductTypeCondition.Contains(Product.ProductType.ID.ToString())).ToList();
                }
                //клиент
                if (WhereProductClientCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductClientCondition.Contains(Product.Order.ClientID.ToString())).ToList();
                }
                //менеджер/дизайнер/прочие
                //менеджер
                if (WhereProductManagerCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductManagerCondition.Contains(Product.Order.ManagerID.ToString())).ToList();
                }
                //дизайнер
                if (WhereProductDesignerCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductDesignerCondition.Contains(Product.DesignerID.ToString())).ToList();
                }
                //прочие
                if (WhereProductWorkerCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductWorkerCondition.Contains(Product.Order.OrderEnteredID.ToString())).ToList();
                }
                //состояние
                if (WhereStateCondition.Length > 0)
                {
                    ProductList = ProductList.Where(Product => WhereStateCondition.IndexOf(Product.State) >= 0).ToList();
                }
                productsViewSource.Source = ProductList.OrderBy(Products => Products.Order.Number);
                OrderListView.Visibility = Visibility.Collapsed;
                ProductionProductListView.Visibility = Visibility.Collapsed;
                ProductListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_context != null)
                {
                    _context.Dispose();
                }
                statusBar.ClearStatus();
            }
        }

        private void ShowProductionProducts()
        {
            statusBar.WriteStatus("Получение данных ...", Cursors.Wait);
            using App.AppDbContext _context = new App.AppDbContext(Connectiondata.Connectionstring);
            try
            {
                List<WorkInTechCard> workInTechCards;
                //.FromSqlRaw($"SELECT * FROM WorkInTechCards {WhereCondition}")
                workInTechCards = _context.WorkInTechCards.AsNoTracking()
                    .Include(WorkInTechCard => WorkInTechCard.TechCard)
                    .Include(WorkInTechCard => WorkInTechCard.TypeOfActivity)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product.ProductType)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product.Order)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product.Order.Client)
                    .Where(w => !w.DateFactCompletion.HasValue)
                    .ToList();
                workInTechCardViewSource = (CollectionViewSource)FindResource(nameof(workInTechCardViewSource));
                if (!Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    workInTechCards = workInTechCards.Where(w => w.TechCard.Product.Order.ManagerID == Userdata.ID).ToList(); //значит текущему пользователю предоставить только его заказы
                }
                //дата приема заказа
                //if (dStartDate.HasValue && dEndDate.HasValue)
                //{
                //    workInTechCards = workInTechCards.Where(workInTechCards => workInTechCards.TechCard.Product.Order.DateAdmission >= dStartDate && workInTechCards.TechCard.Product.Order.DateAdmission <= dEndDate).ToList();
                //}
                //вид деятельности
                if (WhereTypeOfActivityCondition.Count > 0)
                {
                    workInTechCards = workInTechCards.Where(WorkInTechCard => WhereTypeOfActivityCondition.Contains(WorkInTechCard.TypeOfActivity.ID)).ToList();
                }
                //if (WhereStateCondition.Length > 0) //указан отбор по состоянию изделия
                //{
                //    workInTechCards = workInTechCards.Where(workInTechCards => WhereStateCondition.IndexOf(workInTechCards.TechCard.Product.State) >= 0).ToList();
                //}
                workInTechCardViewSource.Source = workInTechCards.Where(w => w.TechCard.Product.State == OrderProductStates.GetProductState(3)).OrderBy(w => w.TechCard.Product.Order.Number);
                // || w.TechCard.Product.State == OrderProductStates.GetProductState(4))
                OrderListView.Visibility = Visibility.Collapsed;
                ProductListView.Visibility = Visibility.Collapsed;
                ProductionProductListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                statusBar.ClearStatus();
            }
        }

        private void ProductListView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ProductList.Height = (bool)e.NewValue ? new GridLength(1, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            OrderList.Height = new GridLength(1, GridUnitType.Auto);
            ProductionProductList.Height = new GridLength(1, GridUnitType.Auto);
            TotalOrder.Height = new GridLength(1, GridUnitType.Auto);
            TotalProduct.Height = new GridLength(1, GridUnitType.Auto);
            TotalProductionProduct.Height = new GridLength(1, GridUnitType.Auto);
            if (!(bool)e.NewValue) //скрытие
            {
                SaveListViewSetting(sender as ListView);
            }
        }

        private void OrderListView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OrderList.Height = (bool)e.NewValue ? new GridLength(1, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            ProductList.Height = new GridLength(1, GridUnitType.Auto);
            ProductionProductList.Height = new GridLength(1, GridUnitType.Auto);
            TotalOrder.Height = new GridLength(1, GridUnitType.Auto);
            TotalProduct.Height = new GridLength(1, GridUnitType.Auto);
            TotalProductionProduct.Height = new GridLength(1, GridUnitType.Auto);
            if (!(bool)e.NewValue) //скрытие
            {
                SaveListViewSetting(sender as ListView);
            }
        }

        private void ProductionProductListView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ProductionProductList.Height = (bool)e.NewValue ? new GridLength(1, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            ProductList.Height = new GridLength(1, GridUnitType.Auto);
            OrderList.Height = new GridLength(1, GridUnitType.Auto);
            TotalOrder.Height = new GridLength(1, GridUnitType.Auto);
            TotalProduct.Height = new GridLength(1, GridUnitType.Auto);
            TotalProductionProduct.Height = new GridLength(1, GridUnitType.Auto);
            if (!(bool)e.NewValue) //скрытие
            {
                SaveListViewSetting(sender as ListView);
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            GridViewColumnHeader columnHeader = sender as GridViewColumnHeader;
            ListViewSort listViewSort = ListViewSorts.FirstOrDefault(l => l.СolumnHeader.Equals(columnHeader));
            if (listViewSort is null) //если столбец отсутствует в списке для сортировки
            {
                ListViewSorts.Add(new ListViewSort(columnHeader, SortDirection.Ascending)); //то добавить с начальным направлением сортировки "по возрастанию"
            }
            else //столбец есть в списке для сортировки
            {
                listViewSort.NextSortDirection(); //установить следующее направление сортировки (циклическое переключение)
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) <= 0) //при щелчке по заголовку НЕ БЫЛА зажата клавиша CTRL
            {
                ListViewSorts.Where(lws => !lws.СolumnHeader.Equals(columnHeader)).ToList().ForEach(lws => lws.ClearSortDirection()); //то сбросить для всех столбцов направление сортировки КРОМЕ ТЕКУЩЕГО
                _ = ListViewSorts.RemoveAll(lws => !lws.СolumnHeader.Equals(columnHeader)); //и удалить все КРОМЕ ТЕКУЩЕГО
            }
            if (OrderListView.IsVisible)
            {
                OrderListView.Items.SortDescriptions.Clear();
            }
            else if (ProductListView.IsVisible)
            {
                ProductListView.Items.SortDescriptions.Clear();
            }
            else if (ProductionProductListView.IsVisible)
            {
                ProductionProductListView.Items.SortDescriptions.Clear();
            }
            foreach (ListViewSort lws in ListViewSorts)
            {
                if (lws.ColumnSortDirection == SortDirection.None)
                {
                    continue;
                }
                if (OrderListView.IsVisible)
                {
                    OrderListView.Items.SortDescriptions.Add(new SortDescription(lws.SortBy, lws.ColumnSortDirection == SortDirection.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending));
                }
                else if (ProductListView.IsVisible)
                {
                    ProductListView.Items.SortDescriptions.Add(new SortDescription(lws.SortBy, lws.ColumnSortDirection == SortDirection.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending));
                }
                else if (ProductionProductListView.IsVisible)
                {
                    ProductionProductListView.Items.SortDescriptions.Add(new SortDescription(lws.SortBy, lws.ColumnSortDirection == SortDirection.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending));
                }
            }


            //GridViewColumnHeader column = sender as GridViewColumnHeader;
            //string sortBy = column.Tag.ToString();
            //if (listViewSortCol != null)
            //{
            //    AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
            //    if (OrderListView.IsVisible)
            //    {
            //        OrderListView.Items.SortDescriptions.Clear();
            //    }
            //    else if (ProductListView.IsVisible)
            //    {
            //        ProductListView.Items.SortDescriptions.Clear();
            //    }
            //    else if (ProductionProductListView.IsVisible)
            //    {
            //        ProductionProductListView.Items.SortDescriptions.Clear();
            //    }
            //}

            //ListSortDirection newDir = ListSortDirection.Ascending;
            //if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
            //{
            //    newDir = ListSortDirection.Descending;
            //}

            //listViewSortCol = column;
            //listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            //AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            //if (OrderListView.IsVisible)
            //{
            //    OrderListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
            //}
            //else if (ProductListView.IsVisible)
            //{
            //    ProductListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
            //}
            //else if (ProductionProductListView.IsVisible)
            //{
            //    ProductionProductListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
            //}
        }

        protected class ListViewSort
        {
            protected internal GridViewColumnHeader СolumnHeader { get; set; }                          //определение заголовка столбца GridView в составе ListView
            protected AdornerSort ColumnSortAdorner { get; set; }                                       //рисуемый значок направления сортировки
            protected internal SortDirection ColumnSortDirection { get; set; } = SortDirection.None;    //новое/текущее направление сортировки 
            protected internal string SortBy { get; set; }                                              //столбец для сортировки

            public ListViewSort(GridViewColumnHeader gridViewColumnHeader, SortDirection sortDirection)
            {
                СolumnHeader = gridViewColumnHeader;
                ColumnSortDirection = sortDirection;
                SortBy = gridViewColumnHeader.Tag.ToString();
                AddAdornerLayer();
            }

            protected internal void NextSortDirection() //установить следующее направление сортировки
            {
                static SortDirection Next(SortDirection sortDirection) => sortDirection switch
                {
                    SortDirection.None => SortDirection.Ascending,
                    SortDirection.Ascending => SortDirection.Descending,
                    _ => SortDirection.None //в случае SortDirection.Descending
                };
                RemoveAdornerLayer();
                ColumnSortDirection = Next(ColumnSortDirection);
                AddAdornerLayer();
            }

            protected internal void ClearSortDirection() //сбросить направление сортировки
            {
                RemoveAdornerLayer();
                ColumnSortDirection = SortDirection.None;
            }

            private void AddAdornerLayer()
            {
                ColumnSortAdorner = ColumnSortDirection == SortDirection.None ? null :
                                    (ColumnSortDirection == SortDirection.Ascending ? new AdornerSort(СolumnHeader, SortDirection.Ascending) : new AdornerSort(СolumnHeader, SortDirection.Descending));
                if (ColumnSortAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer(СolumnHeader).Add(ColumnSortAdorner); //нарисовать символ направления сортировки
                }
            }

            private void RemoveAdornerLayer()
            {
                if (ColumnSortAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer(СolumnHeader).Remove(ColumnSortAdorner); //убрать символ направления сортировки
                }
            }

            protected class AdornerSort : Adorner
            {
                private static Geometry ascGeometry = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
                private static Geometry descGeometry = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

                public SortDirection Direction { get; private set; }

                public AdornerSort(UIElement element, SortDirection dir) : base(element)
                {
                    Direction = dir;
                }

                protected override void OnRender(DrawingContext drawingContext)
                {
                    base.OnRender(drawingContext);

                    if (AdornedElement.RenderSize.Width < 20)
                    {
                        return;
                    }

                    TranslateTransform transform = new TranslateTransform
                        (
                            AdornedElement.RenderSize.Width - 15,
                            (AdornedElement.RenderSize.Height - 5) / 2
                        );
                    drawingContext.PushTransform(transform);

                    Geometry geometry = ascGeometry;
                    if (Direction == SortDirection.Descending)
                    {
                        geometry = descGeometry;
                    }

                    drawingContext.DrawGeometry(Brushes.Black, null, geometry);
                    drawingContext.Pop();
                }
            }
        }

        private void FilterOrders()
        {
            ListViewSorts.Clear(); //очистить список для сортировки
            FilterWindow fw = new FilterWindow();
            if ((bool)fw.ShowDialog())
            {
                ShowOrders();
            }
        }

        private void FilterProducts()
        {
            ListViewSorts.Clear(); //очистить список для сортировки
            FilterWindow fw = new FilterWindow(orderFilterMode: 1);
            if ((bool)fw.ShowDialog())
            {
                ShowProducts();
            }
        }

        private void FilterProductionProducts()
        {
            ListViewSorts.Clear(); //очистить список для сортировки
            FilterWindow fw = new FilterWindow(orderFilterMode: 2);
            if ((bool)fw.ShowDialog())
            {
                ShowProductionProducts();
            }
        }

        private void Email_Item_Click(object sender, RoutedEventArgs e)
        {
            return;

            EmailWindow ew = new EmailWindow
            {
            };
            ew.Show();
        }

        private void FastReportDesigner_Item_Click(object sender, RoutedEventArgs e)
        {
            if (Userdata.IsAdmin)
            {
                using FastReport.Report report = new FastReport.Report();
                {
                    FastReport.Utils.Res.LoadLocale(@"Localization\Russian.frl");
                    _ = report.Design();
                }
            }
        }
    }

    public class SortAdorner : Adorner
    {
        private static Geometry ascGeometry = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
        private static Geometry descGeometry = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public ListSortDirection Direction { get; private set; }

        public SortAdorner(UIElement element, ListSortDirection dir) : base(element)
        {
            Direction = dir;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
            {
                return;
            }

            TranslateTransform transform = new TranslateTransform
                (
                    AdornedElement.RenderSize.Width - 15,
                    (AdornedElement.RenderSize.Height - 5) / 2
                );
            drawingContext.PushTransform(transform);

            Geometry geometry = ascGeometry;
            if (Direction == ListSortDirection.Descending)
            {
                geometry = descGeometry;
            }

            drawingContext.DrawGeometry(Brushes.Black, null, geometry);
            drawingContext.Pop();
        }
    }

    public static class OrderPaid
    {
        public static bool NotPaid(ref Order order) //не оплачено
        {
            return order.OrderPayments == 0 && order.OrderCost > 0;
        }
        public static bool PartiallyPaid(ref Order order) //частично оплачено
        {
            return order.OrderCost > 0 && order.OrderPayments > 0 && order.OrderPayments < order.OrderCost;
        }
        public static bool OverPaid(ref Order order) //переплачено
        {
            return order.OrderPayments > order.OrderCost;
        }
    }

    public class OrderBackgroundPaymentSelector : IValueConverter
    {
        public SolidColorBrush Paid = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Order order = (Order)value;
            if (OrderPaid.NotPaid(ref order)) //не оплачено
            {
                return MainWindow.orderLegendColors.NotPaidColor;
            }
            else if (OrderPaid.PartiallyPaid(ref order)) //частично оплачено
            {
                return MainWindow.orderLegendColors.PartiallyPaidColor;
            }
            else if (OrderPaid.OverPaid(ref order)) //переплачено
            {
                return MainWindow.orderLegendColors.OverPaidColor;
            }
            return Paid;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OrderForegroundPaymentSelector : IValueConverter
    {
        public SolidColorBrush Paid = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Order order = (Order)value;
            if (OrderPaid.NotPaid(ref order)) //не оплачено
            {
                return GetFontAttribute("NotPaid", (string)parameter);
            }
            else if (OrderPaid.PartiallyPaid(ref order)) //частично оплачено
            {
                return GetFontAttribute("PartiallyPaid", (string)parameter);
            }
            else if (OrderPaid.OverPaid(ref order)) //переплачено
            {
                return GetFontAttribute("OverPaid", (string)parameter);
            }
            return GetFontAttribute("Default", (string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        private object GetFontAttribute(string sMode, string sParameter)
        {
            if (sParameter == "Foreground")
            {
                if (sMode == "NotPaid")
                {
                    return MainWindow.orderLegendColors.NotPaidFont.Foreground;
                }
                else if (sMode == "PartiallyPaid")
                {
                    return MainWindow.orderLegendColors.PartiallyPaidFont.Foreground;
                }
                else if (sMode == "OverPaid")
                {
                    return MainWindow.orderLegendColors.OverPaidFont.Foreground;
                }
                return MainWindow.orderLegendColors.DefaultFont.Foreground;
            }
            else if (sParameter == "FontFamily")
            {
                if (sMode == "NotPaid")
                {
                    return MainWindow.orderLegendColors.NotPaidFont.FontFamily;
                }
                else if (sMode == "PartiallyPaid")
                {
                    return MainWindow.orderLegendColors.PartiallyPaidFont.FontFamily;
                }
                else if (sMode == "OverPaid")
                {
                    return MainWindow.orderLegendColors.OverPaidFont.FontFamily;
                }
                return MainWindow.orderLegendColors.DefaultFont.FontFamily;
            }
            else if (sParameter == "FontSize")
            {
                if (sMode == "NotPaid")
                {
                    return MainWindow.orderLegendColors.NotPaidFont.FontSize;
                }
                else if (sMode == "PartiallyPaid")
                {
                    return MainWindow.orderLegendColors.PartiallyPaidFont.FontSize;
                }
                else if (sMode == "OverPaid")
                {
                    return MainWindow.orderLegendColors.OverPaidFont.FontSize;
                }
                return MainWindow.orderLegendColors.DefaultFont.FontSize;
            }
            else if (sParameter == "FontStyle")
            {
                if (sMode == "NotPaid")
                {
                    return MainWindow.orderLegendColors.NotPaidFont.FontStyle;
                }
                else if (sMode == "PartiallyPaid")
                {
                    return MainWindow.orderLegendColors.PartiallyPaidFont.FontStyle;
                }
                else if (sMode == "OverPaid")
                {
                    return MainWindow.orderLegendColors.OverPaidFont.FontStyle;
                }
                return MainWindow.orderLegendColors.DefaultFont.FontStyle;
            }
            else if (sParameter == "FontWeight")
            {
                if (sMode == "NotPaid")
                {
                    return MainWindow.orderLegendColors.NotPaidFont.FontWeight;
                }
                else if (sMode == "PartiallyPaid")
                {
                    return MainWindow.orderLegendColors.PartiallyPaidFont.FontWeight;
                }
                else if (sMode == "OverPaid")
                {
                    return MainWindow.orderLegendColors.OverPaidFont.FontWeight;
                }
                return MainWindow.orderLegendColors.DefaultFont.FontWeight;
            }
            return null;
        }
    }

    public static class CustomCommands
    {
        private static RelayCommand handBooks;
        public static RelayCommand HandBooks => handBooks ??= new RelayCommand((o) =>
        {
        }, (o) => MainWindow.Connectiondata.Is_verify_connection && (IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "MainMenuItemHandBook") || MainWindow.Userdata.Is_sysadmin));

        private static RelayCommand reports;
        public static RelayCommand Reports => reports ??= new RelayCommand((o) =>
        {
            ReportWindow rw = new ReportWindow();
            _ = rw.ShowDialog();
        }, (o) => MainWindow.Connectiondata.Is_verify_connection && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "MainMenuItemReport"));

        private static RelayCommand setting;
        public static RelayCommand Setting => setting ??= new RelayCommand((o) =>
        {
        }, (o) => MainWindow.Connectiondata.Is_verify_connection && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "MainMenuItemSetting"));

        private static RelayCommand systemSetting;
        public static RelayCommand SystemSetting => systemSetting ??= new RelayCommand((o) =>
        {
            SettingWindow ss = new SettingWindow { };
            _ = ss.ShowDialog();
        }, (o) => MainWindow.Userdata.IsAdmin || MainWindow.Userdata.Is_sysadmin);

        private static RelayCommand accessMatrix;
        public static RelayCommand AccessMatrix => accessMatrix ??= new RelayCommand((o) =>
        {
            AccessMatrixWindow am = new AccessMatrixWindow();
            _ = am.ShowDialog();
        }, (o) => MainWindow.Userdata.IsAdmin || MainWindow.Userdata.Is_sysadmin);

        private static RelayCommand constructors;
        public static RelayCommand Constructors => constructors ??= new RelayCommand((o) =>
        {
        }, (o) => MainWindow.Connectiondata.Is_verify_connection && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "MainMenuItemConstructor"));

        private static RelayCommand usersAndRoles;
        public static RelayCommand UsersAndRoles => usersAndRoles ??= new RelayCommand((o) =>
        {
            UsersAndRolesWindow ur = new UsersAndRolesWindow
            {
                Owner = Application.Current.MainWindow
            };
            _ = ur.ShowDialog();
        }, (o) => MainWindow.Userdata.Is_sysadmin);

        private static RelayCommand newOrder;
        public static RelayCommand NewOrder => newOrder ??= new RelayCommand((o) =>
        {
            RequestWindow requestWindow = new RequestWindow();
            _ = requestWindow.ShowDialog();
            object btn = Application.Current.MainWindow.FindName("RefreshOrderButton");
            _ = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(btn, new object[0]);
        }, null);
#if NEWORDER
#else
        //    OrderWindow order = new OrderWindow(NewOrder: true) { };
        //    _ = order.ShowDialog();
        //    object btn = Application.Current.MainWindow.FindName("RefreshOrderButton");
        //    _ = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(btn, new object[0]);
        //}, null);
#endif
    }

    public class SumListViewDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IEnumerable enumerable))
            {
                return DependencyProperty.UnsetValue;
            }

            IEnumerable<object> collection = enumerable.Cast<object>();

            PropertyInfo property = null;
            if (parameter is string propertyName && collection.Any())
            {
                property = collection.First().GetType().GetProperty(propertyName);
            }

            return collection.Select(x => System.Convert.ToDecimal(property != null ? property.GetValue(x) : x)).Sum();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

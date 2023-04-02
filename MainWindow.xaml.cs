﻿using System;
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
using System.Drawing;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Reflection;

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
        public static List<string> WhereProductCategoryCondition = new List<string> { }, WhereProductClientCondition = new List<string> { },
            WhereProductManagerCondition = new List<string> { }, WhereProductDesignerCondition = new List<string> { };
        public static List<long> WhereTypeOfActivityCondition = new List<long> { };
        public static DateTime? dStartDate, dEndDate;

        public MainWindow()
        {
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
            Connectiondata = new ConnectionData();
            section.Bind(Connectiondata);
            Connectiondata.Is_verify_connection = false;
            statusBar.ClearStatus();
            orderLegendColors = new OrderLegendColors();
            //Connectiondata.Connectionstring = "Data Source = YEVGENIY; Database = AdvertisementNF; Integrated Security = True;"; // УБРАТЬ!!!!
            Connectiondata.Connectionstring = "Data Source = ; Database = ; Integrated Security = True";
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
            LogonWindow logon = new LogonWindow
            {
                Owner = this
            };
            if ((bool)logon.ShowDialog())
            {
                if (Connectiondata.Is_verify_connection)
                {
                    SetSetting();
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

        private void BankLocalityUnit_Item_Click(object sender, RoutedEventArgs e)
        {
            SmallHandBookWindow hb = new SmallHandBookWindow
            {
                Owner = this
            };
            _ = hb.ShowDialog();
        }

        private void Handbook_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void Handbook_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem && Connectiondata != null && Connectiondata.Is_verify_connection)
            {
                if (menuItem.Name == "ReportMenuItem")
                {
                    e.CanExecute = true;
                    return;
                }
            }
            e.CanExecute = Connectiondata != null && Userdata != null
                ? Connectiondata.Is_verify_connection && (Userdata.IsAdmin || Userdata.Is_sysadmin)
                : false;
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

        private void UserAndRoles_Item_Click(object sender, RoutedEventArgs e)
        {
            UsersAndRolesWindow ur = new UsersAndRolesWindow
            {
                Owner = this
            };
            _ = ur.ShowDialog();
        }

        private void ClientsAndContractors_Item_Click(object sender, RoutedEventArgs e)
        {
            ClientsAndContractorsHandbook cc = new ClientsAndContractorsHandbook
            {
                Owner = this
            };
            _ = cc.ShowDialog();
        }

        private void SystemSetting_Item_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow ss = new SettingWindow { };
            _ = ss.ShowDialog();
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
                if (btn.Name == "NewOrderButton" || btn.Name == "NewOrderButton1")
                {
                    OrderWindow order = new OrderWindow(NewOrder: true) { };
                    _ = order.ShowDialog();
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
                    OrderWindow order = new OrderWindow(NewOrder: false, EditMode: btn == EditOrderButton, nOrderID: nOrderID) { };
                    _ = order.ShowDialog();
                }
                else if (btn == AllOrdersButton)
                {
                    //очистить условие отбора
                    WhereCondition = WhereStateCondition = "";
                    ShowOrders();
                }
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

        public static void RefreshOrderProduct()
        {
            MainWindow mainWnd = (MainWindow)Application.Current.MainWindow;
            if (mainWnd.OrderListView.IsVisible)
            {
                int nCurrentIndex = mainWnd.ordersViewSource.View.CurrentPosition >= 0 ? mainWnd.ordersViewSource.View.CurrentPosition : 0;
                _ = mainWnd.ShowDialog();
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

        private void Order_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Connectiondata != null && Connectiondata.Is_verify_connection)
            {
                if (e.OriginalSource is Button btn)
                {
                    if ((btn == NewOrderButton || btn == NewOrderButton1) && Userdata.ID > 0)
                    {
                        e.CanExecute = true;
                    }
                    if (btn == AllOrdersButton || (btn == EditOrderButton && OrderListView.IsVisible && ordersViewSource?.View != null && ordersViewSource.View.CurrentItem is Order))
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == EditOrderButton && ProductListView.IsVisible && productsViewSource?.View != null && productsViewSource.View.CurrentItem is Product)
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == EditOrderButton && ProductionProductListView.IsVisible && workInTechCardViewSource?.View != null && workInTechCardViewSource.View.CurrentItem is WorkInTechCard)
                    {
                        e.CanExecute = true;
                    }                    
                    else if (btn == ViewOrderButton && OrderListView.IsVisible && ordersViewSource != null && ordersViewSource.View != null && ordersViewSource.View.CurrentItem is Order)
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == ViewOrderButton && ProductListView.IsVisible && productsViewSource?.View.CurrentItem is Product)
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == ViewOrderButton && ProductionProductListView.IsVisible && workInTechCardViewSource?.View != null && workInTechCardViewSource.View.CurrentItem is WorkInTechCard)
                    {
                        e.CanExecute = true;
                    }
                    else if (btn == FilterOrdersButton || btn == FilterProductsButton || btn == FilterProductionProductsButton)
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

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn)
            {
                if (btn.Name == "DeleteOrderButton" && ordersViewSource.View.CurrentItem is Order order)
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
                    if (btn == DeleteOrderButton && ordersViewSource.View.CurrentItem is Order && Userdata.IsAdmin)
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
                if (IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    OrderList = OrderList.Where(w => w.ManagerID == Userdata.ID).ToList(); //значит текущему пользователю предоставить только его заказы
                }
                if (WhereStateCondition.Length > 0) //указан отбор по состоянию заказа
                {
                    OrderList = OrderList.Where(Order => WhereStateCondition.IndexOf(Order.State) >= 0).ToList();
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
                    .Include(Products => Products.Order)
                    .Include(Products => Products.Designer)
                    .ToList();
                if (IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    ProductList = ProductList.Where(p => p.Order.ManagerID == Userdata.ID).ToList(); //значит текущему пользователю предоставить только его заказы
                }
                //дата приема заказа
                if (dStartDate.HasValue && dEndDate.HasValue)
                {
                    ProductList = ProductList.Where(Product => Product.Order.DateAdmission >= dStartDate && Product.Order.DateAdmission <= dEndDate).ToList();
                }
                //категория
                if (WhereProductCategoryCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductCategoryCondition.Contains(Product.ProductType.CategoryOfProductID.ToString())).ToList();
                }
                //клиент
                if (WhereProductClientCondition.Count > 0)
                {
                    ProductList = ProductList.Where(Product => WhereProductClientCondition.Contains(Product.Order.ClientID.ToString())).ToList();
                }
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
                workInTechCards = _context.WorkInTechCards.AsNoTracking()
                    .Include(WorkInTechCard => WorkInTechCard.TechCard)
                    .Include(WorkInTechCard => WorkInTechCard.TypeOfActivity)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product.ProductType)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product.Order)
                    .Include(WorkInTechCard => WorkInTechCard.TechCard.Product.Order.Client)
                    .Where(WorkInTechCard => WhereTypeOfActivityCondition.Contains(WorkInTechCard.TypeOfActivity.ID))
                    .ToList();
                workInTechCardViewSource = (CollectionViewSource)FindResource(nameof(workInTechCardViewSource));
                if (IGrantAccess.CheckGrantAccess(userIAccessMatrix, Userdata.RoleID, "ListManager")) //роль текущего пользователя относится к Менеджерам
                {
                    workInTechCards = workInTechCards.Where(w => w.TechCard.Product.Order.ManagerID == Userdata.ID).ToList(); //значит текущему пользователю предоставить только его заказы
                }
                workInTechCardViewSource.Source = workInTechCards.Where(w => w.TechCard.Product.State == OrderProductStates.GetProductState(3) || w.TechCard.Product.State == OrderProductStates.GetProductState(4))
                    .OrderBy(w => w.TechCard.Product.Order.Number);
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

        private void FilterOrders()
        {
            FilterWindow fw = new FilterWindow();
            if ((bool)fw.ShowDialog())
            {
                ShowOrders();
            }
        }

        private void FilterProducts()
        {
            FilterWindow fw = new FilterWindow(orderFilterMode: 1);
            if ((bool)fw.ShowDialog())
            {
                ShowProducts();
            }
        }

        private void FilterProductionProducts()
        {
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

        private void AccessMatrix_Item_Click(object sender, RoutedEventArgs e)
        {
            AccessMatrixWindow am = new AccessMatrixWindow();
            _ = am.ShowDialog();
        }

        private void ReportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow rw = new ReportWindow();
            _ = rw.ShowDialog();
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
}

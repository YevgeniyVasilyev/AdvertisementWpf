using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ReportWindow.xaml
    /// </summary>
    public partial class ReportWindow : Window
    {
        private CollectionViewSource reportsViewSource, usersViewSource, clientsViewSource;
        private App.AppDbContext _context;

        public List<User> usersList = new List<User> { };

        private string _filterString { get; set; } = "";
        public string FilterString
        {
            get => _filterString;
            set
            {
                _filterString = value;
                if (clientsViewSource?.View != null)
                {
                    clientsViewSource.View.Refresh();
                }
            }
        }

        public ReportWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();
            _ = ReportListBox.Focus();

            try
            {
                MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

                YearUpDown.Value = DateTime.Today.Year;
                QuarterComboBox.SelectedIndex = ((DateTime.Today.Month + 2) / 3) - 1;
                MonthCheckBox.SelectedIndex = DateTime.Today.Month - 1;
                DayDateTime.SelectedDate = DateTime.Today;
                StartDate.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                EndDate.SelectedDate = DateTime.Today;

                _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
                reportsViewSource = (CollectionViewSource)FindResource(nameof(reportsViewSource));
                usersViewSource = (CollectionViewSource)FindResource(nameof(usersViewSource));
                clientsViewSource = (CollectionViewSource)FindResource(nameof(clientsViewSource));

                reportsViewSource.Source = _context.Reports.AsNoTracking().ToList();
                usersList = _context.Users.AsNoTracking().ToList();
                clientsViewSource.Source = _context.Clients.AsNoTracking().ToList();
                clientsViewSource.View.Filter = ClientFilter;

                reportsViewSource.View.CurrentChanged += ReportsViewSource_CurrentChanged;
                ReportsViewSource_CurrentChanged(null, null);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка инициализации формы отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void ReportsViewSource_CurrentChanged(object sender, EventArgs e)
        {
            if (reportsViewSource?.View != null && reportsViewSource.View.CurrentItem is Report report)
            {
                if (report.Code == "RORW" || report.Code == "RORWRP" ||  report.Code == "BCAFPD" || report.Code == "__AR")
                {
                    usersViewSource.Source = usersList.Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListManager")); //только менеджеры
                }
                else if (report.Code == "MBTD" || report.Code == "PSFD")
                {
                    usersViewSource.Source = usersList.Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListDesigner")); //только дизайнеры
                }
                else
                {
                    usersViewSource.Source = usersList.Where(u => !IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListDesigner")); //сотрудники без дизайнеров
                }
                usersViewSource.View.Refresh();
            }
        }

        private bool ClientFilter(object item)
        {
            Client client = item as Client;
            return string.IsNullOrEmpty(_filterString) || client.Name.IndexOf(_filterString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            MainWindow.statusBar.ClearStatus();
        }

        private void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is RepeatButton || e.OriginalSource is Xceed.Wpf.Toolkit.WatermarkTextBox)
            {
                YearDate.IsChecked = true;
                return;
            }
            if (e.OriginalSource is CalendarDayButton && sender is DatePicker datePicker)
            {
                if (datePicker.Name == "DayDateTime")
                {
                    DayDate.IsChecked = true;
                }
                if (datePicker.Name == "StartDate" || datePicker.Name == "EndDate")
                {
                    PeriodDate.IsChecked = true;
                }
                return;
            }
            if (e.OriginalSource is ComboBox comboBox)
            {
                if (comboBox.Name == "QuarterComboBox")
                {
                    QuarterDate.IsChecked = true;
                }
                if (comboBox.Name == "MonthCheckBox")
                {
                    MonthDate.IsChecked = true;
                }
                return;
            }
        }

        private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn && reportsViewSource.View.CurrentItem is Report report)
            {
                if (btn == MakeReportButton)
                {
                    DateTime dBeginPeriod = new DateTime(DateTime.Today.Year, 1, 1); //01 января текущего года
                    DateTime dEndPeriod = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59); //31 декабря текущего года
                    List <User> userList = new List<User> { };
                    List <long> clientsIDList = new List<long> { };
                    try
                    {
                        if (PeriodGroupBox.IsEnabled) //обработка условия "Дата"
                        {
                            if ((bool)YearDate.IsChecked) //отбор за "год"
                            {
                                dBeginPeriod = new DateTime((int)YearUpDown.Value, 1, 1); //01 января текущего года
                                dEndPeriod = new DateTime((int)YearUpDown.Value, 12, 31, 23, 59, 59); //31 декабря текущего года
                            }
                            if ((bool)QuarterDate.IsChecked) //отбор за "квартал"
                            {
                                switch (QuarterComboBox.SelectedIndex)
                                {
                                    case 0: //1 квартал
                                        dEndPeriod = new DateTime(DateTime.Today.Year, 3, 31, 23, 59, 59);
                                        break;
                                    case 1: //2 квартал
                                        dBeginPeriod = new DateTime(DateTime.Today.Year, 4, 1);
                                        dEndPeriod = new DateTime(DateTime.Today.Year, 6, 30, 23, 59, 59);
                                        break;
                                    case 2: //3 квартал
                                        dBeginPeriod = new DateTime(DateTime.Today.Year, 7, 1);
                                        dEndPeriod = new DateTime(DateTime.Today.Year, 9, 30, 23, 59, 59);
                                        break;
                                    case 3: //4 квартал
                                        dBeginPeriod = new DateTime(DateTime.Today.Year, 10, 1);
                                        break;
                                }
                            }
                            if ((bool)MonthDate.IsChecked) //отбор за "месяц"
                            {
                                dBeginPeriod = new DateTime(DateTime.Today.Year, MonthCheckBox.SelectedIndex + 1, 1);
                                dEndPeriod = new DateTime(DateTime.Today.Year, MonthCheckBox.SelectedIndex + 1, DateTime.DaysInMonth(DateTime.Today.Year, MonthCheckBox.SelectedIndex + 1), 23, 59, 59);
                            }
                            if ((bool)DayDate.IsChecked && DayDateTime.SelectedDate.HasValue) //отбор за "день"
                            {
                                dBeginPeriod = DayDateTime.SelectedDate.Value;
                                dEndPeriod = new DateTime(dBeginPeriod.Year, dBeginPeriod.Month, dBeginPeriod.Day, 23, 59, 59);
                            }
                            if ((bool)PeriodDate.IsChecked && StartDate.SelectedDate.HasValue && EndDate.SelectedDate.HasValue) //отбор за "произвольный интервал"
                            {
                                dBeginPeriod = StartDate.SelectedDate.Value;
                                dEndPeriod = EndDate.SelectedDate.Value;
                                dEndPeriod = new DateTime(dEndPeriod.Year, dEndPeriod.Month, dEndPeriod.Day, 23, 59, 59);
                            }
                        }
                        if (UserGroupBox.IsEnabled) //обработка условия "Сотрудники"
                        {
                            foreach (object u in UserListBox.Items)
                            {
                                ListBoxItem listBoxitem = (ListBoxItem)UserListBox.ItemContainerGenerator.ContainerFromItem(u);
                                if (listBoxitem != null && listBoxitem.IsSelected)
                                {
                                    userList.Add((User)u);
                                    if (report.Code == "BCAFPD") //для данного отчета брать только первого отмеченного
                                    {
                                        break; 
                                    }
                                }
                            }
                            if (userList.Count == 0 && report.Code != "BCAFPD") //если не было отмечено ни одного, то берем всех кроме отчета "BCAFPD"
                            {
                                userList.AddRange((IEnumerable<User>)usersViewSource.View.SourceCollection);
                            }
                        }
                        if (ClientGroupBox.IsEnabled) //обработка условия "Клиенты"
                        {
                            foreach (object c in ClientListBox.Items)
                            {
                                ListBoxItem listBoxitem = (ListBoxItem)ClientListBox.ItemContainerGenerator.ContainerFromItem(c);
                                if (listBoxitem != null && listBoxitem.IsSelected)
                                {
                                    clientsIDList.Add(((Client)c).ID);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования условия отбора", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        MainWindow.statusBar.ClearStatus();
                        MakeReport(ref report, dBeginPeriod, dEndPeriod, userList, clientsIDList);
                    }
                }
            }
        }

        private void Print_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is Button btn)
            {
                if (btn == MakeReportButton && reportsViewSource?.View?.CurrentItem is Report)
                {
                    e.CanExecute = true;
                }
            }
        }

        private void MakeReport(ref Report report, DateTime beginPeriod, DateTime endPeriod, List<User> usersList, List<long> cliensIDtList)
        {
            endPeriod = new DateTime(endPeriod.Year, endPeriod.Month, endPeriod.Day, 23, 59, 59); //добавить минуты конца дня
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            using App.AppDbContext _report = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);
                string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                bool lCanMakeReport = false;
                if (File.Exists(Path.Combine(_pathToReportTemplate, $"{report.Code}.frx")))
                {
                    Reports.ReportFileName = Path.Combine(_pathToReportTemplate, $"{report.Code}.frx");
                    if (report.Code == "VMP") //volume of mastered products
                    {
                        Reports.ReportMode = report.Code;
                        var grouping = from pCost in _report.ProductCosts
                                       join p in _report.Products on pCost.ProductID equals p.ID
                                       join tofa in _report.TypeOfActivitys on pCost.TypeOfActivityID equals tofa.ID
                                       where p.DateManufacture >= beginPeriod && p.DateManufacture <= endPeriod
                                       orderby tofa.Code, p.DateManufacture
                                       select new
                                       {
                                           code = tofa.Code,
                                           name = tofa.Name,
                                           date = $"{InWords.MonthName((DateTime)pCost.Product.DateManufacture)}, {pCost.Product.DateManufacture.Value.Year}",
                                           month = pCost.Product.DateManufacture.Value.Month,
                                           year = pCost.Product.DateManufacture.Value.Year,
                                           cost = pCost.Cost
                                       };
                        Reports.ObjectDataSet = new List<object> { };
                        Reports.ObjectDataSet.AddRange(grouping);
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.ObjectDataSet.Count > 0;
                    }
                    else if (report.Code == "MBTD" || report.Code == "PSFD") //mastered by the designer | payment statement for designers
                    {
                        Reports.ReportMode = report.Code;
                        Reports.ProductCostDataSet = _report.ProductCosts
                            .Include(pc => pc.Product).ThenInclude(product => product.Designer)
                            .Include(pc => pc.Product.Order)
                            .Include(pc => pc.Product.ProductType)
                            .Include(pc => pc.TypeOfActivity)
                            .AsNoTracking()
                            .Where(pc => pc.TypeOfActivity.Code.Trim() == "10" && usersList.Contains(pc.Product.Designer) && pc.Product.DateApproval >= beginPeriod && pc.Product.DateApproval <= endPeriod)
                            .OrderBy(pc => pc.Product.DesignerID)
                            .ToList();
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.ProductCostDataSet.Count > 0;
                    }
                    else if (report.Code == "RETP")
                    {
                        Reports.ReportMode = report.Code;
                        Reports.PaymentsDataSet = _report.Payments
                            .Include(Payment => Payment.Order)
                            .Include(Payment => Payment.Order.Manager)
                            .AsNoTracking()
                            .Where(Payment => (Payment.TypeOfPayment == 0 || Payment.TypeOfPayment == 1) && Payment.PaymentDate >= beginPeriod && Payment.PaymentDate <= endPeriod
                                                && usersList.Contains(Payment.Order.Manager))
                            .OrderBy(Payment => Payment.Order.Manager.ID)
                            .ToList();
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.PaymentsDataSet.Count > 0;
                    }
                    else if (report.Code == "RORW")
                    {
                        Reports.ReportMode = report.Code;

                        var orders = from o in _report.Orders
                                     join m in _report.Users on o.ManagerID equals m.ID
                                     where usersList.Contains(o.Manager)
                                     group o by new { o.ID, o.Number, m.FirstName, m.LastName, m.MiddleName } into grp
                                     select new
                                     {
                                         orderID = grp.Key.ID,
                                         number = grp.Key.Number,
                                         manager = $"{grp.Key.FirstName} {grp.Key.LastName} {grp.Key.MiddleName}"
                                     };

                        var products = (from op in orders.ToList()
                                        join p in _report.Products on op.orderID equals p.OrderID
                                        join pt in _report.ProductTypes on p.ProductTypeID equals pt.ID
                                        join pc in _report.ProductCosts on p.ID equals pc.ProductID
                                        where p.DateManufacture >= beginPeriod && p.DateManufacture <= endPeriod
                                        group p by new { op.orderID, p.Quantity, op.number, op.manager } into grp
                                        select new
                                        {
                                            id = grp.Key.orderID,
                                            grp.Key.number,
                                            Name = grp.First().ProductType.Name,
                                            cost = grp.First().ProductCosts.Sum(pc => pc.Cost),
                                            outlay = grp.First().ProductCosts.Sum(pc => pc.Outlay),
                                            quantity = grp.Key.Quantity,
                                            DateManufacture = grp.First().DateManufacture,
                                            grp.Key.manager
                                        }).OrderBy(p => p.id);

                        Reports.ObjectDataSet = new List<object> { };
                        Reports.ObjectDataSet.AddRange(products);
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.ObjectDataSet.Count > 0;
                    }
                    else if (report.Code == "BCAFPD" && usersList.Count > 0)
                    {
                        Reports.ReportMode = report.Code;

                        //Заказ должен соответствовать одному из условий:
                        //дата последней отгрузки меньше или равна верхней дате интервала и дата последнего платежа в указанном интервале (1),
                        //либо дата последнего платежа меньше или равна верхней дате интервала и дата последней отгрузки в указанном интервале. (2)
                        //(т.е.смотрим оплачен ли уже изготовленый ранее заказ или изготовлен уже оплаченый и в том и втом случае заказ попадает в таблицу)

                        //    ВНИМАНИЕ!!! Если вдруг в датах при выборке будет NULL, то получим ошибку

                        var orderPayments = from pay in _report.Payments
                                            join o in _report.Orders on pay.OrderID equals o.ID
                                            join c in _report.Clients on o.ClientID equals c.ID
                                            where usersList.Contains(o.Manager)
                                            group pay by new { o.ID, o.Number, c.Name } into grp
                                            select new
                                            {
                                                orderID = grp.Key.ID,
                                                number = grp.Key.Number,
                                                paymentSum = grp.Sum(pay => pay.PaymentAmount),
                                                maxPaymentDate = grp.Max(pay => pay.PaymentDate),
                                                client = grp.Key.Name
                                            }; //платежи по заказам

                        var productCosts = (from op in orderPayments.ToList()
                                            join p in _report.Products on op.orderID equals p.OrderID
                                            join pc in _report.ProductCosts on p.ID equals pc.ProductID
                                            group pc by new { p.OrderID, op.number, op.maxPaymentDate, op.paymentSum, op.client } into grp
                                            select new
                                            {
                                                orderID = grp.Key.OrderID,
                                                number = grp.Key.number,
                                                cost = grp.Sum(pc => pc.Cost),
                                                outlay = grp.Sum(pc => pc.Outlay),
                                                margin = grp.Sum(pc => pc.Margin),
                                                maxDateShipment = grp.Max(pc => pc.Product.DateShipment),
                                                paymentSum = grp.Key.paymentSum,
                                                maxPaymentDate = grp.Key.maxPaymentDate,
                                                client = grp.Key.client,
                                                manager = usersList.FirstOrDefault().FullUserName
                                            }).Where(pc => (pc.maxDateShipment <= endPeriod && pc.maxPaymentDate >= beginPeriod && pc.maxPaymentDate <= endPeriod)
                                                     || (pc.maxPaymentDate <= endPeriod && pc.maxDateShipment >= beginPeriod && pc.maxDateShipment <= endPeriod))
                                           .OrderBy(pc => pc.orderID)
                                           .ToList(); //оплата, затраты и маржа по заказам
                        Order order;
                        for (int ind = 0; ind < productCosts.Count; ind++)
                        {
                            order = _report.Orders.Include(o => o.Products).First(o => o.ID == productCosts[ind].orderID);
                            if (order.State != OrderProductStates.GetOrderState(4))
                            {
                                productCosts.RemoveAt(ind); //удалить все заказы НЕ в состоянии ОТГРУЖЕН
                                ind--;
                            }
                        }
                        Reports.ObjectDataSet = new List<object> { };
                        Reports.ObjectDataSet.AddRange(productCosts);
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.ObjectDataSet.Count > 0;
                    }
                    else if (report.Code == "__AR")
                    {
                        var orders = from o in _report.Orders
                                     join p in _report.Payments on o.ID equals p.OrderID into pay
                                     from p in pay.DefaultIfEmpty()
                                     join c in _report.Clients on o.ClientID equals c.ID
                                     join m in _report.Users on o.ManagerID equals m.ID
                                     where o.DateAdmission >= beginPeriod && o.DateAdmission <= endPeriod && usersList.Contains(o.Manager)
                                     group p by new { o.ID, o.Number, o.Note, o.ClientID, c.Name, m.FirstName, m.LastName, m.MiddleName} into grp
                                     select new
                                     {
                                         orderID = grp.Key.ID,
                                         number = grp.Key.Number,
                                         note = grp.Key.Note,
                                         paymentSum = grp.Sum(p => p.PaymentAmount),
                                         client = grp.Key.Name,
                                         clientID = grp.Key.ClientID,
                                         manager = $"{grp.Key.FirstName} {grp.Key.LastName} {grp.Key.MiddleName}"
                                     };

                        var productCosts = (from op in orders.ToList()
                                            join p in _report.Products on op.orderID equals p.OrderID
                                            join pc in _report.ProductCosts on p.ID equals pc.ProductID
                                            group pc by new { op.orderID, op.clientID, op.number, op.client, op.note, op.manager, op.paymentSum } into grp
                                            select new
                                            {
                                                grp.Key.orderID,
                                                grp.Key.clientID,
                                                grp.Key.number,
                                                cost = grp.Sum(pc => pc.Cost),
                                                grp.Key.paymentSum,
                                                dateManufactureMax = grp.Max(pc => pc.Product.DateManufacture),
                                                grp.Key.client,
                                                grp.Key.note,
                                                grp.Key.manager
                                            }).Where(pc => pc.paymentSum < pc.cost) //выбрать те где сумма оплат меньше стоимости
                                           .OrderBy(pc => pc.client)
                                           .ToList(); 

                        if (cliensIDtList.Count > 0)
                        {
                            _ = productCosts.RemoveAll(pc => !cliensIDtList.Contains(pc.clientID)); //удалить все заказы НЕ ВЫБРФННЫХ клиентов
                        }
                        Order order;
                        for (int ind = 0; ind < productCosts.Count; ind++)
                        {
                            order = _report.Orders.Include(o => o.Products).First(o => o.ID == productCosts[ind].orderID);
                            if (order.State == OrderProductStates.GetOrderState(0) || order.State == OrderProductStates.GetOrderState(1))
                            {
                                productCosts.RemoveAt(ind); //удалить все ИЗГОТОВЛЕННЫЕ, но НЕ ОПЛАЧЕННЫЕ заказы 
                                ind--;
                            }
                        }
                        Reports.ReportMode = report.Code;
                        Reports.ObjectDataSet = new List<object> { };
                        Reports.ObjectDataSet.AddRange(productCosts);
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.ObjectDataSet.Count > 0;
                    }
                    else if (report.Code == "RORWRP")
                    {
                        Reports.ReportMode = report.Code;
                        Reports.OrderDataSet = _report.Orders
                            .Include(Order => Order.Products).ThenInclude(Product => Product.ProductCosts)
                            .Include(Order => Order.Products).ThenInclude(Product => Product.ProductCosts).ThenInclude(ProductCost => ProductCost.TypeOfActivity)
                            .Include(Order => Order.Products).ThenInclude(Product => Product.ProductType)
                            .Where(Order => usersList.Contains(Order.Manager) && Order.Products.Any(Product => Product.DateManufacture >= beginPeriod && Product.DateManufacture <= endPeriod))
                            .AsNoTracking()
                            .OrderBy(Order => Order.ID)
                            .ToList();
                        foreach (Order order in Reports.OrderDataSet)
                        {
                            order.Products = order.Products.Where(product => product.DateManufacture >= beginPeriod && product.DateManufacture <= endPeriod).ToArray();
                        }

                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.OrderDataSet.Count > 0;
                    }
                    else if (report.Code == "TEST")
                    {
                        Reports.ReportMode = report.Code;
                        lCanMakeReport = true;
                    }
                    if (lCanMakeReport)
                    {
                        Reports.RunReport();
                    }
                    else //ничего не отобрано
                    {
                        _ = MessageBox.Show("Нет данных для включения в отчет!", "Получение данных для отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    _ = MessageBox.Show($"Не найден файл {report.Code}.frx !", "Ошибка формирования отчета", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_reportcontext != null)
                {
                    _reportcontext.Dispose();
                }
                if (_report != null)
                {
                    _report.Dispose();
                }
                MainWindow.statusBar.ClearStatus();
            }
        }
    }

    public class EnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Report report = (Report)value;
            if (report != null && (string)parameter == "P") //P - Period
            {
                return report.Parameters.Contains("P");
            }
            else if (report != null && (string)parameter == "PU") //U - User
            {
                return report.Parameters.Contains("P") && report.Parameters.Contains("U");
            }
            else if (report != null && (string)parameter == "PUC") //C - Client
            {
                return report.Parameters.Contains("P") && report.Parameters.Contains("U") && report.Parameters.Contains("C");
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }
}

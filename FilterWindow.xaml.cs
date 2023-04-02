using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AdvertisementWpf
{
    public partial class FilterWindow : Window
    {
        private CollectionViewSource categoryOfProductsDataSource, clientsDataSource, managersDataSource, designersDataSource, orderStatesDataSource, productStatesDataSource, typeOfActivityDataSource;
        private App.AppDbContext _context;
        private static short OrderFilterMode;

        private string sDateCondition = "", sClientCondition = "", sManagerCondition = "", sStateCondition = "", sNumberCondition = ""; //sDesignerCondition = "",
        private readonly List<string> LCategoryCondition = new List<string> { }, LClientCondition = new List<string> { }, LDesignerCondition = new List<string> { }, LManagerCondition = new List<string> { };

        public FilterWindow(short orderFilterMode = 0)
        {
            OrderFilterMode = orderFilterMode;
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();
            if (OrderFilterMode == 0)
            {
                OrdersTabItem.IsSelected = true;
            }
            else if (OrderFilterMode == 1)
            {
                ProductsTabItem.IsSelected = true;
            }
            else if (OrderFilterMode == 2)
            {
                ProductionProductsTabItem.IsSelected = true;
            }
            OrdersTabItem.IsEnabled = OrderFilterMode == 0;
            ProductsTabItem.IsEnabled = OrderFilterMode == 1;
            ProductionProductsTabItem.IsEnabled = OrderFilterMode == 2;

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            categoryOfProductsDataSource = (CollectionViewSource)FindResource(nameof(categoryOfProductsDataSource));
            clientsDataSource = (CollectionViewSource)FindResource(nameof(clientsDataSource));
            managersDataSource = (CollectionViewSource)FindResource(nameof(managersDataSource));
            designersDataSource = (CollectionViewSource)FindResource(nameof(designersDataSource));
            orderStatesDataSource = (CollectionViewSource)FindResource(nameof(orderStatesDataSource));
            productStatesDataSource = (CollectionViewSource)FindResource(nameof(productStatesDataSource));
            productStatesDataSource = (CollectionViewSource)FindResource(nameof(productStatesDataSource));
            typeOfActivityDataSource = (CollectionViewSource)FindResource(nameof(typeOfActivityDataSource));

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                categoryOfProductsDataSource.Source = _context.CategoryOfProducts.AsNoTracking().ToList();
                typeOfActivityDataSource.Source = _context.TypeOfActivitys.AsNoTracking().ToList();
                clientsDataSource.Source = _context.Clients.AsNoTracking().ToList();
                managersDataSource.Source = _context.Users.AsNoTracking().ToList().Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListManager"));  // ListManager
                designersDataSource.Source = _context.Users.AsNoTracking().ToList().Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListDesigner")); //ListDesigner
                orderStatesDataSource.Source = OrderProductStates.GetOrderListState();
                productStatesDataSource.Source = OrderProductStates.GetProductListState();

                if (OrdersTabItem.IsEnabled)
                {
                    oYearUpDown.Value = DateTime.Today.Year;
                    oQuarterComboBox.SelectedIndex = ((DateTime.Today.Month + 2) / 3) - 1;
                    oMonthCheckBox.SelectedIndex = DateTime.Today.Month - 1;
                    oDayDateTime.SelectedDate = DateTime.Today;
                    oStartDate.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    oEndDate.SelectedDate = DateTime.Today;
                }
                else if (ProductsTabItem.IsEnabled)
                {
                    pYearUpDown.Value = DateTime.Today.Year;
                    pQuarterComboBox.SelectedIndex = ((DateTime.Today.Month + 2) / 3) - 1;
                    pMonthCheckBox.SelectedIndex = DateTime.Today.Month - 1;
                    pDayDateTime.SelectedDate = DateTime.Today;
                    pStartDate.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    pEndDate.SelectedDate = DateTime.Today;
                }
                else if (ProductionProductsTabItem.IsEnabled)
                {
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

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersTabItem.IsEnabled)
            {
                OrderWhereCondition();
            }
            else if (ProductsTabItem.IsEnabled)
            {
                ProductWhereCondition();
            }
            else if (ProductionProductsTabItem.IsEnabled)
            {
                ProductionProductWhereCondition();
            }
        }

        private void OrderWhereCondition()
        {
            try
            {
                //обработка условия "Дата"
                string sDateName = "";
                DateTime dStartDate = new DateTime(DateTime.Today.Year, 1, 1); //01 января текущего года
                DateTime dEndDate = new DateTime(DateTime.Today.Year, 12, 31); //31 декабря текущего года
                if ((bool)oYearDate.IsChecked) //отбор за "год"
                {
                    dStartDate = new DateTime((int)oYearUpDown.Value, 1, 1); //01 января текущего года
                    dEndDate = new DateTime((int)oYearUpDown.Value, 12, 31); //31 декабря текущего года
                }
                if ((bool)oQuarterDate.IsChecked) //отбор за "квартал"
                {
                    switch (oQuarterComboBox.SelectedIndex)
                    {
                        case 0: //1 квартал
                            dEndDate = new DateTime(DateTime.Today.Year, 3, 31);
                            break;
                        case 1: //2 квартал
                            dStartDate = new DateTime(DateTime.Today.Year, 4, 1);
                            dEndDate = new DateTime(DateTime.Today.Year, 6, 30);
                            break;
                        case 2: //3 квартал
                            dStartDate = new DateTime(DateTime.Today.Year, 7, 1);
                            dEndDate = new DateTime(DateTime.Today.Year, 9, 30);
                            break;
                        case 3: //4 квартал
                            dStartDate = new DateTime(DateTime.Today.Year, 10, 1);
                            break;
                    }
                }
                if ((bool)oMonthDate.IsChecked) //отбор за "месяц"
                {
                    List<byte> nMonth = new List<byte> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                    dStartDate = new DateTime(DateTime.Today.Year, oMonthCheckBox.SelectedIndex + 1, 1);
                    dEndDate = new DateTime(DateTime.Today.Year, oMonthCheckBox.SelectedIndex + 1, nMonth[oMonthCheckBox.SelectedIndex] + (DateTime.IsLeapYear(DateTime.Today.Year) ? 1 : 0));
                }
                if ((bool)oDayDate.IsChecked && oDayDateTime.SelectedDate.HasValue) //отбор за "день"
                {
                    dStartDate = oDayDateTime.SelectedDate.Value;
                    dEndDate = dStartDate;
                }
                if ((bool)oPeriodDate.IsChecked && oStartDate.SelectedDate.HasValue && oEndDate.SelectedDate.HasValue) //отбор за "произвольный интервал"
                {
                    dStartDate = oStartDate.SelectedDate.Value;
                    dEndDate = oEndDate.SelectedDate.Value;
                }
                if (!(bool)oNoDate.IsChecked)
                {
                    switch (oDateName.SelectedIndex)
                    {
                        case 0:
                            sDateName = "DateAdmission";
                            break;
                        case 1:
                            sDateName = "DateCompletion";
                            break;
                        case 2:
                            sDateName = "DateProductionLayout";
                            break;
                    }
                    sDateCondition = $"{sDateName} >= '{dStartDate.Date}' AND {sDateName} <= '{dEndDate.Date}'";
                }
                //обработка условия "Клиент"
                foreach (object client in ClientsListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)ClientsListBox.ItemContainerGenerator.ContainerFromItem(client);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        sClientCondition += string.IsNullOrWhiteSpace(sClientCondition) ? "" : ", ";
                        sClientCondition += ((Client)client).ID.ToString();
                    }
                }
                if (!string.IsNullOrWhiteSpace(sClientCondition))
                {
                    sClientCondition = $"ClientID IN ({sClientCondition})";
                }
                //обработка условия "Менеджер/дизайнер"
                //обработка условия "Менеджер"
                foreach (object manager in ManagersListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)ManagersListBox.ItemContainerGenerator.ContainerFromItem(manager);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        sManagerCondition += string.IsNullOrWhiteSpace(sManagerCondition) ? "" : ", ";
                        sManagerCondition += ((User)manager).ID.ToString();
                    }
                }
                if (!string.IsNullOrWhiteSpace(sManagerCondition))
                {
                    sManagerCondition = $"ManagerID IN ({sManagerCondition})";
                }
                //foreach (object designer in DesignersListBox.Items)
                //{
                //    ListBoxItem listBoxitem = (ListBoxItem)DesignersListBox.ItemContainerGenerator.ContainerFromItem(designer);
                //    if (listBoxitem != null && listBoxitem.IsSelected)
                //    {
                //        sDesignerCondition += string.IsNullOrWhiteSpace(sDesignerCondition) ? "" : ", ";
                //        sDesignerCondition += ((User)designer).ID.ToString();
                //    }
                //}
                //if (!string.IsNullOrWhiteSpace(sDesignerCondition))
                //{
                //    sDesignerCondition = $"DesignerID IN ({sDesignerCondition})";
                //}
                //обработка условия "Состояние/признак оплаты"
                foreach (object state in StateListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)StateListBox.ItemContainerGenerator.ContainerFromItem(state);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        sStateCondition += $"{state} ";
                    }
                }
                //обработка условия "Номера заказов"
                if (!string.IsNullOrWhiteSpace(OrderNumberTextBox.Text))
                {
                    string[] aNumber = OrderNumberTextBox.Text.Trim().Split();
                    for (int ind = 0; ind < aNumber.Length; ind++)
                    {
                        aNumber[ind] = $"'{aNumber[ind].PadLeft(10, '0')}'";
                    }
                    if (aNumber.Length > 0)
                    {
                        sNumberCondition = $"Number IN ({string.Join(", ", aNumber)})";
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования условия отбора", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.WhereCondition = sDateCondition;
                MainWindow.WhereCondition += string.IsNullOrWhiteSpace(sClientCondition) ? "" : string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? sClientCondition : $" AND {sClientCondition}";
                MainWindow.WhereCondition += string.IsNullOrWhiteSpace(sManagerCondition) ? "" : string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? sManagerCondition : $" AND {sManagerCondition}";
                //MainWindow.WhereCondition += string.IsNullOrWhiteSpace(sDesignerCondition) ? "" : string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? sDesignerCondition : $" AND {sDesignerCondition}";
                MainWindow.WhereStateCondition = sStateCondition;
                MainWindow.WhereCondition += string.IsNullOrWhiteSpace(sNumberCondition) ? "" : string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? sNumberCondition : $" AND {sNumberCondition}";
                MainWindow.WhereCondition = string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? "" : $"WHERE {MainWindow.WhereCondition}";
                DialogResult = true;
                Close();
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void ProductWhereCondition()
        {
            try
            {
                //обработка условия "Дата"
                string sDateName = "";
                DateTime dStartDate = new DateTime(DateTime.Today.Year, 1, 1); //01 января текущего года
                DateTime dEndDate = new DateTime(DateTime.Today.Year, 12, 31); //31 декабря текущего года
                MainWindow.dStartDate = null;
                MainWindow.dEndDate = null;
                MainWindow.WhereProductCategoryCondition.Clear();
                MainWindow.WhereProductClientCondition.Clear();
                MainWindow.WhereProductDesignerCondition.Clear();
                MainWindow.WhereProductManagerCondition.Clear();
                if ((bool)pYearDate.IsChecked) //отбор за "год"
                {
                    dStartDate = new DateTime((int)pYearUpDown.Value, 1, 1); //01 января текущего года
                    dEndDate = new DateTime((int)pYearUpDown.Value, 12, 31); //31 декабря текущего года
                }
                if ((bool)pQuarterDate.IsChecked) //отбор за "квартал"
                {
                    switch (pQuarterComboBox.SelectedIndex)
                    {
                        case 0: //1 квартал
                            dEndDate = new DateTime(DateTime.Today.Year, 3, 31);
                            break;
                        case 1: //2 квартал
                            dStartDate = new DateTime(DateTime.Today.Year, 4, 1);
                            dEndDate = new DateTime(DateTime.Today.Year, 6, 30);
                            break;
                        case 2: //3 квартал
                            dStartDate = new DateTime(DateTime.Today.Year, 7, 1);
                            dEndDate = new DateTime(DateTime.Today.Year, 9, 30);
                            break;
                        case 3: //4 квартал
                            dStartDate = new DateTime(DateTime.Today.Year, 10, 1);
                            break;
                    }
                }
                if ((bool)pMonthDate.IsChecked) //отбор за "месяц"
                {
                    List<byte> nMonth = new List<byte> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                    dStartDate = new DateTime(DateTime.Today.Year, pMonthCheckBox.SelectedIndex + 1, 1);
                    dEndDate = new DateTime(DateTime.Today.Year, pMonthCheckBox.SelectedIndex + 1, nMonth[pMonthCheckBox.SelectedIndex] + (DateTime.IsLeapYear(DateTime.Today.Year) ? 1 : 0));
                }
                if ((bool)pDayDate.IsChecked && pDayDateTime.SelectedDate.HasValue) //отбор за "день"
                {
                    dStartDate = pDayDateTime.SelectedDate.Value;
                    dEndDate = dStartDate;
                }
                if ((bool)pPeriodDate.IsChecked && pStartDate.SelectedDate.HasValue && pEndDate.SelectedDate.HasValue) //отбор за "произвольный интервал"
                {
                    dStartDate = pStartDate.SelectedDate.Value;
                    dEndDate = pEndDate.SelectedDate.Value;
                }
                if (!(bool)pNoDate.IsChecked)
                {
                    switch (pDateName.SelectedIndex)
                    {
                        case 0:
                            sDateName = "Order.DateAdmission";
                            break;
                        case 1:
                            sDateName = "DateTransferDesigner";
                            break;
                        case 2:
                            sDateName = "DateTransferApproval";
                            break;
                        case 3:
                            sDateName = "DateApproval";
                            break;
                        case 4:
                            sDateName = "DateTransferProduction";
                            break;
                        case 5:
                            sDateName = "DateManufacture";
                            break;
                        case 6:
                            sDateName = "DateShipment";
                            break;
                    }
                    if (pDateName.SelectedIndex > 0) //для дат изделия. Дата заказа обрабатывается в MainWindow
                    {
                        sDateCondition = $"{sDateName} >= '{dStartDate.Date}' AND {sDateName} <= '{dEndDate.Date}'";
                    }
                    else if (pDateName.SelectedIndex == 0)
                    {
                        MainWindow.dStartDate = dStartDate.Date;
                        MainWindow.dEndDate = dEndDate.Date;
                    }
                }
                //обработка условия "Категория" 
                foreach (object categoryOfProduct in CategoryOfProductsListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)CategoryOfProductsListBox.ItemContainerGenerator.ContainerFromItem(categoryOfProduct);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        LCategoryCondition.Add(((CategoryOfProduct)categoryOfProduct).ID.ToString());
                    }
                }
                MainWindow.WhereProductCategoryCondition = LCategoryCondition;
                //обработка условия "Клиент"
                foreach (object client in pClientsListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)pClientsListBox.ItemContainerGenerator.ContainerFromItem(client);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        LClientCondition.Add(((Client)client).ID.ToString());
                    }
                }
                MainWindow.WhereProductClientCondition = LClientCondition;
                //обработка условия "Менеджер/дизайнер"
                foreach (object manager in pManagersListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)pManagersListBox.ItemContainerGenerator.ContainerFromItem(manager);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        LManagerCondition.Add(((User)manager).ID.ToString());
                    }
                }
                MainWindow.WhereProductManagerCondition = LManagerCondition;
                foreach (object designer in pDesignersListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)pDesignersListBox.ItemContainerGenerator.ContainerFromItem(designer);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        LDesignerCondition.Add(((User)designer).ID.ToString());
                    }
                }
                MainWindow.WhereProductDesignerCondition = LDesignerCondition;
                //обработка условия "Состояние"
                foreach (object state in pStateListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)pStateListBox.ItemContainerGenerator.ContainerFromItem(state);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        sStateCondition += $"{state} ";
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования условия отбора", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.WhereCondition = sDateCondition;
                MainWindow.WhereStateCondition = sStateCondition;
                MainWindow.WhereCondition = string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? "" : $"WHERE {MainWindow.WhereCondition}";
                DialogResult = true;
                Close();
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void ProductionProductWhereCondition()
        {
            try
            {
                MainWindow.WhereTypeOfActivityCondition.Clear();

                //обработка условия "КВД" 
                foreach (object typeOfActivity in TypeOfActivityListBox.Items)
                {
                    ListBoxItem listBoxitem = (ListBoxItem)TypeOfActivityListBox.ItemContainerGenerator.ContainerFromItem(typeOfActivity);
                    if (listBoxitem != null && listBoxitem.IsSelected)
                    {
                        MainWindow.WhereTypeOfActivityCondition.Add(((TypeOfActivity)typeOfActivity).ID);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка формирования условия отбора", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.WhereCondition = string.IsNullOrWhiteSpace(MainWindow.WhereCondition) ? "" : $"WHERE {MainWindow.WhereCondition}";
                DialogResult = true;
                Close();
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is RepeatButton || e.OriginalSource is Xceed.Wpf.Toolkit.WatermarkTextBox)
            {
                if (OrdersTabItem.IsSelected)
                {
                    oYearDate.IsChecked = true;
                }
                else
                {
                    pYearDate.IsChecked = true;
                }
                return;
            }
            if (e.OriginalSource is CalendarDayButton && sender is DatePicker datePicker)
            {
                if (datePicker.Name == "oDayDateTime")
                {
                    oDayDate.IsChecked = true;
                }
                if (datePicker.Name == "oStartDate" || datePicker.Name == "oEndDate")
                {
                    oPeriodDate.IsChecked = true;
                }
                return;
            }
            if (e.OriginalSource is ComboBox comboBox)
            {
                if (comboBox.Name == "oQuarterComboBox")
                {
                    oQuarterDate.IsChecked = true;
                }
                if (comboBox.Name == "oMonthCheckBox")
                {
                    oMonthDate.IsChecked = true;
                }
                return;
            }
        }

        private void TextToFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            clientsDataSource.View.Refresh();
        }

        private void ClientsCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is Client client)
            {
                if (OrdersTabItem.IsSelected)
                {
                    if (!string.IsNullOrEmpty(oTextToFilter.Text))
                    {
                        if (!client.Name.ToLower().Contains(oTextToFilter.Text.ToLower()))
                        {
                            e.Accepted = false;
                        }
                    }
                }
                else if (ProductsTabItem.IsSelected)
                {
                    if (!string.IsNullOrEmpty(pTextToFilter.Text))
                    {
                        if (!client.Name.ToLower().Contains(pTextToFilter.Text.ToLower()))
                        {
                            e.Accepted = false;
                        }
                    }
                }
            }
        }

    }
}

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
        private CollectionViewSource reportsViewSource;
        private App.AppDbContext _context;

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
                reportsViewSource.Source = _context.Reports.AsNoTracking().ToList();
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
            if (e.OriginalSource is Button btn)
            {
                if (btn == MakeReportButton && reportsViewSource.View.CurrentItem is Report report)
                {
                    DateTime dBeginPeriod = new DateTime(DateTime.Today.Year, 1, 1); //01 января текущего года
                    DateTime dEndPeriod = new DateTime(DateTime.Today.Year, 12, 31); //31 декабря текущего года
                    try
                    {
                        if (PeriodGroupBox.IsEnabled) //обработка условия "Дата"
                        {
                            if ((bool)YearDate.IsChecked) //отбор за "год"
                            {
                                dBeginPeriod = new DateTime((int)YearUpDown.Value, 1, 1); //01 января текущего года
                                dEndPeriod = new DateTime((int)YearUpDown.Value, 12, 31); //31 декабря текущего года
                            }
                            if ((bool)QuarterDate.IsChecked) //отбор за "квартал"
                            {
                                switch (QuarterComboBox.SelectedIndex)
                                {
                                    case 0: //1 квартал
                                        dEndPeriod = new DateTime(DateTime.Today.Year, 3, 31);
                                        break;
                                    case 1: //2 квартал
                                        dBeginPeriod = new DateTime(DateTime.Today.Year, 4, 1);
                                        dEndPeriod = new DateTime(DateTime.Today.Year, 6, 30);
                                        break;
                                    case 2: //3 квартал
                                        dBeginPeriod = new DateTime(DateTime.Today.Year, 7, 1);
                                        dEndPeriod = new DateTime(DateTime.Today.Year, 9, 30);
                                        break;
                                    case 3: //4 квартал
                                        dBeginPeriod = new DateTime(DateTime.Today.Year, 10, 1);
                                        break;
                                }
                            }
                            if ((bool)MonthDate.IsChecked) //отбор за "месяц"
                            {
                                List<byte> nMonth = new List<byte> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                                dBeginPeriod = new DateTime(DateTime.Today.Year, MonthCheckBox.SelectedIndex + 1, 1);
                                dEndPeriod = new DateTime(DateTime.Today.Year, MonthCheckBox.SelectedIndex + 1, nMonth[MonthCheckBox.SelectedIndex] + (DateTime.IsLeapYear(DateTime.Today.Year) ? 1 : 0));
                            }
                            if ((bool)DayDate.IsChecked && DayDateTime.SelectedDate.HasValue) //отбор за "день"
                            {
                                dBeginPeriod = DayDateTime.SelectedDate.Value;
                                dEndPeriod = dBeginPeriod;
                            }
                            if ((bool)PeriodDate.IsChecked && StartDate.SelectedDate.HasValue && EndDate.SelectedDate.HasValue) //отбор за "произвольный интервал"
                            {
                                dBeginPeriod = StartDate.SelectedDate.Value;
                                dEndPeriod = EndDate.SelectedDate.Value;
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
                        MakeReport(ref report, dBeginPeriod, dEndPeriod);
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

        private void MakeReport(ref Report report, DateTime beginPeriod, DateTime endPeriod)
        {
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
                        if (!lCanMakeReport) //ничего не отобрано
                        {
                            _ = MessageBox.Show("Нет данных за указанный период!", "Получение данных для отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else if (report.Code == "MBTD" || report.Code == "PSFD") //mastered by the designer | payment statement for designers
                    {
                        Reports.ReportMode = report.Code;
                        Reports.ProductCostDataSet = _report.ProductCosts
                            .Include(pc => pc.Product)
                            .ThenInclude(product => product.Designer)
                            .Include(pc => pc.Product.Order)
                            .Include(pc => pc.Product.ProductType)
                            .Where(pc => pc.TypeOfActivity.Code.Trim() == "10" && pc.Product.DesignerID != null)
                            .OrderBy(pc => pc.Product.DesignerID)
                            .ToList();
                        Reports.BeginPeriod = beginPeriod;
                        Reports.EndPeriod = endPeriod;
                        lCanMakeReport = Reports.ProductCostDataSet.Count > 0;
                        if (!lCanMakeReport) //ничего не отобрано
                        {
                            _ = MessageBox.Show("Нет данных за указанный период!", "Получение данных для отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    if (lCanMakeReport)
                    {
                        Reports.RunReport();
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
            if (report != null && (string)parameter == "P" && report.Parameters.Contains("P")) //P - Period
            {
                return true;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }
}

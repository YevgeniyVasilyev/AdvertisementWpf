﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AdvertisementWpf.Models;
using System.IO.Compression;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Input;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FastReport.Utils;

namespace AdvertisementWpf
{

    public class NotNullAndEmptyValidationGroupRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                BindingGroup bg = value as BindingGroup;
                if (bg.Items[0].GetType().Name == "User")
                {
                    User user = bg.Items[0] as User;
                    if (string.IsNullOrWhiteSpace(user.FirstName))
                    {
                        return new ValidationResult(false, "Значение поля ФАМИЛИЯ не может быть пустым!");
                    }
                    if (string.IsNullOrWhiteSpace(user.LoginName))
                    {
                        return new ValidationResult(false, "Значение поля ЛОГИН не может быть пустым!");
                    }
                    if (user.RoleID <= 0)
                    {
                        return new ValidationResult(false, "Значение поля РОЛЬ не может быть пустым!");
                    }
                    if (user.CategoryWork < 0)
                    {
                        return new ValidationResult(false, "Значение поля КАТЕГОРИЯ не может быть пустым!");
                    }
                }
                if (bg.Items[0].GetType().Name == "Client")
                {
                    Client client = bg.Items[0] as Client;
                    if (string.IsNullOrWhiteSpace(client.ShortName))
                    {
                        return new ValidationResult(false, "Значение поля НАИМЕНОВАНИЕ не может быть пустым!");
                    }
                    //if (client.UserID == null || client.UserID <= 0)
                    //{
                    //    return new ValidationResult(false, "Значение поля МЕНЕДЖЕР не может быть пустым!");
                    //}
                    //if (string.IsNullOrWhiteSpace(client.INN) || (client.INN.Trim().Length < 12 && client.IsIndividual))
                    //{
                    //    return new ValidationResult(false, "Значение поля ИНН не может быть пустым или менее 12 знаков!");
                    //}
                    //if (string.IsNullOrWhiteSpace(client.INN) || (client.INN.Trim().Length < 10 && !client.IsIndividual))
                    //{
                    //    return new ValidationResult(false, "Значение поля ИНН не может быть пустым или менее 10 знаков!");
                    //}
                }
                if (bg.Items[0].GetType().Name == "Contractor")
                {
                    Contractor contractor = bg.Items[0] as Contractor;
                    if (string.IsNullOrWhiteSpace(contractor.Name))
                    {
                        return new ValidationResult(false, "Значение поля НАИМЕНОВАНИЕ не может быть пустым!");
                    }
                    if (string.IsNullOrWhiteSpace(contractor.INN) || contractor.INN.Length < 10)
                    {
                        return new ValidationResult(false, "Значение поля ИНН не может быть пустым или менее 10 знаков!");
                    }
                }
                if (bg.Items[0].GetType().Name == "ParameterInProductType")
                {
                    ParameterInProductType pinpt = bg.Items[0] as ParameterInProductType;
                    if (string.IsNullOrWhiteSpace(pinpt.Name))
                    {
                        return new ValidationResult(false, "Значение поля НАИМЕНОВАНИЕ ПАРАМЕТРА не может быть пустым!");
                    }
                }
                return new ValidationResult(true, null);
            }
            catch
            {
                return new ValidationResult(false, "NotNullAndEmptyValidationGroupRule: Ошибка проверки данных!");
            }
        }
    }

    public class NotNullAndEmptyValidationRule : ValidationRule
    {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                return string.IsNullOrEmpty((string)value)
                    ? new ValidationResult(false, "Значение не может быть пустым!")
                    : new ValidationResult(true, null);
            }
            catch
            {
                return new ValidationResult(false, "NotNullAndEmptyValidationRule: Ошибка проверки данных!");
            }
        }
    }

    public class CostValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                if (((string)value).Trim().Length > 0)
                {
                    if (!decimal.TryParse((string)value, NumberStyles.Any, cultureInfo, out decimal cost))
                    {
                        return new ValidationResult(false, "Значение не является корректной суммой!");
                    }
                }
                return new ValidationResult(true, null);
            }
            catch
            {
                return new ValidationResult(false, "CostValidationRule: Ошибка проверки данных!");
            }
        }
    }

    public static class ValidationChecker
    {
        public static bool HasInvalidRows(DataGrid datagrid)
        {
            bool valid = true;
            foreach (object item in datagrid.ItemContainerGenerator.Items)
            {
                DependencyObject evaluateItem = datagrid.ItemContainerGenerator.ContainerFromItem(item);
                if (evaluateItem == null) continue; //null объекты пропустить
                if (!(evaluateItem is DataGridRow dgr)) continue; //если это не строка таблицы,то пропустить
                                                                  //dgr.BindingGroup.CommitEdit();
                valid &= !Validation.GetHasError(evaluateItem);
            }
            return !valid;
        }
        public static bool HasInvalidTextBox(DependencyObject obj)
        {
            bool valid = true;
            foreach (object item in LogicalTreeHelper.GetChildren(obj))
            {
                if (item is TextBox evaluateItem)
                {
                    valid &= !Validation.GetHasError(evaluateItem);
                    continue;
                }
                if (item is StackPanel evalItem)
                {
                    valid &= !HasInvalidTextBox(evalItem);
                    continue;
                }
                if (item is WrapPanel evaItem)
                {
                    valid &= !HasInvalidTextBox(evaItem);
                    continue;
                }
                if (item is Grid evaluateItem1)
                {
                    valid &= !HasInvalidTextBox(evaluateItem1);
                    continue;
                }
            }
            return !valid;
        }
    }

    [ValueConversion(typeof(decimal), typeof(string))]
    public class CostConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal cost = (decimal)value;
            return cost.ToString("C", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string cost = value.ToString();
            return decimal.TryParse(cost, NumberStyles.Any, culture, out decimal result) ? result : value;
        }
    }

    public class RegexUtilities
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                // Examines the domain part of the email and normalizes it.
                static string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    IdnMapping idn = new IdnMapping();
                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);
                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            try
            {
                return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }

    public class CreateArchive
    {
        private string SourcePath;
        private string DestinationPath;

        public CreateArchive(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        public void Start()
        {
            MainWindow.statusBar.ActivateProgressBar(IsIndeterminate: true, sTextNear: "Создание архива ...");
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) //вызывается при необходимости обновить интерфейс
        {
            (sender as BackgroundWorker).ReportProgress(0, 0);
            ZipFile.CreateFromDirectory(SourcePath, DestinationPath, CompressionLevel.Optimal, false); //СДЕЛАТЬ ОТРАБОТКУ ОШИБОК И В WorkerCompleted ПЕРЕДАВАТЬ СТАТУС!!!
                                                                                                       // через e.Result = result;
                                                                                                       //System.Threading.Thread.Sleep(1);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _ = MessageBox.Show("Создание архива завершено!", "Создание архива");
            MainWindow.statusBar.DeActivateProgressBar();
        }
    }

    public static class OrderProductStates
    {
        public static List<string> productStates = new List<string> { "Не определено", "Отгружено", "Запланирована отгрузка", "В производстве", "Подготовка", "Утвержден макет", "Передано на утверждение", "В разработке" };
        //public static List<string> productStates = new List<string> { "Не определено", "Отгружено", "Запланирована отгрузка", "Подготовка", "Утвержден макет", "Передано на утверждение", "В разработке" };
        public static List<string> orderStates = new List<string> { "Оформление", "В производстве", "Не отгружен", "Частично отгружен", "Отгружен" };

        public static string GetProductState(byte nIndex = 0)
        {
            return productStates[nIndex];
        }
        public static List<string> GetProductListState()
        {
            return productStates;
        }

        public static string GetOrderState(byte nIndex = 0)
        {
            return orderStates[nIndex];
        }
        public static List<string> GetOrderListState()
        {
            return orderStates;
        }
    }

    public class RootAppConfigObject
    {
        public Database DataBase { get; set; }
        public Listview[] LstView { get; set; }

        public RootAppConfigObject()
        {
            DataBase = new Database();
            LstView = new Listview[] { };
        }
    }

    public class Database
    {
        public string UserName { get; set; }
        public string ServerName { get; set; }
        public string[] BaseAliases { get; set; }
        public string[] BaseNames { get; set; }
    }

    public class Listview
    {
        public string ListViewName { get; set; }
        public ListviewColumn[] ListViewColumns { get; set; }
    }

    public class ListviewColumn
    {
        public double ColumnWidth { get; set; }
        public string ColumnHeader { get; set; }
    }

    public static class AppConfig
    {
        private static readonly string fileName = "appsetting.json";

        public static RootAppConfigObject DeSerialize()
        {
            string jsonString = File.ReadAllText(fileName);
            RootAppConfigObject rootAppConfigObject = JsonSerializer.Deserialize<RootAppConfigObject>(jsonString);
            return rootAppConfigObject;
        }

        public static void Serialize(object objectToSerialize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(objectToSerialize, options);
            File.WriteAllText(fileName, jsonString);
        }
    }

    public static class OpenFileInOSShell
    {
        public static void OpenFile(string fileName)
        {
            try
            {
                if (HasRegisteredFileExstension(Path.GetExtension(fileName)))
                {
                    _ = Process.Start(new ProcessStartInfo { FileName = fileName, UseShellExecute = true });
                }
                else
                {
                    _ = MessageBox.Show("Программы для указанного расширения файла в ОС не зарегистрировано!",
                        "Ошибка открытия файла во внешней программе", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка открытия файла во внешней программе", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Проверяем есть ли в ОС программа, которая может открыть файл с указанным расширением.
        private static bool HasRegisteredFileExstension(string fileExstension)
        {
            RegistryKey rkRoot = Registry.ClassesRoot;
            RegistryKey rkFileType = rkRoot.OpenSubKey(fileExstension);

            return rkFileType != null;
        }
    }

    public static class Reports
    {
        public static string ReportMode = "";
        public static string ReportFileName = "";
        public static List<Order> OrderDataSet = null;
        public static List<Account> AccountDataSet = null;
        public static List<Act> ActDataSet = null;
        public static List<Product> ProductDataSet = null;
        public static List<object> ObjectDataSet = null;
        public static List<TechCard> TechCardDataSet = null;
        public static List<WorkInTechCard> WorkInTechCardDataSet = null;
        public static List<ProductCost> ProductCostDataSet = null;
        public static List<Payment> PaymentsDataSet = null;
        public static List<Client> ClientsDataSet = null;
        public static List<IGrouping<Order, Product>> OrderProductDataSet = null;
        public static DateTime? ReportDate = DateTime.Now;
        public static DateTime BeginPeriod = DateTime.Now;
        public static DateTime EndPeriod = DateTime.Now;
        public static bool WithSignature = true;
        public static long designerID = 0;
        public static string AmountInWords = "";
        public static string NumberInWords = "";
        public static string ReportDateInWords = "";
        public static string MonthInWords = "";
        public static string CargoReleasePostName = "";
        public static string CargoReleaseName = "";
        public static List<string> FreeValue = null;
        public static string FreeString = "";
        private static string ReadyReportFileName = "";

        public static void RunReport()
        {
            FastReport.Report report = new FastReport.Report();
            try
            {
                MainWindow.statusBar.WriteStatus("Подготовка отчета ...", Cursors.Wait);
                report.AutoFillDataSet = true;
                if (ReportMode == "OrderForm")
                {
                    //report.Dictionary.RegisterBusinessObject(OrderDataSet, "Order", 4, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(OrderDataSet, "Order", 3);
                    report.SetParameterValue("DesignerID", designerID);
                    ReadyReportFileName = "Order.pdf";
                }
                else if (ReportMode == "AccountForm")
                {
                    //report.Dictionary.RegisterBusinessObject(AccountDataSet, "Account", 4, true);
                    //report.Save(ReportFileName + "_data");
                    report.Load(ReportFileName);
                    report.RegisterData(AccountDataSet, "Account", 3);
                    report.SetParameterValue("ReportDate", ReportDate);
                    report.SetParameterValue("WithSignature", WithSignature);
                    report.SetParameterValue("AmountInWords", AmountInWords);
                    ReadyReportFileName = "Account.pdf";
                }
                else if (ReportMode == "ActForm")
                {
                    //report.Dictionary.RegisterBusinessObject(ActDataSet, "Act", 4, true);
                    //report.Save(ReportFileName + "_data");
                    report.Load(ReportFileName);
                    report.RegisterData(ActDataSet, "Act", 3);
                    report.SetParameterValue("ReportDate", ReportDate);
                    report.SetParameterValue("AmountInWords", AmountInWords);
                    report.SetParameterValue("WithSignature", WithSignature);
                    ReadyReportFileName = "Act.pdf";
                }
                else if (ReportMode == "SFForm")
                {
                    report.Load(ReportFileName);
                    report.RegisterData(ActDataSet, "Act", 3);
                    report.SetParameterValue("ReportDate", ReportDate);
                    report.SetParameterValue("DateInWords", ReportDateInWords);
                    report.SetParameterValue("AmountInWords", AmountInWords);
                    report.SetParameterValue("WithSignature", WithSignature);
                    ReadyReportFileName = "SF.pdf";
                }
                else if (ReportMode == "TNForm")
                {
                    report.Load(ReportFileName);
                    report.RegisterData(ActDataSet, "Act", 3);
                    report.SetParameterValue("ReportDate", ReportDate);
                    report.SetParameterValue("AmountInWords", AmountInWords);
                    report.SetParameterValue("NumberInWords", NumberInWords);
                    report.SetParameterValue("CargoReleasePostName", CargoReleasePostName);
                    report.SetParameterValue("CargoReleaseName", CargoReleaseName);
                    report.SetParameterValue("freeString1", FreeValue[0]);
                    report.SetParameterValue("freeString2", FreeValue[1]);
                    report.SetParameterValue("DateInWords", ReportDateInWords);
                    report.SetParameterValue("WithSignature", WithSignature);
                    ReadyReportFileName = "TN.pdf";
                }
                else if (ReportMode == "UPDForm")
                {
                    report.Load(ReportFileName);
                    report.RegisterData(ActDataSet, "Act", 3);
                    report.SetParameterValue("ReportDate", ReportDate);
                    report.SetParameterValue("AmountInWords", AmountInWords);
                    report.SetParameterValue("DateInWords", ReportDateInWords);
                    report.SetParameterValue("MonthInWords", MonthInWords);
                    report.SetParameterValue("CargoReleasePostName", CargoReleasePostName);
                    report.SetParameterValue("CargoReleaseName", CargoReleaseName);
                    report.SetParameterValue("WithSignature", WithSignature);
                    ReadyReportFileName = "UPD.pdf";
                }
                else if (ReportMode == "OrderListViewForm")
                {
                    report.Load(ReportFileName);
                    report.RegisterData(OrderDataSet, "Order", 3);
                    ReadyReportFileName = "OrderListView.pdf";
                }
                else if (ReportMode == "ProductListViewForm")
                {
                    //report.Dictionary.RegisterBusinessObject(ProductDataSet, "Product", 4, true);
                    //report.Save("ProductListView_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(ProductDataSet, "Product", 3);
                    ReadyReportFileName = "ProductListView.pdf";
                }
                else if (ReportMode == "ProductionProductListViewForm")
                {
                    //report.Dictionary.RegisterBusinessObject(WorkInTechCardDataSet, "WorkInTechCard", 5, true);
                    //report.Save("ProductionProductListView_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(WorkInTechCardDataSet, "WorkInTechCard", 5);
                    ReadyReportFileName = "ProductionProductListView.pdf";
                }
                else if (ReportMode == "TechCard")
                {
                    //report.Dictionary.RegisterBusinessObject(TechCardDataSet, "TechCard", 6, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(TechCardDataSet, "TechCard");
                    ReadyReportFileName = "TechCard.pdf";
                }
                else if (ReportMode == "VMP")
                {
                    //report.Dictionary.RegisterBusinessObject(ObjectDataSet, "dataset", 4, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(ObjectDataSet, "dataset", 3);
                    report.SetParameterValue("BeginPeriod", BeginPeriod);
                    report.SetParameterValue("EndPeriod", EndPeriod);
                    ReportMode = "ReportForm";
                }
                else if (ReportMode == "MBTD" || ReportMode == "PSFD")
                {
                    //report.Dictionary.RegisterBusinessObject(ProductCostDataSet, "ProductCost", 4, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(ProductCostDataSet, "ProductCost", 3);
                    report.SetParameterValue("BeginPeriod", BeginPeriod);
                    report.SetParameterValue("EndPeriod", EndPeriod);
                    ReportMode = "ReportForm";
                }
                else if (ReportMode == "RETP")
                {
                    //report.Dictionary.RegisterBusinessObject(PaymentsDataSet, "Payment", 3, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(PaymentsDataSet, "Payment", 3);
                    report.SetParameterValue("BeginPeriod", BeginPeriod);
                    report.SetParameterValue("EndPeriod", EndPeriod);
                    ReportMode = "ReportForm";
                }
                else if (ReportMode == "RORW")
                {
                    //report.Dictionary.RegisterBusinessObject(ObjectDataSet, "dataset", 4, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(ObjectDataSet, "dataset", 3);
                    report.SetParameterValue("BeginPeriod", BeginPeriod);
                    report.SetParameterValue("EndPeriod", EndPeriod);
                    ReportMode = "ReportForm";
                }
                else if (ReportMode == "RORWRP" || ReportMode == "RORTWK" || ReportMode == "RTCPINPK")
                {
                    //report.Dictionary.RegisterBusinessObject(OrderDataSet, "Order", 4, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(OrderDataSet, "Order", 3);
                    report.SetParameterValue("BeginPeriod", BeginPeriod);
                    report.SetParameterValue("EndPeriod", EndPeriod);
                    report.SetParameterValue("KVD", FreeString);
                    ReportMode = "ReportForm";
                }
                else if (ReportMode == "BCAFPD" || ReportMode == "__AR")
                {
                    //report.Dictionary.RegisterBusinessObject(ObjectDataSet, "dataset", 5, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(ObjectDataSet, "dataset", 4);
                    report.SetParameterValue("BeginPeriod", BeginPeriod);
                    report.SetParameterValue("EndPeriod", EndPeriod);
                    ReportMode = "ReportForm";
                }
                else if (ReportMode == "ClientHandBookForm")
                {
                    //report.Dictionary.RegisterBusinessObject(ClientsDataSet, "Clients", 2, true);
                    //report.Save($"{Path.GetFileNameWithoutExtension(ReportFileName)}_data.frx");
                    //return;
                    report.Load(ReportFileName);
                    report.RegisterData(ClientsDataSet, "Clients", 2);
                    ReportMode = "ReportForm";
                }
                else
                {
                    report.Load(ReportFileName);
                    report.RegisterData(OrderDataSet, "Order", 3);
                }
                Config.ReportSettings.ShowProgress = true;
                Config.PreviewSettings.ShowInTaskbar = true;
                Res.LoadLocale(@"Localization\Russian.frl");
                //report.Prepare();
                report.Show();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                report?.Dispose();
                MainWindow.statusBar.ClearStatus();
            }
        }
    }

    public static class InWords
    {
        //cDigitInWords
        static readonly string[][] sDigitInWords = {
            new string[] {""},
            new string[] {"", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять", "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцть"},
            new string[] { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто"},
            new string[] { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"}
        };
        //_cDigitInWords
        static readonly string[] _sDigitInWords = { "", "одна", "две" };
        //cLowWords
        static readonly string[] sLowWords = {
            "копейка", //младшая часть валюты с окончанием на 1 
            "копейки", //младшая часть валюты с окончанием на 2-4
            "копеек"  //младшая часть валюты с окончанием на 0,5 и выше
        }; //названия младших частей валюты
        //cHighWords
        static readonly string[][] sHighWords = {
            new string[] {""},
            new string[] { "рубль", "рубля", "рублей", "0"},
            new string[] { "тысяча", "тысячи", "тысяч", "1"},
            new string[] {"миллион", "миллиона", "миллионов", "0"},
            new string[] { "миллиард", "миллиарда", "миллиардов", "0"}
        }; //названия старших частей валюты

        public static string Number(decimal nNumber)
        {
            string sNumberInWords;
            try
            {
                bool lCurrencyInWord = false;
                sNumberInWords = AmountInWords(ref nNumber, ref lCurrencyInWord);
            }
            catch
            {
                sNumberInWords = "Ошибка получения числа прописью";
            }
            return sNumberInWords;
        }

        public static string Amount(decimal nAmount, bool lCurrencyInWord = true)
        {
            string sAmountInWords;
            try
            {
                sAmountInWords = AmountInWords(ref nAmount, ref lCurrencyInWord);
            }
            catch
            {
                sAmountInWords = "Ошибка получения суммы прописью";
            }
            return sAmountInWords;
        }

        private static string AmountInWords(ref decimal nAmount, ref bool lCurrencyInWord)
        {
            //сумма прописью
            string sSumInWords = "", sSum, sTemp, sSumOfLow;
            decimal nTemp,
                    nSumOfTriad,   //сумма очередной триады
                    nSumOfLow,     //сумма младшей части валюты
                    nSumOfHigh;    //сумма старшей части валюты
            ushort nNumberOfTriad,
                   nCountOfTriad; //кол-во триад в числе       
            sSum = nAmount.ToString(); //перевод в строку

            NumberFormatInfo nfi = new CultureInfo("ru-RU", false).NumberFormat;
            if (sSum.Contains(nfi.NumberDecimalSeparator)) //есть копейки
            {
                nSumOfHigh = Convert.ToDecimal(sSum.Substring(0, sSum.IndexOf(nfi.NumberDecimalSeparator))); //получить рубли
                sTemp = sSum.Substring(sSum.IndexOf(nfi.NumberDecimalSeparator) + 1, 2); //получить копейки
                nSumOfLow = Convert.ToDecimal(sTemp.PadLeft(2, '0'));
            }
            else
            {
                nSumOfHigh = Convert.ToDecimal(sSum); //получить рубли
                nSumOfLow = 0; //получить копейки
            }
            if (nSumOfHigh.ToString().Length % 3 != 0)
            {
                nCountOfTriad = (ushort)(Math.Floor((double)nSumOfHigh.ToString().Length / 3) + 1);
            }
            else
            {
                nCountOfTriad = (ushort)(nSumOfHigh.ToString().Length / 3);
            }
            if (nSumOfHigh == 0)
            {
                if (lCurrencyInWord)
                {
                    sSumInWords = $"Ноль {sHighWords[1][EndOfTriad(ref nSumOfHigh)]} ";
                }
                else
                {
                    sSumInWords = " ";
                }
            }
            else
            {
                if ((nCountOfTriad <= sHighWords.Length) && (nAmount > 0))
                {
                    for (nNumberOfTriad = nCountOfTriad; nNumberOfTriad >= 1; nNumberOfTriad--)
                    {
                        nSumOfTriad = nSumOfHigh % (decimal)(Math.Pow(1000, nNumberOfTriad));
                        nSumOfTriad = Math.Floor(nSumOfTriad / (decimal)Math.Pow(1000, nNumberOfTriad - 1));
                        nTemp = nSumOfTriad;
                        if (nTemp > 0) sSumInWords += TriadaInWords(ref nSumOfTriad, ref nNumberOfTriad) + " ";
                        if ((nTemp > 0) || (nNumberOfTriad == 1))
                        {
                            if (nNumberOfTriad == 1) sSumInWords += (lCurrencyInWord ? sHighWords[nNumberOfTriad][EndOfTriad(ref nSumOfTriad)] : "") + " ";
                            else sSumInWords += sHighWords[nNumberOfTriad][EndOfTriad(ref nSumOfTriad)] + " ";
                        }
                    }
                }
                else
                {
                    if (nCountOfTriad > sHighWords.Length) return "Очень большая сумма";
                    if (nAmount < 0) return "Где вы видели знак минус на деньгах ?";
                }
            }
            if (lCurrencyInWord) //nSumOfLow > 0 &&
            {
                sSumOfLow = nSumOfLow.ToString();
                if (nSumOfLow < 10) sSumOfLow = $"0{sSumOfLow}";
                sSumInWords = $"{sSumInWords}{sSumOfLow} ";
                if (nSumOfLow < 20) sSumInWords += sLowWords[EndOfTriad(ref nSumOfLow)];
                else
                {
                    nSumOfLow %= 10;
                    sSumInWords += sLowWords[EndOfTriad(ref nSumOfLow)];
                }
            }
            return sSumInWords.Substring(0, 1).ToUpper() + sSumInWords.Substring(1).Trim();
        }

        private static string TriadaInWords(ref decimal nSumOfTriad, ref ushort nNumberOfTriad)
        {
            //триада прописью
            string sTriadaInWords = "";
            if (nSumOfTriad > 99)
            {
                sTriadaInWords += sDigitInWords[3][(int)Math.Floor(nSumOfTriad / 100)];
                nSumOfTriad %= 100;
            }
            if (nSumOfTriad > 19)
            {
                sTriadaInWords += (!string.IsNullOrWhiteSpace(sTriadaInWords) ? " " : "") + sDigitInWords[2][(int)Math.Floor(nSumOfTriad / 10)];
                nSumOfTriad %= 10;
            }
            if (Convert.ToInt16(sHighWords[nNumberOfTriad][3]) != 0 && (nSumOfTriad < 3) && (nSumOfTriad > 0))
            {
                sTriadaInWords += (!string.IsNullOrWhiteSpace(sTriadaInWords) ? " " : "") + _sDigitInWords[(int)nSumOfTriad];
            }
            else
            {
                if (nSumOfTriad > 0)
                {
                    sTriadaInWords += (!string.IsNullOrWhiteSpace(sTriadaInWords) ? " " : "") + sDigitInWords[1][(int)nSumOfTriad];
                }
            }
            return sTriadaInWords;
        }

        private static ushort EndOfTriad(ref decimal nSumOfTriad) //окончание триады
        {
            if (nSumOfTriad == 1)
            {
                return 0;
            }
            else
            {
                return (nSumOfTriad >= 2) && (nSumOfTriad <= 4) ? (ushort)1 : (ushort)2;
            }
        }

        public static string Month(DateTime? dateTime)
        {
            string[] sMonthName = { "", "января", "Февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            return dateTime == null ? "" : sMonthName[((DateTime)dateTime).Month];
        }

        public static string Date(DateTime? dateTime)
        {
            return dateTime == null ? "" : $"{((DateTime)dateTime).Day} {Month(dateTime)} {((DateTime)dateTime).Year} года";
        }

        public static string MonthName(DateTime dateTime)
        {
            string[] sMonthName = { "", "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            return sMonthName[dateTime.Month];
        }

    }

    public static class PrintControl
    {
        private static IEnumerator enumerator;

        public static void OrderListView(ref ListView listView)
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                MainWindow.statusBar.WriteStatus("Подготовка списка ...", Cursors.Wait);
                string viewFilename = "OrderListView.frx";
                string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                enumerator = listView.ItemsSource.GetEnumerator();
                List<Order> list = new List<Order> { };
                while (enumerator.MoveNext())
                {
                    list.Add((Order)enumerator.Current);
                }
                MainWindow.statusBar.WriteStatus("Вывод списка ...", Cursors.Wait);
                if (File.Exists(Path.Combine(_pathToReportTemplate, viewFilename)))
                {
                    Reports.ReportFileName = Path.Combine(_pathToReportTemplate, viewFilename); ;
                    Reports.OrderDataSet = list;
                    Reports.ReportMode = "OrderListViewForm";
                    Reports.RunReport();
                }
                else
                {
                    _ = MessageBox.Show($"Не найден файл {viewFilename} !", "Ошибка отображения списка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка отображения списка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        public static void ProductListView(ref ListView listView)
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                MainWindow.statusBar.WriteStatus("Подготовка списка ...", Cursors.Wait);
                string viewFilename = "ProductListView.frx";
                string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                enumerator = listView.ItemsSource.GetEnumerator();
                List<Product> list = new List<Product> { };
                while (enumerator.MoveNext())
                {
                    list.Add((Product)enumerator.Current);
                }
                MainWindow.statusBar.WriteStatus("Вывод списка ...", Cursors.Wait);
                if (File.Exists(Path.Combine(_pathToReportTemplate, viewFilename)))
                {
                    Reports.ReportFileName = Path.Combine(_pathToReportTemplate, viewFilename); ;
                    Reports.ProductDataSet = list;
                    Reports.ReportMode = "ProductListViewForm";
                    Reports.RunReport();
                }
                else
                {
                    _ = MessageBox.Show($"Не найден файл {viewFilename} !", "Ошибка отображения списка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка отображения списка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        public static void ProductionProductListView(ref ListView listView)
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                MainWindow.statusBar.WriteStatus("Подготовка списка ...", Cursors.Wait);
                string viewFilename = "ProductionProductListView.frx";
                string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                enumerator = listView.ItemsSource.GetEnumerator();
                List<WorkInTechCard> list = new List<WorkInTechCard> { };
                while (enumerator.MoveNext())
                {
                    list.Add((WorkInTechCard)enumerator.Current);
                }
                MainWindow.statusBar.WriteStatus("Вывод списка ...", Cursors.Wait);
                if (File.Exists(Path.Combine(_pathToReportTemplate, viewFilename)))
                {
                    Reports.ReportFileName = Path.Combine(_pathToReportTemplate, viewFilename);
                    Reports.WorkInTechCardDataSet = list;
                    Reports.ReportMode = "ProductionProductListViewForm";
                    Reports.RunReport();
                }
                else
                {
                    _ = MessageBox.Show($"Не найден файл {viewFilename} !", "Ошибка отображения списка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка отображения списка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }
    }

    public class ObservableCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type observableType = typeof(ObservableCollection<>).MakeGenericType(value.GetType().GetGenericArguments()); 
            return Activator.CreateInstance(observableType, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type hashSetType = typeof(HashSet<>).MakeGenericType(value.GetType().GetGenericArguments());
            return Activator.CreateInstance(hashSetType, value);
        }
    }

    public class TreeViewItemBase : INotifyPropertyChanged
    {
        private bool isSelected;
        [NotMapped]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        private bool isExpanded = false;
        [NotMapped]
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private bool isPrinted;
        [NotMapped]
        public bool IsPrinted
        {
            get => isPrinted;
            set
            {
                if (value != isPrinted)
                {
                    isPrinted = value;
                    NotifyPropertyChanged("IsPrinted");
                }
            }
        }

        private bool isChecked;
        [NotMapped]
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value != isChecked)
                {
                    isChecked = value;
                    NotifyPropertyChanged("IsChecked");
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class BoolNotBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class MultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AddBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool lBool = true;
            bool lReverse = (string)parameter == "True";
            foreach (object value in values)
            {
                //if (!value.GetType().FullName.Contains("MS.Internal.NamedObject"))
                //{
                //    lBool = lBool && (bool)value;
                //}
                if (value is bool boolean)
                {
                    lBool = lBool && boolean;
                }
            }
            return lReverse ? !lBool : lBool; //parameter == True -> return not lBool
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class LogicalOrBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool lBool = false;
            bool lReverse = (string)parameter == "True";
            foreach (object value in values)
            {
                if (value is bool boolean)
                {
                    lBool = lBool || boolean;
                }
            }
            return lReverse ? !lBool : lBool; //parameter == True -> return not lBool
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

#nullable enable
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;

        private readonly EventHandler _requerySuggested;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;

            _requerySuggested = (o, e) => Invalidate();
            CommandManager.RequerySuggested += _requerySuggested;
        }

        public void Invalidate()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            execute?.Invoke(parameter);
        }
    }

}

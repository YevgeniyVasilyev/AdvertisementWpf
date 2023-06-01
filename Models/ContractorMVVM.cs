using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdvertisementWpf.Models
{
    public partial class Contractor
    {
        public string ContractorInfoForAccount => $"{Name ?? ""}, ИНН {INN ?? ""}, КПП {KPP ?? ""}, {BusinessAddress ?? ""}";
        public string ContractorInfoForAct => $"{Name ?? ""}, ИНН {INN ?? ""}, {BusinessAddress ?? ""}, р/с {BankAccount ?? ""}, в банке {Bank?.Name ?? ""}, БИК {Bank?.BIK ?? ""}, к/с {Bank?.CorrAccount ?? ""}";
        public string DirectorShortName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DirectorName))
                {
                    return "";
                }
                string[] aDn = DirectorName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (aDn.Length == 3)
                {
                    return $"{aDn[0]} {aDn[1].Substring(0, 1)}. {aDn[2].Substring(0, 1)}.";
                }
                else if (aDn.Length == 2)
                {
                    return $"{aDn[0]} {aDn[1].Substring(0, 1)}.";
                }
                return DirectorName;
            }
        }
        public string ChiefAccountantShortName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ChiefAccountant))
                {
                    return "";
                }
                string[] aCa = ChiefAccountant.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (aCa.Length == 3)
                {
                    return $"{aCa[0]} {aCa[1].Substring(0, 1)}. {aCa[2].Substring(0, 1)}.";
                }
                else if (aCa.Length == 2)
                {
                    return $"{aCa[0]} {aCa[1].Substring(0, 1)}.";
                }
                return ChiefAccountant;
            }
        }

    }

    public class ContractorViewModel
    {
        RelayCommand saveCommand;
        App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
        public ObservableCollection<Contractor> Contractors { get; set; }

        public ContractorViewModel()
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Загрузка справочника подрядчиков ...", Cursors.Wait);
                _context.Contractors.Load();
                Contractors = _context.Contractors.Local.ToObservableCollection();
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
        ~ContractorViewModel()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            MainWindow.statusBar.ClearStatus();
        }
        // команда сохранения
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand((o) =>
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Сохранение данных ...", Cursors.Wait);
                _ = _context.SaveChanges();
                _ = MessageBox.Show("   Сохранено успешно!   ", "Сохранение данных");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка сохранения данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }, (o) => ValidationCheck((object[])o));

        private bool ValidationCheck(object[] obj)
        {
            bool valid = true;
            foreach (object o in obj)
            {
                if (o is DataGrid dataGrid)
                {
                    valid &= !ValidationChecker.HasInvalidRows(dataGrid);
                }
                else if (o is TextBox textBox)
                {
                    valid &= !Validation.GetHasError(textBox);
                }
            }
            //!ValidationChecker.HasInvalidRows(ContractorsGrid) && !ValidationChecker.HasInvalidTextBox(Contractor)
            return valid;
        }
    }
}

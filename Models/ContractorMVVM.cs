using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private RelayCommand saveCommand, openFileDialogCommand;
        private App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
        public ObservableCollection<Contractor> Contractors { get; set; }

        public ContractorViewModel()
        {
            try
            {
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

        //команда открытия диалога для выбора файла
        public RelayCommand OpenFileDialog => openFileDialogCommand ??= new RelayCommand((o) =>
        {
            try
            {
                string initialDirectory = _context.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                CommonOpenFileDialog dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = false,
                    InitialDirectory = initialDirectory
                };
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Contractor contractor = o as Contractor;
                    contractor.AccountFileTemplate = System.IO.Path.GetFileName(dialog.FileName);
                    //AccountFileTemplateTextBlock.Text = System.IO.Path.GetFileName(dialog.FileName);
                }
                //_ = Activate();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

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

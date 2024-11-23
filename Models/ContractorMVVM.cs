using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdvertisementWpf.Models
{
    public partial class Contractor
    {
        private string _contractorInfoForAccount;
        [NotMapped]
        public string ContractorInfoForAccount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_contractorInfoForAccount))
                {
                    _contractorInfoForAccount = $"{Name ?? ""}, ИНН {INN ?? ""}, КПП {KPP ?? ""}, {BusinessAddress ?? ""}";
                    NotifyPropertyChanged("ContractorInfoForAccount");
                }
                return _contractorInfoForAccount;
            }
            set
            {
                if (_contractorInfoForAccount != value)
                {
                    _contractorInfoForAccount = value;
                    NotifyPropertyChanged("ContractorInfoForAccount");
                }
            }
        }
        private string _contractorInfoForAct;
        [NotMapped]
        public string ContractorInfoForAct
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_contractorInfoForAct))
                {
                    _contractorInfoForAct = $"{Name ?? ""}, ИНН {INN ?? ""}, КПП {KPP ?? ""}, {BusinessAddress ?? ""}, р/с {BankAccount ?? ""}, в банке {Bank?.Name ?? ""}, БИК {Bank?.BIK ?? ""}, к/с {Bank?.CorrAccount ?? ""}";
                    NotifyPropertyChanged("ContractorInfoForAct");
                }
                return _contractorInfoForAct;
            }
            set
            {
                if (_contractorInfoForAct != value)
                {
                    _contractorInfoForAct = value;
                    NotifyPropertyChanged("ContractorInfoForAct");
                }
            }
        }
        private string _shipperInfoForSFUPD = "";
        [NotMapped]
        public string ShipperInfoForSFUPD
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_shipperInfoForSFUPD))
                {
                    _shipperInfoForSFUPD = "-";
                    NotifyPropertyChanged("ShipperInfoForSFUPD");
                }
                return _shipperInfoForSFUPD;
            }
            set
            {
                if (_shipperInfoForSFUPD != value)
                {
                    _shipperInfoForSFUPD = value;
                    NotifyPropertyChanged("ShipperInfoForSFUPD");
                }
            }
        }
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
        }, (o) => ValidationCheck((object[])o) && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ContractorsHandBookNewEdit"));

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
                    Contractor contractor = ((object[])o)[0] as Contractor;
                    string mode = ((object[])o)[1] as string;
                    if (mode.ToLower() == "account")
                    {
                        contractor.AccountFileTemplate = System.IO.Path.GetFileName(dialog.FileName);
                    }
                    else if (mode.ToLower() == "act")
                    {
                        contractor.ActFileTemplate = System.IO.Path.GetFileName(dialog.FileName);
                    }
                    else if (mode.ToLower() == "sf")
                    {
                        contractor.SFFileTemplate = System.IO.Path.GetFileName(dialog.FileName);
                    }
                    else if (mode.ToLower() == "tn")
                    {
                        contractor.TNFileTemplate = System.IO.Path.GetFileName(dialog.FileName);
                    }
                    else if (mode.ToLower() == "upd")
                    {
                        contractor.UPDFileTemplate = System.IO.Path.GetFileName(dialog.FileName);
                    }
                }
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

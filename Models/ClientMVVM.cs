using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


namespace AdvertisementWpf.Models
{
    public partial class Client
    {
        public string ClientInfoForAccount => $"{Name ?? ""}, ИНН {INN ?? ""}, КПП {KPP ?? ""}, {BusinessAddress ?? ""}, {WorkPhone ?? ""}";
        public string ClientInfoForAct => $"{Name ?? ""}, ИНН {INN ?? ""}, {BusinessAddress ?? ""}, р/с {BankAccount ?? ""}, в банке {Bank?.Name ?? ""}, БИК {Bank?.BIK ?? ""}, к/с {Bank?.CorrAccount ?? ""}";
    }

    public class ClientViewModel
    {
        private RelayCommand saveCommand;
        private App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);

        private ObservableCollection<Client> _Clients { get; set; }
        public ICollectionView Clients { get; set; }

        private string _filterString { get; set; } = "";
        public string FilterString
        {
            get => _filterString;
            set
            {
                _filterString = value;
                Clients.Refresh();
            }
        }

        public ClientViewModel()
        {
            try
            {
                MainWindow.statusBar.WriteStatus("Загрузка справочника подрядчиков ...", Cursors.Wait);
                _context.Clients.Load();
                _Clients = _context.Clients.Local.ToObservableCollection();
                Clients = CollectionViewSource.GetDefaultView(_Clients);
                Clients.Filter = ClientFilter;
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

        ~ClientViewModel()
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
        }, (o) => ValidationCheck((object[])o) && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ClientsHandBookNewEdit"));

        private bool ClientFilter(object item)
        {
            Client client = item as Client;
            //return client.Name.Contains(_filterString);
            return string.IsNullOrEmpty(_filterString) || client.Name.IndexOf(_filterString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

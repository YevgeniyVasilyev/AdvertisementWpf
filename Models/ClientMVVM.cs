using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


namespace AdvertisementWpf.Models
{
    public partial class Client : INotifyPropertyChanged
    {
        private string _clientInfoForAccount = "";
        [NotMapped]
        public string ClientInfoForAccount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_clientInfoForAccount))
                {
                    if (IsPrivate)
                    {
                        _clientInfoForAccount = $"{Name ?? ""}, ИНН {INN ?? ""}, {BusinessAddress ?? ""}";
                    }
                    else
                    {
                        _clientInfoForAccount = $"{Name ?? ""}, ИНН {INN ?? ""}, КПП {KPP ?? ""}, {BusinessAddress ?? ""}";
                    }
                    NotifyPropertyChanged("ClientInfoForAccount");
                }
                return _clientInfoForAccount;
            }
            set
            {
                if (_clientInfoForAccount != value)
                {
                    _clientInfoForAccount = value;
                    NotifyPropertyChanged("ClientInfoForAccount");
                }
            }
        }
        private string _clientInfoForAct = "";
        [NotMapped]
        public string ClientInfoForAct
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_clientInfoForAct))
                {
                    if (IsPrivate)
                    {
                        _clientInfoForAct = $"{Name ?? ""}, {BusinessAddress ?? ""}, тел. {WorkPhone ?? ""}";
                    }
                    else if (IsLegal || IsIndividual)
                    {
                        _clientInfoForAct = $"{Name ?? ""}, ИНН {INN ?? ""}, {BusinessAddress ?? ""}, тел. {WorkPhone ?? ""}, р/с {BankAccount ?? ""}, в банке {Bank?.Name ?? ""}, БИК {Bank?.BIK ?? ""}, к/с {Bank?.CorrAccount ?? ""}";
                    }
                    else if (IsBudget)
                    {
                        _clientInfoForAct = $"{Name ?? ""}, ИНН {INN ?? ""}, {BusinessAddress ?? ""}, тел. {WorkPhone ?? ""} {(PersonalAccount != null ? ", л/с " + PersonalAccount : "")}, казн.счет {BankAccount ?? ""}, в банке {Bank?.Name ?? ""}, БИК ТОФК {Bank?.BIK ?? ""}, ЕКС {Bank?.CorrAccount ?? ""}";
                    }
                    NotifyPropertyChanged("ClientInfoForAct");
                }
                return _clientInfoForAct;
            }
            set
            {
                if (_clientInfoForAct != value)
                {
                    _clientInfoForAct = value;
                    NotifyPropertyChanged("ClientInfoForAct");
                }
            }
        }
        private string _consigneeForSFUPD = "";
        [NotMapped]
        public string ConsigneeForSFUPD //грузополучатель для СФ и УПД
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_consigneeForSFUPD))
                {
                    if (ConsigneeIsSame)
                    {
                        _consigneeForSFUPD = "-";
                    }
                    else
                    {
                        _consigneeForSFUPD = $"{ConsigneeName ?? ""}, {ConsigneeBusinessAddress ?? ""}";
                    }
                    NotifyPropertyChanged("ConsigneeForSFUPD");
                }
                return _consigneeForSFUPD;
            }
            set
            {
                if (_consigneeForSFUPD != value)
                {
                    _consigneeForSFUPD = value;
                    NotifyPropertyChanged("ConsigneeForSFUPD");
                }
            }
        }
        private string _consigneeForTN = "";
        [NotMapped]
        public string ConsigneeForTN //грузополучатель для ТН
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_consigneeForTN))
                {
                    if (ConsigneeIsSame)
                    {
                        _consigneeForTN = ClientInfoForAct; //грузополучатель "он же"
                    }
                    else
                    {
                        if (IsLegal || IsIndividual)
                        {
                            _consigneeForTN = $"{ConsigneeName ?? ""}, ИНН {ConsigneeINN ?? ""}, {ConsigneeBusinessAddress ?? ""}, р/с {ConsigneeBankAccount ?? ""}, в банке {ConsigneeBank?.Name ?? ""}, БИК {ConsigneeBank?.BIK ?? ""}, к/с {ConsigneeBank?.CorrAccount ?? ""}, тел. {ConsigneeWorkPhone ?? ""}";
                        }
                        else if (IsBudget)
                        {
                            _consigneeForTN = $"{ConsigneeName ?? ""}, ИНН {ConsigneeINN ?? ""}, {ConsigneeBusinessAddress ?? ""} {(ConsigneePersonalAccount != null ? ", л/с " + ConsigneePersonalAccount : "")}, казн.счет {ConsigneeBankAccount ?? ""}, в банке {ConsigneeBank?.Name ?? ""}, БИК ТОФК {ConsigneeBank?.BIK ?? ""}, ЕКС {ConsigneeBank?.CorrAccount ?? ""}, тел. {ConsigneeWorkPhone ?? ""}";
                        }
                        else
                        {
                            _consigneeForTN = $"{ConsigneeName ?? ""}, {ConsigneeBusinessAddress ?? ""}";
                        }
                    }
                    NotifyPropertyChanged("ConsigneeForTN");
                }
                return _consigneeForTN;
            }
            set
            {
                if (_consigneeForTN != value)
                {
                    _consigneeForTN = value;
                    NotifyPropertyChanged("ConsigneeForTN");
                }
            }
        }

        public bool IsPrivate => PropertyForm.IsPrivate(FormProperty);
        public bool IsLegal => PropertyForm.IsLegal(FormProperty); 
        public bool IsIndividual => PropertyForm.IsIndividual(FormProperty);
        public bool IsBudget => PropertyForm .IsBudget(FormProperty);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class ClientViewModel
    {
        private RelayCommand saveCommand, newCommand, deleteCommand;
        public App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);

        private ObservableCollection<Client> _Clients { get; set; }
        public ICollectionView Clients { get; set; }
        public List<string> PropertyFormNames { get; set; }

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
                _context.Clients
                    .Include(Client => Client.Bank)
                    .Load();
                _Clients = _context.Clients.Local.ToObservableCollection();
                Clients = CollectionViewSource.GetDefaultView(_Clients);
                Clients.Filter = ClientFilter;
                PropertyFormNames = PropertyForm.ListPropertyFormName();
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

        public RelayCommand NewCommand => newCommand ??= new RelayCommand((o) =>
        {
            try
            {
                FilterString = ""; //очистить фильтр, иначе добавленную строку может быть не видно
                Client client = new Client
                {
                    Name = "Новый клиент",
                    ShortName = "Новый клиент",
                    UserID = MainWindow.Userdata.ID,
                    FormProperty = 1
                };
                _context.Clients.Add(client);
                _ = Clients.MoveCurrentTo(client);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка добавления данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }, null);

        public RelayCommand DeleteCommand => deleteCommand ??= new RelayCommand((o) =>
        {
            try
            {
                if (o is Client client)
                {
                    _context.Clients.Remove(client);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка удаления данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }, null);

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

    public static class PropertyForm
    {
        private static Dictionary<short, string> FormProperty = new Dictionary<short, string> { { 1, "Юридическое лицо" }, { 2, "ИП" }, { 3, "Бюджет" }, { 0, "Физическое лицо" } };
        public static List<string> ListPropertyFormName()
        {
            return FormProperty.Values.ToList();
        }
        public static string GetPropertyFormValue(byte key)
        {
            _ = FormProperty.TryGetValue(key, out string value);
            return value;
        }
        public static short GetPropertyFormKey(string valueName)
        {
            return FormProperty.FirstOrDefault(f => f.Value.Equals(valueName)).Key;
        }
        public static bool IsPrivate(byte pf) => pf == 0;     // Физическое лицо
        public static bool IsLegal(byte pf) => pf == 1;       // Юридическое лицо
        public static bool IsIndividual(byte pf) => pf == 2;  // ИП
        public static bool IsBudget(byte pf) => pf == 3;      // Бюджет
    }

    public class PropertyFormConverter : IValueConverter //конвертер нужен чтобы не зависеть от положения ключей и значений в словаре
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return PropertyForm.GetPropertyFormValue((byte)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return PropertyForm.GetPropertyFormKey((string)value);
        }
    }
}

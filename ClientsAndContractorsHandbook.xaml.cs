using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Reflection;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ClientsAndContractorsHandbook.xaml
    /// </summary>
    public partial class ClientsAndContractorsHandbook : Window
    {
        public ClientsAndContractorsHandbook()
        {
            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            try
            {
                DataContext = new ClientAndContractorViewModel();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }

            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);
            InitializeComponent();

            try
            {
                Clients_Tab.DataContext = new ClientViewModel();
                Contractor_Tab.DataContext = new ContractorViewModel();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }

        }

        private void ClientsGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            PropertyInfo userList = DataContext.GetType().GetProperty("UserList", BindingFlags.Public | BindingFlags.Instance); //получить Свойство UserList из DataContext
            ICollectionView collectionView = userList.GetValue(DataContext) as ICollectionView; //получить значение этого свойства в DataContext
            if (collectionView.MoveCurrentToFirst())
            {
                User user = collectionView.CurrentItem as User;
                Client client = e.NewItem as Client;
                client.UserID = user.ID;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _ = Owner.Activate();
            MainWindow.statusBar.ClearStatus();
        }
    }

    public class ClientAndContractorViewModel
    {
        public ICollectionView BankList { get; set; }
        public ICollectionView UserList { get; set; }
        public ObservableCollection<User> UsersList { get; set; }
        public bool ClientsTabEnabled { get; set; }

        public ClientAndContractorViewModel()
        {
            ClientsTabEnabled = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ListManager");

            using App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            {
                BankList = CollectionViewSource.GetDefaultView(_context.Banks.AsNoTracking().ToList());
                //_context.Users.AsNoTracking().Load();
                _context.Users.Load();
                UsersList = _context.Users.Local.ToObservableCollection();
                UserList = CollectionViewSource.GetDefaultView(UsersList);
            }
        }
    }
}

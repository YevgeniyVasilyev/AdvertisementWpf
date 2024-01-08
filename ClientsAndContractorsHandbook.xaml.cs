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
using System.Collections.Generic;

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
                //Clients_Tab.DataContext = new ClientViewModel();
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
            Client client = e.NewItem as Client;
            if (collectionView.MoveCurrentToFirst())
            {
                User user = collectionView.CurrentItem as User;
                client.UserID = user.ID;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _ = Owner.Activate();
            MainWindow.statusBar.ClearStatus();
        }

        private void BankComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PropertyInfo bankList = DataContext.GetType().GetProperty("BankList", BindingFlags.Public | BindingFlags.Instance); //получить Свойство UserList из DataContext
            //ICollectionView collectionView = bankList.GetValue(DataContext) as ICollectionView; //получить значение этого свойства в DataContext
            ClientViewModel clientViewModel = (ClientViewModel)Clients_Tab.DataContext; //БОЛЕЕ ПРОСТОЙ СПОСОБ ДОБРАТЬСЯ ДО DataContext
            if (sender is ComboBox && clientViewModel?.Clients?.CurrentItem is Client client && clientViewModel?._context != null)
            {
                clientViewModel._context.Entry(client).Reference(c => c.Bank).Load();
                e.Handled = true;
            }
        }
    }

    public class ClientAndContractorViewModel
    {
        public ICollectionView BankList { get; set; }
        public ICollectionView ManagerList { get; set; }
        //public ObservableCollection<User> UsersList { get; set; }
        public List<User> UsersList = new List<User> { };
        public bool ClientsHandBookNew { get; set; }
        public bool ClientsHandBookDelete { get; set; }
        public bool ContractorsHandBookNew { get; set; }
        public bool ContractorsHandBookDelete { get; set; }

        public ClientAndContractorViewModel()
        {
            ClientsHandBookNew = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ClientsHandBookNewEdit");
            ClientsHandBookDelete = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ClientsHandBookDelete");
            ContractorsHandBookNew = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ContractorsHandBookNewEdi");
            ContractorsHandBookDelete = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ContractorsHandBookDelete");
            
            using App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            {
                BankList = CollectionViewSource.GetDefaultView(_context.Banks.AsNoTracking().ToList());
                UsersList = _context.Users.AsNoTracking().ToList();
                //_context.Users.Load();
                //UsersList = (ObservableCollection<User>)_context.Users.Local.ToObservableCollection().Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListManager"));
                ManagerList = CollectionViewSource.GetDefaultView(UsersList.Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListManager")));
            }
        }
    }
}

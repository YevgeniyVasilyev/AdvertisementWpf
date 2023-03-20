using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ClientsAndContractorsHandbook.xaml
    /// </summary>
    public partial class ClientsAndContractorsHandbook : Window
    {
        private CollectionViewSource clientsViewSource, contractorsViewSource, usersViewSource, banksViewSource;
        private App.AppDbContext _context;

        public ClientsAndContractorsHandbook()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();
            Clients_Tab.Focus();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            usersViewSource = (CollectionViewSource)FindResource(nameof(usersViewSource)); //найти описание view в разметке
            clientsViewSource = (CollectionViewSource)FindResource(nameof(clientsViewSource)); //найти описание view в разметке
            contractorsViewSource = (CollectionViewSource)FindResource(nameof(contractorsViewSource)); //найти описание view в разметке
            banksViewSource = (CollectionViewSource)FindResource(nameof(banksViewSource)); //найти описание view в разметке

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                _context.Clients.Load();
                _context.Contractors.Load();
                usersViewSource.Source = _context.Users.AsNoTracking().ToList();
                banksViewSource.Source = _context.Banks.AsNoTracking().ToList();
                clientsViewSource.Source = _context.Clients.Local.ToObservableCollection();
                contractorsViewSource.Source = _context.Contractors.Local.ToObservableCollection();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex.InnerException.Message, "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void SaveClientsAndContractors(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.ToString().Contains("Button"))
                {
                    MainWindow.statusBar.WriteStatus("Сохранение данных ...", Cursors.Wait);
                    _ = _context.SaveChanges();
                    _ = MessageBox.Show("   Сохранено успешно!   ", "Сохранение данных");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex.InnerException.Message, "Ошибка сохранения данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void CanExecuteClientsAndContractors(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            //Client & Contractor is Grid name
            e.CanExecute = _context != null && !ValidationChecker.HasInvalidRows(ClientsGrid) && !ValidationChecker.HasInvalidTextBox(Client)
                && !ValidationChecker.HasInvalidRows(ContractorsGrid) && !ValidationChecker.HasInvalidTextBox(Contractor);
        }

        private void DeleteClientsAndContractors(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.ToString().Contains("Button"))
                {
                    Button btn = (Button)e.OriginalSource;
                    if (btn.Name.Contains("Clients"))
                    {
                        if (ClientsGrid.HasItems)
                        {
                            Client client = ClientsGrid.SelectedItem as Client;
                            if (client != null)
                            {
                                _ = _context.Clients.Remove(client);
                            }
                        }
                    }
                    else
                    {
                        if (ContractorsGrid.HasItems)
                        {
                            Contractor contractor = ContractorsGrid.SelectedItem as Contractor;
                            if (contractor != null)
                            {
                                _ = _context.Contractors.Remove(contractor);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex.InnerException.Message, "Ошибка удаления ПОЛЬЗОВАТЕЛЯ из таблицы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CanExecuteDeleteClientsAndContractors(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ClientsGrid.HasItems || (ContractorsGrid != null && ContractorsGrid.HasItems);
        }

        private void RefreshClientsAndContractors(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.ToString().Contains("Button"))
                {
                    Button btn = (Button)e.OriginalSource;
                    if (btn.Name.Contains("Clients"))
                    {
                        clientsViewSource.View.Refresh();
                    }
                    else
                    {
                        contractorsViewSource.View.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex.InnerException.Message, "Ошибка обновления таблицы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
            }
        }

        private void CanExecuteRefreshClientsAndContractors(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context != null && !ValidationChecker.HasInvalidRows(ClientsGrid) && !ValidationChecker.HasInvalidTextBox(Client)
                && !ValidationChecker.HasInvalidRows(ContractorsGrid) && !ValidationChecker.HasInvalidTextBox(Contractor); ;
        }

        private void ClientsGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            Client client = e.NewItem as Client;
            usersViewSource.View.MoveCurrentToFirst();
            if (usersViewSource.View.CurrentItem is User user)
            {
                client.UserID = user.ID;
            }
        }

        private void TextToFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            clientsViewSource.View.Refresh();
        }

        private void ClientsCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is Client client)
            {
                if (!string.IsNullOrEmpty(TextToFilter.Text))
                {
                    if (!client.Name.ToLower().Contains(TextToFilter.Text.ToLower()))
                    {
                        e.Accepted = false;
                    }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            _ = Owner.Activate();
            MainWindow.statusBar.ClearStatus();
        }

        private void OpenDialogButton_Click(object sender, RoutedEventArgs e)
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
                    AccountFileTemplateTextBlock.Text = System.IO.Path.GetFileName(dialog.FileName);
                }
                _ = Activate();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

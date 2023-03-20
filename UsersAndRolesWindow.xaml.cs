using System;
//using System.Collections.Generic;
//using System.Text;
using System.Windows;
//using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
//using System.Windows.Shapes;
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для UsersAndRolesWindow.xaml
    /// </summary>
    public partial class UsersAndRolesWindow : Window
    {
        private CollectionViewSource usersViewSource, rolesViewSource, categoryWorkDataSource;
        private App.AppDbContext _context;
        private readonly ObservableCollection<CategoryWork> categoryWork;

        public UsersAndRolesWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            usersViewSource = (CollectionViewSource)FindResource(nameof(usersViewSource)); //найти описание view в разметке
            rolesViewSource = (CollectionViewSource)FindResource(nameof(rolesViewSource)); //найти описание view в разметке
            categoryWorkDataSource = (CollectionViewSource)FindResource(nameof(categoryWorkDataSource)); //найти описание view в разметке

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                _context.Users.Load();
                _context.Roles.Load();
                usersViewSource.Source = _context.Users.Local.ToObservableCollection();
                rolesViewSource.Source = _context.Roles.Local.ToObservableCollection();
                categoryWork = new ObservableCollection<CategoryWork>();
                foreach(CaregoryWorkName ct in Enum.GetValues(typeof(CaregoryWorkName))) //CaregoryWorkName declare in Users
                {
                    categoryWork.Add(new CategoryWork((short)ct, ct.ToString()));
                }
                categoryWorkDataSource.Source = categoryWork;
                UsersGrid.Focus();
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

        private void SaveUsersAndRoles(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_context != null)
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
            }
        }

        private void CanExecuteSaveUsersAndRoles(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context != null && !ValidationChecker.HasInvalidRows(UsersGrid);
        }

        private void UsersGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            User user = e.NewItem as User;
            user.LoginName = Environment.UserDomainName + '\\';
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
    }
}

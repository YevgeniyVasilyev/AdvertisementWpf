using System;
using System.Windows;
using System.Windows.Input;
using System.Data.Common;
using System.Linq;
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows.Controls;
using System.Windows.Data;

namespace AdvertisementWpf
{
    public partial class LogonWindow : Window
    {
        private RootAppConfigObject rootObject;

        public LogonWindow()
        {
            InitializeComponent();

            DataContext = MainWindow.Connectiondata;
            if (MainWindow.Connectiondata.Basealiases.Count > 0)
            {
                InstanceDB.SelectedIndex = 0;
            }
            using App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
            {
                _context.Database.CloseConnection();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettingGrid.IsVisible) //сохраняем новые данные конфигурации
            {
                AppSettingGrid.Visibility = Visibility.Collapsed;
                rootObject.DataBase.UserName = MainWindow.Connectiondata.Username;
                rootObject.DataBase.ServerName = MainWindow.Connectiondata.Servername;
                rootObject.DataBase.BaseNames = MainWindow.Connectiondata.Basenames.ToArray();
                rootObject.DataBase.BaseAliases = MainWindow.Connectiondata.Basealiases.ToArray();
                AppConfig.Serialize(rootObject);
            }
            if (InstanceDB.SelectedIndex >= 0 && InstanceDB.SelectedIndex < MainWindow.Connectiondata.Basenames.Count)
            {
                MainWindow.Connectiondata.Basename = MainWindow.Connectiondata.Basenames[InstanceDB.SelectedIndex];
                MainWindow.Connectiondata.Connectionstring =
                    "Data Source=" + MainWindow.Connectiondata.Servername +
                    "; Database=" + MainWindow.Connectiondata.Basename + ";";
                if ((bool)IsWindowsAuth.IsChecked)
                {
                    MainWindow.Connectiondata.Connectionstring += "Integrated Security=True;";
                    MainWindow.Connectiondata.Username = Environment.UserDomainName + @"\" + Environment.UserName; //USE IN CheckUser !!!
                }
                else
                {
                    MainWindow.Connectiondata.Connectionstring +=
                        "User ID=" + MainWindow.Connectiondata.Username +
                        "; Password=" + userpwd.Password;
                }
                CheckConnection();
                if (MainWindow.Connectiondata.Is_verify_connection)
                {
                    CheckUser();
                }
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void IsWindowsAuth_Checked(object sender, RoutedEventArgs e)
        {
            username.IsEnabled = false;
            userpwd.IsEnabled = false;
            InstanceDB.IsEnabled = false;
        }
        private void IsWindowsAuth_Unchecked(object sender, RoutedEventArgs e)
        {
            username.IsEnabled = true;
            userpwd.IsEnabled = true;
            InstanceDB.IsEnabled = true;
        }

        private void CheckConnection() //проверка подключения к БД 
        {
            using App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            MainWindow.statusBar.WriteStatus("Подключение к базе данных ...", Cursors.Wait);
            try
            {
                MainWindow.Connectiondata.Is_verify_connection = _context.Database.CanConnect();
                if (!MainWindow.Connectiondata.Is_verify_connection)
                {
                    _ = MessageBox.Show("Подключение не выполнено!" + "\n" + "Проверьте указанные имя и пароль, либо обратитесь к администратору!", 
                        "Ошибка проверки подключения к базе данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка проверки подключения к базе данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
                _context.Dispose();
            }
        }

        private void CheckUser() //проверка на наличие в БД 
        {
            if (!MainWindow.Connectiondata.Is_verify_connection)
            {
                return;
            }
            App.AppDbContext _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            MainWindow.statusBar.WriteStatus("Подключение к базе данных ...", Cursors.Wait);
            try
            {
                //проверяем на наличие в БД users
                string userLogin = MainWindow.Connectiondata.Username.ToLower();
                MainWindow.Userdata = _context.Users.Where(u => u.LoginName.ToLower() == userLogin).AsNoTracking().FirstOrDefault();
                //User user = (User)from u in _context.Users where u.LoginName.ToLower() == userLogin select u;
                //проверяем IS_SRVROLEMEMBER на sysadmin
                if (MainWindow.Userdata == null)
                {
                    MainWindow.Userdata = new User { ID = 0 };
                }
                else
                {
                    _context.Entry(MainWindow.Userdata).State = EntityState.Detached;
                }
                string sql = "SELECT IS_SRVROLEMEMBER('sysadmin', '" + MainWindow.Connectiondata.Username + "')"; //"EXEC sp_helpsrvrolemember";
                using (DbCommand command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    bool lOpenConnection = _context.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
                    if (!lOpenConnection)
                    {
                        _context.Database.OpenConnection();
                    }
                    DbDataReader dbReader = command.ExecuteReader();
                    if (dbReader.HasRows)
                    {
                        dbReader.Read();
                        MainWindow.Userdata.Is_sysadmin = dbReader.GetInt32(0) == 1;
                    }
                    dbReader.Close();
                    if (!lOpenConnection)
                    {
                        _context.Database.CloseConnection();
                    }
                }
                if (MainWindow.Userdata.ID > 0)
                {
                    if (MainWindow.Userdata.Disabled)
                    {
                        _ = MessageBox.Show("Данный пользователь отключен в системе!" + "\n" + "Обратитесь к администратору!", "Проверка подключения к базе данных", MessageBoxButton.OK, MessageBoxImage.Information);
                        MainWindow.Connectiondata.Is_verify_connection = false;
                    }
                    else
                    {
                        MainWindow.Userdata.CategoryWorkName = Enum.GetName(typeof(CaregoryWorkName), MainWindow.Userdata.CategoryWork);
                        MainWindow.statusBar.WriteUserInfo();
                    }
                }
                else
                {
                    if (!MainWindow.Userdata.Is_sysadmin)
                    {
                        _ = MessageBox.Show("Вы не являетесь пользователем системы!" + "\n" + "Обратитесь к администратору!", "Проверка подключения к базе данных", MessageBoxButton.OK, MessageBoxImage.Information);
                        MainWindow.Connectiondata.Is_verify_connection = false;
                    }
                    else
                    {
                        MainWindow.Userdata.ID = 0;
                        MainWindow.Userdata.FirstName = "Системный";
                        MainWindow.Userdata.LastName = "администратор";
                        MainWindow.statusBar.WriteUserInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка при подключении к базе данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 && (Keyboard.Modifiers & ModifierKeys.Shift) > 0 && e.Key == Key.A)
            {
                AppSettingGrid.Visibility = AppSettingGrid.IsVisible ? Visibility.Collapsed : Visibility.Visible;
                if (AppSettingGrid.IsVisible)
                {
                    rootObject = new RootAppConfigObject();
                    rootObject.DataBase = new Database();
                    rootObject = AppConfig.DeSerialize();
                }
            }
        }

        private void NewBDButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Connectiondata.Basenames.Add("NewDBName");
            MainWindow.Connectiondata.Basealiases.Add("NewBaseAlias");
            BasenamesListBox.Items.Refresh();
            BasealiasesListBox.Items.Refresh();
        }

        private void ApplayNewBDButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = 0;
            if (BasenamesListBox.SelectedIndex >= 0)
            {
                nIndex = BasenamesListBox.SelectedIndex;
                BasenamesListBox.Items.Refresh();
                BasenamesListBox.SelectedIndex = nIndex;
            }
            if (BasealiasesListBox.SelectedIndex >= 0)
            {
                nIndex = BasealiasesListBox.SelectedIndex;
                BasealiasesListBox.Items.Refresh();
                BasealiasesListBox.SelectedIndex = nIndex;
            }
        }

        private void NewBasenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BasenamesListBox.SelectedIndex >= 0)
            {
                MainWindow.Connectiondata.Basenames[BasenamesListBox.SelectedIndex] = NewBasenameTextBox.Text;
            }
        }

        private void NewBasealiaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BasealiasesListBox.SelectedIndex >= 0)
            {
                MainWindow.Connectiondata.Basealiases[BasealiasesListBox.SelectedIndex] = NewBasealiaseTextBox.Text;
            }
        }

        private void BasenamesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (BasenamesListBox.SelectedIndex >= 0)
                {
                    MainWindow.Connectiondata.Basenames.Remove(MainWindow.Connectiondata.Basenames[BasenamesListBox.SelectedIndex]);
                    MainWindow.Connectiondata.Basealiases.Remove(MainWindow.Connectiondata.Basealiases[BasenamesListBox.SelectedIndex]);
                    BasenamesListBox.Items.Refresh();
                    BasealiasesListBox.Items.Refresh();
                }
            }
        }

        private void BasealiasesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (BasealiasesListBox.SelectedIndex >= 0)
                {
                    MainWindow.Connectiondata.Basealiases.Remove(MainWindow.Connectiondata.Basealiases[BasealiasesListBox.SelectedIndex]);
                    MainWindow.Connectiondata.Basenames.Remove(MainWindow.Connectiondata.Basenames[BasealiasesListBox.SelectedIndex]);
                    BasenamesListBox.Items.Refresh();
                    BasealiasesListBox.Items.Refresh();
                }
            }
        }
    }
}

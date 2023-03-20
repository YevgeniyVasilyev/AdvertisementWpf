using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using AdvertisementWpf.Models;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class SmallHandBookWindow : Window
    {
        private CollectionViewSource banksViewSource, unitsViewSource, typeOfActivitysDataSource, localitysDataSource, categoryOfProductssDataSource;
        private App.AppDbContext _context;

        public SmallHandBookWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            banksViewSource = (CollectionViewSource)FindResource(nameof(banksViewSource)); //найти описание view в разметке
            unitsViewSource = (CollectionViewSource)FindResource(nameof(unitsViewSource)); //найти описание view в разметке
            typeOfActivitysDataSource = (CollectionViewSource)FindResource(nameof(typeOfActivitysDataSource));
            localitysDataSource = (CollectionViewSource)FindResource(nameof(localitysDataSource));
            categoryOfProductssDataSource = (CollectionViewSource)FindResource(nameof(categoryOfProductssDataSource));

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                _context.Banks.Load();
                _context.Units.Load();
                _context.TypeOfActivitys.Load();
                _context.Localities.Load();
                _context.CategoryOfProducts.Load();
                banksViewSource.Source = _context.Banks.Local.ToObservableCollection();
                unitsViewSource.Source = _context.Units.Local.ToObservableCollection();
                typeOfActivitysDataSource.Source = _context.TypeOfActivitys.Local.ToObservableCollection();
                localitysDataSource.Source = _context.Localities.Local.ToObservableCollection();
                categoryOfProductssDataSource.Source = _context.CategoryOfProducts.Local.ToObservableCollection();
                _ = BanksGrid.Focus();
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

        private void SaveHandbooks(object sender, ExecutedRoutedEventArgs e)
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
                    _ = MessageBox.Show(ex.Message + "\n" + ex.InnerException.Message, "Ошибка сохранения данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MainWindow.statusBar.ClearStatus();
                }
            }
        }

        private void CanExecuteSaveHandbooks(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context != null && !HasInvalidRows(BanksGrid);
        }

        public static bool HasInvalidRows(DataGrid datagrid)
        {
            bool valid = true;
            foreach (object item in datagrid.ItemContainerGenerator.Items)
            {
                DependencyObject evaluateItem = datagrid.ItemContainerGenerator.ContainerFromItem(item);
                if (evaluateItem == null) continue; //null объекты пропустить
                if (!(evaluateItem is DataGridRow dgr)) continue; //если это не строка,то пропустить
                //dgr.BindingGroup.CommitEdit();

                valid &= !Validation.GetHasError(evaluateItem);
            }
            return !valid;
        }
    }

    public class NotNullAndEmptyValidationCheckRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                BindingGroup bg = value as BindingGroup;
                if (bg.Items[0].GetType().Name == "Bank")
                {
                    Bank bank = bg.Items[0] as Bank;
                    if (string.IsNullOrEmpty(bank.Name))
                    {
                        return new ValidationResult(false, "Значение поля НАИМЕНОВАНИЕ БАНКА не может быть пустым!");
                    }
                    if (bank.LocalitiesID <= 0)
                    {
                        return new ValidationResult(false, "Значение поля НАСЕЛЕННЫЙ ПУНКТ не может быть пустым!");
                    }
                    if (string.IsNullOrEmpty(bank.CorrAccount))
                    {
                        return new ValidationResult(false, "Значение поля КОРРЕСПОНДЕНТСКИЙ СЧЕТ не может быть пустым!");
                    }
                    else
                    {
                        if (bank.CorrAccount.Trim().Length != 20)
                        {
                            return new ValidationResult(false, "Количество знаков поля КОРРЕСПОНДЕНТСКИЙ СЧЕТ должно быть равно 20!");
                        }
                    }
                    if (string.IsNullOrEmpty(bank.BIK))
                    {
                        return new ValidationResult(false, "Значение поля БИК не может быть пустым!");
                    }
                    else
                    {
                        if (bank.BIK.Trim().Length != 9)
                        {
                            return new ValidationResult(false, "Количество знаков поля БИК должно быть равно 9!");
                        }
                    }
                }
                return new ValidationResult(true, null);
            }
            catch
            {
                return new ValidationResult(false, "NotNullAndEmptyValidationGroupRule: Ошибка проверки данных!");
            }
        }
    }
}

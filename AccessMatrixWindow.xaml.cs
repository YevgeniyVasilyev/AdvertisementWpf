using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для AccessMatrixWindow.xaml
    /// </summary>
    public partial class AccessMatrixWindow : Window
    {
        private CollectionViewSource iAccessMatrixViewSource;
        private App.AppDbContext _context, __context;
        private List<Role> roles;

        public AccessMatrixWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            iAccessMatrixViewSource = (CollectionViewSource)FindResource(nameof(iAccessMatrixViewSource)); //найти описание view в разметке
            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                roles = _context.Roles.ToList();
                _context.IAccessMatrix.Load();
                iAccessMatrixViewSource.Source = _context.IAccessMatrix.Local.ToObservableCollection();
                foreach (Role role in roles) //дополнить таблицу столбцами по количеству ролей
                {
                    MatrixGrid.Columns.Add(new DataGridCheckBoxColumn { Header = role.RoleName });
                }
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

        private void MatrixGrid_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (IAccessMatrix accessMatrix in iAccessMatrixViewSource.View) //проход по видам доступа
            {
                accessMatrix.GrantToList(); //преобразовать строку в список
                int idx = 0;
                foreach (object column in MatrixGrid.Columns) //проход по столбцам таблицы в текущей строке
                {
                    if (column is DataGridCheckBoxColumn checkBoxColumn)
                    {
                        //checkBoxColumn.GetCellContent(accessMatrix).SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                        if (accessMatrix.accessGrant.Contains(roles[idx].ID)) //ID роли есть в списке доступа
                        {
                            checkBoxColumn.GetCellContent(accessMatrix).SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                        }
                        idx++;
                    }
                }
            }
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_context != null)
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Сохранение данных ...", Cursors.Wait);
                    foreach (IAccessMatrix accessMatrix in iAccessMatrixViewSource.View) //проход по строкам
                    {
                        int idx = 0;
                        foreach (object column in MatrixGrid.Columns) //проход по столбцам
                        {
                            if (column is DataGridCheckBoxColumn checkBoxColumn)
                            {
                                if ((bool)checkBoxColumn.GetCellContent(accessMatrix).GetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty)) //роль отмечена для предоставления права
                                {
                                    if (!accessMatrix.accessGrant.Contains(roles[idx].ID)) //ID роли нет в списке
                                    {
                                        accessMatrix.accessGrant.Add(roles[idx].ID);
                                    }
                                }
                                else //отрабатываем ситуацию когда ID роли был в списке
                                {
                                    if (accessMatrix.accessGrant.Contains(roles[idx].ID)) //ID роли есть в списке
                                    {
                                        accessMatrix.accessGrant.Remove(roles[idx].ID);
                                    }
                                }
                                idx++;
                            }
                        }
                        accessMatrix.ListToGrant();
                    }
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

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_context != null)
            {
                e.CanExecute = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            if (__context != null)
            {
                __context.Dispose();
                __context = null;
            }
            MainWindow.statusBar.ClearStatus();
        }
    }
}

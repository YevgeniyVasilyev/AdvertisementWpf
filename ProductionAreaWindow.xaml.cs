using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ProductionAreaWindow.xaml
    /// </summary>
    public partial class ProductionAreaWindow : Window
    {
        private CollectionViewSource typeOfActivityViewSource, typeOfActivityInProdAreaViewSource;
        private App.AppDbContext _context;

        public ProductionAreaWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);
            InitializeComponent();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            typeOfActivityViewSource = (CollectionViewSource)FindResource(nameof(typeOfActivityViewSource));
            typeOfActivityInProdAreaViewSource = (CollectionViewSource)FindResource(nameof(typeOfActivityInProdAreaViewSource));

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                _context.TypeOfActivitys
                    .Include(TypeOfActivity => TypeOfActivity.TypeOfActivityInProdAreas)
                    .ThenInclude(TypeOfActivityInProdArea => TypeOfActivityInProdArea.ProductionArea)
                    .Load();
                typeOfActivityViewSource.Source = _context.TypeOfActivitys.Local.ToObservableCollection();
                typeOfActivityViewSource.View.CurrentChanged += TypeOfActivityViewSourceView_CurrentChanged;

                TypeOfActivityViewSourceView_CurrentChanged(null, null); //"дернем" коллекции и фильтр для инициализации
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

        private void TypeOfActivityViewSourceView_CurrentChanged(object sender, EventArgs e)
        {
            if (typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && typeOfActivityViewSource.View.CurrentItem is TypeOfActivity typeOfActivity)
            {
                typeOfActivityInProdAreaViewSource.Source = new ObservableCollection<TypeOfActivityInProdArea>(typeOfActivity.TypeOfActivityInProdAreas);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            MainWindow.statusBar.ClearStatus();
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_context != null)
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Сохранение данных ...", Cursors.Wait);
                    _ = _context.SaveChanges(); //сохранить 
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
            if (_context != null && typeOfActivityViewSource != null && typeOfActivityViewSource.View != null 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ProductionAreasHandBookNewEdit"))
            {
                e.CanExecute = true;
            }
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.GetType().FullName.Contains("Button"))
                {
                    Button btn = e.OriginalSource as Button;
                    if (btn == ProductionAreaButton)
                    {
                        TypeOfActivity typeOfActivity = typeOfActivityViewSource.View.CurrentItem as TypeOfActivity;
                        ProductionArea productionArea = new ProductionArea { Name = "Новый производственный участок" };
                        _ = _context.ProductionAreas.Add(productionArea);
                        TypeOfActivityInProdArea typeOfActivityInProdArea = new TypeOfActivityInProdArea
                        {
                            TypeOfActivityID = typeOfActivity.ID,
                            ProductionAreaID = productionArea.ID,
                            ProductionArea = productionArea
                        };
                        typeOfActivity.TypeOfActivityInProdAreas.Add(typeOfActivityInProdArea);
                        _context.TypeOfActivityInProdAreas.Add(typeOfActivityInProdArea);
                        typeOfActivityViewSource.View.Refresh();
                        typeOfActivityViewSource.View.MoveCurrentTo(typeOfActivity);
                        typeOfActivityInProdAreaViewSource.View.Refresh();
                        typeOfActivityInProdAreaViewSource.View.MoveCurrentTo(typeOfActivityInProdArea);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Ошибка добавления данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
            }
        }

        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                Button btn = e.OriginalSource as Button;
                if (btn == ProductionAreaButton && typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && typeOfActivityViewSource.View.CurrentItem != null 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ProductionAreasHandBookNewEdit"))
                {
                    e.CanExecute = true;
                }
            }
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && typeOfActivityViewSource != null && typeOfActivityViewSource.View != null
                && typeOfActivityViewSource.View.CurrentItem is TypeOfActivity typeOfActivity 
                && typeOfActivityInProdAreaViewSource != null && typeOfActivityInProdAreaViewSource.View != null 
                && typeOfActivityInProdAreaViewSource.View.CurrentItem is TypeOfActivityInProdArea typeOfActivityInProdArea 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ProductionAreasHandBookDelete"))
            {
                try
                {
                    typeOfActivity.TypeOfActivityInProdAreas.Remove(typeOfActivityInProdArea);
                    _ = _context.ProductionAreas.Remove(typeOfActivityInProdArea.ProductionArea);
                    _ = _context.TypeOfActivityInProdAreas.Remove(typeOfActivityInProdArea);
                    typeOfActivityViewSource.View.Refresh();
                    typeOfActivityInProdAreaViewSource.View.Refresh();
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

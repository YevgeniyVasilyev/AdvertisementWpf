using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ReferencebookWindow.xaml
    /// </summary>
    public partial class ReferencebookWindow : Window
    {
        private CollectionViewSource referencebookViewSource, categoryOfProductViewSource, typeOfActivityViewSource;
        private App.AppDbContext _context;
        private List<ReferencebookParameter> referencebookParameters;
        private List<ReferencebookApplicability> referencebookApplicabilities;

        public ReferencebookWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();
            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            referencebookViewSource = (CollectionViewSource)FindResource(nameof(referencebookViewSource)); //найти описание view в разметке
            categoryOfProductViewSource = (CollectionViewSource)FindResource(nameof(categoryOfProductViewSource));
            typeOfActivityViewSource = (CollectionViewSource)FindResource(nameof(typeOfActivityViewSource));

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                _context.Referencebook.Load();
                referencebookViewSource.Source = _context.Referencebook.Local.ToObservableCollection();
                referencebookParameters = _context.ReferencebookParameter.AsNoTracking().ToList();
                referencebookApplicabilities = _context.ReferencebookApplicability.AsNoTracking().ToList();
                
                categoryOfProductViewSource.Source = _context.CategoryOfProducts.AsNoTracking().ToList();
                typeOfActivityViewSource.Source = _context.TypeOfActivitys.AsNoTracking().ToList();

                if (_context.Referencebook.Local.Count > 0)
                {
                    foreach (Referencebook referencebook in referencebookViewSource.View)
                    {
                        referencebook.ReferencebookParameters = referencebookParameters.Where(referencebookParameters => referencebookParameters.ReferencebookID == referencebook.ID).ToList();
                        referencebook.ReferencebookApplicabilities = referencebookApplicabilities.Where(referencebookApplicabilities => referencebookApplicabilities.ReferencebookID == referencebook.ID).ToList();
                        foreach (ReferencebookApplicability referencebookApplicability in referencebook.ReferencebookApplicabilities)
                        {
                            if (referencebookApplicability.CategoryOfProductID > 0 && _context.CategoryOfProducts.Find(referencebookApplicability.CategoryOfProductID) is CategoryOfProduct categoryOfProduct)
                            {
                                referencebookApplicability.CategoryOfProductName = categoryOfProduct.Name;
                            }
                            if (referencebookApplicability.TypeOfActivityID > 0 && _context.TypeOfActivitys.Find(referencebookApplicability.TypeOfActivityID) is TypeOfActivity typeOfActivity)
                            {
                                referencebookApplicability.TypeOfActivityName = typeOfActivity.Name;
                            }
                            _context.ReferencebookApplicability.Attach(referencebookApplicability);
                        }
                        foreach (ReferencebookParameter referencebookParameter in referencebook.ReferencebookParameters)
                        {
                            _context.ReferencebookParameter.Attach(referencebookParameter);
                        }
                    }
                    referencebookViewSource.View.MoveCurrentToFirst();
                    referencebookViewSource.View.Refresh();
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

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_context != null)
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Сохранение данных ...", Cursors.Wait);
                    foreach (Referencebook referencebook in referencebookViewSource.View)
                    {
                        foreach (ReferencebookParameter referencebookParameter in referencebook.ReferencebookParameters)
                        {
                            if (referencebookParameter.ReferencebookID == 0)
                            {
                                referencebookParameter.ReferencebookID = referencebook.ID;
                            }
                            if (referencebookParameter.ID == 0) //новый параметр
                            {
                                _ = _context.ReferencebookParameter.Add(referencebookParameter);
                            }
                            //else if (_context.Entry(referencebookParameter).State != EntityState.Unchanged) //если сущность НЕ ИМЕЕТ статус "неизменено"
                            //{
                            //    _ = __context.ReferencebookParameter.Update(referencebookParameter);
                            //}
                        }
                        foreach (ReferencebookApplicability referencebookApplicability in referencebook.ReferencebookApplicabilities)
                        {
                            if (referencebookApplicability.ReferencebookID == 0)
                            {
                                referencebookApplicability.ReferencebookID = referencebook.ID;
                            }
                            if (referencebookApplicability.ID == 0) //новый параметр
                            {
                                _ = _context.ReferencebookApplicability.Add(referencebookApplicability);
                            }
                            //else if (_context.Entry(referencebookApplicability).State != EntityState.Unchanged) //если сущность НЕ ИМЕЕТ статус "неизменено"
                            //{
                            //    _ = __context.ReferencebookApplicability.Update(referencebookApplicability);
                            //}
                        }
                    }
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
            if (referencebookViewSource != null)
            {
                foreach (Referencebook referencebook in referencebookViewSource.View)
                {
                    if (string.IsNullOrWhiteSpace(referencebook.Name))
                    {
                        e.CanExecute = false;
                        return;
                    }
                    if (referencebook.ReferencebookParameters.Count == 0)
                    {
                        e.CanExecute = false;
                        return;
                    }
                    foreach (ReferencebookParameter referencebookParameter in referencebook.ReferencebookParameters)
                    {
                        if (string.IsNullOrWhiteSpace(referencebookParameter.Value))
                        {
                            e.CanExecute = false;
                            return;
                        }
                    }
                }
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
                    if (btn.Name == "ReferencebookButton")
                    {
                        Referencebook referencebook = new Referencebook
                        {
                            Name = "Новый справочник",
                            ReferencebookParameters = new List<ReferencebookParameter> { }
                        };
                        _ = _context.Referencebook.Add(referencebook);
                        referencebookViewSource.View.MoveCurrentTo(referencebook);
                        referencebookViewSource.View.Refresh();
                    }
                    if (btn.Name == "ReferencebookParameterButton")
                    {
                        if (referencebookViewSource.View.CurrentItem is Referencebook referencebook)
                        {
                            referencebook.ReferencebookParameters.Add(new ReferencebookParameter { ID = 0, ReferencebookID = referencebook.ID, Value = "Новый параметр"});
                            ReferencebookParametersListBox.Items.Refresh();
                            referencebookViewSource.View.MoveCurrentTo(referencebook);
                        }
                    }
                    if (btn.Name == "CategoryOfProductApplicabilitiesButton")
                    {
                        categoryOfProductViewSource.View.Refresh();
                        categoryOfProductViewSource.View.MoveCurrentToLast();
                        ListCategoryOfProduct.Visibility = Visibility.Visible;
                    }
                    if (btn.Name == "TypeOfActyvityApplicabilitiesButton")
                    {
                        typeOfActivityViewSource.View.Refresh();
                        typeOfActivityViewSource.View.MoveCurrentToLast();
                        ListTypeOfActyvity.Visibility = Visibility.Visible;
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
                if (btn.Name == "ReferencebookButton" && referencebookViewSource != null)
                {
                    e.CanExecute = true;
                }
                if (btn.Name == "ReferencebookParameterButton" && referencebookViewSource != null && (referencebookViewSource.View.CurrentItem is Referencebook))
                {
                    e.CanExecute = true;
                }
                if (btn.Name == "CategoryOfProductApplicabilitiesButton" && referencebookViewSource != null && (referencebookViewSource.View.CurrentItem is Referencebook) && categoryOfProductViewSource != null)
                {
                    e.CanExecute = true;
                }
                if (btn.Name == "TypeOfActyvityApplicabilitiesButton" && referencebookViewSource != null && (referencebookViewSource.View.CurrentItem is Referencebook) && typeOfActivityViewSource != null)
                {
                    e.CanExecute = true;
                }
            }
        }

        //private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (e.OriginalSource.GetType().FullName.Contains("Item"))
        //        {
        //            ListBoxItem item = e.OriginalSource as ListBoxItem;
        //            if (referencebookViewSource.View.CurrentItem is Referencebook referencebook)
        //            {
        //                if (item.DataContext is Referencebook refBook)
        //                {
        //                    _ = _context.Referencebook.Remove(refBook);
        //                }
        //                if (item.DataContext is ReferencebookParameter referencebookParameter)
        //                {
        //                    if (referencebookParameter.ID > 0)
        //                    {
        //                        _ = _context.ReferencebookParameter.Remove(referencebookParameter);
        //                    }
        //                    _ = referencebook.ReferencebookParameters.Remove(referencebookParameter);
        //                    ReferencebookParametersListBox.Items.Refresh();
        //                }
        //                if (item.DataContext is ReferencebookApplicability referencebookApplicability && !(referencebookApplicability.CategoryOfProductID is null) && referencebookApplicability.CategoryOfProductID > 0)
        //                {
        //                    if ((referencebookApplicability.TypeOfActivityID is null) || referencebookApplicability.TypeOfActivityID == 0) //КВД в паре не определн
        //                    {
        //                        if (referencebookApplicability.ID > 0)
        //                        {
        //                            _context.ReferencebookApplicability.Remove(referencebookApplicability); //удалить полностью
        //                        }
        //                        referencebook.ReferencebookApplicabilities.Remove(referencebookApplicability);
        //                    }
        //                    else
        //                    {
        //                        referencebookApplicability.CategoryOfProductID = null;
        //                        referencebookApplicability.CategoryOfProductName = "";
        //                        _ = _context.ReferencebookApplicability.Update(referencebookApplicability);
        //                    }
        //                }
        //                if (item.DataContext is ReferencebookApplicability referencebookApplicability1 && !(referencebookApplicability1.TypeOfActivityID is null) && referencebookApplicability1.TypeOfActivityID > 0)
        //                {
        //                    if ((referencebookApplicability1.CategoryOfProductID is null) || referencebookApplicability1.CategoryOfProductID == 0) //категория в паре не определна
        //                    {
        //                        if (referencebookApplicability1.ID > 0)
        //                        {
        //                            _ = _context.ReferencebookApplicability.Remove(referencebookApplicability1); //удалить полностью
        //                        }
        //                        _ = referencebook.ReferencebookApplicabilities.Remove(referencebookApplicability1);
        //                    }
        //                    else
        //                    {
        //                        referencebookApplicability1.TypeOfActivityID = null;
        //                        referencebookApplicability1.TypeOfActivityName = "";
        //                        _context.ReferencebookApplicability.Update(referencebookApplicability1);
        //                    }
        //                }
        //                if (_context.Referencebook.Local.Count > 0)
        //                {
        //                    referencebookViewSource.View.Refresh();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = true;
        //}

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (referencebookViewSource != null && categoryOfProductViewSource != null)
            {
                if (e.Item is CategoryOfProduct categoryOfProduct && referencebookViewSource.View.CurrentItem is Referencebook referencebook)
                {
                    foreach (ReferencebookApplicability referencebookApplicability in referencebook.ReferencebookApplicabilities)
                    {
                        if (referencebookApplicability.CategoryOfProductID.Equals(categoryOfProduct.ID)) //вид деятельности уже есть в списке
                        {
                            e.Accepted = false;
                            return;
                        }
                    }
                    e.Accepted = true;
                }
            }
            if (referencebookViewSource != null && typeOfActivityViewSource != null)
            {
                if (e.Item is TypeOfActivity typeOfActivity && referencebookViewSource.View.CurrentItem is Referencebook referencebook)
                {
                    foreach (ReferencebookApplicability referencebookApplicability in referencebook.ReferencebookApplicabilities)
                    {
                        if (referencebookApplicability.TypeOfActivityID.Equals(typeOfActivity.ID)) //вид деятельности уже есть в списке
                        {
                            e.Accepted = false;
                            return;
                        }
                    }
                    e.Accepted = true;
                }
            }
        }

        private void ListCategoryOfProduct_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListCategoryOfProduct.Visibility = Visibility.Collapsed;

            if (referencebookViewSource.View.CurrentItem is Referencebook referencebook && categoryOfProductViewSource.View.CurrentItem is CategoryOfProduct categoryOfProduct)
            {
                if (referencebook.ReferencebookApplicabilities.Count > 0)
                {
                    foreach (ReferencebookApplicability referencebookApplicability in referencebook.ReferencebookApplicabilities)
                    {
                        if (referencebookApplicability.CategoryOfProductID is null) //ищем пустую строку
                        {
                            referencebookApplicability.CategoryOfProductID = categoryOfProduct.ID;
                            referencebookApplicability.CategoryOfProductName = categoryOfProduct.Name;
                            ReferencebookApplicabilitiesCategoryListBox.Items.Refresh();
                            return;
                        }
                    }
                } //пустой строки нет
                referencebook.ReferencebookApplicabilities.Add(new ReferencebookApplicability 
                { ID = 0, ReferencebookID = referencebook.ID, CategoryOfProductID = categoryOfProduct.ID, CategoryOfProductName = categoryOfProduct.Name });
                ReferencebookApplicabilitiesCategoryListBox.Items.Refresh();
            }
        }

        private void ListTypeOfActyvity_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListTypeOfActyvity.Visibility = Visibility.Collapsed;
            if (referencebookViewSource.View.CurrentItem is Referencebook referencebook && typeOfActivityViewSource.View.CurrentItem is TypeOfActivity typeOfActivity)
            {
                if (referencebook.ReferencebookApplicabilities.Count > 0)
                {
                    foreach (ReferencebookApplicability referencebookApplicability in referencebook.ReferencebookApplicabilities)
                    {
                        if (referencebookApplicability.TypeOfActivityID is null) //ищем пустую строку
                        {
                            referencebookApplicability.TypeOfActivityID = typeOfActivity.ID;
                            referencebookApplicability.TypeOfActivityName = typeOfActivity.Name;
                            ReferencebookApplicabilitiesTypeOfActivityListBox.Items.Refresh();
                            return;
                        }
                    }
                } //пустой строки нет
                referencebook.ReferencebookApplicabilities.Add(new ReferencebookApplicability
                { ID = 0, ReferencebookID = referencebook.ID, TypeOfActivityID = typeOfActivity.ID, TypeOfActivityName = typeOfActivity.Name });
                ReferencebookApplicabilitiesTypeOfActivityListBox.Items.Refresh();
            }
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && referencebookViewSource != null && referencebookViewSource.View != null && referencebookViewSource.View.CurrentItem is Referencebook referencebook)
            {
                if (e.OriginalSource as ListBoxItem is null)
                {
                    return;
                }
                ListBoxItem item = e.OriginalSource as ListBoxItem;
                try
                {
                    if (item.DataContext is Referencebook refBook)
                    {
                        _ = _context.Referencebook.Remove(refBook);
                    }
                    if (item.DataContext is ReferencebookParameter referencebookParameter)
                    {
                        if (referencebookParameter.ID > 0)
                        {
                            _ = _context.ReferencebookParameter.Remove(referencebookParameter);
                        }
                        _ = referencebook.ReferencebookParameters.Remove(referencebookParameter);
                        ReferencebookParametersListBox.Items.Refresh();
                    }
                    string ListBoxName = (sender as ListBox).Name;
                    if (item.DataContext is ReferencebookApplicability referencebookApplicability)
                    {
                        if (ListBoxName == "ReferencebookApplicabilitiesCategoryListBox")
                        {
                            if ((referencebookApplicability.TypeOfActivityID is null) || referencebookApplicability.TypeOfActivityID == 0) //КВД в паре не определн
                            {
                                if (referencebookApplicability.ID > 0) //не только что добавленный
                                {
                                    _context.ReferencebookApplicability.Remove(referencebookApplicability); //удалить полностью
                                }
                                referencebook.ReferencebookApplicabilities.Remove(referencebookApplicability);
                            }
                            else
                            {
                                referencebookApplicability.CategoryOfProductID = null;
                                referencebookApplicability.CategoryOfProductName = "";
                                _ = _context.ReferencebookApplicability.Update(referencebookApplicability);
                            }
                        }
                        if (ListBoxName == "ReferencebookApplicabilitiesTypeOfActivityListBox")
                        {
                            if ((referencebookApplicability.CategoryOfProductID is null) || referencebookApplicability.CategoryOfProductID == 0) //категория в паре не определна
                            {
                                if (referencebookApplicability.ID > 0)
                                {
                                    _ = _context.ReferencebookApplicability.Remove(referencebookApplicability); //удалить полностью
                                }
                                _ = referencebook.ReferencebookApplicabilities.Remove(referencebookApplicability);
                            }
                            else
                            {
                                referencebookApplicability.TypeOfActivityID = null;
                                referencebookApplicability.TypeOfActivityName = "";
                                _context.ReferencebookApplicability.Update(referencebookApplicability);
                            }
                        }
                        ReferencebookApplicabilitiesCategoryListBox.Items.Refresh();
                        ReferencebookApplicabilitiesTypeOfActivityListBox.Items.Refresh();
                    }
                    if (_context.Referencebook.Local.Count > 0)
                    {
                        referencebookViewSource.View.Refresh();
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HideCategoryOfProductApplicabilitiesButton_Click(object sender, RoutedEventArgs e)
        {
            ListCategoryOfProduct.Visibility = Visibility.Collapsed;
        }

        private void HideTypeOfActyvityApplicabilitiesButton_Click(object sender, RoutedEventArgs e)
        {
            ListTypeOfActyvity.Visibility = Visibility.Collapsed;
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
    }
}

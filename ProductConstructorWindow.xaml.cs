using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AdvertisementWpf.Models;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ProductConstructorWindow.xaml
    /// </summary>
    public partial class ProductConstructorWindow : Window
    {
        private CollectionViewSource productTypesViewSource, categoryOfProductsViewSource, unitsViewSource, typeOfActivitysViewSource, referencebookViewSource;
        private App.AppDbContext _context, __context;
        private List<ParameterInProductType> parameterInProductTypes;
        private List<TypeOfActivityInProduct> typeOfActivityInProducts;
        private List<ReferencebookApplicability> referencebookApplicabilities;

        public ProductConstructorWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();
            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            productTypesViewSource = (CollectionViewSource)FindResource(nameof(productTypesViewSource)); //найти описание view в разметке
            categoryOfProductsViewSource = (CollectionViewSource)FindResource(nameof(categoryOfProductsViewSource));
            typeOfActivitysViewSource = (CollectionViewSource)FindResource(nameof(typeOfActivitysViewSource));
            unitsViewSource = (CollectionViewSource)FindResource(nameof(unitsViewSource));
            referencebookViewSource = (CollectionViewSource)FindResource(nameof(referencebookViewSource));

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            __context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                categoryOfProductsViewSource.Source = _context.CategoryOfProducts.AsNoTracking().ToList();
                categoryOfProductsViewSource.View.CurrentChanged += CategoryOfProductsView_CurrentChanged;
                unitsViewSource.Source = _context.Units.AsNoTracking().ToList();
                typeOfActivitysViewSource.Source = _context.TypeOfActivitys.AsNoTracking().ToList();
                referencebookViewSource.Source = _context.Referencebook.AsNoTracking().ToList();
                referencebookApplicabilities = _context.ReferencebookApplicability.AsNoTracking().ToList();

                _context.ProductTypes.Load(); //виды изделий
                productTypesViewSource.Source = _context.ProductTypes.Local.ToObservableCollection();

                parameterInProductTypes = _context.ParameterInProductTypes.AsNoTracking().ToList(); //набор параметров в изделии
                typeOfActivityInProducts = _context.TypeOfActivityInProducts.AsNoTracking().ToList(); //коды видов деятельности в изделии
                foreach (ProductType productType in productTypesViewSource.View)
                {
                    productType.ParametersInProductType = parameterInProductTypes.Where(pinpt => pinpt.ProductTypeID == productType.ID).ToList();
                    productType.TypeOfActivitysInProduct = typeOfActivityInProducts.Where(tainp => tainp.ProductTypeID == productType.ID).ToList();
                    __context.ParameterInProductTypes.AttachRange(productType.ParametersInProductType);
                    __context.TypeOfActivityInProducts.AttachRange(productType.TypeOfActivitysInProduct);
                }
                productTypesViewSource.Filter += ProductTypesViewSource_Filter;
                CategoryOfProductsView_CurrentChanged(null, null);
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

        private void CategoryOfProductsView_CurrentChanged(object sender, EventArgs e)
        {
            if (productTypesViewSource != null && productTypesViewSource.View != null)
            {
                productTypesViewSource.View.Refresh();
                referencebookViewSource.View.Refresh();
            }
        }

        private void ProductTypesViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (categoryOfProductsViewSource != null && categoryOfProductsViewSource.View != null && categoryOfProductsViewSource.View.CurrentItem is CategoryOfProduct categoryOfProduct)
            {
                if (e.Item is ProductType productType)
                {
                    e.Accepted = productType.CategoryOfProductID == categoryOfProduct.ID;
                }
            }
        }

        private void ReferencebookViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (productTypesViewSource != null && productTypesViewSource.View != null && productTypesViewSource.View.CurrentItem is ProductType productType)
            {
                foreach (ReferencebookApplicability referencebookApplicability in referencebookApplicabilities)
                {
                    if (referencebookApplicability.CategoryOfProductID == productType.CategoryOfProductID)
                    {
                        if (referencebookApplicability.ReferencebookID == (e.Item as Referencebook).ID)
                        {
                            e.Accepted = true;
                            return;
                        }
                    }
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (productTypesViewSource != null && productTypesViewSource.View != null && productTypesViewSource.View.CurrentItem is ProductType productType)
            {
                if (ParametersInProductstGrid.SelectedItem is ParameterInProductType parameterInProductType)
                {
                    if (ParametersInProductstGrid.CancelEdit() && ParametersInProductstGrid.CancelEdit())
                    {
                        if (parameterInProductType.ReferencebookID > 0)
                        {
                            parameterInProductType.ReferencebookID = null;
                            ParametersInProductstGrid.Items.Refresh();
                        }
                    }
                }
            }
        }

        private void ProductListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && productTypesViewSource != null && productTypesViewSource.View.CurrentItem is ProductType productType)
            {
                foreach (ParameterInProductType parameterInProductType in productType.ParametersInProductType)
                {
                    _ = _context.ParameterInProductTypes.Remove(parameterInProductType);
                }
                foreach (TypeOfActivityInProduct typeOfActivityInProduct in productType.TypeOfActivitysInProduct)
                {
                    _ = _context.TypeOfActivityInProducts.Remove(typeOfActivityInProduct);
                }
                _ = _context.ProductTypes.Remove(productType);
            }
        }

        private void ParametersInProductstGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && productTypesViewSource?.View.CurrentItem is ProductType productType)
            {
                if (((DataGrid)sender).SelectedItem is ParameterInProductType parameterInProductType && parameterInProductType.ID > 0)
                {
                    _ = _context.ParameterInProductTypes.Remove(parameterInProductType);
                }
            }
        }

        private void TypeOfActivityInProductstGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && productTypesViewSource?.View.CurrentItem is ProductType)
            {
                if (((DataGrid)sender).SelectedItem is TypeOfActivityInProduct typeOfActivityInProduct && typeOfActivityInProduct.ID > 0)
                {
                    _ = _context.TypeOfActivityInProducts.Remove(typeOfActivityInProduct);
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
            if (__context != null)
            {
                __context.Dispose();
                __context = null;
            }
            MainWindow.statusBar.ClearStatus();
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.GetType().FullName.Contains("Button"))
                {
                    Button btn = e.OriginalSource as Button;
                    if (btn.Name == "NewProductTypeButton")
                    {
                        CategoryOfProduct cp = categoryOfProductsViewSource.View.CurrentItem as CategoryOfProduct;
                        ProductType pt = new ProductType
                        {
                            Name = "наименование",
                            CategoryOfProductID = cp?.ID ?? 0
                        };
                        _ = _context.ProductTypes.Add(pt);
                        _ = productTypesViewSource.View.MoveCurrentTo(pt);
                        productTypesViewSource.View.Refresh();
                    }
                    if (btn.Name == "NewParameterInProductTypeButton")
                    {
                        if (productTypesViewSource.View.CurrentItem is ProductType productType)
                        {
                            ParameterInProductType pinpt = new ParameterInProductType
                            {
                                Name = "параметр",
                                UnitID = (unitsViewSource.View.CurrentItem as Unit).ID,
                                IsRequired = true,
                            };
                            if (productType.ID > 0)
                            {
                                pinpt.ProductTypeID = productType.ID;
                            }
                            productType.ParametersInProductType.Add(pinpt);
                            ParametersInProductstGrid.Items.Refresh();
                        }
                    }
                    if (btn.Name == "NewTypeOfActivityInProductButton")
                    {
                        if (productTypesViewSource.View.CurrentItem is ProductType productType)
                        {
                            _ = typeOfActivitysViewSource.View.MoveCurrentToFirst();
                            TypeOfActivityInProduct tainp = new TypeOfActivityInProduct
                            {
                                TypeOfActivityID = (typeOfActivitysViewSource.View.CurrentItem as TypeOfActivity).ID,
                            };
                            if (productType.ID > 0)
                            {
                                tainp.ProductTypeID = productType.ID;
                            }
                            productType.TypeOfActivitysInProduct.Add(tainp);
                            TypeOfActivityInProductstGrid.Items.Refresh();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Ошибка добавления данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            { }
        }

        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                Button btn = e.OriginalSource as Button;
                if (btn.Name == "NewProductTypeButton" && productTypesViewSource != null)
                {
                    e.CanExecute = true;
                }
                if (btn.Name == "NewParameterInProductTypeButton" && productTypesViewSource != null && productTypesViewSource.View != null && productTypesViewSource.View.CurrentItem is ProductType)
                {
                    e.CanExecute = true;
                }
                if (btn.Name == "NewTypeOfActivityInProductButton" && productTypesViewSource != null && productTypesViewSource.View != null && productTypesViewSource.View.CurrentItem is ProductType)
                {
                    e.CanExecute = true;
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
                    _ = _context.SaveChanges(); //виды изделий
                    foreach (ProductType productType in productTypesViewSource.View)
                    {
                        if (productType.ParametersInProductType != null && productType.ParametersInProductType.Count > 0)
                        {
                            foreach (ParameterInProductType parameterInProductType in productType.ParametersInProductType)
                            {
                                if (parameterInProductType.ProductTypeID == 0)
                                {
                                    parameterInProductType.ProductTypeID = productType.ID;
                                }
                                if (parameterInProductType.ID == 0) //новый параметр
                                {
                                    _ = __context.ParameterInProductTypes.Add(parameterInProductType);
                                }
                                //else if (_context.Entry(parameterInProductType).State != EntityState.Unchanged)
                                //{
                                //    _ = __context.ParameterInProductTypes.Update(parameterInProductType);
                                //}
                            }
                        }
                        if (productType.TypeOfActivitysInProduct != null && productType.TypeOfActivitysInProduct.Count > 0)
                        {
                            foreach (TypeOfActivityInProduct typeOfActivityInProduct in productType.TypeOfActivitysInProduct)
                            {
                                if (typeOfActivityInProduct.ProductTypeID == 0)
                                {
                                    typeOfActivityInProduct.ProductTypeID = productType.ID;
                                }
                                if (typeOfActivityInProduct.ID == 0) //новый код вида деятельности
                                {
                                    _ = __context.TypeOfActivityInProducts.Add(typeOfActivityInProduct);
                                }
                                //else if (_context.Entry(typeOfActivityInProduct).State != EntityState.Unchanged)
                                //{
                                //    _ = __context.TypeOfActivityInProducts.Update(typeOfActivityInProduct);
                                //}
                            }
                        }
                    }
                    __context.SaveChanges(); //параметры и коды видов деятельности изделий
                    _ = MessageBox.Show("   Сохранено успешно!   ", "Сохранение данных");
                    if (productTypesViewSource != null && productTypesViewSource.View != null)
                    {
                        productTypesViewSource.View.Refresh();
                        ParametersInProductstGrid.Items.Refresh();
                        TypeOfActivityInProductstGrid.Items.Refresh();
                    }
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
            if (_context != null && productTypesViewSource != null)
            {
                e.CanExecute = !ValidationChecker.HasInvalidRows(ParametersInProductstGrid) && !ValidationChecker.HasInvalidRows(TypeOfActivityInProductstGrid) &&
                    !ValidationChecker.HasInvalidTextBox(EditGrid);
            }
            else
            {
                e.CanExecute = false;
            }
        }

        //private void Delele_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    try
        //    {
        //        if ((string)Delete_button.Tag == "ProductType")
        //        {
        //            if (productTypesViewSource.View.CurrentItem is ProductType pt)
        //            {
        //                _ = _context.ProductTypes.Remove(pt);
        //                _ = productTypesViewSource.View.MoveCurrentToNext();
        //                if (productTypesViewSource.View.IsCurrentAfterLast)
        //                {
        //                    productTypesViewSource.View.MoveCurrentToFirst();
        //                }
        //                productTypesViewSource.View.Refresh();
        //            }
        //        }
        //        if ((string)Delete_button.Tag == "ParametersInProduct")
        //        {
        //            if (parameterInProductTypesViewSource.View.CurrentItem is ParameterInProductType pinpt)
        //            {
        //                _ = _context.ParameterInProductTypes.Remove(pinpt);
        //                parameterInProductTypesViewSource.View.Refresh();
        //            }
        //        }
        //        if ((string)Delete_button.Tag == "TypeOfActivityInProduct")
        //        {
        //            if (typeOfActivityInProductsViewSource.View.CurrentItem is TypeOfActivityInProduct tainp)
        //            {
        //                _ = _context.TypeOfActivityInProducts.Remove(tainp);
        //                typeOfActivityInProductsViewSource.View.Refresh();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _ = MessageBox.Show(ex.Message, "Ошибка обновления данных", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //    finally
        //    { }
        //}

        //private void Delele_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    if (_context != null && productTypesViewSource != null && categoryOfProductsViewSource != null && parameterInProductTypesViewSource != null)
        //    {
        //        e.CanExecute = true;
        //    }
        //}
    }
}

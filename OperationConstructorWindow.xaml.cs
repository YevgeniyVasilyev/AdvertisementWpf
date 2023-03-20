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
    /// Логика взаимодействия для OperationConstructorWindow.xaml
    /// </summary>
    public partial class OperationConstructorWindow : Window
    {
        private CollectionViewSource unitsViewSource, productionAreaViewSource, typeOfActivityViewSource, referencebookViewSource;
        private App.AppDbContext _context;
        private List<ReferencebookApplicability> referencebookApplicabilities;


        public OperationConstructorWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            typeOfActivityViewSource = (CollectionViewSource)FindResource(nameof(typeOfActivityViewSource));
            unitsViewSource = (CollectionViewSource)FindResource(nameof(unitsViewSource));
            productionAreaViewSource = (CollectionViewSource)FindResource(nameof(productionAreaViewSource));
            referencebookViewSource = (CollectionViewSource)FindResource(nameof(referencebookViewSource));

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                _context.TypeOfActivitys
                    .Include(TypeOfActivity => TypeOfActivity.TypeOfActivityInOperations)
                    .ThenInclude(TypeOfActivity => TypeOfActivity.Operation)
                    .ThenInclude(Operation => Operation.ParameterInOperations)
                    .Load();
                typeOfActivityViewSource.Source = _context.TypeOfActivitys.Local.ToObservableCollection();
                unitsViewSource.Source = _context.Units.AsNoTracking().ToList();
                productionAreaViewSource.Source = _context.ProductionAreas
                    .Include(ProductionArea => ProductionArea.TypeOfActivityInProdAreas)
                    .AsNoTracking().ToList();
                referencebookApplicabilities = _context.ReferencebookApplicability.AsNoTracking().ToList();
                referencebookViewSource.Source = _context.Referencebook.AsNoTracking().ToList();

                referencebookViewSource.Filter += ReferencebookViewSource_Filter;
                productionAreaViewSource.Filter += ProductionAreaViewSource_Filter;
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

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
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
                    if (btn == NewOperationButton)
                    {
                        TypeOfActivity typeOfActivity = typeOfActivityViewSource.View.CurrentItem as TypeOfActivity;
                        Operation operation = new Operation { Name = "Новая операция", ProductionAreaID = null };
                        TypeOfActivityInOperation typeOfActivityInOperation = new TypeOfActivityInOperation { OperationID = operation.ID, TypeOfActivityID = typeOfActivity.ID, Operation = operation };
                        _ = _context.TypeOfActivityInOperations.Add(typeOfActivityInOperation);
                        typeOfActivity.TypeOfActivityInOperations.Add(typeOfActivityInOperation);
                        _context.Entry(typeOfActivityInOperation).Reference(toaino => toaino.TypeOfActivity).Load(); //загрузить связанные сущности через св-во наыигации
                        typeOfActivityViewSource.View.Refresh();
                        OperationsListBox.Items.Refresh();
                    }
                    if (btn == NewParameterButton)
                    {
                        TypeOfActivityInOperation typeOfActivityInOperation = OperationsListBox.SelectedItem as TypeOfActivityInOperation;
                        ParameterInOperation parameterInOperation = new ParameterInOperation
                        {
                            Name = "Новый параметер",
                            IsRefbookOnRequest = false,
                            OperationID = typeOfActivityInOperation.OperationID,
                            ReferencebookID = null,
                            UnitID = null
                        };
                        typeOfActivityInOperation.Operation.ParameterInOperations.Add(parameterInOperation);
                        _ = _context.ParameterInOperations.Add(parameterInOperation);
                        _context.Entry(parameterInOperation).Reference(p => p.Operation).Load(); //загрузить связанные сущности через св-во наыигации
                        OperationsListBox.SelectedItem = null;  //имитация смены выбранного элемента
                        OperationsListBox.SelectedItem = typeOfActivityInOperation; 
                        ParametersInOperationGrid.Items.Refresh();
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
                if (btn == NewOperationButton && typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && typeOfActivityViewSource.View.CurrentItem != null)
                {
                    e.CanExecute = true;
                }
                if (btn == NewParameterButton && typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && typeOfActivityViewSource.View.CurrentItem != null)
                {
                    if (OperationsListBox.Items.Count > 0 && OperationsListBox.SelectedItem is TypeOfActivityInOperation)
                    {
                        e.CanExecute = true;
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
            if (_context != null && typeOfActivityViewSource != null)
            {
                e.CanExecute = !ValidationChecker.HasInvalidRows(ParametersInOperationGrid);
            }
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && OperationsListBox.SelectedItem != null && OperationsListBox.SelectedItem is TypeOfActivityInOperation typeOfActivityInOperation)
            {
                try
                {
                    int nIndex = OperationsListBox.SelectedIndex;  //для имитация смены выбранного элемента
                    foreach (ParameterInOperation parameterInOperation in typeOfActivityInOperation.Operation.ParameterInOperations)
                    {
                        _ = _context.ParameterInOperations.Remove(parameterInOperation); //удаляем параметры операции
                    }
                    typeOfActivityInOperation.Operation.ParameterInOperations.Clear();
                    _ = _context.Operations.Remove(typeOfActivityInOperation.Operation); //удаляем саму операцию
                    TypeOfActivity typeOfActivity = typeOfActivityViewSource.View.CurrentItem as TypeOfActivity;
                    typeOfActivity.TypeOfActivityInOperations.Remove(typeOfActivityInOperation);
                    _ = _context.TypeOfActivityInOperations.Remove(typeOfActivityInOperation); //удаляем связку "операция - КВД"
                    typeOfActivityViewSource.View.Refresh();
                    OperationsListBox.Items.Refresh();
                    if (OperationsListBox.Items.Count > 0)
                    {
                        OperationsListBox.SelectedIndex = Math.Min(nIndex, OperationsListBox.Items.Count - 1);
                    }
                    else
                    {
                        OperationsListBox.SelectedItem = null;
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && ParametersInOperationGrid.SelectedItem != null && ParametersInOperationGrid.SelectedItem is ParameterInOperation parameterInOperation)
            {
                if (ParametersInOperationGrid.CancelEdit() && ParametersInOperationGrid.CancelEdit())
                {
                    if (parameterInOperation.ReferencebookID > 0)
                    {
                        parameterInOperation.ReferencebookID = null;
                        ParametersInOperationGrid.Items.Refresh();
                    }
                }
            }
        }

        private void ParametersInOperationGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context != null && typeOfActivityViewSource != null && typeOfActivityViewSource.View.CurrentItem != null &&
                ((DataGrid)sender).SelectedItem is ParameterInOperation parameterInOperation)
            {
                _ = _context.ParameterInOperations.Remove(parameterInOperation);
                TypeOfActivityInOperation  typeOfActivityInOperation = OperationsListBox.SelectedItem as TypeOfActivityInOperation;
                _ = typeOfActivityInOperation.Operation.ParameterInOperations.Remove(parameterInOperation);
                OperationsListBox.SelectedItem = null;  //имитация смены выбранного элемента
                OperationsListBox.SelectedItem = typeOfActivityInOperation;
                ParametersInOperationGrid.Items.Refresh();
            }
        }

        private void ReferencebookViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && typeOfActivityViewSource.View.CurrentItem != null)
            {
                TypeOfActivity typeOfActivity = typeOfActivityViewSource.View.CurrentItem as TypeOfActivity;
                e.Accepted = referencebookApplicabilities.
                    FindAll(referencebookApplicability => referencebookApplicability.TypeOfActivityID == typeOfActivity.ID) //список справочников для TypeOfActivity
                    .Any(rf => rf.ReferencebookID == (e.Item as Referencebook).ID); //среди списка справочников есть искомый
            }
        }

        private void ProductionAreaViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (typeOfActivityViewSource != null && typeOfActivityViewSource.View != null && typeOfActivityViewSource.View.CurrentItem != null)
            {
                TypeOfActivity typeOfActivity = typeOfActivityViewSource.View.CurrentItem as TypeOfActivity;
                e.Accepted = (e.Item as ProductionArea).TypeOfActivityInProdAreas.Any(toainpa => toainpa.TypeOfActivityID == typeOfActivity.ID);
            }
        }

        private void ListTypeOfActyvity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (referencebookViewSource != null && referencebookViewSource.View != null)
            {
                referencebookViewSource.View.Refresh();
            }
            if (productionAreaViewSource != null && productionAreaViewSource.View != null)
            {
                productionAreaViewSource.View.Refresh();
            }
        }
    }
}

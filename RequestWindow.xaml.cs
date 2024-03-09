using AdvertisementWpf.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
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
    /// Логика взаимодействия для RequestWindow.xaml
    /// </summary>
    public partial class RequestWindow : Window
    {
        private string _pathToFilesOfProduct = "";

        public RequestWindow(long nOrderID = 0, bool ViewMode = false)
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);
            InitializeComponent();
            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            DataContext = new OrderViewModel(nOrderID, lViewMode: ViewMode);

            MainWindow.statusBar.ClearStatus();
        }

        private void ListViewUpAndDown_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListView listView)
            {
                ItemContainerGenerator generator = listView.ItemContainerGenerator;
                //получить имя текущего(в фокусе) TextBox
                ListViewItem selectedItem = (ListViewItem)generator.ContainerFromItem(listView.SelectedItem);
                string textBoxFocusedName = "";
                if (GetFocusedDescendantByType(selectedItem, typeof(TextBox)) is TextBox textBox)
                {
                    textBoxFocusedName = textBox.Name;
                }
                if (e.Key == Key.Down)
                {
                    if (!listView.Items.MoveCurrentToNext())
                    {
                        _ = listView.Items.MoveCurrentToFirst();
                    }
                }
                else if (e.Key == Key.Up)
                {
                    if (!listView.Items.MoveCurrentToPrevious())
                    {
                        _ = listView.Items.MoveCurrentToLast();
                    }
                }
                else
                {
                    return;
                }
                selectedItem = (ListViewItem)generator.ContainerFromItem(listView.SelectedItem);
                if (GetDescendantByType(selectedItem, typeof(TextBox), textBoxFocusedName) is TextBox tbFind)
                {
                    _ = tbFind.Focus();
                }
            }
        }

        private static Visual GetFocusedDescendantByType(Visual element, Type type)
        {
            Visual foundElement = null;
            if (element != null)
            {
                if (element.GetType() == type && element is FrameworkElement fe && fe.IsFocused)
                {
                    return fe;
                }
                if (element is FrameworkElement)
                {
                    _ = (element as FrameworkElement).ApplyTemplate();
                }
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                    foundElement = GetFocusedDescendantByType(visual, type);
                    if (foundElement != null)
                    {
                        break;
                    }
                }
            }
            return foundElement;
        }

        private static Visual GetDescendantByType(Visual element, Type type, string name)
        {
            Visual foundElement = null;
            if (element != null)
            {
                if (element.GetType() == type)
                {
                    if (element is FrameworkElement fe && fe.Name == name)
                    {
                        return fe;
                    }
                }
                if (element is FrameworkElement)
                {
                    _ = (element as FrameworkElement).ApplyTemplate();
                }
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                    foundElement = GetDescendantByType(visual, type, name);
                    if (foundElement != null)
                    {
                        break;
                    }
                }
            }
            return foundElement;
        }

        private void QuantityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txtBox)
            {
                _ = ListProduct.Items.MoveCurrentTo(txtBox.GetBindingExpression(TextBox.TextProperty).DataItem);
            }
        }

        private void DesignerComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _ = ListProduct.Items.MoveCurrentTo(comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem);
            }
        }

        private void TextBoxError(object sender, ValidationErrorEventArgs e)
        {
            FieldInfo fieldInfo = DataContext.GetType().GetField("ErrorsCount"); //получить поле ErrorsCount из DataContext
            int ErrorsCount = (int)fieldInfo.GetValue(DataContext);
            if (e.Action == ValidationErrorEventAction.Added)
            {
                ErrorsCount++; //увеличить счетчик кол-ва ошибок
            }
            else
            {
                ErrorsCount--; //уменьшить счетчик кол-ва ошибок
            }
            fieldInfo.SetValue(DataContext, ErrorsCount); //изменить счетчик кол-ва ошибок
        }

        private void DatePicker_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            BindingOperations.GetBindingExpression(OrderStateTextBlock, TextBlock.TextProperty).UpdateTarget();
        }

        private void ValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txtBox)
            {
                _ = ListParametersInProduct.Items.MoveCurrentTo(txtBox.GetBindingExpression(TextBox.TextProperty).DataItem);
            }
            else if (sender is ComboBox comboBox)
            {
                _ = ListParametersInProduct.Items.MoveCurrentTo(comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("ReferencebookListSelectionChanged");
            _ = methodInfo?.Invoke(DataContext, new object[] { (sender as ComboBox).GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem });
        }

        private void ValueTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("ProductParametersSourceUpdated");
            _ = methodInfo?.Invoke(DataContext, new object[] { ListProduct.SelectedItem });
        }

        private void TypeOfActivityCostTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("TypeOfActivityCostSourceUpdated");
            _ = methodInfo?.Invoke(DataContext, new object[] { ListProduct.SelectedItem });
            BindingOperations.GetBindingExpression(OrderTotalCosts, TextBlock.TextProperty).UpdateTarget();
        }

        private void TypeOfActivityCostTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txtBox)
            {
                _ = CostOfProduct.Items.MoveCurrentTo(txtBox.GetBindingExpression(TextBox.TextProperty).DataItem);
            }
        }

        private void ListPayments_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPaymentsNewChangeDelete"))
            {
                MethodInfo methodInfo = DataContext.GetType().GetMethod("DeletePayment");
                _ = methodInfo?.Invoke(DataContext, new object[] { ListPayments.SelectedItem });
                PaymentSourceUpdated();
            }
        }

        private void PaymentSourceUpdated()
        {
            BindingOperations.GetBindingExpression(TotalPaymentsTextBlock, TextBlock.TextProperty).UpdateTarget();
        }

        private void PaymentAmountTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            PaymentSourceUpdated();
        }

        private void AccountsListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("AccountsListComboBoxSelectionChanged");
            _ = methodInfo?.Invoke(DataContext, new object[] { ListPayments.SelectedItem });
            ListPayments.Items.Refresh();
        }

        private void ContractorNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("ComntactorNameComboBoxSelectionChanged");
            _ = methodInfo?.Invoke(DataContext, new object[] { ContractorNameComboBox.SelectedItem, ListAccount.SelectedItem });
            int nSelectedIndex = ListAccount.SelectedIndex;
            ListAccount.SelectedIndex = -1;
            ListAccount.SelectedIndex = nSelectedIndex;             //имитация изменения SelectedItem
        }

        private void ListAct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"))
            {
                MethodInfo methodInfo = DataContext.GetType().GetMethod("DeleteAct");
                _ = methodInfo?.Invoke(DataContext, new object[] { ListAccount.SelectedItem, ListAct.SelectedItem });
                int nSelectedIndex = ListAccount.SelectedIndex;
                ListAccount.SelectedIndex = 0;
                ListAccount.SelectedIndex = nSelectedIndex;             //имитация изменения SelectedItem
            }
        }

        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("AccountDetailTextBoxChanged");
            _ = methodInfo?.Invoke(DataContext, new object[] { ListAccount.SelectedItem });
        }

        private void TechCardTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //NewOperationListBox.Visibility = Visibility.Collapsed;
            //NewOperationListBoxRow.Height = GridLength.Auto;
        }

        private void TechCardTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                //    if (TechCardTreeView.SelectedItem is TechCard techCard)
                //    {
                //        foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                //        {
                //            foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                //            {
                //                _ = _context_.OperationInWorks.Remove(operationInWork);
                //            }
                //            _ = _context_.WorkInTechCards.Remove(workInTechCard);
                //        }
                //        techCard.Product.IsHasTechcard = false; //убрать признак наличия Техкарты
                //        techCard.Product.DateTransferProduction = null; //убрать дату передачи в производство
                //        techCard.Product.DateManufacture = null; //убрать дату Изготовления
                //        _ = _context_.TechCards.Remove(techCard);
                //        Product product = _context.Products.Local.First(Product => Product.ID == techCard.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                //        product.IsHasTechcard = false;
                //        product.DateTransferProduction = null; //убрать дату передачи в производство
                //        product.DateManufacture = null; //убрать дату Изготовления
                //    }
                //    else if (TechCardTreeView.SelectedItem is WorkInTechCard workInTechCard1)
                //    {
                //        foreach (OperationInWork operationInWork in workInTechCard1.OperationInWorks)
                //        {
                //            _ = _context_.OperationInWorks.Remove(operationInWork);
                //        }
                //        TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard1, 0);
                //        _ = _context_.WorkInTechCards.Remove(workInTechCard1);
                //        _ = tc.WorkInTechCards.Remove(workInTechCard1);
                //        tc.WorkInTechCards_ = null;
                //        Product product = _context.Products.Local.First(Product => Product.ID == tc.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                //        product.DateManufacture = null; //убрать дату Изготовления
                //    }
                //    else if (TechCardTreeView.SelectedItem is OperationInWork operationInWork1)
                //    {
                //        WorkInTechCard wTC = (WorkInTechCard)GetParentTreeViewItem(operationInWork1, 1);
                //        _ = _context_.OperationInWorks.Remove(operationInWork1);
                //        _ = wTC.OperationInWorks.Remove(operationInWork1);
                //        wTC.OperationInWorks_ = null;
                //    }
            }
        }

        private void TechCardTreeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {            
            MethodInfo methodInfo = DataContext.GetType().GetMethod("CheckTechCard");
            e.Handled = (bool)methodInfo?.Invoke(DataContext, new object[] { (sender as TextBlock).DataContext });
        }

        private void WorkInTechCardTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MethodInfo methodInfo = DataContext.GetType().GetMethod("CheckWorkInTechCard");
            e.Handled = (bool)methodInfo?.Invoke(DataContext, new object[] { sender, e.ClickCount });
        }

        private void WorkTypeOfActivityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox?.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem != null)
            {
                MethodInfo methodInfo = DataContext.GetType().GetMethod("WorkTypeOfActivityComboBoxSelectionChanged");
                _ = methodInfo?.Invoke(DataContext, new object[] { comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem });
            }
        }

        private void WorkTypeOfActivityComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox?.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem != null)
            {
                MethodInfo methodInfo = DataContext.GetType().GetMethod("WorkTypeOfActivityComboBoxGotFocus");
                _ = methodInfo?.Invoke(DataContext, new object[] { comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem });
            }
        }

        private void WorkInTechCardComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox?.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem != null)
            {
                comboBox.Visibility = Visibility.Collapsed; //скрыть самого себя
                MethodInfo methodInfo = DataContext.GetType().GetMethod("WorkInTechCardComboBoxDropDownClosed");
                _ = methodInfo?.Invoke(DataContext, new object[] { comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem });
                object selectedItem = TechCardTreeView.SelectedItem;
                TechCardTreeView.Items.Refresh();
                methodInfo = DataContext.GetType().GetMethod("WorkInTechCardSetSelected");
                _ = methodInfo?.Invoke(DataContext, new object[] { selectedItem });
            }
        }

    }
}

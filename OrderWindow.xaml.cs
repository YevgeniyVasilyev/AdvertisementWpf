using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AdvertisementWpf.Models;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Collections;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.RegularExpressions;
using System.Text;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для Order.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        private CollectionViewSource productsViewSource, ordersViewSource, usersViewSource, designersViewSource, clientsViewSource, productTypesViewSource, parametersInProductViewSource,
            filesListViewSource, productCostsViewSource, paymentsViewSource, purposeOfPaymentsViewSource, typeOfPaymentsViewSource, totalProductCostsViewSource, accountsViewSource,
            contractorsViewSource, techCardsViewSource, operationsViewSource, accountsListViewSource;
        private App.AppDbContext _context, __context, ___context, context_, context__, _context_;
        private List<ReferencebookParameter> referencebookParameters;
        private List<Referencebook> referencebooks;
        private List<ReferencebookApplicability> referencebookApplicabilities;
        public static IEnumerable<TypeOfActivity> typeOfActivityList;
        //public static IEnumerable<Operation> operationList;
        public static IEnumerable<User> designerList;
        private Order currentOrder;

        private bool _isEditMode;
        private bool _isNewOrder;
        private string _pathToFilesOfProduct = "";

        public bool IsNeedUpdate = false;

        public OrderWindow(bool NewOrder = false, long nOrderID = 0, bool EditMode = false)
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();

            _isNewOrder = NewOrder;
            _isEditMode = EditMode;
            _isEditMode = _isNewOrder || _isEditMode; //для нового заказа всегда режим правки

            SaveOrderButton.Tag = !_isEditMode;

            Title = _isEditMode ? "Заказ (редактирование)" : _isNewOrder ? "Заказ (новый)" : "Заказ (просмотр)";

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            productsViewSource = (CollectionViewSource)FindResource(nameof(productsViewSource)); //найти описание view в разметке
            ordersViewSource = (CollectionViewSource)FindResource(nameof(ordersViewSource));
            usersViewSource = (CollectionViewSource)FindResource(nameof(usersViewSource));
            designersViewSource = (CollectionViewSource)FindResource(nameof(designersViewSource));
            clientsViewSource = (CollectionViewSource)FindResource(nameof(clientsViewSource));
            productTypesViewSource = (CollectionViewSource)FindResource(nameof(productTypesViewSource));
            paymentsViewSource = (CollectionViewSource)FindResource(nameof(paymentsViewSource));
            purposeOfPaymentsViewSource = (CollectionViewSource)FindResource(nameof(purposeOfPaymentsViewSource));
            typeOfPaymentsViewSource = (CollectionViewSource)FindResource(nameof(typeOfPaymentsViewSource));
            totalProductCostsViewSource = (CollectionViewSource)FindResource(nameof(totalProductCostsViewSource));
            accountsViewSource = (CollectionViewSource)FindResource(nameof(accountsViewSource));
            contractorsViewSource = (CollectionViewSource)FindResource(nameof(contractorsViewSource));
            techCardsViewSource = (CollectionViewSource)FindResource(nameof(techCardsViewSource));
            operationsViewSource = (CollectionViewSource)FindResource(nameof(operationsViewSource));
            accountsListViewSource = (CollectionViewSource)FindResource(nameof(accountsListViewSource));

            ___context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring); //для ProductCosts
            __context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);  //для Orders
            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);   //все остальное (кроме платежей, ПУД)
            context_ = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);   //платежи
            context__ = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);   //ПУД
            _context_ = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);   //техкарты
            try
            {
                _pathToFilesOfProduct = _context.Setting.AsNoTracking().Where(Setting => Setting.SettingParameterName == "PathToFilesOfProduct").FirstOrDefault().SettingParameterValue;
                //designersViewSource.Source = _context.Users.AsNoTracking().ToList().Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListDesigner")); //ListDesigner
                designerList = _context.Users.AsNoTracking().ToList().Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListDesigner")); //ListDesigner
                typeOfActivityList = _context_.TypeOfActivitys.AsNoTracking().ToList();

                DataContext = new ViewModel();

                operationsViewSource.Source = _context_.Operations
                    .Include(Operation => Operation.ParameterInOperations)
                    .Include(Operation => Operation.TypeOfActivityInOperations)
                    .AsNoTracking().ToList();
                if (_isNewOrder || _isEditMode)
                {
                    usersViewSource.Source = _context.Users.AsNoTracking().ToList().Where(u => IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, u.RoleID, "ListManager"));  // ListManager
                    clientsViewSource.Source = _context.Clients.AsNoTracking().ToList();

                    //_ = __context.Orders.Where(Order => Order.ID == nOrderID).Include(Order => Order.OrderEntered).FirstOrDefault();
                    _ = __context.Orders.Where(Order => Order.ID == nOrderID).FirstOrDefault();
                    ordersViewSource.Source = __context.Orders.Local.ToObservableCollection();
                    if (_isNewOrder)
                    {
                        _ = _context.Products.Where(Product => Product.OrderID == nOrderID).FirstOrDefault();
                        _ = clientsViewSource.View.MoveCurrentToFirst();
                        Order order = new Order
                        {
                            Number = "",
                            ClientID = (clientsViewSource.View.CurrentItem as Client)?.ID ?? 0,
                            DateAdmission = DateTime.Today
                        };
                        order.OrderEnteredID = MainWindow.Userdata.ID;
                        OrderEntered.Text = MainWindow.Userdata.FullUserName;
                        order.ManagerID = MainWindow.Userdata.ID;
                        _ = __context.Orders.Add(order);
                        _ = ordersViewSource.View.MoveCurrentTo(order);
                    }
                    else
                    {
                        _context.Products.Where(Product => Product.OrderID == nOrderID).Include(Product => Product.ProductType).Load();
                        OrderEntered.Text = _context.Users.Where(Users => Users.ID == ((Order)ordersViewSource.View.CurrentItem).OrderEnteredID).AsNoTracking().FirstOrDefault().FullUserName;
                        _ = ordersViewSource.View.MoveCurrentToFirst();
                    }
                    productsViewSource.Source = _context.Products.Local.ToObservableCollection();
                }
                else
                {
                    ordersViewSource.Source = __context.Orders.AsNoTracking().Where(Order => Order.ID == nOrderID).Include(Order => Order.OrderEntered).ToList();
                    OrderEntered.Text = ((Order)ordersViewSource.View.CurrentItem).OrderEntered.FullUserName;
                    _ = ordersViewSource.View.MoveCurrentToFirst();
                    usersViewSource.Source = _context.Users.AsNoTracking().Where(User => User.ID == (ordersViewSource.View.CurrentItem as Order).ManagerID).ToList();
                    clientsViewSource.Source = _context.Clients.AsNoTracking().Include(Client => Client.User).Where(Client => Client.ID == (ordersViewSource.View.CurrentItem as Order).ClientID).ToList();
                    productsViewSource.Source = _context.Products.AsNoTracking().Where(Product => Product.OrderID == nOrderID).Include(Product => Product.ProductType).ToList();
                }
                //далее идут действия для любого режима ("новый", "правка", "просмотр")
                DateAdmissionDate.IsEnabled = _isNewOrder;
                productTypesViewSource.Source = _context.ProductTypes.AsNoTracking().Include(ProductTypes => ProductTypes.CategoryOfProduct).ToList();
                referencebooks = _context.Referencebook.AsNoTracking().ToList();
                referencebookParameters = _context.ReferencebookParameter.AsNoTracking().ToList();
                referencebookApplicabilities = _context.ReferencebookApplicability.AsNoTracking().ToList();
                foreach (Product product in productsViewSource.View) //проход по изделиям 
                {
                    GetProductParameters(product);
                    product.FilesToList();
                    GetProductCosts(product);
                    //product.ProductState();
                }
                currentOrder = ordersViewSource.View.CurrentItem as Order;
                currentOrder.State = currentOrder.OrderState(productsViewSource);
                _ = CountTotals();
                LoadOrderPayments();
                totalProductCostsViewSource.Source = TotalProductCostsList();
                parametersInProductViewSource = (CollectionViewSource)FindResource(nameof(parametersInProductViewSource));
                filesListViewSource = (CollectionViewSource)FindResource(nameof(filesListViewSource));
                productCostsViewSource = (CollectionViewSource)FindResource(nameof(productCostsViewSource));
                productsViewSource.View.CurrentChanged += ProductsViewSourceView_CurrentChanged;
                _ = productsViewSource.View.MoveCurrentToFirst();
                ProductsViewSourceView_CurrentChanged(null, null);
                contractorsViewSource.Source = context__.Contractors.AsNoTracking().ToList();
                LoadTechCard();
                operationsViewSource.Filter += OperationsViewSource_Filter;
                clientsViewSource.Filter += ClientsViewSource_Filter;
                productTypesViewSource.Filter += ProductTypesViewSource_Filter;
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
            if (__context != null)
            {
                __context.Dispose();
                __context = null;
            }
            if (___context != null)
            {
                ___context.Dispose();
                ___context = null;
            }
            if (context_ != null)
            {
                context_.Dispose();
                context_ = null;
            }
            if (context__ != null)
            {
                context__.Dispose();
                context__ = null;
            }
            if (_context_ != null)
            {
                _context_.Dispose();
                _context_ = null;
            }
            MainWindow.statusBar.ClearStatus();
        }

        private void ProductsViewSourceView_CurrentChanged(object sender, EventArgs e)
        {
            if (productsViewSource.View.CurrentItem is Product product)
            {
                parametersInProductViewSource.Source = product.ProductParameter;
                filesListViewSource.Source = product.FilesList;
                productCostsViewSource.Source = product.Costs;
                //product.ProductState();
            }
        }

        private void GetProductParameters(Product product)
        {
            IQueryable<ProductParameters> parameterQuery;
            parameterQuery = from pinpt in _context.ParameterInProductTypes
                             where pinpt.ProductTypeID == product.ProductTypeID
                             join u in _context.Units on pinpt.UnitID equals u.ID
                             select new ProductParameters { ID = pinpt.ID, Name = pinpt.Name, IsRequired = pinpt.IsRequired, UnitName = u.Name, ReferencebookID = pinpt.ReferencebookID,
                                 IsRefbookOnRequest = pinpt.IsRefbookOnRequest };
            product.ProductParameter = parameterQuery.AsNoTracking().ToList();
            product.ParametersToList(); //заполнить список параметров
            foreach (ProductParameters productParameter in product.ProductParameter) //проход по параметрам
            {
                if (productParameter.IsRefbookOnRequest) //задан параметр "выбор справочника по запросу"
                {
                    long? categoryOfProductID = 0;
                    categoryOfProductID = _context.ProductTypes.FirstOrDefault(pt => pt.ID == product.ProductTypeID).CategoryOfProductID;
                    if (categoryOfProductID is null)
                    {
                        categoryOfProductID = 0;
                    }
                    IEnumerable<ReferencebookApplicability> enumerable = referencebookApplicabilities.Where(refApp => refApp.CategoryOfProductID == categoryOfProductID);
                    productParameter.ReferencebookList = referencebooks.FindAll(
                        delegate (Referencebook referencebook)
                        {
                            return enumerable.Any(e => e.ReferencebookID == referencebook.ID); //найти в referencebooks все ID (справочники), которые применяются для CategoryOfProduct
                        }
                        );
                }
                if (productParameter.ReferencebookID > 0) //для параметра установлено брать значение из справочника 
                {
                    productParameter.ReferencebookParametersList = referencebookParameters.Where(refbookParameters => refbookParameters.ReferencebookID == productParameter.ReferencebookID).ToList();
                }
            }
        }

        private void GetProductCosts(Product product)
        {
            List<TypeOfActivityInProduct> typeOfActivityInProduct;
            typeOfActivityInProduct = _context.TypeOfActivityInProducts.AsNoTracking()
                .Where(TypeOfActivityInProducts => TypeOfActivityInProducts.ProductTypeID == product.ProductTypeID)
                .Include(TypeOfActivityInProducts => TypeOfActivityInProducts.TypeOfActivity).ToList();
            List<ProductCost> costs;
            costs = _context.ProductCosts.Where(ProductCost => ProductCost.ProductID == product.ID).Include(ProductCost => ProductCost.TypeOfActivity).ToList();
            foreach (TypeOfActivityInProduct tainp in typeOfActivityInProduct) //проход по видам деятельности, содержащихся в издеии (его виде)
            {
                bool lFind = false;
                foreach (ProductCost productCost in costs) //ищем вид деятельности в стоимости изделия
                {
                    if (tainp.TypeOfActivityID == productCost.TypeOfActivityID)
                    {
                        lFind = true;
                        break;
                    }
                }
                if (!lFind) //вида деятельности нет в стоимости изделия
                {
                    ProductCost pc = new ProductCost
                    {
                        ID = 0,
                        ProductID = product.ID,
                        TypeOfActivityID = tainp.TypeOfActivityID,
                        Cost = 0,
                        Name = tainp.TypeOfActivity.Name,
                        Code = tainp.TypeOfActivity.Code
                    };
                    costs.Add(pc);
                }
            }
            //product.Costs = costs.OrderBy(cost => cost.TypeOfActivityID).ToList();
            product.Costs = costs;
            if (_isEditMode && product.ID > 0) //отслеживать только для режима правки
            {
                ___context.ProductCosts.AttachRange(product.Costs);
            }
        }

        private void NewProductListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NewProductGrid.Visibility = Visibility.Collapsed;
            //NewProductListBox.Visibility = Visibility.Collapsed;
            //ListBoxTypeOfProductsRow.Height = GridLength.Auto;
            if (CreateNewProduct() is Product product)
            {
                _context.Products.Add(product);
                _ = CountTotals();
                _ = productsViewSource.View.MoveCurrentTo(product);
                productsViewSource.View.Refresh();
            }
        }

        private Product CreateNewProduct()
        {
            if (productTypesViewSource.View.CurrentItem is ProductType pt)
            {
                Product product = new Product
                {
                    ProductTypeID = pt.ID,
                    Parameters = "",
                    Cost = 0.00M,
                    Note = "",
                    Files = "",
                    Quantity = 1,
                    Costs = new List<ProductCost> { },
                    ProductTypeName = pt.Name
                };
                product.FilesToList();
                GetProductParameters(product);
                GetProductCosts(product);
                currentOrder.State = currentOrder.OrderState(productsViewSource);
                return product;
            }
            return null;
        }

        private string GenerateNewOrderNumber(long Number)
        {
            return Number.ToString().Trim().PadLeft(10, '0');
        }

        private void ValueTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (productsViewSource.View.CurrentItem is Product product)
            {
                product.ListToParameters();
            }
        }

        private void QuantityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (productsViewSource.View != null && sender is TextBox txtBox && txtBox.GetBindingExpression(TextBox.TextProperty).DataItem is Product product)
            {
                _ = productsViewSource.View.MoveCurrentTo(product);
            }
        }

        private void ValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (parametersInProductViewSource.View != null)
            {
                if (sender is TextBox txtBox && txtBox.GetBindingExpression(TextBox.TextProperty).DataItem is ProductParameters productParameters)
                {
                    _ = parametersInProductViewSource.View.MoveCurrentTo(productParameters);
                    return;
                }
                if (sender is ComboBox comboBox && comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is ProductParameters productParameter)
                {
                    _ = parametersInProductViewSource.View.MoveCurrentTo(productParameter);
                }
            }
        }

        private void DesignerComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (productsViewSource != null && productsViewSource.View != null)
            {
                if (sender is ComboBox comboBox && comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is Product product)
                {
                    _ = productsViewSource.View.MoveCurrentTo(product);
                }
            }
        }

        private void TypeOfActivityCostTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (productCostsViewSource.View != null && sender is TextBox txtBox && txtBox.GetBindingExpression(TextBox.TextProperty).DataItem is ProductCost productCost)
            {
                _ = productCostsViewSource.View.MoveCurrentTo(productCost);
            }
        }

        private void TypeOfActivityCostTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            decimal nTotalCostOnTypeOfActivity = 0;
            if (productsViewSource.View.CurrentItem is Product product)
            {
                foreach (ProductCost productCost in product.Costs) //проход по стоимостям продукта
                {
                    nTotalCostOnTypeOfActivity += productCost.Cost;
                }
                product.Cost = nTotalCostOnTypeOfActivity;
                productsViewSource.View.Refresh();
                _ = CountTotals();
                totalProductCostsViewSource.Source = TotalProductCostsList();
            }
        }

        //private bool ValidTotalCostsOnTypeOfActivity()
        //{
        //    bool lResult = true;
        //    decimal nResult;
        //    decimal nTotalCostOnTypeOfActivity = 0;
        //    foreach (Product product in productsViewSource.View) //проход по продуктам
        //    {
        //        nResult = 0;
        //        foreach (ProductCost productCost in product.Costs) //проход по стоимостям продукта
        //        {
        //            nTotalCostOnTypeOfActivity += productCost.Cost;
        //            nResult += productCost.Cost;
        //        }
        //        lResult &= product.Cost >= nResult;
        //    }
        //    nResult = decimal.TryParse(TotalCost.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("ru-RU"), out nResult) ? nResult : 0;
        //    lResult &= nTotalCostOnTypeOfActivity <= nResult;
        //    return lResult;
        //}

        private decimal CountTotals()
        {
            decimal nTotalCost = 0;
            foreach (Product product in productsViewSource.View)
            {
                nTotalCost += product.Cost;
            }
            //decimal nTotalCost = _context.Products.Local.Sum(Product => Product.Cost);
            TotalCost.Text = nTotalCost.ToString("C", CultureInfo.GetCultureInfo("ru-RU"));
            return nTotalCost;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_context != null && __context != null)
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Сохранение данных ...", Cursors.Wait);
                    if (e.OriginalSource == SaveOrderButton)
                    {
                        _ = __context.SaveChanges(); //сохранить Orders
                        Order order = ordersViewSource.View.CurrentItem as Order;
                        Title = "Заказ (редактирование)";
                        if (string.IsNullOrWhiteSpace(order.Number)) //для нового заказа фиксируем его номер и ID для изделий
                        {
                            order.Number = GenerateNewOrderNumber(order.ID);
                            _ = __context.SaveChanges();
                        }
                        foreach (Product product in productsViewSource.View)
                        {
                            if (product.OrderID == 0)
                            {
                                product.OrderID = order.ID;
                            }
                        }
                        _ = _context.SaveChanges(); //сохранить все остальное
                        foreach (Product product in productsViewSource.View)
                        {
                            foreach (ProductCost productCost in product.Costs)
                            {
                                if (productCost.ProductID == 0)
                                {
                                    productCost.ProductID = product.ID;
                                }
                                if (productCost.ID == 0) //новая сумма в стоимости изделия
                                {
                                    ___context.ProductCosts.Add(productCost);
                                }
                            }
                        }
                        _ = ___context.SaveChanges(); //сохранить ProductCosts
                        MainWindow.RefreshOrderProduct();
                    }
                    if (e.OriginalSource == SavePaymentButton)
                    {
                        _ = context_.SaveChanges();

                    }
                    if (e.OriginalSource == SavePADButton)
                    {
                        int nSelectedIndex = ListAccount.SelectedIndex;
                        _ = context__.SaveChanges();
                        if (accountsViewSource.View.CurrentItem is Account account && (account.Order is null || account.Contractor is null))
                        {
                            LoadOrderAccounts(false);
                        }
                        ListAccount.SelectedIndex = -1;
                        ListAccount.SelectedIndex = Math.Min(nSelectedIndex, ListAccount.Items.Count - 1);
                    }
                    if (e.OriginalSource == SaveTechCardButton)
                    {   //формируем номера для использования при печати техкарты
                        //«номер заказа».«порядковый номер изделия в заказе».«КВД».«порядковый номер операции»
                        short nTechCard = 1;
                        short nOperationInWork = 1;
                        foreach (TechCard techCard in techCardsViewSource.View)
                        {
                            techCard.Number = $"{currentOrder.Number.TrimStart('0')}.{nTechCard++}";
                            foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                            {
                                workInTechCard.Number = $"{techCard.Number}.{workInTechCard.TypeOfActivity.Code.Trim()}";
                                nOperationInWork = 1;
                                foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                                {
                                    operationInWork.Number = $"{workInTechCard.Number}.{nOperationInWork++}";
                                }
                            }
                        }
                        _ = _context_.SaveChanges();
                    }
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
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                Button btn = e.OriginalSource as Button;
                if (btn == SavePaymentButton && context_ != null && currentOrder != null && currentOrder.ID > 0 && paymentsViewSource != null && paymentsViewSource.View != null && !ValidationChecker.HasInvalidTextBox(PaymentAmountTextBox) 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPaymentsNewChangeDelete"))
                {
                    e.CanExecute = true;
                    return;
                }
                if (btn == SavePADButton && context__ != null && currentOrder != null && currentOrder.ID > 0 && accountsViewSource != null && accountsViewSource.View != null && !ValidationChecker.HasInvalidTextBox(PADGrid) 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"))
                {
                    e.CanExecute = true;
                    return;
                }
                if (e.OriginalSource == SaveTechCardButton && _context_ != null && currentOrder != null && currentOrder.ID > 0 && techCardsViewSource?.View != null && TechCardTreeView != null && TechCardTreeView.Items.Count > 0 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"))
                {
                    e.CanExecute = true;
                    return;
                }
                
                if (!_isEditMode)
                {
                    e.CanExecute = false;
                    return;
                }
                if (btn == SaveOrderButton)
                {
                    if (_context != null)
                    {
                        bool lCanExecute = true;
                        if (!HasInvalidListView())
                        {
                            lCanExecute = false;
                        }
                        e.CanExecute = lCanExecute;
                    }
                    else
                    {
                        e.CanExecute = false;
                    }
                }
            }
        }

        private void DatePicker_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (productsViewSource.View.CurrentItem is Product)
            {
                //product.State = product.ProductState();
                currentOrder.State = currentOrder.OrderState(productsViewSource);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null)
            {
                Referencebook refBook = e.AddedItems[0] as Referencebook;
                if (productsViewSource != null && productsViewSource.View != null &&
                    comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is ProductParameters productParameter)
                {
                    productParameter.ReferencebookID = refBook.ID;
                    productParameter.ReferencebookParametersList = referencebookParameters.Where(refbookParameters => refbookParameters.ReferencebookID == productParameter.ReferencebookID).ToList();
                }
            }
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                Button btn = e.OriginalSource as Button;
                if (btn == NewPaymentButton && context_ != null)
                {
                    _ = context_.Payments.Add(NewPayment());
                    _ = paymentsViewSource.View.MoveCurrentToLast();
                }
                //if (btn == LoadPaymentButton && context_ != null)
                //{
                //    LoadOrderPayments();
                //}
                else if (btn == NewAccountButton)
                {
                    CreateNewAccount();
                }
                else if (btn == LoadPADButton)
                {
                    LoadOrderAccounts();
                }
                else if (btn == NewActButton)
                {
                    CreateNewAct();
                }
                else if (btn == LoadTechCardButton)
                {
                    LoadTechCard(true);
                    return;
                }
                else if (btn == AddTechCardButton)
                {
                    AddNewObjectInTechCard("TechCard");
                    return;
                }
                else if (btn == AddTechCardWorkButton)
                {
                    AddNewObjectInTechCard("Work");
                    return;
                }
                else if (btn == AddTechCardOperationButton)
                {
                    NewOperationListBoxRow.Height = new GridLength(1, GridUnitType.Star);
                    NewOperationListBox.Visibility = Visibility.Visible;
                    operationsViewSource.View.Refresh(); //для активации фильтра
                    return;
                }
                else if (btn == SendTechCardButton)
                {
                    SendTechCardToProduction();
                    return;
                }
                else if (btn == RecallTechCardButton)
                {
                    RecallTechCardFromProduction();
                    return;
                }
                else  if (btn == NewOperationFileButton && TechCardTreeView != null && TechCardTreeView.SelectedItem is OperationInWork operationInWork)
                {
                    try
                    {

                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Multiselect = true;
                        if ((bool)openFileDialog.ShowDialog()) // Открываем окно диалога с пользователем
                        {
                            foreach (string fullFileName in openFileDialog.FileNames)
                            {
                                operationInWork.FilesList.Add(Path.GetFullPath(fullFileName));
                            }
                            operationInWork.ListToFiles();
                            OperationInWorkFilesListBox.Items.Refresh();
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка выбора папки", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                if (!_isEditMode)
                {
                    return;
                }
                if (btn == NewProductButton)
                {
                    NewProductGrid.Visibility = Visibility.Visible;
                    //ListBoxTypeOfProductsRow.Height = new GridLength(1, GridUnitType.Star);
                    //NewProductListBox.Visibility = Visibility.Visible;
                }
                if (btn == NewFileButton)
                {
                    try
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Multiselect = true;
                        if ((bool)openFileDialog.ShowDialog()) // Открываем окно диалога с пользователем
                        {
                            if (productsViewSource.View.CurrentItem is Product product)
                            {
                                foreach (string fullFileName in openFileDialog.FileNames)
                                {
                                    product.FilesList.Add(Path.GetFileName(fullFileName));
                                }
                                product.ListToFiles();
                                filesListViewSource.View.Refresh();
                                CopyFileToPathToFilesOfProduct(openFileDialog.FileNames);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка выбора папки", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                Button btn = e.OriginalSource as Button;
                //if (btn == LoadPaymentButton && context_ != null)
                //{
                //    e.CanExecute = true;
                //    return;
                //}
                if (btn == NewPaymentButton && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPaymentsNewChangeDelete"))
                {
                    if (context_ != null && currentOrder != null && currentOrder.ID > 0 && paymentsViewSource?.View != null)
                    {
                        e.CanExecute = true;
                        return;
                    }
                }
                else if (btn == NewAccountButton && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"))
                {
                    if (currentOrder != null && currentOrder.ID > 0)
                    {
                        e.CanExecute = true;
                        return;
                    }
                }
                else if (btn == NewActButton && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"))
                {
                    if (accountsViewSource?.View != null && accountsViewSource.View.CurrentItem is Account && CanCreateNewAct())
                    {
                        e.CanExecute = true;
                        return;
                    }
                }
                else if (btn == LoadPADButton && currentOrder != null && currentOrder.ID > 0)
                {
                    e.CanExecute = true;
                    return;
                }
                else if (btn == LoadTechCardButton && currentOrder != null && currentOrder.ID > 0)
                {
                    e.CanExecute = true;
                    return;
                }
                else if (btn == AddTechCardButton && currentOrder != null && currentOrder.ID > 0 && techCardsViewSource != null && techCardsViewSource.View != null 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"))
                {
                    e.CanExecute = true;
                    return;
                }
                else if (btn == AddTechCardWorkButton && techCardsViewSource?.View != null && TechCardTreeView.SelectedItem != null && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"))
                {
                    e.CanExecute = true;
                    return;
                }
                else if (btn == AddTechCardOperationButton && techCardsViewSource?.View != null && TechCardTreeView.SelectedItem != null && (TechCardTreeView.SelectedItem is WorkInTechCard || TechCardTreeView.SelectedItem is OperationInWork) 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardNewChangeDelete"))
                {
                    e.CanExecute = true;
                    return;
                }
                else if (btn == SendTechCardButton && techCardsViewSource?.View != null && TechCardTreeView?.SelectedItem != null 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardTransferToProduction"))
                {
                    TechCard techCard = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0); //ищем корневого родителя на уровне TechCard
                    if (techCard != null && !techCard.Product.DateTransferProduction.HasValue) //если дата передачи в производство не установлена
                    {
                        e.CanExecute = true;
                    }
                    return;
                }
                else if (btn == RecallTechCardButton && techCardsViewSource?.View != null && TechCardTreeView?.SelectedItem != null 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardRecallFromProduction"))
                {
                    TechCard techCard = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0); //ищем корневого родителя на уровне TechCard
                    if (techCard != null && techCard.Product.DateTransferProduction.HasValue) //если дата передачи в производство установлена
                    {
                        e.CanExecute = true;
                    }
                    return;
                }
                else if (btn == NewOperationFileButton && techCardsViewSource?.View != null && TechCardTreeView != null && TechCardTreeView.SelectedItem is OperationInWork)
                {
                    e.CanExecute = true;
                    return;
                }

                if (!_isEditMode)
                {
                    e.CanExecute = false;
                    return;
                }
                if (btn.Name == "NewProductButton" && productsViewSource != null)
                {
                    e.CanExecute = true;
                }
                if (btn.Name == "NewFileButton" && productsViewSource != null && filesListViewSource != null && ordersViewSource != null)
                {
                    e.CanExecute = !string.IsNullOrWhiteSpace((ordersViewSource.View.CurrentItem as Order).Number) && ListProduct.Items.Count > 0;
                }
            }
        }

        private void LoadOrderPayments()
        {
            MainWindow.statusBar.WriteStatus("Загрузка платежей ...", Cursors.Wait);
            try
            {
                Payment payment = new Payment { PaymentDate = DateTime.Now };
                context_ = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);   //платежи, ПУД
                context_.Payments.Where(Payment => Payment.OrderID == currentOrder.ID)
                    .Include(Payment => Payment.Account)
                    .Load();
                paymentsViewSource.Source = context_.Payments.Local.ToObservableCollection();
                accountsListViewSource.Source = context_.Accounts.AsNoTracking().Where(account => account.OrderID == currentOrder.ID).ToList();
                foreach (Payment p in paymentsViewSource.View)
                {
                    p.Account?.DetailsToList(); //для каждого счета распаковать его детали
                }
                purposeOfPaymentsViewSource.Source = payment.ListPurposeOfPayment;
                typeOfPaymentsViewSource.Source = payment.ListTypeOfPayment;
                accountsListViewSource.Filter += AccountsListViewSource_Filter;
                paymentsViewSource.View.CurrentChanged += PaymentsViewSource_CurrentChanged;
                _ = paymentsViewSource.View.MoveCurrentToFirst();
                //_ = MessageBox.Show("   Все платежи загружены успешно!   ", "Загрузка платежей");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки платежей", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void PaymentsViewSource_CurrentChanged(object sender, EventArgs e)
        {
            if (accountsListViewSource?.View != null)
            {
                accountsListViewSource.View.Refresh(); //инициализация фильтра
                if (paymentsViewSource?.View?.CurrentItem is Payment payment)
                {
                    AccountsListComboBox.SelectedValue = payment.AccountID;
                }
            }
        }

        private Payment NewPayment()
        {
            return new Payment
            {
                OrderID = currentOrder.ID,
                //PaymentDate = Now,
                PaymentAmount = 0,
                PaymentDocNumber = "",
                PurposeOfPayment = 0,
                TypeOfPayment = 0
            };
        }

        private void ListPayments_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && context_ != null && paymentsViewSource?.View?.CurrentItem is Payment payment 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPaymentsNewChangeDelete"))
            {
                accountsListViewSource.Filter -= AccountsListViewSource_Filter;
                _ = context_.Payments.Remove(payment);
                accountsListViewSource.Filter += AccountsListViewSource_Filter;
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


        private List<object> TotalProductCostsList()
        {
            List<ProductCost> productCost = new List<ProductCost>();
            List<object> totalProductCosts = new List<object>();
            if (productsViewSource != null && productsViewSource.View != null)
            {
                foreach (Product product in productsViewSource.View)
                {
                    if (product.Costs != null)
                    {
                        productCost.AddRange(product.Costs);
                    }
                }
            }
            var grouping = from pc in productCost
                           group pc by pc.Code into grp
                           select new { Code = grp.Key, Name = grp.Select(pc => pc.Name).FirstOrDefault(), Cost = grp.Sum(pc => pc.Cost), Outlay = grp.Sum(pc => pc.Outlay), Margin = grp.Sum(pc => pc.Margin) };
            totalProductCosts.AddRange(grouping.OrderBy(grp => grp.Code));
            totalProductCosts.Add(new { Code = "", Name = "Итого по заказу ", Cost = grouping.Sum(grp => grp.Cost), Outlay = grouping.Sum(grp => grp.Outlay), Margin = grouping.Sum(grp => grp.Margin) });
            return totalProductCosts;
        }

        private void AccountsListViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is Account account && paymentsViewSource?.View != null)
            {
                decimal nAllPaymentOnAccount = 0, nAllCostOnAccount = 0;
                nAllPaymentOnAccount = context_.Payments.Local.Where(p => p.Account?.ID == account.ID).Sum(p => p.PaymentAmount); //получить ВСЕ платежи по данному счету
                foreach (Payment payment in paymentsViewSource.View)
                {
                    if (payment.Account?.ID == account.ID) //получить ВСЕ платежи по данному счету
                    {
                        foreach (AccountDetail accountDetail in payment.Account.DetailsList) //полная стоимость изделий по данному счету
                        {
                            nAllCostOnAccount += accountDetail.Cost;
                        }
                        break;
                    }
                }
                //MessageBox.Show($"{nAllPaymentOnAccount}    {nAllCostOnAccount}     {nAllPaymentOnAccount < nAllCostOnAccount}");

                //foreach (Payment payment in paymentsViewSource.View)
                //{
                //    nAllPaymentOnAccount += payment.PaymentAmount;
                //    if (payment.Account?.ID == account.ID) //получить ВСЕ платежи по данному счету
                //    {
                //        foreach (AccountDetail accountDetail in payment.Account.DetailsList) //полная стоимость изделий по данному счету
                //        {
                //            nAllCostOnAccount += accountDetail.Cost;
                //        }
                //    }
                //}
                if (nAllPaymentOnAccount > 0 && nAllCostOnAccount > 0) //если есть платежи по счету
                {
                    if (nAllPaymentOnAccount >= nAllCostOnAccount && account?.ID == (paymentsViewSource?.View?.CurrentItem as Payment).Account?.ID)
                    {
                        e.Accepted = true; //"свой" счет тоже берем 
                    }
                    else
                    {
                        e.Accepted = nAllPaymentOnAccount < nAllCostOnAccount; //брать счета для которых сумма платежей меньше стоимости изделий 
                    }
                }
                else
                {
                    e.Accepted = true;
                }

                //if (payment.Account is null || payment.Account.ID == account.ID) //"свой" текущий счет добавить обязательно
                //{
                //    e.Accepted = true;
                //}
                //else
                //{
                //    //decimal nAllPaymentOnAccount = context_.Payments.Local.Where(p => p.Account?.ID == account.ID).Sum(p => p.PaymentAmount); //получить ВСЕ платежи по данному счету
                //    //decimal? nAllCostOnAccount = payment.Account?.DetailsList?.Sum(p => p.Cost) ?? 0; //полная стоимость изделий по данному счету
                //    decimal nAllPaymentOnAccount = 0; 
                //    foreach (Payment p in paymentsViewSource.View)
                //    {
                //        if (p.Account?.ID == account.ID)
                //        {
                //            nAllPaymentOnAccount += p.PaymentAmount; //получить ВСЕ платежи по данному счету
                //        }
                //    }
                //    decimal nAllCostOnAccount = 0; 
                //    foreach (AccountDetail accountDetail in payment.Account.DetailsList)
                //    {
                //        nAllCostOnAccount += accountDetail.Cost; //полная стоимость изделий по данному счету
                //    }
                //    e.Accepted = nAllPaymentOnAccount < nAllCostOnAccount; //брать счета для которых сумма платежей меньше стоимости изделий 
                //}
            }
        }

        private void AccountsListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (paymentsViewSource?.View?.CurrentItem is Payment payment)
            {
                context_.Entry(payment).Reference(p => p.Account).Load();
                payment.Account?.DetailsToList(); 
                paymentsViewSource.View.Refresh();
            }
        }

        private void CopyFileToPathToFilesOfProduct(string[] aFullFileNames)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_pathToFilesOfProduct) && Directory.Exists(_pathToFilesOfProduct) && ordersViewSource.View.CurrentItem is Order order)
                {
                    string destinationPath = Path.Combine(_pathToFilesOfProduct, order.Number);
                    //string destinationPath = System.IO.Path.Combine(_pathToFilesOfProduct, "0000000001");
                    if (!Directory.Exists(destinationPath))
                    {
                        _ = Directory.CreateDirectory(destinationPath);
                    }
                    string ending = aFullFileNames.Length > 1 && aFullFileNames.Length < 5 ? "а" : aFullFileNames.Length >= 5 ? "ов" : "";
                    bool lNeedDeleteFile = MessageBox.Show($"В папку {destinationPath} будет скопировано {aFullFileNames.Length} файл{ending}.\n\n Удалить файл(-ы) в исходной папке?",
                        "Копирование файлов", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
                    foreach (string fullFileName in aFullFileNames)
                    {
                        FileSystem.CopyFile(fullFileName, Path.Combine(destinationPath, Path.GetFileName(fullFileName)), UIOption.AllDialogs, UICancelOption.DoNothing);
                        if (lNeedDeleteFile)
                        {
                            FileSystem.DeleteFile(fullFileName, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        }
                    }
                }
                else
                {
                    _ = MessageBox.Show("Не задан путь к папке размещения файлов заказа или папка не существует!" + "\n" + "Копирование файла не возможно!",
                        "Ошибка копирования файла", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (OperationCanceledException ex)
            {
                _ = MessageBox.Show("Копирование отменено или возникла неопределенная ошибка ввода-вывода!" + "\n" + ex.Message + "\n" + ex?.InnerException?.Message ?? "",
                    "Ошибка копирования файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка копирования файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Del_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.GetType().FullName.Contains("Button"))
                {
                    Button btn = e.OriginalSource as Button;
                    if (btn == DeleteProductButton)
                    {
                        if (productsViewSource.View.CurrentItem is Product product)
                        {
                            foreach (ProductCost productCost in product.Costs)
                            {
                                ___context.Entry(productCost).State = EntityState.Detached;
                            }
                            product.Costs.Clear();
                            _ = _context.Products.Remove(product);
                            currentOrder.State = currentOrder.OrderState(productsViewSource);
                            _ = CountTotals();
                        }
                        return;
                    }
                    if (btn == DeleteFileButton)
                    {
                        if (productsViewSource.View.CurrentItem is Product product && FilesListBox.SelectedItem is string fileToDelete)
                        {
                            product.FilesList.Remove(fileToDelete);
                            product.ListToFiles();
                            FilesListBox.Items.Refresh();
                        }
                        return;
                    }
                    if (btn == DeleteOperationInWorkFileButton && TechCardTreeView.SelectedItem is OperationInWork operationInWork)
                    {
                        _ = operationInWork.FilesList.Remove(OperationInWorkFilesListBox.SelectedItem as string);
                        operationInWork.ListToFiles();
                        OperationInWorkFilesListBox.Items.Refresh();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Del_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                e.CanExecute = false;
                return;
            }
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                Button btn = e.OriginalSource as Button;
                if (btn == DeleteProductButton && productsViewSource != null && ListProduct.Items.Count > 0)
                {
                    e.CanExecute = true;
                    return;
                }
                if (btn == DeleteFileButton && filesListViewSource != null && FilesListBox.Items.Count > 0)
                {
                    e.CanExecute = true;
                    return;
                }
                if (btn == DeleteOperationInWorkFileButton && techCardsViewSource != null && techCardsViewSource.View != null && TechCardTreeView != null && TechCardTreeView.SelectedItem is OperationInWork)
                {
                    e.CanExecute = true;
                    return;
                }
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ordersViewSource.View.CurrentItem is Order order && Directory.Exists(Path.Combine(_pathToFilesOfProduct, order.Number)))
            {
                _ = Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/n, {Path.Combine(_pathToFilesOfProduct, order.Number)}" });
            }
        }

        private void FilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = (string)((ListBox)sender).Items.CurrentItem;
            if (ordersViewSource != null && ordersViewSource.View.CurrentItem is Order order)
            {
                OpenFileInOSShell.OpenFile(Path.Combine(Path.Combine(_pathToFilesOfProduct, order.Number), fileName));
            }
        }

        private bool HasInvalidListView()
        {
            bool valid = true;
            foreach (object itemObj in ListProduct.Items)
            {
                ListBoxItem listBoxitem = (ListBoxItem)ListProduct.ItemContainerGenerator.ContainerFromItem(itemObj);
                foreach (object item in FindVisualChildren<TextBox>(listBoxitem))
                {
                    if (((TextBox)item).Name == "CostTextBox")
                    {
                        if (Validation.GetHasError((TextBox)item))
                        {
                            return !valid;
                        }
                    }
                }
                if (listBoxitem != null)
                {
                    foreach (ProductParameters productParameters in ((Product)listBoxitem.DataContext).ProductParameter)
                    {
                        if (productParameters.IsRequired) //параметр обязательный к заполнению
                        {
                            if (productParameters.ReferencebookID > 0 && productParameters.ParameterID is null)
                            {
                                return !valid;
                            }
                            if (productParameters.ReferencebookID is null && string.IsNullOrWhiteSpace(productParameters.ParameterValue))
                            {
                                return !valid;
                            }
                        }
                    }
                }
            }
            foreach (object itemObj in CostOfProduct.Items)
            {
                ListBoxItem listBoxitem = (ListBoxItem)CostOfProduct.ItemContainerGenerator.ContainerFromItem(itemObj);
                foreach (object item in FindVisualChildren<TextBox>(listBoxitem))
                {
                    if (((TextBox)item).Name == "TypeOfActivityCostTextBox" || ((TextBox)item).Name == "TypeOfActivityOutlayTextBox")
                    {
                        if (Validation.GetHasError((TextBox)item))
                        {
                            return !valid;
                        }
                    }
                }
            }
            return valid;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Label)
            {
                Label label = e.Source as Label;
                if (label.Name.Contains("DateDeliveryPlan") && DateDeliveryPlanDate.IsEnabled)
                {
                    DateDeliveryPlanDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("DateAdmission") && DateAdmissionDate.IsEnabled)
                {
                    DateAdmissionDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("DateCompletion") && DateCompletionDate.IsEnabled)
                {
                    DateCompletionDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("DateProductionLayout") && DateProductionLayoutDate.IsEnabled)
                {
                    DateProductionLayoutDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("DateTransferDesigner") && DateTransferDesignerDate.IsEnabled)
                {
                    DateTransferDesignerDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("DateTransferApproval") && DateTransferApprovalDate.IsEnabled)
                {
                    DateTransferApprovalDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("DateApproval") && DateApprovalDate.IsEnabled)
                {
                    DateApprovalDate.SelectedDate = DateTime.Now;
                }
                //else if (label.Name.Contains("DateTransferProduction") && DateTransferProductionDate.IsEnabled)
                //{
                //    DateTransferProductionDate.SelectedDate = DateTime.Now;
                //}
                //else if (label.Name.Contains("DateManufacture") && DateManufactureDate.IsEnabled)
                //{
                //    DateManufactureDate.SelectedDate = DateTime.Now;
                //}
                else if (label.Name.Contains("DateShipment") && DateShipmentDate.IsEnabled)
                {
                    DateShipmentDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("PaymentDate") && DatePaymentDate.IsEnabled)
                {
                    DatePaymentDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("PayBeforeDate") && DatePayBeforeDate.IsEnabled)
                {
                    DatePayBeforeDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("PlanCompletionDate") && DatePlanCompletionDate.IsEnabled)
                {
                    DatePlanCompletionDate.SelectedDate = DateTime.Now;
                }
                else if (label.Name.Contains("FactCompletionDate") && DateFactCompletionDate.IsEnabled)
                {
                    DateFactCompletionDate.SelectedDate = DateTime.Now;
                }
            }
        }

        private void PrintOrderFormButton_Click(object sender, RoutedEventArgs e)
        {
            PrintOrderForm();
        }

        private void PrintOrderFormForDesignerButton_Click(object sender, RoutedEventArgs e)
        {
            if (productsViewSource is null || productsViewSource.View is null)
            {
                return;
            }
            List<long> designerIDList = new List<long> { };
            PrintOrderFormForDesignerButton.ContextMenu.Items.Clear();
            foreach (Product product in productsViewSource.View)
            {
                if (!(product.DesignerID is null) && !designerIDList.Contains((long)product.DesignerID))
                {
                    foreach (User designer in designerList)
                    {
                        if (designer.ID == product.DesignerID)
                        {
                            MenuItem menuItem = new MenuItem { Header = designer.FullUserName, Tag = (long)product.DesignerID, IsCheckable = false };
                            menuItem.Click += DesignerContextMenuItem_Click;
                            _ = PrintOrderFormForDesignerButton.ContextMenu.Items.Add(menuItem);
                            designerIDList.Add((long)product.DesignerID);
                        }
                    }
                }
            }
            if (designerIDList.Count > 0)
            {
                PrintOrderFormForDesignerButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                PrintOrderFormForDesignerButton.ContextMenu.PlacementTarget = PrintOrderFormForDesignerButton;
                PrintOrderFormForDesignerButton.ContextMenu.Visibility = Visibility.Visible;
                PrintOrderFormForDesignerButton.ContextMenu.IsOpen = true;
            }
            else
            {
                PrintOrderForm();
            }
        }

        private void DesignerContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PrintOrderFormForDesignerButton.ContextMenu.Visibility = Visibility.Hidden;
            PrintOrderForm((long)(sender as MenuItem).Tag);
        }

        private void PrintOrderForm(long designerID = 0)
        {
            List<long> listProductID = new List<long> { };
            foreach (Product product in ListProduct.SelectedItems)
            {
                listProductID.Add(product.ID);
            }
            if (listProductID.Count == 0) //ничего не было выбрано
            {
                foreach (Product product in productsViewSource.View) //добавить ВСЕ
                {
                    listProductID.Add(product.ID);
                }
            }
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            {
                try
                {
                    MainWindow.statusBar.WriteStatus("Получение данных заказа ...", Cursors.Wait);
                    string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                    List<Order> orderList = new List<Order> { };
                    orderList = _reportcontext.Orders.AsNoTracking().Where(Order => Order.ID == currentOrder.ID)
                        .Include(Order => Order.Products).ThenInclude(Products => Products.ProductType)
                        .Include(Order => Order.Products).ThenInclude(Products => Products.Designer)
                        .Include(Order => Order.Manager)
                        .Include(Order => Order.OrderEntered)
                        .Include(Order => Order.Client)
                        .AsNoTracking()
                        .ToList();
                    foreach (Order order in orderList)
                    {
                        Array array = order.Products.ToArray();
                        for (int ind = 0; ind < array.Length; ind++)
                        {
                            if (!listProductID.Contains(((Product)array.GetValue(ind)).ID)) //если изделия нет в писке требующихся для печати, то удалить
                            {
                                _ = order.Products.Remove((Product)array.GetValue(ind));
                            }
                        }
                    }
                    foreach (Order order in orderList)
                    {
                        foreach (Product product in order.Products)
                        {
                            GetProductParameters(product);
                            product.FilesToList();
                            foreach (ProductParameters productParameter in product.ProductParameter)
                            {
                                if (productParameter.ReferencebookID > 0) //если параметр должен заполниться из справочника
                                {
                                    productParameter.ParameterValue = referencebookParameters.Find(rp => rp.ReferencebookID == productParameter.ReferencebookID && rp.ID == productParameter.ParameterID)?.Value ?? "";
                                }
                            }
                            product.Costs = _reportcontext.ProductCosts.AsNoTracking().Where(ProductCost => ProductCost.ProductID == product.ID).Include(ProductCost => ProductCost.TypeOfActivity).ToList();
                            product.KVDForReport = string.Join(",", product.Costs.Where(c => c.Cost != 0).Select(c => c.Code.Trim()).ToList());
                        }
                    }
                    if (File.Exists(Path.Combine(_pathToReportTemplate, "OrderForm.frx")))
                    {
                        Reports.OrderDataSet = orderList;
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "OrderForm.frx");
                        Reports.ReportMode = "OrderForm";
                        Reports.designerID = designerID;
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show("Не найден файл OrderForm.frx !", "Ошибка формирования бланка заказа", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка получения данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (_reportcontext != null)
                    {
                        _reportcontext.Dispose();
                    }
                    MainWindow.statusBar.ClearStatus();
                }
            }
        }

        private void CreateNewAccount()
        {
            if (accountsViewSource is null || accountsViewSource.View is null)
            {
                LoadOrderAccounts(false);
            }
            if (contractorsViewSource.View.CurrentItem is null)
            {
                _ = contractorsViewSource.View.MoveCurrentToFirst();
            }
            Contractor contractor = contractorsViewSource.View.CurrentItem as Contractor;
            Account account = new Account
            {
                OrderID = currentOrder.ID,
                IsManual = false,
                ContractorID = contractor.ID,
                ContractorInfoForAccount = contractor.ContractorInfoForAccount,
                ContractorName = contractor.Name,
                AccountNumber = ""
            };
            _ = context__.Accounts.Add(account);
            context__.Entry(account).Reference(account => account.Contractor).Load(); //загрузить через св-во навигации
            contractor = account.Contractor;
            context__.Entry(contractor).Reference(contractor => contractor.Bank).Load(); //загрузить через св-во навигации
            context__.Entry(account).Reference(account => account.Order).Load(); //загрузить через св-во навигации
            account.AccountNumber = GetNewAccountNumber(ref account);
            Order order = account.Order;
            context__.Entry(order).Collection(o => o.Products).Load(); //загрузить через св-во навигации
            context__.Entry(order).Reference(o => o.Client).Load(); //загрузить через св-во навигации
            Client client = order.Client;
            context__.Entry(client).Reference(client => client.Bank).Load(); //загрузить через св-во навигации
            foreach (Product product in order.Products)
            {
                context__.Entry(product).Reference(p => p.ProductType).Load(); //загрузить через св-во навигации
            }
            _ = accountsViewSource.View.MoveCurrentTo(account);
            account.DetailsList = CreateNewAccountDetails();
            account.ListToDetails();
             accountsViewSource.View.Refresh();
            _ = accountsViewSource.View.MoveCurrentTo(account);
        }

        private void CreateNewAct()
        {
            if (accountsViewSource.View.CurrentItem is Account account)
            {
                Act act = new Act
                {
                    ActDate = DateTime.Now,
                    AccountID = account.ID,
                };
                _ = context__.Acts.Add(act);
                context__.Entry(act).Reference(act => act.Account).Load(); //загрузить через св-во навигации                
                context__.Entry(account).Reference(account => account.Contractor).Load(); //загрузить через св-во навигации
                List<long> listProductID = new List<long> { };
                foreach (Act a in account.Acts) //получить список всех изделий, уже входящих в акты данного счета
                {
                    listProductID.AddRange(a.DetailsList.Select(d => d.ProductID));
                }
                act.ListProductInAct = account.DetailsList.Select(d => d.ProductID).Except(listProductID).ToList(); //список productID 
                act.ListToProductInAct(); //свернуть список productID в строку
                account.Acts.Add(act);
                act.CreateDetailsList(account);
                act.ActNumber = GetNewActNumber(act);
                if (ListAct.Items.Count > 0)
                {
                    ListAct.SelectedIndex = ListAct.Items.Count - 1;
                }
                ListAct.Items.Refresh();
                ListActDetail.Items.Refresh();
            }
        }

        private string GetNewAccountNumber(ref Account account)
        {
            short nMaxAccountNumber = 1;
            if (accountsViewSource?.View != null)
            {
                foreach (Account acc in accountsViewSource.View)
                {
                    string[] s = acc.AccountNumber.Split('/');
                    if (s.Length > 1 && short.TryParse(s[1], out short nMax))
                    {
                        nMaxAccountNumber = (short)Math.Max(nMax + 1, nMaxAccountNumber);
                    }
                }
            }
            return $"{currentOrder.Number.TrimStart('0')}/{nMaxAccountNumber}/{account.Contractor.AbbrForAcc}";
        }

        private string GetNewActNumber(Act act)
        {
            string sNewActNumber = "";
            if (accountsViewSource.View.CurrentItem is Account account)
            {
                if (account.IsManual)
                {
                    sNewActNumber = account.AccountNumber;
                }
                else
                {
                    if (act.DetailsList.Count < account.DetailsList.Count) //если количество изделий в счете(заказе) больше чем в текущем Акте, то это означает частичная отгрузка
                    {
                        //string sLastActNumber = account.Acts.Last().ActNumber;
                        short nNumber = 1;
                        foreach (Act a in account.Acts)
                        {
                            string[] s = a.ActNumber.Split('-');
                            if (s.Length > 1 && short.TryParse(s[1], out short nMax))
                            {
                                nNumber = (short)Math.Max(nMax + 1, nNumber);
                            }
                        }
                        sNewActNumber = $"{account.AccountNumber} - {nNumber}";
                    }
                    else
                    {
                        sNewActNumber = account.AccountNumber;
                    }
                }
            }
            return sNewActNumber;
        }

        private bool CanCreateNewAct()
        {
            bool lCanCreateNewAct = false;
            if (accountsViewSource.View.CurrentItem is Account account)
            {
                lCanCreateNewAct = account.IsManual ? account.Acts?.Count == 0 : (account.Acts?.Count < account.DetailsList.Count && GetProductsInAct(account).Count > 0); //в счете кол-во актов д.б. меньше кол-ва изделий в счете
            }
            return lCanCreateNewAct;
        }

        private List<long> GetProductsInAct(Account account)
        {
            List<long> listProductsID = new List<long> { };
            if (!account.IsManual) //счет создан НЕ вручную, для ручного возврат пустого списка
            { //ищем изделия со статусом "Запланирована отгрузка" для включения в акт
                if (account.Order != null)
                {
                    //listProductsID = account.Order.Products.Where(product => product.ProductState() == OrderProductStates.GetProductState(2)).Select(product => product.ID).ToList(); //список всех изделий, которые можно отгрузить
                    listProductsID = account.Order.Products.Select(product => product.ID).ToList(); //список всех изделий
                }
                else
                {
                    foreach (Product product in productsViewSource.View)
                    {
                        listProductsID.Add(product.ID);
                        //if (product.ProductState() == OrderProductStates.GetProductState(2))
                        //{
                        //    listProductsID.Add(product.ID);
                        //}
                    }
                }
                foreach (Account acc in accountsViewSource.View) //проверяем было ли изделие уже отгружено в другом акте любого счета
                {
                    if (acc.Acts != null)
                    {
                        foreach (Act act in acc.Acts)
                        {
                            _ = listProductsID.RemoveAll(delegate (long productInAct)  //в сухом остатке останутся только изделия не входящие ни в один акт, либо пустой список
                            {
                                return act.ListProductInAct.Contains(productInAct);
                            });
                        }
                    }
                }
            }
            return listProductsID;
        }

        private List<AccountDetail> CreateNewAccountDetails(bool IsManual = false)
        {
            List<AccountDetail> accountDetails = new List<AccountDetail> { };
            if (accountsViewSource?.View != null)
            {
                if (accountsViewSource.View.CurrentItem is null)
                {
                    _ = accountsViewSource.View.MoveCurrentToFirst();
                }
                Account account = accountsViewSource.View.CurrentItem as Account;
                if (account.IsManual) //ручной ввод деталей счета
                {
                    accountDetails.Add(new AccountDetail { ProductID = 0, ProductInfoForAccount = "Оплата по договору № ...", Quantity = 1, UnitName = "шт.", Cost = account.Order?.OrderCost ?? CountTotals() });
                }
                else //загрузка из номенклатуры изделий заказа. Грузим только не включенные в другой счет
                {
                    IEnumerator products;
                    products = account.Order is null
                        ? productsViewSource.View.GetEnumerator()
                        : account.Order.Products.GetEnumerator();
                    while (products.MoveNext())
                    {
                        Product product = (Product)products.Current;
                        bool lIncluded = false;
                        foreach (Account a in accountsViewSource.View) //проходим по счетам
                        {
                            foreach (AccountDetail accountDetail in a.DetailsList) //проверяем включено ли изделие в какой-либо счет
                            {
                                if (accountDetail.ProductID.Equals(product.ID))
                                {
                                    lIncluded = true;
                                    break; //выход во внешний цикл "по счетам"
                                }
                            }
                        }
                        if (!lIncluded) //в итоге ни в одном из счетов изделия нет
                        {
                            accountDetails.Add(new AccountDetail { ProductID = product.ID, ProductInfoForAccount = product.ProductType.Name, Quantity = product.Quantity, UnitName = "шт.", Cost = product.Cost });
                        }
                    }
                }
            }
            return accountDetails;
        }

        private void LoadOrderAccounts(bool lShowMessage = true)
        {
            MainWindow.statusBar.WriteStatus("Загрузка счетов ...", Cursors.Wait);
            try
            {
                if (accountsViewSource?.View != null)
                {
                    foreach (EntityEntry entityEntry in context__.ChangeTracker.Entries().ToArray()) //для повторной загрузки из БД
                    {
                        if (entityEntry.Entity != null)
                        {
                            entityEntry.State = EntityState.Detached;
                        }
                    }
                }
                context__.Accounts
                    .Where(Account => Account.OrderID == currentOrder.ID)
                    .Include(Account => Account.Acts)
                    .Include(Account => Account.Order)
                    .Include(Account => Account.Contractor).ThenInclude(Contractor => Contractor.Bank).ThenInclude(Bank => Bank.Localities)
                    .Include(Account => Account.Order.Client).ThenInclude(Client => Client.Bank).ThenInclude(Bank => Bank.Localities)
                    .Include(Account => Account.Order.Client).ThenInclude(Client => Client.ConsigneeBank).ThenInclude(ConsigneeBank => ConsigneeBank.Localities)
                    .Include(Account => Account.Order.Products).ThenInclude(Product => Product.ProductType)
                    .Load();
                accountsViewSource.Source = context__.Accounts.Local.ToObservableCollection();
                foreach (Account account in accountsViewSource.View)
                {
                    account.DetailsToList(); //распаковать детали счета
                    foreach (Act act in account.Acts)
                    {
                        act.CreateDetailsList();
                    }
                }
                _ = accountsViewSource.View.MoveCurrentToFirst();
                accountsViewSource.View.Refresh();
                ListAct.SelectedIndex = -1;
                ListAct.SelectedIndex = 0; //имитация смены текущего выбора для инициации обновления дочерней таблицы
                if (lShowMessage)
                {
                    _ = MessageBox.Show("   Все счета загружены успешно!   ", "Загрузка счетов");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки счетов", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void ContractorNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null)
            {
                if (accountsViewSource?.View?.CurrentItem is Account account && context__.Entry(account).State != EntityState.Detached)
                {
                    context__.Entry(account).Reference(account => account.Contractor).Load(); 
                    Contractor contractor = account.Contractor;
                    if (context__.Entry(contractor).State != EntityState.Detached)
                    {
                        context__.Entry(contractor).Reference(contractor => contractor.Bank).Load();
                        contractor.ContractorInfoForAccount = "";
                        account.ContractorName = contractor.Name;
                    }
                    if (account.AccountNumber.Count(c => c == '/') > 1) //наименование счета нового формата
                    {
                        string[] s = account.AccountNumber.Split("/");
                        account.AccountNumber = $"{s[0]}/{s[1]}/{account.Contractor.AbbrForAcc}";
                    }
                    int nSelectedIndex = ListAccount.SelectedIndex;
                    ListAccount.SelectedIndex = -1;
                    ListAccount.SelectedIndex = nSelectedIndex;
                    if (ListAct.Items.Count > 0)
                    {
                        ListAct.Items.Refresh();
                    }
                }
            }
        }

        private void ListAccount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && context__ != null && accountsViewSource?.View?.CurrentItem is Account account 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete")) //&& accountsViewSource != null
            {
                _ = context__.Accounts.Remove(account);
            }
        }

        private void ListAccountDetail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is ListViewItem listViewItem && e.Key == Key.Delete && context__ != null && accountsViewSource?.View?.CurrentItem is Account account && account.Acts.Count == 0 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete")) //при наличии актов не удалять
            {
                AccountDetail accountDetail = (AccountDetail)listViewItem.DataContext;
                int nSelectedIndex = ListAccountDetail.SelectedIndex;
                _ = account.DetailsList.Remove(accountDetail);
                account.ListToDetails();
                ListAccountDetail.Items.Refresh();
                ListAccountDetail.SelectedIndex = Math.Min(nSelectedIndex, ListAccountDetail.Items.Count - 1);
            }
        }

        private void ListAct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && context__ != null && accountsViewSource != null && accountsViewSource.View.CurrentItem is Account account 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"))
            {
                ListViewItem listViewItem = (ListViewItem)e.OriginalSource;
                Act act = (Act)listViewItem.DataContext;
                _ = account.Acts.Remove(act);
                if (act.ID > 0)
                {
                    _ = context__.Acts.Remove(act);
                }
                ListAct.Items.Refresh();
            }
        }

        private void ListActDetail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && context__ != null && accountsViewSource?.View.CurrentItem is Account account 
                && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductPADNewChangeDelete"))
            {
                ListViewItem listViewItem = (ListViewItem)e.OriginalSource;
                AccountDetail accountDetail = (AccountDetail)listViewItem.DataContext;
                Act act = (Act)ListAct.SelectedItem;
                int nSelectedIndex = ListAct.SelectedIndex;
                _ = act.ListProductInAct.Remove(accountDetail.ProductID);
                act.ListToProductInAct();
                act.CreateDetailsList(account); //лучше использовать account, т.к. Account для Act может быть пустым для нового и не сохраненного счета
                foreach (Act a in account.Acts)
                {
                    a.ActNumber = GetNewActNumber(a); //перенумерация актов
                }
                ListAct.SelectedIndex = -1;
                ListAct.SelectedIndex = Math.Min(nSelectedIndex, ListAct.Items.Count - 1);
            }
        }

        private void ManualInput_Click(object sender, RoutedEventArgs e)
        {
            if (accountsViewSource != null && accountsViewSource.View != null && accountsViewSource.View.CurrentItem is Account account)
            {
                account.DetailsList = CreateNewAccountDetails((bool)(sender as CheckBox).IsChecked);
                accountsViewSource.View.Refresh();
                _ = accountsViewSource.View.MoveCurrentTo(account);
                account.ListToDetails(); //свернуть детали счета
            }
        }

        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (accountsViewSource != null && accountsViewSource.View != null && accountsViewSource.View.CurrentItem is Account account)
            {
                account.ListToDetails(); //свернуть детали счета
                foreach (Act act in account.Acts)
                {
                    act.CreateDetailsList();
                }
            }
        }

        private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                if (e.OriginalSource == PrintAccountButton)
                {
                    AccountPrint();
                }
                if (e.OriginalSource == PrintActButton)
                {
                    ActPrint();
                }
                if (e.OriginalSource == PrintTechCardButton)
                {
                    TechCardPrepeareToPrint();
                }
            }
        }

        private void Print_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType().FullName.Contains("Button"))
            {
                if (e.OriginalSource == PrintAccountButton && accountsViewSource != null && accountsViewSource.View != null && accountsViewSource.View.CurrentItem is Account a && a.ID != 0)
                {
                    e.CanExecute = true;
                }
                if (e.OriginalSource == PrintActButton && accountsViewSource != null && accountsViewSource.View != null && accountsViewSource.View.CurrentItem is Account account && account.ID != 0)
                {
                    if (account.Acts != null && account.Acts.Count > 0)
                    {
                        e.CanExecute = true;
                    }
                }
                if (e.OriginalSource == PrintTechCardButton && techCardsViewSource?.View != null && TechCardTreeView?.SelectedItem != null 
                    && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductTechCardPrint"))
                {
                    e.CanExecute = true;
                }
            }
        }

        private void AccountPrint()
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                MainWindow.statusBar.WriteStatus("Получение данных счета ...", Cursors.Wait);
                Account account = accountsViewSource.View.CurrentItem as Account;
                string AccountFileTemplate = account.Contractor?.AccountFileTemplate ?? "";
                string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                if (File.Exists(Path.Combine(_pathToReportTemplate, AccountFileTemplate)))
                {
                    List<Account> accounts = new List<Account> { account };
                    Reports.AccountDataSet = accounts;
                    Reports.ReportFileName = Path.Combine(_pathToReportTemplate, AccountFileTemplate);
                    Reports.ReportDate = AccountDate.SelectedDate ?? null;
                    Reports.ReportMode = "AccountForm";
                    Reports.WithSignature = (bool)WithSignature.IsChecked;
                    //Reports.AmountInWords = InWords.Amount(CountTotals());
                    Reports.AmountInWords = InWords.Amount(account.DetailsList.Sum(c => c.Cost));
                    Reports.RunReport();
                }
                else
                {
                    _ = MessageBox.Show($"Не найден файл {AccountFileTemplate} !", "Ошибка формирования счета", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования счета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void ActPrint()
        {
            using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            try
            {
                MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);
                string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;             
                List<Account> accounts = new List<Account> { accountsViewSource.View.CurrentItem as Account };
                Reports.AccountDataSet = accounts;
                Reports.ActDataSet = new List<Act> { };
                if (ListAct.SelectedItem != null) //фиксируем акт, который выбран для печати
                {
                    Reports.ActDataSet = new List<Act> { (Act)ListAct.SelectedItem };
                }
                Reports.AmountInWords = InWords.Amount(Reports.ActDataSet[0].DetailsList.Sum(a => a.Cost));
                Reports.ReportDate = ActDate.SelectedDate ?? null;
                if ((bool)TemplateAct.IsChecked)
                {
                    if (File.Exists(Path.Combine(_pathToReportTemplate, "Act.frx")))
                    {
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "Act.frx");
                        Reports.ReportMode = "ActForm";
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show("Не найден файл Act.frx !", "Ошибка формирования акта", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if ((bool)TemplateSFTN.IsChecked)
                {
                    if (File.Exists(Path.Combine(_pathToReportTemplate, "SF.frx")))
                    {
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "SF.frx");
                        Reports.ReportDateInWords = InWords.Date(ActDate.SelectedDate);
                        Reports.ReportMode = "SFForm";
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show("Не найден файл SF.frx !", "Ошибка формирования СФ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    if (File.Exists(Path.Combine(_pathToReportTemplate, "TN.frx")))
                    {
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "TN.frx");
                        Reports.ReportMode = "TNForm";
                        Reports.NumberInWords = InWords.Number(Reports.ActDataSet[0].DetailsList.Count);
                        Reports.CargoReleasePostName = MainWindow.Userdata.PostName;
                        Reports.CargoReleaseName = MainWindow.Userdata.ShortUserName;
                        Reports.ReportDateInWords = InWords.Date(ActDate.SelectedDate);
                        Reports.FreeValue = new List<string> { "", ""};
                        MatchCollection matchCollection = Regex.Matches((accountsViewSource.View.CurrentItem as Account).Footing, @"(№|N)?(\s*\w*-?\w*\s*)от(\s*\d{2}.\d{2}.\d{2,4})", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
                        if (matchCollection.Count > 0)
                        {
                            StringBuilder stringBuilder = new StringBuilder(matchCollection[0].Value.ToLower());
                            _ = stringBuilder.Replace("№", ""); //убрать лишнее
                            _ = stringBuilder.Replace("N", "");
                            _ = stringBuilder.Replace(" ", "");
                            string str = stringBuilder.ToString();
                            Reports.FreeValue[0] = str.Substring(0, str.IndexOf("от"));
                            Reports.FreeValue[1] = str[(str.IndexOf("от") + 2)..];
                        }
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show("Не найден файл TN.frx !", "Ошибка формирования ТН", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if ((bool)TemplateUPD.IsChecked)
                {
                    if (File.Exists(Path.Combine(_pathToReportTemplate, "UPD.frx")))
                    {
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, "UPD.frx");
                        Reports.ReportMode = "UPDForm";
                        Reports.MonthInWords = InWords.Month(ActDate.SelectedDate);
                        Reports.CargoReleasePostName = MainWindow.Userdata.PostName;
                        Reports.CargoReleaseName = MainWindow.Userdata.ShortUserName;
                        Reports.ReportDateInWords = InWords.Date(ActDate.SelectedDate);
                        Reports.RunReport();
                    }
                    else
                    {
                        _ = MessageBox.Show("Не найден файл UPD.frx !", "Ошибка формирования УПД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования документа", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void LoadTechCard(bool lShowMsg = false)
        {
            MainWindow.statusBar.WriteStatus("Загрузка техкарт ...", Cursors.Wait);
            try
            {
                if (techCardsViewSource != null && techCardsViewSource.View != null)
                {
                    foreach (EntityEntry entityEntry in _context_.ChangeTracker.Entries().ToArray()) //для повторной загрузки из БД
                    {
                        if (entityEntry.Entity != null)
                        {
                            entityEntry.State = EntityState.Detached;
                        }
                    }
                }
                _context_.TechCards
                    .Include(TechCard => TechCard.Product)
                    .Include(TechCard => TechCard.Product.Designer)
                    .Include(TechCard => TechCard.Product.ProductType)
                    .Include(TechCard => TechCard.Product.Order)
                    .Include(TechCard => TechCard.Product.Order.Client)
                    .Include(TechCard => TechCard.Product.Order.Manager)
                    .Include(TechCard => TechCard.Product.Order.OrderEntered)
                    .Include(TechCard => TechCard.WorkInTechCards).ThenInclude(WorkInTechCard => WorkInTechCard.TypeOfActivity)
                    .Include(TechCard => TechCard.WorkInTechCards).ThenInclude(WorkInTechCard => WorkInTechCard.OperationInWorks)
                    .ThenInclude(OperationInWork => OperationInWork.Operation).ThenInclude(Operation => Operation.ProductionArea)
                    .Where(TechCard => TechCard.Product.OrderID == currentOrder.ID).Load();
                techCardsViewSource.Source = _context_.TechCards.Local.ToObservableCollection();
                Array array; 
                foreach (TechCard techCard in techCardsViewSource.View)
                {
                    array = techCard.WorkInTechCards.OrderBy(w => w.ID).ToArray();
                    techCard.WorkInTechCards.Clear();
                    for (int ind = 0; ind < array.Length; ind++)
                    {
                        techCard.WorkInTechCards.Add((WorkInTechCard)array.GetValue(ind));
                    }
                    techCard.WorkInTechCards_ = null; //для инициализации ObservaleCollection()
                    foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                    {
                        array = workInTechCard.OperationInWorks.OrderBy(o => o.ID).ToArray();
                        workInTechCard.OperationInWorks.Clear();
                        for (int ind = 0; ind < array.Length; ind++)
                        {
                            workInTechCard.OperationInWorks.Add((OperationInWork)array.GetValue(ind));
                        }
                        workInTechCard.OperationInWorks_ = null; //для инициализации ObservaleCollection()
                        foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                        {
                            GetOperationInWorkParameters(operationInWork); //сформировать список параметров
                            operationInWork.FilesToList(); //развернуть список файлов
                        }
                    }
                }
                if (lShowMsg)
                {
                    _ = MessageBox.Show("   Все техкарты загружены успешно!   ", "Загрузка техкарт");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка загрузки техкарт", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void AddNewObjectInTechCard(string sMode = "")
        {
            try
            {
                if (techCardsViewSource != null && techCardsViewSource.View != null)
                {
                    MainWindow.statusBar.WriteStatus("Загрузка объектов в техкарту ...", Cursors.Wait);
                    if (sMode == "TechCard") //добавить "Техкарта(изделие)"
                    { //добавить все новые техкарты (изделия)                        
                        List<long> ListProductID = _context_.Products.Where(Product => Product.OrderID == currentOrder.ID && !_context_.TechCards.Local.Select(tc => tc.ProductID).ToList().Contains(Product.ID))
                            .Select(p => p.ID).ToList();
                        foreach (long nProductID in ListProductID)
                        {
                            TechCard techCard = new TechCard { Number = "", ProductID = nProductID };
                            _ = _context_.TechCards.Add(techCard);
                            _context_.Entry(techCard).Reference(tc => tc.Product).Load(); //загрузить связки по навигационным свойствам
                        }
                        foreach (TechCard tc in techCardsViewSource.View)
                        {
                            Product pr = tc.Product;
                            _context_.Entry(pr).Reference(p => p.ProductType).Load(); //загрузить связки по навигационным свойствам
                            _context_.Entry(pr).Reference(p => p.Designer).Load(); //загрузить связки по навигационным свойствам
                            _context_.Entry(pr).Reference(p => p.Order).Load();
                            _context_.Entry(pr.Order).Reference(p => p.Client).Load();
                            _context_.Entry(pr.Order).Reference(p => p.Manager).Load();
                            pr.IsHasTechcard = true; //установить признак наличия Техкарты
                            Product product = _context.Products.Local.First(Product => Product.ID == tc.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                            product.IsHasTechcard = true;
                        }
                    }
                    else if (sMode == "Work" && TechCardTreeView.SelectedItem != null) //добавить "Работа"
                    {
                        TechCard tc = TechCardTreeView.SelectedItem as TechCard;
                        if (tc is null) //добавление НЕ с уровня "Техкарта"
                        {
                            if (TechCardTreeView.SelectedItem is WorkInTechCard) //добавление с уровня "Работа"
                            {
                                tc = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0);
                            }
                            else if (TechCardTreeView.SelectedItem is OperationInWork) //добавление с уровня "Операция"
                            {
                                tc = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0);
                            }
                            if (tc is null)
                            {
                                return; //по неизвестной причине родитель "Техкарта" не определен. Соответственно ничего не делаем
                            }
                        }
                        WorkInTechCard workInTechCard = new WorkInTechCard 
                        { 
                            TechCardID = tc.ID,
                            TypeOfActivityID = typeOfActivityList.ToList().FirstOrDefault().ID,
                            DatePlanCompletion = currentOrder.DateCompletion
                        };
                        _ = _context_.WorkInTechCards.Add(workInTechCard);
                        tc.WorkInTechCards.Add(workInTechCard);
                        tc.WorkInTechCards_ = null; //для обновления дерева
                        tc.IsExpanded = true;
                        _context_.Entry(workInTechCard).Reference(w => w.TypeOfActivity).Load(); //загрузить связки по навигационным свойствам
                        _context_.Entry(workInTechCard).Reference(w => w.TechCard).Load(); //загрузить связки по навигационным свойствам
                        workInTechCard.IsSelected = true;
                    }
                    else if (sMode == "Operation" && TechCardTreeView.SelectedItem != null && (TechCardTreeView.SelectedItem is WorkInTechCard || TechCardTreeView.SelectedItem is OperationInWork)) //добавить "Операция"
                    {
                        WorkInTechCard workInTechCard = TechCardTreeView.SelectedItem as WorkInTechCard;
                        if (workInTechCard is null) //добавление НЕ с уровня "Работа"
                        {
                            if (TechCardTreeView.SelectedItem is OperationInWork) //добавление с уровня "Операция"
                            {
                                workInTechCard = (WorkInTechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 1);
                            }
                            if (workInTechCard is null)
                            {
                                return; //по неизвестной причине родитель "Работа" не определен. Соответственно ничего не делаем
                            }
                        }
                        OperationInWork operationInWork = new OperationInWork
                        {
                            WorkInTechCardID = workInTechCard.ID,
                            OperationID = (operationsViewSource.View.CurrentItem as Operation).ID,
                            Note = "",
                            Parameters = "",
                            Files = ""
                        };
                        _ = _context_.OperationInWorks.Add(operationInWork);
                        workInTechCard.OperationInWorks.Add(operationInWork);
                        TechCard tc = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0);
                        if (tc != null)
                        {
                            tc.WorkInTechCards_ = null; //для обновления дерева
                        }
                        workInTechCard.IsExpanded = true;
                        operationInWork.IsSelected = true;
                        _context_.Entry(operationInWork).Reference(o => o.Operation).Load(); //загрузить связки по навигационным свойствам
                        _context_.Entry(operationInWork).Reference(o => o.WorkInTechCard).Load(); //загрузить связки по навигационным свойствам
                        _context_.Entry(operationInWork.Operation).Reference(o => o.ProductionArea).Load(); //загрузить связки по навигационным свойствам
                        _context_.Entry(workInTechCard).Reference(w => w.TypeOfActivity).Load(); //загрузить связки по навигационным свойствам
                        GetOperationInWorkParameters(operationInWork);
                        operationInWork.FilesToList();
                        workInTechCard.OperationInWorks_ = null; //для обновления дерева
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка добавления объекта в техкарту", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }

        public object GetParentTreeViewItem(object item, short nLevel = 1)
        {
            object oObject = null;
            foreach (TechCard techCard in TechCardTreeView.Items)
            {
                if (techCard.Equals(item))
                {
                    return techCard; //сам себе родитель
                }
                foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                {
                    if (workInTechCard.Equals(item))
                    {
                        return techCard; //родитель с уровня "Техкарта"
                    }
                    foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                    {
                        if (operationInWork.Equals(item))
                        {
                            return nLevel == 0 ? techCard : (object)workInTechCard;
                        }
                    }
                }
            }
            return oObject;
        }

        private void WorkTypeOfActivityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null && comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is WorkInTechCard workInTechCard)
            {
                MainWindow.statusBar.WriteStatus("Загрузка объектов в техкарту ...", Cursors.Wait);
                _context_.Entry(workInTechCard).Reference(w => w.TypeOfActivity).Load(); //загрузить связки по навигационным свойствам
                workInTechCard.IsSelected = true;
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void WorkInTechCardTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2 && (sender as TextBlock).DataContext is WorkInTechCard workInTechCard && workInTechCard.OperationInWorks.Count == 0) //ловим двойной или более клик
            {
                (sender as TextBlock).Visibility = Visibility.Collapsed; //скрыть самого себя
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && TechCardTreeView.SelectedItem != null) //при щелчке мышью нажат Ctrl
            {
                if (TechCardTreeView.SelectedItem is WorkInTechCard workInTC)
                {
                    workInTC.IsChecked = !workInTC.IsChecked;
                    workInTC.IsPrinted = workInTC.IsChecked; //отмечен, значит печатается
                    e.Handled = true;
                }
            }
        }

        private void WorkInTechCardComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null && comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is WorkInTechCard workInTechCard)
            {
                TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard, 0);
                if (tc != null)
                {
                    tc.WorkInTechCards_ = null; //для обновления дерева
                }
                comboBox.Visibility = Visibility.Collapsed; //скрыть самого себя
            }
        }

        private void NewOperationListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NewOperationListBox.Visibility = Visibility.Collapsed;
            NewOperationListBoxRow.Height = GridLength.Auto;
            AddNewObjectInTechCard("Operation");
        }

        private void HideNewOperationListBoxButton_Click(object sender, RoutedEventArgs e)
        {
            NewOperationListBox.Visibility = Visibility.Collapsed;
            NewOperationListBoxRow.Height = GridLength.Auto;
        }

        private void HideNewProductButton_Click(object sender, RoutedEventArgs e)
        {
            NewProductGrid.Visibility = Visibility.Collapsed;
            //NewProductListBox.Visibility = Visibility.Collapsed;
            //ListBoxTypeOfProductsRow.Height = GridLength.Auto;
        }

        private void WorkTypeOfActivityComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null && comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is WorkInTechCard workInTechCard)
            {
                workInTechCard.IsSelected = true;                
            }
        }

        private void TechCardTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            NewOperationListBox.Visibility = Visibility.Collapsed;
            NewOperationListBoxRow.Height = GridLength.Auto;
            //if (sender is TreeView treeView && treeView.SelectedItem != null)
            //{
            //    TechCard techCard = (TechCard)GetParentTreeViewItem(treeView.SelectedItem, 0);
            //    _ = techCardsViewSource.View.MoveCurrentTo(techCard); //синхронизация с ObservableCollection
            //}
        }

        private void SendTechCardToProduction()
        {
            if (TechCardTreeView?.SelectedItem != null)
            {
                try
                {
                    TechCard techCard = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0); //ищем корневого родителя на уровне TechCard
                    if (techCard != null)
                    {
                        techCard.Product.DateTransferProduction = DateTime.Now; //фиксируем дату передачи в производство
                        foreach (Product product in productsViewSource.View) //найти изделие в контекте Заказа
                        {
                            if (product.ID == techCard.Product.ID)
                            {
                                product.DateTransferProduction = DateTime.Now;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка передачи в производство", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RecallTechCardFromProduction()
        {
            if (TechCardTreeView?.SelectedItem != null)
            {
                try
                {
                    TechCard techCard = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0); //ищем корневого родителя на уровне TechCard
                    if (techCard != null)
                    {
                        techCard.Product.DateTransferProduction = null; //убрать дату передачи в производство
                        foreach (Product product in productsViewSource.View) //найти изделие в контекте Заказа
                        {
                            if (product.ID == techCard.Product.ID)
                            {
                                product.DateTransferProduction = null;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка отзыва с производства", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DateFactCompletionDate_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (techCardsViewSource?.View != null)
            {
                if (TechCardTreeView?.SelectedItem != null)
                {
                    try
                    {
                        TechCard techCard = (TechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 0); //ищем корневого родителя на уровне TechCard
                        if (techCard != null)
                        {
                            foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards) //проходим по всем работам текущей техкарты
                            {
                                if (!workInTechCard.DateFactCompletion.HasValue) //если нашли хоть одну незаполненную дату, то ничего не делаем
                                {
                                    techCard.Product.DateManufacture = null; //на всякий случай обнуляем дату
                                    foreach (Product product in productsViewSource.View) //найти изделие в контекте Заказа
                                    {
                                        if (product.ID == techCard.Product.ID)
                                        {
                                            product.DateManufacture = null;
                                            return;
                                        }
                                    }
                                    return;
                                }
                            }
                            techCard.Product.DateManufacture = (TechCardTreeView.SelectedItem as WorkInTechCard).DateFactCompletion; //DateTime.Now; //фиксируем дату изготовления по изделию
                            foreach (Product product in productsViewSource.View) //найти изделие в контекте Заказа
                            {
                                if (product.ID == techCard.Product.ID)
                                {
                                    product.DateManufacture = techCard.Product.DateManufacture;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка простановки даты изготоления", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OperationsViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (operationsViewSource != null && e.Item is Operation operation && TechCardTreeView != null && TechCardTreeView.SelectedItem != null)
            {
                WorkInTechCard workInTechCard;
                if (TechCardTreeView.SelectedItem is WorkInTechCard) //если в дереве выбран не уровень "Работа"
                {
                    workInTechCard = TechCardTreeView.SelectedItem as WorkInTechCard;
                }
                else
                {
                    workInTechCard = (WorkInTechCard)GetParentTreeViewItem(TechCardTreeView.SelectedItem, 1); //найти родителя на уровне "Работа"
                }
                e.Accepted = operation.TypeOfActivityInOperations.Any(t => t.TypeOfActivityID.Equals(workInTechCard.TypeOfActivityID)); //если операция относится к КВД выбранной работы
            }
        }

        private void GetOperationInWorkParameters(OperationInWork operationInWork)
        {
            operationInWork.OperationInWorkParameters = _context_.ParameterInOperations
                .Include(ParameterInOperation => ParameterInOperation.Unit)
                .Where(ParameterInOperation => ParameterInOperation.OperationID == operationInWork.OperationID) //отбор параметров только для конкретной операции
                .Select((ParameterInOperation) => new OperationInWorkParameter
                {
                    ID = ParameterInOperation.ID,
                    Name = ParameterInOperation.Name,
                    UnitName = ParameterInOperation.Unit.Name,
                    ReferencebookID = ParameterInOperation.ReferencebookID,
                    IsRefbookOnRequest = ParameterInOperation.IsRefbookOnRequest
                })
                .AsNoTracking().OrderBy(opinwp => opinwp.ID).ToList(); //этот запрос заполняет список OperationInWorkParameters пустым шаблоном 
            operationInWork.ParametersToList(); //заполнить пустой шаблон значениями из строки Parameters
            foreach (OperationInWorkParameter operationInWorkParameter in operationInWork.OperationInWorkParameters) //проходим по параметрам для дальнейшей инициализацц
            {
                if (operationInWorkParameter.IsRefbookOnRequest) //задан параметр "выбор справочника по запросу"
                {
                    long? typeOfActivityID = operationInWork.WorkInTechCard?.TypeOfActivityID ?? 0; //справочники будем брать только для конкретного КВД
                    IEnumerable<ReferencebookApplicability> enumerable = referencebookApplicabilities.Where(refApp => refApp.TypeOfActivityID == typeOfActivityID);
                    operationInWorkParameter.ReferencebookList = referencebooks.FindAll(
                        delegate (Referencebook referencebook)
                        {
                            return enumerable.Any(e => e.ReferencebookID == referencebook.ID); //найти в referencebooks все ID (справочники), которые применяются для TypeOfActivity
                        }
                        );
                }
                if (operationInWorkParameter.ReferencebookID > 0) //для параметра установлено брать значение из справочника
                {
                    operationInWorkParameter.ReferencebookParametersList = referencebookParameters
                        .Where(refbookParameters => refbookParameters.ReferencebookID == operationInWorkParameter.ReferencebookID).ToList();
                    operationInWorkParameter.ParameterValue = referencebookParameters.Find(rp => rp.ReferencebookID == operationInWorkParameter.ReferencebookID && rp.ID == operationInWorkParameter.ParameterID)?.Value ?? "";
                }
            }
        }

        private void OperationParameterTextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (TechCardTreeView != null && TechCardTreeView.SelectedItem != null && TechCardTreeView.SelectedItem is OperationInWork operationInWork)
            {
                operationInWork.ListToParameters(); //свернуть список значений параметров в строку Parameters
            }
        }

        private void OperationParameterTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TechCardTreeView != null && TechCardTreeView.SelectedItem != null && TechCardTreeView.SelectedItem is OperationInWork)
            {
                if (sender is ComboBox comboBox && comboBox != null &&
                    comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is OperationInWorkParameter operationInWorkParameter)
                {
                    ListOperationInWorkParameters.SelectedItem = operationInWorkParameter;
                }
                else if (sender is TextBox textBox && textBox != null && textBox.GetBindingExpression(TextBox.TextProperty).DataItem is OperationInWorkParameter operationInWorkParameter1)
                {
                    ListOperationInWorkParameters.SelectedItem = operationInWorkParameter1;
                }
            }
        }

        private void OperationParameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null)
            {
                Referencebook refBook = e.AddedItems[0] as Referencebook;
                if (techCardsViewSource != null && techCardsViewSource != null &&
                    comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is OperationInWorkParameter operationInWorkParameter)
                {
                    operationInWorkParameter.ReferencebookID = refBook.ID;
                    operationInWorkParameter.ReferencebookParametersList = referencebookParameters.Where(refbookParameters => refbookParameters.ReferencebookID == operationInWorkParameter.ReferencebookID).ToList();
                }
            }
        }

        private void OperationParameterValueComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox != null)
            {
                if (techCardsViewSource != null && techCardsViewSource != null &&
                    comboBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty).DataItem is OperationInWorkParameter operationInWorkParameter)
                {
                    operationInWorkParameter.ParameterValue = referencebookParameters.Find(rp => rp.ReferencebookID == operationInWorkParameter.ReferencebookID && rp.ID == operationInWorkParameter.ParameterID)?.Value ?? "";
                }
            }
        }

        private void OperationInWorkFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = (string)((ListBox)sender).Items.CurrentItem;
            OpenFileInOSShell.OpenFile(fileName);
        }

        private void TechCardTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _context_ != null && techCardsViewSource != null && techCardsViewSource.View != null && TechCardTreeView != null && TechCardTreeView.SelectedItem != null)
            {
                if (TechCardTreeView.SelectedItem is TechCard techCard)
                {
                    foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                    {
                        foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                        {
                            _ = _context_.OperationInWorks.Remove(operationInWork);
                        }
                        _ = _context_.WorkInTechCards.Remove(workInTechCard);
                    }
                    techCard.Product.IsHasTechcard = false; //убрать признак наличия Техкарты
                    techCard.Product.DateTransferProduction = null; //убрать дату передачи в производство
                    techCard.Product.DateManufacture = null; //убрать дату Изготовления
                    _ = _context_.TechCards.Remove(techCard);
                    Product product = _context.Products.Local.First(Product => Product.ID == techCard.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                    product.IsHasTechcard = false;
                    product.DateTransferProduction = null; //убрать дату передачи в производство
                    product.DateManufacture = null; //убрать дату Изготовления
                }
                else if (TechCardTreeView.SelectedItem is WorkInTechCard workInTechCard1)
                {
                    foreach (OperationInWork operationInWork in workInTechCard1.OperationInWorks)
                    {
                        _ = _context_.OperationInWorks.Remove(operationInWork);
                    }
                    TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard1, 0);
                    _ = _context_.WorkInTechCards.Remove(workInTechCard1);
                    _ = tc.WorkInTechCards.Remove(workInTechCard1);
                    tc.WorkInTechCards_ = null;
                    Product product = _context.Products.Local.First(Product => Product.ID == tc.Product.ID); //найти изделие в контекте Заказа/ Если вдруг не нйдет, то будет ошибка!!!
                    product.DateManufacture = null; //убрать дату Изготовления
                }
                else if (TechCardTreeView.SelectedItem is OperationInWork operationInWork1)
                {
                    WorkInTechCard wTC = (WorkInTechCard)GetParentTreeViewItem(operationInWork1, 1);
                    _ = _context_.OperationInWorks.Remove(operationInWork1);
                    _ = wTC.OperationInWorks.Remove(operationInWork1);
                    wTC.OperationInWorks_ = null;
                }
            }
        }

        private void TechCardTreeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && TechCardTreeView.SelectedItem != null) //при щелчке мышью нажат Ctrl
            {
                if (TechCardTreeView.SelectedItem is TechCard techCard)
                {
                    techCard.IsChecked = !techCard.IsChecked;
                    techCard.IsPrinted = techCard.IsChecked;
                    e.Handled = true;
                }
            }
        }

        private void TechCardPrepeareToPrint()
        {
            List<long> prodAreaID = new List<long> { };
            List<TechCard> techCards = new List<TechCard> { }; //список Техкарт для печати
            if (techCardsViewSource?.View != null && TechCardTreeView?.SelectedItem != null)
            {
                if (TechCardTreeView.SelectedItem is TechCard techCard) //печать с уровня Изделие
                {
                    techCards = _context_.TechCards.Local.Where(t => t.IsPrinted).ToList(); //отобрать все ТК с отметкой для печати
                    if (techCards.Count == 0) //если по факту ничего не отобралось, тогда берем только текущую ТК
                    {
                        techCard.IsPrinted = true;
                        techCards.Add(techCard);
                    }
                    foreach (TechCard tc in techCards)
                    {
                        foreach (WorkInTechCard workInTechCard in tc.WorkInTechCards) //ВСЕ дочерние элементы отметить На печать
                        {
                            workInTechCard.IsPrinted = true;
                            foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                            {
                                operationInWork.IsPrinted = true;
                            }
                        }
                    }
                    TechCardPrint(ref techCards);
                }
                else if (TechCardTreeView.SelectedItem is WorkInTechCard workInTechCard) //печать с уровня Работа
                {
                    TechCard tc = (TechCard)GetParentTreeViewItem(workInTechCard, 0); //берем ТК родителя
                    techCards = _context_.TechCards.Local.Where(t => t.Equals(tc) && tc.WorkInTechCards.Any(w => w.IsPrinted)).ToList(); //отобрать эту ТК, если  есть Работы с отметкой для печати
                    if (techCards.Count == 0) //по факту ничего не отобралось, тогда берем только текущую ТК
                    {
                        tc.IsPrinted = true;
                        workInTechCard.IsPrinted = true;
                        techCards.Add(tc);
                        foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks) //для всех операций текущей работы проставить отметку На печать
                        {
                            operationInWork.IsPrinted = true;
                        }
                    }
                    else
                    {
                        foreach (TechCard t in techCards)
                        {
                            foreach (WorkInTechCard workInTech in t.WorkInTechCards) //для отмеченных работ и операций проставить отметку На печать
                            {
                                if (workInTech.IsPrinted)
                                {
                                    foreach (OperationInWork operationInWork in workInTech.OperationInWorks)
                                    {
                                        operationInWork.IsPrinted = true;
                                    }
                                }
                            }
                        }
                    }
                    TechCardPrint(ref techCards);
                }
                else if (TechCardTreeView.SelectedItem is OperationInWork operationInWork) //печать с уровня Операция
                {
                    WorkInTechCard workInTC = (WorkInTechCard)GetParentTreeViewItem(operationInWork, 1);
                    PrintTechCardButton.ContextMenu.Items.Clear();
                    foreach (OperationInWork operationInW in workInTC.OperationInWorks)
                    {
                        if (operationInW.Operation.ProductionAreaID != null && !prodAreaID.Contains((long)operationInW.Operation.ProductionAreaID)) //добавление без дубляжа
                        {
                            MenuItem menuItem = new MenuItem
                            {
                                Header = operationInW.Operation.ProductionArea.Name,
                                Tag = (long)operationInW.Operation.ProductionAreaID,
                                IsCheckable = true,
                                StaysOpenOnClick = true
                            };
                            _ = PrintTechCardButton.ContextMenu.Items.Add(menuItem); //добавляем все производственные участки, которые есть в ТК
                            prodAreaID.Add((long)operationInW.Operation.ProductionAreaID);
                        }
                    }
                    _ = PrintTechCardButton.ContextMenu.Items.Add(new Separator()); //добавляем разделитель
                    MenuItem m = new MenuItem { Header = " На печать ", Tag = (long)0 };
                    m.Click += PrintTechCardMenuItem_Click;
                    _ = PrintTechCardButton.ContextMenu.Items.Add(m); //добавляем кнопку подтверждения
                    PrintTechCardButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    PrintTechCardButton.ContextMenu.PlacementTarget = PrintTechCardButton;
                    PrintTechCardButton.ContextMenu.Visibility = Visibility.Visible;
                    PrintTechCardButton.ContextMenu.IsOpen = true;

                    void PrintTechCardMenuItem_Click(object sender, RoutedEventArgs e)
                    {
                        try
                        {
                            if (sender is null)
                            {
                                return;
                            }
                            PrintTechCardButton.ContextMenu.IsOpen = false;
                            PrintTechCardButton.ContextMenu.Visibility = Visibility.Hidden;
                            prodAreaID.Clear();
                            for (short i = 0; i < PrintTechCardButton.ContextMenu.Items.Count; i++)
                            {
                                if (PrintTechCardButton.ContextMenu.Items[i] is MenuItem menuItem)
                                {
                                    long nID = (long)(menuItem.Tag ?? 0);
                                    if (menuItem.IsChecked && nID > 0)
                                    {
                                        prodAreaID.Add(nID); //собираем ID выбранных для печати операций
                                    }
                                }
                            }
                            TechCard tc = (TechCard)GetParentTreeViewItem(operationInWork, 0);
                            WorkInTechCard work = (WorkInTechCard)GetParentTreeViewItem(operationInWork, 1);
                            tc.IsPrinted = true; //берем родителя в печать
                            work.IsPrinted = true; //берем родителя в печать
                            techCards.Add(tc);
                            if (prodAreaID.Count > 0) //есть отобранные на печать производственные участки
                            {
                                foreach (OperationInWork operationInW in workInTC.OperationInWorks)
                                {
                                    operationInW.IsPrinted = prodAreaID.Contains((long)operationInW.Operation.ProductionAreaID); //берем операцию для печати если производственный участок есть в отобранных
                                }
                            }
                            else
                            {
                                operationInWork.IsPrinted = true; //если отобранных участков нет, то берем только текущую операцию
                            }
                            TechCardPrint(ref techCards);
                        }
                        catch (Exception ex)
                        {
                            _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка подготовки техкарты к печати", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void TechCardPrint(ref List<TechCard> techCards)
        {
            if (techCards.Count > 0)
            {
                foreach (TechCard tc in techCards)
                {
                    if (tc.WorkInTechCards.Count == 0) //работы не определены
                    {
                        tc.WorkInTechCards.Add(new WorkInTechCard { Number = "_Fake_", IsPrinted = false }); //добавить фиктивную
                    }
                    foreach (WorkInTechCard workInTechCard in tc.WorkInTechCards)
                    {
                        if (workInTechCard.OperationInWorks.Count == 0) //операции не определены
                        {
                            workInTechCard.OperationInWorks.Add(new OperationInWork { Number = "_Fake_", IsPrinted = false }); //добавить фиктивную
                        }
                    }
                }
                using App.AppDbContext _reportcontext = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
                try
                {
                    MainWindow.statusBar.WriteStatus("Получение данных техкарты ...", Cursors.Wait);
                    string _pathToReportTemplate = _reportcontext.Setting.FirstOrDefault(setting => setting.SettingParameterName == "PathToReportTemplate").SettingParameterValue;
                    string techCardFileTemplate = "TechCard.frx";
                    if (File.Exists(Path.Combine(_pathToReportTemplate, techCardFileTemplate)))
                    {
                        Reports.TechCardDataSet = techCards;
                        Reports.ReportFileName = Path.Combine(_pathToReportTemplate, techCardFileTemplate);
                        Reports.ReportMode = "TechCard";
                        Reports.RunReport();
                        foreach (TechCard techCard in techCards) //снять отметки На печать для всех операций и их родителей, добавленных как текущие
                        {
                            techCard.IsPrinted = (techCard.IsChecked || !techCard.IsPrinted) && techCard.IsPrinted; //не отмечана, но имеет признака На печать, тогда признак печати снять
                            foreach (WorkInTechCard workInTechCard in techCard.WorkInTechCards)
                            {
                                workInTechCard.IsPrinted = (workInTechCard.IsChecked || !workInTechCard.IsPrinted) && workInTechCard.IsPrinted;

                                foreach (OperationInWork operationInWork in workInTechCard.OperationInWorks)
                                {
                                    operationInWork.IsPrinted = false; //для операций снять отметку На печать без условий
                                }
                                Array arrayList = workInTechCard.OperationInWorks.Where(o => o.Number == "_Fake_").ToArray();
                                for (int i = 0; i < arrayList.Length; i++)
                                {
                                    _ = workInTechCard.OperationInWorks.Remove((OperationInWork)arrayList.GetValue(i)); //удалить Фейковые
                                }
                            }
                            Array array = techCard.WorkInTechCards.Where(o => o.Number == "_Fake_").ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                _ = techCard.WorkInTechCards.Remove((WorkInTechCard)array.GetValue(i)); //удалить Фейковые
                            }
                        }
                    }
                    else
                    {
                        _ = MessageBox.Show($"Не найден файл {techCardFileTemplate} !", "Ошибка формирования ТК", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка формирования ТК", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MainWindow.statusBar.ClearStatus();
                }
            }
            else
            {
                _ = MessageBox.Show(" Нет данных для печати ТК! ", "Печать техкарты", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        public static Visual GetFocusedDescendantByType(Visual element, Type type)
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

        public static Visual GetDescendantByType(Visual element, Type type, string name)
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

        //public static class FocusHelper
        //{
        //    public static void Focus(UIElement element)
        //    {
        //        _ = element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(delegate ()
        //          {
        //              element.Focus();
        //          }));
        //    }
        //}

        private void ClientSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (clientsViewSource?.View != null)
            {
                clientsViewSource.View.Refresh();
                clientsViewSource.View.MoveCurrentToFirst();
            }
        }

        private void ClientsViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is Client client && !string.IsNullOrWhiteSpace(ClientSearchTextBox.Text))
            {
                e.Accepted = client.Name.IndexOf(ClientSearchTextBox.Text, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
            else
            {
                e.Accepted = true;
            }
        }

        private void FindNewProductTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (productTypesViewSource?.View != null)
            {
                productTypesViewSource.View.Refresh();
                productTypesViewSource.View.MoveCurrentToFirst();
            }
        }

        private void ProductTypesViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is ProductType productType && !string.IsNullOrWhiteSpace(FindNewProductTextBox.Text))
            {
                e.Accepted = productType.Name.IndexOf(FindNewProductTextBox.Text, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
            else
            {
                e.Accepted = true;
            }
        }
    }

    public class CostTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommonTextBox { get; set; }
        public DataTemplate ComboboxTextBox { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ProductParameters productParameter)
            {
                return (productParameter.ReferencebookID > 0 || productParameter.IsRefbookOnRequest) ? ComboboxTextBox : CommonTextBox;
            }
            else if (item is OperationInWorkParameter operationInWorkParameter)
            {
                return (operationInWorkParameter.ReferencebookID > 0 || operationInWorkParameter.IsRefbookOnRequest) ? ComboboxTextBox : CommonTextBox;
            }
            return null;
        }
    }

    public class ReferencebookTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommonTextBlock { get; set; }
        public DataTemplate ComboboxRefbookRequestTextBox { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ProductParameters productParameter)
            {
                return productParameter.IsRefbookOnRequest ? ComboboxRefbookRequestTextBox : CommonTextBlock;
            }
            else if (item is OperationInWorkParameter operationInWorkParameter)
            {
                return operationInWorkParameter.IsRefbookOnRequest ? ComboboxRefbookRequestTextBox : CommonTextBlock;
            }
            return null;
        }
    }

    public class VisibilitySelector : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter.ToString() == "NoteTechCardStackPanel")
            {
                if (value != null && value is TechCard)
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "WorkInTechCardAttribute")
            {
                if (value != null && value is WorkInTechCard)
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "OperationInWorkAttribute")
            {
                if (value != null && value is OperationInWork)
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "WorkInTechCardTextAttribute")
            {
                if (value != null) //если есть определение операций
                {
                    return Visibility.Visible;
                }
            }
            if (parameter.ToString() == "WorkInTechCardComboBoxAttribute")
            {
                if (value != null && value is WorkInTechCard workInTechCard && workInTechCard.OperationInWorks.Count == 0) //если еще нет определения операций
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class NullDateTimeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime?)value).HasValue ? true : (object)false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ViewModel
    {
        public List<User> DesignerList { get; set; }
        public List<TypeOfActivity> TypeOfActivityList { get; set; }
        public bool ProductCostAndCostsKVDCostsChange { get; set; }
        public bool OrderCardDateCompletion { get; set; }
        public bool OrderCardPrintOrderForm { get; set; }
        public bool OrderCardPrintOrderFormForDesigner { get; set; }
        public bool OrderCardProductDesigner { get; set; }
        public bool OrderCardProductDateProductionLayout { get; set; }
        public bool OrderCardProductDateTransferDesigner { get; set; }
        public bool IsManager { get; set; }

        public ViewModel()
        {
            DesignerList = OrderWindow.designerList.ToList();
            TypeOfActivityList = OrderWindow.typeOfActivityList.ToList();
            ProductCostAndCostsKVDCostsChange = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductCostAndCostsKVDCostsChange");
            OrderCardDateCompletion = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardDateCompletion");
            OrderCardPrintOrderForm = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardPrintOrderForm");
            OrderCardPrintOrderFormForDesigner = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardPrintOrderFormForDesigner");
            OrderCardProductDesigner = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDesigner");
            OrderCardProductDateProductionLayout = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDateProductionLayout");
            OrderCardProductDateTransferDesigner = IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "OrderCardProductDateTransferDesigner");
            IsManager = !MainWindow.Userdata.IsAdmin && IGrantAccess.CheckGrantAccess(MainWindow.userIAccessMatrix, MainWindow.Userdata.RoleID, "ListManager");
        }
    }
}
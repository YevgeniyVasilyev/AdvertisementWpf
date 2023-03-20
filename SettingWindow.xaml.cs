using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AdvertisementWpf.Models;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using System.Collections;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        private CollectionViewSource settingViewSource;
        protected internal static OrderLegendColors orderLegendColors;
        private App.AppDbContext _context;

        public SettingWindow()
        {
            MainWindow.statusBar.WriteStatus("Инициализация формы ...", Cursors.Wait);

            InitializeComponent();

            MainWindow.statusBar.WriteStatus("Получение данных ...", Cursors.Wait);

            settingViewSource = (CollectionViewSource)FindResource(nameof(settingViewSource)); //найти описание view в разметке

            _context = new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
            orderLegendColors = new OrderLegendColors();
            try
            {
                _context.Setting.Load();
                settingViewSource.Source = _context.Setting.Local.ToObservableCollection();
                orderLegendColors = GetOrderLegendColorsSetting.OrderLegendColors(settingViewSource.View);
                //Grid gGrid = new Grid
                //{
                //    Margin = new Thickness(5, 0, 5, 0)
                //};
                //for (short ind = 0; ind < 3; ind++)
                //{
                //    ColumnDefinition col = new ColumnDefinition();
                //    if (ind == 0 || ind == 2)
                //    {
                //        col.Width = GridLength.Auto;
                //    }
                //    gGrid.ColumnDefinitions.Add(col);
                //}
                //short nRowIndex = 0;
                //foreach (Setting setting in settingViewSource.View)
                //{
                //    settingViewSource.View.MoveCurrentTo(setting);
                //    RowDefinition r = new RowDefinition();
                //    gGrid.RowDefinitions.Add(r);
                //    TextBlock textBlock = new TextBlock
                //    {
                //        VerticalAlignment = VerticalAlignment.Center,
                //        Text = setting.SettingParameterDescription,
                //        Margin = new Thickness(5, 5, 5, 5),
                //    };
                //    Grid.SetColumn(textBlock, 0);
                //    Grid.SetRow(textBlock, nRowIndex);
                //    Binding bind = new Binding
                //    {
                //        Source = settingViewSource,
                //        Path = new PropertyPath("SettingParameterValue"),
                //        Mode = BindingMode.TwoWay
                //    };
                //    TextBox textBox = new TextBox
                //    {
                //        VerticalAlignment = VerticalAlignment.Center,
                //        Margin = new Thickness(5, 5, 5, 5),
                //    };
                //    _ = textBox.SetBinding(TextBox.TextProperty, bind);
                //    Grid.SetColumn(textBox, 1);
                //    Grid.SetRow(textBox, nRowIndex);
                //    if (setting.SettingParameterName == "PathToFilesOfProduct" || setting.SettingParameterName == "PathToReportTemplate") //параметр вводится через OpenDialog
                //    {
                //        textBox.IsHitTestVisible = false;
                //        textBox.IsTabStop = false;
                //        textBox.Name = "PathToFile";
                //        StackPanel stackPanel = new StackPanel
                //        {
                //            Margin = new Thickness(5, 5, 5, 5),
                //            Orientation = Orientation.Horizontal
                //        };
                //        BitmapImage biOpen = new BitmapImage();
                //        biOpen.BeginInit();
                //        biOpen.UriSource = new Uri(@"\Images\free-icon-folder_open.png", UriKind.Relative);
                //        biOpen.EndInit();
                //        Image imgOpen = new Image
                //        {
                //            Width = 20,
                //            Height = 20,
                //            Stretch = Stretch.Fill,
                //            Source = biOpen
                //         };
                //        Button openDialogButton = new Button //кнопка открыть диалог выбора пути
                //        {
                //            Name = "OpenDialogButton",
                //            Background = Brushes.Transparent,
                //            BorderBrush = Brushes.Transparent,
                //            Style = (Style)FindResource(nameof(ButtonOfOrder)),
                //            ToolTip = "Открыть диалог выбора папки",
                //        };
                //        openDialogButton.Click += OpenDialogButton_Click;
                //        openDialogButton.Content = imgOpen;
                //        BitmapImage biDelete = new BitmapImage();
                //        biDelete.BeginInit();
                //        biDelete.UriSource = new Uri(@"\Images\free-icon-bin-Recycler.png", UriKind.Relative);
                //        biDelete.EndInit();
                //        Image imgDelete = new Image
                //        {
                //            Width = 20,
                //            Height = 20,
                //            Stretch = Stretch.Fill,
                //            Source = biDelete
                //        };
                //        Button clearPathButton = new Button //кнопка очистить выбранный путь
                //        {
                //            Name = "ClearPathButton",
                //            Background = Brushes.Transparent,
                //            BorderBrush = Brushes.Transparent,
                //            Style = (Style)FindResource(nameof(ButtonOfOrder)),
                //            ToolTip = "Очистить",
                //        };
                //        clearPathButton.Click += ClearPathButton_Click;
                //        clearPathButton.Content = imgDelete;
                //        _ = stackPanel.Children.Add(openDialogButton);
                //        _ = stackPanel.Children.Add(clearPathButton);
                //        Grid.SetColumn(stackPanel, 2);
                //        Grid.SetRow(stackPanel, nRowIndex);
                //        _ = gGrid.Children.Add(stackPanel);
                //    }
                //    nRowIndex++;
                //    _ = gGrid.Children.Add(textBlock);
                //    _ = gGrid.Children.Add(textBox);
                //}
                //RowDefinition row = new RowDefinition();
                //gGrid.RowDefinitions.Add(row);
                //StackPanel sPanel = new StackPanel
                //{
                //    Margin = new Thickness(5, 5, 5, 5),
                //    Orientation = Orientation.Horizontal
                //};
                //BitmapImage biSave = new BitmapImage();
                //biSave.BeginInit();
                //biSave.UriSource = new Uri(@"\Images\free-icon-floppy-disk_save.png", UriKind.Relative);
                //biSave.EndInit();
                //Image imgSave = new Image
                //{
                //    Width = 20,
                //    Height = 20,
                //    Stretch = Stretch.Fill,
                //    Source = biSave
                //};
                //Button buttonSave = new Button
                //{
                //    Name = "SaveSettingButton",
                //    Margin = new Thickness(5, 5, 5, 5),
                //    HorizontalAlignment = HorizontalAlignment.Left,
                //};
                //buttonSave.Click += SaveSettingButton_Click;
                //_ = sPanel.Children.Add(imgSave);
                //_ = sPanel.Children.Add(new Label { Content = "Сохранить" });
                //buttonSave.Content = sPanel;
                //Grid.SetColumn(buttonSave, 0);
                //Grid.SetRow(buttonSave, nRowIndex);
                //_ = gGrid.Children.Add(buttonSave);
                //Content = gGrid;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void SaveSettingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_context != null && settingViewSource != null)
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

        private void OpenDialogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string initialDirectory = "";
                if (sender is Button button && button.DataContext is Setting setting)
                {
                    _ = settingViewSource.View.MoveCurrentTo(setting);
                    if (SettingParameters.PathDialogParameterslist.Contains(setting.SettingParameterName))
                    {
                        initialDirectory = setting.SettingParameterValue;
                        CommonOpenFileDialog dialog = new CommonOpenFileDialog
                        {
                            IsFolderPicker = true,
                            InitialDirectory = initialDirectory
                        };
                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            (settingViewSource.View.CurrentItem as Setting).SettingParameterValue = System.IO.Path.GetFullPath(dialog.FileName);
                        }
                    }
                    else if (SettingParameters.ColorDialogParameterslist.Contains(setting.SettingParameterName))
                    {
                        System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog
                        {
                            AllowFullOpen = false,
                            AnyColor = true
                        };
                        if ((byte)colorDialog.ShowDialog() == 1) //1 == System.Windows.Forms.DialogResult.OK
                        {
                            System.Drawing.SolidBrush sb = new System.Drawing.SolidBrush(colorDialog.Color);
                            SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromArgb(sb.Color.A, sb.Color.R, sb.Color.G, sb.Color.B));
                            (settingViewSource.View.CurrentItem as Setting).SettingParameterValue = solidColorBrush.ToString();
                            settingViewSource.View.Refresh();
                        }
                    }
                    else if (SettingParameters.FontDialogParameterslist.Contains(setting.SettingParameterName))
                    {
                        string[] sFontParameters = setting.SettingParameterValue.Split(';'); //разбить строку не параметры
                        System.Windows.Forms.FontDialog fontDialog = new System.Windows.Forms.FontDialog
                        {
                            FontMustExist = true,
                            ShowColor = true,
                            AllowScriptChange = false,
                            AllowVectorFonts = false,
                        };
                        if ((byte)fontDialog.ShowDialog() == 1) //1 == System.Windows.Forms.DialogResult.OK
                        {
                            //FontFamily, FontSize, FontStyle, FontWeight, Foreground
                            (settingViewSource.View.CurrentItem as Setting).SettingParameterValue = $"{fontDialog.Font.FontFamily.Name};{fontDialog.Font.Size};{fontDialog.Font.Style};" +
                            $"{(fontDialog.Font.Bold ? FontWeights.Bold : FontWeights.Normal)};{fontDialog.Color.Name}";
                            if (setting.SettingParameterName == "NotPaidOrderFontOnMainWnd")
                            {
                                orderLegendColors.NotPaidFont.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fontDialog.Color.Name));
                                orderLegendColors.NotPaidFont.FontSize = fontDialog.Font.Size;
                                orderLegendColors.NotPaidFont.FontFamily = new FontFamily(fontDialog.Font.FontFamily.Name);
                                orderLegendColors.NotPaidFont.FontStyle = fontDialog.Font.Style.ToString().Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;
                                orderLegendColors.NotPaidFont.FontWeight = fontDialog.Font.Bold ? FontWeights.Bold : FontWeights.Normal;
                            }
                            else if (setting.SettingParameterName == "PartiallyPaidOrderFontOnMainWnd")
                            {
                                orderLegendColors.PartiallyPaidFont.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fontDialog.Color.Name));
                                orderLegendColors.PartiallyPaidFont.FontSize = fontDialog.Font.Size;
                                orderLegendColors.PartiallyPaidFont.FontFamily = new FontFamily(fontDialog.Font.FontFamily.Name);
                                orderLegendColors.PartiallyPaidFont.FontStyle = fontDialog.Font.Style.ToString().Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;
                                orderLegendColors.PartiallyPaidFont.FontWeight = fontDialog.Font.Bold ? FontWeights.Bold : FontWeights.Normal;
                            }
                            else if (setting.SettingParameterName == "OverPaidOrderFontOnMainWnd")
                            {
                                orderLegendColors.OverPaidFont.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fontDialog.Color.Name));
                                orderLegendColors.OverPaidFont.FontSize = fontDialog.Font.Size;
                                orderLegendColors.OverPaidFont.FontFamily = new FontFamily(fontDialog.Font.FontFamily.Name);
                                orderLegendColors.OverPaidFont.FontStyle = fontDialog.Font.Style.ToString().Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;
                                orderLegendColors.OverPaidFont.FontWeight = fontDialog.Font.Bold ? FontWeights.Bold : FontWeights.Normal;
                            }
                            settingViewSource.View.Refresh();
                        }
                    }
                }
                _ = Activate();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка выбора папки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Setting setting)
            {
                _ = settingViewSource.View.MoveCurrentTo(setting);
                setting.SettingParameterValue = "";
                if (SettingParameters.ColorDialogParameterslist.Contains(setting.SettingParameterName) || SettingParameters.FontDialogParameterslist.Contains(setting.SettingParameterName))
                {
                    if (SettingParameters.FontDialogParameterslist.Contains(setting.SettingParameterName))
                    {
                        orderLegendColors.OrderLegendColorsFontSetToDefault(ref orderLegendColors, setting.SettingParameterName); //установить по умолчанию
                    }
                    settingViewSource.View.Refresh();
                }
            }
        }

        private void ValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (settingViewSource.View != null)
            {
                if (sender is TextBox txtBox && txtBox.GetBindingExpression(TextBox.TextProperty).DataItem is Setting setting)
                {
                    _ = settingViewSource.View.MoveCurrentTo(setting);
                    return;
                }
            }
        }
    }

    public static class SettingParameters
    {
        public static List<string> PathDialogParameterslist = new List<string> { "PathToFilesOfProduct", "PathToReportTemplate" };
        public static List<string> ColorDialogParameterslist = new List<string> { "NotPaidOrderColorOnMainWnd", "PartiallyPaidOrderColorOnMainWnd", "OverPaidOrderColorOnMainWnd" };
        public static List<string> FontDialogParameterslist = new List<string> { "NotPaidOrderFontOnMainWnd", "PartiallyPaidOrderFontOnMainWnd", "OverPaidOrderFontOnMainWnd" };
    }

    public class SettingButtonTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EmptyTextBlock { get; set; }
        public DataTemplate ButtonTextBlock { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Setting setting = item as Setting;
            return SettingParameters.PathDialogParameterslist.Contains(setting.SettingParameterName) || SettingParameters.ColorDialogParameterslist.Contains(setting.SettingParameterName) ||
                SettingParameters.FontDialogParameterslist.Contains(setting.SettingParameterName)
                ? ButtonTextBlock
                : EmptyTextBlock;
        }
    }

    public class SettingEditingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommonTextBox { get; set; }
        public DataTemplate CommonTextBlock { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Setting setting = item as Setting;
            return SettingParameters.PathDialogParameterslist.Contains(setting.SettingParameterName) || SettingParameters.ColorDialogParameterslist.Contains(setting.SettingParameterName) ||
                SettingParameters.FontDialogParameterslist.Contains(setting.SettingParameterName)
                ? CommonTextBlock
                : CommonTextBox;
        }
    }

    public class OrderLegendColorSelector : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Setting setting = (Setting)value;
            if (SettingParameters.ColorDialogParameterslist.Contains(setting.SettingParameterName) && !string.IsNullOrWhiteSpace(setting.SettingParameterValue))
            {
                if ((string)parameter == "Background")
                {
                    Color color = (Color)ColorConverter.ConvertFromString(setting.SettingParameterValue);
                    return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                }
                else
                {
                    if (setting.SettingParameterName == "NotPaidOrderColorOnMainWnd")
                    {
                        return (string)parameter switch
                        {
                            "Foreground" => SettingWindow.orderLegendColors.NotPaidFont.Foreground,
                            "FontSize" => SettingWindow.orderLegendColors.NotPaidFont.FontSize,
                            "FontFamily" => SettingWindow.orderLegendColors.NotPaidFont.FontFamily,
                            "FontStyle" => SettingWindow.orderLegendColors.NotPaidFont.FontStyle,
                            "FontWeight" => SettingWindow.orderLegendColors.NotPaidFont.FontWeight,
                            _ => null,
                        };
                    }
                    else if (setting.SettingParameterName == "PartiallyPaidOrderColorOnMainWnd")
                    {
                        return (string)parameter switch
                        {
                            "Foreground" => SettingWindow.orderLegendColors.PartiallyPaidFont.Foreground,
                            "FontSize" => SettingWindow.orderLegendColors.PartiallyPaidFont.FontSize,
                            "FontFamily" => SettingWindow.orderLegendColors.PartiallyPaidFont.FontFamily,
                            "FontStyle" => SettingWindow.orderLegendColors.PartiallyPaidFont.FontStyle,
                            "FontWeight" => SettingWindow.orderLegendColors.PartiallyPaidFont.FontWeight,
                            _ => null,
                        };
                    }
                    else if (setting.SettingParameterName == "OverPaidOrderColorOnMainWnd")
                    {
                        return (string)parameter switch
                        {
                            "Foreground" => SettingWindow.orderLegendColors.OverPaidFont.Foreground,
                            "FontSize" => SettingWindow.orderLegendColors.OverPaidFont.FontSize,
                            "FontFamily" => SettingWindow.orderLegendColors.OverPaidFont.FontFamily,
                            "FontStyle" => SettingWindow.orderLegendColors.OverPaidFont.FontStyle,
                            "FontWeight" => SettingWindow.orderLegendColors.OverPaidFont.FontWeight,
                            _ => null,
                        };
                    }
                }
            }
            //return null;
            return (string)parameter switch
            {
                "Foreground" => SettingWindow.orderLegendColors.DefaultFont.Foreground,
                "FontSize" => SettingWindow.orderLegendColors.DefaultFont.FontSize,
                "FontFamily" => SettingWindow.orderLegendColors.DefaultFont.FontFamily,
                "FontStyle" => SettingWindow.orderLegendColors.DefaultFont.FontStyle,
                "FontWeight" => SettingWindow.orderLegendColors.DefaultFont.FontWeight,
                _ => null,
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public static class GetOrderLegendColorsSetting
    {
        public static OrderLegendColors OrderLegendColors(IEnumerable settings)
        {
            OrderLegendColors orderLegendColors = new OrderLegendColors();
            foreach (Setting setting in settings)
            {
                Color color;
                if (SettingParameters.ColorDialogParameterslist.Contains(setting.SettingParameterName))
                {
                    if (string.IsNullOrWhiteSpace(setting.SettingParameterValue)) //ПУСТЫОЕ ЗНАЧЕНИЕ НЕДОПУСТИМО !!!
                    {
                        continue;
                    }
                    color = (Color)ColorConverter.ConvertFromString(setting.SettingParameterValue);
                }
                if (setting.SettingParameterName == "NotPaidOrderColorOnMainWnd")
                {
                    //orderLegendColors.NotPaidColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                    orderLegendColors.NotPaidColor = color; // System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
                }
                else if (setting.SettingParameterName == "PartiallyPaidOrderColorOnMainWnd")
                {
                    //orderLegendColors.PartiallyPaidColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                    orderLegendColors.PartiallyPaidColor = color; // System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
                }
                else if (setting.SettingParameterName == "OverPaidOrderColorOnMainWnd")
                {
                    //orderLegendColors.OverPaidColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                    orderLegendColors.OverPaidColor = color; // System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
                }
                if (SettingParameters.FontDialogParameterslist.Contains(setting.SettingParameterName))
                {
                    if (!string.IsNullOrWhiteSpace(setting.SettingParameterValue)) //значение задано
                    {
                        string[] sFontParameters = setting.SettingParameterValue.Split(';'); //разбить строку не параметры
                        if (setting.SettingParameterName == "NotPaidOrderFontOnMainWnd")
                        {
                            orderLegendColors.NotPaidFont.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sFontParameters[4]));
                            orderLegendColors.NotPaidFont.FontSize = Convert.ToDouble(sFontParameters[1]);
                            orderLegendColors.NotPaidFont.FontFamily = new FontFamily(sFontParameters[0]);
                            orderLegendColors.NotPaidFont.FontStyle = sFontParameters[2].Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;
                            orderLegendColors.NotPaidFont.FontWeight = sFontParameters[3].Contains("Bold") ? FontWeights.Bold : FontWeights.Normal;
                        }
                        else if (setting.SettingParameterName == "PartiallyPaidOrderFontOnMainWnd")
                        {
                            orderLegendColors.PartiallyPaidFont.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sFontParameters[4]));
                            orderLegendColors.PartiallyPaidFont.FontSize = Convert.ToDouble(sFontParameters[1]);
                            orderLegendColors.PartiallyPaidFont.FontFamily = new FontFamily(sFontParameters[0]);
                            orderLegendColors.PartiallyPaidFont.FontStyle = sFontParameters[2].Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;
                            orderLegendColors.PartiallyPaidFont.FontWeight = sFontParameters[3].Contains("Bold") ? FontWeights.Bold : FontWeights.Normal;
                        }
                        else if (setting.SettingParameterName == "OverPaidOrderFontOnMainWnd")
                        {
                            orderLegendColors.OverPaidFont.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sFontParameters[4]));
                            orderLegendColors.OverPaidFont.FontSize = Convert.ToDouble(sFontParameters[1]);
                            orderLegendColors.OverPaidFont.FontFamily = new FontFamily(sFontParameters[0]);
                            orderLegendColors.OverPaidFont.FontStyle = sFontParameters[2].Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;
                            orderLegendColors.OverPaidFont.FontWeight = sFontParameters[3].Contains("Bold") ? FontWeights.Bold : FontWeights.Normal;
                        }
                    }
                }
            }
            return orderLegendColors;
        }
    }

    public class OrderLegendColors
    {
        public Color NotPaidColor { get; set; } = Colors.Red;
        public Color PartiallyPaidColor { get; set; } = Colors.Gray;
        public Color OverPaidColor { get; set; } = Colors.Yellow;
        private static ListViewItem listView = new ListViewItem();
 
        public FontPaid DefaultFont = new FontPaid
        {
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = SystemFonts.MessageFontSize,
            FontStyle = SystemFonts.MessageFontStyle,
            FontWeight = SystemFonts.MessageFontWeight,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF042271"))
        };
        public FontPaid NotPaidFont = new FontPaid { };
        public FontPaid PartiallyPaidFont = new FontPaid { };
        public FontPaid OverPaidFont = new FontPaid { };
        public struct FontPaid
        {
            public FontFamily FontFamily; // = System.Windows.SystemFonts.MessageFontFamily;
            public double FontSize; //= System.Windows.SystemFonts.MessageFontSize;
            public FontStyle FontStyle; // = System.Windows.SystemFonts.MessageFontStyle;
            public FontWeight FontWeight; // = System.Windows.SystemFonts.MessageFontWeight;
            public Brush Foreground; // = System.Windows.SystemColors.WindowTextBrush;
        };

        public OrderLegendColors()
        {
            NotPaidFont.FontFamily = DefaultFont.FontFamily;
            NotPaidFont.FontSize = DefaultFont.FontSize;
            NotPaidFont.FontStyle = DefaultFont.FontStyle;
            NotPaidFont.FontWeight = DefaultFont.FontWeight;
            NotPaidFont.Foreground = DefaultFont.Foreground;

            PartiallyPaidFont.FontFamily = DefaultFont.FontFamily;
            PartiallyPaidFont.FontSize = DefaultFont.FontSize;
            PartiallyPaidFont.FontStyle = DefaultFont.FontStyle;
            PartiallyPaidFont.FontWeight = DefaultFont.FontWeight;
            PartiallyPaidFont.Foreground = DefaultFont.Foreground;

            OverPaidFont.FontFamily = DefaultFont.FontFamily;
            OverPaidFont.FontSize = DefaultFont.FontSize;
            OverPaidFont.FontStyle = DefaultFont.FontStyle;
            OverPaidFont.FontWeight = DefaultFont.FontWeight;
            OverPaidFont.Foreground = DefaultFont.Foreground;
        }

        public void OrderLegendColorsFontSetToDefault(ref OrderLegendColors orderLegendColors, string sMode)
        {
            if (sMode.Contains("NotPaid"))
            {
                orderLegendColors.NotPaidFont.FontFamily = DefaultFont.FontFamily;
                orderLegendColors.NotPaidFont.FontSize = DefaultFont.FontSize;
                orderLegendColors.NotPaidFont.FontStyle = DefaultFont.FontStyle;
                orderLegendColors.NotPaidFont.FontWeight = DefaultFont.FontWeight;
                orderLegendColors.NotPaidFont.Foreground = DefaultFont.Foreground;
            }
            else if (sMode.Contains("PartiallyPaid"))
            {
                orderLegendColors.PartiallyPaidFont.FontFamily = DefaultFont.FontFamily;
                orderLegendColors.PartiallyPaidFont.FontSize = DefaultFont.FontSize;
                orderLegendColors.PartiallyPaidFont.FontStyle = DefaultFont.FontStyle;
                orderLegendColors.PartiallyPaidFont.FontWeight = DefaultFont.FontWeight;
                orderLegendColors.PartiallyPaidFont.Foreground = DefaultFont.Foreground;
            }
            else if (sMode.Contains("OverPaid"))
            {
                orderLegendColors.OverPaidFont.FontFamily = DefaultFont.FontFamily;
                orderLegendColors.OverPaidFont.FontSize = DefaultFont.FontSize;
                orderLegendColors.OverPaidFont.FontStyle = DefaultFont.FontStyle;
                orderLegendColors.OverPaidFont.FontWeight = DefaultFont.FontWeight;
                orderLegendColors.OverPaidFont.Foreground = DefaultFont.Foreground;
            }
        }
    }
}

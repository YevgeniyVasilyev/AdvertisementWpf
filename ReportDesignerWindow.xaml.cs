using System;
using System.Windows;
using System.Windows.Input;
using FastReport;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ReportDesignerWindow.xaml
    /// </summary>
    public partial class ReportDesignerWindow : Window
    {
        public ReportDesignerWindow()
        {
            MainWindow.statusBar.WriteStatus("Загрузка дизайнера ...", Cursors.Wait);

            InitializeComponent();

            try
            {
                designer.Report = new Report();

                // in case you need more control over how to load the designer configuration, use this code
                designer.HandleWindowEvents = false;
                Loaded += (s, e) =>
                {
                    designer.RestoreConfig();
                    designer.StartAutoSave();
                };
                Closing += (s, e) =>
                {
                    designer.ParentWindowClosing(e);
                    if (!e.Cancel)
                    {
                        designer.SaveConfig();
                        designer.StopAutoSave();
                    }
                };
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка загрузки дизайнера", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }
    }
}

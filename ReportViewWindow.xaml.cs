using System;
using System.Windows;
using FastReport;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для ReportViewWindow.xaml
    /// </summary>
    public partial class ReportViewWindow : Window
    {
        public ReportViewWindow(Report report)
        {
            InitializeComponent();
            try
            {
                report.WpfPreview = previewControl;
                _ = report.Prepare();
                report.ShowPrepared();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка отображения отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

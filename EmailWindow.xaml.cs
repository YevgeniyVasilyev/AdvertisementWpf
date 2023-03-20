using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using System.Collections;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AdvertisementWpf
{
    /// <summary>
    /// Логика взаимодействия для EmailWindow.xaml
    /// </summary>
    public partial class EmailWindow : Window
    {
        private string SMTPserverName = "";

        public EmailWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //if (_context != null)
            //{
            //    _context.Dispose();
            //    _context = null;
            //}
            MainWindow.statusBar.ClearStatus();
        }

        private void SendEmailButton_Click(object sender, RoutedEventArgs e)
        {
            CreateArchive archive = new CreateArchive(@"D:\123", @"D:\321\archive.zip");
            archive.Start();
            return;
            MimeMessage message = new MimeMessage();
            Multipart multipart = new Multipart("mixed");
            SmtpClient client = new SmtpClient();
            try
            {
                message.From.Add(new MailboxAddress("", EmailSenderAddress.Text));
                message.To.Add(new MailboxAddress("", EmailRecipientAddress.Text));
                message.Subject = EmailSubject.Text;

                TextPart body = new TextPart("plain")
                {
                    Text = EmailBody.Text
                };
                //MimePart attachment = new MimePart("image", "gif")
                //{
                //    Content = new MimeContent(System.IO.File.OpenRead(path), ContentEncoding.Default),
                //    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                //    ContentTransferEncoding = ContentEncoding.Base64,
                //    FileName = System.IO.Path.GetFileName(path)
                //};
                multipart.Add(body);
                //multipart.Add(attachment);

                message.Body = multipart;
                client.Connect(SMTPserverName, SMTPserverName.ToLower().Contains("gmail") ? 587 : 25, SecureSocketOptions.Auto);
                client.Authenticate(SenderName.Text, SenderName.Text.Contains("inbox") ? "jaCA3yUTwiQRfKWd3qCM" : SenderPassword.Password); //"jaCA3yUTwiQRfKWd3qCM"
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка отправки сообщения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                message.Dispose();
                multipart.Dispose();
                client.Dispose();
                MainWindow.statusBar.ClearStatus();
            }
        }

        private void EmailSenderAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmailSenderAddress.Text.Contains("@") && RegexUtilities.IsValidEmail(EmailSenderAddress.Text))
            {
                SMTPserverName = $"smtp.{EmailSenderAddress.Text[(EmailSenderAddress.Text.IndexOf('@') + 1)..]}";
            }
            SenderName.Text = EmailSenderAddress.Text;
        }
    }
}

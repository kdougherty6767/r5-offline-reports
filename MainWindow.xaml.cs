using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using System.IO;
using System.Reflection.Metadata;
using System.Windows.Documents;

namespace R5OfflineReports
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void FetchHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text;

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                MessageBox.Show("Please enter a valid IP address.");
                return;
            }

            string htmlContent = await FetchHtmlAsync(ipAddress);

            if (!string.IsNullOrEmpty(htmlContent))
            {
                HtmlTextBox.Text = htmlContent;
            }
            else
            {
                MessageBox.Show("Failed to fetch HTML content.");
            }
        }

        private async Task<string> FetchHtmlAsync(string ipAddress)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Add "http://" prefix if not provided
                    if (!ipAddress.StartsWith("http://") && !ipAddress.StartsWith("https://"))
                    {
                        ipAddress = "http://" + ipAddress;
                    }

                    HttpResponseMessage response = await client.GetAsync(ipAddress);
                    response.EnsureSuccessStatusCode();

                    string htmlContent = await response.Content.ReadAsStringAsync();
                    return htmlContent;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                return null;
            }
        }

        private void SaveAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            string htmlContent = HtmlTextBox.Text;

            if (string.IsNullOrEmpty(htmlContent))
            {
                MessageBox.Show("No HTML content to save.");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Would you like to save this content as a PDF?", "Save as PDF", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                SaveHtmlAsPdf(htmlContent);
                MessageBox.Show("PDF saved successfully!");
                // Clear text box and go back to initial state
                IpAddressTextBox.Text = string.Empty;
                HtmlTextBox.Text = string.Empty;
            }
        }

        private void SaveHtmlAsPdf(string htmlContent)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    iTextSharp.text.Document pdfDoc = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                    pdfDoc.Open();
                    pdfDoc.Add(new iTextSharp.text.Paragraph(htmlContent));
                    pdfDoc.Close();
                    writer.Close();
                }
            }
        }
    }
}

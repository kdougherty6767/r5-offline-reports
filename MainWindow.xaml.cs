using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using System.IO;
using System.Reflection.Metadata;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace R5OfflineReports
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class DataFetcher
        {
            public string FetchHtml(string url)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        // Optionally set any headers here if necessary
                        // client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string htmlContent = client.DownloadString(url);
                        return htmlContent;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error fetching HTML: {e.Message}");
                    return null;
                }
            }
        }

        private void FetchHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text; // Assuming you have a TextBox for IP address input
            if (string.IsNullOrEmpty(ipAddress))
            {
                MessageBox.Show("Please enter a valid IP address.");
                return;
            }

            string url = $"http://{ipAddress}"; // Construct the URL from the IP address
            DataFetcher fetcher = new DataFetcher();
            string htmlContent = fetcher.FetchHtml(url);

            if (htmlContent != null)
            {
                HtmlTextBox.Text = htmlContent; // Assuming you have a TextBox to display the HTML
            }
            else
            {
                MessageBox.Show("Failed to fetch HTML content.");
            }
        }



        public string FormatHtmlBeforePdf(string htmlContent)
        {
            // Step 1: Remove all hyperlinks and buttons
            string noLinks = Regex.Replace(htmlContent, @"<a[^>]*>.*?</a>", "", RegexOptions.Singleline);
            string noButtons = Regex.Replace(noLinks, @"<input[^>]*type=""submit""[^>]*>", "");

            // Step 2: Filter h4 elements to keep only those containing "OFFLINE"
            MatchCollection allH4 = Regex.Matches(noButtons, @"<h4[^>]*>.*?</h4>", RegexOptions.Singleline);
            string htmlWithoutH4 = Regex.Replace(noButtons, @"<h4[^>]*>.*?</h4>", "", RegexOptions.Singleline);
            foreach (Match h4 in allH4)
            {
                if (h4.Value.Contains("OFFLINE"))
                {
                    htmlWithoutH4 += h4.Value;  // Add back only the h4s with "OFFLINE"
                }
            }

            // Step 3: Remove the word "Supervision" from all h4 elements
            string finalHtml = Regex.Replace(htmlWithoutH4, "Supervision", "");

            return finalHtml;
        }

        private void SaveAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            string rawHtmlContent = HtmlTextBox.Text;

            if (string.IsNullOrEmpty(rawHtmlContent))
            {
                MessageBox.Show("No HTML content to save.");
                return;
            }

            // Ask the user if they want to save the content as a PDF
            MessageBoxResult result = MessageBox.Show("Would you like to save this content as a PDF?", "Save as PDF", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                // Format the HTML before saving
                string formattedHtml = FormatHtmlBeforePdf(rawHtmlContent);

                // Save the formatted HTML as PDF
                SaveHtmlAsPdf(formattedHtml);

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
                    pdfDoc.Add(new iTextSharp.text.Paragraph(htmlContent)); // Assumes you have a way to convert HTML to text for PDF
                    pdfDoc.Close();
                    writer.Close();
                }
            }
        }

    }
}

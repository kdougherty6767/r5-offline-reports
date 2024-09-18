using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PuppeteerSharp;
using HtmlAgilityPack;
using Microsoft.Win32;
using System.Reflection.Metadata;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Runtime.Loader;
using PuppeteerSharp.Media;

namespace R5OfflineReports
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

public class HtmlFetcher
    {
        public static string FetchHtmlWithTcpClient(string ipAddress, int port, string requestUri)
        {
            try
            {
                using (var client = new TcpClient(ipAddress, port))
                using (var networkStream = client.GetStream())
                using (var reader = new StreamReader(networkStream, Encoding.ASCII))  // Assuming server responds with ASCII encoding
                using (var writer = new StreamWriter(networkStream, Encoding.ASCII))
                {
                    // Construct the HTTP request
                    var request = $"GET {requestUri} HTTP/1.0\r\nHost: {ipAddress}\r\n\r\n";
                    writer.Write(request);
                    writer.Flush();

                    // Read the response
                    StringBuilder response = new StringBuilder();
                    while (!reader.EndOfStream)
                    {
                        response.Append((char)reader.Read());
                    }

                    // The response includes the HTTP headers and the body
                    // Extract the HTML content after the headers
                    var responseString = response.ToString();
                    var responseBody = responseString.Substring(responseString.IndexOf("\r\n\r\n") + 4);

                    return responseBody;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                return null;
            }
        }
    }


    private void FetchHtmlButton_Click(object sender, RoutedEventArgs e)
    {
        string ipAddress = IpAddressTextBox.Text; // IP address input
        if (string.IsNullOrEmpty(ipAddress))
        {
            MessageBox.Show("Please enter a valid IP address.");
            return;
        }

        // For testing - 127.0.0.1:5500/RawHTML.html
        int port = 80; // Default HTTP port
        string url = ""; // Default to root path
        string htmlContent = HtmlFetcher.FetchHtmlWithTcpClient(ipAddress, port, url);
        string formattedHtml = FormatHtmlBeforePdf(htmlContent);

        if (!string.IsNullOrEmpty(htmlContent))
        {
            HtmlTextBox.Text = formattedHtml;
        }
        else
        {
            MessageBox.Show("Failed to fetch HTML content.");
        }
    }




public string FormatHtmlBeforePdf(string htmlContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Step 1: Keep only <h3>, <h4>, and <h5> elements. Remove everything else.
        var nodesToKeep = doc.DocumentNode.SelectNodes("//h3|//h4|//h5")?.ToList() ?? new List<HtmlNode>();
        doc.DocumentNode.RemoveAllChildren();  // Clear the document
        foreach (var node in nodesToKeep)
        {
            doc.DocumentNode.AppendChild(node);  // Append only the <h3>, <h4>, and <h5> elements back
        }

        // Step 2: Filter <h3> elements to keep only those containing the word "Issues"
        var h3Elements = doc.DocumentNode.SelectNodes("//h3").ToList();
        foreach (var h3 in h3Elements)
        {
            if (!h3.InnerText.Contains("Issues"))
            {
                h3.Remove();
            }
                else
                {
                    // Remove child elements but keep their text content
                    h3.InnerHtml = h3.InnerText;
                }

            }

        // Step 3: Filter <h5> elements and remove those containing "Supervision" but not "OFFLINE"
        var h5Elements = doc.DocumentNode.SelectNodes("//h5").ToList();
        foreach (var h5 in h5Elements)
        {
            if (h5.InnerText.Contains("Supervision") && !h5.InnerText.Contains("OFFLINE"))
            {
                h5.Remove();
            }
                else
                {
                    // Remove child elements but keep their text content
                    h5.InnerHtml = h5.InnerText;
                }
            }

        // Step 4: Filter <h4> elements to keep only those containing "OFFLINE"
        var h4Elements = doc.DocumentNode.SelectNodes("//h4").ToList();
        foreach (var h4 in h4Elements)
        {
            if (!h4.InnerText.Contains("OFFLINE") && !h4.InnerText.Contains("Issues"))
            {
                h4.Remove();
            }
                else
                {
                    // Remove child elements but keep their text content
                    h4.InnerHtml = h4.InnerText;
                }
            }

        // Step 5: Remove the word "Supervision:" from all remaining elements
        foreach (var element in doc.DocumentNode.SelectNodes("//h3|//h4|//h5"))
        {
            element.InnerHtml = element.InnerHtml.Replace("Supervision:", "").Trim();
        }

        // Step 6: Remove all inline styling
        var elementsWithStyle = doc.DocumentNode.SelectNodes("//*[@style]");
        if (elementsWithStyle != null)
        {
            foreach (var element in elementsWithStyle)
            {
                element.Attributes["style"].Remove();
            }
        }

        // Step 7: Normalize all remaining text (e.g., remove excess whitespace)
        foreach (var element in doc.DocumentNode.Descendants())
        {
            if (element.NodeType == HtmlNodeType.Text)
            {
                element.InnerHtml = System.Text.RegularExpressions.Regex.Replace(element.InnerText, @"\s+", " ").Trim();
            }
        }

        // Step 8: Add an <h1> element at the top of the document that says "R5 Offline Report"
        var bodyNode = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
        var h1Node = doc.CreateElement("h1");
        h1Node.InnerHtml = "R5 Offline Report";
        bodyNode.PrependChild(h1Node);

            // Step 9: Add HTML boilerplate
            var finalHtml = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>R5 Offline Report</title>
    <style>
        h1 {{ color: blue; }}
        h3 {{ color: black; }}
        h4 {{ color: red; }}
    </style>
</head>
<body>
    {doc.DocumentNode.InnerHtml}
</body>
</html>";

            // Return the complete HTML document
            return finalHtml;

    }

        public class PdfService
        {
            public async Task ConvertHtmlToPdf(string htmlContent, string outputPath)
            {
                // Create a BrowserFetcher instance and download the latest version of Chromium
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync(); // Automatically downloads the latest Chromium

                // Launch Chromium in headless mode
                using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }))
                using (var page = await browser.NewPageAsync())
                {
                    // Set the content of the page to the HTML you want to convert
                    await page.SetContentAsync(htmlContent);

                    // Save the page as a PDF
                    await page.PdfAsync(outputPath, new PdfOptions
                    {
                        Format = PaperFormat.A4,
                        PrintBackground = true // Include CSS background colors in the PDF
                    });
                }
            }
        }


        private async void SaveAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
            saveFileDialog.DefaultExt = "pdf";
            saveFileDialog.Title = "Save PDF File";
            saveFileDialog.FileName = "R5 Offline Report";

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputPath = saveFileDialog.FileName;
                //string testHtml = "<html><body><h1>Test PDF</h1><p>This is a test.</p></body></html>";
                // Assuming formattedHtml contains the final HTML to convert
                string formattedHtml = FormatHtmlBeforePdf(HtmlTextBox.Text);

                try
                {
                    PdfService pdfService = new PdfService();
                    await pdfService.ConvertHtmlToPdf(formattedHtml, outputPath);
                    MessageBox.Show("PDF saved successfully at " + outputPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save PDF: {ex.Message}");
                }
            }
        }





    }
}

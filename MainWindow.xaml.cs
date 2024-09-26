using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows.Media;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace R5OfflineReports
{
    public partial class MainWindow : Window
    {
        private string StoredFormattedHtml = ""; // Store the fetched and formatted HTML for later use

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
                    using (var reader = new StreamReader(networkStream, Encoding.ASCII))
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

        private async void FetchHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text; // IP address input
            if (string.IsNullOrEmpty(ipAddress))
            {
                MessageBox.Show("Please enter a valid IP address.");
                return;
            }

            int port = 80; // Default HTTP port
            string url = "/"; // Default to some URI

            // Clear the status message before fetching
            StatusMessage.Text = "Trying...";
            StatusMessage.Foreground = new SolidColorBrush(Colors.Orange);
            StatusMessage.Visibility = Visibility.Visible;

            //Set the PDF Save button back to collapsed until html success
            SaveAsPdfButton.Visibility = Visibility.Collapsed;

            try
            {
                // Fetch HTML using the TcpClient
                string htmlContent = await Task.Run(() => HtmlFetcher.FetchHtmlWithTcpClient(ipAddress, port, url));

                if (!string.IsNullOrEmpty(htmlContent))
                {
                    StatusMessage.Text = "HTML fetch successful!";
                    StatusMessage.Foreground = new SolidColorBrush(Colors.Green);

                    // Apply all the formatting to the fetched HTML
                    StoredFormattedHtml = FormatHtmlBeforePdf(htmlContent);

                    // Make the "Save as PDF" button visible once HTML is successfully fetched
                    SaveAsPdfButton.Visibility = Visibility.Visible;
                }
                else
                {
                    StatusMessage.Text = "Failed to fetch HTML content.";
                    StatusMessage.Foreground = new SolidColorBrush(Colors.Red);
                    SaveAsPdfButton.Visibility = Visibility.Collapsed; // Keep the button hidden if fetching fails
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to fetch HTML: {ex.Message}";
                StatusMessage.Foreground = new SolidColorBrush(Colors.Red);
                SaveAsPdfButton.Visibility = Visibility.Collapsed; // Keep the button hidden if fetching fails
            }

            // Display the status message
            StatusMessage.Visibility = Visibility.Visible;
        }

        // The original method for formatting HTML content before splitting it into sections
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

            // Step 2: Filter <h3> elements and remove those that do not contain 'color: Red' or 'color: Gray' in their inline style
            var h3Elements = doc.DocumentNode.SelectNodes("//h3").ToList();
            foreach (var h3 in h3Elements)
            {
                // Check if the h3 element has a style attribute and contains 'color: Red' or 'color: Gray'
                var styleAttribute = h3.GetAttributeValue("style", string.Empty).ToLower();

                if (!styleAttribute.Contains("color: red") && !styleAttribute.Contains("color: gray"))
                {
                    h3.Remove(); // Remove h3 elements that don't have 'color: Red' or 'color: Gray' in their inline style
                }
                else
                {
                    // Remove child elements but keep their text content
                    h3.InnerHtml = h3.InnerText;
                }
            }


            // Step 3: Filter <h5> elements and remove those not containing 'color: Red' in their inline style
            var h5Elements = doc.DocumentNode.SelectNodes("//h5").ToList();
            foreach (var h5 in h5Elements)
            {
                // Check if the h5 element has a style attribute and contains 'color: Red'
                var styleAttribute = h5.GetAttributeValue("style", string.Empty).ToLower();

                if (!styleAttribute.Contains("color: red"))
                {
                    h5.Remove(); // Remove h5 elements that don't have 'color: Red' in their inline style
                }
                else
                {
                    // Remove child elements but keep their text content
                    h5.InnerHtml = h5.InnerText;
                }
            }


            // Step 3: Filter <h4> elements and remove those that do not contain 'color: Red' or 'color: Gray' in their inline style
            var h4Elements = doc.DocumentNode.SelectNodes("//h4").ToList();
            foreach (var h4 in h4Elements)
            {
                // Check if the h4 element has a style attribute and contains 'color: Red' or 'color: Gray'
                var styleAttribute = h4.GetAttributeValue("style", string.Empty).ToLower();

                if (!styleAttribute.Contains("color: red") && !styleAttribute.Contains("color: gray"))
                {
                    h4.Remove(); // Remove h4 elements that don't have 'color: Red' or 'color: Gray' in their inline style
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

        // Split the already formatted HTML into sections based on <h3> elements
        public List<HtmlSection> FormatHtmlIntoSections(string formattedHtmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(formattedHtmlContent);

            List<HtmlSection> sections = new List<HtmlSection>();

            // Find all <h3> elements to create sections
            var h3Elements = doc.DocumentNode.SelectNodes("//h3")?.ToList();
            if (h3Elements == null) return sections;

            for (int i = 0; i < h3Elements.Count; i++)
            {
                HtmlNode h3 = h3Elements[i];
                string sectionTitle = h3.InnerText.Trim();

                // Get content after this h3 until the next h3
                StringBuilder sectionContent = new StringBuilder();
                var currentNode = h3.NextSibling;

                while (currentNode != null && currentNode.Name != "h3")
                {
                    sectionContent.Append(currentNode.OuterHtml);
                    currentNode = currentNode.NextSibling;
                }

                // Create a new section with title and content
                sections.Add(new HtmlSection
                {
                    Title = sectionTitle,
                    Content = sectionContent.ToString()
                });
            }

            return sections; // Return all sections
        }

        private async void SaveAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure that the HTML content has been fetched and formatted into sections
            List<HtmlSection> sections = FormatHtmlIntoSections(StoredFormattedHtml);

            // Show the section selection dialog
            SectionSelectionDialog dialog = new SectionSelectionDialog(sections);
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                // Get the selected section titles from the dialog
                var selectedSectionTitles = dialog.Sections
                    .Where(s => s.IsSelected)
                    .Select(s => s.Title)
                    .ToList();

                // Get the current date and time
                string dateTimeStamp = DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
                string fileDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

                // Start building the final HTML document, adding the styling and HTML boilerplate
                var finalHtmlContent = new StringBuilder();
                finalHtmlContent.Append($@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>R5 Offline Report</title>
    <style>
        h1 {{ 
            color: blue; 
            text-align: center; /* Center the H1 elements */
        }}
        h3 {{ 
            color: black; 
            text-decoration: underline; /* Add underline to H3 elements */
        }}
        h4 {{ 
            color: red; 
            margin-left: 20px; /* Indent H4 elements */
        }}
        h5 {{
            color: black; 
            margin-left: 40px; /* Indent H5 elements */
        }}
    </style>
</head>
<body>");

                // Add the title "R5 Offline Report" with the current date and time
                finalHtmlContent.Append($"<h1>R5 Offline Report - {dateTimeStamp}</h1>");

                // Filter the original HtmlSections based on the selected section titles
                foreach (var section in sections.Where(s => selectedSectionTitles.Contains(s.Title)))
                {
                    finalHtmlContent.Append($"<h3>{section.Title}</h3>");
                    finalHtmlContent.Append(section.Content);
                }

                finalHtmlContent.Append("</body></html>");

                // Save the final HTML as a PDF
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                saveFileDialog.DefaultExt = "pdf";
                saveFileDialog.Title = "Save PDF File";
                saveFileDialog.FileName = $"R5 Offline Report - {fileDateTimeStamp}";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string outputPath = saveFileDialog.FileName;
                    StatusMessage.Text = "Saving...";
                    StatusMessage.Foreground = new SolidColorBrush(Colors.Orange);

                    try
                    {
                        PdfService pdfService = new PdfService();
                        await pdfService.ConvertHtmlToPdf(finalHtmlContent.ToString(), outputPath);
                        MessageBox.Show("PDF saved successfully at " + outputPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save PDF: {ex.Message}");
                    }
                    StatusMessage.Text = "Saved!";
                    StatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                }
            }
        }





        public class PdfService
        {
            public async Task ConvertHtmlToPdf(string htmlContent, string outputPath)
            {
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync(); // Automatically downloads the latest Chromium

                using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }))
                using (var page = await browser.NewPageAsync())
                {
                    // Set the content of the page to the HTML you want to convert
                    await page.SetContentAsync(htmlContent);

                    // Save the page as a PDF
                    await page.PdfAsync(outputPath, new PdfOptions
                    {
                        Format = PaperFormat.A4,
                        PrintBackground = true
                    });
                }
            }
        }

        public class HtmlSection
        {
            public string Title { get; set; } // The <h3> content (section title)
            public string Content { get; set; } // The section content (HTML after <h3> until next <h3>)
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static R5OfflineReports.MainWindow;

namespace R5OfflineReports
{
    public partial class SectionSelectionDialog : Window
    {
        public List<SectionViewModel> Sections { get; set; }

        public SectionSelectionDialog(List<HtmlSection> htmlSections)
        {
            InitializeComponent();
            Sections = htmlSections.Select(s => new SectionViewModel { Title = s.Title, IsSelected = true }).ToList();
            SectionsList.ItemsSource = Sections;
        }

        private void GeneratePdf_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Close the dialog and return to the main window
        }
    }

    public class SectionViewModel
    {
        public string Title { get; set; }
        public bool IsSelected { get; set; } // Whether the section is selected
    }
}

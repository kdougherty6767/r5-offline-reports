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
            // Initialize the sections with IsSelected set to false (unselected by default)
            Sections = htmlSections.Select(s => new SectionViewModel { Title = s.Title, IsSelected = false }).ToList();
            SectionsList.ItemsSource = Sections;
        }

        // Generate PDF button handler
        private void GeneratePdf_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Close the dialog and return to the main window
        }

        // Select All button handler
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in Sections)
            {
                section.IsSelected = true; // Select all sections
            }
            SectionsList.ItemsSource = null; // Refresh UI
            SectionsList.ItemsSource = Sections;
        }

        // Deselect All button handler
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in Sections)
            {
                section.IsSelected = false; // Deselect all sections
            }
            SectionsList.ItemsSource = null; // Refresh UI
            SectionsList.ItemsSource = Sections;
        }
    }

    public class SectionViewModel
    {
        public string Title { get; set; }
        public bool IsSelected { get; set; } // Whether the section is selected
    }
}

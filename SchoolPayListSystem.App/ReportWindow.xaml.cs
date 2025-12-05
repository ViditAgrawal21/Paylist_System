using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Core.DTOs;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;
using SchoolPayListSystem.Reports;

namespace SchoolPayListSystem.App
{
    public partial class ReportWindow : Window
    {
        private readonly ReportService _reportService;
        private readonly SchoolTypeService _schoolTypeService;
        private readonly BranchService _branchService;
        private readonly HtmlPdfGenerator _pdfGenerator;
        public string ReportType { get; set; } = "SchoolTypeSummary";

        // Store current report data for export
        private List<BranchReportDTO> _currentSchoolTypeSummaryReport;
        private BranchDetailReportDTO _currentBranchDetailReport;
        private List<SchoolTypeDetailReportDTO> _currentSchoolTypeDetailReport;
        private User _currentLoggedInUser;
        private DateTime _currentReportDate;
        private string _currentSchoolTypeName;
        private string _currentBranchName;

        public ReportWindow()
        {
            InitializeComponent();
            
            var context = new SchoolPayListDbContext();
            _reportService = new ReportService(context, new SalaryEntryRepository(context));
            _schoolTypeService = new SchoolTypeService(new SchoolTypeRepository(context));
            _branchService = new BranchService(new BranchRepository(context));
            _pdfGenerator = new HtmlPdfGenerator();
            
            ReportDatePicker.SelectedDate = DateTime.Now;
            
            Loaded += async (s, e) => await LoadFilterOptions();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize visibility after window is fully loaded
            ReportType_Changed(null, null);
        }

        private async System.Threading.Tasks.Task LoadFilterOptions()
        {
            try
            {
                if (ReportType == "SchoolTypeSummary")
                {
                    FilterLabel.Text = "Select School Type";
                    FilterLabel2.Visibility = Visibility.Collapsed;
                    FilterCombo2.Visibility = Visibility.Collapsed;
                    AdviceNumberTogglePanel.Visibility = Visibility.Visible;
                    
                    var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                    FilterCombo.ItemsSource = schoolTypes;
                    FilterCombo.DisplayMemberPath = "TypeName";
                    FilterCombo.SelectedValuePath = "SchoolTypeId";
                }
                else if (ReportType == "BranchDetail")
                {
                    FilterLabel.Text = "Select Branch";
                    FilterLabel2.Visibility = Visibility.Visible;
                    FilterCombo2.Visibility = Visibility.Visible;
                    AdviceNumberTogglePanel.Visibility = Visibility.Visible;
                    
                    // Load branches with "All Branches" option
                    var branches = await _branchService.GetAllBranchesAsync();
                    var allBranchesList = new List<Branch>
                    {
                        new Branch { BranchId = -1, BranchName = "All Branches" }
                    };
                    allBranchesList.AddRange(branches);
                    
                    FilterCombo.ItemsSource = allBranchesList;
                    FilterCombo.DisplayMemberPath = "BranchName";
                    FilterCombo.SelectedValuePath = "BranchId";
                    FilterCombo.SelectedIndex = 0; // Select "All Branches" by default
                    
                    // Load school types in second combo
                    var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                    FilterCombo2.ItemsSource = schoolTypes;
                    FilterCombo2.DisplayMemberPath = "TypeName";
                    FilterCombo2.SelectedValuePath = "SchoolTypeId";
                }
                else if (ReportType == "SchoolTypeDetail")
                {
                    FilterLabel.Text = "Select School Type";
                    FilterLabel2.Visibility = Visibility.Collapsed;
                    FilterCombo2.Visibility = Visibility.Collapsed;
                    AdviceNumberTogglePanel.Visibility = Visibility.Collapsed;
                    
                    var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                    FilterCombo.ItemsSource = schoolTypes;
                    FilterCombo.DisplayMemberPath = "TypeName";
                    FilterCombo.SelectedValuePath = "SchoolTypeId";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading filter options: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // School Type dropdown is always visible for BranchDetail reports
                // No need to hide it when "All Branches" is selected
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterCombo_SelectionChanged error: {ex.Message}");
            }
        }

        private void ReportType_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if controls are initialized
                if (ReportTypeDateRange == null || ReportTypeAllTime == null)
                    return;

                // Show/hide date pickers based on report type
                bool isDateRange = ReportTypeDateRange.IsChecked == true;
                bool isAllTime = ReportTypeAllTime.IsChecked == true;
                
                if (ReportDatePicker != null)
                    ReportDatePicker.Visibility = !isDateRange && !isAllTime ? Visibility.Visible : Visibility.Collapsed;
                
                if (ReportDateLabel != null)
                    ReportDateLabel.Visibility = !isDateRange && !isAllTime ? Visibility.Visible : Visibility.Collapsed;
                
                if (StartDatePicker != null)
                    StartDatePicker.Visibility = isDateRange ? Visibility.Visible : Visibility.Collapsed;
                
                if (StartDateLabel != null)
                    StartDateLabel.Visibility = isDateRange ? Visibility.Visible : Visibility.Collapsed;
                
                if (EndDatePicker != null)
                    EndDatePicker.Visibility = isDateRange ? Visibility.Visible : Visibility.Collapsed;
                
                if (EndDateLabel != null)
                    EndDateLabel.Visibility = isDateRange ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReportType_Changed error: {ex.Message}");
            }
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loggedInUser = ((App)Application.Current).LoggedInUser;
                if (loggedInUser == null)
                {
                    MessageBox.Show("No user logged in", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var filterId = FilterCombo.SelectedValue as int?;

                if (!filterId.HasValue)
                {
                    MessageBox.Show($"Please select a {FilterLabel.Text.ToLower()}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Determine report date based on report type
                DateTime reportDate = DateTime.Now;
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (ReportTypeToday.IsChecked == true)
                {
                    reportDate = ReportDatePicker.SelectedDate ?? DateTime.Now;
                }
                else if (ReportTypeDateRange.IsChecked == true)
                {
                    startDate = StartDatePicker.SelectedDate;
                    endDate = EndDatePicker.SelectedDate;
                    
                    if (!startDate.HasValue || !endDate.HasValue)
                    {
                        MessageBox.Show("Please select both start and end dates.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                // For All-Time, no date filtering needed

                if (ReportType == "SchoolTypeSummary")
                {
                    List<BranchReportDTO> report = null;

                    if (ReportTypeToday.IsChecked == true)
                    {
                        report = await _reportService.GenerateSchoolTypeSummaryReportAsync(filterId.Value, loggedInUser.UserId, reportDate);
                    }
                    else if (ReportTypeDateRange.IsChecked == true)
                    {
                        report = await _reportService.GenerateSchoolTypeSummaryReportByDateRangeAsync(filterId.Value, loggedInUser.UserId, startDate.Value, endDate.Value);
                    }
                    else if (ReportTypeAllTime.IsChecked == true)
                    {
                        report = await _reportService.GenerateSchoolTypeAllTimeReportAsync(filterId.Value, loggedInUser.UserId);
                    }
                    
                    if (report == null || report.Count == 0)
                    {
                        MessageBox.Show("No entries found for the selected criteria.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    _currentSchoolTypeSummaryReport = report;
                    _currentLoggedInUser = loggedInUser;
                    _currentReportDate = reportDate;
                    
                    // Get school type name
                    var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                    var selectedSchoolType = schoolTypes.FirstOrDefault(st => st.SchoolTypeId == filterId.Value);
                    _currentSchoolTypeName = selectedSchoolType?.TypeName ?? "Unknown";

                    DisplaySchoolTypeSummaryReport(report, loggedInUser, reportDate);
                }
                else if (ReportType == "BranchDetail")
                {
                    var branchId = FilterCombo.SelectedValue as int?;
                    var schoolTypeId = FilterCombo2.SelectedValue as int?;

                    if (!branchId.HasValue)
                    {
                        MessageBox.Show("Please select a branch.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // School Type is always required for Branch Detail reports
                    if (!schoolTypeId.HasValue)
                    {
                        MessageBox.Show("Please select both branch and school type.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    BranchDetailReportDTO report = null;

                    // If branchId is -1, it means "All Branches"
                    if (branchId.Value == -1)
                    {
                        // Generate report for all branches with the selected school type
                        var allBranches = await _branchService.GetAllBranchesAsync();
                        // Generate report for first branch with the selected school type
                        if (allBranches.Count > 0)
                        {
                            report = await _reportService.GenerateBranchWithSchoolTypeReportAsync(
                                allBranches[0].BranchId,
                                schoolTypeId.Value,  // Now always required
                                loggedInUser.UserId,
                                reportDate);
                        }
                    }
                    else
                    {
                        // Call method that generates report with branch + school type filter
                        report = await _reportService.GenerateBranchWithSchoolTypeReportAsync(
                            branchId.Value, 
                            schoolTypeId.Value, 
                            loggedInUser.UserId, 
                            reportDate);
                    }
                    
                    if (report == null || report.Entries.Count == 0)
                    {
                        MessageBox.Show("No entries found for the selected branch, school type and date.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    _currentBranchDetailReport = report;
                    _currentLoggedInUser = loggedInUser;
                    _currentReportDate = reportDate;

                    DisplayBranchDetailReport(report, loggedInUser);
                }
                else if (ReportType == "SchoolTypeDetail")
                {
                    var report = await _reportService.GenerateSchoolTypeDetailReportAsync(filterId.Value, loggedInUser.UserId, reportDate);
                    
                    if (report == null || report.Count == 0)
                    {
                        MessageBox.Show("No entries found for the selected school type and date.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    _currentSchoolTypeDetailReport = report;
                    _currentLoggedInUser = loggedInUser;
                    _currentReportDate = reportDate;

                    // Get school type name
                    var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                    var selectedSchoolType = schoolTypes.FirstOrDefault(st => st.SchoolTypeId == filterId.Value);
                    _currentSchoolTypeName = selectedSchoolType?.TypeName ?? "Unknown";

                    DisplaySchoolTypeDetailReport(report);
                }

                MessageBox.Show($"Report generated successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplaySchoolTypeSummaryReport(List<BranchReportDTO> report, User loggedInUser, DateTime reportDate)
        {
            var totalAmount = 0m;
            int totalEntries = 0;
            
            foreach (var branch in report)
            {
                totalEntries += branch.Entries.Count;
                totalAmount += branch.Entries.Sum(e => e.AMOUNT);
            }

            // Show status message
            string status = $"School Type Summary Report Generated\n\n" +
                          $"Total Entries: {totalEntries}\n" +
                          $"Total Amount: ₹{totalAmount:F2}\n\n" +
                          $"Click 'Print' to print or 'Export PDF/HTML' to save the report.";
            
            ReportStatusText.Text = status;

            TotalRecordsText.Text = totalEntries.ToString();
            TotalAmountText.Text = $"₹{totalAmount:F2}";
        }

        private void DisplayBranchDetailReport(BranchDetailReportDTO report, User loggedInUser)
        {
            int totalEntries = report.Entries.Count;
            decimal totalAmount = report.TotalAmount;

            // Show status message
            string status = $"Branch Detail Report Generated\n\n" +
                          $"Branch: {report.BranchName}\n" +
                          $"Total Entries: {totalEntries}\n" +
                          $"Total Amount: ₹{totalAmount:F2}\n\n" +
                          $"Click 'Print' to print or 'Export PDF/HTML' to save the report.";
            
            ReportStatusText.Text = status;
            
            // Hide the top-level advice number display
            AdviceNumberDisplay.Visibility = Visibility.Collapsed;

            TotalRecordsText.Text = totalEntries.ToString();
            TotalAmountText.Text = $"₹{totalAmount:F2}";
        }

        private void DisplaySchoolTypeDetailReport(List<SchoolTypeDetailReportDTO> report)
        {
            int totalEntries = report.Count;
            decimal totalAmount1 = 0m, totalAmount2 = 0m, totalAmount3 = 0m;

            foreach (var entry in report)
            {
                totalAmount1 += entry.Amount1;
                totalAmount2 += entry.Amount2;
                totalAmount3 += entry.Amount3;
            }

            decimal totalAmount = totalAmount1 + totalAmount2 + totalAmount3;

            // Show status message
            string status = $"School Type Detail Report Generated\n\n" +
                          $"Total Entries: {totalEntries}\n" +
                          $"Amount 1 Total: ₹{totalAmount1:F2}\n" +
                          $"Amount 2 Total: ₹{totalAmount2:F2}\n" +
                          $"Amount 3 Total: ₹{totalAmount3:F2}\n" +
                          $"Grand Total: ₹{totalAmount:F2}\n\n" +
                          $"Click 'Print' to print or 'Export PDF/HTML' to save the report.";
            
            ReportStatusText.Text = status;

            // Hide advice number display for this report type
            AdviceNumberDisplay.Visibility = Visibility.Collapsed;

            TotalRecordsText.Text = totalEntries.ToString();
            TotalAmountText.Text = $"₹{totalAmount:F2}";
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if any report data is loaded
                if ((_currentSchoolTypeSummaryReport == null || _currentSchoolTypeSummaryReport.Count == 0) &&
                    (_currentBranchDetailReport == null || _currentBranchDetailReport.Entries.Count == 0) &&
                    (_currentSchoolTypeDetailReport == null || _currentSchoolTypeDetailReport.Count == 0))
                {
                    MessageBox.Show("No data to print. Please generate a report first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Create a simple text to print
                    FlowDocument doc = new FlowDocument();
                    doc.Blocks.Add(new Paragraph(new Run(ReportStatusText.Text)));
                    
                    IDocumentPaginatorSource idocument = doc;
                    printDialog.PrintDocument(idocument.DocumentPaginator, "Report Print");
                    MessageBox.Show("Print job sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if any report data is loaded
                if ((_currentSchoolTypeSummaryReport == null || _currentSchoolTypeSummaryReport.Count == 0) &&
                    (_currentBranchDetailReport == null || _currentBranchDetailReport.Entries.Count == 0) &&
                    (_currentSchoolTypeDetailReport == null || _currentSchoolTypeDetailReport.Count == 0))
                {
                    MessageBox.Show("No data to export. Please generate a report first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html",
                    DefaultExt = ".html",
                    FileName = $"PayReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string htmlContent = "";

                    if (ReportType == "SchoolTypeSummary" && _currentSchoolTypeSummaryReport != null)
                    {
                        htmlContent = _pdfGenerator.GenerateSchoolTypeSummaryReportHtml(
                            _currentSchoolTypeSummaryReport,
                            _currentSchoolTypeName,
                            _currentReportDate,
                            _currentLoggedInUser.FullName);
                    }
                    else if (ReportType == "BranchDetail" && _currentBranchDetailReport != null)
                    {
                        htmlContent = _pdfGenerator.GenerateBranchDetailReportHtml(
                            _currentBranchDetailReport,
                            _currentLoggedInUser.FullName,
                            false,
                            1);
                    }
                    else if (ReportType == "SchoolTypeDetail" && _currentSchoolTypeDetailReport != null)
                    {
                        htmlContent = _pdfGenerator.GenerateSchoolTypeDetailReportHtml(
                            _currentSchoolTypeDetailReport,
                            _currentSchoolTypeName,
                            _currentReportDate,
                            _currentLoggedInUser.FullName,
                            1);
                    }

                    if (!string.IsNullOrEmpty(htmlContent))
                    {
                        File.WriteAllText(saveDialog.FileName, htmlContent, Encoding.UTF8);
                        MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Open the file in default browser
                        System.Diagnostics.Process.Start(saveDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

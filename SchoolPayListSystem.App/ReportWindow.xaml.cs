using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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

        private async System.Threading.Tasks.Task LoadFilterOptions()
        {
            try
            {
                if (ReportType == "SchoolTypeSummary")
                {
                    FilterLabel.Text = "Select School Type";
                    IncludeAdviceNumberCheckBox.Visibility = Visibility.Collapsed;
                    var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                    FilterCombo.ItemsSource = schoolTypes;
                    FilterCombo.DisplayMemberPath = "TypeName";
                    FilterCombo.SelectedValuePath = "SchoolTypeId";
                }
                else if (ReportType == "BranchDetail")
                {
                    FilterLabel.Text = "Select Branch";
                    IncludeAdviceNumberCheckBox.Visibility = Visibility.Visible;
                    var branches = await _branchService.GetAllBranchesAsync();
                    FilterCombo.ItemsSource = branches;
                    FilterCombo.DisplayMemberPath = "BranchName";
                    FilterCombo.SelectedValuePath = "BranchId";
                }
                else if (ReportType == "SchoolTypeDetail")
                {
                    FilterLabel.Text = "Select School Type";
                    IncludeAdviceNumberCheckBox.Visibility = Visibility.Collapsed;
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

                var reportDate = ReportDatePicker.SelectedDate ?? DateTime.Now;
                var filterId = FilterCombo.SelectedValue as int?;

                if (!filterId.HasValue)
                {
                    MessageBox.Show($"Please select a {FilterLabel.Text.ToLower()}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ReportType == "SchoolTypeSummary")
                {
                    var report = await _reportService.GenerateSchoolTypeSummaryReportAsync(filterId.Value, loggedInUser.UserId, reportDate);
                    
                    if (report == null || report.Count == 0)
                    {
                        MessageBox.Show("No entries found for the selected school type and date.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    var report = await _reportService.GenerateBranchSpecificReportAsync(filterId.Value, loggedInUser.UserId, reportDate);
                    
                    if (report == null || report.Entries.Count == 0)
                    {
                        MessageBox.Show("No entries found for the selected branch and date.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var reportData = new ObservableCollection<dynamic>();

            var totalAmount = 0m;
            foreach (var branch in report)
            {
                foreach (var entry in branch.Entries)
                {
                    dynamic row = new System.Dynamic.ExpandoObject();
                    row.BranchCode = branch.BranchCode;
                    row.BranchName = branch.BranchName;
                    row.Amount = entry.TotalAmount;
                    row.AdviceNumber = entry.AdviceNumber;
                    reportData.Add(row);
                    totalAmount += entry.TotalAmount;
                }
            }

            ReportDataGrid.ItemsSource = reportData;
            TotalRecordsText.Text = reportData.Count.ToString();
            TotalAmountText.Text = $"₹{totalAmount:F2}";
        }

        private void DisplayBranchDetailReport(BranchDetailReportDTO report, User loggedInUser)
        {
            var reportData = new ObservableCollection<dynamic>();

            foreach (var entry in report.Entries)
            {
                dynamic row = new System.Dynamic.ExpandoObject();
                row.SerialNo = entry.SerialNumber;
                row.SchoolCode = entry.SchoolCode;
                row.AccountNo = entry.AccountNumber;
                row.SchoolName = entry.SchoolName;
                row.Amount = entry.Amount;
                row.AdviceNumber = entry.AdviceNumber;
                reportData.Add(row);
            }

            ReportDataGrid.ItemsSource = reportData;
            TotalRecordsText.Text = reportData.Count.ToString();
            TotalAmountText.Text = $"₹{report.TotalAmount:F2}";
        }

        private void DisplaySchoolTypeDetailReport(List<SchoolTypeDetailReportDTO> report)
        {
            var reportData = new ObservableCollection<dynamic>();
            decimal totalAmount1 = 0m, totalAmount2 = 0m, totalAmount3 = 0m;

            foreach (var entry in report)
            {
                dynamic row = new System.Dynamic.ExpandoObject();
                row.SerialNo = entry.SerialNumber;
                row.SchoolCode = entry.SchoolCode;
                row.SchoolName = entry.SchoolName;
                row.Amount1 = entry.Amount1;
                row.Amount2 = entry.Amount2;
                row.Amount3 = entry.Amount3;
                row.Total = entry.TotalAmount;
                reportData.Add(row);

                totalAmount1 += entry.Amount1;
                totalAmount2 += entry.Amount2;
                totalAmount3 += entry.Amount3;
            }

            ReportDataGrid.ItemsSource = reportData;
            TotalRecordsText.Text = reportData.Count.ToString();
            TotalAmountText.Text = $"₹{(totalAmount1 + totalAmount2 + totalAmount3):F2}";
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ReportDataGrid.Items.Count == 0)
                {
                    MessageBox.Show("No data to print. Please generate a report first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(ReportDataGrid, "Report Print");
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
                if (ReportDataGrid.Items.Count == 0)
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
                        bool includeAdviceNumber = IncludeAdviceNumberCheckBox.IsChecked ?? true;
                        htmlContent = _pdfGenerator.GenerateBranchDetailReportHtml(
                            _currentBranchDetailReport,
                            _currentLoggedInUser.FullName,
                            includeAdviceNumber,
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

        private string GenerateHtmlReport()
        {
            var reportType = ReportType;
            var reportDate = ReportDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "N/A";
            
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'><style>");
            html.Append("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
            html.Append("h1 { color: #333; text-align: center; }");
            html.Append("table { border-collapse: collapse; width: 100%; margin-top: 20px; background-color: white; }");
            html.Append("th, td { border: 1px solid #ddd; padding: 10px; text-align: right; }");
            html.Append("td:first-child { text-align: left; }");
            html.Append("th { background-color: #9933CC; color: white; font-weight: bold; }");
            html.Append("tr:nth-child(even) { background-color: #f9f9f9; }");
            html.Append(".summary { margin-top: 20px; font-weight: bold; background-color: #e8f4f8; padding: 15px; border-left: 4px solid #9933CC; }");
            html.Append(".summary-row { display: flex; justify-content: space-between; margin: 5px 0; }");
            html.Append(".final-total { background-color: #fff3cd; padding: 15px; margin-top: 20px; font-size: 18px; border: 2px solid #9933CC; }");
            html.Append("</style></head><body>");

            html.Append($"<h1>Pay List Report - {reportType}</h1>");
            html.Append($"<p>Report Date: <strong>{reportDate}</strong></p>");

            html.Append("<table>");
            
            if (ReportType == "SchoolTypeSummary")
            {
                html.Append("<tr><th style='text-align: left;'>Branch Code</th><th>Branch Name</th><th>Amount</th><th>Advice Number</th></tr>");
            }
            else
            {
                html.Append("<tr><th>Sr.No</th><th style='text-align: left;'>School Code</th><th style='text-align: left;'>Account No</th><th style='text-align: left;'>School Name</th><th>Amount</th><th>Advice Number</th></tr>");
            }

            var totalAmount = 0m;

            try
            {
                foreach (var item in ReportDataGrid.Items)
                {
                    try
                    {
                        dynamic row = item;
                        
                        if (ReportType == "SchoolTypeSummary")
                        {
                            decimal amount = Convert.ToDecimal(row.Amount ?? 0);
                            totalAmount += amount;
                            
                            html.Append("<tr>");
                            html.Append($"<td>{row.BranchCode}</td>");
                            html.Append($"<td>{row.BranchName}</td>");
                            html.Append($"<td>₹{amount:F2}</td>");
                            html.Append($"<td>{row.AdviceNumber}</td>");
                            html.Append("</tr>");
                        }
                        else
                        {
                            decimal amount = Convert.ToDecimal(row.Amount ?? 0);
                            totalAmount += amount;
                            
                            html.Append("<tr>");
                            html.Append($"<td>{row.SerialNo}</td>");
                            html.Append($"<td>{row.SchoolCode}</td>");
                            html.Append($"<td>{row.AccountNo}</td>");
                            html.Append($"<td>{row.SchoolName}</td>");
                            html.Append($"<td>₹{amount:F2}</td>");
                            html.Append($"<td>{row.AdviceNumber}</td>");
                            html.Append("</tr>");
                        }
                    }
                    catch (Exception rowEx)
                    {
                        MessageBox.Show($"Error processing row: {rowEx.Message}", "Row Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Add totals row
            html.Append("<tr style='background-color: #e8f4f8; font-weight: bold;'>");
            if (ReportType == "SchoolTypeSummary")
            {
                html.Append("<td>TOTAL</td>");
                html.Append("<td></td>");
                html.Append($"<td>₹{totalAmount:F2}</td>");
                html.Append("<td></td>");
            }
            else
            {
                html.Append("<td colspan='4'>BRANCH TOTAL</td>");
                html.Append($"<td>₹{totalAmount:F2}</td>");
                html.Append("<td></td>");
            }
            html.Append("</tr>");
            html.Append("</table>");

            html.Append("<div class='summary'>");
            html.Append($"<p style='font-size: 16px; margin-bottom: 10px;'>Total Records: {ReportDataGrid.Items.Count}</p>");
            html.Append($"<div class='summary-row'><span>Total Amount:</span><strong>₹{totalAmount:F2}</strong></div>");
            html.Append("</div>");

            html.Append($"<div class='final-total'>Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</div>");
            html.Append("</body></html>");

            return html.ToString();
        }
    }
}

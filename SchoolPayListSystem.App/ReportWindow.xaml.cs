using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class ReportWindow : Window
    {
        private readonly SalaryService _salaryService;
        private readonly BranchService _branchService;
        private readonly SchoolService _schoolService;

        public ReportWindow()
        {
            InitializeComponent();
            
            var context = new SchoolPayListDbContext();
            _salaryService = new SalaryService(new SalaryEntryRepository(context));
            _branchService = new BranchService(new BranchRepository(context));
            _schoolService = new SchoolService(new SchoolRepository(context));
            
            ReportTypeCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            ToDatePicker.SelectedDate = DateTime.Now;
            
            Loaded += async (s, e) => await LoadFilterOptions();
            ReportTypeCombo.SelectionChanged += async (s, e) => await LoadFilterOptions();
        }

        private async System.Threading.Tasks.Task LoadFilterOptions()
        {
            try
            {
                var selectedType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Branch";
                
                if (selectedType == "Branch")
                {
                    FilterLabel.Text = "Filter By Branch";
                    var branches = await _branchService.GetAllBranchesAsync();
                    FilterCombo.ItemsSource = branches;
                    FilterCombo.DisplayMemberPath = "BranchName";
                    FilterCombo.SelectedValuePath = "BranchId";
                }
                else if (selectedType == "School")
                {
                    FilterLabel.Text = "Filter By School";
                    var schools = await _schoolService.GetAllSchoolsAsync();
                    FilterCombo.ItemsSource = schools;
                    FilterCombo.DisplayMemberPath = "SchoolName";
                    FilterCombo.SelectedValuePath = "SchoolId";
                }
                else if (selectedType == "Date")
                {
                    FilterLabel.Text = "No Filter";
                    FilterCombo.ItemsSource = null;
                    FilterCombo.IsEnabled = false;
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
                var reportType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Branch";
                var fromDate = FromDatePicker.SelectedDate ?? DateTime.Now.AddMonths(-1);
                var toDate = ToDatePicker.SelectedDate ?? DateTime.Now;

                // Ensure end date is inclusive of entire day
                toDate = toDate.Date.AddDays(1).AddSeconds(-1);

                ReportDataGrid.ItemsSource = null;
                var reportData = new ObservableCollection<dynamic>();

                var entries = await _salaryService.GetEntriesByDateRangeAsync(fromDate, toDate);

                if (entries.Count == 0)
                {
                    MessageBox.Show("No salary entries found for the selected date range.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    TotalRecordsText.Text = "0";
                    TotalAmountText.Text = "₹0.00";
                    return;
                }

                if (reportType == "Branch")
                {
                    var branchId = FilterCombo.SelectedValue as int?;
                    if (branchId.HasValue)
                    {
                        entries = entries.Where(x => x.BranchId == branchId.Value).ToList();
                    }

                    var groupedByBranch = entries.GroupBy(x => x.Branch?.BranchName ?? "Unknown");
                    foreach (var group in groupedByBranch)
                    {
                        foreach (var entry in group)
                        {
                            dynamic row = new System.Dynamic.ExpandoObject();
                            row.BranchName = group.Key;
                            row.EntryDate = entry.EntryDate;
                            row.Amount1 = entry.Amount1;
                            row.Amount2 = entry.Amount2;
                            row.Amount3 = entry.Amount3;
                            row.Total = entry.TotalAmount;
                            reportData.Add(row);
                        }
                    }
                }
                else if (reportType == "School")
                {
                    var schoolId = FilterCombo.SelectedValue as int?;
                    if (schoolId.HasValue)
                    {
                        entries = entries.Where(x => x.SchoolId == schoolId.Value).ToList();
                    }

                    var groupedBySchool = entries.GroupBy(x => x.School?.SchoolName ?? "Unknown");
                    foreach (var group in groupedBySchool)
                    {
                        foreach (var entry in group)
                        {
                            dynamic row = new System.Dynamic.ExpandoObject();
                            row.BranchName = group.Key;
                            row.EntryDate = entry.EntryDate;
                            row.Amount1 = entry.Amount1;
                            row.Amount2 = entry.Amount2;
                            row.Amount3 = entry.Amount3;
                            row.Total = entry.TotalAmount;
                            reportData.Add(row);
                        }
                    }
                }
                else if (reportType == "Date")
                {
                    var groupedByDate = entries.GroupBy(x => x.EntryDate.Date);
                    foreach (var group in groupedByDate.OrderByDescending(x => x.Key))
                    {
                        foreach (var entry in group)
                        {
                            dynamic row = new System.Dynamic.ExpandoObject();
                            row.BranchName = group.Key.ToString("dd/MM/yyyy");
                            row.EntryDate = entry.EntryDate;
                            row.Amount1 = entry.Amount1;
                            row.Amount2 = entry.Amount2;
                            row.Amount3 = entry.Amount3;
                            row.Total = entry.TotalAmount;
                            reportData.Add(row);
                        }
                    }
                }

                if (reportData.Count == 0)
                {
                    MessageBox.Show("No entries match the selected filter.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    TotalRecordsText.Text = "0";
                    TotalAmountText.Text = "₹0.00";
                    return;
                }

                ReportDataGrid.ItemsSource = reportData;
                TotalRecordsText.Text = reportData.Count.ToString();
                
                var totalAmount = reportData.Cast<dynamic>().Sum(x => (decimal)x.Total);
                TotalAmountText.Text = $"₹{totalAmount:F2}";
                
                MessageBox.Show($"Report generated successfully with {reportData.Count} records.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // Create a save dialog
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html|Text Files (*.txt)|*.txt",
                    DefaultExt = ".html",
                    FileName = $"Report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string content;
                    if (saveDialog.FileName.EndsWith(".txt"))
                    {
                        content = GenerateTextReport();
                    }
                    else
                    {
                        content = GenerateHtmlReport();
                    }

                    File.WriteAllText(saveDialog.FileName, content, Encoding.UTF8);
                    MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateHtmlReport()
        {
            var reportType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Branch";
            var fromDate = FromDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "N/A";
            var toDate = ToDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "N/A";
            
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

            html.Append($"<h1>Pay List Report - {reportType} Wise</h1>");
            html.Append($"<p>Report Type: <strong>{reportType}</strong></p>");
            html.Append($"<p>Date Range: <strong>{fromDate} to {toDate}</strong></p>");

            html.Append("<table>");
            html.Append("<tr><th style='text-align: left;'>Branch/School</th><th>Entry Date</th><th>Amount 1</th><th>Amount 2</th><th>Amount 3</th><th>Total</th></tr>");

            var totalRecords = 0;
            var totalAmount1 = 0m;
            var totalAmount2 = 0m;
            var totalAmount3 = 0m;
            var totalAmount = 0m;

            try
            {
                foreach (var item in ReportDataGrid.Items)
                {
                    try
                    {
                        dynamic row = item;
                        totalRecords++;
                        
                        decimal amount1 = 0m;
                        decimal amount2 = 0m;
                        decimal amount3 = 0m;
                        
                        try { amount1 = Convert.ToDecimal(row.Amount1 ?? 0); } catch { }
                        try { amount2 = Convert.ToDecimal(row.Amount2 ?? 0); } catch { }
                        try { amount3 = Convert.ToDecimal(row.Amount3 ?? 0); } catch { }
                        
                        decimal rowTotal = amount1 + amount2 + amount3;
                        totalAmount1 += amount1;
                        totalAmount2 += amount2;
                        totalAmount3 += amount3;
                        totalAmount += rowTotal;
                        
                        string branchName = row.BranchName?.ToString() ?? "";
                        DateTime entryDate = DateTime.Now;
                        try { entryDate = (DateTime)row.EntryDate; } catch { }
                        
                        html.Append("<tr>");
                        html.Append($"<td>{branchName}</td>");
                        html.Append($"<td>{entryDate:dd/MM/yyyy}</td>");
                        html.Append($"<td>₹{amount1:F2}</td>");
                        html.Append($"<td>₹{amount2:F2}</td>");
                        html.Append($"<td>₹{amount3:F2}</td>");
                        html.Append($"<td>₹{rowTotal:F2}</td>");
                        html.Append("</tr>");
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
            html.Append("<td>AMOUNT-WISE TOTAL</td>");
            html.Append("<td></td>");
            html.Append($"<td>₹{totalAmount1:F2}</td>");
            html.Append($"<td>₹{totalAmount2:F2}</td>");
            html.Append($"<td>₹{totalAmount3:F2}</td>");
            html.Append($"<td>₹{totalAmount:F2}</td>");
            html.Append("</tr>");
            html.Append("</table>");

            html.Append("<div class='summary'>");
            html.Append("<p style='font-size: 16px; margin-bottom: 10px;'>📊 SUMMARY</p>");
            html.Append($"<div class='summary-row'><span>Total Records:</span><strong>{totalRecords}</strong></div>");
            html.Append($"<div class='summary-row'><span>Amount 1 Total:</span><strong>₹{totalAmount1:F2}</strong></div>");
            html.Append($"<div class='summary-row'><span>Amount 2 Total:</span><strong>₹{totalAmount2:F2}</strong></div>");
            html.Append($"<div class='summary-row'><span>Amount 3 Total:</span><strong>₹{totalAmount3:F2}</strong></div>");
            html.Append("</div>");

            html.Append($"<div class='final-total'>🎯 FINAL TOTAL: ₹{totalAmount:F2}</div>");
            html.Append("</body></html>");

            return html.ToString();
        }

        private string GenerateTextReport()
        {
            var reportType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Branch";
            var fromDate = FromDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "N/A";
            var toDate = ToDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "N/A";

            var text = new StringBuilder();
            text.AppendLine("================================================================================");
            text.AppendLine("PAY LIST REPORT");
            text.AppendLine("================================================================================");
            text.AppendLine($"Report Type: {reportType}");
            text.AppendLine($"Date Range: {fromDate} to {toDate}");
            text.AppendLine($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            text.AppendLine("================================================================================");
            text.AppendLine(string.Format("{0,-30} {1,-15} {2,-15} {3,-15} {4,-15} {5,-15}",
                "Branch/School", "Entry Date", "Amount 1", "Amount 2", "Amount 3", "Total"));
            text.AppendLine("================================================================================");

            var totalRecords = 0;
            var totalAmount1 = 0m;
            var totalAmount2 = 0m;
            var totalAmount3 = 0m;
            var totalAmount = 0m;

            try
            {
                foreach (var item in ReportDataGrid.Items)
                {
                    try
                    {
                        dynamic row = item;
                        totalRecords++;

                        decimal amount1 = 0m;
                        decimal amount2 = 0m;
                        decimal amount3 = 0m;
                        
                        try { amount1 = Convert.ToDecimal(row.Amount1 ?? 0); } catch { }
                        try { amount2 = Convert.ToDecimal(row.Amount2 ?? 0); } catch { }
                        try { amount3 = Convert.ToDecimal(row.Amount3 ?? 0); } catch { }
                        
                        decimal rowTotal = amount1 + amount2 + amount3;
                        totalAmount1 += amount1;
                        totalAmount2 += amount2;
                        totalAmount3 += amount3;
                        totalAmount += rowTotal;

                        string branchName = row.BranchName?.ToString() ?? "";
                        DateTime entryDate = DateTime.Now;
                        try { entryDate = (DateTime)row.EntryDate; } catch { }

                        text.AppendLine(string.Format("{0,-30} {1,-15} {2,-15:F2} {3,-15:F2} {4,-15:F2} {5,-15:F2}",
                            branchName,
                            entryDate.ToString("dd/MM/yyyy"),
                            amount1,
                            amount2,
                            amount3,
                            rowTotal));
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

            text.AppendLine("================================================================================");
            text.AppendLine(string.Format("{0,-30} {1,-15} {2,-15:F2} {3,-15:F2} {4,-15:F2} {5,-15:F2}",
                "AMOUNT-WISE TOTAL", "", totalAmount1, totalAmount2, totalAmount3, totalAmount));
            text.AppendLine("================================================================================");
            text.AppendLine();
            text.AppendLine("📊 SUMMARY");
            text.AppendLine("================================================================================");
            text.AppendLine($"Total Records: {totalRecords}");
            text.AppendLine($"Amount 1 Total: ₹{totalAmount1:F2}");
            text.AppendLine($"Amount 2 Total: ₹{totalAmount2:F2}");
            text.AppendLine($"Amount 3 Total: ₹{totalAmount3:F2}");
            text.AppendLine("================================================================================");
            text.AppendLine($"🎯 FINAL TOTAL: ₹{totalAmount:F2}");
            text.AppendLine("================================================================================");

            return text.ToString();
        }
    }
}

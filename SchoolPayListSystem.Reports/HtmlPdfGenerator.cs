using System;
using System.Collections.Generic;
using System.Linq;
using SchoolPayListSystem.Core.DTOs;

namespace SchoolPayListSystem.Reports
{
    public class HtmlPdfGenerator
    {
        private const string BankName = "AMRAVATI DISTRICT CO.OP BANK LTD., H.O., AMRAVATI";

        /// <summary>
        /// Generate School Type Summary Report HTML
        /// Shows all branches with their totals for a specific school type
        /// </summary>
        public string GenerateSchoolTypeSummaryReportHtml(
            List<BranchReportDTO> branchReports, 
            string schoolTypeGroup, 
            DateTime reportDate, 
            string preparedBy,
            int pageNumber = 1)
        {
            try
            {
                var html = new System.Text.StringBuilder();

                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"utf-8\" />");
                html.AppendLine("<title>School Type Summary Report</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 11pt; margin: 10px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th, td { border: 1px solid #000; padding: 8px; text-align: left; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".header { text-align: center; font-weight: bold; margin-bottom: 10px; }");
                html.AppendLine(".subheader { text-align: center; font-size: 10pt; margin-bottom: 20px; }");
                html.AppendLine(".total-row { background-color: #e8e8e8; font-weight: bold; }");
                html.AppendLine(".grand-total { background-color: #cccccc; font-weight: bold; font-size: 12pt; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".footer { margin-top: 30px; font-size: 9pt; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header\">{BankName}</div>");
                html.AppendLine($"<div class=\"header\">BRANCHWISE SUMMARY FOR GROUP : {schoolTypeGroup.ToUpper()}</div>");
                html.AppendLine($"<div class=\"subheader\">Posting Date: {reportDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Page No. {pageNumber}</div>");

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 10%;\">BRANCH CODE</th>");
                html.AppendLine("<th style=\"width: 50%;\">BRANCH NAME</th>");
                html.AppendLine("<th style=\"width: 20%;\" class=\"amount-right\">AMOUNT</th>");
                html.AppendLine("<th style=\"width: 20%;\" class=\"amount-right\">ADVICE NO.</th>");
                html.AppendLine("</tr>");

                decimal grandTotal = 0;

                // Data Rows
                foreach (var branch in branchReports)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{branch.BranchCode}</td>");
                    html.AppendLine($"<td>{branch.BranchName}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{branch.TotalAmount:N2}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{branch.AdviceNumber ?? ""}</td>");
                    html.AppendLine("</tr>");

                    grandTotal += branch.TotalAmount;
                }

                // Grand Total Row
                html.AppendLine("<tr class=\"grand-total\">");
                html.AppendLine("<td colspan=\"2\" style=\"text-align: right;\">LIST GRAND TOTAL</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandTotal:N2}</td>");
                html.AppendLine("<td></td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<p>Prepared By: {preparedBy}</p>");
                html.AppendLine($"<p>Printed On : {DateTime.Now:dd/MM/yyyy}</p>");
                html.AppendLine("<p style=\"margin-top: 50px; border-top: 1px solid #000; padding-top: 10px;\">");
                html.AppendLine("<div style=\"float: left;\">Cashier/Clerk</div>");
                html.AppendLine("<div style=\"float: right;\">Officer/Manager</div>");
                html.AppendLine("</p>");
                html.AppendLine("</div>");

                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating school type summary report HTML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate Branch-Specific Report HTML
        /// Shows detailed entries for a particular branch
        /// </summary>
        public string GenerateBranchSpecificReportHtml(
            BranchDetailReportDTO branchReport,
            string preparedBy,
            int pageNumber = 1)
        {
            try
            {
                var html = new System.Text.StringBuilder();

                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"utf-8\" />");
                html.AppendLine("<title>Branch Specific Report</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 10pt; margin: 10px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th, td { border: 1px solid #000; padding: 6px; text-align: left; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".header { text-align: center; font-weight: bold; margin-bottom: 5px; }");
                html.AppendLine(".subheader { text-align: center; font-size: 9pt; margin-bottom: 15px; }");
                html.AppendLine(".total-row { background-color: #e8e8e8; font-weight: bold; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".footer { margin-top: 20px; font-size: 8pt; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header\">{BankName}</div>");
                html.AppendLine($"<div class=\"header\">TO BRANCH: {branchReport.BranchCode} {branchReport.BranchName}</div>");
                html.AppendLine($"<div class=\"subheader\">");
                html.AppendLine($"Posting Date: {branchReport.EntryDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Page No. {pageNumber}");
                html.AppendLine($"<br/>Please Note that your A/c has been CREDITED/DEBIT totay as below");
                html.AppendLine("</div>");

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 8%;\">Sr. No.</th>");
                html.AppendLine("<th style=\"width: 8%;\">SCH CODE</th>");
                html.AppendLine("<th style=\"width: 15%;\">ACCOUNT NO.</th>");
                html.AppendLine("<th style=\"width: 38%;\">SCHOOL/COLLEGE NAME</th>");
                html.AppendLine("<th style=\"width: 15%;\" class=\"amount-right\">ADVICE NO.</th>");
                html.AppendLine("<th style=\"width: 16%;\" class=\"amount-right\">AMOUNT</th>");
                html.AppendLine("</tr>");

                // Data Rows
                foreach (var entry in branchReport.Entries)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entry.SerialNumber}</td>");
                    html.AppendLine($"<td>{entry.SchoolCode}</td>");
                    html.AppendLine($"<td>{entry.AccountNumber}</td>");
                    html.AppendLine($"<td>{entry.SchoolName}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.AdviceNumber}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.Amount:N2}</td>");
                    html.AppendLine("</tr>");
                }

                // Total Row
                html.AppendLine("<tr class=\"total-row\">");
                html.AppendLine("<td colspan=\"4\" style=\"text-align: right;\">BRANCH TOTAL</td>");
                html.AppendLine($"<td colspan=\"2\" class=\"amount-right\">{branchReport.TotalAmount:N2}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<p>Prepared By: {preparedBy}</p>");
                html.AppendLine($"<p>Printed On : {DateTime.Now:dd/MM/yyyy}</p>");
                html.AppendLine("<p><strong>Note:</strong> This is Computer generated statement and required no SIGNATURE</p>");
                html.AppendLine("</div>");

                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating branch-specific report HTML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate Branch Detail Report HTML with Advice Number at top
        /// </summary>
        public string GenerateBranchDetailReportHtml(
            BranchDetailReportDTO branchReport,
            string preparedBy,
            bool includeAdviceNumber = true,
            int pageNumber = 1)
        {
            try
            {
                var html = new System.Text.StringBuilder();

                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"utf-8\" />");
                html.AppendLine("<title>Branch Detail Report</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 10pt; margin: 10px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th, td { border: 1px solid #000; padding: 6px; text-align: left; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".header { text-align: center; font-weight: bold; margin-bottom: 5px; }");
                html.AppendLine(".subheader { text-align: center; font-size: 9pt; margin-bottom: 15px; }");
                html.AppendLine(".advice-number { text-align: left; font-weight: bold; color: #9933CC; margin: 10px 0; font-size: 11pt; }");
                html.AppendLine(".total-row { background-color: #e8e8e8; font-weight: bold; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".footer { margin-top: 20px; font-size: 8pt; }");
                html.AppendLine(".signature-area { margin-top: 50px; display: flex; justify-content: space-between; border-top: 1px solid #000; padding-top: 20px; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header\">{BankName}</div>");
                html.AppendLine($"<div class=\"header\">TO BRANCH: {branchReport.BranchCode} {branchReport.BranchName}</div>");
                html.AppendLine($"<div class=\"subheader\">");
                
                html.AppendLine($"Posting Date: {branchReport.EntryDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Page No. {pageNumber}");
                html.AppendLine($"<br/>Please Note that your A/c has been CREDITED/DEBIT totay as below");
                html.AppendLine("</div>");

                // Display Advice Number at top
                if (!string.IsNullOrEmpty(branchReport.Entries.FirstOrDefault()?.AdviceNumber))
                {
                    html.AppendLine($"<div class=\"advice-number\">Advice Number: {branchReport.Entries.First().AdviceNumber}</div>");
                }

                // Table Header (without ADVICE NO column)
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 8%;\">Sr. No.</th>");
                html.AppendLine("<th style=\"width: 8%;\">SCH CODE</th>");
                html.AppendLine("<th style=\"width: 15%;\">ACCOUNT NO.</th>");
                html.AppendLine("<th style=\"width: 53%;\">SCHOOL/COLLEGE NAME</th>");
                html.AppendLine("<th style=\"width: 16%;\" class=\"amount-right\">AMOUNT</th>");
                html.AppendLine("</tr>");

                // Data Rows (without ADVICE NO column)
                foreach (var entry in branchReport.Entries)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entry.SerialNumber}</td>");
                    html.AppendLine($"<td>{entry.SchoolCode}</td>");
                    html.AppendLine($"<td>{entry.AccountNumber}</td>");
                    html.AppendLine($"<td>{entry.SchoolName}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.Amount:N2}</td>");
                    html.AppendLine("</tr>");
                }

                // Total Row
                html.AppendLine("<tr class=\"total-row\">");
                html.AppendLine("<td colspan=\"4\" style=\"text-align: right;\">BRANCH TOTAL</td>");
                html.AppendLine($"<td class=\"amount-right\">{branchReport.TotalAmount:N2}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<p>Prepared By: {preparedBy}</p>");
                html.AppendLine($"<p>Printed On : {DateTime.Now:dd/MM/yyyy}</p>");
                html.AppendLine("<p><strong>Note:</strong> This is Computer generated statement and required no SIGNATURE</p>");
                
                html.AppendLine("<div class=\"signature-area\">");
                html.AppendLine("<div>Cashier/Clerk</div>");
                html.AppendLine("<div>Officer/Manager</div>");
                html.AppendLine("</div>");
                
                html.AppendLine("</div>");

                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating branch detail report HTML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate School Type Detail Report HTML (List of all schools of a type)
        /// </summary>
        public string GenerateSchoolTypeDetailReportHtml(
            List<SchoolTypeDetailReportDTO> schoolEntries,
            string schoolTypeName,
            DateTime reportDate,
            string preparedBy,
            int pageNumber = 1)
        {
            try
            {
                var html = new System.Text.StringBuilder();

                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"utf-8\" />");
                html.AppendLine("<title>School Type Detail Report</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 11pt; margin: 10px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th, td { border: 1px solid #000; padding: 8px; text-align: left; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".header { text-align: center; font-weight: bold; margin-bottom: 10px; }");
                html.AppendLine(".subheader { text-align: center; font-size: 10pt; margin-bottom: 20px; }");
                html.AppendLine(".total-row { background-color: #e8e8e8; font-weight: bold; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".footer { margin-top: 30px; font-size: 9pt; }");
                html.AppendLine(".signature-area { margin-top: 50px; display: flex; justify-content: space-between; border-top: 1px solid #000; padding-top: 20px; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header\">{BankName}</div>");
                html.AppendLine($"<div class=\"header\">LIST OF SCHOOL/COLLEGE POSTED</div>");
                html.AppendLine($"<div class=\"header\">{schoolTypeName.ToUpper()}</div>");
                html.AppendLine($"<div class=\"subheader\">Posting Date: {reportDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Page No. {pageNumber}</div>");

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 8%;\">Sr. No.</th>");
                html.AppendLine("<th style=\"width: 12%;\">SCH CODE</th>");
                html.AppendLine("<th style=\"width: 50%;\">SCHOOL/COLLEGE NAME</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">AMOUNT 1</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">AMOUNT 2</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">AMOUNT 3</th>");
                html.AppendLine("<th style=\"width: 15%;\" class=\"amount-right\">TOTAL</th>");
                html.AppendLine("</tr>");

                decimal grandTotal = 0;
                decimal grandAmount1 = 0;
                decimal grandAmount2 = 0;
                decimal grandAmount3 = 0;

                // Data Rows
                foreach (var entry in schoolEntries)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entry.SerialNumber}</td>");
                    html.AppendLine($"<td>{entry.SchoolCode}</td>");
                    html.AppendLine($"<td>{entry.SchoolName}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.Amount1:N2}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.Amount2:N2}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.Amount3:N2}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.TotalAmount:N2}</td>");
                    html.AppendLine("</tr>");

                    grandTotal += entry.TotalAmount;
                    grandAmount1 += entry.Amount1;
                    grandAmount2 += entry.Amount2;
                    grandAmount3 += entry.Amount3;
                }

                // Grand Total Row
                html.AppendLine("<tr class=\"total-row\">");
                html.AppendLine("<td colspan=\"3\" style=\"text-align: right;\">LIST GRAND TOTAL</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandAmount1:N2}</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandAmount2:N2}</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandAmount3:N2}</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandTotal:N2}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<p>Prepared By: {preparedBy}</p>");
                html.AppendLine($"<p>Printed On : {DateTime.Now:dd/MM/yyyy}</p>");
                
                html.AppendLine("<div class=\"signature-area\">");
                html.AppendLine("<div>Cashier/Clerk</div>");
                html.AppendLine("<div>Officer/Manager</div>");
                html.AppendLine("</div>");
                
                html.AppendLine("</div>");

                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating school type detail report HTML: {ex.Message}", ex);
            }
        }

        public string GenerateBranchReport()
        {
            return "<html><body>Branch Report</body></html>";
        }
    }
}

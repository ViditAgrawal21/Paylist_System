using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SchoolPayListSystem.Core.DTOs;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

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
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 11pt; margin: 15px; line-height: 1.4; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
                html.AppendLine("table td, table th { padding: 8px; text-align: left; border-bottom: 1px solid #000; }");
                html.AppendLine("th { background-color: #f5f5f5; font-weight: bold; border-top: 1px solid #000; }");
                html.AppendLine(".header-main { text-align: center; font-weight: bold; font-size: 12pt; margin-bottom: 5px; }");
                html.AppendLine(".header-sub { text-align: center; font-weight: bold; font-size: 11pt; margin-bottom: 5px; }");
                html.AppendLine(".header-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; font-size: 10pt; }");
                html.AppendLine(".posting-date { text-align: right; font-size: 10pt; }");
                html.AppendLine(".total-row { font-weight: bold; background-color: #f5f5f5; }");
                html.AppendLine(".grand-total { font-weight: bold; background-color: #e8e8e8; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".code-col { text-align: left; }");
                html.AppendLine(".footer { margin-top: 40px; font-size: 9pt; }");
                html.AppendLine(".footer-row { display: flex; justify-content: space-between; margin-top: 30px; }");
                html.AppendLine(".signature-space { border-top: 1px solid #000; padding-top: 20px; text-align: center; font-size: 9pt; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header-main\">{BankName}</div>");
                html.AppendLine($"<div class=\"header-sub\">BRANCHWISE SUMMARY FOR GROUP: {schoolTypeGroup.ToUpper()}</div>");
                html.AppendLine($"<div class=\"header-info\"><span></span><span class=\"posting-date\">Posting Date: {reportDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Page No. {pageNumber}</span></div>");

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"code-col\">BRANCH CODE</th>");
                html.AppendLine("<th style=\"width: 45%;\">BRANCH NAME</th>");
                html.AppendLine("<th style=\"width: 20%;\" class=\"amount-right\">AMOUNT</th>");
                html.AppendLine("<th style=\"width: 25%;\" class=\"amount-right\">ADVICE NO.</th>");
                html.AppendLine("</tr>");

                decimal grandTotal = 0;

                // Data Rows
                foreach (var branch in branchReports)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class=\"code-col\">{branch.BranchCode}</td>");
                    html.AppendLine($"<td>{branch.BranchName}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{branch.TotalAmount:N2}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{(string.IsNullOrEmpty(branch.AdviceNumber) ? "" : branch.AdviceNumber)}</td>");
                    html.AppendLine("</tr>");

                    grandTotal += branch.TotalAmount;
                }

                // Grand Total Row
                html.AppendLine("<tr class=\"grand-total\">");
                html.AppendLine("<td colspan=\"2\" class=\"code-col\" style=\"text-align: right;\">LIST GRAND TOTAL</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandTotal:N2}</td>");
                html.AppendLine("<td class=\"amount-right\"></td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<p>Prepared By: {preparedBy} &nbsp;&nbsp;&nbsp;&nbsp; Printed On: {DateTime.Now:dd/MM/yyyy}</p>");
                html.AppendLine("<div class=\"footer-row\">");
                html.AppendLine("<div class=\"signature-space\">Cashier/Clerk</div>");
                html.AppendLine("<div class=\"signature-space\">Officer/Manager</div>");
                html.AppendLine("</div>");
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
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 10pt; margin: 15px; line-height: 1.4; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 15px 0; }");
                html.AppendLine("table td, table th { padding: 7px; text-align: left; border-bottom: 1px solid #000; }");
                html.AppendLine("th { background-color: #f5f5f5; font-weight: bold; border-top: 1px solid #000; }");
                html.AppendLine(".header-main { text-align: center; font-weight: bold; font-size: 12pt; margin-bottom: 3px; }");
                html.AppendLine(".header-branch { text-align: center; font-weight: bold; font-size: 11pt; margin-bottom: 5px; }");
                html.AppendLine(".header-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; font-size: 9pt; }");
                html.AppendLine(".posting-date { text-align: right; font-size: 9pt; }");
                html.AppendLine(".note-text { text-align: center; font-size: 9pt; margin-bottom: 15px; font-style: italic; }");
                html.AppendLine(".total-row { font-weight: bold; background-color: #f5f5f5; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".code-col { text-align: left; }");
                html.AppendLine(".footer { margin-top: 30px; font-size: 9pt; }");
                html.AppendLine(".footer-text { margin-bottom: 10px; }");
                html.AppendLine(".footer-row { display: flex; justify-content: space-between; margin-top: 30px; }");
                html.AppendLine(".signature-space { border-top: 1px solid #000; padding-top: 15px; text-align: center; font-size: 9pt; width: 35%; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header-main\">{BankName}</div>");
                html.AppendLine($"<div class=\"header-branch\">TO BRANCH: {branchReport.BranchCode} {branchReport.BranchName}</div>");
                html.AppendLine($"<div class=\"header-info\"><span></span><span class=\"posting-date\">Posting Date: {branchReport.EntryDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Advice No: {pageNumber}</span></div>");
                html.AppendLine($"<div class=\"note-text\">Please Note that your A/c has been CREDITED/DEBIT today as below</div>");

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 8%;\" class=\"code-col\">Sr. No.</th>");
                html.AppendLine("<th style=\"width: 8%;\" class=\"code-col\">SCH CODE</th>");
                html.AppendLine("<th style=\"width: 12%;\">ACCOUNT NO.</th>");
                html.AppendLine("<th style=\"width: 50%;\">SCHOOL/COLLEGE NAME</th>");
                html.AppendLine("<th style=\"width: 22%;\" class=\"amount-right\">AMOUNT</th>");
                html.AppendLine("</tr>");

                // Data Rows
                foreach (var entry in branchReport.Entries)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class=\"code-col\">{entry.SerialNumber}</td>");
                    html.AppendLine($"<td class=\"code-col\">{entry.SchoolCode}</td>");
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
                html.AppendLine($"<div class=\"footer-text\">Prepared By: {preparedBy} &nbsp;&nbsp;&nbsp;&nbsp; Printed On: {DateTime.Now:dd/MM/yyyy}</div>");
                html.AppendLine("<div class=\"footer-text\"><strong>Note:</strong> This is Computer generated statement and required no SIGNATURE</div>");
                html.AppendLine("<div class=\"footer-row\">");
                html.AppendLine("<div class=\"signature-space\">Cashier/Clerk</div>");
                html.AppendLine("<div class=\"signature-space\">Officer/Manager</div>");
                html.AppendLine("</div>");
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
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 10pt; margin: 15px; line-height: 1.4; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 15px 0; }");
                html.AppendLine("table td, table th { padding: 7px; text-align: left; border-bottom: 1px solid #000; }");
                html.AppendLine("th { background-color: #f5f5f5; font-weight: bold; border-top: 1px solid #000; }");
                html.AppendLine(".header-main { text-align: center; font-weight: bold; font-size: 12pt; margin-bottom: 3px; }");
                html.AppendLine(".header-branch { text-align: center; font-weight: bold; font-size: 11pt; margin-bottom: 5px; }");
                html.AppendLine(".header-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; font-size: 9pt; }");
                html.AppendLine(".posting-date { text-align: right; font-size: 9pt; }");
                html.AppendLine(".note-text { text-align: center; font-size: 9pt; margin-bottom: 10px; font-style: italic; }");
                html.AppendLine(".advice-number { text-align: left; font-weight: bold; margin-bottom: 10px; font-size: 10pt; }");
                html.AppendLine(".total-row { font-weight: bold; background-color: #f5f5f5; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".code-col { text-align: left; }");
                html.AppendLine(".footer { margin-top: 30px; font-size: 9pt; }");
                html.AppendLine(".footer-text { margin-bottom: 10px; }");
                html.AppendLine(".footer-row { display: flex; justify-content: space-between; margin-top: 30px; }");
                html.AppendLine(".signature-space { border-top: 1px solid #000; padding-top: 15px; text-align: center; font-size: 9pt; width: 35%; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header-main\">{BankName}</div>");
                html.AppendLine($"<div class=\"header-branch\">TO BRANCH: {branchReport.BranchCode} {branchReport.BranchName}</div>");
                html.AppendLine($"<div class=\"header-info\"><span></span><span class=\"posting-date\">Posting Date: {branchReport.EntryDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Advice No: {pageNumber}</span></div>");
                html.AppendLine($"<div class=\"note-text\">Please Note that your A/c has been CREDITED/DEBIT today as below</div>");

                // Display Advice Number at top
                if (!string.IsNullOrEmpty(branchReport.Entries.FirstOrDefault()?.AdviceNumber))
                {
                    html.AppendLine($"<div class=\"advice-number\">Advice Number: {branchReport.Entries.First().AdviceNumber}</div>");
                }

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 8%;\" class=\"code-col\">Sr. No.</th>");
                html.AppendLine("<th style=\"width: 8%;\" class=\"code-col\">SCH CODE</th>");
                html.AppendLine("<th style=\"width: 12%;\">ACCOUNT NO.</th>");
                html.AppendLine("<th style=\"width: 50%;\">SCHOOL/COLLEGE NAME</th>");
                html.AppendLine("<th style=\"width: 22%;\" class=\"amount-right\">AMOUNT</th>");
                html.AppendLine("</tr>");

                // Data Rows
                foreach (var entry in branchReport.Entries)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class=\"code-col\">{entry.SerialNumber}</td>");
                    html.AppendLine($"<td class=\"code-col\">{entry.SchoolCode}</td>");
                    html.AppendLine($"<td>{entry.AccountNumber}</td>");
                    html.AppendLine($"<td>{entry.SchoolName}</td>");
                    html.AppendLine($"<td class=\"amount-right\">{entry.Amount:N2}</td>");
                    html.AppendLine("</tr>");
                }

                // Total Row
                html.AppendLine("<tr class=\"total-row\">");
                html.AppendLine("<td colspan=\"4\" class=\"code-col\" style=\"text-align: right;\">BRANCH TOTAL</td>");
                html.AppendLine($"<td class=\"amount-right\">{branchReport.TotalAmount:N2}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<div class=\"footer-text\">Prepared By: {preparedBy} &nbsp;&nbsp;&nbsp;&nbsp; Printed On: {DateTime.Now:dd/MM/yyyy}</div>");
                html.AppendLine("<div class=\"footer-text\"><strong>Note:</strong> This is Computer generated statement and required no SIGNATURE</div>");
                html.AppendLine("<div class=\"footer-row\">");
                html.AppendLine("<div class=\"signature-space\">Cashier/Clerk</div>");
                html.AppendLine("<div class=\"signature-space\">Officer/Manager</div>");
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
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 11pt; margin: 15px; line-height: 1.4; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
                html.AppendLine("table td, table th { padding: 8px; text-align: left; border-bottom: 1px solid #000; }");
                html.AppendLine("th { background-color: #f5f5f5; font-weight: bold; border-top: 1px solid #000; }");
                html.AppendLine(".header-main { text-align: center; font-weight: bold; font-size: 12pt; margin-bottom: 3px; }");
                html.AppendLine(".header-sub { text-align: center; font-weight: bold; font-size: 11pt; margin-bottom: 2px; }");
                html.AppendLine(".header-info { text-align: center; font-size: 10pt; margin-bottom: 20px; }");
                html.AppendLine(".total-row { font-weight: bold; background-color: #f5f5f5; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".code-col { text-align: left; }");
                html.AppendLine(".footer { margin-top: 40px; font-size: 9pt; }");
                html.AppendLine(".footer-text { margin-bottom: 10px; }");
                html.AppendLine(".footer-row { display: flex; justify-content: space-between; margin-top: 30px; }");
                html.AppendLine(".signature-space { border-top: 1px solid #000; padding-top: 15px; text-align: center; font-size: 9pt; width: 35%; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header-main\">{BankName}</div>");
                html.AppendLine($"<div class=\"header-sub\">LIST OF SCHOOL/COLLEGE POSTED</div>");
                html.AppendLine($"<div class=\"header-sub\">{schoolTypeName.ToUpper()}</div>");
                html.AppendLine($"<div class=\"header-info\">Posting Date: {reportDate:dd/MM/yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Page No. {pageNumber}</div>");

                // Table Header
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style=\"width: 8%;\" class=\"code-col\">Sr. No.</th>");
                html.AppendLine("<th style=\"width: 12%;\" class=\"code-col\">SCH CODE</th>");
                html.AppendLine("<th style=\"width: 50%;\">SCHOOL/COLLEGE NAME</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">AMOUNT 1</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">AMOUNT 2</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">AMOUNT 3</th>");
                html.AppendLine("<th style=\"width: 10%;\" class=\"amount-right\">TOTAL</th>");
                html.AppendLine("</tr>");

                decimal grandTotal = 0;
                decimal grandAmount1 = 0;
                decimal grandAmount2 = 0;
                decimal grandAmount3 = 0;

                // Data Rows
                foreach (var entry in schoolEntries)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class=\"code-col\">{entry.SerialNumber}</td>");
                    html.AppendLine($"<td class=\"code-col\">{entry.SchoolCode}</td>");
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
                html.AppendLine("<td colspan=\"3\" class=\"code-col\" style=\"text-align: right;\">LIST GRAND TOTAL</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandAmount1:N2}</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandAmount2:N2}</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandAmount3:N2}</td>");
                html.AppendLine($"<td class=\"amount-right\">{grandTotal:N2}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</table>");

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<div class=\"footer-text\">Prepared By: {preparedBy} &nbsp;&nbsp;&nbsp;&nbsp; Printed On: {DateTime.Now:dd/MM/yyyy}</div>");
                html.AppendLine("<div class=\"footer-row\">");
                html.AppendLine("<div class=\"signature-space\">Cashier/Clerk</div>");
                html.AppendLine("<div class=\"signature-space\">Officer/Manager</div>");
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

        public string GenerateAllBranchesReportHtml(
            List<AllBranchesReportDTO> reports,
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
                html.AppendLine("<title>All Branches Report</title>");
                html.AppendLine("<style>");
                html.AppendLine("@page { margin: 0.5in; size: A4; }");
                html.AppendLine("@media print { body { margin: 0; padding: 0; } }");
                html.AppendLine("body { font-family: Arial, sans-serif; font-size: 11pt; margin: 15px; line-height: 1.4; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 10px 0; }");
                html.AppendLine("table td, table th { padding: 8px; text-align: left; border-bottom: 1px solid #000; }");
                html.AppendLine("th { background-color: #f5f5f5; font-weight: bold; border-top: 1px solid #000; }");
                html.AppendLine(".header { text-align: center; font-weight: bold; margin-bottom: 5px; font-size: 14pt; }");
                html.AppendLine(".schooltype-header { text-align: center; font-weight: bold; font-size: 12pt; padding: 10px 0; margin: 20px 0 15px 0; }");
                html.AppendLine(".page-number { text-align: right; font-size: 9pt; margin-bottom: 10px; }");
                html.AppendLine(".branch-section { margin: 15px 0; page-break-inside: avoid; }");
                html.AppendLine(".branch-heading { font-weight: bold; font-size: 11pt; padding: 5px 0; margin-bottom: 8px; border-bottom: 1px solid #000; }");
                html.AppendLine(".advice-number { font-weight: bold; margin-bottom: 8px; font-size: 10pt; }");
                html.AppendLine(".header-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; font-size: 10pt; }");
                html.AppendLine(".posting-date { text-align: right; font-size: 10pt; }");
                html.AppendLine(".total-row { background-color: #f5f5f5; font-weight: bold; }");
                html.AppendLine(".schooltype-total { font-weight: bold; padding: 8px 0; margin: 15px 0; text-align: right; font-size: 11pt; }");
                html.AppendLine(".amount-right { text-align: right; }");
                html.AppendLine(".footer { margin-top: 40px; font-size: 9pt; }");
                html.AppendLine(".section-divider { margin: 30px 0; }");
                html.AppendLine("</style>");;
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Header
                html.AppendLine($"<div class=\"header\">{BankName}</div>");
                html.AppendLine($"<div class=\"header\">ALL BRANCHES REPORT</div>");
                html.AppendLine($"<div class=\"header-row\"><span></span><span class=\"posting-date\">Posting Date: {reportDate:dd/MM/yyyy}</span></div>");

                // Process each school type
                bool isFirstSchoolType = true;
                int pageNum = 1;
                
                foreach (var schoolTypeReport in reports)
                {
                    // School Type Heading - Centered at Top on new page
                    if (!isFirstSchoolType)
                        html.AppendLine("<div class=\"section-divider\"></div>");
                    
                    html.AppendLine($"<div class=\"page-number\">Page No. {pageNum}</div>");
                    html.AppendLine($"<div class=\"schooltype-header\">{schoolTypeReport.SchoolTypeCode} - {schoolTypeReport.SchoolTypeName.ToUpper()}</div>");
                    
                    isFirstSchoolType = false;
                    pageNum++;

                    // Process each branch within this school type
                    int branchCount = 0;
                    foreach (var branchSection in schoolTypeReport.BranchSections)
                    {
                        // Add page break before each branch (except first)
                        if (branchCount > 0)
                        {
                            html.AppendLine("<div class=\"section-divider\"></div>");
                            html.AppendLine($"<div class=\"page-number\">Page No. {pageNum}</div>");
                            html.AppendLine($"<div class=\"schooltype-header\">{schoolTypeReport.SchoolTypeCode} - {schoolTypeReport.SchoolTypeName.ToUpper()}</div>");
                            pageNum++;
                        }

                        html.AppendLine("<div class=\"branch-section\">");

                        // Branch Heading
                        html.AppendLine($"<div class=\"branch-heading\">Branch: {branchSection.BranchCode} - {branchSection.BranchName}</div>");
                        
                        // Advice Number (above table)
                        if (!string.IsNullOrEmpty(branchSection.AdviceNumber))
                            html.AppendLine($"<div class=\"advice-number\">Advice No: {branchSection.AdviceNumber}</div>");

                        // Table Header
                        html.AppendLine("<table>");
                        html.AppendLine("<tr>");
                        html.AppendLine("<th style=\"width: 10%;\">SCHOOL CODE</th>");
                        html.AppendLine("<th style=\"width: 40%;\">SCHOOL NAME</th>");
                        html.AppendLine("<th style=\"width: 25%;\">ACCOUNT NO</th>");
                        html.AppendLine("<th style=\"width: 25%;\" class=\"amount-right\">AMOUNT</th>");
                        html.AppendLine("</tr>");

                        // Entries for this branch
                        foreach (var entry in branchSection.Entries)
                        {
                            html.AppendLine("<tr>");
                            html.AppendLine($"<td>{entry.SchoolCode}</td>");
                            html.AppendLine($"<td>{entry.SchoolName}</td>");
                            html.AppendLine($"<td>{entry.BankAccount}</td>");
                            html.AppendLine($"<td class=\"amount-right\">{entry.AMOUNT:N2}</td>");
                            html.AppendLine("</tr>");
                        }

                        // Branch Total Row
                        html.AppendLine("<tr class=\"total-row\">");
                        html.AppendLine($"<td colspan=\"3\" style=\"text-align: right;\">BRANCH TOTAL</td>");
                        html.AppendLine($"<td class=\"amount-right\">{branchSection.BranchTotal:N2}</td>");
                        html.AppendLine("</tr>");

                        html.AppendLine("</table>");
                        html.AppendLine("</div>");
                        
                        branchCount++;
                    }

                    // School Type Total (with page break after)
                    html.AppendLine($"<div class=\"schooltype-total\">");
                    html.AppendLine($"School Type Total: ₹{schoolTypeReport.SchoolTypeTotal:N2}");
                    html.AppendLine($"</div>");
                }

                // Footer
                html.AppendLine("<div class=\"footer\">");
                html.AppendLine($"<p>Prepared By: {preparedBy}</p>");
                html.AppendLine($"<p>Printed On : {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
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
                throw new Exception($"Error generating all branches report HTML: {ex.Message}", ex);
            }
        }

        public string GenerateBranchReport()
        {
            return "<html><body>Branch Report</body></html>";
        }

        /// <summary>
        /// Save HTML as a file (can be opened in browser and printed to PDF)
        /// </summary>
        public void SaveHtmlReport(string htmlContent, string outputPath)
        {
            try
            {
                // Ensure output directory exists
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Change .pdf to .html extension
                string htmlPath = outputPath.Replace(".pdf", ".html");
                
                // Write HTML file
                File.WriteAllText(htmlPath, htmlContent);
                
                System.Diagnostics.Debug.WriteLine($"HTML report saved to: {htmlPath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving HTML report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get HTML content as bytes
        /// <summary>
        /// Generate All Branches Report as PDF with proper page breaks
        /// </summary>
        public byte[] GenerateAllBranchesReportPdf(
            List<AllBranchesReportDTO> reports,
            DateTime reportDate,
            string preparedBy)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    PdfDocument pdf = new PdfDocument(new PdfWriter(memoryStream));
                    Document document = new Document(pdf);
                    document.SetMargins(30, 30, 30, 30);

                    int pageNum = 1;
                    bool isFirstBranch = true;

                    // Process each school type
                    foreach (var schoolTypeReport in reports)
                    {
                        // Process each branch
                        foreach (var branchSection in schoolTypeReport.BranchSections)
                        {
                            // Add page break before each branch (except first)
                            if (!isFirstBranch)
                            {
                                document.Add(new AreaBreak(iText.Layout.Properties.AreaBreakType.NEXT_PAGE));
                            }

                            // ===== HEADER FOR EACH PAGE =====
                            Paragraph bankName = new Paragraph(BankName)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                .SetFontSize(12)
                                .SetBold();
                            document.Add(bankName);

                            Paragraph reportTitle = new Paragraph("ALL BRANCHES REPORT")
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                .SetFontSize(11)
                                .SetBold();
                            document.Add(reportTitle);

                            Paragraph reportDatePara = new Paragraph($"Posting Date: {reportDate:dd/MM/yyyy}")
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                                .SetFontSize(10);
                            document.Add(reportDatePara);

                            // School Type Header with Page Number
                            Table headerTable = new Table(2);
                            headerTable.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));
                            Cell schoolTypeCell = new Cell().Add(new Paragraph($"{schoolTypeReport.SchoolTypeCode} - {schoolTypeReport.SchoolTypeName.ToUpper()}")
                                .SetBold()
                                .SetFontSize(11));
                            Cell pageNumCell = new Cell().Add(new Paragraph($"Page No. {pageNum}")
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                                .SetFontSize(10));
                            headerTable.AddCell(schoolTypeCell);
                            headerTable.AddCell(pageNumCell);
                            document.Add(headerTable);

                            document.Add(new Paragraph("\n"));

                            // Advice Number
                            if (!string.IsNullOrEmpty(branchSection.AdviceNumber))
                            {
                                Paragraph adviceNo = new Paragraph($"Advice No: {branchSection.AdviceNumber}")
                                    .SetBold()
                                    .SetFontSize(10);
                                document.Add(adviceNo);
                            }

                            // Create table
                            Table table = new Table(4);
                            table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                            // Header row
                            AddTableHeaderCell(table, "SCHOOL CODE");
                            AddTableHeaderCell(table, "SCHOOL NAME");
                            AddTableHeaderCell(table, "ACCOUNT NO");
                            AddTableHeaderCell(table, "AMOUNT");

                            // Data rows
                            foreach (var entry in branchSection.Entries)
                            {
                                AddTableCell(table, entry.SchoolCode);
                                AddTableCell(table, entry.SchoolName);
                                AddTableCell(table, entry.BankAccount);
                                AddTableCell(table, $"₹ {entry.AMOUNT:N2}");
                            }

                            // Branch Total Row
                            AddTableHeaderCell(table, "");
                            AddTableHeaderCell(table, "");
                            AddTableHeaderCell(table, "BRANCH TOTAL");
                            AddTableHeaderCell(table, $"₹ {branchSection.BranchTotal:N2}");

                            document.Add(table);

                            document.Add(new Paragraph("\n"));

                            // ===== FOOTER FOR EACH PAGE =====
                            Paragraph footer = new Paragraph($"Prepared By: {preparedBy}")
                                .SetFontSize(9);
                            document.Add(footer);

                            Paragraph printDate = new Paragraph($"Printed On : {DateTime.Now:dd/MM/yyyy}")
                                .SetFontSize(9);
                            document.Add(printDate);

                            // Signature area
                            document.Add(new Paragraph("\n"));
                            Table signatureTable = new Table(2);
                            signatureTable.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));
                            Cell cashierCell = new Cell().Add(new Paragraph("Cashier/Clerk").SetFontSize(9));
                            Cell officerCell = new Cell().Add(new Paragraph("Officer/Manager").SetFontSize(9));
                            signatureTable.AddCell(cashierCell);
                            signatureTable.AddCell(officerCell);
                            document.Add(signatureTable);

                            isFirstBranch = false;
                            pageNum++;
                        }
                    }

                    document.Close();
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating All Branches Report PDF: {ex.Message}", ex);
            }
        }

        private void AddTableHeaderCell(Table table, string content)
        {
            Cell cell = new Cell();
            cell.SetBackgroundColor(new iText.Kernel.Colors.DeviceGray(0.85f));
            cell.Add(new Paragraph(content).SetBold().SetFontSize(10));
            cell.SetPadding(6);
            table.AddCell(cell);
        }

        private void AddTableCell(Table table, string content)
        {
            Cell cell = new Cell();
            cell.Add(new Paragraph(content).SetFontSize(10));
            cell.SetPadding(6);
            table.AddCell(cell);
        }

        /// <summary>
        /// Generate School Type Summary Report as PDF
        /// </summary>
        public byte[] GenerateSchoolTypeSummaryReportPdf(
            List<BranchReportDTO> branchReports,
            string schoolTypeGroup,
            DateTime reportDate,
            string preparedBy)
        {
            try
            {
                var memoryStream = new System.IO.MemoryStream();
                var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);

                // Header
                document.Add(new Paragraph(BankName).SetFontSize(12).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"BRANCHWISE SUMMARY FOR GROUP : {schoolTypeGroup}").SetFontSize(11).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Posting Date: {reportDate:dd/MM/yyyy}").SetFontSize(10).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
                document.Add(new Paragraph("Page No. 1").SetFontSize(10).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
                document.Add(new Paragraph("\n"));

                // Create table
                float[] columnWidths = { 1.5f, 3f, 2f, 1.5f };
                Table table = new Table(columnWidths);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Header row
                AddTableHeaderCell(table, "BRANCH CODE");
                AddTableHeaderCell(table, "BRANCH NAME");
                AddTableHeaderCell(table, "AMOUNT");
                AddTableHeaderCell(table, "ADVICE NO.");

                // Data rows
                decimal grandTotal = 0;
                foreach (var branch in branchReports)
                {
                    AddTableCell(table, branch.BranchCode.ToString());
                    AddTableCell(table, branch.BranchName);
                    AddTableCell(table, branch.TotalAmount.ToString("N2"));
                    AddTableCell(table, branch.AdviceNumber ?? "");
                    grandTotal += branch.TotalAmount;
                }

                // Grand Total Row
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "LIST GRAND TOTAL");
                AddTableHeaderCell(table, grandTotal.ToString("N2"));
                AddTableHeaderCell(table, "");

                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Footer
                document.Add(new Paragraph($"Prepared By: {preparedBy}").SetFontSize(9));
                document.Add(new Paragraph($"Printed On : {DateTime.Now:dd/MM/yyyy}").SetFontSize(9));
                
                // Signature area
                document.Add(new Paragraph("\n"));
                Table signatureTable = new Table(2);
                signatureTable.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));
                Cell cashierCell = new Cell().Add(new Paragraph("Cashier/Clerk").SetFontSize(9));
                Cell officerCell = new Cell().Add(new Paragraph("Officer/Manager").SetFontSize(9));
                signatureTable.AddCell(cashierCell);
                signatureTable.AddCell(officerCell);
                document.Add(signatureTable);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating School Type Summary Report PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate Branch Detail Report as PDF
        /// </summary>
        public byte[] GenerateBranchDetailReportPdf(
            BranchDetailReportDTO branchReport,
            string branchName,
            DateTime reportDate,
            string preparedBy)
        {
            try
            {
                var memoryStream = new System.IO.MemoryStream();
                var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);

                // Header
                document.Add(new Paragraph(BankName).SetFontSize(12).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Branch Detail Report - {branchName}").SetFontSize(11).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Posting Date: {reportDate:dd/MM/yyyy}").SetFontSize(10).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
                document.Add(new Paragraph("\n"));

                // Create table
                float[] columnWidths = { 2, 3, 2, 2 };
                Table table = new Table(columnWidths);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Header row
                AddTableHeaderCell(table, "SCHOOL CODE");
                AddTableHeaderCell(table, "SCHOOL NAME");
                AddTableHeaderCell(table, "ACCOUNT NO");
                AddTableHeaderCell(table, "AMOUNT");

                // Data rows
                foreach (var entry in branchReport.Entries)
                {
                    AddTableCell(table, entry.SchoolCode);
                    AddTableCell(table, entry.SchoolName);
                    AddTableCell(table, entry.AccountNumber);
                    AddTableCell(table, entry.Amount.ToString("N2"));
                }

                // Total row
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "TOTAL");
                AddTableHeaderCell(table, branchReport.TotalAmount.ToString("N2"));

                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Footer
                document.Add(new Paragraph($"Prepared By: {preparedBy}").SetFontSize(9));
                document.Add(new Paragraph($"Printed On: {DateTime.Now:dd-MM-yyyy HH:mm:ss}").SetFontSize(9));

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating Branch Detail Report PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate School Type Detail Report as PDF
        /// </summary>
        public byte[] GenerateSchoolTypeDetailReportPdf(
            List<SchoolTypeDetailReportDTO> schoolTypeDetails,
            string schoolTypeName,
            DateTime reportDate,
            string preparedBy)
        {
            try
            {
                var memoryStream = new System.IO.MemoryStream();
                var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);

                // Header
                document.Add(new Paragraph(BankName).SetFontSize(12).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"School Type Detail Report - {schoolTypeName}").SetFontSize(11).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Posting Date: {reportDate:dd/MM/yyyy}").SetFontSize(10).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
                document.Add(new Paragraph("\n"));

                // Create table
                float[] columnWidths = { 1, 2, 3, 1, 1, 1, 1 };
                Table table = new Table(columnWidths);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Header row
                AddTableHeaderCell(table, "Sr. No.");
                AddTableHeaderCell(table, "SCH CODE");
                AddTableHeaderCell(table, "SCHOOL NAME");
                AddTableHeaderCell(table, "AMT 1");
                AddTableHeaderCell(table, "AMT 2");
                AddTableHeaderCell(table, "AMT 3");
                AddTableHeaderCell(table, "TOTAL");

                // Data rows
                decimal totalAmount1 = 0, totalAmount2 = 0, totalAmount3 = 0, grandTotal = 0;
                foreach (var detail in schoolTypeDetails)
                {
                    AddTableCell(table, detail.SerialNumber.ToString());
                    AddTableCell(table, detail.SchoolCode);
                    AddTableCell(table, detail.SchoolName);
                    AddTableCell(table, detail.Amount1.ToString("N2"));
                    AddTableCell(table, detail.Amount2.ToString("N2"));
                    AddTableCell(table, detail.Amount3.ToString("N2"));
                    AddTableCell(table, detail.TotalAmount.ToString("N2"));
                    totalAmount1 += detail.Amount1;
                    totalAmount2 += detail.Amount2;
                    totalAmount3 += detail.Amount3;
                    grandTotal += detail.TotalAmount;
                }

                // Total row
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "TOTAL");
                AddTableHeaderCell(table, totalAmount1.ToString("N2"));
                AddTableHeaderCell(table, totalAmount2.ToString("N2"));
                AddTableHeaderCell(table, totalAmount3.ToString("N2"));
                AddTableHeaderCell(table, grandTotal.ToString("N2"));

                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Footer
                document.Add(new Paragraph($"Prepared By: {preparedBy}").SetFontSize(9));
                document.Add(new Paragraph($"Printed On: {DateTime.Now:dd-MM-yyyy HH:mm:ss}").SetFontSize(9));

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating School Type Detail Report PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate Branch Specific Report as PDF
        /// </summary>
        public byte[] GenerateBranchSpecificReportPdf(
            BranchDetailReportDTO branchReport,
            string preparedBy,
            int pageNumber = 1)
        {
            try
            {
                var memoryStream = new System.IO.MemoryStream();
                var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);

                // Header
                document.Add(new Paragraph(BankName).SetFontSize(12).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"TO BRANCH: {branchReport.BranchCode} {branchReport.BranchName}").SetFontSize(11).SetBold().SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Posting Date: {branchReport.EntryDate:dd/MM/yyyy} - Page No. {pageNumber}").SetFontSize(10).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
                
                // Display Advice Number at top left
                if (branchReport.Entries.Count > 0 && !string.IsNullOrEmpty(branchReport.Entries.First().AdviceNumber))
                {
                    document.Add(new Paragraph($"Advice Number: {branchReport.Entries.First().AdviceNumber}").SetFontSize(10).SetBold());
                }
                
                document.Add(new Paragraph("Please Note that your A/c has been CREDITED/DEBIT today as below").SetFontSize(9).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph("\n"));

                // Create table (5 columns - no advice number column)
                float[] columnWidths = { 1f, 1.5f, 2f, 3f, 1.5f };
                Table table = new Table(columnWidths);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Header row
                AddTableHeaderCell(table, "Sr. No.");
                AddTableHeaderCell(table, "SCH CODE");
                AddTableHeaderCell(table, "ACCOUNT NO.");
                AddTableHeaderCell(table, "SCHOOL/COLLEGE NAME");
                AddTableHeaderCell(table, "AMOUNT");

                // Data rows
                foreach (var entry in branchReport.Entries)
                {
                    AddTableCell(table, entry.SerialNumber.ToString());
                    AddTableCell(table, entry.SchoolCode);
                    AddTableCell(table, entry.AccountNumber);
                    AddTableCell(table, entry.SchoolName);
                    AddTableCell(table, entry.Amount.ToString("N2"));
                }

                // Total row
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "");
                AddTableHeaderCell(table, "BRANCH TOTAL");
                AddTableHeaderCell(table, branchReport.TotalAmount.ToString("N2"));

                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Footer
                document.Add(new Paragraph($"Prepared By: {preparedBy}").SetFontSize(9));
                document.Add(new Paragraph($"Printed On : {DateTime.Now:dd/MM/yyyy}").SetFontSize(9));
                document.Add(new Paragraph("Note: This is Computer generated statement and required no SIGNATURE").SetFontSize(8).SetItalic());

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating branch-specific report PDF: {ex.Message}", ex);
            }
        }
    }
}
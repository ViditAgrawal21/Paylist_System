using System;
using System.Linq;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Services
{
    /// <summary>
    /// Service for generating unique advice numbers in format YYMMDDXXXXX
    /// where YYMMDD is the date and XXXXX is a branch-specific daily resetting serial number
    /// Each branch gets its own unique advice number sequence per day
    /// </summary>
    public class AdviceNumberService
    {
        private readonly SchoolPayListDbContext _context;

        public AdviceNumberService(SchoolPayListDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Generates a new advice number for the given branch and date
        /// Format: DDMMYY + 2-digit serial (e.g., 26112501 for 26-Nov-2025 with serial 01)
        /// Serial number resets daily and is unique per branch (all schools in same branch get same advice number)
        /// </summary>
        public string GenerateAdviceNumber(DateTime date, int branchId)
        {
            try
            {
                // Format: DDMMYY (today: 261125 for 26-Nov-2025)
                string datePrefix = date.ToString("ddMMyy");

                // Get all advice numbers for this branch today
                var todayStart = date.Date;
                var todayEnd = todayStart.AddDays(1);

                var todayAdviceNumbers = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId 
                        && se.EntryDate >= todayStart 
                        && se.EntryDate < todayEnd 
                        && se.AdviceNumber != null
                        && se.AdviceNumber != "")
                    .Select(se => se.AdviceNumber)
                    .Distinct()
                    .ToList();

                // Extract serial numbers from today's advice numbers for this branch
                int maxSerial = 0;
                foreach (var adviceNo in todayAdviceNumbers)
                {
                    if (adviceNo.Length >= 8)
                    {
                        string serialPart = adviceNo.Substring(6); // Get last 2 digits (serial)
                        if (int.TryParse(serialPart, out int serial))
                        {
                            if (serial > maxSerial)
                                maxSerial = serial;
                        }
                    }
                }

                // If there are already entries for this branch today, return the same advice number
                if (maxSerial > 0)
                {
                    // Return the existing advice number for this branch (shared among all schools in branch)
                    string existingAdviceNumber = datePrefix + maxSerial.ToString("D2");
                    return existingAdviceNumber;
                }

                // First entry for this branch today - create new advice number with serial 01
                int nextSerial = 1;

                // Format: DDMMYY + 2-digit serial (e.g., 26112501)
                string adviceNumber = datePrefix + nextSerial.ToString("D2");

                return adviceNumber;
            }
            catch (Exception)
            {
                // Fallback: generate with current date to ensure we don't return empty
                string datePrefix = DateTime.Now.ToString("ddMMyy");
                return datePrefix + "01";
            }
        }

        /// <summary>
        /// Gets the next serial number for the given branch and date (without generating)
        /// </summary>
        public int GetNextSerialNumber(DateTime date, int branchId)
        {
            try
            {
                var todayStart = date.Date;
                var todayEnd = todayStart.AddDays(1);

                var todayAdviceNumbers = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId 
                        && se.EntryDate >= todayStart 
                        && se.EntryDate < todayEnd 
                        && se.AdviceNumber != null)
                    .Select(se => se.AdviceNumber)
                    .Distinct()
                    .ToList();

                int maxSerial = 0;
                foreach (var adviceNo in todayAdviceNumbers)
                {
                    if (adviceNo.Length >= 8)
                    {
                        string serialPart = adviceNo.Substring(6);
                        if (int.TryParse(serialPart, out int serial))
                        {
                            if (serial > maxSerial)
                                maxSerial = serial;
                        }
                    }
                }

                return maxSerial + 1;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// Legacy method - kept for backward compatibility
        /// Generates a global advice number without branch filtering
        /// </summary>
        [Obsolete("Use GenerateAdviceNumber(DateTime date, int branchId) instead")]
        public string GenerateAdviceNumber(DateTime date)
        {
            try
            {
                // Format: DDMMYY
                string datePrefix = date.ToString("ddMMyy");

                // Get all advice numbers for today (legacy - no branch filtering)
                var todayStart = date.Date;
                var todayEnd = todayStart.AddDays(1);

                var todayAdviceNumbers = _context.SalaryEntries
                    .Where(se => se.EntryDate >= todayStart 
                        && se.EntryDate < todayEnd 
                        && se.AdviceNumber != null 
                        && se.AdviceNumber != "")
                    .Select(se => se.AdviceNumber)
                    .ToList();

                // Extract serial numbers from today's advice numbers
                int maxSerial = 0;
                foreach (var adviceNo in todayAdviceNumbers)
                {
                    if (adviceNo.Length >= 8)
                    {
                        string serialPart = adviceNo.Substring(6); // Get last 2 digits
                        if (int.TryParse(serialPart, out int serial))
                        {
                            if (serial > maxSerial)
                                maxSerial = serial;
                        }
                    }
                }

                // Next serial number
                int nextSerial = maxSerial + 1;

                // Format: DDMMYY + 2-digit serial
                string adviceNumber = datePrefix + nextSerial.ToString("D2");

                return adviceNumber;
            }
            catch (Exception)
            {
                // Fallback: generate with current date
                string datePrefix = DateTime.Now.ToString("ddMMyy");
                return datePrefix + "01";
            }
        }
    }
}

using System;
using System.Linq;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Services
{
    /// <summary>
    /// Service for generating unique advice numbers in format YYMMDDXXXXX
    /// where YYMMDD is the date and XXXXX is a sequential number
    /// Advice numbers are sequential per school type per day
    /// Each school type starts its own sequence (High School: 01-10, Primary: 11-20, etc.)
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
        /// Format: YYMMDD + 2-digit serial (e.g., 25110501 for 05-Nov-2025 with serial 01)
        /// Per-Branch Sequential Numbering:
        /// - Advice numbers are sequential PER BRANCH per day
        /// - All school types within a branch share the same number sequence
        /// - Branch1 entries (any school type) = 01-99, Branch2 entries (any school type) = 01-99, etc.
        /// - Each entry gets a UNIQUE advice number per branch per day
        /// </summary>
        public string GenerateAdviceNumber(DateTime date, int branchId, int schoolTypeId)
        {
            try
            {
                // Format: YYMMDD (today: 251128 for 28-Nov-2025)
                string datePrefix = date.ToString("yyMMdd");

                var todayStart = date.Date;
                var todayEnd = todayStart.AddDays(1);

                // Get ALL advice numbers for THIS BRANCH TODAY (across all school types)
                var branchTodayAdviceNumbers = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId
                        && se.EntryDate >= todayStart
                        && se.EntryDate < todayEnd
                        && se.AdviceNumber != null
                        && se.AdviceNumber != "")
                    .Select(se => se.AdviceNumber)
                    .Distinct()
                    .ToList();

                // Find the highest serial number used for THIS BRANCH TODAY
                int maxSerial = 0;
                foreach (var adviceNo in branchTodayAdviceNumbers)
                {
                    if (adviceNo.Length >= 8 && adviceNo.StartsWith(datePrefix))
                    {
                        string serialPart = adviceNo.Substring(6); // Get last 2 digits
                        if (int.TryParse(serialPart, out int serial))
                        {
                            if (serial > maxSerial)
                                maxSerial = serial;
                        }
                    }
                }

                // Next serial number is unique for this branch today
                int nextSerial = maxSerial + 1;
                
                // Ensure serial is within 2 digits (01-99)
                if (nextSerial > 99)
                {
                    nextSerial = 1; // Reset if we somehow exceed 99
                }

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
        /// Gets the next serial number for the given branch and date (per-branch unique)
        /// </summary>
        public int GetNextSerialNumber(DateTime date, int branchId, int schoolTypeId)
        {
            try
            {
                var todayStart = date.Date;
                var todayEnd = todayStart.AddDays(1);

                // Get ALL advice numbers for THIS BRANCH TODAY (across all school types)
                var branchTodayAdviceNumbers = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId
                        && se.EntryDate >= todayStart
                        && se.EntryDate < todayEnd
                        && se.AdviceNumber != null)
                    .Select(se => se.AdviceNumber)
                    .Distinct()
                    .ToList();

                int maxSerial = 0;
                string datePrefix = date.ToString("ddMMyy");
                foreach (var adviceNo in branchTodayAdviceNumbers)
                {
                    if (adviceNo.Length >= 8 && adviceNo.StartsWith(datePrefix))
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
        /// Legacy method - Gets the next serial number for the given school type and date (without generating)
        /// </summary>
        [Obsolete("Use GetNextSerialNumber(DateTime date, int branchId, int schoolTypeId) instead")]
        public int GetNextSerialNumber(DateTime date, int schoolTypeId)
        {
            try
            {
                var todayStart = date.Date;
                var todayEnd = todayStart.AddDays(1);

                var todayAdviceNumbers = _context.SalaryEntries
                    .Join(_context.Schools, se => se.SchoolId, s => s.SchoolId, (se, s) => new { se, s })
                    .Where(x => x.s.SchoolTypeId == schoolTypeId
                        && x.se.EntryDate >= todayStart
                        && x.se.EntryDate < todayEnd
                        && x.se.AdviceNumber != null)
                    .Select(x => x.se.AdviceNumber)
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
        /// Generates advice number for branch (uses old method signature)
        /// </summary>
        [Obsolete("Use GenerateAdviceNumber(DateTime date, int branchId, int schoolTypeId) instead")]
        public string GenerateAdviceNumber(DateTime date, int branchId)
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

using System;
using System.Linq;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    /// <summary>
    /// Service for generating unique advice numbers in format YYMMDD + 2-digit serial
    /// 
    /// Key Features:
    /// - Globally sequential per day across all (branch + school type) combinations
    /// - Serial resets daily but continues across all branches and school types
    /// - Each (date + branch + school type) combo gets exactly one unique advice number
    /// - Provides automatic sync between Branch Reports and School Summary Reports
    /// 
    /// Example for date 251206:
    /// - BranchA + HighSchool → 25120601
    /// - BranchA + JuniorCollege → 25120602
    /// - BranchB + HighSchool → 25120603
    /// - BranchB + JuniorCollege → 25120604
    /// </summary>
    public class AdviceNumberService
    {
        private readonly SchoolPayListDbContext _context;
        private readonly IAdviceNumberMappingRepository _adviceNumberMappingRepository;

        public AdviceNumberService(SchoolPayListDbContext context)
        {
            _context = context;
            _adviceNumberMappingRepository = new AdviceNumberMappingRepository(context);
        }

        public AdviceNumberService(SchoolPayListDbContext context, IAdviceNumberMappingRepository adviceNumberMappingRepository)
        {
            _context = context;
            _adviceNumberMappingRepository = adviceNumberMappingRepository;
        }

        /// <summary>
        /// Generates or retrieves a unique advice number for the given (date + branch + school type) combination
        /// 
        /// Process:
        /// 1. Check if advice number already exists for this (date, branchId, schoolTypeId)
        /// 2. If exists → return it (for sync between Branch Report and School Summary)
        /// 3. If not exists → generate new advice number using next global serial
        /// 4. Store in AdviceNumberMapping table and return
        /// 
        /// Format: YYMMDD + 2-digit serial (e.g., 25120601 for Dec 6, 2025, serial 01)
        /// </summary>
        public string GenerateAdviceNumber(DateTime date, int branchId, int schoolTypeId, string generatedByModule = "Manual")
        {
            try
            {
                // Step 1: Check if advice number already exists for this combination
                var existingMapping = _adviceNumberMappingRepository
                    .GetByDateBranchSchoolTypeAsync(date, branchId, schoolTypeId)
                    .Result;

                if (existingMapping != null)
                {
                    // Advice number already exists - return it (ensures sync)
                    return existingMapping.AdviceNumber;
                }

                // Step 2: Generate new advice number
                string datePrefix = date.ToString("yyMMdd");

                // Get next global serial number for this date
                int nextSerial = _adviceNumberMappingRepository
                    .GetNextSerialNumberAsync(date)
                    .Result;

                // Ensure serial is within 2 digits (01-99)
                if (nextSerial > 99)
                    nextSerial = 1;

                string adviceNumber = datePrefix + nextSerial.ToString("D2");

                // Step 3: Create and store mapping
                var newMapping = new AdviceNumberMapping
                {
                    AdviceNumber = adviceNumber,
                    AdviceDate = date,
                    BranchId = branchId,
                    SchoolTypeId = schoolTypeId,
                    SerialNumber = nextSerial,
                    GeneratedByModule = generatedByModule,
                    GeneratedTimestamp = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _adviceNumberMappingRepository.AddAsync(newMapping).Wait();
                _adviceNumberMappingRepository.SaveChangesAsync().Wait();

                return adviceNumber;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating advice number: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Asynchronous version of GenerateAdviceNumber
        /// </summary>
        public async Task<string> GenerateAdviceNumberAsync(DateTime date, int branchId, int schoolTypeId, string generatedByModule = "Manual")
        {
            try
            {
                // Check if advice number already exists for this combination
                var existingMapping = await _adviceNumberMappingRepository
                    .GetByDateBranchSchoolTypeAsync(date, branchId, schoolTypeId);

                if (existingMapping != null)
                {
                    return existingMapping.AdviceNumber;
                }

                // Generate new advice number
                string datePrefix = date.ToString("yyMMdd");

                // Get next global serial number for this date
                int nextSerial = await _adviceNumberMappingRepository
                    .GetNextSerialNumberAsync(date);

                // Ensure serial is within 2 digits (01-99)
                if (nextSerial > 99)
                    nextSerial = 1;

                string adviceNumber = datePrefix + nextSerial.ToString("D2");

                // Create and store mapping
                var newMapping = new AdviceNumberMapping
                {
                    AdviceNumber = adviceNumber,
                    AdviceDate = date,
                    BranchId = branchId,
                    SchoolTypeId = schoolTypeId,
                    SerialNumber = nextSerial,
                    GeneratedByModule = generatedByModule,
                    GeneratedTimestamp = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _adviceNumberMappingRepository.AddAsync(newMapping);
                await _adviceNumberMappingRepository.SaveChangesAsync();

                return adviceNumber;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating advice number asynchronously: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets existing advice number for a (date + branch + school type) combination without generating
        /// Returns null if not found
        /// </summary>
        public async Task<string> GetExistingAdviceNumberAsync(DateTime date, int branchId, int schoolTypeId)
        {
            try
            {
                var mapping = await _adviceNumberMappingRepository
                    .GetByDateBranchSchoolTypeAsync(date, branchId, schoolTypeId);

                return mapping?.AdviceNumber;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving existing advice number: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the next global serial number for a given date (without generating/storing)
        /// </summary>
        public async Task<int> GetNextSerialNumberAsync(DateTime date)
        {
            try
            {
                return await _adviceNumberMappingRepository.GetNextSerialNumberAsync(date);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting next serial number: {ex.Message}", ex);
            }
        }
    }
}

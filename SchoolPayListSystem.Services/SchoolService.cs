using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class SchoolService
    {
        private readonly ISchoolRepository _schoolRepository;
        private readonly ISalaryEntryRepository _salaryRepository;

        public SchoolService(ISchoolRepository schoolRepository, ISalaryEntryRepository salaryRepository = null)
        {
            _schoolRepository = schoolRepository;
            _salaryRepository = salaryRepository;
        }

        public async Task<List<School>> GetAllSchoolsAsync()
        {
            return await _schoolRepository.GetAllWithNavigationAsync();
        }

        public async Task<(bool success, string message)> AddSchoolAsync(string schoolCode, string schoolName, 
            int schoolTypeId, int branchId, string bankAccount)
        {
            try
            {
                if (schoolCode == null || schoolName == null)
                {
                    return (false, "School Code and Name cannot be null");
                }

                var school = new School
                {
                    SchoolCode = schoolCode,
                    SchoolName = schoolName,
                    SchoolTypeId = schoolTypeId,
                    BranchId = branchId,
                    BankAccountNumber = bankAccount ?? "",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                await _schoolRepository.AddAsync(school);
                await _schoolRepository.SaveChangesAsync();
                return (true, "School added successfully");
            }
            catch (NullReferenceException nex)
            {
                return (false, $"Null Reference Error: {nex.Message}");
            }
            catch (DbUpdateException dbex)
            {
                return (false, $"Database Error: {dbex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public async Task<List<SalaryEntry>> GetAssociatedSalaryEntriesAsync(int schoolId)
        {
            try
            {
                if (_salaryRepository == null)
                    return new List<SalaryEntry>();
                    
                var allEntries = await _salaryRepository.GetAllWithNavigationAsync();
                var schoolEntries = allEntries.FindAll(se => se.SchoolId == schoolId);
                return schoolEntries;
            }
            catch
            {
                return new List<SalaryEntry>();
            }
        }

        public async Task<(bool success, string message, int associatedDataCount)> DeleteSchoolAsync(int schoolId)
        {
            try
            {
                var school = await _schoolRepository.GetByIdAsync(schoolId);
                if (school == null)
                {
                    return (false, "School not found", 0);
                }

                // Check for associated salary entries
                int associatedCount = 0;
                if (_salaryRepository != null)
                {
                    var associatedEntries = await GetAssociatedSalaryEntriesAsync(schoolId);
                    associatedCount = associatedEntries.Count;
                }
                
                await _schoolRepository.DeleteAsync(schoolId);
                await _schoolRepository.SaveChangesAsync();
                return (true, "School deleted successfully", associatedCount);
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting school: {ex.Message}", 0);
            }
        }
    }
}

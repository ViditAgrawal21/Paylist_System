using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class BranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly ISalaryEntryRepository _salaryRepository;

        public BranchService(IBranchRepository branchRepository, ISalaryEntryRepository salaryRepository = null)
        {
            _branchRepository = branchRepository;
            _salaryRepository = salaryRepository;
        }

        public async Task<List<Branch>> GetAllBranchesAsync()
        {
            return await _branchRepository.GetAllAsync();
        }

        public async Task<(bool success, string message)> AddBranchAsync(int branchCode, string branchName)
        {
            try
            {
                var branch = new Branch 
                { 
                    BranchCode = branchCode, 
                    BranchName = branchName, 
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                await _branchRepository.AddAsync(branch);
                await _branchRepository.SaveChangesAsync();
                return (true, "Branch added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<List<SalaryEntry>> GetAssociatedSalaryEntriesAsync(int branchId)
        {
            try
            {
                if (_salaryRepository == null)
                    return new List<SalaryEntry>();
                    
                var allEntries = await _salaryRepository.GetAllWithNavigationAsync();
                var branchEntries = allEntries.FindAll(se => se.BranchId == branchId);
                return branchEntries;
            }
            catch
            {
                return new List<SalaryEntry>();
            }
        }

        public async Task<(bool success, string message, int associatedDataCount)> DeleteBranchAsync(int branchId)
        {
            try
            {
                var branch = await _branchRepository.GetByIdAsync(branchId);
                if (branch == null)
                {
                    return (false, "Branch not found", 0);
                }

                // Check for associated salary entries
                int associatedCount = 0;
                if (_salaryRepository != null)
                {
                    var associatedEntries = await GetAssociatedSalaryEntriesAsync(branchId);
                    associatedCount = associatedEntries.Count;
                }

                await _branchRepository.DeleteAsync(branchId);
                await _branchRepository.SaveChangesAsync();
                return (true, "Branch deleted successfully", associatedCount);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", 0);
            }
        }
    }
}
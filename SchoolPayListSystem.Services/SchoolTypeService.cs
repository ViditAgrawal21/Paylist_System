using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class SchoolTypeService
    {
        private readonly ISchoolTypeRepository _typeRepository;

        public SchoolTypeService(ISchoolTypeRepository typeRepository)
        {
            _typeRepository = typeRepository;
        }

        public async Task<List<SchoolType>> GetAllTypesAsync()
        {
            return await _typeRepository.GetAllAsync();
        }

        public async Task<(bool success, string message)> AddTypeAsync(string typeName)
        {
            try
            {
                var type = new SchoolType { TypeName = typeName, IsDefault = false, CreatedAt = DateTime.Now };
                await _typeRepository.AddAsync(type);
                await _typeRepository.SaveChangesAsync();
                return (true, "School type added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> AddTypeAsync(string typeCode, string typeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(typeName))
                {
                    return (false, "Type Code and Name cannot be empty");
                }

                var type = new SchoolType 
                { 
                    TypeCode = typeCode, 
                    TypeName = typeName, 
                    IsDefault = false, 
                    CreatedAt = DateTime.Now 
                };
                await _typeRepository.AddAsync(type);
                await _typeRepository.SaveChangesAsync();
                return (true, "School type added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdateTypeAsync(int typeId, string typeName)
        {
            try
            {
                var existingType = await _typeRepository.GetByIdAsync(typeId);
                if (existingType == null)
                {
                    return (false, "School type not found");
                }

                existingType.TypeName = typeName;
                await _typeRepository.UpdateAsync(existingType);
                await _typeRepository.SaveChangesAsync();
                return (true, "School type updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteTypeAsync(int typeId)
        {
            try
            {
                await _typeRepository.DeleteAsync(typeId);
                await _typeRepository.SaveChangesAsync();
                return (true, "School type deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<SchoolType> GetTypeByIdAsync(int typeId)
        {
            return await _typeRepository.GetByIdAsync(typeId);
        }
    }
}

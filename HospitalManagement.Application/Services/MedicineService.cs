using HospitalManagement.Application.DTOs;
using HospitalManagement.Domain.Models;
using HospitalManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace HospitalManagement.Application.Services
{
    public class MedicineService
    {
        private readonly ApplicationDBContext _context;
        private readonly IDbConnection _dbConnection;

        public MedicineService(ApplicationDBContext context, IDbConnection dbConnection)
        {
            _context = context;
            _dbConnection = dbConnection;
        }
         
        public async Task<MedicineResponseDto> CreateMedicineAsync(CreateMedicineDto dto)
        {
            var medicine = new Medicine
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                ManufacturerName = dto.ManufacturerName,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                LastUpdated = DateTime.Now
            };

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return MapToResponseDto(medicine);
        }
         
        public async Task<List<MedicineResponseDto>> GetAllMedicinesDapperAsync()
        {
            var sql = "SELECT Id, Name, ManufacturerName, Price, StockQuantity, LastUpdated FROM Medicines";
            var results = await _dbConnection.QueryAsync<MedicineResponseDto>(sql);
            return results.ToList();
        }
         
        public async Task<MedicineResponseDto> GetMedicineByIdDapperAsync(Guid id)
        {
            var sql = "SELECT Id, Name, ManufacturerName, Price, StockQuantity, LastUpdated FROM Medicines WHERE Id = @Id";
            var result = await _dbConnection.QueryFirstOrDefaultAsync<MedicineResponseDto>(sql, new { Id = id });
            if (result == null)
                throw new KeyNotFoundException($"Medicine with ID {id} not found");
            return result;
        }
         
        //public async Task<List<MedicineResponseDto>> GetAllMedicinesAsync()
        //{
        //    var medicines = await _context.Medicines.ToListAsync();
        //    return medicines.Select(MapToResponseDto).ToList();
        //}
         
        //public async Task<MedicineResponseDto> GetMedicineByIdAsync(Guid id)
        //{
        //    var medicine = await _context.Medicines.FindAsync(id);
        //    if (medicine == null)
        //        throw new KeyNotFoundException($"Medicine with ID {id} not found");

        //    return MapToResponseDto(medicine);
        //}
         
        public async Task<MedicineResponseDto> UpdateMedicineAsync(UpdateMedicineDto dto)
        {
            var medicine = await _context.Medicines.FindAsync(dto.Id);
            if (medicine == null)
                throw new KeyNotFoundException($"Medicine with ID {dto.Id} not found");

            medicine.Name = dto.Name;
            medicine.ManufacturerName = dto.ManufacturerName;
            medicine.Price = dto.Price;
            medicine.StockQuantity = dto.StockQuantity;
            medicine.LastUpdated = DateTime.Now;

            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();

            return MapToResponseDto(medicine);
        }
         
        public async Task<bool> DeleteMedicineAsync(Guid id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
                throw new KeyNotFoundException($"Medicine with ID {id} not found");

            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();

            return true;
        }
         
        public async Task<MedicineResponseDto> UpdateMedicineStockAsync(Guid id, int quantity)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
                throw new KeyNotFoundException($"Medicine with ID {id} not found");

            medicine.StockQuantity = quantity;
            medicine.LastUpdated = DateTime.Now;

            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();

            return MapToResponseDto(medicine);
        }
         
        private MedicineResponseDto MapToResponseDto(Medicine medicine)
        {
            return new MedicineResponseDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                ManufacturerName = medicine.ManufacturerName,
                Price = medicine.Price,
                StockQuantity = medicine.StockQuantity,
                LastUpdated = medicine.LastUpdated
            };
        }
    }
}

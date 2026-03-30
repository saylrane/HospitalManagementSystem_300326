using HospitalManagement.Application.DTOs;
using HospitalManagement.Domain.Models;
using HospitalManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalManagement.Application.Services
{
    public class BillService
    {
        private readonly ApplicationDBContext _context;

        public BillService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<BillResponseDto> CreateBillAsync(CreateBillDto dto)
        {
            throw new InvalidOperationException("Bills can only be created against prescription");
        }

        public async Task<BillDetailResponseDto> CreateBillFromPrescriptionAsync(Guid prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medicine)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
                throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found");

            if (prescription.isDispensed)
                throw new InvalidOperationException("Bill cannot be created. This prescription has already been dispensed and paid.");

            var patientId = prescription.Appointment?.PatientId ?? throw new InvalidOperationException("Prescription has no patient info");

            var bill = new Bill
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                CreatedAt = DateTime.Now,
                IsPaid = false,
                PrescriptionId = prescriptionId
            };

            var unavailable = new List<string>();
            var billItems = new List<BillItem>();
            decimal totalAmount = 0m;

            foreach (var item in prescription.PrescriptionItems)
            {
                var med = item.Medicine;
                if (med == null)
                {
                    unavailable.Add($"Unknown medicine id: {item.MedicineId}");
                    continue;
                }

                if (med.StockQuantity >= item.Quantity)
                {
                    var unitPrice = med.Price;
                    var totalPrice = unitPrice * item.Quantity;

                    var billItem = new BillItem
                    {
                        Id = Guid.NewGuid(),
                        BillId = bill.Id,
                        MedicineId = med.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice
                    };

                    billItems.Add(billItem);
                    totalAmount += totalPrice;
                }
                else
                {
                    unavailable.Add(med.Name);
                    continue;
                }
            }

            bill.Amount = totalAmount;

            _context.Bill.Add(bill);
            if (billItems.Any())
                _context.AddRange(billItems);

            await _context.SaveChangesAsync();

            var response = new BillDetailResponseDto
            {
                BillId = bill.Id,
                PrescriptionId = prescriptionId,
                PatientId = bill.PatientId,
                Amount = bill.Amount,
                IsPaid = bill.IsPaid,
                BillItems = billItems.Select(bi => new BillItemDto
                {
                    MedicineId = bi.MedicineId,
                    MedicineName = bi.Medicine?.Name ?? string.Empty,
                    Quantity = bi.Quantity,
                    UnitPrice = bi.UnitPrice,
                    TotalPrice = bi.TotalPrice
                }).ToList(),
                UnavailableMedicines = unavailable
            };

            return response;
        }

        public async Task<List<BillResponseDto>> GetAllBillsAsync()
        {
            var bills = await _context.Bill
                .Include(b => b.Patient)
                .ToListAsync();

            var dtos = new List<BillResponseDto>();
            foreach (var bill in bills)
            {
                dtos.Add(await MapToResponseDtoAsync(bill));
            }
            return dtos;
        }

        public async Task<BillResponseDto> GetBillByIdAsync(Guid id)
        {
            var bill = await _context.Bill
                .Include(b => b.Patient)
                .Include(b => b.BillItems)
                    .ThenInclude(bi => bi.Medicine)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
                throw new KeyNotFoundException($"Bill with ID {id} not found");

            return await MapToResponseDtoAsync(bill);
        }

        public async Task<List<BillResponseDto>> GetBillsByPatientIdAsync(Guid patientId)
        {
            var bills = await _context.Bill
                .Where(b => b.PatientId == patientId)
                .Include(b => b.Patient)
                .ToListAsync();

            var dtos = new List<BillResponseDto>();
            foreach (var bill in bills)
            {
                dtos.Add(await MapToResponseDtoAsync(bill));
            }
            return dtos;
        }

        public async Task<List<BillResponseDto>> GetUnpaidBillsAsync()
        {
            var bills = await _context.Bill
                .Where(b => !b.IsPaid)
                .Include(b => b.Patient)
                .ToListAsync();

            var dtos = new List<BillResponseDto>();
            foreach (var bill in bills)
            {
                dtos.Add(await MapToResponseDtoAsync(bill));
            }
            return dtos;
        }

        public async Task<BillResponseDto> UpdateBillAsync(UpdateBillDto dto)
        {
            var bill = await _context.Bill.FindAsync(dto.Id);
            if (bill == null)
                throw new KeyNotFoundException($"Bill with ID {dto.Id} not found");

            bill.Amount = dto.Amount;
            bill.IsPaid = dto.IsPaid;

            _context.Bill.Update(bill);
            await _context.SaveChangesAsync();

            return await MapToResponseDtoAsync(bill);
        }

        public async Task<bool> DeleteBillAsync(Guid id)
        {
            var bill = await _context.Bill.FindAsync(id);
            if (bill == null)
                throw new KeyNotFoundException($"Bill with ID {id} not found");

            _context.Bill.Remove(bill);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<BillResponseDto> MapToResponseDtoAsync(Bill bill)
        {
            return new BillResponseDto
            {
                Id = bill.Id,
                PatientId = bill.PatientId,
                PatientName = bill.Patient?.PatientName ?? "Unknown",
                CreatedAt = bill.CreatedAt,
                Amount = bill.Amount,
                IsPaid = bill.IsPaid
            };
        }
    }
}

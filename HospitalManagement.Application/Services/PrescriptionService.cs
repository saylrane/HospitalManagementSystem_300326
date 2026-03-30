using Dapper;
using HospitalManagement.Application.DTOs;
using HospitalManagement.Domain.Models;
using HospitalManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HospitalManagement.Application.Services
{
    public class PrescriptionService
    {
        private readonly ApplicationDBContext _context;
        private readonly IDbConnection _dbConnection;

        public PrescriptionService(ApplicationDBContext context, IDbConnection dbConnection)
        {
            _context = context;
            _dbConnection = dbConnection;
        }
         
        public async Task<PrescriptionResponseDto> CreatePrescriptionAsync(CreatePrescriptionDto dto)
        {
            var appointment = await _context.Appointment.FindAsync(dto.AppointmentId);
            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {dto.AppointmentId} not found");

            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = dto.AppointmentId,
                DatePrescribed = DateTime.Now,
                Remark = dto.Remark,
                isDispensed = false,
                PrescriptionItems = new List<PrescriptionItems>()
            };
             
            foreach (var item in dto.PrescriptionItems)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine == null)
                    throw new KeyNotFoundException($"Medicine with ID {item.MedicineId} not found");

                var prescriptionItem = new PrescriptionItems
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    MedicineId = item.MedicineId,
                    Quantity = item.Quantity,
                    Dosage = item.Dosage,
                    DurationDays = item.DurationDays
                };

                prescription.PrescriptionItems.Add(prescriptionItem);
            }

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return await MapToResponseDtoAsync(prescription);
        }
         
        public async Task<List<PrescriptionResponseDto>> GetAllPrescriptionsAsync()
        {
            var prescriptions = new List<PrescriptionResponseDto>();

            var presRows = await _dbConnection.QueryAsync<(Guid Id, Guid AppointmentId, DateTime DatePrescribed, string Remark, bool isDispensed)>(
                "SELECT Id, AppointmentId, DatePrescribed, Remark, isDispensed FROM Prescriptions");

            foreach (var row in presRows)
            {
                var items = await _dbConnection.QueryAsync<(Guid Id, Guid MedicineId, int Quantity, string Dosage, int DurationDays)>(
                    "SELECT Id, MedicineId, Quantity, Dosage, DurationDays FROM PrescriptionItems WHERE PrescriptionId = @Pid",
                    new { Pid = row.Id });

                var itemDtos = new List<PrescriptionItemResponseDto>();
                foreach (var it in items)
                {
                    var med = await _dbConnection.QueryFirstOrDefaultAsync<(Guid Id, string Name)>(
                        "SELECT Id, Name FROM Medicines WHERE Id = @Id", new { Id = it.MedicineId });

                    itemDtos.Add(new PrescriptionItemResponseDto
                    {
                        Id = it.Id,
                        MedicineId = it.MedicineId,
                        MedicineName = med.Name,
                        Quantity = it.Quantity,
                        Dosage = it.Dosage,
                        DurationDays = it.DurationDays
                    });
                }

                prescriptions.Add(new PrescriptionResponseDto
                {
                    Id = row.Id,
                    AppointmentId = row.AppointmentId,
                    DatePrescribed = row.DatePrescribed,
                    Remark = row.Remark,
                    isDispensed = row.isDispensed,
                    PrescriptionItems = itemDtos
                });
            }

            return prescriptions;
        }
         
        public async Task<PrescriptionResponseDto> GetPrescriptionByIdAsync(Guid id)
        {
            var row = await _dbConnection.QueryFirstOrDefaultAsync<(Guid Id, Guid AppointmentId, DateTime DatePrescribed, string Remark, bool isDispensed)>(
                "SELECT Id, AppointmentId, DatePrescribed, Remark, isDispensed FROM Prescriptions WHERE Id = @Id", new { Id = id });

            if (row.Id == Guid.Empty)
                throw new KeyNotFoundException($"Prescription with ID {id} not found");

            var items = await _dbConnection.QueryAsync<(Guid Id, Guid MedicineId, int Quantity, string Dosage, int DurationDays)>(
                "SELECT Id, MedicineId, Quantity, Dosage, DurationDays FROM PrescriptionItems WHERE PrescriptionId = @Pid",
                new { Pid = id });

            var itemDtos = new List<PrescriptionItemResponseDto>();
            foreach (var it in items)
            {
                var med = await _dbConnection.QueryFirstOrDefaultAsync<(Guid Id, string Name)>(
                    "SELECT Id, Name FROM Medicines WHERE Id = @Id", new { Id = it.MedicineId });

                itemDtos.Add(new PrescriptionItemResponseDto
                {
                    Id = it.Id,
                    MedicineId = it.MedicineId,
                    MedicineName = med.Name,
                    Quantity = it.Quantity,
                    Dosage = it.Dosage,
                    DurationDays = it.DurationDays
                });
            }

            return new PrescriptionResponseDto
            {
                Id = row.Id,
                AppointmentId = row.AppointmentId,
                DatePrescribed = row.DatePrescribed,
                Remark = row.Remark,
                isDispensed = row.isDispensed,
                PrescriptionItems = itemDtos
            };
        }
         
        public async Task<List<PrescriptionResponseDto>> GetPrescriptionsByAppointmentIdAsync(Guid appointmentId)
        {
            var presRows = await _dbConnection.QueryAsync<(Guid Id, Guid AppointmentId, DateTime DatePrescribed, string Remark, bool isDispensed)>(
                "SELECT Id, AppointmentId, DatePrescribed, Remark, isDispensed FROM Prescriptions WHERE AppointmentId = @AId", new { AId = appointmentId });

            var prescriptions = new List<PrescriptionResponseDto>();
            foreach (var row in presRows)
            {
                var items = await _dbConnection.QueryAsync<(Guid Id, Guid MedicineId, int Quantity, string Dosage, int DurationDays)>(
                    "SELECT Id, MedicineId, Quantity, Dosage, DurationDays FROM PrescriptionItems WHERE PrescriptionId = @Pid",
                    new { Pid = row.Id });

                var itemDtos = new List<PrescriptionItemResponseDto>();
                foreach (var it in items)
                {
                    var med = await _dbConnection.QueryFirstOrDefaultAsync<(Guid Id, string Name)>(
                        "SELECT Id, Name FROM Medicines WHERE Id = @Id", new { Id = it.MedicineId });

                    itemDtos.Add(new PrescriptionItemResponseDto
                    {
                        Id = it.Id,
                        MedicineId = it.MedicineId,
                        MedicineName = med.Name,
                        Quantity = it.Quantity,
                        Dosage = it.Dosage,
                        DurationDays = it.DurationDays
                    });
                }

                prescriptions.Add(new PrescriptionResponseDto
                {
                    Id = row.Id,
                    AppointmentId = row.AppointmentId,
                    DatePrescribed = row.DatePrescribed,
                    Remark = row.Remark,
                    isDispensed = row.isDispensed,
                    PrescriptionItems = itemDtos
                });
            }

            return prescriptions;
        }
         
        public async Task<PrescriptionResponseDto> UpdatePrescriptionAsync(UpdatePrescriptionDto dto)
        {
            var prescription = await _context.Prescriptions.FindAsync(dto.Id);
            if (prescription == null)
                throw new KeyNotFoundException($"Prescription with ID {dto.Id} not found");

            prescription.Remark = dto.Remark;
            prescription.isDispensed = dto.isDispensed;

            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();

            return await GetPrescriptionByIdAsync(dto.Id);
        }

         
        public async Task<bool> DeletePrescriptionAsync(Guid id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
                throw new KeyNotFoundException($"Prescription with ID {id} not found");
              
            _context.PrescriptionItems.RemoveRange(prescription.PrescriptionItems);
             
            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();

            return true;
        }
        private async Task<PrescriptionResponseDto> MapToResponseDtoAsync(Prescription prescription)
        {
            return new PrescriptionResponseDto
            {
                Id = prescription.Id,
                AppointmentId = prescription.AppointmentId,
                DatePrescribed = prescription.DatePrescribed,
                Remark = prescription.Remark,
                isDispensed = prescription.isDispensed,
                PrescriptionItems = prescription.PrescriptionItems.Select(pi => new PrescriptionItemResponseDto
                {
                    Id = pi.Id,
                    MedicineId = pi.MedicineId,
                    MedicineName = pi.Medicine?.Name ?? "Unknown",
                    Quantity = pi.Quantity,
                    Dosage = pi.Dosage,
                    DurationDays = pi.DurationDays
                }).ToList()
            };
        }
    }
}

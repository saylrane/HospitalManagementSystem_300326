using HospitalManagement.Application.DTOs;
using HospitalManagement.Domain.Models;
using HospitalManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Application.Services
{
    public class PaymentService
    {
        private readonly ApplicationDBContext _context;

        public PaymentService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
        {
            var bill = await _context.Bill
                .Include(b => b.Patient)
                .Include(b => b.BillItems)
                    .ThenInclude(bi => bi.Medicine)
                .FirstOrDefaultAsync(b => b.Id == dto.BillId);

            if (bill == null)
                throw new KeyNotFoundException($"Bill with ID {dto.BillId} not found");

            if (bill.IsPaid)
                throw new InvalidOperationException("This bill has already been paid. Cannot process another payment.");

            if (!bill.PrescriptionId.HasValue)
                throw new InvalidOperationException("Payment requires a linked prescription. Bill must be created from a prescription.");

            var existingPayment = await _context.Payment
                .FirstOrDefaultAsync(p => p.BillId == dto.BillId);

            if (existingPayment != null)
                throw new InvalidOperationException("Payment has already been processed for this bill.");

            if (dto.PaymentMethod?.ToLower() == "cash")
            {
                if (dto.Amount < bill.Amount)
                    throw new InvalidOperationException($"Payable amount is less than bill amount. Bill amount: {bill.Amount}");
            }
            else
            {
                if (dto.Amount != bill.Amount)
                    throw new InvalidOperationException($"Payable amount does not match bill amount. Bill amount: {bill.Amount}");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                BillId = dto.BillId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                PaidAt = DateTime.Now
            };

            _context.Payment.Add(payment);


            foreach (var item in bill.BillItems)
            {
                var med = item.Medicine;
                if (med == null) continue;

                if (med.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for medicine {med.Name}");

                med.StockQuantity -= item.Quantity;
                med.LastUpdated = DateTime.Now;
                _context.Medicines.Update(med);

                var log = new InventoryLogs
                {
                    Id = Guid.NewGuid(),
                    MedicineId = med.Id,
                    QuantityChanged = -item.Quantity,
                    ActionType = "Dispensed (Payment)",
                    Remarks = $"Dispensed against Bill {bill.Id}"
                };

                _context.InventoryLogs.Add(log);
            }

            bill.IsPaid = true;
            _context.Bill.Update(bill);

            var prescription = await _context.Prescriptions.FirstOrDefaultAsync(p => p.Id == bill.PrescriptionId);
            if (prescription != null)
            {
                prescription.isDispensed = true;
                prescription.DispensedAt = DateTime.Now;
                _context.Prescriptions.Update(prescription);

                var appointment = await _context.Appointment.FirstOrDefaultAsync(a => a.Id == prescription.AppointmentId);
                if (appointment != null)
                {
                    appointment.AppointmentStatus = "Completed";
                    _context.Appointment.Update(appointment);
                }
            }

            await _context.SaveChangesAsync();

            var changeDue = 0m;
            if (dto.PaymentMethod?.ToLower() == "cash")
            {
                changeDue = dto.Amount - bill.Amount;
            }

            return MapToResponseDto(payment, changeDue);
        }

        public async Task<List<PaymentResponseDto>> GetAllPaymentsAsync()
        {
            var payments = await _context.Payment.ToListAsync();
            return payments.Select(p => MapToResponseDto(p, 0)).ToList();
        }

        public async Task<PaymentResponseDto> GetPaymentByIdAsync(Guid id)
        {
            var payment = await _context.Payment.FindAsync(id);
            if (payment == null)
                throw new KeyNotFoundException($"Payment with ID {id} not found");

            return MapToResponseDto(payment, 0);
        }

        public async Task<List<PaymentResponseDto>> GetPaymentsByBillIdAsync(Guid billId)
        {
            var payments = await _context.Payment
                .Where(p => p.BillId == billId)
                .ToListAsync();

            return payments.Select(p => MapToResponseDto(p, 0)).ToList();
        }

        public async Task<PaymentResponseDto> UpdatePaymentAsync(UpdatePaymentDto dto)
        {
            var payment = await _context.Payment.FindAsync(dto.Id);
            if (payment == null)
                throw new KeyNotFoundException($"Payment with ID {dto.Id} not found");

            payment.Amount = dto.Amount;
            payment.PaymentMethod = dto.PaymentMethod;

            _context.Payment.Update(payment);
            await _context.SaveChangesAsync();

            return MapToResponseDto(payment, 0);
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            var payment = await _context.Payment.FindAsync(id);
            if (payment == null)
                throw new KeyNotFoundException($"Payment with ID {id} not found");

            _context.Payment.Remove(payment);
            await _context.SaveChangesAsync();

            return true;
        }

        private PaymentResponseDto MapToResponseDto(Payment payment, decimal changeDue)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                BillId = payment.BillId,
                PaidAt = payment.PaidAt,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                ChangeDue = changeDue
            };
        }
    }
}

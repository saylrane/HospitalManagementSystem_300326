using System;
using System.Collections.Generic;

namespace HospitalManagement.Application.DTOs
{
    public class BillItemDto
    {
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class BillDetailResponseDto
    {
        public Guid BillId { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public List<BillItemDto> BillItems { get; set; } = new List<BillItemDto>();
        public List<string> UnavailableMedicines { get; set; } = new List<string>();
    }
}
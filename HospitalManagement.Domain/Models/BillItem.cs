using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagement.Domain.Models
{
    public class BillItem
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Bill")]
        public Guid BillId { get; set; }
        public Bill Bill { get; set; }

        [ForeignKey("Medicine")]
        public Guid MedicineId { get; set; }
        public Medicine Medicine { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HospitalManagement.Domain.Models
{
    public class Doctor
    {
        public Guid Id { get; set; }
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Name cannot contain special characters")]
        public string Name { get; set; }
        public string Specialty { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public string UserId { get; set; }
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }
}

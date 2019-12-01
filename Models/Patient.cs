using System;
using System.ComponentModel.DataAnnotations;

namespace BackendProject.Models 
{
    public class PatientRegisterModel {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Lastname { get; set; }
        [Required]
        [StringLength(11)]
        public string PESEL { get; set; }
    }

    public class PatientModel {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string PESEL { get; set; }
    }
}
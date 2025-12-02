using System.ComponentModel.DataAnnotations;

namespace ELibrarySystem.Models
{
    public class StudentFormViewModel
    {
        [Key]
        public int StudentId { get; set; }

        [StringLength(60)]
        public string StudentName { get; set; }

        public int SelectedSchoolId { get; set; }

        public int SelectedStandardId { get; set; }

        public int SelectedDivisionId { get; set; }

        public long? StudentAdmissionNo { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(60)]
        [EmailAddress]
        public string EmailId { get; set; }

        [StringLength(70)]
        public string StudentAddress { get; set; }

        [StringLength(150)]
        public string StudentCity { get; set; }

        [StringLength(150)]
        public string StudentDistrict { get; set; }

        [StringLength(150)]
        public string StudentState { get; set; }

        [StringLength(150)]
        public string StudentFatherName { get; set; }

        public long? FatherNumber { get; set; }

        public long? FatherWhatsappNo { get; set; }

        [StringLength(150)]
        public string MotherName { get; set; }

        public long? MotherNumber { get; set; }

        public long? MotherWhatsappNo { get; set; }

        

        public IList<School> Schools { get; set; } = new List<School>();
        public IList<Standard> Standards { get; set; } = new List<Standard>();
        public IList<Division> Divisions { get; set; } = new List<Division>();

        public IList<SchoolUser>schoolUsers { get; set; } = new List<SchoolUser>(); 
    }
}

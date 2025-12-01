using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELibrarySystem.Models
{
    [Table("tbl_student")]
    public class Student
    {
        [Key]
        [Column("student_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StudentId { get; set; }

        [Column("student_name")]
        [StringLength(60)]
        public string StudentName { get; set; }

        [Column("school_Id")]
        public int SchoolId { get; set; }

        [Column("standard_Id")]
        public int StandardId { get; set; }

        [Column("division_Id")]
        public int DivisionId { get; set; }

        [Column("student_admission_no")]
        public long? StudentAdmissionNo { get; set; }

        [Column("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }

        [Column("email_Id")]
        [StringLength(60)]
        public string EmailId { get; set; }

        [Column("student_address")]
        [StringLength(70)]
        public string StudentAddress { get; set; }

        [Column("student_city")]
        [StringLength(150)]
        public string StudentCity { get; set; }

        [Column("student_district")]
        [StringLength(150)]
        public string StudentDistrict { get; set; }

        [Column("student_state")]
        [StringLength(150)]
        public string StudentState { get; set; }

        [Column("student_father_name")]
        [StringLength(150)]
        public string StudentFatherName { get; set; }

        [Column("father_number")]
        public long? FatherNumber { get; set; }

        [Column("father_whatsapp_no")]
        public long? FatherWhatsappNo { get; set; }

        [Column("mother_name")]
        [StringLength(150)]
        public string MotherName { get; set; }

        [Column("mother_number")]
        public long? MotherNumber { get; set; }

        [Column("mother_whatsapp_no")]
        public long? MotherWhatsappNo { get; set; }

        // Navigation properties
        [ForeignKey("SchoolId")]
        public virtual School School { get; set; }

        [ForeignKey("StandardId")]
        public virtual Standard Standard { get; set; }

        [ForeignKey("DivisionId")]
        public virtual Division Division { get; set; }

        public virtual SchoolUser SchoolUser { get; set; }
    }

}

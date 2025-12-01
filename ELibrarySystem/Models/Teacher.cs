using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELibrarySystem.Models
{
    [Table("tbl_teacher")]
    public class Teacher
    {
        [Key]
        [Column("teacher_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeacherId { get; set; }

        [Column("teacher_name")]
        [StringLength(60)]
        public string TeacherName { get; set; }

        [Column("school_Id")]
        public int SchoolId { get; set; }

        [Column("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }

        [Column("email_Id")]
        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string EmailId { get; set; }

        [Column("teacher_address")]
        [StringLength(150)]
        public string TeacherAddress { get; set; }

        [Column("teacher_city")]
        [StringLength(150)]
        public string TeacherCity { get; set; }

        [Column("teacher_District")]
        [StringLength(160)]
        public string TeacherDistrict { get; set; }

        [Column("teacher_state")]
        [StringLength(150)]
        public string TeacherState { get; set; }

        [Column("teacher_mobile_no")]
        public long? TeacherMobileNo { get; set; }

        [Column("teacher_whatsapp_no")]
        public long? TeacherWhatsappNo { get; set; }

        // Navigation property
        [ForeignKey("SchoolId")]
        public virtual School School { get; set; }

        public virtual ICollection<SchoolUser> SchoolUsers { get; set; }
    }
}

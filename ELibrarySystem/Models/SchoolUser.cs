using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELibrarySystem.Models
{
    [Table("tbl_school_users")]
    public class SchoolUser
    {
        [Key]
        [Column("user_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Column("school_Id")]
        [Required]
        public int SchoolId { get; set; }

        [Column("school_code")]
        [Required]
        [StringLength(50)]
        public string SchoolCode { get; set; }

        [Column("student_Id")]
        public int? StudentId { get; set; }

        [Column("teacher_Id")]
        public int? TeacherId { get; set; }

        [Column("username")]
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Column("password")]
        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [Column("role")]
        [Required]
        [StringLength(30)]
        public string Role { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        [ForeignKey("SchoolId")]
        public virtual School School { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; }
    }

}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELibrarySystem.Models
{
    [Table("tbl_division")]
    public class Division
    {
        [Key]
        [Column("division_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DivisionId { get; set; }

        [Column("division_name")]
        [StringLength(50)]
        public string DivisionName { get; set; }

        // Navigation property
        public virtual ICollection<Student> Students { get; set; }
    }
}

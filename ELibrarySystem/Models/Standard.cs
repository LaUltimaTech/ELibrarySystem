using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELibrarySystem.Models
{
    [Table("tbl_standard")]
    public class Standard
    {
        [Key]
        [Column("standard_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StandardId { get; set; }

        [Column("standard_name")]
        [StringLength(40)]
        public string StandardName { get; set; }

        // Navigation property
        public virtual ICollection<Student> Students { get; set; }
    }
}

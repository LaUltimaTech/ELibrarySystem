using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELibrarySystem.Models
{
    [Table("tbl_School")]
    public class School
    {
        [Key]
        [Column("school_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SchoolId { get; set; }

        [Column("school_code")]
        [StringLength(50)]
        public string SchoolCode { get; set; }

        [Column("school_name")]
        [StringLength(150)]
        public string SchoolName { get; set; }

        [Column("school_address")]
        [StringLength(150)]
        public string SchoolAddress { get; set; }

        [Column("school_city")]
        [StringLength(150)]
        public string SchoolCity { get; set; }

        [Column("school_district")]
        [StringLength(150)]
        public string SchoolDistrict { get; set; }

        [Column("school_state")]
        [StringLength(150)]
        public string SchoolState { get; set; }

        [Column("office_number")]
        public long? OfficeNumber { get; set; }

        [Column("whatsapp_number")]
        public long? WhatsappNumber { get; set; }

        [Column("email_Id")]
        [StringLength(150)]
        public string EmailId { get; set; }

        [Column("contact_person")]
        [StringLength(150)]
        public string ContactPerson { get; set; }

        [Column("contact_number")]
        public long? ContactNumber { get; set; }

        [Column("website")]
        [StringLength(150)]
        public string Website { get; set; }

        [Column("Logo")]
        [StringLength(150)]
        public string Logo { get; set; }

        // Navigation properties
        public virtual ICollection<Teacher> Teachers { get; set; }
        public virtual ICollection<Student> Students { get; set; }
        public virtual ICollection<SchoolUser> SchoolUsers { get; set; }
    }
}

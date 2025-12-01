using System.ComponentModel.DataAnnotations;

namespace ELibrarySystem.Models
{
    public class TeacherFormViewModel
    {
        public int TeacherId { get; set; }


        [StringLength(60)]
        public string TeacherName { get; set; }


        public int SelectedSchoolId { get; set; }

        public DateTime? DateOfBirth { get; set; }

       
        [StringLength(150)]
        public string EmailId { get; set; }

        [StringLength(150)]
        public string TeacherAddress { get; set; }

        [StringLength(150)]
        public string TeacherCity { get; set; }

        [StringLength(160)]
        public string TeacherDistrict { get; set; }

        [StringLength(150)]
        public string TeacherState { get; set; }

        public long? TeacherMobileNo { get; set; }

        public long? TeacherWhatsappNo { get; set; }

        // Dropdown data
        public IList<School> Schools { get; set; }
    }
}

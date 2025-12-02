using ELibrarySystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ELibrarySystem.ViewModels
{
    public class TeacherListViewModel
    {
        public int SelectedSchoolId { get; set; }
        public string SearchTerm { get; set; }
        public IList<School> Schools { get; set; } = new List<School>();
        public List<TeacherListItemViewModel> Teachers { get; set; } = new List<TeacherListItemViewModel>();
    }

    public class TeacherListItemViewModel
    {
        public int SrNo { get; set; }
        public int TeacherId { get; set; }
        [StringLength(60)]
        public string TeacherName { get; set; }
        public string Username { get; set; }
        public string School { get; set; }
        public string DOB { get; set; }
        public string Email { get; set; }
        public string TeacherMobileNo { get; set; }
        public string TeacherWhatsappNo { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string State { get; set; }
    }
}
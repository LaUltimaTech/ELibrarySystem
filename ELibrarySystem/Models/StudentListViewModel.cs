using ELibrarySystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ELibrarySystem.ViewModels
{
    public class StudentListViewModel
    {
        public int SelectedSchoolId { get; set; }
        public string SearchTerm { get; set; }
        public IList<School> Schools { get; set; } = new List<School>();
        public List<StudentListItemViewModel> Students { get; set; } = new List<StudentListItemViewModel>();
    }

    public class StudentListItemViewModel
    {
        public int SrNo { get; set; }
        public int StudentId { get; set; }
        [StringLength(60)]
        public string StudentName { get; set; }
        public string Username { get; set; }
        public string AdmissionNo { get; set; }
        public string School { get; set; }
        public string Standard { get; set; }
        public string Division { get; set; }
        public string DOB { get; set; }
        public string Email { get; set; }
        public string FatherName { get; set; }
        public string FatherMobile { get; set; }
        public string MotherName { get; set; }
        public string MotherMobile { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string State { get; set; }
    }
}
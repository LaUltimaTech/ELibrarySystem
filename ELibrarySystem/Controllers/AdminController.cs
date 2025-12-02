using ELibrarySystem.Data;
using ELibrarySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELibraryAdminManagement.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardStatsViewModel
            {
              
                TotalStudents = await _db.Students.CountAsync(),
                TotalSchools = await _db.Schools.CountAsync(),
                TotalTeachers = await _db.Teachers.CountAsync()
            };

            return View(vm);
        }
    }
}

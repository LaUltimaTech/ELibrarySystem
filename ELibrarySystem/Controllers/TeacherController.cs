using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELibrarySystem.Data;
using ELibrarySystem.Models;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;

namespace ELibrarySystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(AppDbContext context, ILogger<TeacherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: Teacher/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TeacherFormViewModel model)
        {
            try
            {
                // Always reload dropdowns
                model.Schools = await _context.Schools.ToListAsync();

                // Validate School selection
                if (model.SelectedSchoolId <= 0)
                {
                    ModelState.AddModelError("SelectedSchoolId", "Please select a school");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Create new teacher entity
                var teacher = new Teacher
                {
                    TeacherName = model.TeacherName,
                    SchoolId = model.SelectedSchoolId,
                    DateOfBirth = model.DateOfBirth,
                    EmailId = model.EmailId,
                    TeacherAddress = model.TeacherAddress,
                    TeacherCity = model.TeacherCity,
                    TeacherDistrict = model.TeacherDistrict,
                    TeacherState = model.TeacherState,
                    TeacherMobileNo = model.TeacherMobileNo,
                    TeacherWhatsappNo = model.TeacherWhatsappNo
                };

                // Add to database
                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                // Generate username: first 4 letters of teacher name + first 4 digits of mobile
                string username = GenerateUsername(model.TeacherName, model.TeacherMobileNo?.ToString() ?? "0000");

                // Get school code
                var school = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == model.SelectedSchoolId);
                string schoolCode = school?.SchoolCode ?? "";

                // Create SchoolUser record
                var schoolUser = new SchoolUser
                {
                    SchoolId = model.SelectedSchoolId,
                    SchoolCode = schoolCode,
                    TeacherId = teacher.TeacherId,
                    Username = username,
                    Password = "1111",
                    Role = "Teacher",
                    CreatedDate = DateTime.Now
                };

                _context.SchoolUsers.Add(schoolUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Teacher created successfully with ID: {teacher.TeacherId}, Username: {username}");

                // Success message
                TempData["SuccessMessage"] = $"Teacher '{model.TeacherName}' has been created successfully! Username: {username}, Password: 1111";

                // Redirect back to Index to show success message
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating teacher");
                TempData["ErrorMessage"] = "Error saving teacher to database. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            }

            // Reload dropdowns if there's an error
            model.Schools = await _context.Schools.ToListAsync();

            return View(model);
        }

        // GET: Teacher/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var schools = await _context.Schools.ToListAsync();

                if (schools == null || schools.Count == 0)
                {
                    _logger.LogWarning("No schools found in database");
                }

                var viewModel = new TeacherFormViewModel
                {
                    Schools = schools ?? new List<School>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Index view: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error loading form: {ex.Message}";

                // Return view with empty lists instead of crashing
                var viewModel = new TeacherFormViewModel
                {
                    Schools = new List<School>()
                };
                return View(viewModel);
            }
        }

        // GET: Teacher/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(id);

                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("Index");
                }

                var viewModel = new TeacherFormViewModel
                {
                    TeacherId = teacher.TeacherId,
                    TeacherName = teacher.TeacherName,
                    SelectedSchoolId = teacher.SchoolId,
                    DateOfBirth = teacher.DateOfBirth,
                    EmailId = teacher.EmailId,
                    TeacherAddress = teacher.TeacherAddress,
                    TeacherCity = teacher.TeacherCity,
                    TeacherDistrict = teacher.TeacherDistrict,
                    TeacherState = teacher.TeacherState,
                    TeacherMobileNo = teacher.TeacherMobileNo,
                    TeacherWhatsappNo = teacher.TeacherWhatsappNo,
                    Schools = await _context.Schools.ToListAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit view");
                TempData["ErrorMessage"] = "Error loading teacher data.";
                return RedirectToAction("Index");
            }
        }

        // POST: Teacher/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeacherFormViewModel model)
        {
            if (id != model.TeacherId)
            {
                TempData["ErrorMessage"] = "Invalid teacher ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var teacher = await _context.Teachers.FindAsync(id);

                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("Index");
                }

                // Update teacher properties
                teacher.TeacherName = model.TeacherName;
                teacher.SchoolId = model.SelectedSchoolId;
                teacher.DateOfBirth = model.DateOfBirth;
                teacher.EmailId = model.EmailId;
                teacher.TeacherAddress = model.TeacherAddress;
                teacher.TeacherCity = model.TeacherCity;
                teacher.TeacherDistrict = model.TeacherDistrict;
                teacher.TeacherState = model.TeacherState;
                teacher.TeacherMobileNo = model.TeacherMobileNo;
                teacher.TeacherWhatsappNo = model.TeacherWhatsappNo;

                _context.Teachers.Update(teacher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Teacher '{model.TeacherName}' has been updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher");
                TempData["ErrorMessage"] = "Error updating teacher. Please try again.";
            }

            model.Schools = await _context.Schools.ToListAsync();

            return View(model);
        }

        // GET: Teacher/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.School)
                    .FirstOrDefaultAsync(t => t.TeacherId == id);

                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("Index");
                }

                return View(teacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete view");
                TempData["ErrorMessage"] = "Error loading teacher data.";
                return RedirectToAction("Index");
            }
        }

        // POST: Teacher/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(id);

                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("Index");
                }

                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Teacher '{teacher.TeacherName}' has been deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting teacher");
                TempData["ErrorMessage"] = "Error deleting teacher. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // Helper method to generate username
        private string GenerateUsername(string teacherName, string mobileNo)
        {
            // Get first 4 letters of teacher name
            string firstFourLetters = teacherName.Length >= 4
                ? teacherName.Substring(0, 4).ToLower()
                : teacherName.ToLower().PadRight(4, 'x');

            // Get first 4 digits of mobile number
            string firstFourDigits = !string.IsNullOrEmpty(mobileNo) && mobileNo.Length >= 4
                ? mobileNo.Substring(0, 4)
                : (!string.IsNullOrEmpty(mobileNo) ? mobileNo : "0000");

            return firstFourLetters + firstFourDigits;
        }

        // GET: Export Teachers Template
        public IActionResult ExportTeachers()
        {
            var columns = new[]
            {
                "TeacherName", "Email", "DOB", "Address",
                "City", "District", "State",
                "MobileNo", "WhatsappNo"
            };

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("TeachersTemplate");

                // Add headers
                for (int i = 0; i < columns.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = columns[i];
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "TeachersTemplate.xlsx");
                }
            }
        }

        // GET: Teacher/ImportTeachers
        public async Task<IActionResult> ImportTeachers()
        {
            var model = new TeacherFormViewModel
            {
                Schools = await _context.Schools.ToListAsync()
            };
            return View(model);
        }

        // POST: Teacher/ImportTeachers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTeachers(int SelectedSchoolId, IFormFile ExcelFile)
        {
            if (SelectedSchoolId <= 0 || ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a school and upload a valid Excel file.";
                return RedirectToAction("ImportTeachers");
            }

            int importedCount = 0;

            try
            {
                using (var stream = new MemoryStream())
                {
                    await ExcelFile.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                        foreach (var row in rows)
                        {
                            try
                            {
                                _logger.LogInformation($"Processing row {row.RowNumber()}...");

                                // Safe parsing helpers
                                long? GetLongValue(IXLCell cell)
                                {
                                    if (cell.DataType == XLDataType.Number)
                                        return (long)cell.GetDouble();
                                    return long.TryParse(cell.GetString(), out var result) ? result : (long?)null;
                                }

                                DateTime? GetDateValue(IXLCell cell)
                                {
                                    if (cell.DataType == XLDataType.DateTime)
                                        return cell.GetDateTime();
                                    return DateTime.TryParse(cell.GetString(), out var result) ? result : (DateTime?)null;
                                }

                                var teacher = new Teacher
                                {
                                    TeacherName = row.Cell(1).GetString(),
                                    EmailId = row.Cell(2).GetString(),
                                    DateOfBirth = GetDateValue(row.Cell(3)),
                                    TeacherAddress = row.Cell(4).GetString(),
                                    TeacherCity = row.Cell(5).GetString(),
                                    TeacherDistrict = row.Cell(6).GetString(),
                                    TeacherState = row.Cell(7).GetString(),
                                    TeacherMobileNo = GetLongValue(row.Cell(8)),
                                    TeacherWhatsappNo = GetLongValue(row.Cell(9)),
                                    SchoolId = SelectedSchoolId
                                };

                                _context.Teachers.Add(teacher);
                                await _context.SaveChangesAsync();
                                importedCount++;

                                // Generate username
                                string username = GenerateUsername(teacher.TeacherName, teacher.TeacherMobileNo?.ToString() ?? "0000");

                                // Ensure username is unique
                                if (await _context.SchoolUsers.AnyAsync(u => u.Username == username && u.Role == "Teacher"))
                                {
                                    _logger.LogWarning($"Duplicate username '{username}' at row {row.RowNumber()}, appending ID");
                                    username = username + teacher.TeacherId; // Make it unique by adding ID
                                }

                                // Get school code
                                var school = await _context.Schools.FindAsync(SelectedSchoolId);
                                string schoolCode = school?.SchoolCode ?? "";

                                // Create SchoolUser record
                                var schoolUser = new SchoolUser
                                {
                                    SchoolId = SelectedSchoolId,
                                    SchoolCode = schoolCode,
                                    TeacherId = teacher.TeacherId,
                                    Username = username,
                                    Password = "1111",
                                    Role = "Teacher",
                                    CreatedDate = DateTime.Now
                                };

                                _context.SchoolUsers.Add(schoolUser);
                                await _context.SaveChangesAsync();

                                _logger.LogInformation($"Row {row.RowNumber()} imported successfully: {teacher.TeacherName}, Username={username}");
                            }
                            catch (Exception rowEx)
                            {
                                _logger.LogError(rowEx, $"Error processing row {row.RowNumber()}");
                                continue; // skip row, don't break whole loop
                            }
                        }
                    }
                }

                if (importedCount > 0)
                {
                    TempData["SuccessMessage"] = $"{importedCount} teachers imported successfully! Each teacher has been assigned a username and password (1111).";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "No teachers were imported. Please check the file format and data.";
                    return RedirectToAction("ImportTeachers");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing teachers: {ex.Message}");
                TempData["ErrorMessage"] = $"Error importing teachers: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("ImportTeachers");
            }
        }
    }
}
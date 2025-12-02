using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELibrarySystem.Data;
using ELibrarySystem.Models;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;


namespace ELibrarySystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StudentController> _logger;

        public StudentController(AppDbContext context, ILogger<StudentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: Student/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(StudentFormViewModel model)
        {
            try
            {
                // Always reload dropdowns
                model.Schools = await _context.Schools.ToListAsync();
                model.Standards = await _context.Standards.ToListAsync();
                model.Divisions = await _context.Divisions.ToListAsync();

                // Validate School, Standard, Division selections
                if (model.SelectedSchoolId <= 0)
                {
                    ModelState.AddModelError("SelectedSchoolId", "Please select a school");
                }

                if (model.SelectedStandardId <= 0)
                {
                    ModelState.AddModelError("SelectedStandardId", "Please select a standard");
                }

                if (model.SelectedDivisionId <= 0)
                {
                    ModelState.AddModelError("SelectedDivisionId", "Please select a division");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Create new student entity
                var student = new Student
                {
                    StudentName = model.StudentName,
                    SchoolId = model.SelectedSchoolId,
                    StandardId = model.SelectedStandardId,
                    DivisionId = model.SelectedDivisionId,
                    StudentAdmissionNo = model.StudentAdmissionNo,
                    DateOfBirth = model.DateOfBirth,
                    EmailId = model.EmailId,
                    StudentAddress = model.StudentAddress,
                    StudentCity = model.StudentCity,
                    StudentDistrict = model.StudentDistrict,
                    StudentState = model.StudentState,
                    StudentFatherName = model.StudentFatherName,
                    FatherNumber = model.FatherNumber,
                    FatherWhatsappNo = model.FatherWhatsappNo,
                    MotherName = model.MotherName,
                    MotherNumber = model.MotherNumber,
                    MotherWhatsappNo = model.MotherWhatsappNo
                };

                // Add to database
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Generate username: first 4 letters of student name + first letter of father number
                string username = GenerateUsername(model.StudentName, model.FatherNumber.ToString());

                // Get school code
                var school = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == model.SelectedSchoolId);
                string schoolCode = school?.SchoolCode ?? "";

                // Create SchoolUser record
                var schoolUser = new SchoolUser
                {
                    SchoolId = model.SelectedSchoolId,
                    SchoolCode = schoolCode,
                    StudentId = student.StudentId,
                    Username = username,
                    Password = "1111",
                    Role = "Student",
                    CreatedDate = DateTime.Now
                };

                _context.SchoolUsers.Add(schoolUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Student created successfully with ID: {student.StudentId}, Username: {username}");

                // Success message
                TempData["SuccessMessage"] = $"Student '{model.StudentName}' has been created successfully! Username: {username}, Password: 1111";

                // Redirect back to Index to show success message
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating student");
                TempData["ErrorMessage"] = "Error saving student to database. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            }

            // Reload dropdowns if there's an error
            model.Schools = await _context.Schools.ToListAsync();
            model.Standards = await _context.Standards.ToListAsync();
            model.Divisions = await _context.Divisions.ToListAsync();

            return View(model);
        }

        // GET: Student/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var schools = await _context.Schools.ToListAsync();
                var standards = await _context.Standards.ToListAsync();
                var divisions = await _context.Divisions.ToListAsync();

                if (schools == null || schools.Count == 0)
                {
                    _logger.LogWarning("No schools found in database");
                }
                if (standards == null || standards.Count == 0)
                {
                    _logger.LogWarning("No standards found in database");
                }
                if (divisions == null || divisions.Count == 0)
                {
                    _logger.LogWarning("No divisions found in database");
                }

                var viewModel = new StudentFormViewModel
                {
                    Schools = schools ?? new List<School>(),
                    Standards = standards ?? new List<Standard>(),
                    Divisions = divisions ?? new List<Division>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Index view: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error loading form: {ex.Message}";

                // Return view with empty lists instead of crashing
                var viewModel = new StudentFormViewModel
                {
                    Schools = new List<School>(),
                    Standards = new List<Standard>(),
                    Divisions = new List<Division>()
                };
                return View(viewModel);
            }
        }

        // GET: Student/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                var viewModel = new StudentFormViewModel
                {
                    StudentId = student.StudentId,
                    StudentName = student.StudentName,
                    SelectedSchoolId = student.SchoolId,
                    SelectedStandardId = student.StandardId,
                    SelectedDivisionId = student.DivisionId,
                    StudentAdmissionNo = student.StudentAdmissionNo,
                    DateOfBirth = student.DateOfBirth,
                    EmailId = student.EmailId,
                    StudentAddress = student.StudentAddress,
                    StudentCity = student.StudentCity,
                    StudentDistrict = student.StudentDistrict,
                    StudentState = student.StudentState,
                    StudentFatherName = student.StudentFatherName,
                    FatherNumber = student.FatherNumber,
                    FatherWhatsappNo = student.FatherWhatsappNo,
                    MotherName = student.MotherName,
                    MotherNumber = student.MotherNumber,
                    MotherWhatsappNo = student.MotherWhatsappNo,
                    Schools = await _context.Schools.ToListAsync(),
                    Standards = await _context.Standards.ToListAsync(),
                    Divisions = await _context.Divisions.ToListAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit view");
                TempData["ErrorMessage"] = "Error loading student data.";
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentFormViewModel model)
        {
            if (id != model.StudentId)
            {
                TempData["ErrorMessage"] = "Invalid student ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                // Update student properties
                student.StudentName = model.StudentName;
                student.SchoolId = model.SelectedSchoolId;
                student.StandardId = model.SelectedStandardId;
                student.DivisionId = model.SelectedDivisionId;
                student.StudentAdmissionNo = model.StudentAdmissionNo;
                student.DateOfBirth = model.DateOfBirth;
                student.EmailId = model.EmailId;
                student.StudentAddress = model.StudentAddress;
                student.StudentCity = model.StudentCity;
                student.StudentDistrict = model.StudentDistrict;
                student.StudentState = model.StudentState;
                student.StudentFatherName = model.StudentFatherName;
                student.FatherNumber = model.FatherNumber;
                student.FatherWhatsappNo = model.FatherWhatsappNo;
                student.MotherName = model.MotherName;
                student.MotherNumber = model.MotherNumber;
                student.MotherWhatsappNo = model.MotherWhatsappNo;

                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Student '{model.StudentName}' has been updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                TempData["ErrorMessage"] = "Error updating student. Please try again.";
            }

            model.Schools = await _context.Schools.ToListAsync();
            model.Standards = await _context.Standards.ToListAsync();
            model.Divisions = await _context.Divisions.ToListAsync();

            return View(model);
        }

        // GET: Student/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.School)
                    .Include(s => s.Standard)
                    .Include(s => s.Division)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete view");
                TempData["ErrorMessage"] = "Error loading student data.";
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Student '{student.StudentName}' has been deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                TempData["ErrorMessage"] = "Error deleting student. Please try again.";
                return RedirectToAction("Index");
            }
        }
        // Helper method to generate username
        private string GenerateUsername(string studentName, string fatherNumber)
        {
            // Get first 4 letters of student name
            string firstFourLetters = studentName.Length >= 4
                ? studentName.Substring(0, 4).ToLower()
                : studentName.ToLower().PadRight(4, 'x');

            // Get first 4 digits of father number
            string firstFourDigits = !string.IsNullOrEmpty(fatherNumber) && fatherNumber.Length >= 4
                ? fatherNumber.Substring(0, 4)
                : (!string.IsNullOrEmpty(fatherNumber) ? fatherNumber : "0000");

            return firstFourLetters + firstFourDigits;
        }

        public IActionResult ExportStudents()
        {
            var columns = new[]
            {
        "StudentName", "AdmissionNo", "Standard", "Division", "DOB", "Email",
        "Address", "City", "District", "State",
        "FatherName", "FatherNumber", "FatherWhatsappNo",
        "MotherName", "MotherNumber", "MotherWhatsappNo"
    };

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("StudentsTemplate");

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
                        "StudentsTemplate.xlsx");
                }
            }
        }


        public async Task<IActionResult> ImportStudents()
        {
            var model = new StudentFormViewModel
            {
                Schools = await _context.Schools.ToListAsync()
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStudents(int SelectedSchoolId, IFormFile ExcelFile)
        {
            if (SelectedSchoolId <= 0 || ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a school and upload a valid Excel file.";
                return RedirectToAction("ImportStudents");
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

                                var student = new Student
                                {
                                    StudentName = row.Cell(1).GetString(),
                                    StudentAdmissionNo = GetLongValue(row.Cell(2)),
                                    StandardId = await GetStandardId(row.Cell(3).GetString()),
                                    DivisionId = await GetDivisionId(row.Cell(4).GetString()),
                                    DateOfBirth = GetDateValue(row.Cell(5)),
                                    EmailId = row.Cell(6).GetString(),
                                    StudentAddress = row.Cell(7).GetString(),
                                    StudentCity = row.Cell(8).GetString(),
                                    StudentDistrict = row.Cell(9).GetString(),
                                    StudentState = row.Cell(10).GetString(),
                                    StudentFatherName = row.Cell(11).GetString(),
                                    FatherNumber = GetLongValue(row.Cell(12)),
                                    FatherWhatsappNo = GetLongValue(row.Cell(13)),
                                    MotherName = row.Cell(14).GetString(),
                                    MotherNumber = GetLongValue(row.Cell(15)),
                                    MotherWhatsappNo = GetLongValue(row.Cell(16)),
                                    SchoolId = SelectedSchoolId
                                };

                                // Validate FK values
                                if (student.StandardId == 0 || student.DivisionId == 0)
                                {
                                    _logger.LogError($"Invalid Standard/Division at row {row.RowNumber()}");
                                    continue; // skip this row
                                }

                                _context.Students.Add(student);
                                await _context.SaveChangesAsync();
                                importedCount++;

                                // Generate username
                                string username = GenerateUsername(student.StudentName, student.FatherNumber?.ToString() ?? "0000");

                                // Ensure username is unique
                                if (await _context.SchoolUsers.AnyAsync(u => u.Username == username))
                                {
                                    _logger.LogError($"Duplicate username '{username}' at row {row.RowNumber()}");
                                    continue; // skip this row
                                }

                                var school = await _context.Schools.FindAsync(SelectedSchoolId);

                                var schoolUser = new SchoolUser
                                {
                                    SchoolId = SelectedSchoolId,
                                    SchoolCode = school?.SchoolCode ?? "",
                                    StudentId = student.StudentId,
                                    Username = username,
                                    Password = "1111",
                                    Role = "Student",
                                    CreatedDate = DateTime.Now
                                };

                                _context.SchoolUsers.Add(schoolUser);
                                await _context.SaveChangesAsync();

                                _logger.LogInformation($"Row {row.RowNumber()} imported successfully: {student.StudentName}, Username={username}");
                            }
                            catch (Exception rowEx)
                            {
                                _logger.LogError(rowEx, $"Error processing row {row.RowNumber()}");
                                continue; // skip row, don’t break whole loop
                            }
                        }
                    }
                }

                if (importedCount > 0)
                {
                    TempData["SuccessMessage"] = $"{importedCount} students imported successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "No students were imported. Please check the file format and data.";
                    return RedirectToAction("ImportStudents");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing students: {ex.Message}");
                TempData["ErrorMessage"] = $"Error importing students: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("ImportStudents");
            }
        }




        private async Task<int> GetStandardId(string name)
        {
            var standard = await _context.Standards.FirstOrDefaultAsync(s => s.StandardName == name);
            return standard?.StandardId ?? 0;
        }

        private async Task<int> GetDivisionId(string name)
        {
            var division = await _context.Divisions.FirstOrDefaultAsync(d => d.DivisionName == name);
            return division?.DivisionId ?? 0;
        }

        private long? GetLongValue(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
            {
                return (long)cell.GetDouble();
            }
            else
            {
                return long.TryParse(cell.GetString(), out var result) ? result : (long?)null;
            }
        }



        public async Task<IActionResult> ListStudents(int? schoolId, int pageNumber = 1)
        {
            try
            {
                int pageSize = 10;
                var query = _context.Students
                    .Include(s => s.School)
                    .Include(s => s.Standard)
                    .Include(s => s.Division)
                    .AsQueryable();

                // Filter by school if selected
                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(s => s.SchoolId == schoolId);
                }

                // Get total count
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Paginate
                var students = await query
                    .OrderByDescending(s => s.StudentId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get list of schools for dropdown
                var schools = await _context.Schools.ToListAsync();

                // Create view model with student data
                var viewModel = new StudentFormViewModel
                {
                    Schools = schools,
                    SelectedSchoolId = schoolId ?? 0
                };

                // Store additional data in ViewBag for list display
                ViewBag.Students = students;
                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;
                ViewBag.SelectedColumns = GetSelectedColumns();
                ViewBag.SchoolUsers = await GetSchoolUsersForStudents(students.Select(s => s.StudentId).ToList());

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student list");
                TempData["ErrorMessage"] = "Error loading student list.";
                return RedirectToAction("Index");
            }
        }

        // Helper: Get SchoolUser data for students
        private async Task<List<dynamic>> GetSchoolUsersForStudents(List<int> studentIds)
        {
            var schoolUsers = await _context.SchoolUsers
                .Where(su => studentIds.Contains(su.StudentId ?? 0))
                .ToListAsync();

            return schoolUsers.Cast<dynamic>().ToList();
        }

        // POST: Update selected columns for visibility
        [HttpPost]
        public IActionResult UpdateColumnVisibility(string columns)
        {
            try
            {
                if (!string.IsNullOrEmpty(columns))
                {
                    HttpContext.Session.SetString("VisibleColumns", columns);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating column visibility");
                return BadRequest();
            }
        }

        // Helper: Get selected columns from session
        private List<string> GetSelectedColumns()
        {
            var defaultColumns = new List<string>
            {
                "StudentName", "Username", "Standard", "Mobile", "Edit", "Delete"
            };

            var columnString = HttpContext.Session.GetString("VisibleColumns");
            if (string.IsNullOrEmpty(columnString))
            {
                return defaultColumns;
            }

            return columnString.Split(',').ToList();
        }

        // GET: Export to Excel with selected school filter
        public async Task<IActionResult> ExportToExcel(int? schoolId)
        {
            try
            {
                var query = _context.Students
                    .Include(s => s.School)
                    .Include(s => s.Standard)
                    .Include(s => s.Division)
                    .AsQueryable();

                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(s => s.SchoolId == schoolId);
                }

                var students = await query.ToListAsync();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Students");

                    // Add headers
                    worksheet.Cell(1, 1).Value = "Sr No.";
                    worksheet.Cell(1, 2).Value = "Student Name";
                    worksheet.Cell(1, 3).Value = "Username";
                    worksheet.Cell(1, 4).Value = "Admission No.";
                    worksheet.Cell(1, 5).Value = "School";
                    worksheet.Cell(1, 6).Value = "Standard";
                    worksheet.Cell(1, 7).Value = "Division";
                    worksheet.Cell(1, 8).Value = "DOB";
                    worksheet.Cell(1, 9).Value = "Email";
                    worksheet.Cell(1, 10).Value = "Address";
                    worksheet.Cell(1, 11).Value = "City";
                    worksheet.Cell(1, 12).Value = "District";
                    worksheet.Cell(1, 13).Value = "State";
                    worksheet.Cell(1, 14).Value = "Father Name";
                    worksheet.Cell(1, 15).Value = "Father Mobile";
                    worksheet.Cell(1, 16).Value = "Mother Name";
                    worksheet.Cell(1, 17).Value = "Mother Mobile";

                    // Format header row
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data rows
                    for (int i = 0; i < students.Count; i++)
                    {
                        var student = students[i];
                        var schoolUser = await _context.SchoolUsers
                            .FirstOrDefaultAsync(su => su.StudentId == student.StudentId);

                        worksheet.Cell(i + 2, 1).Value = i + 1;
                        worksheet.Cell(i + 2, 2).Value = student.StudentName;
                        worksheet.Cell(i + 2, 3).Value = schoolUser?.Username ?? "";
                        worksheet.Cell(i + 2, 4).Value = student.StudentAdmissionNo;
                        worksheet.Cell(i + 2, 5).Value = student.School?.SchoolName ?? "";
                        worksheet.Cell(i + 2, 6).Value = student.Standard?.StandardName ?? "";
                        worksheet.Cell(i + 2, 7).Value = student.Division?.DivisionName ?? "";
                        worksheet.Cell(i + 2, 8).Value = student.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
                        worksheet.Cell(i + 2, 9).Value = student.EmailId;
                        worksheet.Cell(i + 2, 10).Value = student.StudentAddress;
                        worksheet.Cell(i + 2, 11).Value = student.StudentCity;
                        worksheet.Cell(i + 2, 12).Value = student.StudentDistrict;
                        worksheet.Cell(i + 2, 13).Value = student.StudentState;
                        worksheet.Cell(i + 2, 14).Value = student.StudentFatherName;
                        worksheet.Cell(i + 2, 15).Value = student.FatherNumber;
                        worksheet.Cell(i + 2, 16).Value = student.MotherName;
                        worksheet.Cell(i + 2, 17).Value = student.MotherNumber;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"Students_{DateTime.Now:ddMMyyyy}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                TempData["ErrorMessage"] = "Error exporting data.";
                return RedirectToAction("ListStudents");
            }
        }

        // GET: Export to PDF
        public async Task<IActionResult> ExportToPdf(int? schoolId)
        {
            try
            {
                var query = _context.Students
                    .Include(s => s.School)
                    .Include(s => s.Standard)
                    .Include(s => s.Division)
                    .AsQueryable();

                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(s => s.SchoolId == schoolId);
                }

                var students = await query.ToListAsync();

                // Return view that generates PDF
                return View("ExportPdf", students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                TempData["ErrorMessage"] = "Error exporting data.";
                return RedirectToAction("ListStudents");
            }
        }




    }
}
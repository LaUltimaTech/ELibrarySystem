using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELibrarySystem.Data;
using ELibrarySystem.Models;
using ELibrarySystem.ViewModels;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using BaseFont = iTextSharp.text.pdf.BaseFont;

namespace ELibrarySystem.Controllers
{
    public class StudentListController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StudentListController> _logger;

        public StudentListController(AppDbContext context, ILogger<StudentListController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: StudentList/Index
        [HttpGet]
        public async Task<IActionResult> Index(int? schoolId, int pageNumber = 1, string searchTerm = "")
        {
            try
            {
                int pageSize = 10;
                var query = _context.Students
                    .Include(s => s.School)
                    .Include(s => s.Standard)
                    .Include(s => s.Division)
                    .AsQueryable();

                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(s => s.SchoolId == schoolId);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(s =>
                        s.StudentName.Contains(searchTerm) ||
                        s.StudentAdmissionNo.ToString().Contains(searchTerm) ||
                        s.EmailId.Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

                var students = await query
                    .OrderByDescending(s => s.StudentId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var studentIds = students.Select(s => s.StudentId).ToList();
                var schoolUsers = await _context.SchoolUsers
                    .Where(su => studentIds.Contains(su.StudentId ?? 0))
                    .ToDictionaryAsync(su => su.StudentId ?? 0);

                var schools = await _context.Schools
                    .OrderBy(s => s.SchoolName)
                    .ToListAsync();

                var viewModel = new StudentListViewModel
                {
                    Schools = schools,
                    SelectedSchoolId = schoolId ?? 0,
                    SearchTerm = searchTerm,
                    Students = new List<StudentListItemViewModel>()
                };

                int srNo = ((pageNumber - 1) * pageSize) + 1;
                foreach (var student in students)
                {
                    var username = schoolUsers.ContainsKey(student.StudentId)
                        ? schoolUsers[student.StudentId].Username
                        : "";

                    viewModel.Students.Add(new StudentListItemViewModel
                    {
                        SrNo = srNo++,
                        StudentId = student.StudentId,
                        StudentName = student.StudentName,
                        Username = username,
                        AdmissionNo = student.StudentAdmissionNo?.ToString() ?? "",
                        School = student.School?.SchoolName ?? "",
                        Standard = student.Standard?.StandardName ?? "",
                        Division = student.Division?.DivisionName ?? "",
                        DOB = student.DateOfBirth?.ToString("dd/MM/yyyy") ?? "",
                        Email = student.EmailId ?? "",
                        FatherName = student.StudentFatherName ?? "",
                        FatherMobile = student.FatherNumber?.ToString() ?? "",
                        MotherName = student.MotherName ?? "",
                        MotherMobile = student.MotherNumber?.ToString() ?? "",
                        Address = student.StudentAddress ?? "",
                        City = student.StudentCity ?? "",
                        District = student.StudentDistrict ?? "",
                        State = student.StudentState ?? ""
                    });
                }

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageSize = pageSize;

                _logger.LogInformation($"StudentList: Retrieved {students.Count} students for page {pageNumber}, SchoolId: {schoolId}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student list");
                TempData["ErrorMessage"] = "Error loading student list.";
                return View(new StudentListViewModel { Schools = new List<School>() });
            }
        }

        // POST: StudentList/UpdateStudent
        [HttpPost]
        public async Task<IActionResult> UpdateStudent([FromBody] Student student)
        {
            try
            {
                if (student == null || student.StudentId <= 0)
                {
                    return Json(new { success = false, message = "Invalid student data." });
                }

                var existingStudent = await _context.Students.FindAsync(student.StudentId);
                if (existingStudent == null)
                {
                    return Json(new { success = false, message = "Student not found." });
                }

                // Update only allowed fields (not School, Standard, Division)
                existingStudent.StudentName = student.StudentName;
                existingStudent.StudentAdmissionNo = student.StudentAdmissionNo;
                existingStudent.DateOfBirth = student.DateOfBirth;
                existingStudent.EmailId = student.EmailId;
                existingStudent.StudentAddress = student.StudentAddress;
                existingStudent.StudentCity = student.StudentCity;
                existingStudent.StudentDistrict = student.StudentDistrict;
                existingStudent.StudentState = student.StudentState;
                existingStudent.StudentFatherName = student.StudentFatherName;
                existingStudent.FatherNumber = student.FatherNumber;
                existingStudent.FatherWhatsappNo = student.FatherWhatsappNo;
                existingStudent.MotherName = student.MotherName;
                existingStudent.MotherNumber = student.MotherNumber;
                existingStudent.MotherWhatsappNo = student.MotherWhatsappNo;

                _context.Students.Update(existingStudent);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Student '{student.StudentName}' (ID: {student.StudentId}) updated successfully");

                return Json(new { success = true, message = $"Student '{student.StudentName}' has been updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating student");
                return Json(new { success = false, message = $"Error updating student: {ex.Message}" });
            }
        }

        // POST: StudentList/DeleteStudent
        [HttpPost]
        public async Task<IActionResult> DeleteStudent([FromBody] DeleteRequest request)
        {
            try
            {
                var student = await _context.Students.FindAsync(request.Id);
                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found." });
                }

                var studentName = student.StudentName;

                // Delete SchoolUser(s) associated with this student
                var schoolUsers = await _context.SchoolUsers
                    .Where(su => su.StudentId == request.Id)
                    .ToListAsync();

                foreach (var schoolUser in schoolUsers)
                {
                    _context.SchoolUsers.Remove(schoolUser);
                }

                // Delete the student
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Student '{studentName}' (ID: {request.Id}) and associated SchoolUser records deleted successfully");

                return Json(new { success = true, message = $"Student '{studentName}' has been deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting student");
                return Json(new { success = false, message = "Error deleting student. Please try again." });
            }
        }

        // GET: StudentList/ExportToExcel
        [HttpGet]
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

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Students");

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

                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

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

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        _logger.LogInformation($"Exported {students.Count} students to Excel, SchoolId: {schoolId}");
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
                return RedirectToAction("Index");
            }
        }

        // GET: StudentList/ExportToPdf
        [HttpGet]
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

                Document doc = new Document(PageSize.A4.Rotate());
                MemoryStream stream = new MemoryStream();
                PdfWriter.GetInstance(doc, stream);

                doc.Open();

                Font titleFont = new Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 16, Font.BOLD);
                Font headerFont = new Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 10, Font.BOLD);
                Font cellFont = new Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 10);

                Paragraph title = new Paragraph("Student List Report", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                doc.Add(title);

                Paragraph dateInfo = new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", cellFont);
                dateInfo.Alignment = Element.ALIGN_CENTER;
                doc.Add(dateInfo);

                doc.Add(new Paragraph("\n"));

                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5, 15, 12, 12, 10, 10, 12, 12, 12 });

                string[] headers = { "Sr No.", "Student Name", "Username", "Admission No.", "Standard", "Division", "Email", "Father Mobile", "Mother Mobile" };
                foreach (var header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(200, 200, 200);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    var schoolUser = await _context.SchoolUsers
                        .FirstOrDefaultAsync(su => su.StudentId == student.StudentId);

                    table.AddCell(new PdfPCell(new Phrase((i + 1).ToString(), cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.StudentName ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(schoolUser?.Username ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.StudentAdmissionNo?.ToString() ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.Standard?.StandardName ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.Division?.DivisionName ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.EmailId ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.FatherNumber?.ToString() ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(student.MotherNumber?.ToString() ?? "", cellFont)));
                }

                doc.Add(table);
                doc.Close();

                byte[] pdfContent = stream.ToArray();
                _logger.LogInformation($"Exported {students.Count} students to PDF, SchoolId: {schoolId}");

                return File(pdfContent, "application/pdf", $"Students_{DateTime.Now:ddMMyyyy}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                TempData["ErrorMessage"] = $"Error exporting to PDF: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: StudentList/UpdateColumnVisibility
        [HttpPost]
        public IActionResult UpdateColumnVisibility(string columns)
        {
            try
            {
                if (!string.IsNullOrEmpty(columns))
                {
                    HttpContext.Session.SetString("VisibleColumns", columns);
                    _logger.LogInformation($"Column visibility updated: {columns}");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating column visibility");
                return BadRequest();
            }
        }

        // GET: StudentList/GetStudentDetails/{id}
        [HttpGet]
        public async Task<IActionResult> GetStudentDetails(int id)
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
                    return NotFound();
                }

                var schoolUser = await _context.SchoolUsers
                    .FirstOrDefaultAsync(su => su.StudentId == id);

                var details = new
                {
                    studentId = student.StudentId,
                    studentName = student.StudentName,
                    username = schoolUser?.Username ?? "",
                    admissionNo = student.StudentAdmissionNo,
                    school = student.School?.SchoolName ?? "",
                    standard = student.Standard?.StandardName ?? "",
                    division = student.Division?.DivisionName ?? "",
                    dob = student.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                    email = student.EmailId,
                    fatherName = student.StudentFatherName,
                    fatherNumber = student.FatherNumber,
                    fatherWhatsappNo = student.FatherWhatsappNo,
                    motherName = student.MotherName,
                    motherNumber = student.MotherNumber,
                    motherWhatsappNo = student.MotherWhatsappNo,
                    address = student.StudentAddress,
                    city = student.StudentCity,
                    district = student.StudentDistrict,
                    state = student.StudentState
                };

                return Json(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting student details for ID: {id}");
                return BadRequest();
            }
        }
    }

    // Simple request model for delete
    public class DeleteRequest
    {
        public int Id { get; set; }
    }
}
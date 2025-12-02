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
    public class TeacherListController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TeacherListController> _logger;

        public TeacherListController(AppDbContext context, ILogger<TeacherListController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: TeacherList/Index
        [HttpGet]
        public async Task<IActionResult> Index(int? schoolId, int pageNumber = 1, string searchTerm = "")
        {
            try
            {
                int pageSize = 10;
                var query = _context.Teachers
                    .Include(t => t.School)
                    .AsQueryable();

                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(t => t.SchoolId == schoolId);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(t =>
                        t.TeacherName.Contains(searchTerm) ||
                        t.EmailId.Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

                var teachers = await query
                    .OrderByDescending(t => t.TeacherId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var teacherIds = teachers.Select(t => t.TeacherId).ToList();
                var schoolUsers = await _context.SchoolUsers
                    .Where(su => teacherIds.Contains(su.TeacherId ?? 0))
                    .ToDictionaryAsync(su => su.TeacherId ?? 0);

                var schools = await _context.Schools
                    .OrderBy(s => s.SchoolName)
                    .ToListAsync();

                var viewModel = new TeacherListViewModel
                {
                    Schools = schools,
                    SelectedSchoolId = schoolId ?? 0,
                    SearchTerm = searchTerm,
                    Teachers = new List<TeacherListItemViewModel>()
                };

                int srNo = ((pageNumber - 1) * pageSize) + 1;
                foreach (var teacher in teachers)
                {
                    var username = schoolUsers.ContainsKey(teacher.TeacherId)
                        ? schoolUsers[teacher.TeacherId].Username
                        : "";

                    viewModel.Teachers.Add(new TeacherListItemViewModel
                    {
                        SrNo = srNo++,
                        TeacherId = teacher.TeacherId,
                        TeacherName = teacher.TeacherName,
                        Username = username,
                        School = teacher.School?.SchoolName ?? "",
                        DOB = teacher.DateOfBirth?.ToString("dd/MM/yyyy") ?? "",
                        Email = teacher.EmailId ?? "",
                        TeacherMobileNo = teacher.TeacherMobileNo?.ToString() ?? "",
                        TeacherWhatsappNo = teacher.TeacherWhatsappNo?.ToString() ?? "",
                        Address = teacher.TeacherAddress ?? "",
                        City = teacher.TeacherCity ?? "",
                        District = teacher.TeacherDistrict ?? "",
                        State = teacher.TeacherState ?? ""
                    });
                }

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageSize = pageSize;

                _logger.LogInformation($"TeacherList: Retrieved {teachers.Count} teachers for page {pageNumber}, SchoolId: {schoolId}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher list");
                TempData["ErrorMessage"] = "Error loading teacher list.";
                return View(new TeacherListViewModel { Schools = new List<School>() });
            }
        }

        // POST: TeacherList/UpdateTeacher
        [HttpPost]
        public async Task<IActionResult> UpdateTeacher([FromBody] Teacher teacher)
        {
            try
            {
                if (teacher == null || teacher.TeacherId <= 0)
                {
                    return Json(new { success = false, message = "Invalid teacher data." });
                }

                var existingTeacher = await _context.Teachers.FindAsync(teacher.TeacherId);
                if (existingTeacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found." });
                }

                // Update only allowed fields
                existingTeacher.TeacherName = teacher.TeacherName;
                existingTeacher.DateOfBirth = teacher.DateOfBirth;
                existingTeacher.EmailId = teacher.EmailId;
                existingTeacher.TeacherAddress = teacher.TeacherAddress;
                existingTeacher.TeacherCity = teacher.TeacherCity;
                existingTeacher.TeacherDistrict = teacher.TeacherDistrict;
                existingTeacher.TeacherState = teacher.TeacherState;
                existingTeacher.TeacherMobileNo = teacher.TeacherMobileNo;
                existingTeacher.TeacherWhatsappNo = teacher.TeacherWhatsappNo;

                _context.Teachers.Update(existingTeacher);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Teacher '{teacher.TeacherName}' (ID: {teacher.TeacherId}) updated successfully");

                return Json(new { success = true, message = $"Teacher '{teacher.TeacherName}' has been updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating teacher");
                return Json(new { success = false, message = $"Error updating teacher: {ex.Message}" });
            }
        }

        // POST: TeacherList/DeleteTeacher
        [HttpPost]
        public async Task<IActionResult> DeleteTeacher([FromBody] DeleteRequest request)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(request.Id);
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found." });
                }

                var teacherName = teacher.TeacherName;

                // Delete SchoolUser(s) associated with this teacher
                var schoolUsers = await _context.SchoolUsers
                    .Where(su => su.TeacherId == request.Id)
                    .ToListAsync();

                foreach (var schoolUser in schoolUsers)
                {
                    _context.SchoolUsers.Remove(schoolUser);
                }

                // Delete the teacher
                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Teacher '{teacherName}' (ID: {request.Id}) and associated SchoolUser records deleted successfully");

                return Json(new { success = true, message = $"Teacher '{teacherName}' has been deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting teacher");
                return Json(new { success = false, message = "Error deleting teacher. Please try again." });
            }
        }

        // GET: TeacherList/ExportToExcel
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int? schoolId)
        {
            try
            {
                var query = _context.Teachers
                    .Include(t => t.School)
                    .AsQueryable();

                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(t => t.SchoolId == schoolId);
                }

                var teachers = await query.ToListAsync();

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Teachers");

                    worksheet.Cell(1, 1).Value = "Sr No.";
                    worksheet.Cell(1, 2).Value = "Teacher Name";
                    worksheet.Cell(1, 3).Value = "Username";
                    worksheet.Cell(1, 4).Value = "School";
                    worksheet.Cell(1, 5).Value = "DOB";
                    worksheet.Cell(1, 6).Value = "Email";
                    worksheet.Cell(1, 7).Value = "Address";
                    worksheet.Cell(1, 8).Value = "City";
                    worksheet.Cell(1, 9).Value = "District";
                    worksheet.Cell(1, 10).Value = "State";
                    worksheet.Cell(1, 11).Value = "Mobile No.";
                    worksheet.Cell(1, 12).Value = "WhatsApp No.";

                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                    for (int i = 0; i < teachers.Count; i++)
                    {
                        var teacher = teachers[i];
                        var schoolUser = await _context.SchoolUsers
                            .FirstOrDefaultAsync(su => su.TeacherId == teacher.TeacherId);

                        worksheet.Cell(i + 2, 1).Value = i + 1;
                        worksheet.Cell(i + 2, 2).Value = teacher.TeacherName;
                        worksheet.Cell(i + 2, 3).Value = schoolUser?.Username ?? "";
                        worksheet.Cell(i + 2, 4).Value = teacher.School?.SchoolName ?? "";
                        worksheet.Cell(i + 2, 5).Value = teacher.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
                        worksheet.Cell(i + 2, 6).Value = teacher.EmailId;
                        worksheet.Cell(i + 2, 7).Value = teacher.TeacherAddress;
                        worksheet.Cell(i + 2, 8).Value = teacher.TeacherCity;
                        worksheet.Cell(i + 2, 9).Value = teacher.TeacherDistrict;
                        worksheet.Cell(i + 2, 10).Value = teacher.TeacherState;
                        worksheet.Cell(i + 2, 11).Value = teacher.TeacherMobileNo;
                        worksheet.Cell(i + 2, 12).Value = teacher.TeacherWhatsappNo;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        _logger.LogInformation($"Exported {teachers.Count} teachers to Excel, SchoolId: {schoolId}");
                        return File(content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"Teachers_{DateTime.Now:ddMMyyyy}.xlsx");
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

        // GET: TeacherList/ExportToPdf
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(int? schoolId)
        {
            try
            {
                var query = _context.Teachers
                    .Include(t => t.School)
                    .AsQueryable();

                if (schoolId.HasValue && schoolId > 0)
                {
                    query = query.Where(t => t.SchoolId == schoolId);
                }

                var teachers = await query.ToListAsync();

                Document doc = new Document(PageSize.A4.Rotate());
                MemoryStream stream = new MemoryStream();
                PdfWriter.GetInstance(doc, stream);

                doc.Open();

                Font titleFont = new Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 16, Font.BOLD);
                Font headerFont = new Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 10, Font.BOLD);
                Font cellFont = new Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 10);

                Paragraph title = new Paragraph("Teacher List Report", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                doc.Add(title);

                Paragraph dateInfo = new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", cellFont);
                dateInfo.Alignment = Element.ALIGN_CENTER;
                doc.Add(dateInfo);

                doc.Add(new Paragraph("\n"));

                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5, 20, 12, 15, 10, 10, 12, 12 });

                string[] headers = { "Sr No.", "Teacher Name", "Username", "School", "Email", "Mobile", "DOB", "City" };
                foreach (var header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(200, 200, 200);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                for (int i = 0; i < teachers.Count; i++)
                {
                    var teacher = teachers[i];
                    var schoolUser = await _context.SchoolUsers
                        .FirstOrDefaultAsync(su => su.TeacherId == teacher.TeacherId);

                    table.AddCell(new PdfPCell(new Phrase((i + 1).ToString(), cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(teacher.TeacherName ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(schoolUser?.Username ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(teacher.School?.SchoolName ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(teacher.EmailId ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(teacher.TeacherMobileNo?.ToString() ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(teacher.DateOfBirth?.ToString("dd/MM/yyyy") ?? "", cellFont)));
                    table.AddCell(new PdfPCell(new Phrase(teacher.TeacherCity ?? "", cellFont)));
                }

                doc.Add(table);
                doc.Close();

                byte[] pdfContent = stream.ToArray();
                _logger.LogInformation($"Exported {teachers.Count} teachers to PDF, SchoolId: {schoolId}");

                return File(pdfContent, "application/pdf", $"Teachers_{DateTime.Now:ddMMyyyy}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                TempData["ErrorMessage"] = $"Error exporting to PDF: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: TeacherList/UpdateColumnVisibility
        [HttpPost]
        public IActionResult UpdateColumnVisibility(string columns)
        {
            try
            {
                if (!string.IsNullOrEmpty(columns))
                {
                    HttpContext.Session.SetString("VisibleColumnsTeacher", columns);
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

        // GET: TeacherList/GetTeacherDetails/{id}
        [HttpGet]
        public async Task<IActionResult> GetTeacherDetails(int id)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.School)
                    .FirstOrDefaultAsync(t => t.TeacherId == id);

                if (teacher == null)
                {
                    return NotFound();
                }

                var schoolUser = await _context.SchoolUsers
                    .FirstOrDefaultAsync(su => su.TeacherId == id);

                var details = new
                {
                    teacherId = teacher.TeacherId,
                    teacherName = teacher.TeacherName,
                    username = schoolUser?.Username ?? "",
                    school = teacher.School?.SchoolName ?? "",
                    dob = teacher.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                    email = teacher.EmailId,
                    address = teacher.TeacherAddress,
                    city = teacher.TeacherCity,
                    district = teacher.TeacherDistrict,
                    state = teacher.TeacherState,
                    teacherMobileNo = teacher.TeacherMobileNo,
                    teacherWhatsappNo = teacher.TeacherWhatsappNo
                };

                return Json(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting teacher details for ID: {id}");
                return BadRequest();
            }
        }
    }
}
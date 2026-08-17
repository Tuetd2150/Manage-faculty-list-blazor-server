using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClosedXML.Excel;
using Manage_faculty_list_task02.Models;

namespace Manage_faculty_list_task02.Services;

public sealed class ExcelService
{
    public const long MaxImportFileSize = 10 * 1024 * 1024;

    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public byte[] ExportFaculty(IEnumerable<FacultyMember> items,
        IReadOnlyList<LecturerCoefficient> coefficients,
        IReadOnlyList<AcademicRank> ranks, IReadOnlyList<Degree> degrees)
    {
        using XLWorkbook book = new();
        IXLWorksheet sheet = book.AddWorksheet("Giảng viên");
        string[] headers = ["STT", "Họ và tên", "Số điện thoại", "Ngày sinh", "Email",
            "Bậc lương", "Hệ số lương", "Mức lương", "Học hàm", "Học vị", "Trạng thái"];
        WriteHeaders(sheet, headers);
        int row = 2;
        foreach (FacultyMember item in items)
        {
            LecturerCoefficient? coefficient = coefficients.FirstOrDefault(x => x.Id == item.LecturerCoefficientId);
            object?[] values = [row - 1, item.FullName, item.PhoneNumber, item.DateOfBirth,
                item.Email, coefficient?.SalaryGrade, coefficient?.SalaryCoefficient,
                coefficient?.SalaryAmount, ranks.FirstOrDefault(x => x.Id == item.AcademicRankId)?.Name ?? "Không có",
                degrees.FirstOrDefault(x => x.Id == item.DegreeId)?.Name ?? "Không có", StatusText(item.ApprovalStatus)];
            WriteRow(sheet, row++, values);
        }
        sheet.Column(4).Style.DateFormat.Format = "dd/MM/yyyy";
        return Save(book, sheet);
    }

    public byte[] ExportCoefficients(IEnumerable<LecturerCoefficient> items)
    {
        using XLWorkbook book = new();
        IXLWorksheet sheet = book.AddWorksheet("Hệ số giảng viên");
        WriteHeaders(sheet, ["STT", "Bậc lương", "Hệ số lương", "Mức lương", "Trạng thái", "Mô tả"]);
        int row = 2;
        foreach (LecturerCoefficient item in items)
            WriteRow(sheet, row++, [row - 2, item.SalaryGrade, item.SalaryCoefficient,
                item.SalaryAmount, StatusText(item.ApprovalStatus), item.Description]);
        return Save(book, sheet);
    }

    public async Task<ImportResult<FacultyMember>> ImportFacultyAsync(
        Stream uploadStream,
        IReadOnlyList<LecturerCoefficient> coefficients,
        IReadOnlyList<AcademicRank> ranks, IReadOnlyList<Degree> degrees)
    {
        try
        {
            using MemoryStream memoryStream = new();
            await uploadStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            using XLWorkbook book = new(memoryStream);
            IXLWorksheet sheet = book.Worksheets.First();
            Dictionary<string, int> columns = Headers(sheet);
            string[] required = ["Họ và tên", "Số điện thoại", "Ngày sinh", "Email", "Học hàm", "Học vị", "Trạng thái"];
            List<string> errors = Missing(columns, required);
            if (!columns.ContainsKey("Bậc lương") && !columns.ContainsKey("Hệ số giảng viên"))
                errors.Add("Thiếu cột 'Bậc lương' hoặc 'Hệ số giảng viên'.");
            if (errors.Count > 0) return new([], errors);

            List<FacultyMember> result = [];
            HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);
            foreach (IXLRow row in sheet.RowsUsed().Skip(1))
            {
                int number = row.RowNumber();
                if (row.IsEmpty()) continue;
                LecturerCoefficient? coefficient = FindCoefficient(row, columns, coefficients);
                string rankName = Text(row, columns, "Học hàm");
                string degreeName = Text(row, columns, "Học vị");
                int? rankId = FindNamedId(rankName, ranks.Select(x => (x.Id, x.Name)));
                int? degreeId = FindNamedId(degreeName, degrees.Select(x => (x.Id, x.Name)));
                FacultyMember item = new()
                {
                    Id = result.Count + 1,
                    FullName = Text(row, columns, "Họ và tên"),
                    PhoneNumber = Text(row, columns, "Số điện thoại"),
                    DateOfBirth = ParseDate(row.Cell(columns["Ngày sinh"])),
                    Email = Text(row, columns, "Email"),
                    LecturerCoefficientId = coefficient?.Id ?? 0,
                    AcademicRankId = rankId,
                    DegreeId = degreeId,
                    ApprovalStatus = ParseStatus(Text(row, columns, "Trạng thái"), errors, number)
                };
                Validate(item, number, errors);
                if (coefficient is null) errors.Add($"Dòng {number}: Không tìm thấy hệ số giảng viên phù hợp.");
                if (!IsNone(rankName) && rankId is null) errors.Add($"Dòng {number}: Học hàm '{rankName}' không tồn tại.");
                if (!IsNone(degreeName) && degreeId is null) errors.Add($"Dòng {number}: Học vị '{degreeName}' không tồn tại.");
                if (!emails.Add(item.Email.Trim())) errors.Add($"Dòng {number}: Email bị trùng trong file.");
                result.Add(item);
            }
            if (result.Count == 0) errors.Add("File không có dòng dữ liệu.");
            return new(result, errors);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new([], [$"Không thể đọc file Excel: {ex.Message}"]);
        }
    }

    public async Task<ImportResult<LecturerCoefficient>> ImportCoefficientsAsync(
        Stream uploadStream)
    {
        try
        {
            using MemoryStream memoryStream = new();
            await uploadStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            using XLWorkbook book = new(memoryStream);
            IXLWorksheet sheet = book.Worksheets.First();
            Dictionary<string, int> columns = Headers(sheet);
            List<string> errors = Missing(columns, ["Bậc lương", "Hệ số lương", "Mức lương", "Trạng thái", "Mô tả"]);
            if (errors.Count > 0) return new([], errors);
            List<LecturerCoefficient> result = [];
            HashSet<(int, decimal)> keys = [];
            foreach (IXLRow row in sheet.RowsUsed().Skip(1))
            {
                int number = row.RowNumber();
                if (row.IsEmpty()) continue;
                LecturerCoefficient item = new()
                {
                    Id = result.Count + 1,
                    SalaryGrade = ParseInt(Text(row, columns, "Bậc lương")),
                    SalaryCoefficient = ParseDecimal(row.Cell(columns["Hệ số lương"])),
                    SalaryAmount = ParseDecimal(row.Cell(columns["Mức lương"])),
                    ApprovalStatus = ParseStatus(Text(row, columns, "Trạng thái"), errors, number),
                    Description = Text(row, columns, "Mô tả")
                };
                Validate(item, number, errors);
                if (!keys.Add((item.SalaryGrade, item.SalaryCoefficient)))
                    errors.Add($"Dòng {number}: Bậc lương và hệ số lương bị trùng trong file.");
                result.Add(item);
            }
            if (result.Count == 0) errors.Add("File không có dòng dữ liệu.");
            return new(result, errors);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new([], [$"Không thể đọc file Excel: {ex.Message}"]);
        }
    }

    private static void Validate(object item, int row, List<string> errors)
    {
        List<ValidationResult> validation = [];
        Validator.TryValidateObject(item, new ValidationContext(item), validation, true);
        errors.AddRange(validation.Select(error => $"Dòng {row}: {error.ErrorMessage}"));
    }
    private static Dictionary<string, int> Headers(IXLWorksheet sheet) => sheet.FirstRowUsed()!.CellsUsed()
        .ToDictionary(cell => cell.GetString().Trim(), cell => cell.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
    private static List<string> Missing(Dictionary<string, int> columns, IEnumerable<string> required) => required
        .Where(name => !columns.ContainsKey(name)).Select(name => $"Thiếu cột '{name}'.").ToList();
    private static string Text(IXLRow row, Dictionary<string, int> columns, string name) => row.Cell(columns[name]).GetFormattedString().Trim();
    private static int ParseInt(string value) { string digits = new(value.Where(char.IsDigit).ToArray()); return int.TryParse(digits, out int result) ? result : 0; }
    private static decimal ParseDecimal(IXLCell cell) => cell.TryGetValue(out decimal value) ? value : decimal.TryParse(cell.GetString(), NumberStyles.Any, Vi, out value) ? value : 0;
    private static DateTime? ParseDate(IXLCell cell) => cell.TryGetValue(out DateTime value) ? value : DateTime.TryParseExact(cell.GetString().Trim(), ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], Vi, DateTimeStyles.None, out value) ? value : null;
    private static ApprovalStatus ParseStatus(string value, List<string> errors, int row)
    {
        if (value.Equals("Đã duyệt", StringComparison.OrdinalIgnoreCase)) return ApprovalStatus.Approved;
        if (value.Equals("Chưa duyệt", StringComparison.OrdinalIgnoreCase)) return ApprovalStatus.Pending;
        errors.Add($"Dòng {row}: Trạng thái phải là 'Chưa duyệt' hoặc 'Đã duyệt'."); return ApprovalStatus.Pending;
    }
    private static int? FindNamedId(string value, IEnumerable<(int Id, string Name)> options)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Không có", StringComparison.OrdinalIgnoreCase)) return null;
        return options.FirstOrDefault(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase)).Id is int id && id > 0 ? id : null;
    }
    private static bool IsNone(string value) => string.IsNullOrWhiteSpace(value)
        || value.Equals("Không có", StringComparison.OrdinalIgnoreCase);
    private static LecturerCoefficient? FindCoefficient(IXLRow row, Dictionary<string, int> columns, IReadOnlyList<LecturerCoefficient> options)
    {
        if (columns.TryGetValue("Bậc lương", out int gradeColumn))
        { int grade = ParseInt(row.Cell(gradeColumn).GetFormattedString()); return options.FirstOrDefault(x => x.SalaryGrade == grade); }
        string display = row.Cell(columns["Hệ số giảng viên"]).GetFormattedString();
        return options.FirstOrDefault(x => display.Contains(x.SalaryCoefficient.ToString("0.00", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));
    }
    private static void WriteHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
    { for (int i = 0; i < headers.Count; i++) sheet.Cell(1, i + 1).Value = headers[i]; sheet.Row(1).Style.Font.Bold = true; }
    private static void WriteRow(IXLWorksheet sheet, int row, IReadOnlyList<object?> values)
    { for (int i = 0; i < values.Count; i++) sheet.Cell(row, i + 1).Value = XLCellValue.FromObject(values[i]); }
    private static byte[] Save(XLWorkbook book, IXLWorksheet sheet)
    { sheet.ColumnsUsed().AdjustToContents(); using MemoryStream stream = new(); book.SaveAs(stream); return stream.ToArray(); }
    private static string StatusText(ApprovalStatus status) => status == ApprovalStatus.Approved ? "Đã duyệt" : "Chưa duyệt";
}

public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

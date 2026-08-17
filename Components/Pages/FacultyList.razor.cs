using Manage_faculty_list_task02.Components.Dialogs;
using Manage_faculty_list_task02.Models;
using Manage_faculty_list_task02.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text;

namespace Manage_faculty_list_task02.Components.Pages;

public partial class FacultyList
{
    [Inject]
    private FacultyService FacultyService { get; set; } = default!;

    [Inject]
    private LecturerCoefficientService CoefficientService { get; set; } = default!;

    [Inject] private ExcelService ExcelService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly List<SelectOption> statusOptions =
    [
        new("", "Tất cả trạng thái"),
        new("0", "Chưa duyệt"),
        new("1", "Đã duyệt")
    ];

    private readonly List<SelectOption> pageSizeOptions =
    [
        new("5", "5"),
        new("10", "10"),
        new("20", "20")
    ];

    private List<FacultyMember> allItems = [];
    private List<FacultyMember> filteredItems = [];
    private readonly PaginationState pagination = new() { ItemsPerPage = 10 };
    private GridSort<FacultyMember> lecturerCoefficientSort =>
        GridSort<FacultyMember>.ByAscending(item =>
            GetCoefficientSalaryGrade(item.LecturerCoefficientId));
    private string keywordInput = string.Empty;
    private string appliedKeyword = string.Empty;
    private SelectOption statusInput = new("", "Tất cả trạng thái");
    private SelectOption appliedStatus = new("", "Tất cả trạng thái");
    private SelectOption degreeInput = new("", "Tất cả học vị");
    private SelectOption appliedDegree = new("", "Tất cả học vị");
    private SelectOption selectedPageSize = new("10", "10");
    private IEnumerable<SelectOption> selectedStatusOptions = [];
    private IEnumerable<SelectOption> selectedDegreeOptions = [];
    private int pageSize = 10;
    private int currentPage = 1;
    private bool facultyDialogOpen;
    private bool confirmationDialogOpen;
    private FacultyDialog.FacultyDialogMode dialogMode;
    private FacultyMember selectedMember = new();
    private string confirmationTitle = string.Empty;
    private string confirmationMessage = string.Empty;
    private Action? pendingAction;
    private string? notification;
    private MessageIntent notificationIntent = MessageIntent.Success;

    private IReadOnlyList<SelectOption> DegreeOptions
    {
        get
        {
            return
            [
                new("", "Tất cả học vị"),
                .. FacultyService.Degrees.Select(degree =>
                    new SelectOption(degree.Id.ToString(), degree.Name))
            ];
        }
    }

    private int TotalPages
    {
        get
        {
            return Math.Max(
                1,
                (int)Math.Ceiling(filteredItems.Count / (double)pageSize));
        }
    }

    protected override void OnInitialized()
    {
        selectedStatusOptions = [statusOptions[0]];
        selectedDegreeOptions = [DegreeOptions[0]];
        ReloadData();
    }

    private void ReloadData()
    {
        allItems = FacultyService.GetAll().ToList();
        ApplyCurrentFilters();
    }

    private void ApplyFilters()
    {
        statusInput = selectedStatusOptions.FirstOrDefault() ?? statusOptions[0];
        degreeInput = selectedDegreeOptions.FirstOrDefault() ?? DegreeOptions[0];
        appliedKeyword = keywordInput.Trim();
        appliedStatus = statusInput;
        appliedDegree = degreeInput;
        currentPage = 1;
        ApplyCurrentFilters();
    }

    private void SearchStatusOptions(OptionsSearchEventArgs<SelectOption> args)
    {
        args.Items = statusOptions.Where(option =>
            option.Text.Contains(args.Text, StringComparison.OrdinalIgnoreCase));
    }

    private void SearchDegreeOptions(OptionsSearchEventArgs<SelectOption> args)
    {
        args.Items = DegreeOptions.Where(option =>
            option.Text.Contains(args.Text, StringComparison.OrdinalIgnoreCase));
    }

    private void HandleKeywordKeyUp(KeyboardEventArgs eventArgs)
    {
        if (eventArgs.Key == "Enter")
        {
            ApplyFilters();
        }
    }

    private void ApplyCurrentFilters()
    {
        IEnumerable<FacultyMember> query = allItems;

        if (!string.IsNullOrWhiteSpace(appliedKeyword))
        {
            query = query.Where(item => MatchesKeyword(item, appliedKeyword));
        }

        if (Enum.TryParse(appliedStatus.Value, out ApprovalStatus status))
        {
            query = query.Where(item => item.ApprovalStatus == status);
        }

        if (int.TryParse(appliedDegree.Value, out int degreeId))
        {
            query = query.Where(item => item.DegreeId == degreeId);
        }

        filteredItems = query.ToList();
        currentPage = Math.Min(currentPage, TotalPages);
        UpdatePage();
    }

    private static bool MatchesKeyword(FacultyMember member, string keyword)
    {
        return MatchesName(member.FullName, keyword)
            || MatchesPhone(member.PhoneNumber, keyword)
            || MatchesEmail(member.Email, keyword);
    }

    private static bool MatchesName(string fullName, string keyword)
    {
        string normalizedName = NormalizeSearchText(fullName);
        string normalizedKeyword = NormalizeSearchText(keyword);
        string[] keywordParts = normalizedKeyword.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        return keywordParts.Length > 0
            && keywordParts.All(part =>
                normalizedName.Contains(part, StringComparison.Ordinal));
    }

    private static bool MatchesPhone(string phoneNumber, string keyword)
    {
        if (!IsPhoneLikeKeyword(keyword))
        {
            return false;
        }

        string normalizedKeyword = DigitsOnly(keyword);

        return normalizedKeyword.Length > 0
            && DigitsOnly(phoneNumber).Contains(
                normalizedKeyword,
                StringComparison.Ordinal);
    }

    private static bool MatchesEmail(string email, string keyword)
    {
        return email.Contains(
            keyword.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSearchText(string value)
    {
        string decomposed = value
            .Trim()
            .ToLowerInvariant()
            .Replace('\u0111', 'd')
            .Normalize(NormalizationForm.FormD);
        StringBuilder normalized = new(decomposed.Length);
        bool previousWasWhitespace = false;

        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace && normalized.Length > 0)
                {
                    normalized.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            normalized.Append(character);
            previousWasWhitespace = false;
        }

        return normalized.ToString().TrimEnd();
    }

    private static string DigitsOnly(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static bool IsPhoneLikeKeyword(string keyword)
    {
        return !string.IsNullOrWhiteSpace(keyword)
            && keyword.Any(char.IsDigit)
            && keyword.All(character =>
                char.IsDigit(character)
                || char.IsWhiteSpace(character)
                || character is '+' or '-' or '.' or '(' or ')');
    }

    private void UpdatePage()
    {
        _ = pagination.SetCurrentPageIndexAsync(currentPage - 1);
    }

    private int GetRowNumber(FacultyMember member)
    {
        return filteredItems.IndexOf(member) + 1;
    }

    private string GetCoefficientDisplay(int coefficientId)
    {
        LecturerCoefficient? coefficient =
            CoefficientService.GetById(coefficientId);

        return coefficient is null
            ? "—"
            : $"Bậc {coefficient.SalaryGrade} – Hệ số {coefficient.SalaryCoefficient:0.00}";
    }

    private int GetCoefficientSalaryGrade(int coefficientId)
    {
        return CoefficientService.GetById(coefficientId)?.SalaryGrade
            ?? int.MaxValue;
    }

    private string GetAcademicRankDisplay(int? academicRankId)
    {
        return FacultyService.AcademicRanks
            .FirstOrDefault(item => item.Id == academicRankId)
            ?.Name
            ?? "Không có";
    }

    private string GetDegreeDisplay(int? degreeId)
    {
        return FacultyService.Degrees
            .FirstOrDefault(item => item.Id == degreeId)
            ?.Name
            ?? "Không có";
    }

    private void ResetFilters()
    {
        keywordInput = string.Empty;
        appliedKeyword = string.Empty;
        statusInput = statusOptions[0];
        appliedStatus = statusOptions[0];
        degreeInput = DegreeOptions[0];
        appliedDegree = DegreeOptions[0];
        selectedStatusOptions = [statusOptions[0]];
        selectedDegreeOptions = [DegreeOptions[0]];
        currentPage = 1;
        notification = null;
        ReloadData();
    }

    private async Task ChangePageSize()
    {
        pageSize = int.Parse(selectedPageSize.Value);
        currentPage = 1;
        await pagination.SetItemsPerPageAsync(pageSize);
        await pagination.SetCurrentPageIndexAsync(0);
    }

    private async Task GoToPreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            await pagination.SetCurrentPageIndexAsync(currentPage - 1);
        }
    }

    private async Task GoToNextPage()
    {
        if (currentPage < TotalPages)
        {
            currentPage++;
            await pagination.SetCurrentPageIndexAsync(currentPage - 1);
        }
    }

    private void OpenCreateDialog()
    {
        selectedMember = new FacultyMember
        {
            DateOfBirth = DateTime.Today.AddYears(-25),
            ApprovalStatus = ApprovalStatus.Pending
        };
        dialogMode = FacultyDialog.FacultyDialogMode.Create;
        facultyDialogOpen = true;
        notification = null;
    }

    private void OpenEditDialog(FacultyMember member)
    {
        selectedMember = member.Clone();
        dialogMode = FacultyDialog.FacultyDialogMode.Edit;
        facultyDialogOpen = true;
    }

    private void OpenDetailDialog(FacultyMember member)
    {
        selectedMember = member.Clone();
        dialogMode = FacultyDialog.FacultyDialogMode.Detail;
        facultyDialogOpen = true;
    }

    private void HandleRowClick(FluentDataGridRow<FacultyMember> row)
    {
        if (row.Item is not null)
        {
            OpenDetailDialog(row.Item);
        }
    }

    private void CloseFacultyDialog()
    {
        facultyDialogOpen = false;
    }

    private void SaveFacultyMember(FacultyMember member)
    {
        if (dialogMode == FacultyDialog.FacultyDialogMode.Create)
        {
            FacultyService.Add(member);
            notification = "Đã thêm giảng viên thành công.";
        }
        else
        {
            FacultyService.Update(member);
            notification = "Đã cập nhật giảng viên thành công.";
        }

        facultyDialogOpen = false;
        ReloadData();
    }

    private void AskToDelete(FacultyMember member)
    {
        OpenConfirmation(
            "Xóa giảng viên",
            $"Bạn có chắc chắn muốn xóa giảng viên “{member.FullName}”?",
            () =>
            {
                FacultyService.Delete(member.Id);
                notification = "Đã xóa giảng viên thành công.";
                ReloadData();
            });
    }

    private void AskToChangeApproval(FacultyMember member)
    {
        bool isApproving = member.ApprovalStatus == ApprovalStatus.Pending;
        string actionText = isApproving ? "duyệt" : "hủy duyệt";

        OpenConfirmation(
            isApproving ? "Duyệt giảng viên" : "Hủy duyệt giảng viên",
            $"Bạn có chắc chắn muốn {actionText} giảng viên “{member.FullName}”?",
            () =>
            {
                FacultyService.SetApprovalStatus(
                    member.Id,
                    isApproving
                        ? ApprovalStatus.Approved
                        : ApprovalStatus.Pending);
                notification = isApproving
                    ? "Đã duyệt giảng viên."
                    : "Đã hủy duyệt giảng viên.";
                ReloadData();
            });
    }

    private void OpenConfirmation(
        string title,
        string message,
        Action confirmedAction)
    {
        confirmationTitle = title;
        confirmationMessage = message;
        pendingAction = confirmedAction;
        confirmationDialogOpen = true;
    }

    private void ConfirmPendingAction()
    {
        confirmationDialogOpen = false;
        Action? confirmedAction = pendingAction;
        pendingAction = null;
        confirmedAction?.Invoke();
    }

    private void CloseConfirmation()
    {
        confirmationDialogOpen = false;
        pendingAction = null;
    }

    private async Task ExportExcel()
    {
        byte[] bytes = ExcelService.ExportFaculty(filteredItems, CoefficientService.GetAll(),
            FacultyService.AcademicRanks, FacultyService.Degrees);
        await JS.InvokeVoidAsync("excelDownload", $"danh-sach-giang-vien-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Convert.ToBase64String(bytes));
    }

    private async Task ImportExcel(InputFileChangeEventArgs args)
    {
        notification = null;
        if (!args.File.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ShowNotification("Vui lòng chọn file Excel định dạng .xlsx.", MessageIntent.Error);
            return;
        }
        if (args.File.Size > ExcelService.MaxImportFileSize)
        {
            ShowNotification("File Excel không được vượt quá 10 MB.", MessageIntent.Error);
            return;
        }
        await using Stream stream = args.File.OpenReadStream(
            ExcelService.MaxImportFileSize);
        ImportResult<FacultyMember> result = await ExcelService.ImportFacultyAsync(stream,
            CoefficientService.GetAll(), FacultyService.AcademicRanks, FacultyService.Degrees);
        if (!result.IsValid)
        {
            ShowNotification(string.Join(" ", result.Errors), MessageIntent.Error);
            return;
        }
        (int added, int updated, int skipped) =
            FacultyService.UpsertImport(result.Items);
        ShowNotification(
            $"Import hoàn tất: thêm {added}, cập nhật {updated}, bỏ qua {skipped} giảng viên trùng khớp.");
        ReloadData();
    }

    private void ShowNotification(string message, MessageIntent intent = MessageIntent.Success)
    {
        notification = message;
        notificationIntent = intent;
    }
}

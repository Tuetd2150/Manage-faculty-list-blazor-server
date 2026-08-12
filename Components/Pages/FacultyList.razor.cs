using Manage_falcuty_list_task02.Components.Dialogs;
using Manage_falcuty_list_task02.Models;
using Manage_falcuty_list_task02.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Manage_falcuty_list_task02.Components.Pages;

public partial class FacultyList
{
    [Inject]
    private FacultyService FacultyService { get; set; } = default!;

    [Inject]
    private LecturerCoefficientService CoefficientService { get; set; } = default!;

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
    private List<FacultyMember> pagedItems = [];
    private string keywordInput = string.Empty;
    private string appliedKeyword = string.Empty;
    private SelectOption statusInput = new("", "Tất cả trạng thái");
    private SelectOption appliedStatus = new("", "Tất cả trạng thái");
    private SelectOption degreeInput = new("", "Tất cả học vị");
    private SelectOption appliedDegree = new("", "Tất cả học vị");
    private SelectOption selectedPageSize = new("10", "10");
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
        ReloadData();
    }

    private void ReloadData()
    {
        allItems = FacultyService.GetAll().ToList();
        ApplyCurrentFilters();
    }

    private Task ApplyFiltersAsync()
    {
        appliedKeyword = keywordInput.Trim();
        appliedStatus = statusInput;
        appliedDegree = degreeInput;
        currentPage = 1;
        ApplyCurrentFilters();

        return Task.CompletedTask;
    }

    private async Task HandleKeywordKeyUp(KeyboardEventArgs eventArgs)
    {
        if (eventArgs.Key == "Enter")
        {
            await ApplyFiltersAsync();
        }
    }

    private void ApplyCurrentFilters()
    {
        IEnumerable<FacultyMember> query = allItems;

        if (!string.IsNullOrWhiteSpace(appliedKeyword))
        {
            query = query.Where(item =>
                item.FullName.Contains(
                    appliedKeyword,
                    StringComparison.OrdinalIgnoreCase)
                || item.PhoneNumber.Contains(
                    appliedKeyword,
                    StringComparison.OrdinalIgnoreCase)
                || item.Email.Contains(
                    appliedKeyword,
                    StringComparison.OrdinalIgnoreCase));
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

    private void UpdatePage()
    {
        pagedItems = filteredItems
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private int GetRowNumber(FacultyMember member)
    {
        return (currentPage - 1) * pageSize
            + pagedItems.IndexOf(member)
            + 1;
    }

    private string GetCoefficientDisplay(int coefficientId)
    {
        LecturerCoefficient? coefficient =
            CoefficientService.GetById(coefficientId);

        return coefficient is null
            ? "—"
            : $"Bậc {coefficient.SalaryGrade} – Hệ số {coefficient.SalaryCoefficient:0.00}";
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
        currentPage = 1;
        notification = null;
        ReloadData();
    }

    private void ChangePageSize()
    {
        pageSize = int.Parse(selectedPageSize.Value);
        currentPage = 1;
        UpdatePage();
    }

    private void GoToPreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void GoToNextPage()
    {
        if (currentPage < TotalPages)
        {
            currentPage++;
            UpdatePage();
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
}

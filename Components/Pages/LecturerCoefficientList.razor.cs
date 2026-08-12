using System.Globalization;
using Manage_faculty_list_task02.Components.Dialogs;
using Manage_faculty_list_task02.Models;
using Manage_faculty_list_task02.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Manage_faculty_list_task02.Components.Pages;

public partial class LecturerCoefficientList
{
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

    private List<LecturerCoefficient> allItems = [];
    private List<LecturerCoefficient> filteredItems = [];
    private List<LecturerCoefficient> pagedItems = [];
    private string keywordInput = string.Empty;
    private string appliedKeyword = string.Empty;
    private SelectOption statusInput = new("", "Tất cả trạng thái");
    private SelectOption appliedStatus = new("", "Tất cả trạng thái");
    private SelectOption selectedPageSize = new("10", "10");
    private int pageSize = 10;
    private int currentPage = 1;
    private bool coefficientDialogOpen;
    private bool confirmationDialogOpen;
    private LecturerCoefficientDialog.DialogMode dialogMode;
    private LecturerCoefficient selectedCoefficient = new();
    private string confirmationTitle = string.Empty;
    private string confirmationMessage = string.Empty;
    private Action? pendingAction;
    private string? notification;
    private MessageIntent notificationIntent = MessageIntent.Success;

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
        allItems = CoefficientService.GetAll().ToList();
        ApplyCurrentFilters();
    }

    private Task ApplyFiltersAsync()
    {
        appliedKeyword = keywordInput.Trim();
        appliedStatus = statusInput;
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
        IEnumerable<LecturerCoefficient> query = allItems;

        if (!string.IsNullOrWhiteSpace(appliedKeyword))
        {
            query = query.Where(item =>
                item.SalaryGrade.ToString(CultureInfo.CurrentCulture)
                    .Contains(appliedKeyword, StringComparison.OrdinalIgnoreCase)
                || item.SalaryCoefficient.ToString(CultureInfo.CurrentCulture)
                    .Contains(appliedKeyword, StringComparison.OrdinalIgnoreCase)
                || item.SalaryAmount.ToString(CultureInfo.CurrentCulture)
                    .Contains(appliedKeyword, StringComparison.OrdinalIgnoreCase));
        }

        string? statusValue = appliedStatus?.Value;

        if (!string.IsNullOrWhiteSpace(statusValue)
            && Enum.TryParse(statusValue, out ApprovalStatus status))
        {
            query = query.Where(item => item.ApprovalStatus == status);
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

    private int GetRowNumber(LecturerCoefficient coefficient)
    {
        return (currentPage - 1) * pageSize
            + pagedItems.IndexOf(coefficient)
            + 1;
    }

    private void ResetFilters()
    {
        keywordInput = string.Empty;
        appliedKeyword = string.Empty;
        statusInput = statusOptions[0];
        appliedStatus = statusOptions[0];
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
        selectedCoefficient = new LecturerCoefficient
        {
            ApprovalStatus = ApprovalStatus.Pending
        };
        dialogMode = LecturerCoefficientDialog.DialogMode.Create;
        coefficientDialogOpen = true;
        notification = null;
    }

    private void OpenEditDialog(LecturerCoefficient coefficient)
    {
        selectedCoefficient = coefficient.Clone();
        dialogMode = LecturerCoefficientDialog.DialogMode.Edit;
        coefficientDialogOpen = true;
        notification = null;
    }

    private void OpenDetailDialog(LecturerCoefficient coefficient)
    {
        selectedCoefficient = coefficient.Clone();
        dialogMode = LecturerCoefficientDialog.DialogMode.Detail;
        coefficientDialogOpen = true;
    }

    private void CloseCoefficientDialog()
    {
        coefficientDialogOpen = false;
    }

    private void SaveCoefficient(LecturerCoefficient coefficient)
    {
        if (dialogMode == LecturerCoefficientDialog.DialogMode.Create)
        {
            CoefficientService.Add(coefficient);
            ShowNotification("Đã thêm hệ số thành công.");
        }
        else
        {
            CoefficientService.Update(coefficient);
            ShowNotification("Đã cập nhật hệ số thành công.");
        }

        coefficientDialogOpen = false;
        ReloadData();
    }

    private void AskToDelete(LecturerCoefficient coefficient)
    {
        int usageCount =
            CoefficientService.CountFacultyUsingCoefficient(coefficient.Id);

        if (usageCount > 0)
        {
            ShowNotification(
                $"Không thể xóa hệ số này vì đang được sử dụng bởi {usageCount} giảng viên.",
                MessageIntent.Error);
            return;
        }

        OpenConfirmation(
            "Xóa hệ số",
            $"Bạn có chắc chắn muốn xóa hệ số bậc {coefficient.SalaryGrade}?",
            () =>
            {
                CoefficientService.Delete(coefficient.Id);
                ShowNotification("Đã xóa hệ số thành công.");
                ReloadData();
            });
    }

    private void AskToChangeApproval(LecturerCoefficient coefficient)
    {
        bool isApproving =
            coefficient.ApprovalStatus == ApprovalStatus.Pending;
        string actionText = isApproving ? "duyệt" : "hủy duyệt";

        OpenConfirmation(
            isApproving ? "Duyệt hệ số" : "Hủy duyệt hệ số",
            $"Bạn có chắc chắn muốn {actionText} hệ số bậc {coefficient.SalaryGrade}?",
            () =>
            {
                CoefficientService.SetApprovalStatus(
                    coefficient.Id,
                    isApproving
                        ? ApprovalStatus.Approved
                        : ApprovalStatus.Pending);
                ShowNotification(
                    isApproving
                        ? "Đã duyệt hệ số."
                        : "Đã hủy duyệt hệ số.");
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

    private void ShowNotification(
        string message,
        MessageIntent intent = MessageIntent.Success)
    {
        notification = message;
        notificationIntent = intent;
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString(
            "N0",
            CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    }

}

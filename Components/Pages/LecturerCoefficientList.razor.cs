using System.Globalization;
using Manage_faculty_list_task02.Components.Dialogs;
using Manage_faculty_list_task02.Models;
using Manage_faculty_list_task02.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Manage_faculty_list_task02.Components.Pages;

public partial class LecturerCoefficientList
{
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

    private List<LecturerCoefficient> allItems = [];
    private List<LecturerCoefficient> filteredItems = [];
    private readonly PaginationState pagination = new() { ItemsPerPage = 10 };
    private readonly GridSort<LecturerCoefficient> salaryGradeSort =
        GridSort<LecturerCoefficient>.ByAscending(item => item.SalaryGrade);
    private readonly GridSort<LecturerCoefficient> salaryCoefficientSort =
        GridSort<LecturerCoefficient>.ByAscending(item => item.SalaryCoefficient);
    private readonly GridSort<LecturerCoefficient> salaryAmountSort =
        GridSort<LecturerCoefficient>.ByAscending(item => item.SalaryAmount);
    private string keywordInput = string.Empty;
    private string appliedKeyword = string.Empty;
    private SelectOption statusInput = new("", "Tất cả trạng thái");
    private SelectOption appliedStatus = new("", "Tất cả trạng thái");
    private SelectOption selectedPageSize = new("10", "10");
    private IEnumerable<SelectOption> selectedStatusOptions = [];
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
        selectedStatusOptions = [statusOptions[0]];
        ReloadData();
    }

    private void ReloadData()
    {
        allItems = CoefficientService.GetAll().ToList();
        ApplyCurrentFilters();
    }

    private Task ApplyFiltersAsync()
    {
        statusInput = selectedStatusOptions.FirstOrDefault() ?? statusOptions[0];
        appliedKeyword = keywordInput.Trim();
        appliedStatus = statusInput;
        currentPage = 1;
        ApplyCurrentFilters();

        return Task.CompletedTask;
    }

    private void SearchStatusOptions(OptionsSearchEventArgs<SelectOption> args)
    {
        args.Items = statusOptions.Where(option =>
            option.Text.Contains(args.Text, StringComparison.OrdinalIgnoreCase));
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
        _ = pagination.SetCurrentPageIndexAsync(currentPage - 1);
    }

    private int GetRowNumber(LecturerCoefficient coefficient)
    {
        return filteredItems.IndexOf(coefficient) + 1;
    }

    private void ResetFilters()
    {
        keywordInput = string.Empty;
        appliedKeyword = string.Empty;
        statusInput = statusOptions[0];
        appliedStatus = statusOptions[0];
        selectedStatusOptions = [statusOptions[0]];
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

    private void HandleRowClick(FluentDataGridRow<LecturerCoefficient> row)
    {
        if (row.Item is not null)
        {
            OpenDetailDialog(row.Item);
        }
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

    private async Task ExportExcel()
    {
        byte[] bytes = ExcelService.ExportCoefficients(filteredItems);
        await JS.InvokeVoidAsync("excelDownload", $"he-so-giang-vien-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
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
        ImportResult<LecturerCoefficient> result =
            await ExcelService.ImportCoefficientsAsync(stream);
        if (!result.IsValid)
        {
            ShowNotification(string.Join(" ", result.Errors), MessageIntent.Error);
            return;
        }
        (int added, int updated, int skipped) =
            CoefficientService.UpsertImport(result.Items);
        ShowNotification(
            $"Import hoàn tất: thêm {added}, cập nhật {updated}, bỏ qua {skipped} hệ số trùng khớp.");
        ReloadData();
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString(
            "N0",
            CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    }

}

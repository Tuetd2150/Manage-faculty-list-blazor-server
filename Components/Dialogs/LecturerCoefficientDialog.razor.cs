using System.Globalization;
using Manage_falcuty_list_task02.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Manage_falcuty_list_task02.Components.Dialogs;

public partial class LecturerCoefficientDialog
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public DialogMode Mode { get; set; }

    [Parameter, EditorRequired]
    public LecturerCoefficient Model { get; set; } = new();

    [Parameter, EditorRequired]
    public IReadOnlyList<LecturerCoefficient> SalaryGradeOptions { get; set; } =
        [];

    [Parameter]
    public Func<int, decimal, int?, bool>? IsDuplicate { get; set; }

    [Parameter]
    public EventCallback<LecturerCoefficient> OnSave { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private readonly ApprovalStatus[] statuses =
        Enum.GetValues<ApprovalStatus>();
    private LecturerCoefficient editModel = new();
    private int initializedId = -1;
    private DialogMode initializedMode;
    private string? duplicateError;
    private string salaryCoefficientInput = string.Empty;
    private string? coefficientError;

    private IEnumerable<int> SalaryGrades =>
        SalaryGradeOptions.Select(item => item.SalaryGrade);

    private string Title => Mode switch
    {
        DialogMode.Create => "Thêm hệ số giảng viên",
        DialogMode.Edit => "Cập nhật hệ số giảng viên",
        _ => "Chi tiết hệ số giảng viên"
    };

    protected override void OnParametersSet()
    {
        bool needsInitialization =
            IsOpen
            && (initializedId != Model.Id || initializedMode != Mode);

        if (needsInitialization)
        {
            editModel = Model.Clone();

            if (Mode == DialogMode.Create)
            {
                LecturerCoefficient? firstOption =
                    SalaryGradeOptions.FirstOrDefault();

                if (firstOption is not null)
                {
                    editModel.SalaryGrade = firstOption.SalaryGrade;
                    editModel.SalaryCoefficient =
                        firstOption.SalaryCoefficient;
                    editModel.SalaryAmount = firstOption.SalaryAmount;
                }
            }

            salaryCoefficientInput = editModel.SalaryCoefficient.ToString(
                "0.00",
                CultureInfo.InvariantCulture);
            initializedId = Model.Id;
            initializedMode = Mode;
            duplicateError = null;
            coefficientError = null;
        }

        if (!IsOpen)
        {
            initializedId = -1;
        }
    }

    private void UpdateCoefficientForGrade()
    {
        LecturerCoefficient? gradeDefault =
            SalaryGradeOptions.FirstOrDefault(
                item => item.SalaryGrade == editModel.SalaryGrade);

        if (gradeDefault is not null)
        {
            editModel.SalaryCoefficient =
                gradeDefault.SalaryCoefficient;
            editModel.SalaryAmount = gradeDefault.SalaryAmount;
            salaryCoefficientInput =
                gradeDefault.SalaryCoefficient.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
            coefficientError = null;
        }
    }

    private void UpdateSalaryFromCoefficientInput()
    {
        if (TryParseSalaryCoefficient(out decimal coefficient))
        {
            editModel.SalaryCoefficient = coefficient;
            editModel.SalaryAmount = decimal.Round(2_340_000m * coefficient, 0);
            coefficientError = null;
        }
    }

    private async Task SaveAsync()
    {
        if (!TryParseSalaryCoefficient(out decimal coefficient))
        {
            coefficientError =
                "Hệ số lương phải là số thực dương và có tối đa 2 chữ số thập phân.";
            return;
        }

        coefficientError = null;
        editModel.SalaryCoefficient = coefficient;
        editModel.SalaryAmount = decimal.Round(2_340_000m * coefficient, 0);
        int? excludedId = editModel.Id == 0 ? null : editModel.Id;

        if (IsDuplicate?.Invoke(
                editModel.SalaryGrade,
                editModel.SalaryCoefficient,
                excludedId) == true)
        {
            duplicateError =
                "Bậc lương và hệ số lương này đã tồn tại.";
            return;
        }

        duplicateError = null;
        editModel.Description = editModel.Description?.Trim();
        await OnSave.InvokeAsync(editModel.Clone());
    }

    private bool TryParseSalaryCoefficient(out decimal coefficient)
    {
        coefficient = 0;
        string normalizedValue = salaryCoefficientInput
            .Trim()
            .Replace(',', '.');

        int decimalSeparatorIndex = normalizedValue.IndexOf('.');
        bool hasAtMostTwoDecimalPlaces =
            decimalSeparatorIndex < 0
            || normalizedValue.Length - decimalSeparatorIndex - 1 <= 2;

        decimal parsedCoefficient = 0;
        bool isValid = hasAtMostTwoDecimalPlaces
            && decimal.TryParse(
                normalizedValue,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out parsedCoefficient)
            && parsedCoefficient > 0
            && parsedCoefficient <= 999;

        if (isValid)
        {
            coefficient = parsedCoefficient;
        }

        return isValid;
    }

    private Task CancelAsync()
    {
        return OnCancel.InvokeAsync();
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString(
            "N0",
            CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    }

    private static string GetStatusText(ApprovalStatus status)
    {
        return status == ApprovalStatus.Approved
            ? "Đã duyệt"
            : "Chưa duyệt";
    }

    public enum DialogMode
    {
        Create,
        Edit,
        Detail
    }
}

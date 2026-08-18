using System.Globalization;
using Manage_faculty_list_task02.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Manage_faculty_list_task02.Components.Dialogs;

public partial class FacultyDialog
{
    private static readonly CultureInfo VietnameseCulture =
        CultureInfo.GetCultureInfo("vi-VN");

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public FacultyDialogMode Mode { get; set; }

    [Parameter, EditorRequired]
    public FacultyMember Model { get; set; } = new();

    [Parameter]
    public IReadOnlyList<LecturerCoefficient> LecturerCoefficients { get; set; } = [];

    [Parameter]
    public IReadOnlyList<AcademicRank> AcademicRanks { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Degree> Degrees { get; set; } = [];

    [Parameter]
    public Func<string, int?, bool>? EmailExists { get; set; }

    [Parameter]
    public EventCallback<FacultyMember> OnSave { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private readonly AcademicRank noAcademicRank =
        new() { Id = 0, Name = "Không có" };
    private readonly Degree noDegree =
        new() { Id = 0, Name = "Không có" };
    private FacultyMember editModel = new();
    private LecturerCoefficient? selectedCoefficient;
    private AcademicRank? selectedAcademicRank;
    private Degree? selectedDegree;
    private IEnumerable<LecturerCoefficient> selectedCoefficientOptions = [];
    private IEnumerable<AcademicRank> selectedAcademicRankOptions = [];
    private IEnumerable<Degree> selectedDegreeOptions = [];
    private int initializedId = -1;
    private FacultyDialogMode initializedMode;
    private string? emailError;

    private bool IsReadOnly => Mode == FacultyDialogMode.Detail;

    private IEnumerable<LecturerCoefficient> SelectableCoefficients
    {
        get
        {
            return LecturerCoefficients.Where(
                item => item.ApprovalStatus == ApprovalStatus.Approved);
        }
    }

    private IEnumerable<AcademicRank> AcademicRankOptions =>
        [noAcademicRank, .. AcademicRanks];

    private IEnumerable<Degree> DegreeOptions =>
        [noDegree, .. Degrees];

    private string AcademicRankDisplay =>
        selectedAcademicRank?.Id > 0
            ? selectedAcademicRank.Name
            : "Không có";

    private string DegreeDisplay =>
        selectedDegree?.Id > 0
            ? selectedDegree.Name
            : "Không có";

    private string Title => Mode switch
    {
        FacultyDialogMode.Create => "Thêm mới giảng viên",
        FacultyDialogMode.Edit => "Cập nhật giảng viên",
        _ => "Chi tiết giảng viên"
    };

    protected override void OnParametersSet()
    {
        bool needsInitialization =
            IsOpen
            && (initializedId != Model.Id || initializedMode != Mode);

        if (needsInitialization)
        {
            editModel = Model.Clone();
            selectedCoefficient = LecturerCoefficients.FirstOrDefault(
                item => item.Id == editModel.LecturerCoefficientId);
            selectedAcademicRank = AcademicRanks.FirstOrDefault(
                item => item.Id == editModel.AcademicRankId) ?? noAcademicRank;
            selectedDegree = Degrees.FirstOrDefault(
                item => item.Id == editModel.DegreeId) ?? noDegree;
            selectedCoefficientOptions = selectedCoefficient is null
                ? []
                : [selectedCoefficient];
            selectedAcademicRankOptions = [selectedAcademicRank];
            selectedDegreeOptions = [selectedDegree];
            initializedId = Model.Id;
            initializedMode = Mode;
            emailError = null;
        }

        if (!IsOpen)
        {
            initializedId = -1;
        }
    }

    private void SynchronizeCoefficient()
    {
        selectedCoefficient = selectedCoefficientOptions.FirstOrDefault();
        editModel.LecturerCoefficientId = selectedCoefficient?.Id ?? 0;
    }

    private void SynchronizeAcademicRank()
    {
        selectedAcademicRank = selectedAcademicRankOptions.FirstOrDefault();
        editModel.AcademicRankId =
            selectedAcademicRank?.Id > 0
                ? selectedAcademicRank.Id
                : null;
    }

    private void SynchronizeDegree()
    {
        selectedDegree = selectedDegreeOptions.FirstOrDefault();
        editModel.DegreeId =
            selectedDegree?.Id > 0
                ? selectedDegree.Id
                : null;
    }

    private void SearchCoefficientOptions(
        OptionsSearchEventArgs<LecturerCoefficient> args)
    {
        string searchText = args.Text ?? string.Empty;
        args.Items = SelectableCoefficients.Where(item =>
            $"Bậc {item.SalaryGrade} – Hệ số {item.SalaryCoefficient:0.00}"
                .Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private void SearchAcademicRankOptions(OptionsSearchEventArgs<AcademicRank> args)
    {
        string searchText = args.Text ?? string.Empty;
        args.Items = AcademicRankOptions.Where(item =>
            item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private void SearchDegreeOptions(OptionsSearchEventArgs<Degree> args)
    {
        string searchText = args.Text ?? string.Empty;
        args.Items = DegreeOptions.Where(item =>
            item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SaveAsync()
    {
        editModel.FullName = editModel.FullName.Trim();
        editModel.PhoneNumber = editModel.PhoneNumber.Trim();
        editModel.Email = editModel.Email.Trim();

        bool coefficientIsSelectable =
            selectedCoefficient is not null
            && selectedCoefficient.ApprovalStatus == ApprovalStatus.Approved;

        if (!coefficientIsSelectable)
        {
            editModel.LecturerCoefficientId = 0;
            return;
        }

        int? excludedId = editModel.Id == 0 ? null : editModel.Id;

        if (EmailExists?.Invoke(editModel.Email, excludedId) == true)
        {
            emailError = "Email đã tồn tại trong danh sách.";
            return;
        }

        emailError = null;
        await OnSave.InvokeAsync(editModel.Clone());
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

    public enum FacultyDialogMode
    {
        Create,
        Edit,
        Detail
    }
}

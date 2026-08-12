using System.ComponentModel.DataAnnotations;

namespace Manage_faculty_list_task02.Models;

public sealed class LecturerCoefficient : IValidatableObject
{
    public int Id { get; set; }

    [Range(1, 20, ErrorMessage = "Bậc lương phải từ 1 đến 20.")]
    public int SalaryGrade { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "999",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Hệ số lương phải lớn hơn 0.")]
    public decimal SalaryCoefficient { get; set; }

    [Range(
        typeof(decimal),
        "1",
        "999999999999",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Mức lương phải là số nguyên dương.")]
    public decimal SalaryAmount { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; }

    [StringLength(
        500,
        ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public string? Description { get; set; }

    public LecturerCoefficient Clone()
    {
        return (LecturerCoefficient)MemberwiseClone();
    }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (decimal.Round(SalaryCoefficient, 2) != SalaryCoefficient)
        {
            yield return new ValidationResult(
                "Hệ số lương chỉ được có tối đa 2 chữ số thập phân.",
                [nameof(SalaryCoefficient)]);
        }

        if (decimal.Truncate(SalaryAmount) != SalaryAmount)
        {
            yield return new ValidationResult(
                "Mức lương phải là số nguyên dương.",
                [nameof(SalaryAmount)]);
        }
    }
}

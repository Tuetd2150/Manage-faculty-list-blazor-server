using System.ComponentModel.DataAnnotations;

namespace Manage_faculty_list_task02.Models;

public sealed class FacultyMember : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [RegularExpression(@"^\d{9,11}$", ErrorMessage = "Số điện thoại phải gồm 9 đến 11 chữ số.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh không được để trống.")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hệ số giảng viên.")]
    public int LecturerCoefficientId { get; set; }

    public int? AcademicRankId { get; set; }
    public int? DegreeId { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }

    public FacultyMember Clone()
    {
        return (FacultyMember)MemberwiseClone();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.HasValue && DateOfBirth.Value.Date >= DateTime.Today)
        {
            yield return new ValidationResult("Ngày sinh phải nhỏ hơn ngày hiện tại.", [nameof(DateOfBirth)]);
        }
    }
}

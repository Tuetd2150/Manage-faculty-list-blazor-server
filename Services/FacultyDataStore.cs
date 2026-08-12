using Manage_faculty_list_task02.Models;

namespace Manage_faculty_list_task02.Services;

public sealed class FacultyDataStore
{
    public FacultyDataStore()
    {
        SalaryGradeDefaults = CreateLecturerCoefficients();
        LecturerCoefficients = SalaryGradeDefaults
            .Take(14)
            .Select(item => item.Clone())
            .ToList();
        FacultyMembers = CreateFacultyMembers(LecturerCoefficients);
        AcademicRanks =
        [
            new() { Id = 1, Name = "Phó giáo sư" },
            new() { Id = 2, Name = "Giáo sư" }
        ];
        Degrees =
        [
            new() { Id = 1, Name = "Cử nhân" },
            new() { Id = 2, Name = "Thạc sĩ" },
            new() { Id = 3, Name = "Tiến sĩ" }
        ];
    }

    public IReadOnlyList<LecturerCoefficient> SalaryGradeDefaults { get; }

    internal List<FacultyMember> FacultyMembers { get; }

    internal List<LecturerCoefficient> LecturerCoefficients { get; }

    public IReadOnlyList<AcademicRank> AcademicRanks { get; }

    public IReadOnlyList<Degree> Degrees { get; }

    private static List<LecturerCoefficient> CreateLecturerCoefficients()
    {
        return Enumerable.Range(1, 20)
            .Select(index => new LecturerCoefficient
            {
                Id = index,
                SalaryGrade = index,
                SalaryCoefficient = 2.10m + index * 0.23m,
                SalaryAmount = decimal.Round(
                    2_340_000m * (2.10m + index * 0.23m),
                    0),
                ApprovalStatus = index % 5 == 0
                    ? ApprovalStatus.Pending
                    : ApprovalStatus.Approved,
                Description = $"Hệ số áp dụng cho bậc lương {index}."
            })
            .ToList();
    }

    private static List<FacultyMember> CreateFacultyMembers(
        IReadOnlyCollection<LecturerCoefficient> coefficients)
    {
        string[] familyNames =
        [
            "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng",
            "Vũ", "Đặng", "Bùi", "Đỗ", "Hồ"
        ];
        string[] givenNames =
        [
            "Minh Anh", "Quang Huy", "Thu Hà", "Đức Long", "Ngọc Mai",
            "Gia Bảo", "Thanh Tâm", "Hoài Nam", "Khánh Linh", "Tuấn Kiệt"
        ];
        int[] approvedIds = coefficients
            .Where(item => item.ApprovalStatus == ApprovalStatus.Approved)
            .Select(item => item.Id)
            .ToArray();

        return Enumerable.Range(1, 36)
            .Select(index => new FacultyMember
            {
                Id = index,
                FullName = $"{familyNames[(index - 1) % familyNames.Length]} {givenNames[((index - 1) / familyNames.Length + ((index - 1) % familyNames.Length) * 3) % givenNames.Length]}",
                PhoneNumber = $"09{index:00000000}",
                DateOfBirth = new DateTime(
                    1970 + index % 28,
                    index % 12 + 1,
                    index % 25 + 1),
                Email = $"giangvien{index:00}@example.edu.vn",
                LecturerCoefficientId = approvedIds[(index - 1) % approvedIds.Length],
                AcademicRankId = index % 5 == 0 ? null : index % 2 + 1,
                DegreeId = index % 4 == 0 ? null : index % 3 + 1,
                ApprovalStatus = index % 3 == 0
                    ? ApprovalStatus.Pending
                    : ApprovalStatus.Approved
            })
            .ToList();
    }
}

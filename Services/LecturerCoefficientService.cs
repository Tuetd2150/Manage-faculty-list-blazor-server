using Manage_faculty_list_task02.Models;

namespace Manage_faculty_list_task02.Services;

public sealed class LecturerCoefficientService(FacultyDataStore dataStore)
{
    public IReadOnlyList<LecturerCoefficient> GetSalaryGradeDefaults()
    {
        return dataStore.SalaryGradeDefaults
            .Select(item => item.Clone())
            .ToList();
    }

    public IReadOnlyList<LecturerCoefficient> GetAll()
    {
        return dataStore.LecturerCoefficients
            .Select(item => item.Clone())
            .ToList();
    }

    public LecturerCoefficient? GetById(int id)
    {
        return dataStore.LecturerCoefficients
            .FirstOrDefault(item => item.Id == id)
            ?.Clone();
    }

    public bool CheckDuplicate(
        int salaryGrade,
        decimal salaryCoefficient,
        int? excludingId = null)
    {
        return dataStore.LecturerCoefficients.Any(item =>
            item.Id != excludingId
            && item.SalaryGrade == salaryGrade
            && item.SalaryCoefficient == salaryCoefficient);
    }

    public LecturerCoefficient Add(LecturerCoefficient coefficient)
    {
        LecturerCoefficient addedCoefficient = coefficient.Clone();
        addedCoefficient.Id = GetNextId();
        dataStore.LecturerCoefficients.Add(addedCoefficient);

        return addedCoefficient.Clone();
    }

    public void Update(LecturerCoefficient coefficient)
    {
        int index = dataStore.LecturerCoefficients
            .FindIndex(item => item.Id == coefficient.Id);

        if (index >= 0)
        {
            dataStore.LecturerCoefficients[index] = coefficient.Clone();
        }
    }

    public bool Delete(int id)
    {
        if (IsInUse(id))
        {
            return false;
        }

        return dataStore.LecturerCoefficients.RemoveAll(item => item.Id == id) > 0;
    }

    public void SetApprovalStatus(int id, ApprovalStatus status)
    {
        LecturerCoefficient? coefficient = dataStore.LecturerCoefficients
            .FirstOrDefault(item => item.Id == id);

        if (coefficient is not null)
        {
            coefficient.ApprovalStatus = status;
        }
    }

    private bool IsInUse(int id)
    {
        return CountFacultyUsingCoefficient(id) > 0;
    }

    public int CountFacultyUsingCoefficient(int id)
    {
        return dataStore.FacultyMembers.Count(
            member => member.LecturerCoefficientId == id);
    }

    private int GetNextId()
    {
        return dataStore.LecturerCoefficients
            .Select(item => item.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }
}

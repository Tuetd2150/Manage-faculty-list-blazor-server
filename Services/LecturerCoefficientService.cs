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

    public void ReplaceAll(IEnumerable<LecturerCoefficient> coefficients)
    {
        dataStore.LecturerCoefficients.Clear();
        dataStore.LecturerCoefficients.AddRange(
            coefficients.Select(item => item.Clone()));
    }

    public (int Added, int Updated, int Skipped) UpsertImport(
        IEnumerable<LecturerCoefficient> coefficients)
    {
        int added = 0;
        int updated = 0;
        int skipped = 0;

        foreach (LecturerCoefficient imported in coefficients)
        {
            int existingIndex = dataStore.LecturerCoefficients.FindIndex(item =>
                item.SalaryGrade == imported.SalaryGrade
                && item.SalaryCoefficient == imported.SalaryCoefficient);

            if (existingIndex < 0)
            {
                LecturerCoefficient newCoefficient = imported.Clone();
                newCoefficient.Id = GetNextId();
                dataStore.LecturerCoefficients.Add(newCoefficient);
                added++;
                continue;
            }

            LecturerCoefficient existing =
                dataStore.LecturerCoefficients[existingIndex];

            if (HasSameImportData(existing, imported))
            {
                skipped++;
                continue;
            }

            LecturerCoefficient updatedCoefficient = imported.Clone();
            updatedCoefficient.Id = existing.Id;
            dataStore.LecturerCoefficients[existingIndex] = updatedCoefficient;
            updated++;
        }

        return (added, updated, skipped);
    }

    private static bool HasSameImportData(
        LecturerCoefficient existing,
        LecturerCoefficient imported)
    {
        return existing.SalaryGrade == imported.SalaryGrade
            && existing.SalaryCoefficient == imported.SalaryCoefficient
            && existing.SalaryAmount == imported.SalaryAmount
            && existing.ApprovalStatus == imported.ApprovalStatus
            && string.Equals(
                existing.Description?.Trim() ?? string.Empty,
                imported.Description?.Trim() ?? string.Empty,
                StringComparison.Ordinal);
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

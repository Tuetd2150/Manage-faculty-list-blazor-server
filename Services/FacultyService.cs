using Manage_faculty_list_task02.Models;

namespace Manage_faculty_list_task02.Services;

public sealed class FacultyService(FacultyDataStore dataStore)
{
    public IReadOnlyList<AcademicRank> AcademicRanks => dataStore.AcademicRanks;

    public IReadOnlyList<Degree> Degrees => dataStore.Degrees;

    public IReadOnlyList<FacultyMember> GetAll()
    {
        return dataStore.FacultyMembers
            .Select(item => item.Clone())
            .ToList();
    }

    public bool CheckDuplicateEmail(string email, int? excludingId = null)
    {
        return dataStore.FacultyMembers.Any(item =>
            item.Id != excludingId
            && string.Equals(
                item.Email.Trim(),
                email.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public FacultyMember Add(FacultyMember member)
    {
        FacultyMember addedMember = member.Clone();
        addedMember.Id = GetNextId();
        addedMember.ApprovalStatus = ApprovalStatus.Pending;
        dataStore.FacultyMembers.Add(addedMember);

        return addedMember.Clone();
    }

    public void Update(FacultyMember member)
    {
        int index = dataStore.FacultyMembers.FindIndex(item => item.Id == member.Id);

        if (index >= 0)
        {
            dataStore.FacultyMembers[index] = member.Clone();
        }
    }

    public void Delete(int id)
    {
        dataStore.FacultyMembers.RemoveAll(item => item.Id == id);
    }

    public void SetApprovalStatus(int id, ApprovalStatus status)
    {
        FacultyMember? member = dataStore.FacultyMembers
            .FirstOrDefault(item => item.Id == id);

        if (member is not null)
        {
            member.ApprovalStatus = status;
        }
    }

    public void ReplaceAll(IEnumerable<FacultyMember> members)
    {
        dataStore.FacultyMembers.Clear();
        dataStore.FacultyMembers.AddRange(members.Select(item => item.Clone()));
    }

    public (int Added, int Updated, int Skipped) UpsertImport(
        IEnumerable<FacultyMember> members)
    {
        int added = 0;
        int updated = 0;
        int skipped = 0;

        foreach (FacultyMember imported in members)
        {
            int existingIndex = dataStore.FacultyMembers.FindIndex(item =>
                string.Equals(
                    item.Email.Trim(),
                    imported.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (existingIndex < 0)
            {
                FacultyMember newMember = imported.Clone();
                newMember.Id = GetNextId();
                dataStore.FacultyMembers.Add(newMember);
                added++;
                continue;
            }

            FacultyMember existing = dataStore.FacultyMembers[existingIndex];

            if (HasSameImportData(existing, imported))
            {
                skipped++;
                continue;
            }

            FacultyMember updatedMember = imported.Clone();
            updatedMember.Id = existing.Id;
            dataStore.FacultyMembers[existingIndex] = updatedMember;
            updated++;
        }

        return (added, updated, skipped);
    }

    private static bool HasSameImportData(
        FacultyMember existing,
        FacultyMember imported)
    {
        return string.Equals(existing.FullName.Trim(), imported.FullName.Trim(), StringComparison.Ordinal)
            && string.Equals(existing.PhoneNumber.Trim(), imported.PhoneNumber.Trim(), StringComparison.Ordinal)
            && existing.DateOfBirth?.Date == imported.DateOfBirth?.Date
            && string.Equals(existing.Email.Trim(), imported.Email.Trim(), StringComparison.OrdinalIgnoreCase)
            && existing.LecturerCoefficientId == imported.LecturerCoefficientId
            && existing.AcademicRankId == imported.AcademicRankId
            && existing.DegreeId == imported.DegreeId
            && existing.ApprovalStatus == imported.ApprovalStatus;
    }

    private int GetNextId()
    {
        return dataStore.FacultyMembers
            .Select(item => item.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }
}

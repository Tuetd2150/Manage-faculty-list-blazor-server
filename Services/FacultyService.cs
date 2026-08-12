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

    private int GetNextId()
    {
        return dataStore.FacultyMembers
            .Select(item => item.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }
}

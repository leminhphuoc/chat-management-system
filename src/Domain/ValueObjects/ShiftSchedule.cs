using ChatSupportSystem.Domain.Common;
using ChatSupportSystem.Domain.Enums;

namespace ChatSupportSystem.Domain.ValueObjects;

public class ShiftSchedule : ValueObject
{
    public ShiftType ShiftType { get; private set; }

    private ShiftSchedule()
    { }

    public ShiftSchedule(ShiftType shiftType)
    {
        ShiftType = shiftType;
    }

    public bool IsWithinShiftHours(TimeSpan currentTime)
    {
        return ShiftType switch
        {
            ShiftType.Morning => currentTime >= TimeSpan.FromHours(6) && currentTime < TimeSpan.FromHours(14),
            ShiftType.Afternoon => currentTime >= TimeSpan.FromHours(14) && currentTime < TimeSpan.FromHours(22),
            ShiftType.Night => currentTime >= TimeSpan.FromHours(22) || currentTime < TimeSpan.FromHours(6),
            _ => throw new ArgumentOutOfRangeException(nameof(ShiftType), "Invalid shift type")
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ShiftType;
    }
}
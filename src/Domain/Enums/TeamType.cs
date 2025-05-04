namespace ChatSupportSystem.Domain.Enums;

public enum TeamType
{
    TeamA = 0,     // Day shift team with mixed seniority
    TeamB = 1,     // Day shift team with mixed seniority
    TeamC = 2,     // Night shift team
    Overflow = 3   // Overflow team for high volume periods
}
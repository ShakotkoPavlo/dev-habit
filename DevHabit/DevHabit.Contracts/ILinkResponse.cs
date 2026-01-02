using DevHabit.Contracts.Habits;

namespace DevHabit.Contracts;

public interface ILinkResponse
{
    List<Link> Links { get; set; }
}

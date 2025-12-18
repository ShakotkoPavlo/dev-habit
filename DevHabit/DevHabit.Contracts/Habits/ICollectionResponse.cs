namespace DevHabit.Contracts.Habits;

public interface ICollectionResponse<T>
{
    List<T> Items { get; init; }
}

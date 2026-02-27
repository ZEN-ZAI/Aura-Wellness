namespace AuraWellness.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

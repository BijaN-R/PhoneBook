namespace PhoneBook.Application.Abstractions;

public interface IPhoneBookDataLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(CancellationToken ct = default);
}

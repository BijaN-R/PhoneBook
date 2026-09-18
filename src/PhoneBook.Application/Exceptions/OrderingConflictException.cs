namespace PhoneBook.Application.Exceptions;

public sealed class OrderingConflictException : Exception
{
    public OrderingConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

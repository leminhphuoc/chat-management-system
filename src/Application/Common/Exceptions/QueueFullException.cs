namespace ChatSupportSystem.Domain.Exceptions;

public class QueueFullException : Exception
{
    public QueueFullException() : base("Queue is full")
    {
    }

    public QueueFullException(string message) : base(message)
    {
    }

    public QueueFullException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
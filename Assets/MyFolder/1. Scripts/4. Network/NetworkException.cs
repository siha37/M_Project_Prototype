using System;

public class NetworkException : Exception
{
    public NetworkException(string message) : base(message) { }
    public NetworkException(string message, Exception innerException) : base(message, innerException) { }
}

public enum NetworkErrorType
{
    ConnectionTimeout,
    ServerUnreachable,
    InvalidResponse,
    JsonParseError,
    Unknown
}

public class NetworkError
{
    public NetworkErrorType type;
    public string message;
    public int errorCode;
    
    public NetworkError(NetworkErrorType type, string message, int errorCode = 0)
    {
        this.type = type;
        this.message = message;
        this.errorCode = errorCode;
    }
} 
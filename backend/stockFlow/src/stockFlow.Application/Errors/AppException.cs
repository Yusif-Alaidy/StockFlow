using System;
using System.Collections.Generic;

namespace stockFlow.Application.Common.Errors;

public enum ErrorKind
{
    Validation, Unauthorized, Forbidden, NotFound, Conflict,
    PreconditionRequired, PreconditionFailed
}

// Only deliberately safe, client-facing messages belong here.
// Ordinary ArgumentException/InvalidOperationException remain server errors.
public sealed class AppException : Exception
{
    public ErrorKind Kind { get; }
    public string Code { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public AppException(ErrorKind kind, string code, string message,
        IReadOnlyDictionary<string, string[]>? errors = null) : base(message)
    {
        Kind = kind;
        Code = code;
        Errors = errors;
    }
}

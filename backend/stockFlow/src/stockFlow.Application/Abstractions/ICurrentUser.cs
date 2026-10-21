using System;
using System.Threading;
using System.Threading.Tasks;

namespace stockFlow.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}

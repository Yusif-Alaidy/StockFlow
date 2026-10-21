using System;
using System.Threading;
using System.Threading.Tasks;

namespace stockFlow.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}

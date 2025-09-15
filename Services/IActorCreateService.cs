namespace DANCustomTools.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
public interface IActorCreateService
{
    Task StartAsync(string[] arguments, CancellationToken cancellationToken = default);
}

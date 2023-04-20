﻿using System.Threading;
using System.Threading.Tasks;

namespace ajiva.Ecs;

public interface IUpdateManager
{
    void RegisterUpdate(IUpdate update);
    void UnRegisterUpdate(IUpdate update);
    void RegisterUpdateForAllInContainer();
    void Run();
    Task Wait(CancellationToken cancellation);
    Task Stop();
}
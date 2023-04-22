using Autofac;

namespace Ajiva.Ecs;

public interface IContainerAccessor
{
    IContainer Container { get; set; }
}

using System;

namespace ajiva.Ecs.Utils;

[AttributeUsage(AttributeTargets.Class)]
public class DependentAttribute : Attribute
{
    public readonly Type[] Dependent;

    public DependentAttribute(params Type[] type)
    {
        Dependent = type;
    }
}
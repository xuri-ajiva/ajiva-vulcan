// See https://aka.ms/new-console-template for more information
using ajiva.Test;

Console.WriteLine("Hello, World!");
var entity = new TestEntity();

Console.WriteLine(entity.Get<Component1>().Value);
Console.WriteLine(entity.HasComponent<Component1>());
foreach (var c in entity.GetComponents())
{
    Console.WriteLine(c?.GetType().ToString() ?? "null");
}
foreach (var c in entity.GetComponentTypes())
{
    Console.WriteLine(c.Name);
}

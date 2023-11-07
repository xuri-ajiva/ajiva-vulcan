# Ajiva Vulcan Engine [![CodeQL](https://github.com/xuri02/ajiva/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/xuri02/ajiva/actions/workflows/codeql-analysis.yml) [![.NET](https://github.com/xuri02/ajiva/actions/workflows/dotnet.yml/badge.svg)](https://github.com/xuri02/ajiva/actions/workflows/dotnet.yml)

Ajiva is a game engine that leverages Vulcan for rendering graphics. It is written in C# and uses PInvoke to call native functions.

## Features

### Entity Component System (ECS)

Ajiva utilizes a custom Event-based Entity Component System (ECS) to manage interactions between various systems. In this event-based approach, the Transform component triggers an event upon modification. Using the IChangingObserver, the rendering system can respond to these events and update the Uniform Buffer accordingly.

### Dependency Injection

The engine has transitioned to using [Autofac](https://github.com/autofac/Autofac) for Dependency Injection. In previous versions, the ECS was used for dependency injection, but this approach proved overly complex and challenging to maintain lifetimes for dependencies, which are necessary for later scene loading and unloading.

### Entities

Entities are generated using a [Source Generator](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) (specifically, [Ajiva.Generator](src/Ajiva.Generator)). To create an entity, a class is tagged with the [EntityComponent] attribute, and the generator parses the class for all components as parameters.

```csharp
[EntityComponent(typeof(Transform3d))]
public sealed partial class FpsCamera : IUpdate, IDisposable { ... }
```

The generator generates a partial class implementing the IEntity interface, adding the components as properties. Additionally, it generates a factory class for each entity, offering a fluent interface for configuring the entity.

<details>
  <summary>Code</summary>

```csharp
public partial class FpsCamera : IEntity
{
    public Guid Id { get; } = Guid.NewGuid();
    public Transform3d Transform3d { get; protected set; }
    public bool TryGetComponent<TComponent>([MaybeNullWhen(false)] out TComponent value) where TComponent : IComponent { ... }
    public bool HasComponent<TComponent>() where TComponent : IComponent { ... }
    public TComponent Get<TComponent>() where TComponent : IComponent { ... }
    public FpsCamera Configure<TComponent>(Action<TComponent> configuration) where TComponent : IComponent { ... }
    public IEnumerable<IComponent> GetComponents() { ... }
    public IEnumerable<Type> GetComponentTypes() { ... }
    protected FpsCamera() {}
    internal static FpsCamera CreateEmpty() { return new(); }
}

public ref struct Creator {
    public FpsCameraFactoryData FactoryData;
    public Transform3d? Transform3d;
    public FpsCamera Create() { ... }
    public FpsCamera Finalize() { ... }
    public FpsCamera.Creator With(Transform3d val) { Transform3d = val; return this; }
}
public partial record FpsCameraFactoryData(IComponentSystem<Transform3d> Transform3d, IEntityRegistry EntityRegistry) : FactoryData {
        public FpsCamera.Creator Begin() => new() { FactoryData = this };
}
```

</details>

Last of all the Generator will generate a Extension for Autofac to register Factory in DI.
The usage of the Factory is as follows:

```csharp
var factory = this.container.Resolve<EntityFactory>() // get universal factory
var cube = factory.CreateCube()
    .With(new Transform3d {
        Position = Vector3.Zero,
        Rotation = Vector3.Zero,
        Scale = Vector3.One
    })
    .Finalize() // creates the Entity with all Specified Components all not specified Components will be resolved using autofac from corresponding ComponentSystems
    .Configure<CollisionsComponent>(c => c.MeshId = MeshPrefab.Cube.MeshId)
    .Configure<PhysicsComponent>(p => p.IsStatic = true);
```

### Bounding Boxes / Spatial Partitioning

Bounding Boxes, a component, listens for updates to the Transform Component and updates the Bounding Box accordingly, utilizing a custom WorkerPool. The updated Bounding Boxes are stored in a Dynamic Octal Tree, used for Spatial Partitioning.

The Dynamic Octal Tree is a custom implementation of an Octal Tree, dynamically expanding when an entity's Bounding Box exceeds the current bounds of the tree. This minimizes the maximum depth of the tree while keeping all entities in leaf nodes beyond the maximum depth. The Dynamic Octal Tree supports a debug visualization, rendering debug boxes for each node.

<details>
  <summary>Image</summary>

![Octal Tree](img/SpatialPartitioning_Debug.png)
</details>

### Renderer

The renderer is quite extensive, supporting Layer Systems like `Ajiva3dLayerSystem` and `Ajiva2dLayerSystem` for 3D and 2D rendering layers, respectively. Current layers include `SolidMeshRenderLayer` and `DebugLayer` for 3D, and `Mesh2dRenderLayer` for 2D. The `AjivaLayerRenderer` blends the outputs of these layers, where color.w < 0.1.

3D rendering employs instancing to render multiple meshes at different positions in a single draw call.

![Instancing](img/Instancing.png)
<details>
  <summary>Debug</summary>

![Instancing_Debug](img/Instancing_Debug.png)
</details>


## Dependencies

[Dependency graph](https://github.com/xuri-ajiva/ajiva-vulcan/network/dependencies)

Within the [libs](libs) directory, you will find some customized libraries:

| name     | Description                                   | Author                                                |
| :------- | :-------------------------------------------- | :---------------------------------------------------- |
| SharpVk  | C#/.NET Bindings for the Vulkan API           | [Andrew Armstrong](https://github.com/FacticiusVir)   |
| Autofac  | Dependency Injection Container                | [Autofac](https://github.com/autofac/Autofac)         |
| Serilog  | Logging                                       | [Serilog](https://github.com/serilog/serilog)         |

## Credits

This core is inspired by or based on the work of:

- FacticiusVir/[SharpVk-Samples](https://github.com/FacticiusVir/SharpVk-Samples)
- Pilzschaf/[OpenGLTutorial](https://github.com/Pilzschaf/OpenGLTutorial)
- OneLoneCoder/[Javidx9](https://github.com/OneLoneCoder/Javidx9/blob/master/PixelGameEngine/SmallerProjects/OneLoneCoder_PGE_QuadTree1.cpp)
- [Vulkan Tutorial.com](https://vulkan-tutorial.com/)

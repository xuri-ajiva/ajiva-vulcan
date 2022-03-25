# Ajiva Vulcan Engine [![CodeQL](https://github.com/xuri02/ajiva/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/xuri02/ajiva/actions/workflows/codeql-analysis.yml) [![.NET](https://github.com/xuri02/ajiva/actions/workflows/dotnet.yml/badge.svg)](https://github.com/xuri02/ajiva/actions/workflows/dotnet.yml)

Ajiva is a Game Engine based using Vulcan to render graphics. Its written in c# and uses the PInvoke method to call
native methods.

The engine uses a custom Event based Entity Component System (ECS) to handle interactions between all system. Event
based means, that e.g the Transform component will fire and event on change, using the IChangingObserver, the render
system can react to this and update the Uniform Buffer.

## Dependencies 
[Dependency graph](https://github.com/xuri02/ajiva/network/dependencies)

Located in [libs](libs) are some adjusted libraries  

| name         | Description                                   | Author                                                 | License                                                             |
|:-------------|:----------------------------------------------|:-------------------------------------------------------|:--------------------------------------------------------------------|
| ajiva-utils  | a custom lib written by me                    | none # todo                                            | [xuri](https://github.com/xuri02)                                   |
| GlmSharp     | C#/.NET math library for vectors and matrices | [Philip Trettner](https://github.com/Philip-Trettner)  | [MIT License](https://github.com/xuri02/GlmSharp/blob/main/LICENSE) | 
| SharpVk      | C#/.NET Bindings for the Vulkan API           | [Andrew Armstrong](https://github.com/FacticiusVir)    | [MIT License](https://github.com/xuri02/SharpVk/blob/main/LICENSE)  |



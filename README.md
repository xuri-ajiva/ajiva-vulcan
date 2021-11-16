# Ajiva Vulcan Engine [![CodeQL](https://github.com/xuri02/ajiva/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/xuri02/ajiva/actions/workflows/codeql-analysis.yml) [![.NET](https://github.com/xuri02/ajiva/actions/workflows/dotnet.yml/badge.svg)](https://github.com/xuri02/ajiva/actions/workflows/dotnet.yml)

Ajiva is a Game Engine based using Vulcan to render graphics. Its written in c# and uses the PInvoke method to call native methods.

The engine uses a custom Event based Entity Component System (ECS) to handle interactions between all system. Event based means, that e.g the Transform component will fire and event on change, using the IChangingObserver, the render system can react to this and update the Uniform Buffer.

## Dependencies
Located in libs

info||ajiva-utils|GlmSharp|SharpVk|
|-|-|-|-|-|
description||a custom lib written by me|C#/.NET math library for vectors and matrices|C#/.NET Bindings for the Vulkan API|
License||none # todo|[MIT License](https://github.com/xuri02/GlmSharp/blob/bef0b608c123e131bd867cf16632c843442f2c2c/LICENSE)|[MIT License](https://github.com/xuri02/SharpVk/blob/570b33f4433400b156befaf5a17c8d6e5280c1e3/LICENSE)|
Author||[xuri](https://github.com/xuri02)|[Philip Trettner](https://github.com/Philip-Trettner)|[Andrew Armstrong](https://github.com/FacticiusVir)

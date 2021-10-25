﻿using ajiva.Ecs.Component;
using ajiva.Utils;

namespace ajiva.Components.Media
{
    public class TextureComponent : DisposingLogger, IComponent
    {
        public uint TextureId { get; set; }
    }
}
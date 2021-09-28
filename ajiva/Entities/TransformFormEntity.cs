using System;
using System.Collections.Generic;
using ajiva.Components.Media;
using ajiva.Components.Transform;
using ajiva.Ecs.Entity;

namespace ajiva.Entities
{
    public class TransformFormEntity<T, TV, TM> : ChangingObserverEntity where T : class, ITransform<TV, TM> where TV : struct, IReadOnlyList<float> where TM : struct, IReadOnlyList<float>
    {
        public TransformFormEntity() : base(0)
        {
            TransformLazy = new Lazy<T>(() => (this.TryGetComponent<T>(out var transform) ? transform : default)!);
        }
                                                        
        public Lazy<T> TransformLazy { get; }
        public T Transform => TransformLazy.Value;
    }
}

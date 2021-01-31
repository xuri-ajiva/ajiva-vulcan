using System;

namespace ajiva.Ecs
{
    public interface IUpdate
    {
        public void Update(TimeSpan delta);
    }
}
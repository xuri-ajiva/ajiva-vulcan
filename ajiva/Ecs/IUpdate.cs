using ajiva.Helpers;

namespace ajiva.Ecs
{
    public interface IUpdate
    {
        public void Update(UpdateInfo delta);
    }
}

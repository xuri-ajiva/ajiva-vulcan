namespace ajiva.Ecs
{
    public interface IInit
    {
        public void Init(AjivaEcs ecs, InitPhase phase);
    }

    public enum InitPhase
    {
        Start,
        PreInit,
        Init,
        PreMain,
        Main,
        PostMain,
        Post,
        Finish,
    }
}

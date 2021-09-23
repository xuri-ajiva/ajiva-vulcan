namespace ajiva.Utils
{
    public interface IComp<in T>
    {
        public bool CompareTo(T other);
    }
}

namespace Ajiva.Entities;

/*
public class TransformFormEntity<T, TV, TM> : ChangingObserverEntity where T : class, ITransform<TV, TM>, new() where TV : struct, IReadOnlyList<float> where TM : struct, IReadOnlyList<float>
{
    public TransformFormEntity() : base(0)
    {
        Transform = new T();
        //AddComponent<T, ITransform<TV, TM>>(Transform);
        AddComponent<T,T>(Transform);
    }

    public T Transform { get; set; }
}
*/

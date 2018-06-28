using System.Runtime.Serialization;

namespace MapSharpLib
{
    public interface IActor<T> where T : ISerializable
    {
        IActor<T> NewActor(IObjectPipe<T> op);
        void Act();
    }
}
using System.Runtime.Serialization;

namespace MapSharpLib
{
    public interface IObjectPipe<T> where T : ISerializable
    {
        T GetObject();
        void PushObject(string receiver, T datum);
    }
}
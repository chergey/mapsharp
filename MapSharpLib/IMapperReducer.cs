using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MapSharpLib
{
    public interface IMapperReducer
    {
        ISerializable MapFunction(ISerializable o);

        //This reduce function must be able to handle 2 types of inputs:
        //1) An IEnumerable container of values returned by the MapFunction
        //2) An IEnumerable container of values returned by other instances of ReduceFunction
        ISerializable ReduceFunction(IEnumerable<ISerializable> iA);
    }
}
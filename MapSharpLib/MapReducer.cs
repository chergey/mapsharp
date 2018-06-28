using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MapSharpLib
{
    public class MapReducer
    {
        public static ISerializable MapReduce(IMapperReducer mr, IEnumerable<ISerializable> inputs)
            => Reduce(mr, MultiMap(mr, inputs));

        private static ISerializable SingleMap(IMapperReducer mr, ISerializable a)
            => mr.MapFunction(a);

        private static IEnumerable<ISerializable> MultiMap(IMapperReducer mr, IEnumerable<ISerializable> a)
        {
            var mapped = new List<ISerializable>();
            foreach (ISerializable i in a)
            {
                mapped.Add(mr.MapFunction(i));
            }

            return mapped;
        }

        public static ISerializable Reduce(IMapperReducer mr, IEnumerable<ISerializable> a)
            => mr.ReduceFunction(a);
    }
}
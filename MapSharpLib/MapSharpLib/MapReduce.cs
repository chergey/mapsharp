using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

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

    public interface IMapperReducer
    {
        ISerializable MapFunction(ISerializable o);

        //This reduce function must be able to handle 2 types of inputs:
        //1) An IEnumerable container of values returned by the MapFunction
        //2) An IEnumerable container of values returned by other instances of ReduceFunction
        ISerializable ReduceFunction(IEnumerable<ISerializable> iA);
    }

    [Serializable]
    public class Wrapper<TV> : ISerializable
    {
        private TV _val;

        public Wrapper(TV value)
        {
            _val = value;
        }

        public TV Value
        {
            get => _val;
            set => _val = value;
        }

        #region Serialization stuff		

        protected Wrapper(SerializationInfo info, StreamingContext c)
        {
            _val = (TV) info.GetValue("Value", typeof(TV));
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Value", _val);
        }

        #endregion
    }
}
/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/3/2009
 * Time: 12:11 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
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
		{
			return Reduce(mr, MultiMap(mr, inputs));
		}			
		
		private static ISerializable SingleMap(IMapperReducer mr, ISerializable a)
		{
			return mr.MapFunction(a);
		}
		
		private static IEnumerable<ISerializable> MultiMap(IMapperReducer mr, IEnumerable<ISerializable> a)
		{
			List<ISerializable> mapped = new List<ISerializable>();
			foreach(ISerializable i in a)
			{
				mapped.Add(mr.MapFunction(i));
			}			
		    return mapped;
		}
		
		public static ISerializable Reduce(IMapperReducer mr, IEnumerable<ISerializable> a)
		{
			return mr.ReduceFunction(a);
		}
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
	public class Wrapper<V> : ISerializable
	{
		private V val;
		
		public Wrapper(V value)
		{
			val = value;
		}
		
		public V Value
		{
			get => val;
			set => val = value;
		}
		
		#region Serialization stuff		
		protected Wrapper (SerializationInfo info, StreamingContext c) 
        {
			val = (V)info.GetValue("Value", typeof(V));
        }
		
		[SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
        public virtual void GetObjectData (SerializationInfo info, StreamingContext context) 
        {
        	info.AddValue ("Value", val);
        }
        #endregion
	}
}
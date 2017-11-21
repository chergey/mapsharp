/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/3/2009
 * Time: 12:19 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace MapSharpLib
{
	[Serializable]
	public class Job : ISerializable
	{
		public string JobName;
		public byte[] Assembly;
		public string MapReducerClass;
		public IEnumerable<ISerializable> Inputs;
		public string Client;
		public ISerializable Results;
		public bool IsDone;
		private Dictionary<string, string> _otherAttributes;
		
		private Job()
		{}
		
		public Job(string jobName, string client, byte[] assembly, string mapReducerClass, IEnumerable<ISerializable> inputs)
		{
			JobName = jobName;
			Assembly = assembly;
			Client=client;
			MapReducerClass = mapReducerClass;
			Inputs = inputs;
			IsDone = false;
		}
		
		public void SetResults(ISerializable results)
		{
			Results = results;
			Inputs = null;
			IsDone = true;
		}
		
		public void SetAtt(string attName, string value)
		{
			if(_otherAttributes==null)
			{
				_otherAttributes = new Dictionary<string, string>();
			}
			_otherAttributes[attName]=value;
		}
		
		public string GetAtt(string attName)
		{
			
			if(_otherAttributes!=null && _otherAttributes.TryGetValue(attName, out var retVal))
			{
				return retVal;
			}
			return string.Empty;			
		}
		
		public Job Clone()
		{
			Job j = new Job
			{
				JobName = JobName,
				Assembly = Assembly,
				MapReducerClass = MapReducerClass,
				Inputs = Inputs,
				Client = Client,
				Results = Results,
				IsDone = IsDone,
				_otherAttributes = _otherAttributes
			};
			return j;
		}
		
		#region Serialization stuff		
		protected Job (SerializationInfo info, StreamingContext c) 
        {
			JobName = info.GetString("JobName");
			Assembly = (byte[])info.GetValue("Assembly", typeof(byte[]));
			Client = info.GetString("Client");
			MapReducerClass = info.GetString("MapReducerClass");
			IsDone = info.GetBoolean("isDone");
			_otherAttributes = (Dictionary<string, string>)info.GetValue("otherAttributes", typeof(Dictionary<string, string>));
			if(IsDone)
				Results = (ISerializable) info.GetValue ("Results", typeof(ISerializable));
			else
			{
				Inputs = (IEnumerable<ISerializable>) info.GetValue ("Inputs", typeof(IEnumerable<ISerializable>));
			}
        }
		
		[SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
        public virtual void GetObjectData (SerializationInfo info, StreamingContext context) 
        {
        	info.AddValue ("JobName", JobName);
			info.AddValue ("Assembly", Assembly);
			info.AddValue ("Client", Client);
			info.AddValue ("isDone", IsDone);
			info.AddValue ("MapReducerClass", MapReducerClass);
			info.AddValue ("otherAttributes", _otherAttributes);
			if(IsDone)
				info.AddValue ("Results", Results);
			else
			{
				info.AddValue("Inputs", Inputs);
			}
        }
        #endregion
	}
}

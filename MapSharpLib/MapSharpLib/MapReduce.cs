using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace MapSharpLib
{
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
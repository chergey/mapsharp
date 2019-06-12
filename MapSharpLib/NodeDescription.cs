using System.Collections.Generic;

namespace MapSharpLib
{
    public class NodeDescription
    {
        readonly string _ipAddress;
        readonly int _port;
        readonly List<string> _workingOn;

        public NodeDescription(string node, List<string> pendingWork)
        {
            string[] a = node.Split(':');
            _ipAddress = a[0];
            _port = int.Parse(a[1]);
            _workingOn = pendingWork ?? new List<string>();
        }

        public string Paddress => _ipAddress;

        public int Port => _port;

        public string[] PendingWork => _workingOn.ToArray();

        public override string ToString()
        {
            string retVal = "Node " + _ipAddress + ":" + _port + "-Working on ";
            foreach (string s in PendingWork)
                retVal += s + ";";
            return retVal;
        }
    }
}
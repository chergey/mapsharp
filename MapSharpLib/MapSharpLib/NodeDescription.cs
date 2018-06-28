using System.Collections.Generic;

namespace MapSharpLib
{
    public class NodeDescription
    {
        readonly string _ipaddress;
        readonly int _port;
        readonly List<string> _workingOn;

        public NodeDescription(string node, List<string> pendingWork)
        {
            string[] a = node.Split(':');
            _ipaddress = a[0];
            _port = int.Parse(a[1]);
            _workingOn = pendingWork ?? new List<string>();
        }

        public string Paddress => _ipaddress;

        public int Port => _port;

        public string[] PendingWork => _workingOn.ToArray();

        public override string ToString()
        {
            string retVal = "Node " + _ipaddress + ":" + _port + "-Working on ";
            foreach (string s in PendingWork)
                retVal += s + ";";
            return retVal;
        }
    }
}
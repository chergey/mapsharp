/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/8/2009
 * Time: 5:09 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using Communication;
using Communication.Network;

namespace MapSharpLib
{
    public class MrManager
    {
        readonly JobPusher _pusher;

        readonly Server<Job> _resultsReturn;
        readonly List<Job> _results;
        readonly string _managerIp;
        readonly int _managerPort;

        volatile int _status;

        readonly Dictionary<string, Job> _waitingOn;
        readonly List<NodeDescription> _nodeStats;
        readonly Dictionary<string, NodeDescription> _nodeStatsDic;
        readonly ReaderWriterLockSlim _nStatsLock;

        public MrManager(int managerPort)
        {
            //HACK: hard coded for testing.
            _managerIp = "127.0.0.1";

            _managerPort = managerPort;
            _pusher = new JobPusher();
            _waitingOn = new Dictionary<string, Job>();
            _results = new List<Job>();

            _nodeStatsDic = new Dictionary<string, NodeDescription>();
            _nodeStats = new List<NodeDescription>();
            _nStatsLock = new ReaderWriterLockSlim();

            _resultsReturn = new Server<Job>(managerPort, new MrmActor(_waitingOn, _nodeStats, _nodeStatsDic, _nStatsLock
                , _results, null));
        }

        public void Do(Job giantJob, int chunking)
        {
            _status = -1;

            string client = _managerIp + ":" + _managerPort;

            IEnumerator<ISerializable> ie = giantJob.Inputs.GetEnumerator();
            ie.MoveNext();
            bool keepGoing = true;

            for (int count = 0; keepGoing; count++)
            {
                List<ISerializable> buff = new List<ISerializable>();

                for (int i = 0; i < chunking && keepGoing; i++)
                {
                    buff.Add(ie.Current);
                    keepGoing = ie.MoveNext();
                }

                string jobname = giantJob.JobName + " " + count;
                Job j = giantJob.Clone();
                j.JobName = jobname;
                j.Client = client;
                j.Inputs = buff;
                Push(j);
            }
            _status = 0;
        }

        private void Push(Job j)
        {
            string node = _pusher.Push(j);

            #region add job to global waitingOn & node's pendingWork lists

            _nStatsLock.EnterWriteLock();

            try
            {
                _waitingOn.Add(j.JobName, j);
                NodeDescription nd = _nodeStatsDic[node];
                List<string> l = new List<string>(nd.PendingWork) {j.JobName};
                NodeDescription newNodeDesc = new NodeDescription(node, l);

                _nodeStatsDic[node] = newNodeDesc;
                _nodeStats.Remove(nd);
                _nodeStats.Add(newNodeDesc);
            }
            finally
            {
                _nStatsLock.ExitWriteLock();
            }

            #endregion
        }

        public bool IsDone() => (_status == 0 && _waitingOn.Count == 0);

        public void Join()
        {
            while (!IsDone())
            {
                Thread.Sleep(300);
            }
        }

        public void Stop()
        {
            _resultsReturn.Stop();
        }

        public IList<string> NodesList
        {
            get => _pusher.NodesList;
            set
            {
                UpdateNodeStats(value);
                _pusher.NodesList = value;
            }
        }

        private void UpdateNodeStats(IList<string> nodelist)
        {
            #region add job to global waitingOn & node's pendingWork lists

            _nStatsLock.EnterWriteLock();

            try
            {
                foreach (string k in _nodeStatsDic.Keys)
                {
                    if (!NodesList.Contains(k))
                    {
                        _nodeStats.Remove(_nodeStatsDic[k]);
                        _nodeStatsDic.Remove(k);
                    }
                }
                foreach (string n in nodelist)
                {
                    if (!_nodeStatsDic.ContainsKey(n))
                    {
                        NodeDescription nd = new NodeDescription(n, null);
                        _nodeStatsDic.Add(n, nd);
                        _nodeStats.Add(nd);
                    }
                }
            }
            finally
            {
                _nStatsLock.ExitWriteLock();
            }

            #endregion
        }

        public IList<NodeDescription> NodeStats
        {
            get
            {
                List<NodeDescription> retVal;

                _nStatsLock.EnterReadLock();

                try
                {
                    retVal = new List<NodeDescription>(_nodeStats);
                }
                finally
                {
                    _nStatsLock.ExitReadLock();
                }

                return retVal;
            }
        }

        private class JobPusher
        {
            readonly List<string> _nodesList;
            readonly ReaderWriterLockSlim _nListLock;
            readonly AsyncTransfer<Job> _aT;
            int _index;

            public IList<string> NodesList
            {
                get
                {
                    List<string> retVal;

                    _nListLock.EnterReadLock();

                    try
                    {
                        retVal = new List<string>(_nodesList);
                    }
                    finally
                    {
                        _nListLock.ExitReadLock();
                    }

                    return retVal;
                }
                set
                {
                    IList<string> v = value;

                    _nListLock.EnterWriteLock();

                    try
                    {
                        _nodesList.Clear();
                        _nodesList.AddRange(v);
                    }
                    finally
                    {
                        _nListLock.ExitWriteLock();
                    }
                }
            }

            public JobPusher()
            {
                _nodesList = new List<string>();
                _nListLock = new ReaderWriterLockSlim();
                _index = -1;
                _aT = new AsyncTransfer<Job>();
            }

            public string Push(Job j)
            {
                string node;

                #region get the right node to push to

                _nListLock.EnterReadLock();

                try
                {
                    _index = (_index + 1) % (_nodesList.Count);
                    node = _nodesList[_index];
                }
                finally
                {
                    _nListLock.ExitReadLock();
                }

                #endregion

                j.SetAtt("workNode", node);
                _aT.PushObject(node, j);

                return node;
            }
        }

        public List<Job> CurrentResults
        {
            get
            {
                List<Job> retVal;
                lock (_results)
                {
                    retVal = new List<Job>(_results);
                    _results.Clear();
                }
                return retVal;
            }
        }
    }

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
            if (pendingWork != null)
                _workingOn = pendingWork;
            else
                _workingOn = new List<string>();
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

    class MrmActor : IActor<Job>
    {
        readonly IObjectPipe<Job> _op;
        readonly Dictionary<string, Job> _waitOn;
        readonly List<Job> _results;
        readonly List<NodeDescription> _nStats;
        readonly Dictionary<string, NodeDescription> _nStatsDic;
        readonly ReaderWriterLockSlim _nStatsLock;

        public MrmActor(Dictionary<string, Job> waitOn,
            List<NodeDescription> nStats,
            Dictionary<string, NodeDescription> nStatsDic,
            ReaderWriterLockSlim nStatsLock,
            List<Job> results,
            IObjectPipe<Job> op)
        {
            _nStats = nStats;
            _nStatsDic = nStatsDic;
            _nStatsLock = nStatsLock;
            _op = op;
            _waitOn = waitOn;
            _results = results;
        }

        public IActor<Job> NewActor(IObjectPipe<Job> op)
        {
            return new MrmActor(_waitOn, _nStats, _nStatsDic, _nStatsLock,
                _results, op);
        }

        public void Act()
        {
            Job j = _op.GetObject();

            _nStatsLock.EnterWriteLock();
            try
            {
                _waitOn.Remove(j.JobName);
                string node = j.GetAtt("workNode");

                NodeDescription nd = _nStatsDic[node];

                List<string> l = new List<string>(nd.PendingWork);
                l.Remove(j.JobName);
                NodeDescription newNodeDesc = new NodeDescription(node, l);

                _nStatsDic[node] = newNodeDesc;
                _nStats.Remove(nd);
                _nStats.Add(newNodeDesc);
            }
            finally
            {
                _nStatsLock.ExitWriteLock();
            }

            lock (_results)
            {
                _results.Add(j);
            }
        }
    }
}
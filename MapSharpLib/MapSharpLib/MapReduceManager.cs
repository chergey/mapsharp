using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using MapSharpLib.Network;

namespace MapSharpLib
{
    public class MrManager
    {
        readonly JobPusher _pusher;

        private readonly Server<Job> _resultsReturn;
        private readonly List<Job> _results;
        private readonly string _managerIp;
        private readonly int _managerPort;

        private volatile int _status;

        private readonly Dictionary<string, Job> _waitingOn;
        private readonly List<NodeDescription> _nodeStats;
        private readonly Dictionary<string, NodeDescription> _nodeStatsDic;
        private readonly ReaderWriterLockSlim _nStatsLock;

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

            _resultsReturn = new Server<Job>(managerPort, new MrmActor(_waitingOn, _nodeStats, _nodeStatsDic,
                _nStatsLock
                , _results, null));
        }

        public void Do(Job giantJob, int chunking)
        {
            _status = -1;
            string client = _managerIp + ":" + _managerPort;
            var ie = giantJob.Inputs.GetEnumerator();
            ie.MoveNext();
            bool keepGoing = true;

            for (int count = 0; keepGoing; count++)
            {
                var buff = new List<ISerializable>();
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
            _nStatsLock.EnterWriteLock();

            try
            {
                _waitingOn.Add(j.JobName, j);
                var nd = _nodeStatsDic[node];
                var l = new List<string>(nd.PendingWork) {j.JobName};
                var newNodeDesc = new NodeDescription(node, l);

                _nodeStatsDic[node] = newNodeDesc;
                _nodeStats.Remove(nd);
                _nodeStats.Add(newNodeDesc);
            }
            finally
            {
                _nStatsLock.ExitWriteLock();
            }
        }

        public bool IsDone() => _status == 0 && _waitingOn.Count == 0;

        public void Join()
        {
            while (!IsDone())
            {
                Thread.Sleep(300);
            }
        }

        public void Stop() => _resultsReturn.Stop();

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
}
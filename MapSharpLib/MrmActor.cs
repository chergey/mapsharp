using System.Collections.Generic;
using System.Threading;

namespace MapSharpLib
{
    class MrmActor : IActor<Job>
    {
        private readonly IObjectPipe<Job> _op;
        private readonly Dictionary<string, Job> _waitOn;
        private readonly List<Job> _results;
        private readonly List<NodeDescription> _nStats;
        private readonly Dictionary<string, NodeDescription> _nStatsDic;
        private  readonly ReaderWriterLockSlim _nStatsLock;

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
            return new MrmActor(_waitOn, _nStats, _nStatsDic, _nStatsLock, _results, op);
        }

        public void Act()
        {
            var job = _op.GetObject();

            _nStatsLock.EnterWriteLock();
            try
            {
                _waitOn.Remove(job.JobName);
                string node = job.GetAtt("workNode");

                var nd = _nStatsDic[node];

                var jobs = new List<string>(nd.PendingWork);
                jobs.Remove(job.JobName);
                var newNodeDesc = new NodeDescription(node, jobs);

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
                _results.Add(job);
            }
        }
    }
}
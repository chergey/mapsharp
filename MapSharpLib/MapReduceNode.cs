using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using MapSharpLib.Network;

namespace MapSharpLib
{
    public class MrNetworkNode
    {
        private readonly Server<Job> _server;
        private readonly MrnActor _actor;
        private readonly string _host;

        public MrNetworkNode(string host, int port, Action<object> callback)
        {
            _host = host;
            _actor = new MrnActor(new AsyncTransfer<Job>(), null, callback);
            _server = new Server<Job>(_host, port, _actor);
        }

        public MrNetworkNode(string host, int port)
        {
            _host = host;
            _actor = new MrnActor(new AsyncTransfer<Job>(), null);
            _server = new Server<Job>(host, port, _actor);
        }

        public void Stop()
        {
            _actor.Stop();
            _server.Stop();
        }

        public void AddListener(Action<object> callback)
        {
            _actor.SetCallback(callback);
        }
    }


    public class MrNode
    {
        public static Job Run(Job j)
        {
            var mapReducerType = DllLoader.LoadDll(j.Assembly, j.MapReducerClass);
            var mr = (IMapperReducer) Activator.CreateInstance(mapReducerType);
            var inputs = j.Inputs;

            var output = MapReducer.MapReduce(mr, inputs);

            j.SetResults(output);
            return j;
        }

        public static Job Reduce(IList<Job> jA)
        {
            var j = jA[0];
            var mapReducerType = DllLoader.LoadDll(j.Assembly, j.MapReducerClass);
            var mr = (IMapperReducer) Activator.CreateInstance(mapReducerType);

            var isL = new List<ISerializable>(jA.Count);

            foreach (Job ij in jA)
                isL.Add(ij.Results);

            var res = MapReducer.Reduce(mr, isL);
            var retVal = j.Clone();
            retVal.SetResults(res);
            return retVal;
        }
    }

    public class MrnActor : IActor<Job>
    {
        private readonly IObjectPipe<Job> _pipe;
        private volatile Queue<Job> _wq;
        private readonly Thread[] _threads;
        private Action<object> _callback;

        public MrnActor(IObjectPipe<Job> dp, Queue<Job> workQueue, Action<object> callback = null)
        {
            _callback = callback;
            if (workQueue == null)
            {
                //Basically, This is a new Node. So make a new work queue
                //and spawn a pair of work threads.
                this._wq = new Queue<Job>();

                //HACK: Hard-coded variable: 2 threads per Node
                int numThreads = 2;

                _threads = new Thread[numThreads];
                for (int i = 0; i < numThreads; i++)
                {
                    _threads[i] = new Thread(this.Worker);
                    _threads[i].Start();
                }
            }
            else
                this._wq = workQueue;

            this._pipe = dp;
        }

        public void Stop()
        {
            foreach (var t in _threads)
                t.Abort();
        }

        private void Worker()
        {
            while (true)
            {
                if (_wq != null && _wq.Count > 0)
                {
                    Job workJob = null;
                    lock (_wq)
                    {
                        if (_wq.Count > 0)
                            workJob = _wq.Dequeue();
                    }

                    if (workJob != null)
                    {
                        Job j = MrNode.Run(workJob);
                        _callback?.Invoke($"Executing job {j.JobName}");
                        _pipe.PushObject(j.Client, j);
                    }
                }

                Thread.Sleep(500);
            }
        }

        public IActor<Job> NewActor(IObjectPipe<Job> dp) => new MrnActor(dp, _wq, _callback);

        public void Act()
        {
            Job j = _pipe.GetObject();
            lock (_wq)
            {
                _wq.Enqueue(j);
            }
        }

        public void SetCallback(Action<object> callback)
        {
            _callback = callback;
        }
    }

    public static class DllLoader
    {
        public static Type LoadDll(string dllPath, string mRclass)
        {
            try
            {
                var fi = new FileInfo(dllPath);
                string fullDllPath = fi.FullName;
                var a = Assembly.LoadFile(fullDllPath);
                Type t = a.GetType(mRclass);
                return t;
            }
            catch (Exception e)
            {
                Exception ex = new Exception("method/class not found", e);
                throw ex;
            }
        }

        public static Type LoadDll(byte[] rawAssembly, string mRclass)
        {
            try
            {
                var a = Assembly.Load(rawAssembly);
                Type t = a.GetType(mRclass);
                return t;
            }
            catch (Exception e)
            {
                Exception ex = new Exception("method/class not found", e);
                throw ex;
            }
        }
    }
}
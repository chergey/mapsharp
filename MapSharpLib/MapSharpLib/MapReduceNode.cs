/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/3/2009
 * Time: 12:12 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

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
        private readonly Server<Job> _ss;
        private readonly MrnActor _m;

        public MrNetworkNode(int port, Action<object> callback)
        {
            _m = new MrnActor(new AsyncTransfer<Job>(), null, callback);
            _ss = new Server<Job>(port, _m);
        }

        public MrNetworkNode(int port)
        {
            _m = new MrnActor(new AsyncTransfer<Job>(), null);
            _ss = new Server<Job>(port, _m);
        }

        public void Stop()
        {
            _m.Stop();
            _ss.Stop();
        }

        public void AddListener(Action<object> callback)
        {
            _m.SetCallback(callback);
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
        readonly IObjectPipe<Job> _pipe;
        volatile Queue<Job> _wq;
        readonly Thread[] _threads;
        private Action<object> _callback;

        public MrnActor(IObjectPipe<Job> dp, Queue<Job> workQueue) : this(dp, workQueue, null)
        {
        }

        public MrnActor(IObjectPipe<Job> dp, Queue<Job> workQueue, Action<object> callback)
        {
            _callback = callback;
            if (workQueue == null)
            {
                //Basically, This is a new Node. So make a new work queue
                //and spawn a pair of work threads.
                this._wq = new Queue<Job>();

                //HACK: Hard-coded variable: 2 threads per Node
                int numthreads = 2;

                _threads = new Thread[numthreads];
                for (int i = 0; i < numthreads; i++)
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
                    Job workjob = null;
                    lock (_wq)
                    {
                        if (_wq.Count > 0)
                            workjob = _wq.Dequeue();
                    }

                    if (workjob != null)
                    {
                        Job j = MrNode.Run(workjob);
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
        public static Type LoadDll(string dlLpath, string mRclass)
        {
            try
            {
                FileInfo fi = new FileInfo(dlLpath);
                string fullDlLpath = fi.FullName;
                Assembly a = Assembly.LoadFile(fullDlLpath);
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
                Assembly a = Assembly.Load(rawAssembly);
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
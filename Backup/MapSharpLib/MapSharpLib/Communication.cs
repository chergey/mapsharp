using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/8/2009
 * Time: 12:15 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Communication
{
    public interface IObjectPipe<T> where T : ISerializable
    {
        T GetObject();
        void PushObject(string receiver, T datum);
    }

    public interface IActor<T> where T : ISerializable
    {
        IActor<T> NewActor(IObjectPipe<T> op);
        void Act();
    }

    namespace Network
    {
        public class Server<T> where T : class, ISerializable
        {
            private readonly Listener<T> _listener;
            private readonly Thread _listenerThread;

            public Server(int port, IActor<T> actor)
            {
                _listener = new Listener<T>(port, actor);
                _listenerThread = new Thread(_listener.Run);
                _listenerThread.Start();
            }

            public void Stop()
            {
                _listenerThread.Abort();
            }
        }

        internal class Listener<T> where T : class, ISerializable
        {
            private readonly int _serverPort;
            private readonly IActor<T> _actor;

            public Listener(int port, IActor<T> actor)
            {
                _serverPort = port;
                _actor = actor;
            }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            public void Run()
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), _serverPort);
                    listener.Start();
                    while (true)
                    {
                        if (listener.Pending())
                        {
                            AsyncTransfer<T> trans = new AsyncTransfer<T>(listener.AcceptTcpClient());
                            IActor<T> newActor = _actor.NewActor(trans);
                            (new Thread(newActor.Act)).Start();
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception)
                {
                    //Console.WriteLine(ex.Message);
                }
            }
        }

        public class AsyncTransfer<T> : IObjectPipe<T> where T : class, ISerializable
        {
            private volatile T _finalObject;
            private readonly Thread _aT;

            //Send only
            public AsyncTransfer()
            {
            }

            public AsyncTransfer(TcpClient tclient)
            {
                AsyncReceiver aR = new AsyncReceiver(tclient, this);
                _aT = new Thread(aR.Receive);
                _aT.Start();
            }

            private class AsyncReceiver
            {
                readonly TcpClient _tClient;
                readonly AsyncTransfer<T> _parent;

                public AsyncReceiver(TcpClient tclient, AsyncTransfer<T> parent)
                {
                    _tClient = tclient;
                    _parent = parent;
                }

                public void Receive()
                {
                    Stream s = _tClient.GetStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    try
                    {
                        _parent._finalObject = (T) bf.Deserialize(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    s.Close();
                }
            }

            public T GetObject()
            {
                _aT.Join();
                return _finalObject;
            }

            public void PushObject(string receiver, T datum)
            {
                PushObj(receiver, datum);
            }

            private static void PushObj(string receiver, T datum)
            {
                string[] sA = receiver.Split(':');
                AsyncPusher ap = new AsyncPusher(sA[0], int.Parse(sA[1]), datum);
                var pT = new Thread(ap.AsyncPush);
                pT.Start();
            }

            private class AsyncPusher
            {
                string _ip;
                int _port;
                T _o;

                public AsyncPusher(string ip, int port, T o)
                {
                    _ip = ip;
                    _port = port;
                    _o = o;
                }

                public void AsyncPush()
                {
                    TcpClient tc = new TcpClient();
                    for (int i = 0; !tc.Connected && i < 200; i++)
                    {
                        try
                        {
                            tc.Connect(_ip, _port);
                        }
                        catch
                        {
                            Thread.Sleep(200);
                        }
                    }
                    Stream s = tc.GetStream();

                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(s, _o);
                    s.Flush();
                    tc.Close();
                }
            }
        }
    }
}
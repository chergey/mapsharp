using System;
using System.Collections.Generic;
using System.Threading;
using MapSharpLib;


namespace PrimeSearchExe
{
    class Program
    {
        public static void Main(string[] args)
        {
            //You should be running a NodeApp at the same time, otherwise, nothing'll happen.
            //var mrnn = new MrNetworkNode(1901);
            //var mrnn2 = new MrNetworkNode(1902);


            #region nodes

            //HACK: Hardcoded stuff
            int serverCount = 2;
            int managerPort = 1900;
            int firstServerPort = 1901;
            int maxValPrime = 170000;
            string host = "127.0.0.1";

            var mrm = new MrManager(host, managerPort);
            var nodes = new List<string>();
            for (int i = 0; i < serverCount; i++)
            {
                nodes.Add($"{host}:" + (firstServerPort + i));
            }

            #endregion

            mrm.NodesList = nodes;
            mrm.Do(PrimeSearch.PrimeSearchJob.FullJob(maxValPrime), 1000);

            //TODO: make it print only if the description's changed.
            while (!mrm.IsDone())
            {
                var nds = mrm.NodeStats;
                foreach (var nd in nds)
                    Console.WriteLine(nd + "\n");

                Thread.Sleep(300);
            }

            var finalResults = new HashSet<int>();
            foreach (var j in mrm.CurrentResults)
            {
                finalResults.UnionWith(((Wrapper<IEnumerable<int>>) (j.Results)).Value);
            }


            if (PrimeSearch.Check.IsCorrect(maxValPrime, finalResults))
                Console.WriteLine("Results match!");
            else
                Console.WriteLine("Results don't match");


            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);

            mrm.Stop();
            //mrnn.Stop();
            //mrnn2.Stop();
        }
    }
}
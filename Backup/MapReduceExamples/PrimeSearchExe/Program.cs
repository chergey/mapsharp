/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/17/2009
 * Time: 12:30 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
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
			Console.WriteLine("Hello PrimeSearch!");
			
			//You should be running a NodeApp at the same time, otherwise, nothing'll happen.
			MrNetworkNode mrnn = new MrNetworkNode(1901);
			MrNetworkNode mrnn2 = new MrNetworkNode(1902);
			
			
			#region nodes
			//HACK: Hardcoded stuff
			int serverCount = 2;
			int managerPort = 1900;
			int firstServerPort = 1901;
			int maxValPrime = 7000;
			
			MrManager mrm = new MrManager(managerPort);
			List<string> nodes = new List<string>();
			for(int i=0; i<serverCount;i++)
			{
				nodes.Add("127.0.0.1:" + (firstServerPort+i));
			}
			#endregion			
			mrm.NodesList = nodes;
			mrm.Do(PrimeSearch.PrimeSearchJob.FullJob(maxValPrime), 1000);
			
			//TODO: make it print only if the description's changed.
			while(!mrm.IsDone())
			{
				IEnumerable<NodeDescription> nds = mrm.NodeStats;
				foreach(NodeDescription nd in nds)
					Console.WriteLine(nd);
				Thread.Sleep(300);
			}
			
			HashSet<int> finalresults = new HashSet<int>();
			foreach(Job j in mrm.CurrentResults)
			{
				finalresults.UnionWith(((Wrapper<IEnumerable<int>>)(j.Results)).Value);
			}
			
			#region prove its correct
			if(PrimeSearch.Check.IsCorrect(maxValPrime, finalresults))
				Console.WriteLine("Woot! it works!");
			else
				Console.WriteLine("Darn! it don't work!");
			#endregion
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
			
			mrm.Stop();
			mrnn.Stop();
			mrnn2.Stop();
		}
	}
}
/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/17/2009
 * Time: 8:31 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using MapSharpLib;

namespace ConsoleNodeApp
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("NodeApp Lives!");
			
			MrNetworkNode mrnn = new MrNetworkNode(1901);
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
			mrnn.Stop();
		}
	}
}
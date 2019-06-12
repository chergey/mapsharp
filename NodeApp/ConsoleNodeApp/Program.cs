using System;
using MapSharpLib;

namespace ConsoleNodeApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("NodeApp Lives!");

            var mrnn = new MrNetworkNode("127.0.0.1", 1901);

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
            mrnn.Stop();
        }
    }
}
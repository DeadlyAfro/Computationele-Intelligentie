using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CI_practical1
{
    public static class Program
    {
        public static int sudokuSize, blockSize;

        public static void Main(string[] args)
        {
            // Most common good values.
            var slist = new [] {1, 2, 3}; //, 4, 5, 6, 7, 8, 9, 10};
            var nlist = new [] {1, 3, 5, 7}; //, 5, 6, 7, 8, 9, 10};

            var tasks = new List<Task>();
            var i = 0;

            foreach (var s in slist)
            {
                foreach (var n in nlist)
                {
                    tasks.Add(Task.Run(() => new Worker(i++, new Random(4), s, n).DoWork()));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Console.ReadLine();
        }
    }
}
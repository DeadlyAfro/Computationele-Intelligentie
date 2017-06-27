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
            var slist = new int[] {1, 2, 3, 5};
            var nlist = new int[] {1, 3, 5, 7};

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
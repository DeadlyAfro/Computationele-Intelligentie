using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CI_practical1
{
    public static class Program
    {
        private static int sudokuSize, recursiveCounter, blockSize;
        private static Stopwatch stopwatch;
        private static bool timeOut = false;

        private static string sudokuPath = "sudoku.txt";

        public static void Main(string[] args)
        {
            var sudoku = createSudoku(); //create the sudoku,
            recursiveCounter = 0;    //set runtime data
            stopwatch = new Stopwatch();
            stopwatch.Start();
            var solution = ILS(sudoku); // start backtracking
            PrintSudoku(solution);
            Console.WriteLine(stopwatch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine(recursiveCounter);
            Console.ReadKey();
        }

        public static int[,] ILS(int[,] sudoku)
        {
            return sudoku;
        }

        public static void PrintSudoku(int[,] sudoku)
        {
            for (int i = 0; i < sudokuSize; i++)
            {
                Console.WriteLine(string.Join(" | ", sudoku.GetRow(i)));
                Console.WriteLine(new string('-', sudokuSize * 4 - 3));
            }
        }

        private static int[,] createSudoku()
        {
            var lines = new List<string>();
            using (StreamReader reader = new StreamReader(sudokuPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            sudokuSize = lines.Count; //standard sudokusize

            if (!sudokuSize.IsPerfect() || sudokuSize != lines[0].Split(' ').Length)
            {
                throw new Exception("Invalid sudoku file.");
            }

            blockSize = (int)Math.Sqrt(sudokuSize);

            var sudoku = new int[sudokuSize, sudokuSize];

            for (var i = 0; i < sudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (int j = 0; j < sudokuSize; j++)
                {
                    sudoku[i, j] = line[j];
                }
            }

            PrintSudoku(sudoku);

            foreach (var (block, (x, y)) in sudoku.GetAllBlocks())
            {
                var domain = Enumerable.Range(1, sudokuSize).ToList();

                for (var i = 0; i < blockSize; i++)
                {
                    for (var j = 0; j < blockSize; j++)
                    {
                        if (block[i, j] > 0)
                        {
                            domain.Remove(block[i, j]);
                        }
                    }
                }

                for (var i = 0; i < blockSize; i++)
                {
                    for (var j = 0; j < blockSize; j++)
                    {
                        if (block[i, j] == 0)
                        {
                            if (!domain.Any()) throw new Exception("Empty domain.");
                            block[i, j] = domain.First();
                            domain.Remove(block[i, j]);
                        }
                    }
                }

                sudoku.SetBlock(block, (x, y));
            }

            return sudoku;
        }

        private static void updateRunTimeData()
        {
            //Implement timer
            var time = stopwatch.ElapsedMilliseconds;
            if (time > 600000) //more than 10 minutes
            {   //TODO:
                timeOut = true;
            }
            //count amount of recursive calls
            //recursiveCounter++;
        }

        private static bool IsPerfect(this int n)
        {
            return Math.Sqrt(n) % 1 == 0;
        }

        private static int[] GetRow(this int[,] array, int rownum)
        {
            if (array == null) throw new ArgumentNullException();

            var row = new int[(int)Math.Sqrt(array.Length)];

            for (int i = 0; i < (int)Math.Sqrt(array.Length); i++)
            {
                row[i] = array[rownum, i];
            }
            return row;
        }

        // List of tuples. Tuple of a Block and its coordinates in the sudoku.
        private static List<(int[,], (int x, int y))> GetAllBlocks(this int[,] sudoku)
        {
            var blocks = new List<(int[,], (int x, int y))>();

            for (var i = 0; i < blockSize; i++)
            {
                for (int j = 0; j < blockSize; j++)
                {
                    blocks.Add((sudoku.GetBlock(i, j), (i, j)));
                }
            }

            return blocks;
        }

        private static int[,] GetBlock(this int[,] sudoku, int x, int y)
        {
            var block = new int[blockSize, blockSize];

            for (var i = 0; i < blockSize; i++)
            {
                for (var j = 0; j < blockSize; j++)
                {
                    block[i, j] = sudoku[i + x * blockSize, j + y * blockSize];
                }
            }

            return block;
        }

        private static void SetBlock(this int[,] sudoku, int[,] block, int x, int y)
        {
            for (var i = 0; i < blockSize; i++)
            {
                for (var j = 0; j < blockSize; j++)
                {
                    sudoku[i + x * blockSize, j + y * blockSize] = block[i, j];
                }
            }
        }

        private static int[,] GetBlock(this int[,] sudoku, (int x, int y) c)
        {
            return GetBlock(sudoku, c.x, c.y);
        }

        private static void SetBlock(this int[,] sudoku, int[,] block, (int x, int y) c)
        {
            SetBlock(sudoku, block, c.x, c.y);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CI_practical1
{
    public static class Program
    {
        public static int SudokuSize, recursiveCounter, blockSize;
        private static Stopwatch stopwatch;
        private static bool timeOut = false;

        private static string sudokuPath = "sudoku.txt";

        public static void Main(string[] args)
        {
            var sudoku = createSudoku(); //create the sudoku,
            recursiveCounter = 0;    //set runtime data
            stopwatch = new Stopwatch();
            stopwatch.Start();
            var solution = BackTracking(sudoku); // start backtracking
            if (solution.values != null && solution.domains != null) PrintSudoku(solution.values);
            if (timeOut) Console.WriteLine("Timed out.");
            Console.WriteLine(stopwatch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine(recursiveCounter);
            Console.ReadKey();
        }

        private static void PrintSudoku(int[,] sudoku)
        {
            Console.WriteLine("Sudoku:");
            for (int i = 0; i < SudokuSize; i++)
            {
                Console.WriteLine(string.Join(" | ", sudoku.GetRow(i)));
                Console.WriteLine(new string('-', SudokuSize * 4 - 3));
            }
        }

        private static (int[,] values, List<int>[,] domains) BackTracking((int[,] values, List<int>[,] domains) t)
        {
            updateRunTimeData();
            //PrintSudoku(t);
            if (timeOut)
            {
                return t;
            }
            if (isGoal(t.values))
            {
                return t;
            }

            if (t.values == null || t.domains == null) return (null, null);

            var emptyfields = new List<(int x, int y)>();

            for (var x = 0; x < SudokuSize; x++)
            {
                for (var y = 0; y < SudokuSize; y++)
                {
                    if (t.values[x, y] == 0)
                    {
                        emptyfields.Add((x, y));
                    }
                }
            }
            var currentValue = SudokuSize;
            var field = (0, 0);
            foreach (var box in emptyfields)
            {
                var i = t.domains[box.x, box.y].Count();
                if (i < currentValue)
                {
                    field = box;
                    currentValue = i;
                }
            }

            foreach (var successor in GetSuccessors(t, field))
            {
                var t2 = BackTracking(successor);
                if (t2.values != null && t2.domains != null && isGoal(t2.values))
                {
                    return t2;
                }
            }
            return (null, null);
        }

        private static bool isGoal(int[,] t)
        {
            //check if the current puzzle state is the goal state
            foreach (var box in t)
            {
                if (box == 0) return false;
            }
            return true;
        }

        private static IEnumerable<(int[,] values, List<int>[,] domains)> GetSuccessors((int[,] values, List<int>[,] domains) t, (int x, int y) c)
        {
            foreach (var i in t.domains[c.x, c.y])
            {
                if (ForwardCheck(t, (c.x, c.y), i))
                {
                    var values = t.values.DeepClone();
                    var domains = t.domains.DeepClone();

                    values[c.x, c.y] = i;
                    RemoveFromDomain((values, domains), (c.x, c.y));

                    yield return (values, domains);
                }
            }
        }

        private static void RemoveFromDomain((int[,] values, List<int>[,] domains) t, (int x, int y) c)
        {
            var newval = t.values[c.x,c.y];

            // Row
            for (int i = 0; i < SudokuSize; i++)
            {
                if (i == c.y || t.values[c.x, i] > 0) continue;

                t.domains[c.x, i].Remove(newval);
            }

            // Column
            for (int i = 0; i < SudokuSize; i++)
            {
                if (i == c.x || t.values[i, c.y] > 0) continue;

                t.domains[i, c.y].Remove(newval);
            }

            // Block
            var blockStartX = blockSize * (c.x / blockSize);
            var blockStartY = blockSize * (c.y / blockSize);
            for (int i = blockStartX; i < blockStartX + blockSize; i++)
            {
                for (int j = blockStartY; j < blockStartY + blockSize; j++)
                {
                    if ((i == c.x && j == c.y) || t.values[i, j] > 0) continue;

                    t.domains[i, j].Remove(newval);
                }
            }
        }

        // Returns false if it makes any domain empty.
        private static bool ForwardCheck((int[,] values, List<int>[,] domains) t, (int x, int y) c, int value)
        {
            var newval = value;

            // Row
            for (int i = 0; i < SudokuSize; i++)
            {
                if (i == c.y || t.values[c.x, i] > 0) continue;

                if (t.domains[c.x, i].Count == 1 && t.domains[c.x, i][0] == value) return false;
            }

            // Column
            for (int i = 0; i < SudokuSize; i++)
            {
                if (i == c.x || t.values[i, c.y] > 0) continue;

                if (t.domains[i, c.y].Count < 2 && t.domains[i, c.y][0] == value) return false;
            }

            // Block
            var blockStartX = blockSize * (c.x / blockSize);
            var blockStartY = blockSize * (c.y / blockSize);
            for (int i = blockStartX; i < blockStartX + blockSize; i++)
            {
                for (int j = blockStartY; j < blockStartY + blockSize; j++)
                {
                    if ((i == c.x && j == c.y) || t.values[i, j] > 0) continue;

                    if (t.domains[i, j].Count < 2 && t.domains[i, j][0] == value) return false;
                }
            }

            return true;
        }

        private static (int[,] values, List<int>[,] domains) createSudoku()
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

            SudokuSize = lines.Count; //standard sudokusize
            blockSize = (int)Math.Sqrt(SudokuSize);

            if (!SudokuSize.IsPerfect() || SudokuSize != lines[0].Split(' ').Length)
            {
                throw new Exception("Invalid sudoku file.");
            }

            var values = new int[SudokuSize, SudokuSize];
            var domains = new List<int>[SudokuSize, SudokuSize];

            for (var i = 0; i < SudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (int j = 0; j < SudokuSize; j++)
                {
                    values[i, j] = line[j];
                    domains[i, j] = Enumerable.Range(1, SudokuSize).ToList();
                }
            }

            // Fix domains:

            for (var i = 0; i < SudokuSize; i++)
            {
                for (int j = 0; j < SudokuSize; j++)
                {
                    RemoveFromDomain((values, domains), (i, j)); //throw new FormatException();
                }
            }


            return (values, domains);
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
            recursiveCounter++;
        }

        private static bool IsPerfect(this int n)
        {
            return Math.Sqrt(n) % 1 == 0;
        }

        private static T[] GetRow<T>(this T[,] array, int rownum)
        {
            if (array == null) throw new ArgumentNullException();

            var row = new T[(int)Math.Sqrt(array.Length)];

            for (int i = 0; i < (int)Math.Sqrt(array.Length); i++)
            {
                row[i] = array[rownum, i];
            }
            return row;
        }

        private static List<int>[,] DeepClone(this List<int>[,] arr)
        {
            var newarr = new List<int>[SudokuSize, SudokuSize];

            for (var i = 0; i < SudokuSize; i++)
            {
                for (var j = 0; j < SudokuSize; j++)
                {
                    newarr[i, j] = new List<int>(arr[i, j]);
                }
            }

            return newarr;
        }

        private static int[,] DeepClone(this int[,] arr)
        {
            var newarr = new int[SudokuSize, SudokuSize];

            for (var i = 0; i < SudokuSize; i++)
            {
                for (var j = 0; j < SudokuSize; j++)
                {
                    newarr[i, j] = arr[i, j];
                }
            }

            return newarr;
        }
    }

    public enum ExpandMethod
    {
        LeftToRight, RightToLeft, Size
    }
}
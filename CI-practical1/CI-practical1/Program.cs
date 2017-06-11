using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CI_practical1
{
    public static class Program
    {
        private static Stack<Field[,]> trackStack = new Stack<Field[,]>();
        public static int SudokuSize, recursiveCounter;
        private static Stopwatch stopwatch;
        private static bool timeOut = false;

        private static string sudokuPath = "sudoku.txt";

        private static List<int[]> testlist = new List<int[]>();

        public static void Main(string[] args)
        {
            var sudoku = createSudoku(); //create the sudoku,
            trackStack.Push(sudoku); //push the first state to the stack,
            recursiveCounter = 0;    //set runtime data
            stopwatch = new Stopwatch();
            stopwatch.Start();
            var solution = BackTracking(trackStack); // start backtracking
            if (solution != null) PrintSudoku(solution);
            if (timeOut) Console.WriteLine("Timed out.");
            Console.WriteLine(stopwatch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine(recursiveCounter);
            Console.ReadKey();
        }

        private static void PrintSudoku(Field[,] sudoku)
        {
            Console.WriteLine("Sudoku:");
            for (int i = 0; i < SudokuSize; i++)
            {
                Console.WriteLine(string.Join(" | ", sudoku.GetRow(i).Select(x => x.Value)));
                Console.WriteLine(new string('-', SudokuSize * 4 - 3));
            }
        }

        private static Field[,] BackTracking(Stack<Field[,]> L)
        {
            updateRunTimeData();
            if (!L.Any())
            {
                Console.WriteLine("stack is empty");
                return null;
            }
            var t = L.First();
            //PrintSudoku(t);
            if (timeOut)
            {
                return t;
            }
            if (isGoal(t))
            {
                return t;
            }

            if (t == null) return null;

            var emptyfields = new List<(int x, int y)>();

            for (var x = 0; x < SudokuSize; x++)
            {
                for (var y = 0; y < SudokuSize; y++)
                {
                    if (t[x, y].Value == 0)
                    {
                        emptyfields.Add((x, y));
                    }
                }
            }
            var currentValue = SudokuSize;
            var field = (0, 0);
            foreach (var box in emptyfields)
            {
                var i = t[box.x, box.y].Domain.Count();
                if (i < currentValue)
                {
                    field = box;
                    currentValue = i;
                }
            }
            
            foreach (var successor in GetSuccessors(t, field))
            {
                L.Push(successor);
                var t2 = BackTracking(L);
                if (t2 != null && isGoal(t2))
                {
                    return t2;
                }
            }

            L.Pop();
            return null;
        }

        private static bool isGoal(Field[,] t)
        {
            //check if the current puzzle state is the goal state
            foreach (var box in t)
            {
                if(box.Value == 0) return false;
            }
            return true;
        }

        private static IEnumerable<Field[,]> GetSuccessors(Field[,] t, (int x, int y) c)
        {
            foreach (var i in t[c.x, c.y].Domain)
            {
                var successor = t.DeepClone(); // Deepclone so the Domain etc also gets cloned instead of copied by ref.

                successor[c.x, c.y].Value = i;

                if (ForwardCheck(successor, (c.x, c.y))) yield return successor;
            }
        }

        // Returns false if it makes any domain empty.
        private static bool ForwardCheck(Field[,] t, (int x, int y) c)
        {
            var newval = t[c.x, c.y].Value;

            // Row
            for (int i = 0; i < SudokuSize; i++)
            {
                if (i == c.y || t[c.x, i].Value > 0) continue;

                var field = t[c.x, i];

                field.Domain.Remove(newval);

                if (!field.Domain.Any()) return false;
            }

            // Column
            for (int i = 0; i < SudokuSize; i++)
            {
                if (i == c.x || t[i, c.y].Value > 0) continue;

                var field = t[i, c.y];

                field.Domain.Remove(newval);

                if (!field.Domain.Any()) return false;
            }

            // Block
            var blockSize = (int)Math.Sqrt(SudokuSize);
            var blockStartX = blockSize * (c.x / blockSize);
            var blockStartY = blockSize * (c.y / blockSize);
            for (int i = blockStartX; i < blockStartX + blockSize; i++)
            {
                for (int j = blockStartY; j < blockStartY + blockSize; j++)
                {
                    if ((i == c.x && j == c.y) || t[i, j].Value > 0) continue;

                    var field = t[i, j];

                    field.Domain.Remove(newval);

                    if (!field.Domain.Any()) return false;
                }
            }

            return true;
        }

        private static Field[,] createSudoku()
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

            if (!SudokuSize.IsPerfect() || SudokuSize != lines[0].Split(' ').Length)
            {
                throw new Exception("Invalid sudoku file.");
            }

            var sudoku = new Field[SudokuSize, SudokuSize];

            for (var i = 0; i < SudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (int j = 0; j < SudokuSize; j++)
                {
                    sudoku[i, j] = new Field(line[j]);
                }
            }

            // Fix domains:

            for (var i = 0; i < SudokuSize; i++)
            {
                for (int j = 0; j < SudokuSize; j++)
                {
                    if (!ForwardCheck(sudoku, (i, j))) throw new FormatException();
                }
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
            recursiveCounter++;
        }

        private static bool IsPerfect(this int n)
        {
            return Math.Sqrt(n) % 1 == 0;
        }

        private static T[] GetRow<T>(this T[,] array, int rownum)
        {
            if (array == null) throw new ArgumentNullException();

            var row = new T[(int)Math .Sqrt(array.Length)];

            for (int i = 0; i < (int)Math.Sqrt(array.Length); i++)
            {
                row[i] = array[rownum, i];
            }
            return row;
        }

        private static Field[,] DeepClone(this Field[,] arr)
        {
            var newarr = new Field[SudokuSize, SudokuSize];

            for (var i = 0; i < SudokuSize; i++)
            {
                for (var j = 0; j < SudokuSize; j++)
                {
                    newarr[i,j] = arr[i,j].Clone();
                }
            }

            return newarr;
        }

        private static int[] Simplify(this Field[,] field)
        {
            if (field == null) throw new ArgumentNullException();

            var intarr = new int[SudokuSize * SudokuSize];

            for (var i = 0; i < SudokuSize; i++)
            {
                for (var j = 0; j < SudokuSize; j++)
                {
                    intarr[i * SudokuSize + j] = field[i, j].Value;
                }
            }

            return intarr;
        }
    }

    public enum ExpandMethod
    {
        LeftToRight, RightToLeft, Size
    }

    public class Field
    {
        public int Value;
        public List<int> Domain;

        public Field(int val)
        {
            this.Value = val;
            this.Domain = Enumerable.Range(1, Program.SudokuSize).ToList();
        }

        public Field(int val, List<int> domain)
        {
            this.Value = val;
            this.Domain = domain;
        }

        public Field Clone()
        {
            return new Field(this.Value, new List<int>(this.Domain));
        }
    }
}
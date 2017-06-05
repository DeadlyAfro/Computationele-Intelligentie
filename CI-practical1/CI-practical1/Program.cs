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
        private static int sudokuSize, recursiveCounter;
        private static ExpandMethod expandMethod = ExpandMethod.Size;
        private static Stopwatch stopwatch;
        private static bool timeOut = false;

        private static List<(int x, int y)> ExpansionPriority;

        private static string sudokuPath = "sudoku.txt";

        public static void Main(string[] args)
        {
            var field = new Field();

            var sudoku = createSudoku(); //create the sudoku,
            trackStack.Push(sudoku); //push the first state to the stack,
            recursiveCounter = 0;    //set runtime data
            stopwatch = new Stopwatch();
            stopwatch.Start();
            var solution = BackTracking(trackStack); // start backtracking
            for (int i = 0; i < sudokuSize; i++)
            {
                Console.WriteLine(string.Join(" | ",solution.GetRow(i).Select(x => x.Value)));
                Console.WriteLine(new string('-', sudokuSize * 4 - 3));
            }
            Console.WriteLine(stopwatch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine(recursiveCounter);
            Console.ReadKey();
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
            if (timeOut)
            {
                Console.WriteLine("timeout");
                return t;
            }
            if (isGoal(t))
            {
                return t;
            }
            var successors = GetSuccessors(t);
            for (var i = 0; i < successors.Count(); i++)
            {
                var tNext = successors[i];
                L.Push(tNext);
                t = BackTracking(L);
                if (t != null && isGoal(t))
                {
                    return t;
                }
            }
            L.Pop();
            return null;
        }

        private static bool isGoal(Field[,] t)
        {
            var state = t;
            //check if the current puzzle state is the goal state
            foreach (var box in t)
            {
                if(box.Value != 0)
                   continue;
                else return false;
            }
            return true;
        }

        private static Field[][,] GetSuccessors(Field[,] t)
        {
            var emptyfields = new List<(int x, int y)>();

            for (var x = 0; x < sudokuSize; x++)
            {
                for (var y = 0; y < sudokuSize; y++)
                {
                    if (t[x, y].Value == 0)
                    {
                        emptyfields.Add((x, y));
                    }
                }
            }

            // Make this an array later?
            var successors = new List<Field[,]>();

            // Handle Fields ordered by domain size --> Most-Constrained Variable = Lowest Domain size
            foreach (var (x, y) in emptyfields.OrderBy(tuple => t[tuple.x, tuple.y].Domain.Count)) // Grr LINQ y u no tuples
            {
                var successor = t.DeepClone(); // Deepclone so the Domain etc also gets cloned instead of copied by ref.

                successor[x, y].Value = successor[x, y].Domain.First();

                if (ForwardCheck(successor, (x, y))) successors.Add(successor);
            }

            return successors.ToArray();
        }

        // Returns false if it makes any domain empty.
        private static bool ForwardCheck(Field[,] t, (int x, int y) c)
        {
            var newval = t[c.x, c.y].Value;

            // Row
            for (int i = 0; i < sudokuSize; i++)
            {
                if (i == c.y) continue;

                var field = t[c.x, i];

                field.Domain.Remove(newval);

                if (!field.Domain.Any()) return false;
            }

            // Column
            for (int i = 0; i < sudokuSize; i++)
            {
                if (i == c.x) continue;

                var field = t[i, c.y];

                field.Domain.Remove(newval);

                if (!field.Domain.Any()) return false;
            }

            // Block
            var blockSize = (int)Math.Sqrt(sudokuSize);
            var blockStartX = blockSize * (c.x / blockSize);
            var blockStartY = blockSize * (c.y / blockSize);
            for (int i = blockStartX; i < blockStartX + blockSize; i++)
            {
                for (int j = blockStartY; j < blockStartY + blockSize; j++)
                {
                    if (i == c.x && j == c.y) continue;

                    var field = t[i, j];

                    field.Domain.Remove(newval);

                    if (!field.Domain.Any()) return false;
                }
            }

            return true;
        }

        /*private static Field[][,] legalMoves(Field[,] t)
        {
            var (x, y) = Expand(t);

            //calculate the possible successors, and return them in an array
            var illegalValues = new HashSet<Field>();
            Field[][,] successors;
            //check for horizontal illegal moves
            for (var i = 0; i < sudokuSize; i++)
            {
                if (t[i, y].Value != 0)
                    illegalValues.Add(t[i, y]);
            }
            //check for vertical illegal moves
            for (var j = 0; j < sudokuSize; j++)
            {
                if (t[x, j].Value != 0)
                    illegalValues.Add(t[x, j]);
            }
            //check for illegal moves in the box's field
            int blockSize = (int)Math.Sqrt(sudokuSize); //Setup the field to look at
            int blockStartX =( x / blockSize) * blockSize;
            int blockStartY = (y / blockSize) * blockSize;

            //check for each value in the field
            for (var i = blockStartX; i < blockStartX + blockSize; i++)
                for (var j = blockStartY; j < blockStartY + blockSize; j++)
                    if (t[i, j] != 0)
                        illegalValues.Add(t[i, j]);

            //store the new possible states in an array and return it
            successors = new int[(sudokuSize - illegalValues.Count())][,];
            int counter = 0;
            for (var i = 1; i < sudokuSize+1; i++)
            {                
                if (!illegalValues.Contains(i))
                {
                    var nextState = t.Clone();
                    int[,] newArray = (int[,])nextState;
                    newArray[x, y] = i;
                    successors[counter] = newArray;
                    counter++;
                }
            }
            return successors;
        }*/

        /*private static (int, int) Expand(int[,] t)
        {
            switch (expandMethod)
            {
                case ExpandMethod.LeftToRight:
                {
                    var (i, j) = (0, 0);
                    while (t[i, j] > 0 && i < sudokuSize)
                    {
                        if (j < sudokuSize -1) j++;
                        else
                        {
                            j = 0;
                            i++;
                        }
                    }
                    return (i, j);
                }
                case ExpandMethod.RightToLeft:
                {
                    var (i, j) = (sudokuSize-1, sudokuSize-1);
                    while (t[i, j] > 0 && i >= 0)
                    {
                        if (j > 0) j--;
                        else
                        {
                            j = sudokuSize - 1;
                            i--;
                        }
                    }
                    return (i, j);
                }
                case ExpandMethod.Size:
                {
                    // Expand the first empty box according to the domain sizes.
                    // This should be: nextbox = ExpansionPriority.First((x, y) => t[x, y] <= 0); buuuut LINQ isn't updated yet.
                    return ExpansionPriority.First(xy => t[xy.x, xy.y] <= 0);
                }
                default:
                    throw new Exception();
            }
        }*/

        // Create a sorted list of boxes in the order they should be expanded with expand method 3.
        /*private static List<(int x, int y)> sortSuccessors(int[,] sudoku)
        {
            ExpansionPriority = new List<(int x, int y)>();

            var domainSizes = new int[sudokuSize, sudokuSize];

            // Calculate how many known values there are in the same row/column/block
            for (int i = 0; i < sudokuSize; i++)
            {
                for (int j = 0; j < sudokuSize; j++)
                {
                    // Assuming value of 0 = empty square.
                    if (sudoku[i, j] > 0)
                    {
                        // Row
                        for (int k = 0; k < sudokuSize; k++)
                        {
                            if (k == j) continue;

                            domainSizes[i, k]++;
                        }

                        // Column
                        for (int k = 0; k < sudokuSize; k++)
                        {
                            if (k == i) continue;

                            domainSizes[k, j]++;
                        }

                        // Block
                        var blockSize = (int)Math.Sqrt(sudokuSize);
                        var blockStartX = blockSize * (i / blockSize);
                        var blockStartY = blockSize * (j / blockSize);
                        for (int k = blockStartX; k < blockStartX + blockSize; k++)
                        {
                            for (int l = blockStartY; l < blockStartY + blockSize; l++)
                            {
                                if (k == i && l == j) continue;

                                domainSizes[k, l]++;
                            }
                        }
                    }
                }
            }

            var expansionDict = new Dictionary<(int, int), int>();  // Ghetto priority list.
            for (int i = 0; i < sudokuSize; i++)
            {
                for (int j = 0; j < sudokuSize; j++)
                {
                    expansionDict.Add((i, j), domainSizes[i, j]);
                }
            }
            return expansionDict.Keys.OrderByDescending(key => expansionDict[key]).ToList();
        }*/

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

            sudokuSize = lines.Count; //standard sudokusize

            if (!sudokuSize.IsPerfect() || sudokuSize != lines[0].Split(' ').Length)
            {
                throw new Exception("Invalid sudoku file.");
            }

            var sudoku = new Field[sudokuSize, sudokuSize];

            for (var i = 0; i < sudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (int j = 0; j < sudokuSize; j++)
                {
                    sudoku[i, j] = new Field(line[j]);
                }
            }

            // Fix domains:

            for (var i = 0; i < sudokuSize; i++)
            {
                for (int j = 0; j < sudokuSize; j++)
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
            var newarr = new Field[arr.GetLength(0), arr.GetLength(1)];

            for (var i = 0; i < arr.GetLength(0); i++)
            {
                for (var j = 0; j < arr.GetLength(1); j++)
                {
                    newarr[i,j] = arr[i,j].Clone();
                }
            }

            return newarr;
        }
    }

    public enum ExpandMethod
    {
        LeftToRight, RightToLeft, Size
    }

    public class Field
    {
        public int Value = 0;
        public List<int> Domain;

        public Field(int val = 0)
        {
            this.Value = val;
            this.Domain = Enumerable.Range(1, 9).ToList();
        }

        public Field Clone()
        {
            return new Field(){Value = this.Value, Domain = this.Domain.Select(x =>x).ToList()};
        }
    }
}
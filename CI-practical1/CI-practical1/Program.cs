using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CI_practical1
{
    public static class Program
    {
        private static Stack<int[,]> trackStack = new Stack<int[,]>();
        private static int sudokuSize, recursiveCounter;
        private static ExpandMethod expandMethod = ExpandMethod.RightToLeft;
        private static Stopwatch stopwatch;
        private static bool timeOut = false;

        private static List<(int x, int y)> ExpansionPriority;

        private static string sudokuPath = "sudoku.txt";

        public static void Main(string[] args)
        {
            var sudoku = createSudoku(); //create the sudoku,
            trackStack.Push(sudoku); //push the first state to the stack,
            recursiveCounter = 0;    //set runtime data
            stopwatch = new Stopwatch();
            stopwatch.Start();
            if (expandMethod == ExpandMethod.Size)
            {
                ExpansionPriority = sortSuccessors(sudoku);
            }
            var solution = BackTracking(trackStack); // start backtracking
            for (int i = 0; i < sudokuSize; i++)
            {
                Console.WriteLine(string.Join(" | ",solution.GetRow(i)));
                Console.WriteLine(new string('-', sudokuSize * 4 - 3));
            }
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.WriteLine(recursiveCounter);
            Console.ReadKey();
        }

        private static int[,] BackTracking(Stack<int[,]> L)
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
            var successors = legalMoves(t);
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

        private static bool isGoal(int[,] t)
        {
            var state = t;
            //check if the current puzzle state is the goal state
            foreach (var box in t)
            {
                if(box != 0)
                   continue;
                else return false;
            }
            return true;
        }

        private static int[][,] legalMoves(int[,] t)
        {
            var (x, y) = Expand(t);

            //calculate the possible successors, and return them in an array
            var illegalValues = new HashSet<int>();
            int[][,] successors;
            //check for horizontal illegal moves
            for (var i = 0; i < sudokuSize; i++)
            {
                if (t[i, y] != 0)
                    illegalValues.Add(t[i, y]);
            }
            //check for vertical illegal moves
            for (var j = 0; j < sudokuSize; j++)
            {
                if (t[x, j] != 0)
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
        }

        private static (int, int) Expand(int[,] t)
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
        }

        // Create a sorted list of boxes in the order they should be expanded with expand method 3.
        private static List<(int x, int y)> sortSuccessors(int[,] sudoku)
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

            var sudoku = new int[sudokuSize, sudokuSize];

            for (var i = 0; i < sudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (int j = 0; j < sudokuSize; j++)
                {
                    sudoku[i, j] = line[j];
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
    }

    public enum ExpandMethod
    {
        LeftToRight, RightToLeft, Size
    }
}
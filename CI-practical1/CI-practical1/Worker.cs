using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CI_practical1
{
    public class Worker
    {
        private Stopwatch stopwatch;
        private bool timeOut = false;
        private int S;
        private int N;

        public int ID;

        private string sudokuPath = "sudoku.txt";

        private int[] Domain;
        public List<(int x, int y)> Pairs;
        public Random Random;
        public int[,] EmptySudoku;

        public Worker(int id, Random random, int s, int n)
        {
            this.ID = id;
            this.Random = random;
            stopwatch = new Stopwatch();
            S = s;
            N = n;
        }

        public void DoWork()
        {
            var sudoku = createSudoku(); //create the sudoku,
            Domain = Enumerable.Range(1, Program.sudokuSize).ToArray();
            Pairs = GeneratePairs();
            stopwatch.Start();
            //PrintSudoku(sudoku);
            var solution = ILS(sudoku); // start backtracking
            //PrintSudoku(solution);
            Log(stopwatch.ElapsedMilliseconds + " milliseconds.");
        }

        public int[,] ILS(int[,] sudoku)
        {
            var counter = 0;

            var statevalue = EvaluateState(sudoku);
            Log("Current state value: " + statevalue);
            while (!timeOut && statevalue > 0)
            {
                if (counter > N)
                {
                    //Log("Random move time.");
                    for (int i = 0; i < S; i++)
                    {
                        SearchOp(sudoku, Random.Next(0, Program.sudokuSize), true);
                    }
                    counter = 0;
                    //Log("New state value: " + statevalue);
                }

                var result = LocalSearch(sudoku, statevalue);

                if (result == 0)
                {
                    counter++;
                }

                statevalue = EvaluateState(sudoku);
                //Log("Current state value: " + statevalue);
            }
            if (timeOut) Log("Final state value: " + statevalue);
            if (statevalue == 0)
            {
                Log("Success! The sudoku was successfully solved.");
            }

            Log($"The used parameters were: S={S}, N={N}");

            return sudoku;
        }

        public int LocalSearch(int[,] sudoku, int statevalue)
        {
            var blacklist = new List<int>();

            var blocknum = Random.Next(0, Program.sudokuSize);
            var result = SearchOp(sudoku, blocknum);
            if (result < 0) blacklist.Add(blocknum);
            statevalue -= result;

            while (result != 0 && blacklist.Count < (Program.sudokuSize - 1) && statevalue > 0)
            {
                blocknum = Random.Next(0, Program.sudokuSize);
                while (blacklist.Contains(blocknum)) blocknum = Random.Next(0, Program.sudokuSize);
                result = SearchOp(sudoku, blocknum);
                if (result < 0) blacklist.Add(blocknum);

                statevalue -= result;
            }

            return result;
        }

        public int SearchOp(int[,] sudoku, int blockNumber, bool perturb = false)
        {
            updateRunTimeData();
            var (blockX, blockY) = blockNumber.GetBlockCoords();

            var block = sudoku.GetBlock(blockX, blockY);
            var flat = block.Flatten();

            if (perturb)
            {
                var i1 = Random.Next(0, flat.Length);
                var i2 = Random.Next(0, flat.Length);
                while (i2 == i1) i2 = Random.Next(0, flat.Length);

                Swap(ref flat[i1], ref flat[i2]);

                sudoku.SetBlock(flat.Enflate(Program.blockSize), blockX, blockY);

                return int.MinValue; // Evalute at all? 
            }

            var possibleSwaps = Pairs.Select(xy => (xy, EvaluateSwap(sudoku, xy, blockX, blockY))).ToArray();
            var bestSwap = possibleSwaps.OrderByDescending(tuple => tuple.Item2).First();

            if (bestSwap.Item2 >= 0)
            {
                Swap(ref flat[bestSwap.Item1.x], ref flat[bestSwap.Item1.y]);
                var newblock = flat.Enflate(Program.blockSize);
                sudoku.SetBlock(newblock, blockX, blockY);
                //Log($"Swapped {bestSwap.Item1.x}|{flat[bestSwap.Item1.x]} and {bestSwap.Item1.y}|{flat[bestSwap.Item1.y]} in block {blockNumber} for +{bestSwap.Item2}.");
            }
            //Log("No swap today.");
            return bestSwap.Item2;
        }

        public int EvaluateSwap(int[,] sudoku, (int i1, int i2) t, int blockX, int blockY)
        {
            (int x1, int y1) = (t.i1.GetBlockCoords().x + blockX * Program.blockSize, t.i1.GetBlockCoords().y + blockY * Program.blockSize);
            if (EmptySudoku[x1, y1] > 0) return int.MinValue;
            var row1 = sudoku.GetRow(y1);
            var col1 = sudoku.GetColumn(x1);

            (int x2, int y2) = (t.i2.GetBlockCoords().x + blockX * Program.blockSize, t.i2.GetBlockCoords().y + blockY * Program.blockSize);
            if (EmptySudoku[x2, y2] > 0) return int.MinValue;
            var row2 = y1 == y2 ? row1 : sudoku.GetRow(y2);
            var col2 = x1 == x2 ? col1 : sudoku.GetColumn(x2);

            var baseEval = EvaluateCell(row1, col1) + EvaluateCell(row2, col2);

            var temp = row1[x1];
            row1[x1] = col1[y1] = row2[x2];
            row2[x2] = col2[y2] = temp;

            var swapEval = EvaluateCell(row1, col1) + EvaluateCell(row2, col2);
            return baseEval - swapEval;
        }

        public int EvaluateCell(int[] row, int[] col)
        {
            var missingInRow = Domain.Where(num => !row.Contains(num));
            var missingInCol = Domain.Where(num => !col.Contains(num));

            var rowc = missingInRow.Count();
            var colc = missingInCol.Count();

            return rowc + colc;
        }

        public int EvaluateState(int[,] sudoku)
        {
            int value = 0;

            for (var i = 0; i < Program.sudokuSize; i++)
            {
                value += Domain.Where(num => !sudoku.GetRow(i).Contains(num)).Count();
                value += Domain.Where(num => !sudoku.GetColumn(i).Contains(num)).Count();
            }

            return value;
        }

        public void PrintSudoku(int[,] sudoku)
        {
            Log(new string('-', 32));
            for (int i = 0; i < Program.sudokuSize; i++)
            {
                Log(string.Join(" | ", sudoku.GetRow(i)));
                Log(new string('-', Program.sudokuSize * 4 - 3));
            }
            Log(new string('-', 32));
        }

        private int[,] createSudoku()
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

            Program.sudokuSize = lines.Count; //standard sudokusize

            if (!Program.sudokuSize.IsPerfect() || Program.sudokuSize != lines[0].Split(' ').Length)
            {
                throw new Exception("Invalid sudoku file.");
            }

            Program.blockSize = (int)Math.Sqrt(Program.sudokuSize);

            var sudoku = new int[Program.sudokuSize, Program.sudokuSize];
            EmptySudoku = new int[Program.sudokuSize, Program.sudokuSize];

            for (var i = 0; i < Program.sudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (var j = 0; j < Program.sudokuSize; j++)
                {
                    sudoku[j, i] = line[j];
                    EmptySudoku[j, i] = line[j];
                }
            }
            foreach (var (block, (x, y)) in sudoku.GetAllBlocks())
            {
                var domain = Enumerable.Range(1, Program.sudokuSize).ToList();

                for (var i = 0; i < Program.blockSize; i++)
                {
                    for (var j = 0; j < Program.blockSize; j++)
                    {
                        if (block[i, j] > 0)
                        {
                            domain.Remove(block[i, j]);
                        }
                    }
                }

                for (var i = 0; i < Program.blockSize; i++)
                {
                    for (var j = 0; j < Program.blockSize; j++)
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

        private void updateRunTimeData()
        {
            var time = stopwatch.ElapsedMilliseconds;
            if (time > 600000)
            {
                timeOut = true;
            }
        }

        private static void Swap(ref int int1, ref int int2)
        {
            var temp = int1;

            int1 = int2;

            int2 = temp;
        }

        private static List<(int x, int y)> GeneratePairs()
        {
            var domain = Enumerable.Range(0, Program.sudokuSize).ToArray();

            // Ugly linq query but no idea how to format this as expression. 
            var pairs = from x in domain
                from y in domain
                where x < y
                select (x, y);
            return pairs.ToList();
        }

        private static int GetSeed()
        {
            return 4; // Totally random.
        }

        private void Log(string s)
        {
            Console.WriteLine($"[{ID}]: " + s);
        }
    }
}

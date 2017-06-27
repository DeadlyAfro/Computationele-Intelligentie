using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CI_practical1
{
    public class Worker
    {
        private int[,] emptySudoku;
        private (int row, int col)[,] evaluation;

        public int ID;

        private readonly int N;

        private List<(int x, int y)> Pairs;

        private readonly Random Random;
        private readonly int S;
        private readonly DateTime starttime;
        private readonly Stopwatch stopwatch;

        private readonly string sudokuPath = "sudokusf2.txt";
        private bool timeOut;

        public Worker(int id, Random random, int s, int n)
        {
            ID = id;
            Random = random;
            stopwatch = new Stopwatch();
            S = s;
            N = n;
            starttime = DateTime.UtcNow;
        }

        public void DoWork()
        {
            var sudoku = createSudoku();
            Pairs = GeneratePairs();
            stopwatch.Start();
            //PrintSudoku(sudoku);
            var solution = ILS(sudoku);
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
                    for (var i = 0; i < S; i++)
                    {
                        SearchOp(sudoku, Random.Next(0, Program.sudokuSize), true);
                    }
                    counter = 0;
                    statevalue = EvaluateState(sudoku);
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
            if (timeOut)
            {
                Log("Final state value: " + statevalue);
            }
            else
            {
                Log("Success! The sudoku was successfully solved.");
                Log($"The used parameters were: S={S}, N={N}");
            }

            return sudoku;
        }

        public int LocalSearch(int[,] sudoku, int statevalue)
        {
            var blacklist = new List<int>();

            int result;

            do
            {
                var blocknum = Random.Next(0, Program.sudokuSize);
                while (blacklist.Contains(blocknum)) blocknum = Random.Next(0, Program.sudokuSize);
                result = SearchOp(sudoku, blocknum);
                if (result < 0) blacklist.Add(blocknum);
                if (result > 0) blacklist.Clear();

                if (result >= 0) statevalue -= result;
            } while (result != 0 && blacklist.Count < Program.sudokuSize - 1 && statevalue > 0);

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
                ReEvaluateCells(sudoku, (i1, i2), blockX, blockY);
                return int.MinValue; // Evalute at all? 
            }

            var possibleSwaps = new List<((int x, int y), int val)>();

            // No linq because slow.
            foreach (var tuple in Pairs)
            {
                possibleSwaps.Add((tuple, EvaluateSwap(sudoku, tuple, blockX, blockY)));
            }
            // No orderby again because slow.
            var bestSwap = possibleSwaps.GetMax(tuple => tuple.val);

            if (bestSwap.val >= 0)
            {
                Swap(ref flat[bestSwap.Item1.x], ref flat[bestSwap.Item1.y]);
                var newblock = flat.Enflate(Program.blockSize);
                sudoku.SetBlock(newblock, blockX, blockY);
                ReEvaluateCells(sudoku, bestSwap.Item1, blockX, blockY);
                //Log($"Swapped {bestSwap.Item1.x}|{flat[bestSwap.Item1.x]} and {bestSwap.Item1.y}|{flat[bestSwap.Item1.y]} in block {blockNumber} for +{bestSwap.Item2}.");
            }
            //Log("No swap today.");
            return bestSwap.val;
        }

        public int EvaluateSwap(int[,] sudoku, (int i1, int i2) t, int blockX, int blockY)
        {
            (int x1, int y1) =
                (t.i1.GetBlockCoords().x + blockX * Program.blockSize, t.i1.GetBlockCoords().y +
                                                                       blockY * Program.blockSize);
            if (emptySudoku[x1, y1] > 0) return int.MinValue;

            (int x2, int y2) =
                (t.i2.GetBlockCoords().x + blockX * Program.blockSize, t.i2.GetBlockCoords().y +
                                                                       blockY * Program.blockSize);
            if (emptySudoku[x2, y2] > 0) return int.MinValue;

            var value1 = sudoku[x1, y1];
            var value2 = sudoku[x2, y2];

            var swapEval = 0;

            if (y1 != y2)
            {
                var row1 = sudoku.GetRow(y1);
                var row2 = sudoku.GetRow(y2);

                row1[x1] = value2;
                row2[x2] = value1;

                swapEval += FastCount(row1);
                swapEval += FastCount(row2);
            }

            if (x1 != x2)
            {
                var col1 = sudoku.GetColumn(x1);
                var col2 = sudoku.GetColumn(x2);

                col1[y1] = value2;
                col2[y2] = value1;

                swapEval += FastCount(col1);
                swapEval += FastCount(col2);
            }

            var baseEval2 = 0;

            if (x1 != x2)
            {
                baseEval2 += evaluation[x2, y2].col;
                baseEval2 += evaluation[x1, y2].col;
            }
            if (y2 != y1)
            {
                baseEval2 += evaluation[x2, y2].row;
                baseEval2 += evaluation[x1, y1].row;
            }

            return baseEval2 - swapEval;
        }

        // Too slow?
        public int EvaluateState(int[,] sudoku)
        {
            var value = 0;

            for (var i = 0; i < Program.sudokuSize; i++)
            {
                value += evaluation[0, i].row;
                value += evaluation[i, 0].col;
            }

            return value;
        }

        public void PrintSudoku(int[,] sudoku)
        {
            Log(new string('-', 32));
            for (var i = 0; i < Program.sudokuSize; i++)
            {
                Log(string.Join(" | ", sudoku.GetRow(i)));
                Log(new string('-', Program.sudokuSize * 4 - 3));
            }
            Log(new string('-', 32));
        }

        public void ReEvaluateCells(int[,] sudoku, (int i1, int i2) t, int blockX, int blockY)
        {
            (int x1, int y1) =
                (t.i1.GetBlockCoords().x + blockX * Program.blockSize, t.i1.GetBlockCoords().y +
                                                                       blockY * Program.blockSize);

            (int x2, int y2) =
                (t.i2.GetBlockCoords().x + blockX * Program.blockSize, t.i2.GetBlockCoords().y +
                                                                       blockY * Program.blockSize);

            for (var i = 0; i < Program.sudokuSize; i++)
            {
                if (x1 != x2)
                {
                    evaluation[x1, i] = GetCellValue(sudoku, x1, i);
                    evaluation[x2, i] = GetCellValue(sudoku, x2, i);
                }
                if (y1 != y2)
                {
                    evaluation[i, y1] = GetCellValue(sudoku, i, y1);
                    evaluation[i, y2] = GetCellValue(sudoku, i, y2);
                }
            }
        }

        // TODO refactor this, no longer needed.
        public (int row, int col) GetCellValue(int[,] sudoku, int x, int y)
        {
            var rowval = GetRowValue(sudoku, y);
            var colval = GetColumnValue(sudoku, x);

            return (rowval, colval);
        }

        // (relatively) fast way to count the number of duplicates. Used to compute the EvalFunc.
        public int FastCount(int[] arr)
        {
            var missingAmount = 0;
            var seen = new bool[Program.sudokuSize];

            for (var x = 0; x < Program.sudokuSize; x++)
            {
                var val = arr[x];

                if (!seen[val - 1])
                {
                    seen[val - 1] = true;
                }
                else
                {
                    missingAmount++;
                }
            }
            return missingAmount;
        }

        public int GetRowValue(int[,] sudoku, int y)
        {
            return FastCount(sudoku.GetRow(y));
        }

        public int GetColumnValue(int[,] sudoku, int x)
        {
            return FastCount(sudoku.GetColumn(x));
        }

        private int[,] createSudoku()
        {
            var lines = new List<string>();
            using (var reader = new StreamReader(sudokuPath))
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

            Program.blockSize = (int) Math.Sqrt(Program.sudokuSize);

            var sudoku = new int[Program.sudokuSize, Program.sudokuSize];
            emptySudoku = new int[Program.sudokuSize, Program.sudokuSize];

            for (var i = 0; i < Program.sudokuSize; i++)
            {
                var line = lines[i].Split(' ').Select(int.Parse).ToList();

                for (var j = 0; j < Program.sudokuSize; j++)
                {
                    sudoku[j, i] = line[j];
                    emptySudoku[j, i] = line[j];
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

            // Improve later.
            evaluation = new(int row, int col)[Program.sudokuSize, Program.sudokuSize];

            for (var x = 0; x < Program.sudokuSize; x++)
            {
                for (var y = 0; y < Program.sudokuSize; y++)
                {
                    evaluation[x, y] = GetCellValue(sudoku, x, y);
                }
            }

            // Note to self: update VS on laptop.
            var s = sudokuPath?.Length;

            return sudoku;
        }

        private void updateRunTimeData()
        {
            if ((DateTime.UtcNow - starttime).TotalMilliseconds > 600000)
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
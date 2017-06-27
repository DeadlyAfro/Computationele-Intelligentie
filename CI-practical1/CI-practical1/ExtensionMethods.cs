using System;
using System.Collections.Generic;
using System.Linq;

namespace CI_practical1
{
    public static class ExtensionMethods
    {
        public static bool IsPerfect(this int n)
        {
            return Math.Sqrt(n) % 1 == 0;
        }

        public static int[] GetRow(this int[,] array, int rownum)
        {
            if (array == null) throw new ArgumentNullException();

            var row = new int[(int) Math.Sqrt(array.Length)];

            for (var i = 0; i < row.Length; i++)
            {
                row[i] = array[i, rownum];
            }
            return row;
        }

        public static int[] GetColumn(this int[,] array, int colnum)
        {
            if (array == null) throw new ArgumentNullException();

            var col = new int[(int) Math.Sqrt(array.Length)];

            for (var i = 0; i < col.Length; i++)
            {
                col[i] = array[colnum, i];
            }
            return col;
        }

        // List of tuples. Tuple of a Block and its coordinates in the sudoku.
        public static List<(int[,], (int x, int y))> GetAllBlocks(this int[,] sudoku)
        {
            var blocks = new List<(int[,], (int x, int y))>();

            for (var i = 0; i < Program.blockSize; i++)
            {
                for (var j = 0; j < Program.blockSize; j++)
                {
                    blocks.Add((sudoku.GetBlock(i, j), (i, j)));
                }
            }

            return blocks;
        }

        public static int[,] GetBlock(this int[,] sudoku, int x, int y)
        {
            var block = new int[Program.blockSize, Program.blockSize];

            for (var i = 0; i < Program.blockSize; i++)
            {
                for (var j = 0; j < Program.blockSize; j++)
                {
                    block[i, j] = sudoku[i + x * Program.blockSize, j + y * Program.blockSize];
                }
            }

            return block;
        }

        public static void SetBlock(this int[,] sudoku, int[,] block, int x, int y)
        {
            for (var i = 0; i < Program.blockSize; i++)
            {
                for (var j = 0; j < Program.blockSize; j++)
                {
                    sudoku[i + x * Program.blockSize, j + y * Program.blockSize] = block[i, j];
                }
            }
        }

        public static int[,] GetBlock(this int[,] sudoku, (int x, int y) c)
        {
            return GetBlock(sudoku, c.x, c.y);
        }

        public static void SetBlock(this int[,] sudoku, int[,] block, (int x, int y) c)
        {
            SetBlock(sudoku, block, c.x, c.y);
        }

        public static int[] Flatten(this int[,] arr)
        {
            var newarr = new int[arr.GetLength(0) * arr.GetLength(1)];

            for (var y = 0; y < arr.GetLength(0); y++)
            {
                for (var x = 0; x < arr.GetLength(1); x++)
                {
                    newarr[x + y * arr.GetLength(1)] = arr[x, y];
                }
            }

            return newarr;
        }

        public static int[,] Enflate(this int[] arr, int size)
        {
            return arr.Enflate(size, size);
        }

        public static int[,] Enflate(this int[] arr, int sizeX, int sizeY)
        {
            var newarr = new int[sizeX, sizeY];

            for (var i = 0; i < arr.Length; i++)
            {
                newarr[i % sizeX, i / sizeY] = arr[i];
            }

            return newarr;
        }

        public static (int x, int y) GetBlockCoords(this int i)
        {
            var result = (i % Program.blockSize, i / Program.blockSize);

            return result;
        }

        public static T GetMax<T>(this IEnumerable<T> e, Func<T, int> f)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            var enumerable = e as T[] ?? e.ToArray();
            if (!enumerable.Any()) throw new ArgumentException(nameof(e) + " is empty.");

            var result = enumerable.First();
            var value = f(result);

            foreach (var t in enumerable)
            {
                if (f(t) > value)
                {
                    result = t;
                    value = f(t);
                }
            }

            return result;
        }
    }
}
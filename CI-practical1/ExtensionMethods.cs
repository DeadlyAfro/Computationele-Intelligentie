using System;

public static class ExtensionMethods
{

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

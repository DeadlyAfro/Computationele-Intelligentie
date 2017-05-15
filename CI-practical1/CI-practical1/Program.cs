using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CI_practical1
{
    class Program
    {
        static Stack<int[,]> trackStack = new Stack<int[,]>();
        static int sudokuSize;
        static Tuple<int,int> nextBox = new Tuple<int, int>(0,0);
        static int expandMethod = 1;

        static void Main(string[] args)
        {
            int[,] sudoku = createSudoku();                 //create the sudoku,
            trackStack.Push(sudoku);                        //push the first state to the stack,
            int[,] solution = BackTracking(trackStack);     //and start backtracking
        }

        static int[,] BackTracking(Stack<int[,]> L)
        {
            if (L.Count() == 0)
                return null;
            else
            {
                int[,] t = L.First();
                if (isGoal(t))
                    return t;
                else
                {
                    Expand(t);
                    int[][,]successors = legalMoves(t, nextBox.Item1, nextBox.Item2);
                    for(int i = 0; i< successors.Count(); i++)
                    {
                        int[,] tNext = successors[i];
                        L.Push(tNext);
                        BackTracking(L);
                    }
                    L.Pop();
                }

            }
            return null;
        }
        static bool isGoal(int[,] t)
        {
            //TODO:
            //check if the current puzzle state is the goal state
            return true;
        }
        static int[][,] legalMoves(int [,] t, int x, int y)
        {
            HashSet<int[,]> successors = new HashSet<int[,]>();
            //TODO:
            //calculate the possible successors, and return them in an array
            return successors.ToArray();
        }
        static void Expand(int[,] t)
        {
            //TODO:
            //select the next box to expand (according to expand method)
            //and set nextBox to the correct value

        }
        static void sortSuccessors()
        {
            //TODO:
            //create a sorted list of boxes in the order they should be expanded with expand method 3.
        }
        static int[,] createSudoku()  
        {
            sudokuSize = 9; //standard sudokusize
            int[,] sudoku = new int[sudokuSize, sudokuSize];
            //TODO:  
            //read sudoku from file and create an array representation of the puzzle
            return sudoku;
        }
        static void updateRunTimeData()
        {
            //TODO:
            //Implement timer
            //count amount of recursive calls
        }
    }
}

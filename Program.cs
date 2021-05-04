using System;

namespace console_maze
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Mazes.SquareMaze mz = new Mazes.SquareMaze(50, 15, 1, Console.WriteLine);
            mz.Generate();
        }
    }
}
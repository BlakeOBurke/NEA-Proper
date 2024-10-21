using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace OpenTk26_3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Game ga = new Game(1200,900,0);
            int SEED = 0;
            while (true)
            {
                switch(Menu())
                {
                    default:
                        Console.WriteLine("enter a seed, press enter for a new course");
                        if(int.TryParse(Console.ReadLine(),out int seed))
                        {
                            SEED = seed;
                            Game ga = new Game(1200, 900, SEED);
                            ga.Run(30, 30);
                            ga.Dispose();
                        }
                        else
                        {
                            Random a = new Random();
                            SEED = a.Next();
                            Game ga = new Game(1200, 900, SEED);
                            ga.Run(30, 30);
                            ga.Dispose();
                        }
                        Console.WriteLine($"your seed was {SEED}");
                        break;
                    case 'L':
                        string choice;
                        while (true)
                        {
                            Console.WriteLine("enter the seed of a course to see it's leaderboard, press enter to return to menu");
                            choice = Console.ReadLine();
                            if(choice.Count() == 0)
                            {
                                break;
                            }
                            try
                            {
                                Leaderboard arrr = new Leaderboard(choice);
                                arrr.top5();
                            }
                            catch
                            {
                                Console.WriteLine("enter a valid seed, or the leaderboard does not exist");
                            }
                        }
                        break;

                }
            }
            
            //Game ga = new Game(1200,900, z);
            //ga.Run(30,30);
            //ga.Run();
            Console.WriteLine("hi");
        }
        public class Leaderboard
        {
            List<Leaderboard_entry> board;
            public string path = "";
            public Leaderboard(int length)
            {
                board = new List<Leaderboard_entry>();
            }
            public Leaderboard(string[] a)
            {
                board = new List<Leaderboard_entry>();
                for (int i = 0; i < a.Length; i++)
                {
                    board.Add(new Leaderboard_entry(a[i].Split(' ')[0], float.Parse(a[i].Split(' ')[1])));
                }
            }
            public Leaderboard(string name, float time, int SEED)
            {
                board = new List<Leaderboard_entry> { new Leaderboard_entry(name, time) };
                path = SEED + "_BOARD.txt";
                SaveBoard();
            }
            public Leaderboard(string path)
            {
                this.path = path;
                string[] a = File.ReadAllLines(path + "_BOARD.txt");
                board = new List<Leaderboard_entry>();
                for (int i = 0; i < a.Length; i++)
                {
                    board.Add(new Leaderboard_entry(a[i].Split(' ')[0], float.Parse(a[i].Split(' ')[1])));
                }
            }
            public void top5()
            {
                Console.WriteLine("the top 5 scores");
                if (board.Count() < 5)
                {
                    for (int i = 0; i < board.Count(); i++)
                    {
                        Console.WriteLine($"{board[i].name}, {board[i].time} seconds");
                    }
                    for (int i = 0; i < 5 - board.Count(); i++)
                    {
                        Console.WriteLine("---");
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Console.WriteLine($"{board[i].name}, {board[i].time} seconds");
                    }
                }
            }
            public void WholeBoard()
            {
                Console.WriteLine("whole leaderboard");
                for (int i = 0; i < board.Count; i++)
                {
                    Console.WriteLine($"{board[i].name}, {board[i].time} seconds");
                }
            }
            public void SaveBoard()
            {
                sortBoard();
                string[] a = new string[board.Count()];
                for (int i = 0; i < a.Count(); i++)
                {
                    a[i] = board[i].name + $" {board[i].time}";
                }
                File.WriteAllLines(path, a);
            }
            public void sortBoard()
            {
                bool swapped;
                for (int i = board.Count(); i > 0; i--)
                {
                    for (int j = 0; j < i - 1; j++)
                    {
                        swapped = false;
                        if (board[j].time > board[j + 1].time)
                        {
                            Leaderboard_entry temp = board[j + 1];
                            board[j + 1] = board[j];
                            board[j] = temp;
                            swapped = true;
                        }
                    }

                }
                //for (int i = 0; i < A.Length; i++)
                //{
                //    Console.Write(A[i] + " ");
                //}

            }
            public void AddValue(string name, float time)
            {
                board.Add(new Leaderboard_entry(name, time));
                sortBoard();
                SaveBoard();
            }
        }
        public class Leaderboard_entry
        {
            public string name;
            public float time;
            public Leaderboard_entry(string name, float time)
            {
                this.name = name;
                this.time = time;
            }
        }
        public static int choiceValidator(int lower, int upper)
        {
            int choice;
            while (true)
            {
                try
                {
                    choice = int.Parse(Console.ReadLine());
                    if (choice >= lower && choice <= upper)
                    {
                        return choice;
                    }
                    Console.WriteLine("invalid input");
                }
                catch
                {
                    Console.WriteLine("invalid input");
                }
            }
        }
        public static char Menu()
        {
            return Console.ReadLine()[0];
        } 



    }

}

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
        public static float FinishedTime = 0;

        public static List<string> Inputs;
        static void Main(string[] args)
        {
            bool finishGame = false;
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
                        SetLeader(SEED, FinishedTime);

                        break;
                    case 'G':
                        //user must enter a seed for ghost mode because a replay must exist for that seed
                        Console.WriteLine("enter a seed, press enter to return to menu");
                        if (int.TryParse(Console.ReadLine(), out seed))
                        {
                            SEED = seed;
                            Game ga = new Game(1200, 900, SEED, "G");
                            ga.Run(30, 30);
                            ga.Dispose();
                            Console.WriteLine($"your seed was {SEED}");
                            SetLeader(SEED, FinishedTime);
                        }


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
                    case 'X':
                        finishGame = true;
                        break;
                    case 'R':
                        Console.WriteLine("enter a seed, press enter for a new course");
                        if (int.TryParse(Console.ReadLine(), out seed))
                        {
                            SEED = seed;
                            Game ga = new Game(1200, 900, SEED, "R");
                            ga.Run(30, 30);
                            ga.Dispose();
                        }
                        break;


                }
                if (finishGame) { break; }
            }
        }
        public static void setTime(float time)
        {
            FinishedTime = time;
        }
        public static void RecordInputs(List<string>inputs)
        {
            Inputs = inputs;
        }
        public static void SetLeader(int seed, float finishTime)
        {
            if (finishTime != 0)
            {
                string LeaderADD = "";
                while (true)
                {
                    Console.WriteLine("enter your name to save your time");
                    LeaderADD = Console.ReadLine();

                    if (LeaderADD.Contains(" "))
                    {
                        Console.WriteLine("invalid name, try again");
                    }
                    else
                    {
                        break;
                    }
                }

                if (Program.Leaderboard.LeaderBoard_Exist(seed.ToString()))
                {
                    Program.Leaderboard board = new Program.Leaderboard(seed.ToString());
                    board.AddValue(LeaderADD, finishTime);
                }
                else
                {
                    Program.Leaderboard board = new Program.Leaderboard(LeaderADD, finishTime, seed);
                }
                Leaderboard CheckForReplay = new Program.Leaderboard(seed.ToString());
                if (float.Parse(CheckForReplay.Fastest().ToString()) <= float.Parse(finishTime.ToString()))
                {
                    try
                    {
                        File.Delete(seed.ToString());
                    }
                    catch { }
                    File.WriteAllLines(seed.ToString(), Inputs);
                }
            }
        }
        public class Leaderboard
        {
            List<Leaderboard_entry> board;
            public string path = "";
            public Leaderboard(string name, float time, int SEED)
            {
                board = new List<Leaderboard_entry> { new Leaderboard_entry(name, time) };
                path = SEED.ToString();
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
            public float Fastest()
            {
                return board[0].time;
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
                File.WriteAllLines(path + "_BOARD.txt", a);
            }
            public void sortBoard()
            {
                bool swapped;
                for (int i = board.Count(); i > 0; i--)
                {
                    swapped = false;
                    for (int j = 0; j < i - 1; j++)
                    {
                        if (board[j].time > board[j + 1].time)
                        {
                            Leaderboard_entry temp = board[j + 1].Clone();
                            board[j + 1] = board[j].Clone();
                            board[j] = temp.Clone();
                            swapped = true;
                        }
                        if (!swapped)
                        {
                            break;
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
            public static bool LeaderBoard_Exist(string path)
            {
                try
                {
                    File.ReadAllLines(path + "_BOARD.txt");
                    return true;
                }
                catch { return false; }
            }
        }

        public struct Leaderboard_entry
        {
            public string name { get; set; }
            public float time { get; set; }
            public Leaderboard_entry(string name, float time)
            {
                this.name = name;
                this.time = time;
            }
            public Leaderboard_entry Clone()
            {
                return new Leaderboard_entry(this.name, this.time);
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
        public static char choiceMaker(string options)
        {
            while (true)
            {
                Console.WriteLine($"enter a character from '{options}'");
                string choice = Console.ReadLine();
                try
                {
                    char Selection = choice[0];
                    char UpperSelection = choice.ToUpper()[0];
                    if(options.Contains(Selection) || options.Contains(UpperSelection))
                    {
                        return UpperSelection;
                    }
                }
                catch
                {

                }
            }

        }
        public static char Menu()
        {
            Console.WriteLine("Play Game 'P'");
            Console.WriteLine("View Leaderboard 'L'");
            Console.WriteLine("PLACEHOLDER 'AHHH'");
            Console.WriteLine("Ghost Mode 'G'");
            Console.WriteLine("Replay 'R'");
            Console.WriteLine("Exit 'X'");
            return choiceMaker("PLGRX");
        } 



    }

}

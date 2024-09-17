using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
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
            Game ga = new Game(1920,1080);
            ga.Run(30,30);
            //ga.Run();
            Console.WriteLine("hi");
        }




    }

}

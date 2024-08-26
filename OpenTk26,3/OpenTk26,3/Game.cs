using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.CodeDom;
using OpenTK.Graphics;
using OpenTK.Input;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Globalization;
using System.Media;
using System.Timers;
using System.Diagnostics;
using System.Security.Policy;
using static OpenTk26_3.Game;
using System.Net.NetworkInformation;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Data.OleDb;

namespace OpenTk26_3
{


    public class Game : GameWindow
    {
        public static Random rnd = new Random();
        static float[] vertices = { };
        static uint[] indices = { };
        public int VertexBufferObject;
        public int ElementBufferObject;
        public int VertexArrayObject;
        public static int MOUSEX, MOUSEY;
        public static bool quickMov = false;

        //distance from car to camera in follow modes
        public static float camDistance = 20;

        public static float maxCarAcceleration = .125f;
        public static float maxCarVelocity = 2.5f;

        public const float tStep = 1 / 30f;
        public const float meter = 0.5f;
        public const float Gravity = -9.81f;

        public static Game.camera player = new Game.camera(0, 0, 0);

        Shader shader;

        Stopwatch stopwatch = new Stopwatch();
        float starttime = 0;

        public static Terrain landscape;
        public static Terrain racetrack;

        public Game(int width, int height) : base(width, height, GraphicsMode.Default, "game")
        {

        }
        public static Color randomColor()
        {
            return Color.FromArgb(255, rnd.Next(50, 205), rnd.Next(50, 205), rnd.Next(50, 205));
        }
        static vertex[] infromFile(string path)
        {
            string[] infile = File.ReadAllLines(path); ;
            List<vertex> vert = new List<vertex>();

            Vector3 col = new Vector3(rnd.Next(150, 205), rnd.Next(150, 205), rnd.Next(1, 205));

            for (int i = 0; i < infile.Count(); i++)
            {
                if (infile[i].Contains("vertex"))
                {
                    string[] invertex = infile[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    vert.Add(new vertex(new Vector3(float.Parse(invertex[1]), float.Parse(invertex[2]), float.Parse(invertex[3])), Color.FromArgb(50, ((int)col[0] + rnd.Next(1, 50)), ((int)col[1] + rnd.Next(1, 50)), ((int)col[2] + rnd.Next(1, 50)))));
                }
            }

            return vert.ToArray();

        }

        public static void FreeCam(ref camera cam, KeyboardState input)
        {
            if (input.IsKeyDown(Key.W))
            {
                Vector3 mov = cam.camforward();

                if (quickMov)
                {
                    mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                }


                cam.pos += new Vector3(mov[0], mov[1], mov[2]);
            }
            if (input.IsKeyDown(Key.S))
            {
                Vector3 mov = cam.camforward();

                if (quickMov)
                {
                    mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                }

                cam.pos += new Vector3(-mov[0], -mov[1], -mov[2]);

            }
            if (input.IsKeyDown(Key.A))
            {
                Vector3 mov = Vector3.Cross(cam.camforward(), new Vector3(0, -1, 0));

                if (quickMov)
                {
                    mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                }

                cam.pos += (new Vector3(mov[0], mov[1], mov[2]));

            }
            if (input.IsKeyDown(Key.D))
            {
                Vector3 mov = Vector3.Cross(cam.camforward(), new Vector3(0, -1, 0));

                if (quickMov)
                {
                    mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                }

                cam.pos += new Vector3(-mov[0], -mov[1], -mov[2]);
            }

            if (input.IsKeyDown(Key.ShiftLeft))
            {
                quickMov = true;
            }
            else if (input.IsKeyUp(Key.ShiftLeft))
            {
                quickMov = false;
            }

            if (input.IsKeyDown(Key.T))
            {
                player.zooooooom -= 0.00174533f * 4;
            }
            else if (input.IsKeyDown(Key.G))
            {
                player.zooooooom += 0.00174533f * 4;
            }
        }
        public static void FreeMouse(ref camera cam)
        {
            MouseState moose = Mouse.GetState();
            cam.direction[1] -= (moose.X - MOUSEX) * 0.001f;
            MOUSEX = moose.X;

            cam.direction[0] += (moose.Y - MOUSEY) * 0.001f;
            MOUSEY = moose.Y;

            if (cam.direction[0] > Math.PI / 2 - 0.05f)
            {
                cam.direction[0] = (float)Math.PI / 2 - 0.05f;
            }
            else if (cam.direction[0] < -Math.PI / 2 + 0.05f)
            {
                cam.direction[0] = (float)-Math.PI / 2 + 0.05f;
            }


        }

        public static void FollowCam(ref camera cam, Kart car)
        {
            MouseState moose = Mouse.GetState();
            cam.direction[1] -= (moose.X - MOUSEX) * 0.001f;
            MOUSEX = moose.X;

            cam.direction[0] += (moose.Y - MOUSEY) * 0.001f;
            MOUSEY = moose.Y;

            if (cam.direction[0] > Math.PI / 2 - 0.05f)
            {
                cam.direction[0] = (float)Math.PI / 2 - 0.05f;
            }
            else if (cam.direction[0] < -Math.PI / 2 + 0.05f)
            {
                cam.direction[0] = (float)-Math.PI / 2 + 0.05f;
            }

        }

        public static void FreeFollowCam(ref camera cam, Kart car)
        {
            //cam.pos = car.centre;


            MouseState moose = Mouse.GetState();
            cam.direction[1] -= (moose.X - MOUSEX) * 0.001f;
            MOUSEX = moose.X;

            cam.direction[0] += (moose.Y - MOUSEY) * 0.001f;
            MOUSEY = moose.Y;

            if (cam.direction[0] > Math.PI / 2 - 0.05f)
            {
                cam.direction[0] = (float)Math.PI / 2 - 0.05f;
            }
            else if (cam.direction[0] < -Math.PI / 2 + 0.05f)
            {
                cam.direction[0] = (float)-Math.PI / 2 + 0.05f;
            }
            cam.pos = car.centre - cam.camforward() * camDistance;
        }

        public static void DriveCar(Kart car, KeyboardState input)
        {
            bool powerred = false;
            if (input.IsKeyDown(Key.Up))
            {
                car.SetPos(new Vector3(0, 0, 1));
            }
            else if (input.IsKeyDown(Key.Down))
            {
                car.SetPos(new Vector3(0, 0, -1));
                //powerred = true; powerred = true;
                //car.acceleration -= car.CarForward();
            }
            if (input.IsKeyDown(Key.Right))
            {
                car.SetPos(new Vector3(-1 , 0, 0));
                //car.acceleration += Vector3.Cross(car.CarForward(), new Vector3(0, 1, 0));
                //car.velocity = car.velocity * Matrix3.CreateRotationY(car.angle.Y+0.1f*tStep);
            }

            else if (input.IsKeyDown(Key.Left))
            {
                car.SetPos(new Vector3(1, 0, 0));
                //car.acceleration -= Vector3.Cross(car.CarForward(), new Vector3(0, 1, 0));
                //car.velocity = car.velocity * Matrix3.CreateRotationY(car.angle.Y-0.1f*tStep);
            }


        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            GL.ClearColor(Color.CornflowerBlue);

            stopwatch = Stopwatch.StartNew();


            shader = new Shader("shader.vert.txt", "shader.frag.txt");


            GL.Enable(EnableCap.DepthTest);

            MOUSEX = 0; MOUSEY = 0;

            Kart.Cars.Add(new Kart(randomColor()));
            Kart.Cars.Last().Scale(1.5f);
            Kart.Cars.Last().SetPos(new Vector3(0,40,0));

            landscape = new Terrain();
            racetrack = new Terrain("circle");

            landscape.terrain.SetPos(landscape.terrain.centre - new Vector3(320 + 128, 0, 320 + 128));
            racetrack.terrain.SetPos(racetrack.terrain.centre - new Vector3(320 + 128, 0, 320 + 128));

            //landscape.terrain.SetPos(-landscape.terrain.avgPos());
            //racetrack.terrain.SetPos(-landscape.terrain.avgPos());
        }
        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
            shader.Dispose();
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            KeyboardState input = Keyboard.GetState();

            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
            }

            //FreeCam(ref player, input);
            //FreeMouse(ref player);
            FreeFollowCam(ref player, Kart.Cars[0]);
            DriveCar(Kart.Cars[0], input);


            if (input.IsKeyDown(Key.Space))
            {
                Kart.Cars.Add(new Kart(randomColor()));
                Kart.Cars.Last().Scale(3);
                Kart.Cars.Last().SetPos(new Vector3(rnd.Next(-Terrain.gridDimension*2,Terrain.gridDimension*2), rnd.Next(-Terrain.gridDimension * 2, Terrain.gridDimension * 2), rnd.Next(-Terrain.gridDimension*2, Terrain.gridDimension * 2)));
            }


            for (int i = 0; i < Shape.Models.Count(); i++)
            {
                Shape.Models[i].MakeMovement(new Vector3(0, 0, 0), new Vector3(0.1777f, 2 * 0.1777f, 0), 1);

                Shape.Models[i].Move();
            }

            for (int i = 0; i < Kart.Cars.Count(); i++)
            {
                Kart.Cars[i].MakeMovement(new Vector3(0, Gravity, 0), new Vector3(0,0,0),1);

                Kart.Cars[i].Move();
            }




            for (int i = 0; i < Kart.Cars.Count(); i++)
            {
                tripleCollide(racetrack, Kart.Cars[i], out Vector2 raceAngle, out float tHeight);
                tripleCollide(landscape, Kart.Cars[i], out Vector2 grassAngle, out float lHeight);
                //Collision(racetrack, Kart.Cars[i].centre, out float tHeight);
                //Collision(landscape, Kart.Cars[i].centre, out float lHeight);
                if (tHeight + Kart.Cars[i].dimensions.Y / 2f + 0.3f > Kart.Cars[i].centre.Y || lHeight + Kart.Cars[i].dimensions.Y / 2f + 0.3f > Kart.Cars[i].centre.Y)
                {
                    Kart.Cars[i].centre.Y = (float)Math.Max(lHeight, tHeight) + Kart.Cars[i].dimensions.Y / 2f;
                }
                Kart.Cars[i].angle.X = grassAngle.X;
                Kart.Cars[i].angle.Z = grassAngle.Y;
            }

            if (input.IsKeyDown(Key.Y))
            {
                racetrack = new Terrain("circle");
            }
        }

        //static List<float> verts;
        //static List<uint> inds;
        public void drawObj(List<Shape> a, Matrix4 proj)
        {
            for (int i = 0; i < a.Count(); i++)
            {
                Matrix4 model = modelMat(a[i]);

                //GL.BindVertexArray(VertexArrayObject);

                //starttime = time;

                //GL.Uniform1(location:(proj), 1);
                Matrix4 aproj = Matrix4.CreateScale(a[i].scale) * model * proj;

                int uniID = GL.GetUniformLocation(3, "projection");


                GL.UniformMatrix4(uniID, true, ref aproj);

                //vertices = a[i].GetFloat();
                //indices = a[i].triangle;


                //GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
                //GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);


                //GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                //GL.EnableVertexAttribArray(0);


                //GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                //GL.EnableVertexAttribArray(1);



                //GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
                //GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

                GL.BindVertexArray(a[i].VertexArrayObject);


                GL.BindBuffer(BufferTarget.ArrayBuffer, a[i].VertexBufferObject);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, a[i].ElementBufferObject);


                GL.VertexAttribPointer(a[i].VertexBufferObject, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                GL.EnableVertexAttribArray(a[i].VertexBufferObject);


                GL.VertexAttribPointer(a[i].ElementBufferObject, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(a[i].ElementBufferObject);




                shader.Use();



                GL.DrawElements(PrimitiveType.Triangles, a[i].count, DrawElementsType.UnsignedInt, 0);



                //GL.EnableVertexAttribArray(a.VBO);



            }
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            List<Shape> models = new List<Shape>();
            models.AddRange(Shape.Models);
            models.AddRange(Kart.Cars);
            models.Add(landscape.terrain);
            models.Add(racetrack.terrain);
            //models.Add(water.terrain);
            //models.AddRange(A.pieces);
            drawObj(models, pro(player));
            //drawTerrain(A, 1, pro(player));


            //Console.WriteLine(Shape.shapes.Sum() + " " + Shape.Models.Count());
            //Console.WriteLine(player.pos.X + "  " +player.pos.Y + "  "+ player.pos.Z);

            shader.Dispose();

            this.SwapBuffers();

        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, this.Width, this.Height);
        }
        public class vertex
        {
            public Vector3 pos;
            public Color color;
            public vertex(float x, float y, float z, Color Colors)
            {
                this.pos = new Vector3(x, y, z);
                this.color = Colors;
            }
            public vertex(Vector3 inp, Color Colors)
            {
                this.pos = inp;
                this.color = Colors;
            }

            public vertex(Vector4 a, Color b)
            {
                this.pos = new Vector3(a);
                this.color = b;
            }

        }
        public class camera
        {
            public Vector3 pos;
            public Vector3 direction;
            public Vector3 forward;
            public float dx, dy, dz;
            public float zooooooom;
            public camera(float x, float y, float z)
            {
                this.pos = new Vector3(x, y, z);
                this.direction = new Vector3(0, 0, 0);
                dx = 0; dy = 0; dz = 1;
                this.zooooooom = (float)(0.0174533 * 60);
                forward = new Vector3(0, 0, 1);
            }

            public Vector3 camforward()
            {
                Matrix4 rotation = Matrix4.CreateRotationZ(-direction[2]) * Matrix4.CreateRotationY(-direction[1]) * Matrix4.CreateRotationX(-direction[0]);
                Vector4 ouut = (rotation * new Vector4(forward, 1));
                return new Vector3(ouut);
            }
        }
        public static Matrix4 modelMat(Shape shapee)
        {
            Matrix4 mov = Matrix4.CreateRotationZ(shapee.angle[2]) * Matrix4.CreateRotationY(shapee.angle[1]) * Matrix4.CreateRotationX(shapee.angle[0]) * Matrix4.CreateTranslation(shapee.centre);
            return mov;
        }
        public static Matrix4 pro(camera cam)
        {
            Vector3 forw = cam.camforward();
            forw[0] += cam.pos[0]; forw[1] += cam.pos[1]; forw[2] += cam.pos[2];
            //MY_vector3 right = MY_vector3.cross(forw, new MY_vector3(0,-1,0)).normalise();
            //MY_vector3 upp = MY_vector3.cross(right, forw);
            Matrix4 camer = Matrix4.LookAt(new Vector3(cam.pos[0], cam.pos[1], cam.pos[2]), new Vector3(forw[0], forw[1], forw[2]), new Vector3(0, 1, 0));

            //camer.Transpose();
            if (cam.zooooooom > Math.PI - 0.001f)
            {
                cam.zooooooom = (float)Math.PI - 0.001f;
            }
            else if (cam.zooooooom < 0.0001f)
            {
                cam.zooooooom = 0.0001f;
            }

            camer = camer * Matrix4.CreatePerspectiveFieldOfView(cam.zooooooom, 1.33333f, 0.1f, 20000f);
            return camer;
        }
        public class Shader
        {
            int Handle;

            public Shader(string vertexPath, string fragmentPath)
            {
                int VertexShader, FragmentShader;
                string VertexShaderSource = File.ReadAllText(vertexPath);
                //string VertexShaderSource = "#version 330 core\nlayout (location = 0) in vec3 aPos;   // the position variable has attribute position 0\nlayout(location = 1) in vec3 aColor; // the color variable has attribute position 1\nout vec3 ourColor; // output a color to the fragment shader\nuniform mat4 projection;\nvoid main()\n{\n    gl_Position = vec4(aPos, 1) * projection;\n    ourColor = aColor; // set ourColor to the input color we got from the vertex data\n}";

                string FragmentShaderSource = File.ReadAllText(fragmentPath);
                //string FragmentShaderSource = "#version 330 core\r\nout vec4 FragColor;  \r\nin vec3 ourColor;\r\n  \r\nvoid main()\r\n{\r\n    FragColor = vec4(ourColor, 0.2f);\r\n}";

                VertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(VertexShader, VertexShaderSource);

                FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(FragmentShader, FragmentShaderSource);


                GL.CompileShader(VertexShader);

                GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int success);
                if (success == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(VertexShader);
                    Console.WriteLine(infoLog);
                }

                GL.CompileShader(FragmentShader);

                GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int ssuccess);
                if (ssuccess == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(FragmentShader);
                    Console.WriteLine(infoLog);
                }




                Handle = GL.CreateProgram();

                GL.AttachShader(Handle, VertexShader);
                GL.AttachShader(Handle, FragmentShader);

                GL.LinkProgram(Handle);

                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int sssuccess);
                if (sssuccess == 0)
                {
                    string infoLog = GL.GetProgramInfoLog(Handle);
                    Console.WriteLine(infoLog);
                }

                GL.DetachShader(Handle, VertexShader);
                GL.DetachShader(Handle, FragmentShader);
                GL.DeleteShader(FragmentShader);
                GL.DeleteShader(VertexShader);
            }
            public void Use()
            {
                GL.UseProgram(Handle);
            }
            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    GL.DeleteProgram(Handle);

                    disposedValue = true;
                }
            }

            ~Shader()
            {
                if (disposedValue == false)
                {
                    Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
                }
            }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public int tripleCollide(Terrain terrain, Shape Shape, out Vector2 angle, out float height)
        {
            //wrapper function, collide 3 points on the car to find the angle of the terrain and make and average position
            Shape.GetDimension();
            Collision(terrain, Shape.centre + new Vector3(0, 0, Shape.dimensions.Z / 2), out float height1);
            Collision(terrain, Shape.centre + new Vector3(-Shape.dimensions.X / 2, 0, -Shape.dimensions.Z / 2), out float height2);
            Collision(terrain, Shape.centre + new Vector3(+Shape.dimensions.X / 2, 0, -Shape.dimensions.Z / 2), out float height3);

            // 1
            //2 3

            height = (height1 + height2 + height3) / 3f;

            angle = new Vector2(0,0);
            //pitch and roll only needed, yaw based on the movement
            angle.X = (float)Math.Asin((- height1 + (height2 + height3) / 2)/Shape.dimensions.Z);
            angle.Y = (float)Math.Asin((height3-height2)/Shape.dimensions.X);
            return 0;

        }

        public int Collision(Terrain terrain, Vector3 B, out float Height)
        {
            //return 0 if above the terrain
            //1 if under the track
            //2 if under the other terrain
            //move up / slow down car accordingly

            //matrix method for barycentric coordinates, formulas from wikipedia

            //get square on grid for 

            //square
            //32
            //10

            //convert shape coordinates to one square on the terrain

            //terrain has 
            //Terrain.gridDimension
            //^2 pieces
            //centres at 0,0

            //each terrain piece is 5 long or 10 of the 0.5 meters 
            //add (gridDimension/2) to the shapes centre then divide by 5 and it will be in relative terrain world with each corner of a terrain piece being easily indedxable from the array 
            //check if outside the range (0,0) to (gridDimension, gridDimension)

            float X = B.X / Terrain.TileSize + (Terrain.gridDimension / 2);
            float Z = B.Z / Terrain.TileSize + (Terrain.gridDimension / 2);

            int X_ = (int)Math.Floor(X);
            int Z_ = (int)Math.Floor(Z);

            //terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].color = Color.Red;
            //terrain.terrain.verts[(X_+1) * Terrain.gridDimension + Z_].color = Color.Red;
            //terrain.terrain.verts[X_ * Terrain.gridDimension + Z_+1].color = Color.Red;
            //terrain.terrain.verts[(X_+1) * Terrain.gridDimension + Z_+1].color = Color.Red;
            ////terrain.terrain.resetBuffers();

            if(X_ < 0 || Z_ < 0 || X_ > Terrain.gridDimension-2 || Z_ > Terrain.gridDimension - 2)
            {
                Height = 0;
                return -1;
            }

            //decide bottom right or top left triangle
            //corners
            Vector3[] c = new Vector3[3];
            float[] barry = new float[3];
            if (X < Z)
            {
                //bottom right
                c[0] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].pos;
                c[1] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos;
                c[2] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos;
                barry = barycentric(terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].pos, terrain.terrain.verts[(X_+1) * Terrain.gridDimension + Z_].pos, terrain.terrain.verts[X_ * Terrain.gridDimension + Z_+1].pos, B);
            }
            else
            {
                //top left
                c[0] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_ + 1].pos;
                c[1] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos;
                c[2] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos;
                barry = barycentric(terrain.terrain.verts[(X_+1) * Terrain.gridDimension + Z_+1].pos, terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos, terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos, B);
            }

            Height = c[0].Y * barry[0] + c[1].Y * barry[1] + c[2].Y * barry[2];
            Height *= Terrain.TileSize;
            //Console.WriteLine(Height);
            return 0;
        }


        public float[] barycentric(Vector3 A, Vector3 B, Vector3 C,Vector3 position)
        {
            //since the terrain is scaled, it needs to be resized
            A *= Terrain.TileSize;
            B *= Terrain.TileSize;
            C *= Terrain.TileSize;
            position += (Terrain.TileSize / 2f) * new Vector3(Terrain.gridDimension, 0, Terrain.gridDimension);
            float[] barycentrics = new float[3];

            float T = (A.X - C.X) * (B.Z-C.Z) - (A.Z-C.Z) * (B.X-C.X);

            barycentrics[0] = ((B.Z - C.Z) * (position.X - C.X) + (C.X - B.X) * (position.Z - C.Z)) / T;
            barycentrics[1] = ((C.Z - A.Z) * (position.X - C.X) + (A.X - C.X) * (position.Z - C.Z)) / T;
            barycentrics[2] = 1f - barycentrics[0] - barycentrics[1];
            
            return barycentrics;
        }
    }
    public class Shape
    {
        public Vector3 angle;
        public int count;
        public vertex[] verts;
        public uint[] triangle;
        public Vector3 direction = new Vector3(0, 0, 1);
        public Vector3 scale = new Vector3(1, 1, 1);
        public Vector3 velocity;
        public Vector3 acceleration;

        public Vector3 centre;
        public Vector3 dimensions;

        public static List<int> shapes = new List<int>();
        public static List<Shape> Models = new List<Shape>();

        public List<Movement> Moves = new List<Movement>();

        public int VertexBufferObject;
        public int ElementBufferObject;
        public int VertexArrayObject;

        public Shape(string path, Color color)
        {

            string[] inp = File.ReadAllLines(path);

            List<vertex> ver = new List<vertex>();

            List<uint> tria = new List<uint>();


            Vector3 col = new Vector3(color.R, color.G, color.B);

            for (int i = 0; i < inp.Count(); i++)
            {
                if (inp[i].Substring(0, 2) == "v ")
                {
                    string[] point = inp[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    ver.Add(new vertex(new Vector3(float.Parse(point[1]), float.Parse(point[2]), float.Parse(point[3])), Color.FromArgb(50, Math.Abs((int)col[0] + Game.rnd.Next(-50, 50)), Math.Abs((int)col[1] + Game.rnd.Next(-50, 50)), Math.Abs((int)col[2] + Game.rnd.Next(-50, 50)))));
                }
                else if (inp[i].Substring(0, 2) == "f ")
                {
                    string[] point = inp[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    tria.Add((uint)(int.Parse(point[1].Split('/')[0]) - 1));
                    tria.Add((uint)(int.Parse(point[2].Split('/')[0]) - 1));
                    tria.Add((uint)(int.Parse(point[3].Split('/')[0]) - 1));
                }
            }

            this.verts = ver.ToArray();
            this.triangle = tria.ToArray();


            this.centre = avgPos();
            this.count = this.triangle.Count();

            this.angle = new Vector3(0, 0, 0);


            SetPosVerts(centre, angle);

            this.centre = avgPos();
            shapes.Add(count);



            doBuffers();

        }
        public void doBuffers()
        {

            this.VertexBufferObject = GL.GenBuffer();
            this.ElementBufferObject = GL.GenBuffer();
            this.VertexArrayObject = GL.GenVertexArray();

            resetBuffers();
        }
        public void resetBuffers()
        {
            float[] vertices = GetFloat();
            uint[] indices = triangle;


            GL.BindVertexArray(VertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);


            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);





        }
        public Shape(Color color)
        {
            List<vertex> ver = new List<vertex>() { new vertex(new Vector3(0, 0, 0), color), new vertex(new Vector3(0, 0, 1), color), new vertex(new Vector3(1, 0, 0), color), new vertex(new Vector3(1, 0, 1), color) };

            List<uint> tria = new List<uint>() { 0, 1, 2, 1, 2, 3 };



            this.verts = ver.ToArray();
            this.triangle = tria.ToArray();


            this.centre = new Vector3(.5f, 0, .5f);
            this.count = this.verts.Count();

            this.angle = new Vector3(0, 0, 0);


            SetPosVerts(centre, angle);

            this.centre = new Vector3(0, 0, 0);
            shapes.Add(count);
        }
        public Vector3 avgPos()
        {
            double x = 0; double y = 0; double z = 0;
            for (int i = 0; i < this.verts.Count(); i++)
            {
                x += this.verts[i].pos[0];
                y += this.verts[i].pos[1];
                z += this.verts[i].pos[2];
            }

            return new Vector3((float)(x / this.verts.Count()), (float)(y / this.verts.Count()), (float)(z / this.verts.Count()));
        }
        public void GetDimension()
        {
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            for (int i = 0; i < this.verts.Count(); i++)
            {
                if (verts[i].pos.X < min.X)
                {
                    min.X = verts[i].pos.X;
                }
                else if (verts[i].pos.X > max.X)
                {
                    max.X = verts[i].pos.X;
                }

                else if (verts[i].pos.Y < min.Y)
                {
                    min.Y = verts[i].pos.Y;
                }
                else if (verts[i].pos.Y > max.Y)
                {
                    max.Y = verts[i].pos.Y;
                }

                else if (verts[i].pos.Z < min.Z)
                {
                    min.Z = verts[i].pos.Z;
                }
                else if (verts[i].pos.Z > max.Z)
                {
                    max.Z = verts[i].pos.Z;
                }
            }

            dimensions = new Vector3((float)Math.Abs(max.X - min.X), (float)Math.Abs(max.Y - min.Y), (float)Math.Abs(max.Z - min.Z));
            dimensions *= scale;
        }
        public void SetPos(Vector3 movement, Vector3 rotation)
        {
            this.angle[0] += rotation[0];
            this.angle[1] += rotation[1];
            this.angle[2] += rotation[2];

            this.direction = new Vector3(new Vector4(0, 0, 1, 0) * Matrix4.CreateRotationZ(this.angle[2]) * Matrix4.CreateRotationY(this.angle[1]) * Matrix4.CreateRotationX(this.angle[0]));

            this.centre += movement;
        }
        public void SetPos(Vector3 movement)
        {
            this.centre += movement;
        }
        public void SetPosVerts(Vector3 movement, Vector3 rotation)
        {

            Matrix4 moova1 = Matrix4.CreateTranslation(centre);
            Matrix4 moova2 = Matrix4.CreateTranslation(-(centre + movement));
            Matrix4 moverr = Matrix4.CreateRotationZ(rotation[2]) * Matrix4.CreateRotationY(rotation[1]) * Matrix4.CreateRotationX(rotation[0]);

            moverr = moova1 * moverr * moova2;
            angle += rotation;

            for (int i = 0; i < verts.Count(); i++)
            {
                verts[i] = new vertex(moverr * (new Vector4(verts[i].pos[0], verts[i].pos[1], verts[i].pos[2], 1)), verts[i].color);
            }

            this.direction = new Vector3(new Vector4(0, 0, 1, 0) * Matrix4.CreateRotationZ(this.angle[2]) * Matrix4.CreateRotationY(this.angle[1]) * Matrix4.CreateRotationX(this.angle[0]));

            centre += movement;
        }
        public virtual void Scale(Vector3 scale)
        {
            this.scale = this.scale * scale;
        }
        public virtual void Scale(float scale)
        {
            this.scale = scale * this.scale;
        }
        public float[] GetFloat()
        {
            //gets all of the information about the vertices to send to the graphics as a single array
            float[] result = new float[count * 6];
            for (int i = 0; i < verts.Count(); i++)
            {
                result[i * 6] = verts[i].pos[0];
                result[i * 6 + 1] = verts[i].pos[1];
                result[i * 6 + 2] = verts[i].pos[2];
                result[i * 6 + 3] = verts[i].color.R / 255f;
                result[i * 6 + 4] = verts[i].color.G / 255f;
                result[i * 6 + 5] = verts[i].color.B / 255f;
            }

            return result;
        }

        public void MakeMovement(Vector3 movement, Vector3 rotation, int times)
        {
            Moves.Add(new Movement(movement, rotation, times));
        }

        public void Move()
        {
            for (int i = 0; i < Moves.Count; i++)
            {
                if (Moves[i].times == 0)
                {
                    Moves.RemoveAt(i);
                    i--;
                }
                else
                {
                    Moves[i].times--;
                    centre += Moves[i].movement * tStep;
                    angle += Moves[i].rotation * tStep;
                }
            }
        }

        public class Movement
        {
            public Vector3 movement;
            public Vector3 rotation;
            public int times;
            public Movement(Vector3 movement, Vector3 rotation, int times)
            {
                this.rotation = rotation;
                this.movement = movement;
                this.times = times;
            }
        }
    }



    public class Kart : Shape
    {
        public static List<Kart> Cars = new List<Kart>();
        public Kart(Color color) : base("Kart.obj", color) { GetDimension(); }
        public override void Scale(float scale)
        {
            base.Scale(scale);
            GetDimension();
        }
        public override void Scale(Vector3 scale)
        {
            base.Scale(scale);
            GetDimension();
        }
        public Vector3 CarForward()
        {
            Matrix4 rotation = Matrix4.CreateRotationZ(-angle[2]) * Matrix4.CreateRotationY(-angle[1]) * Matrix4.CreateRotationX(-angle[0]);
            Vector4 ouut = (rotation * new Vector4(new Vector3(0,0,1), 1));
            return new Vector3(ouut);
        }
    }

    public class Terrain
    {
        public Shape terrain;
        public static int gridDimension = 128;
        public static int HeightMulti = 10;
        public static int TileSize = 5;
        float[,] heights = new float[gridDimension, gridDimension];

        string path = "128_good.obj";
        public Terrain()
        {
            heights = Perlin.DoPerlin(heights, 0, 0);
            List<float> temporary = new List<float>();
            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    temporary.Add(heights[i, j]);
                }
            }
            Console.WriteLine(temporary.Min());
            Console.WriteLine(temporary.Max());

            terrain = new Shape(path, Color.Green);
            terrain.Scale(TileSize);
            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    terrain.verts[gridDimension * (i) + j].pos.Y += (float)Math.Pow(Math.E, heights[i, j]) * HeightMulti * meter;

                    Color col = terrain.verts[gridDimension * i + j].color;
                    terrain.verts[gridDimension * i + j].color = Color.FromArgb(col.R, (col.G + (int)Math.Min((int)(heights[i, j] / Math.Sqrt(2) * 255), 255)) / 2, col.B);
                    Console.WriteLine(terrain.verts[gridDimension *i + j].pos.X);
                }
            }

            terrain.resetBuffers();
        }
        public Terrain(string shape)
        {
            heights = Perlin.DoPerlin(heights, 0, 0);
            List<float> temporary = new List<float>();
            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    temporary.Add(heights[i, j]);
                }
            }
            Console.WriteLine(temporary.Min());
            Console.WriteLine(temporary.Max());

            terrain = new Shape(path, Color.Black);
            terrain.Scale(TileSize);

            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    terrain.verts[gridDimension * (i) + j].pos.Y += (float)Math.Pow(Math.E, heights[i, j]) * HeightMulti * meter;

                    Color col = terrain.verts[gridDimension * i + j].color;
                    terrain.verts[gridDimension * i + j].color = Color.FromArgb(col.R, (col.G + (int)Math.Min((int)(heights[i, j] / Math.Sqrt(2) * 255), 255)) / 2, col.B);
                    //terrain.verts[256 * i + j].color = Color.FromArgb(0,0,0);
                }
            }

            Tile[,] track = new Tile[8, 8];
            Tile.GenerateTrack(ref track);

            int trackMin = 3;
            int trackMax = 12;
            int trackGrid = 16;

            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    int x = i / trackGrid;
                    int y = j / trackGrid;

                    if (x > 7)
                    {
                        x = 7;
                    }
                    if (y > 7)
                    {
                        y = 7;
                    }
                    bool a = false;

                    if ("|" == track[y, x].name && (i % trackGrid > trackMin && i % trackGrid < trackMax))
                    {
                        a = true;
                    }
                    else if ("-" == track[y, x].name && (j % trackGrid > trackMin && j % trackGrid < trackMax))
                    {
                        a = true;
                    }
                    else if ("L" == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid < trackMax) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid > trackMin)))
                    {
                        a = true;
                    }
                    else if ("7" == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid > trackMin) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid < trackMax)))
                    {
                        a = true;
                    }
                    else if ("J" == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid < trackMax) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid < trackMax)))
                    {
                        a = true;
                    }
                    else if ("F" == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid > trackMin) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid > trackMin)))
                    {
                        a = true;
                    }
                    //if ("|-LJF7".Contains(track[y, x].name))
                    //{
                    //    a = true;
                    //}

                    if (a)
                    {
                        terrain.verts[gridDimension * i + j].pos.Y += .2f;
                    }
                    else
                    {
                        terrain.verts[gridDimension * i + j].pos.Y -= 2f;
                    }
                }
                terrain.resetBuffers();
            }

        }
        public class Tile
        {
            public int x;
            public int y;

            public string Up;
            public string Down;
            public string Left;
            public string Right;

            public string name;

            public static Dictionary<string, string[]> lookup = new Dictionary<string, string[]>()
            {
                { "Air",new string[]
                {
                    "x",
                    "x",
                    "x",
                    "x"
                }
                },

                { "|",new string[]
                {
                    " ",
                    " ",
                    "x",
                    "x"
                }
                },

                { "-",new string[]
                {
                    "x",
                    "x",
                    " ",
                    " "
                }
                },

                { "L",new string[]
                {
                    " ",
                    "x",
                    "x",
                    " "
                }
                },

                { "7",new string[]
                {
                    "x",
                    " ",
                    " ",
                    "x"
                }
                },

                { "F",new string[]
                {
                    "x",
                    " ",
                    "x",
                    " "
                }
                },

                { "J",new string[]
                {
                    " ",
                    "x",
                    " ",
                    "x"
                }
                },
            };

            public List<string> possibilities = new List<string>();

            public Tile(string name)
            {
                this.name = name;
            }

            static void TrackOutline(ref Tile[,] track)
            {
                for (int i = 0; i < track.GetLength(0); i++)
                {
                    for (int j = 0; j < track.GetLength(1); j++)
                    {
                        if (i == 0 || j == 0 || i == track.GetLength(0) - 1 || j == track.GetLength(1) - 1)
                        {
                            track[i, j] = new Tile("Air");
                            track[i, j].Up = "x";
                            track[i, j].Down = "x";
                            track[i, j].Left = "x";
                            track[i, j].Right = "x";
                            track[i, j].x = j;
                            track[i, j].y = i;
                        }
                        else
                        {
                            track[i, j] = null;
                        }
                    }
                }
            }

            static void GeneratePossibilities(ref List<Tile> tiles, ref Tile[,] track)
            {
                for (int i = 1; i < track.GetLength(0) - 1; i++)
                {
                    for (int j = 1; j < track.GetLength(0) - 1; j++)
                    {
                        string up = "";
                        string down = "";
                        string left = "";
                        string right = "";

                        if (track[i, j] == null)
                        {
                            //up
                            if (track[i - 1, j] == null)
                            {
                                up = "y";
                            }
                            else if (track[i - 1, j].Down == "x")
                            {
                                up = "x";
                            }
                            else
                            {
                                up = " ";
                            }

                            //down
                            if (track[i + 1, j] == null)
                            {
                                down = "y";
                            }
                            else if (track[i + 1, j].Up == "x")
                            {
                                down = "x";
                            }
                            else
                            {
                                down = " ";
                            }

                            //left
                            if (track[i, j + 1] == null)
                            {
                                right = "y";
                            }
                            else if (track[i, j + 1].Left == "x")
                            {
                                right = "x";
                            }
                            else
                            {
                                right = " ";
                            }

                            //right
                            if (track[i, j - 1] == null)
                            {
                                left = "y";
                            }
                            else if (track[i, j - 1].Right == "x")
                            {
                                left = "x";
                            }
                            else
                            {
                                left = " ";
                            }

                            tiles.Add(new Tile("Unassaigned"));
                            tiles.Last().x = j;
                            tiles.Last().y = i;

                            //air
                            if ((up == "y" || up == "x") && (down == "y" || down == "x") && (left == "y" || left == "x") && (right == "y" || right == "x"))
                            {
                                tiles.Last().possibilities.Add("Air");
                            }

                            //|
                            if ((up == "y" || up == " ") && (down == "y" || down == " ") && (left == "y" || left == "x") && (right == "y" || right == "x"))
                            {
                                tiles.Last().possibilities.Add("|");
                            }

                            //-
                            if ((up == "y" || up == "x") && (down == "y" || down == "x") && (left == "y" || left == " ") && (right == "y" || right == " "))
                            {
                                tiles.Last().possibilities.Add("-");
                            }

                            //L
                            if ((up == "y" || up == " ") && (down == "y" || down == "x") && (left == "y" || left == "x") && (right == "y" || right == " "))
                            {
                                tiles.Last().possibilities.Add("L");
                            }

                            //7
                            if ((up == "y" || up == "x") && (down == "y" || down == " ") && (left == "y" || left == " ") && (right == "y" || right == "x"))
                            {
                                tiles.Last().possibilities.Add("7");
                            }

                            //F
                            if ((up == "y" || up == "x") && (down == "y" || down == " ") && (left == "y" || left == "x") && (right == "y" || right == " "))
                            {
                                tiles.Last().possibilities.Add("F");
                            }

                            //J
                            if ((up == "y" || up == " ") && (down == "y" || down == "x") && (left == "y" || left == " ") && (right == "y" || right == "x"))
                            {
                                tiles.Last().possibilities.Add("J");
                            }
                        }
                    }
                }
            }

            public static void GenerateTrack(ref Tile[,] track)
            {

                do
                {
                    TrackOutline(ref track);

                    Random rand = new Random();

                    int z = rand.Next(1, track.GetLength(1) - 2);
                    int w = rand.Next(2, track.GetLength(0) - 4);
                    track[w, z] = new Tile("|");
                    track[w, z].Up = " ";
                    track[w, z].Down = " ";
                    track[w, z].Left = "x";
                    track[w, z].Right = "x";

                    while (true)
                    {
                        List<Tile> tiles = new List<Tile>();

                        GeneratePossibilities(ref tiles, ref track);


                        if (tiles.Count != 0)
                        {
                            Tile A = tiles[rand.Next(0, tiles.Count)];
                            int minPossibilities = A.possibilities.Count;

                            for (int i = 0; i < tiles.Count; i++)
                            {
                                if (tiles[i].possibilities.Count < A.possibilities.Count)
                                {
                                    A = tiles[i];
                                    minPossibilities = A.possibilities.Count;
                                }
                            }
                            A = tiles.Where(x => x.possibilities.Count == minPossibilities).ToArray()[rand.Next(0, tiles.Where(x => x.possibilities.Count == minPossibilities).Count())];


                            //A.name = A.possibilities[rand.Next(0, A.possibilities.Count)];
                            foreach (string possibility in A.possibilities)
                            {
                                if (possibility == "Air")
                                {
                                    A.name = "Air";
                                }
                            }
                            if (A.name == "Unassaigned")
                            {
                                if (A.possibilities.Count() == 0)
                                {
                                    break;
                                }
                                else
                                {
                                    A.name = A.possibilities[rand.Next(0, A.possibilities.Count)];
                                }
                            }


                            A.Up = Tile.lookup[A.name][0];
                            A.Down = Tile.lookup[A.name][1];
                            A.Left = Tile.lookup[A.name][2];
                            A.Right = Tile.lookup[A.name][3];
                            track[A.y, A.x] = A;
                        }
                        else
                        {
                            break;
                        }

                    }





                } while (trackValid(ref track) == false);

                //do regular track stuff to models in 3d
            }
            static bool trackValid(ref Tile[,] track)
            {
                int cornerCount = 0;
                int tracklength = 0;
                for (int i = 0; i < track.GetLength(0); i++)
                {
                    for (int j = 0; j < track.GetLength(1); j++)
                    {
                        if (track[i, j] == null)
                        {
                            return false;
                        }
                        if ("|-7FLJ".Contains(track[i, j].name))
                        {
                            tracklength++;
                        }
                        if ("7FLJ".Contains(track[i, j].name))
                        {
                            cornerCount++;
                        }
                    }
                }
                if (cornerCount < 7)
                {
                    return false;
                }
                else if (tracklength < 11)
                {
                    return false;
                }
                return true;
            }
        }
        public class Perlin
        {
            public static float[,] DoPerlin(float[,] c, float Ox, float Oy)
            {
                float[,] a = new float[c.GetLength(0), c.GetLength(1)];
                for (int i = 0; i < a.GetLength(0); i++)
                {
                    for (int j = 0; j < a.GetLength(1); j++)
                    {
                        float x = Ox + i / (float)a.GetLength(0);
                        float y = Oy + j / (float)a.GetLength(1);

                        a[i, j] = EvaluateFBM(x, y, 1, 1, 3, 0.5f, 2f);
                        a[i, j] = (float)Math.Pow(a[i, j], 1.3f);
                    }
                }

                /*float max = 0;
                for (int i = 0; i < a.GetLength(0); i++)
                {
                    for (int j = 0; j < a.GetLength(1); j++)
                    {
                        if (a[i, j] > max)
                        {
                            max = a[i, j];
                        }
                    }
                }
                float min = 1;
                for (int i = 0; i < a.GetLength(0); i++)
                {
                    for (int j = 0; j < a.GetLength(1); j++)
                    {
                        if (a[i, j] < min)
                        {
                            min = a[i, j];
                        }
                    }
                }

                for (int i = 0; i < a.GetLength(0); i++)
                {
                    for (int j = 0; j < a.GetLength(1); j++)
                    {
                        a[i, j] = (a[i, j] - min) / (max - min);
                    }
                }*/
                return a;
            }
            static float EvaluateFBM(float x, float y, float amplitude, float frequency, int octaveCount, float persistence, float lacunarity)
            {
                float value = 0;

                for (int i = 0; i < octaveCount; i++)
                {
                    value += amplitude * perlin(x * frequency, y * frequency);
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                return value;
            }

            static float interpolate(float a0, float a1, float w)
            {
                if (0.0 > w) return a0;
                if (1.0 < w) return a1;

                //return a1 * w + (1 - w) * a0;
                return (float)((a1 - a0) * (3.0 - w * 2.0) * w * w + a0);
            }

            static Vector2 randomGradient(int ix, int iy)
            {
                ix = ix % 64;
                iy = iy % 64;
                // No precomputed gradients mean this works for any number of grid coordinates
                const long w = 8 * sizeof(long);
                const long s = w / 2;
                long a = (long)ix, b = (long)iy;
                a *= 3284157443; b ^= (int)a << (int)s | (int)a >> (int)w - (int)s;
                b *= 1911520717; a ^= (int)b << (int)s | (int)b >> (int)w - (int)s;
                a *= 2048419325;
                float random = a * (float)(3.14159265 / ~(~0u >> 1)); // in [0, 2*Pi]
                Vector2 v;
                v.X = (float)Math.Cos(random); v.Y = (float)Math.Sin(random);
                return v;
            }

            static float DotProduct(int ix, int iy, float x, float y)
            {
                Vector2 gradient = randomGradient(ix, iy);

                float dx = x - (float)ix;
                float dy = y - (float)iy;

                return (dx * gradient.X + dy * gradient.Y);
            }

            public static float perlin(float x, float y)
            {
                int x0 = (int)Math.Floor(x);
                int x1 = x0 + 1;
                int y0 = (int)Math.Floor(y);
                int y1 = y0 + 1;

                float sx = x - (float)x0;
                float sy = y - (float)y0;

                float n0, n1, ix0, ix1, value;

                n0 = DotProduct(x0, y0, x, y);
                n1 = DotProduct(x1, y0, x, y);
                ix0 = interpolate(n0, n1, sx);

                n0 = DotProduct(x0, y1, x, y);
                n1 = DotProduct(x1, y1, x, y);
                ix1 = interpolate(n0, n1, sx);

                value = interpolate(ix0, ix1, sy);

                return value + 0.5f;
            }
        }
    }
}

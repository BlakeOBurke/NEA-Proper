using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OpenTk26_3
{
    

    public class Game : GameWindow
    {
        //self explanatory static variables

        static bool replay;
        static List<string> replayInputs;
        static List<string> recordInputs = new List<string>();
        static bool ghost;

        static int screenHeight;
        static int screenWidth;

        static int randomSeed;
        static Random rnd;
        static int mouseX, mouseY;
        static int mouseScroll = 0;
        static bool quickMov = false;
        static int frameCount = 0;
        static int totalframeCount;
        static int targetLaps = 2;
        static bool finished = false;
        static float finishTime;

        static float maxCarAcceleration = .15f * 30;
        static float maxCarVelocity = 2f * 30;

        const float tStep = 1 / 30f;
        const float meter = 0.5f;
        const float gravity = -9.81f * 5;

        const float carScale = .9f;


        static Game.Camera camera = new Game.Camera(0, 0, 0);

        static Shader shader;
        Color ghostColor = Color.Gray;

        static Terrain landscape;
        static Terrain racetrack;


        //in the constructors there are many arbitrary re-assignments. this is because some of these are static to the Game class and would persist between games, reseting them fixes this.
        public Game(int width, int height, int seed) : base(width, height, GraphicsMode.Default, "game")
        {
            resetVariables(width,height,seed);
        }
        public Game(int width, int height, int seed, string mode) : base(width, height, GraphicsMode.Default, "game")
        {
            resetVariables(width , height,seed);

            //change stuff based on gamemode
            if(mode == "R")
            {
                replay = true;
                replayInputs = File.ReadAllLines(seed.ToString()).ToList();
                ghost = false;
            }
            else if(mode == "G")
            {
                ghost = true;
                replayInputs = File.ReadAllLines(seed.ToString()).ToList();
                replay = false;
            }
        }
        void resetVariables(int width, int height, int seed)//stuff that needs to be static to the game class but also needs to be reset between each game
        {
            if (seed % 2 == 0)
            {//grass
                Terrain.landColor = Color.FromArgb(0, 0, 180, 0);
                Terrain.trackColor = Color.DarkGreen;
            }
            else
            {//sand
                Terrain.landColor = Color.SandyBrown;
                Terrain.trackColor = Color.Black;
            }
            randomSeed = seed;
            rnd = new Random(randomSeed);
            Item.itemRand = new Random(randomSeed);
            Decoration.decorRand = new Random(randomSeed);

            screenHeight = height;
            screenWidth = width;
            finished = false;
            Item.items.Clear();
            Kart.cars.Clear();
            Shape.shapes.Clear();
            camera = new Game.Camera(0, 0, 0);
            totalframeCount = 0;
            recordInputs.Clear();
            if (replayInputs != null)
            {
                replayInputs.Clear();
            }

            replay = false;
            ghost = false;
        }
        static Color randomColor()        //self explanatory
        {
            return Color.FromArgb(255, rnd.Next(50, 205), rnd.Next(50, 205), rnd.Next(50, 205));
        }
        static vertex[] infromFile(string path)
        {//unused code to convert .ast 3D files into the program
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

        //built in OpenTK virtual function, run immediately
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            GL.ClearColor(Color.CornflowerBlue);//background colour

            shader = new Shader("shader.vert.txt", "shader.frag.txt");


            GL.Enable(EnableCap.DepthTest);//needed to make 3D

            mouseX = 0; mouseY = 0;

            Kart.cars.Add(new Kart(randomColor()));
            Kart.cars.Last().Scale(carScale);
            Kart.cars.Last().SetPos(new Vector3(0, 40, 0));

            if (ghost)
            {//in ghost mode a second car is needed, the cars and all shapes use OOP so this is simple
                Kart.cars.Add(new Kart(ghostColor));
                Kart.cars.Last().Scale(carScale);
                Kart.cars.Last().SetPos(new Vector3(0, 40, 0));
            }

            //starts the random seed for the terrain stuff, user will be promted to enter one
            Terrain.getRan();

            landscape = new Terrain();
            racetrack = new Terrain(false);

            landscape.terrain.SetPos(landscape.terrain.centre - new Vector3(320 + 128, 0, 320 + 128));
            racetrack.terrain.SetPos(racetrack.terrain.centre - new Vector3(320 + 128, 0, 320 + 128));

            Vector2 pos = Terrain2World(racetrack.startPos);
            if (ghost)
            {
                Kart.cars[1].SetPos(new Vector3(pos.X, 0, pos.Y));
            }
            Kart.cars[0].SetPos(new Vector3(pos.X, 0, pos.Y));

            //some global/ classglobal variables appear to persist between games, reseting randoms fixes this

            Item.itemRand = new Random(randomSeed);
            for (int i = 0; i < 20; i++)
            {
                Item.SpawnItem();
            }

            Decoration.decor.Clear();
            if(randomSeed%2 == 0)
            {
                int trees = Decoration.decorRand.Next(15, 45);
                for(int i  = 0; i < trees; i++)
                {
                    new Decoration("tree.obj", Color.ForestGreen);
                }
            }
            else
            {
                int trees = Decoration.decorRand.Next(15, 45);
                for (int i = 0; i < trees; i++)
                {
                    new Decoration("cactus.obj", Color.ForestGreen);
                }
                //new Decoration("cactus.obj", Color.ForestGreen);
            }
        }

        //built in OpenTK virtual function, run when the window is closed. if the track has been finished then the time and recorded inputs are sent to Program.cs to be saved in a leaderboard, if not a time of 0 is given which is caught in Program.cs
        protected override void OnUnload(EventArgs e)
        {
            if (finished)
            {
                Program.setTime(finishTime);
                Program.RecordInputs(recordInputs);
            }
            else
            {
                Program.setTime(0);
            }
            base.OnUnload(e);
            shader.Dispose();

        }

        //built in OpenTK virtual function, run 30 times per second
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            //Console.WriteLine(Kart.Cars[0].centre.X + "   " + Kart.Cars[0].centre.Z);
            base.OnUpdateFrame(e);


            totalframeCount++;
            frameCount++;
            frameCount = frameCount % 360;

            //OpenTK function to get keyboard input without needing threading or similar
            KeyboardState input = Keyboard.GetState();

            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
            }

            CarAndCamera(input);

            //check if the first car is finished
            if (Kart.cars[0].laps == targetLaps && !finished)
            {
                //based on computer performance a stopwatch would have variable time, using gameupdates is more reliable
                finishTime = totalframeCount/30f;
                float timeTest = replay ? new Program.Leaderboard(randomSeed.ToString()).Fastest().time : finishTime;
                Console.WriteLine($"Finished with a time of {timeTest} seconds!!!");
                finished = true;
            }

            HandleItems();

        }
        void CarAndCamera(KeyboardState input) //car and camera stuff for the on update frame
        {
            //if not finsihed, cars need to move
            if (!finished)
            {
                //in replay mode, use replay car to undo the replay file
                //in ghost mode, let the player drive the first car [0] and use the replay for the second car [1]
                //otherwise just let the player control the car 
                //finally set the camera to follow car[0]
                if (replay)
                {
                    Kart.cars[0].ReplayCar();
                    camera.LooseFollowCam(Kart.cars[0]);
                }
                else if (ghost)
                {
                    Kart.cars[0].DriveCar(input);
                    Kart.cars[1].ReplayCar();
                    Kart.cars[1].MoveCars(true);

                    if (totalframeCount % 3 == 0)
                    {
                        Kart.cars.Add(new Kart(ghostColor));
                        Kart.cars.Last().Scale(carScale);
                        Kart.cars.Last().SetPos(Kart.cars[1].centre);
                        Kart.cars.Last().setScale(Kart.cars[1].scale[0]);
                        Kart.cars.Last().angle = (Kart.cars[1].angle);
                        //cool trail of cars in ghost mode
                        if (Kart.cars.Count() > 6)
                        {
                            Kart.cars.RemoveAt(2);
                        }
                        for (int i = 2; i < Kart.cars.Count(); i++)
                        {
                            Kart.cars.Last().scale *= 0.997f;
                        }
                    }
                    camera.FollowCam(Kart.cars[0]);
                }
                else
                {
                    Kart.cars[0].DriveCar(input);
                    camera.FollowCam(Kart.cars[0]);
                }
                Kart.cars[0].MoveCars(false);

            }
            else
            {
                //if its finished then use the world camera to see the whole course from above
                camera.World_Cam();
            }
        }
        void HandleItems()//does everything for items in OmUpdateFrame
        {
            //in replays and standard mode the items are removed after they are used. in ghost mode, the ghost car does not use up items becuase that would affect the main players gameplay
            if ((ghost) && !finished)
            {
                try//catches the index out of range exception from the ghost car finishing before the player and so trying to get instructions when there aren't any
                {
                    string[] items = replayInputs[totalframeCount - 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Contains("_B_"))
                    {
                        Boost.Consumed(Kart.cars[1]);
                    }
                    if (items.Contains("_G_"))
                    {
                        Giant.Consumed(Kart.cars[1]);
                    }
                    if (items.Contains("_S_"))
                    {
                        Slow.Consumed(Kart.cars[1]);
                    }
                }
                catch { }

            }
            for (int i = 0; i < Item.items.Count; i++)
            {
                if (Item.items[i].Collide(Kart.cars[0]))
                {
                    Item.items[i].Consume(Kart.cars[0]);
                    i--;
                }
            }
        }

        //built in OpenTK virtual function, run 30 times per second
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            List<Shape> models = new List<Shape>();
            models.AddRange(Shape.models);

            Matrix4 camMatrix = camera.CameraMatrix();
            //for(int i = 0; i < Shape.models.Count(); i++)
            //{
            //    drawObj(Shape.models[i], camMatrix);
            //}
            foreach (Kart car in Kart.cars)
            {
                car.drawObj(camMatrix);
            }
            landscape.terrain.drawObj(camMatrix);
            racetrack.terrain.drawObj(camMatrix);
            foreach (Item item in Item.items)
            {//make the items bob up and down and spin for visual splendor 
                item.centre.Y += meter * 0.3f * (float)(Math.Sin((Math.PI / 180f) * (frameCount + item.frameOffset)) - Math.Sin((Math.PI / 180f) * (frameCount - 1 + item.frameOffset)));
                //item.shape.angle += 0.0174533f*new Vector3(1, 1, 1);
                item.angle += 0.0174533f * item.rotateOffset;

                if(VisibleHeuristic(item)) item.drawObj(camMatrix);
            }
            foreach(Decoration decor in Decoration.decor)
            {
                if(VisibleHeuristic(decor)) decor.drawObj(camMatrix);
            }

            //reset the shader
            shader.Dispose();

            this.SwapBuffers();

            //Console.WriteLine((int)RenderFrequency);
        }
        static bool VisibleHeuristic(Shape a)
        {
            //if (Math.Acos(Vector3.Dot(camera.CamForward().Normalized(), (a.centre-camera.pos).Normalized())) < camera.fov/2) return true;
            return true;
        }
        
        ////built in OpenTK virtual function, run whenever the screen is resized so that the game doesnt crash and draws to the new dimensions
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, this.Width, this.Height);
        }

        struct vertex // bit self expanatory
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
        class Camera // camera to get matrices for 3D 
        {
            //distance from car to camera in follow modes
            public float camDistance = 65;
            public float camMinDistance = 15;
            public float camMaxDistance = 65;
            public Vector3 pos;
            public Vector3 direction;
            public Vector2 Ddirection;
            public Vector3 forward;
            public float fov;
            public float tanFov;
            public Camera(float x, float y, float z)
            {
                this.pos = new Vector3(x, y, z);
                this.direction = new Vector3(0, 0, 0);
                this.Ddirection = new Vector2(0, (float)Math.PI);
                ZoomReset();
                forward = new Vector3(0, 0, 1);
            }

            public Vector3 CamForward() // the direction the camera is pointing, changing direction and forward affect this
            {
                Matrix4 rotation = Matrix4.CreateRotationZ(-direction[2]) * Matrix4.CreateRotationY(-direction[1]) * Matrix4.CreateRotationX(-direction[0]);
                Vector4 ouut = (rotation * new Vector4(forward, 1));
                return new Vector3(ouut);
            }
            //these three change FOV based on the speed and slow powerups
            public void ZoomFast()
            {
                fov = (float)(0.0174533 * 110);
                tanFov = (float)Math.Tan(fov / 2f);
            }
            public void ZoomReset()
            {
                fov = (float)(0.0174533 * 90);
                tanFov = (float)Math.Tan(fov / 2f);
            }
            public void ZoomSlow()
            {
                fov = (float)(0.0174533 * 75);
                tanFov = 1/(float)Math.Tan(fov / 2f);
            }

            Matrix4 ProjectionMatrix(float near/*distance of near plane of frustum*/, float far/*distance of far plane of frustum*/, float tanFov2, float a /*aspect ratio*/)
            {
                //most of the matrix is 0's so use built in function 
                //most of the maths is in the write up
                Matrix4 projector = Matrix4.Zero;

                projector[0, 0] = (float)((2f * near) * tanFov2);
                projector[1, 1] = (float)((a * 2f * near) * tanFov2);
                projector[2, 2] = (-far - near) / (far - near);
                projector[2, 3] = (-2f * (far * near)) / (far - near);
                projector[3, 2] = -1f;


                return projector;
            }
            public Matrix4 CameraMatrix() // the projection matrix, if the game is over its slightly different as it looks to the centre of the world
            {
                if (finished == true)
                {
                    Vector3 forww = -this.pos;
                    //forww[0] += cam.pos[0]; forww[1] += cam.pos[1]; forww[2] += cam.pos[2];
                    //MY_vector3 right = MY_vector3.cross(forw, new MY_vector3(0,-1,0)).normalise();
                    //MY_vector3 upp = MY_vector3.cross(right, forw);
                    Matrix4 camera = Matrix4.LookAt(new Vector3(this.pos[0], this.pos[1], this.pos[2]), new Vector3(forww[0], forww[1], forww[2]), new Vector3(0, 1, 0));

                    //camer.Transpose();
                    if (this.fov > Math.PI - 0.001f)
                    {
                        this.fov = (float)Math.PI - 0.001f;
                    }
                    else if (this.fov < 0.0001f)
                    {
                        this.fov = 0.0001f;
                    }
                    //camera = camera * Matrix4.CreatePerspectiveFieldOfView(cam.zooooooom, screenWidth / (float)screenHeight, 10f, 1000f);
                    camera = camera * ProjectionMatrix(.5f, 300f, this.tanFov, screenWidth / (float)screenHeight);
                    return camera;

                }


                Vector3 forw = this.CamForward();
                forw[0] += this.pos[0]; forw[1] += this.pos[1]; forw[2] += this.pos[2];
                //MY_vector3 right = MY_vector3.cross(forw, new MY_vector3(0,-1,0)).normalise();
                //MY_vector3 upp = MY_vector3.cross(right, forw);
                Matrix4 camer = Matrix4.LookAt(new Vector3(this.pos[0], this.pos[1], this.pos[2]), new Vector3(forw[0], forw[1], forw[2]), new Vector3(0, 1, 0));

                //camer.Transpose();

                //camer = camer * Matrix4.CreatePerspectiveFieldOfView(cam.zooooooom, screenWidth/(float)screenHeight , 3.5f, 1000f);
                camer = camer * ProjectionMatrix(.5f, 500f, this.tanFov, screenWidth / (float)screenHeight);
                return camer;
            }
            public void FreeCam(KeyboardState input)
            {
                if (input.IsKeyDown(Key.W))
                {
                    Vector3 mov = this.CamForward();

                    if (quickMov)
                    {
                        mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                    }


                    this.pos += new Vector3(mov[0], mov[1], mov[2]);
                }
                if (input.IsKeyDown(Key.S))
                {
                    Vector3 mov = this.CamForward();

                    if (quickMov)
                    {
                        mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                    }

                    this.pos += new Vector3(-mov[0], -mov[1], -mov[2]);

                }
                if (input.IsKeyDown(Key.A))
                {
                    Vector3 mov = Vector3.Cross(this.CamForward(), new Vector3(0, -1, 0));

                    if (quickMov)
                    {
                        mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                    }

                    this.pos += (new Vector3(mov[0], mov[1], mov[2]));

                }
                if (input.IsKeyDown(Key.D))
                {
                    Vector3 mov = Vector3.Cross(this.CamForward(), new Vector3(0, -1, 0));

                    if (quickMov)
                    {
                        mov = new Vector3(mov[0] * 10, mov[1] * 10, mov[2] * 10);
                    }

                    this.pos += new Vector3(-mov[0], -mov[1], -mov[2]);
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
                    camera.fov -= 0.00174533f * 4;
                }
                else if (input.IsKeyDown(Key.G))
                {
                    camera.fov += 0.00174533f * 4;
                }
            } //camera lets you go anywhere, mostly left over from development
            public void FreeMouse()
            {
                MouseState moose = Mouse.GetState();
                this.direction[1] -= (moose.X - mouseX) * 0.001f;
                mouseX = moose.X;

                this.direction[0] += (moose.Y - mouseY) * 0.001f;
                mouseY = moose.Y;

                if (this.direction[0] > Math.PI / 2 - 0.05f)
                {
                    this.direction[0] = (float)Math.PI / 2 - 0.05f;
                }
                else if (this.direction[0] < -Math.PI / 2 + 0.05f)
                {
                    this.direction[0] = (float)-Math.PI / 2 + 0.05f;
                }


            } //lets you spin the mouse around freely
            public void FollowCam(Kart car)
            {
                MouseState moose = Mouse.GetState();
                this.direction = new Vector3((float)Math.PI / 10f, (float)Math.Atan2(-car.getForward().X, -car.getForward().Z), 0f);


                this.camDistance -= (moose.ScrollWheelValue + mouseScroll);
                mouseScroll = -moose.ScrollWheelValue;
                if (this.camDistance > this.camMaxDistance) { this.camDistance = this.camMaxDistance; }
                if (this.camDistance < this.camMinDistance) { this.camDistance = this.camMinDistance; }

                this.pos = car.centre - this.CamForward() * this.camDistance * (car.scale.Length / 3f) /**(1f+car.velocity.Length/4)*/;
            } //camera follows the car, can't change what way the camera faces
            public void World_Cam()
            {
                float radius = 600f * meter;
                this.pos.X = radius * (float)Math.Cos((Math.PI / 180f) * (0.5f) * totalframeCount);
                this.pos.Z = radius * (float)Math.Sin((Math.PI / 180f) * (0.5f) * totalframeCount);
                Collision(landscape, new Vector3(this.pos.X, -100, this.pos.Z), out float height, out Vector3 normal);
                this.pos.Y = height + 150f;


                this.forward = new Vector3(this.pos.Normalized().X, 0, this.pos.Normalized().Z);
            } //camera maintains alltitude above the ground looking towards the world origin <-- uses polar curves :O
            public void FreeFollowCam(Kart car)
            {
                //cam.pos = car.centre;


                MouseState moose = Mouse.GetState();
                this.direction[1] -= (moose.X - mouseX) * 0.001f;
                mouseX = moose.X;

                this.direction[0] += (moose.Y - mouseY) * 0.001f;
                mouseY = moose.Y;

                if (this.direction[0] > Math.PI / 2 - 0.05f)
                {
                    this.direction[0] = (float)Math.PI / 2 - 0.05f;
                }
                else if (this.direction[0] < -Math.PI / 2 + 0.05f)
                {
                    this.direction[0] = (float)-Math.PI / 2 + 0.05f;
                }
                this.pos = car.centre - this.CamForward() * this.camDistance;
            } //camera follows the car but can look in any direction
            public void LooseFollowCam(Kart car)
            {
                MouseState moose = Mouse.GetState();
                this.Ddirection[1] -= (moose.X - mouseX) * 0.001f;
                mouseX = moose.X;

                this.Ddirection[0] += (moose.Y - mouseY) * 0.001f;
                mouseY = moose.Y;

                //cam.direction.Y = (float)Math.Atan2(car.getForward().X, car.getForward().Z);
                //Console.WriteLine(moose.ScrollWheelValue);
                //Console.WriteLine(MOUSEscroll);

                this.camDistance -= (moose.ScrollWheelValue + mouseScroll);
                mouseScroll = -moose.ScrollWheelValue;
                if (this.camDistance > this.camMaxDistance) { this.camDistance = this.camMaxDistance; }
                if (this.camDistance < this.camMinDistance) { this.camDistance = this.camMinDistance; }

                this.direction = new Vector3(this.Ddirection.X, this.Ddirection.Y + (float)Math.Atan2(car.getForward().X, car.getForward().Z), 0f);

                this.pos = car.centre - this.CamForward() * this.camDistance * (car.scale.Length / 3f) /**(1f+car.velocity.Length/4)*/;

            } //camera follows the car but the direction of the camera is fixed relative to the car
        }

        //a shader is just a function for the gpu to be run in parralel
        //my program has a vertex shader for every vertex and a fragment shader for every pixel
        class Shader //shader stuff, using OpenTK tutorial because it has to be quite exact
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
                    Console.WriteLine("GPU Resource leak");
                }
            }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        static Vector2 Terrain2World(Vector2 pos)
        {
            pos = pos * Terrain.squareSize;

            return pos;
        } //takes the wave function collapse track info into the 3D space

        class Shape // Shape, many functions. Buffers are RAM on the GPU for storing triangles
        {
            public Vector3 angle;
            public int count;
            public vertex[] verts;
            public uint[] triangle;
            public Vector3 direction = new Vector3(0, 0, 1);
            public Vector3 scale = new Vector3(1, 1, 1);
            public Vector3 velocity;
            public Vector3 normal;

            public Vector3 centre;
            public Vector3 dimensions;

            public static List<int> shapes = new List<int>();
            public static List<Shape> models = new List<Shape>();


            public int vertexBufferObject;
            public int elementBufferObject;
            public int vertexArrayObject;

            public void drawObj(Matrix4 proj)
            {//proj is the projection matrix, the same for all objects
                Matrix4 model = this.modelMat();
                //get the model matrix

                Matrix4 aproj = Matrix4.CreateScale(this.scale) * model * proj;
                //using matrix multiplication to apply the transformations

                int uniID = GL.GetUniformLocation(3, "projection");

                //Give the matrix to the GPU
                GL.UniformMatrix4(uniID, true, ref aproj);



                //instruct the GPU on what data to draw
                GL.BindVertexArray(this.vertexArrayObject);

                GL.BindBuffer(BufferTarget.ArrayBuffer, this.vertexBufferObject);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, this.elementBufferObject);

                //defines how data is sent to the GPU, 6* 32 bit numbers, a 3*32 bits for colour, 3*32 bits for location
                GL.VertexAttribPointer(this.vertexBufferObject, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                GL.EnableVertexAttribArray(this.vertexBufferObject);

                GL.VertexAttribPointer(this.elementBufferObject, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(this.elementBufferObject);

                //gpu things
                shader.Use();
                GL.DrawElements(PrimitiveType.Triangles, this.count, DrawElementsType.UnsignedInt, 0);
            } //draw stuff

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
                        ver.Add(new vertex(new Vector3(float.Parse(point[1]), float.Parse(point[2]), float.Parse(point[3])), Color.FromArgb(255, Math.Min(Math.Abs((int)col[0] + Game.rnd.Next(-50, 50)), 255), Math.Min(Math.Abs((int)col[1] + Game.rnd.Next(-50, 50)), 255), Math.Min(Math.Abs((int)col[2] + Game.rnd.Next(-50, 50)), 255))));
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
            public Shape(string path, Color color, int ColorVariation)
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
                        ver.Add(new vertex(new Vector3(float.Parse(point[1]), float.Parse(point[2]), float.Parse(point[3])), Color.FromArgb(255, Math.Min(Math.Abs((int)col[0] + Game.rnd.Next(-ColorVariation, ColorVariation)), 255), Math.Min(Math.Abs((int)col[1] + Game.rnd.Next(-ColorVariation, ColorVariation)), 255), Math.Min(Math.Abs((int)col[2] + Game.rnd.Next(-ColorVariation, ColorVariation)), 255))));
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

                this.vertexBufferObject = GL.GenBuffer();
                this.elementBufferObject = GL.GenBuffer();
                this.vertexArrayObject = GL.GenVertexArray();

                resetBuffers();
            }
            public void resetBuffers()
            {
                float[] vertices = GetFloat();
                uint[] indices = triangle;


                GL.BindVertexArray(vertexArrayObject);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);


                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);
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
            public virtual void setScale(float scale)
            {
                this.scale = new Vector3(scale,scale,scale);
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

            public virtual Matrix4 modelMat()
            {
                Shape shapee = this;
                Matrix4 mov = Matrix4.CreateRotationZ(this.angle[2]) * Matrix4.CreateRotationY(this.angle[1]) * Matrix4.CreateRotationX(this.angle[0]) * Matrix4.CreateTranslation(this.centre);
                return mov;
            }

            public Vector3 getForward()
            {
                Matrix3 rotation = Matrix3.CreateRotationZ(-angle[2]) * Matrix3.CreateRotationY(-angle[1]) * Matrix3.CreateRotationX(-angle[0]);
                Vector3 ouut = (rotation * new Vector3(0, 0, 1));
                return ouut;
            }
        }

        abstract class Item : Shape // items to modify gameplay
        {
            public static Random itemRand;
            public int frameOffset;
            public Vector3 rotateOffset;
            public Item(string a, Color b):base(a,b)
            {
                //this.shape = new Shape(a, b);

                items.Add(this);
                this.Scale(meter * 2);
                //place on track function

                if (!PlaceItem())
                {
                    this.Delete();
                }
                this.centre.Y += 2 * meter;

                frameOffset = itemRand.Next(0, 360); 
                rotateOffset = new Vector3((float)itemRand.NextDouble(), (float)itemRand.NextDouble(), (float)itemRand.NextDouble());
                //Console.WriteLine(this.shape.centre.X + " " + this.shape.centre.Z);
            }

            public virtual void Consume(Kart car)
            {
                items.Remove(this);
            }
            public virtual void Delete()
            {
                items.Remove(this);
            }
            public bool Collide(Shape shape)
            {
                if ((shape.centre - this.centre).Length < this.scale.Length * 2)
                {
                    return true;
                }
                return false;
            }

            public static List<Item> items = new List<Item>();

            public static void SpawnItem()
            {
                switch (itemRand.Next(0, 4))
                {
                    case 0:
                        items.Add(new Boost());
                        break;
                    case 1:
                        items.Add(new Slow());
                        break;
                    case 2:
                        items.Add(new Giant());
                        break;
                }
                //Items.Add(new Microplastic());
            }
            public bool PlaceItem()
            {
                bool tooClose = false;
                int tryCount = 0;
                while (true)
                {
                    centre = new Vector3(itemRand.Next(-Terrain.gridDimension / 2 * Terrain.squareSize, Terrain.gridDimension / 2 * Terrain.squareSize), 0, itemRand.Next(-Terrain.gridDimension / 2 * Terrain.squareSize, Terrain.gridDimension / 2 * Terrain.squareSize));

                    Game.Collide(landscape, this, out float gHeight);
                    Game.Collide(racetrack, this, out float tHeight);
                    if (tHeight > gHeight)
                    {
                        centre.Y = dimensions.Y * 2 + tHeight;
                        foreach (Item item in items)
                        {
                            if (item != this)
                            {
                                if ((item.centre - this.centre).Length < 100 * meter)
                                {
                                    tooClose = true;
                                }
                            }
                        }
                        if (tryCount > 10)
                        {
                            return false;
                        }
                        if (!tooClose)
                        {
                            return true;
                        }
                        tryCount++;
                        tooClose = false;
                    }
                }
            }
            public void JiggleItems(ref List<Shape> models)
            {
                foreach (Item item in Item.items)
                {//make the items bob up and down and spin for visual splendor 
                    item.centre.Y += meter * 0.3f * (float)(Math.Sin((Math.PI / 180f) * (frameCount + item.frameOffset)) - Math.Sin((Math.PI / 180f) * (frameCount - 1 + item.frameOffset)));
                    //item.shape.angle += 0.0174533f*new Vector3(1, 1, 1);
                    item.angle += 0.0174533f * item.rotateOffset;

                    models.Add(item);
                }
            }
        }

        class Boost /*ZOOOOOOOOM*/ : Item
        {
            public Boost(string a = "Cube.obj") : base(a, Color.DarkRed)
            {

            }
            public override void Consume(Kart car)
            {
                car.boostDirection = 1;
                car.boost = (1 * (int)(1 / tStep));
                if (!replay)
                {
                    recordInputs[totalframeCount - 1] += "_B_ ";
                }
                base.Consume(car);
            }
            public static void Consumed(Kart car)
            {
                car.boostDirection = 1;
                car.boost = (1 * (int)(1 / tStep));
            }

        }
        class Slow /*not ZOOOOOOOOM*/ : Item
        {
            public Slow(string a = "Cube.obj") : base(a, Color.DarkMagenta)
            {

            }
            public override void Consume(Kart car)
            {
                if (car.big <= 0)
                {
                    car.boostDirection = -1;
                    car.boost = (1 * (int)(1 / tStep));
                    if (!replay)
                    {
                        recordInputs[totalframeCount - 1] += "_S_ ";
                    }
                    base.Consume(car);
                }
                else
                {
                    base.Consume(car);
                }
            }
            public static void Consumed(Kart car)
            {
                if (car.big <= 0)
                {
                    car.boostDirection = -1;
                    car.boost = (1 * (int)(1 / tStep));
                }
            }

        }
        class Giant /*LAAAAAAARRGE*/ : Item
        {
            public Giant(string a = "Cube.obj") : base(a, Color.LightCyan)
            {

            }
            public override void Consume(Kart car)
            {
                car.big = (2 * (int)(1 / tStep));
                if (!replay)
                {
                    recordInputs[totalframeCount - 1] += "_G_ ";
                }
                base.Consume(car);
            }
            public static void Consumed(Kart car)
            {
                car.big = (2 * (int)(1 / tStep));
            }

        }

        class Decoration : Shape
        {
            public static Random decorRand = new Random(randomSeed);
            public static List<Decoration> decor = new List<Decoration>();
            public Decoration(string path, Color color) : base(path, color)
            {
                if (PlaceItem())
                {
                    decor.Add(this);
                }

               this.Scale(10f + (float)rnd.NextDouble());
            }

            public bool PlaceItem()// same as Item.PlaceItem except on path instead of track
            {
                bool tooClose = false;
                int tryCount = 0;
                while (true)
                {
                    this.centre = new Vector3(decorRand.Next(-Terrain.gridDimension / 2 * Terrain.squareSize, Terrain.gridDimension / 2 * Terrain.squareSize), 0, decorRand.Next(-Terrain.gridDimension / 2 * Terrain.squareSize, Terrain.gridDimension / 2 * Terrain.squareSize));

                    Collide(landscape, this, out float gHeight);
                    Collide(racetrack, this, out float tHeight);
                    if (tHeight < gHeight)
                    {
                        this.centre.Y = this.dimensions.Y * 2 + tHeight;
                        foreach (Decoration a in decor)
                        {
                            if (a != this)
                            {
                                if ((a.centre - this.centre).Length < 100 * meter)
                                {
                                    tooClose = true;
                                }
                            }
                        }
                        if (tryCount > 10)
                        {
                            return false;
                        }
                        if (!tooClose)
                        {
                            return true;
                        }
                        tryCount++;
                        tooClose = false;
                    }
                }
            }
        }
        class Kart /*Kachow*/ : Shape 
        {
            public int big = 0;
            public bool bigged = false;
            public int boost = 0;
            public int boostDirection = 0;
            public float velocityMulti = 1f;
            public bool onGrass = false;
            public char checkState = 'S';
            public int laps = 0;
            static float turnSpeed = 0.5f;
            static float turnAngle = 0.0325f;
            public static List<Kart> cars = new List<Kart>();
            public Kart(Color color) : base("Kart.obj", color) { GetDimension(); normal = new Vector3(0, 1, 0); }
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
            public override Matrix4 modelMat() // different modelmatrix to make the car collide with terrain properly
            {
                Vector3 up = this.normal.Normalized();
                Vector3 right = Vector3.Cross(up, this.getForward()).Normalized();
                Vector3 forward = Vector3.Cross(up, right).Normalized();
                Matrix4 mover = new Matrix4(
                    new Vector4(right, 0),
                    new Vector4(up, 0),
                    new Vector4(forward, 0),
                    new Vector4(0, 0, 0, 1)
                    );
                Matrix4 mov = mover * Matrix4.CreateTranslation(this.centre);
                //Matrix4 mov = Matrix4.CreateRotationY(shapee.angle[1]) * Matrix4.CreateRotationX(shapee.angle[0]) * Matrix4.CreateRotationZ(shapee.angle[2]) * Matrix4.CreateTranslation(shapee.centre);
                return mov;
            }

            public void ReplayCar()
            {
                try
                {
                    string[] inputs = replayInputs[totalframeCount - 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (inputs.Contains("W"))
                    {
                        if (inputs.Contains("S"))
                        {
                            velocity -= getForward() * maxCarAcceleration * 0.5f;
                        }
                        else
                        {
                            velocity -= getForward() * maxCarAcceleration;
                        }
                    }
                    else if (inputs.Contains("S"))
                    {
                        velocity += getForward() * maxCarAcceleration;
                    }

                    if (inputs.Contains("A"))
                    {
                        if (velocity.Length > turnSpeed)
                        {
                            angle.Y += boostDirection == 1 ? turnAngle * velocity.Length * tStep * 1.5f : turnAngle * velocity.Length * tStep;
                        }
                        velocityMulti = 0.85f;
                    }
                    else if (inputs.Contains("D"))
                    {
                        if (velocity.Length > turnSpeed)
                        {
                            angle.Y -= boostDirection == 1 ? turnAngle * velocity.Length * tStep * 1.5f : turnAngle * velocity.Length * tStep;
                        }
                        velocityMulti = 0.85f;
                    }
                    else
                    {
                        velocityMulti = 1;
                    }
                }
                catch
                {

                }
            } //similar to DriveCar but uses text file of replay as a substitute for keyboard input
            public void DriveCar(KeyboardState input)
            {
                recordInputs.Add(" ");
                if (input.IsKeyDown(Key.Up) || input.IsKeyDown(Key.W))
                {
                    if (input.IsKeyDown(Key.S) || input.IsKeyDown(Key.Down))
                    {
                        velocity -= getForward() * maxCarAcceleration * 0.5f;
                        recordInputs[totalframeCount - 1] += "W S ";
                    }
                    else
                    {
                        velocity -= getForward() * maxCarAcceleration;
                        recordInputs[totalframeCount - 1] += "W ";
                    }
                }
                else if (input.IsKeyDown(Key.Down) || input.IsKeyDown(Key.S))
                {
                    velocity += getForward() * maxCarAcceleration;
                    recordInputs[totalframeCount - 1] += "S ";
                }

                if (input.IsKeyDown(Key.Right) || input.IsKeyDown(Key.D))
                {
                    if (velocity.Length > turnSpeed)
                    {
                        angle.Y -= boostDirection == 1 ? turnAngle * velocity.Length * tStep * 1.5f : turnAngle * velocity.Length * tStep;
                    }
                    velocityMulti = 0.85f;
                    recordInputs[totalframeCount - 1] += "D ";
                }

                else if (input.IsKeyDown(Key.Left) || input.IsKeyDown(Key.A))
                {
                    if (velocity.Length > turnSpeed)
                    {
                        angle.Y += boostDirection == 1 ? turnAngle * velocity.Length * tStep * 1.5f : turnAngle * velocity.Length * tStep;
                    }
                    velocityMulti = 0.85f;
                    recordInputs[totalframeCount - 1] += "A ";
                }
                else
                {
                    velocityMulti = 1;
                }
            } //affects the cars velocity based on keyboard inputs
            public void MoveCars(bool IsGhost) //terrain collision + checkpoint + speed modifiers + actually moving the car 
            {
                //get the height of the racectrack and the landscape
                //get the angle from the landscape
                //based on what height is higher the car is either on or off the grass
                //(2x 128*128 grids of squares. based on the track that's generated by wave function collapse the areas of the 'track' grid are 'pulled up' through the 'landscape' grid so the method to determine on track or not works)
                Collide(racetrack, this/*, out Vector2 raceAngle,*/ , out float tHeight);
                Collide(landscape, this/*, out Vector2 grassAngle,*/ , out float lHeight);

                //do some gravity yeah
                centre.Y += gravity * tStep;

                if (tHeight + dimensions.Y / 2f + 0.3f > centre.Y || lHeight + dimensions.Y / 2f + 0.3f > centre.Y)
                {
                    centre.Y = (float)Math.Max(lHeight, tHeight) + dimensions.Y / 2f;
                }
                //actual ongrass check, this will affect speed later
                if (lHeight > tHeight)
                {
                    onGrass = true;
                }
                else
                {//check checkpoints
                    onGrass = false;

                    Vector2 carPos = centre.Zx;
                    Vector2 Checkpos = Terrain2World(racetrack.checkpointPos);
                    Vector2 Startpos = Terrain2World(racetrack.startPos);
                    Checkpos = Checkpos.Yx;
                    Startpos = Startpos.Yx;
                    //fix the startpos and checkpoint pos to align them to world grid


                    //check some funky dot products to see if the car is in a square of the checkpoint or start
                    //^generate vectors from opposite corners of a square to a point (the cars location). if those vectors point 'towards' each other the point is in the square. if two vectors point towards each other the dot product is < 1.
                    //(not proving the maths so please just trust me bro)
                    if (!IsGhost) //the ghost will not be able to activate checkpoints
                    {
                        if (Vector2.Dot(carPos - (Checkpos - (Terrain.gridDimension / (2 * Terrain.trackSize)) * new Vector2(Terrain.squareSize, Terrain.squareSize) * 0.65f), carPos - (Checkpos + (Terrain.gridDimension / (2 * Terrain.trackSize)) * new Vector2(Terrain.squareSize, Terrain.squareSize) * 0.65f)) < 1)
                        {
                            if (checkState == 'S')
                            {
                                Console.WriteLine("hit checkpoint in" + " " + totalframeCount/30f + " seconds");
                                checkState = 'C';
                            }
                        }
                        if (Vector2.Dot(carPos - (Startpos - (Terrain.gridDimension / (2 * Terrain.trackSize)) * new Vector2(Terrain.squareSize, Terrain.squareSize) * 0.65f), carPos - (Startpos + (Terrain.gridDimension / (2 * Terrain.trackSize)) * new Vector2(Terrain.squareSize, Terrain.squareSize) * 0.65f)) < 1)
                        {
                            if (checkState == 'C')
                            {
                                laps++;
                                Console.WriteLine(laps + "/" + targetLaps + " laps in" + " " + totalframeCount / 30f + " seconds");
                                checkState = 'S';
                            }
                        }
                    }

                }

                //car slows down over time, basically this is friction
                velocity *= 0.95f;

                float velocity_multi = this.velocityMulti;

                //if car on grass AND not being affected by a boost or giant item then slow down
                if (onGrass == true && big <= 0 && (boost <= 0 || boostDirection == -1))
                {
                    velocity_multi *= 0.35f;
                }

                //speed up and reduce FOV(zoom in) if speed powerupp, regular speed and FOV without 
                if (boost > 0)
                {
                    if (boostDirection == 1)
                    {
                        velocity_multi *= 2;
                        camera.ZoomFast();
                    }
                    else
                    {
                        velocity_multi *= 0.65f;
                        camera.ZoomSlow();
                    }

                    boost--;
                }
                else
                {
                    camera.ZoomReset();
                    boostDirection = 0;
                }

                //cap its velocity
                if (velocity.Length > maxCarVelocity)
                {
                    velocity = velocity.Normalized() * maxCarVelocity;
                }

                //BIG BOI (scale is self explanatory)
                if (big > 0 && !bigged)
                {
                    Scale(3);
                    bigged = true;
                }
                else if (bigged && big <= 0)
                {
                    Scale(1 / 3f);
                    bigged = false;
                }
                else
                {
                    big--;
                }



                //setpos adds the vector to the cars position, this moves it
                SetPos(velocity * tStep * velocity_multi);

                if (centre.X > Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter)
                {
                    centre.X = Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter;
                }
                else if (centre.X < -( Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter))
                {
                    centre.X = -(Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter);
                }
                if (centre.Z > Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter)
                {
                    centre.Z = Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter;
                }
                else if (centre.Z < -(Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter))
                {
                    centre.Z = -(Terrain.gridDimension * Terrain.squareSize * .5f - 5 * meter);
                }

                //collide after movement to fix visual issues
                Collide(racetrack, this/*, out Vector2 raceAngle,*/ , out tHeight);
                Collide(landscape, this/*, out Vector2 grassAngle,*/ , out lHeight);
                if (tHeight + dimensions.Y / 2f + meter/2f > centre.Y || lHeight + dimensions.Y / 2f + meter / 2f > centre.Y)
                {
                    centre.Y = (float)Math.Max(lHeight, tHeight) + dimensions.Y / 2f;
                }
            }
        }


        static void Collide(Terrain terrain, Shape Shape, out float height)
        {

            Collision(terrain, Shape.centre, out float height1, out Vector3 normal);

            height = height1;

            Shape.normal = normal.Normalized();
        }    
        static float[] Barycentric(Vector3 A, Vector3 B, Vector3 C, Vector3 position)//gets the barycentric coordinates of a point on a triangle for use in the smooth terrain collision
        {
            //since the terrain is scaled, it needs to be resized
            A *= Terrain.squareSize;
            B *= Terrain.squareSize;
            C *= Terrain.squareSize;
            //takes into terrain coords
            position += (Terrain.squareSize / 2f) * new Vector3(Terrain.gridDimension, 0, Terrain.gridDimension);
            float[] barycentrics = new float[3];

            float T = (A.X - C.X) * (B.Z - C.Z) - (A.Z - C.Z) * (B.X - C.X);

            barycentrics[0] = ((B.Z - C.Z) * (position.X - C.X) + (C.X - B.X) * (position.Z - C.Z)) / T;
            barycentrics[1] = ((C.Z - A.Z) * (position.X - C.X) + (A.X - C.X) * (position.Z - C.Z)) / T;
            barycentrics[2] = 1f - barycentrics[0] - barycentrics[1];

            return barycentrics;
        }
        static void Collision(Terrain terrain, Vector3 B, out float Height, out Vector3 normal) //gets height and normal vector of terrain area that the car/ object is currently in
        {
            //matrix method for barycentric coordinates, formulas from wikipedia

            //get square on grid for 

            //convert shape coordinates to one square on the terrain

            //terrain has 
            //Terrain.gridDimension
            //^2 pieces
            //centres at 0,0

            //each terrain piece is 5 long
            //add (gridDimension/2) to the shapes centre then divide by 5 and it will be in relative terrain world with each corner of a terrain piece being easily indedxable from the array 
            //check if outside the range (0,0) to (gridDimension, gridDimension)

            float X = B.X / Terrain.squareSize + ((Terrain.gridDimension+1) / 2);
            float Z = B.Z / Terrain.squareSize + ((Terrain.gridDimension+1) / 2);

            int X_ = (int)Math.Floor(X);
            int Z_ = (int)Math.Floor(Z);

            //terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].color = Color.Red;
            //terrain.terrain.verts[(X_+1) * Terrain.gridDimension + Z_].color = Color.Red;
            //terrain.terrain.verts[X_ * Terrain.gridDimension + Z_+1].color = Color.Red;
            //terrain.terrain.verts[(X_+1) * Terrain.gridDimension + Z_+1].color = Color.Red;
            ////terrain.terrain.resetBuffers();


            //decide bottom right or top left triangle
            //corners
            Vector3[] c = new Vector3[3];
            float[] barry;
            float X_L = X - X_;
            float Z_L = Z - Z_;
            if (X_L < Z_L)
            {
                //bottom right
                //c[2] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_ + 1].pos;
                //c[1] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos;
                //c[0] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].pos;

                c[1] = terrain.terrain.verts[X_ * (Terrain.gridDimension+1) + Z_].pos;
                c[2] = terrain.terrain.verts[(X_ + 1) * (Terrain.gridDimension + 1) + (Z_ + 1)].pos;
                c[0] = terrain.terrain.verts[X_ * (Terrain.gridDimension + 1) + (Z_ + 1)].pos;

                //c[0] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].pos;
                //c[1] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos;
                //c[2] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos;
                //barry = barycentric(terrain.terrain.verts[X_ * Terrain.gridDimension + Z_].pos, terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos, terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos, B);
                barry = Barycentric(c[0], c[1], c[2], B);
                normal = Vector3.Cross(c[2] - c[0], c[1] - c[0]);
            }
            else
            {
                //top left
                //c[0] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_ + 1].pos;
                //c[1] = terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos;
                //c[2] = terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos;
                c[2] = terrain.terrain.verts[(X_ + 1) * (Terrain.gridDimension + 1) + (Z_ + 1)].pos;
                c[0] = terrain.terrain.verts[(X_ + 1) * (Terrain.gridDimension + 1) + Z_].pos;
                c[1] = terrain.terrain.verts[X_ * (Terrain.gridDimension + 1) + Z_].pos;




                //barry = barycentric(terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_ + 1].pos, terrain.terrain.verts[(X_ + 1) * Terrain.gridDimension + Z_].pos, terrain.terrain.verts[X_ * Terrain.gridDimension + Z_ + 1].pos, B);
                barry = Barycentric(c[0], c[1], c[2], B);
                normal = Vector3.Cross(c[1] - c[0], c[2] - c[0]);
            }

            Height = c[0].Y * barry[0] + c[1].Y * barry[1] + c[2].Y * barry[2];
            Height *= Terrain.squareSize;
            //Console.WriteLine(Height);
        }

        class Terrain //perlin noise and terrain stuff
        {

            static Random rand;

            public Shape terrain;
            public static int gridDimension = 128;
            public static float heightMulti = 3.5f;
            public static int trackSize = 8;
            public static int squareSize = 5;
            float[,] heights = new float[gridDimension+1, gridDimension+1];
            public Vector2 startPos;
            public Vector2 checkpointPos;
            public static Vector2 offset;
            public static Color landColor;
            public static Color trackColor;
            string path = "129_Terrain_good.obj";

            public static void getRan() //moves some distance away for the perlin noise
            {
                rand = new Random(randomSeed);
                offset = new Vector2(rand.Next(0, 1000), rand.Next(0, 1000));
            }
            void PerlinHeightsForTerrainArray(Color color)
            {
                heights = Perlin.DoPerlin(heights, offset.X, offset.Y, 4);
                List<float> temporary = new List<float>();
                for (int i = 0; i < heights.GetLength(0); i++)
                {
                    for (int j = 0; j < heights.GetLength(1); j++)
                    {
                        temporary.Add(heights[i, j]);
                    }
                }
                //Console.WriteLine(temporary.Min());
                //Console.WriteLine(temporary.Max());

                terrain = new Shape(path, color, 25);
                terrain.Scale(squareSize);
                for (int i = 0; i < heights.GetLength(0); i++)
                {
                    for (int j = 0; j < heights.GetLength(1); j++)
                    {
                        terrain.verts[(gridDimension+1) * (i) + j].pos.Y += (float)Math.Pow(Math.E, heights[i, j]) * heightMulti * meter;

                        //Color col = LandColor;
                        //terrain.verts[gridDimension * i + j].color = Color.FromArgb(col.R, col.G, col.B);
                        //(col.G + (int)Math.Min((int)(Math.Log(heights[i, j]) * 255), 255)) / 2
                        //Console.WriteLine(terrain.verts[gridDimension *i + j].pos.X);
                    }
                }

            }
            public Terrain() //terrain for landscape
            {

                PerlinHeightsForTerrainArray(landColor);
                terrain.resetBuffers();
            }
            public Terrain(bool DifferentConstructorLol) //terrain for track, uses wavefunctioncollapse
            {
                PerlinHeightsForTerrainArray(trackColor); 

                WaveFunctionCollapse[,] track = new WaveFunctionCollapse[trackSize, trackSize];
                //start position and checkpoint position
                Vector2 StartTile = new Vector2(0, 0);
                Vector2 CheckpointTile = new Vector2(0, 0);
                WaveFunctionCollapse.GenerateTrack(ref track, ref StartTile, ref CheckpointTile);
                startPos = new Vector2((StartTile.X - (trackSize/2f) + .5f) * (gridDimension/trackSize), (StartTile.Y - (trackSize/2f) + .5f) * (gridDimension / trackSize));
                checkpointPos = new Vector2((CheckpointTile.X - (trackSize / 2f) + .5f) * (gridDimension / trackSize), (CheckpointTile.Y - (trackSize / 2f) + .5f) * (gridDimension / trackSize));
                int trackMin = (gridDimension / trackSize)/4;
                int trackMax = ((gridDimension / trackSize)*3)/4;
                int trackGrid = (gridDimension / trackSize);

                for (int i = 0; i < heights.GetLength(0); i++)
                {
                    for (int j = 0; j < heights.GetLength(1); j++)
                    {
                        int x = i / trackGrid;
                        int y = j / trackGrid;
                        x %= (gridDimension/trackGrid);
                        y %= (gridDimension/trackGrid);

                        bool a = false;
                        if ('|' == track[y, x].name && (i % trackGrid > trackMin && i % trackGrid < trackMax))
                        {
                            a = true;
                        }
                        else if ('-' == track[y, x].name && (j % trackGrid > trackMin && j % trackGrid < trackMax))
                        {
                            a = true;
                        }
                        else if ('L' == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid < trackMax) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid > trackMin)))
                        {
                            a = true;
                        }
                        else if ('7' == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid > trackMin) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid < trackMax)))
                        {
                            a = true;
                        }
                        else if ('J' == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid < trackMax) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid < trackMax)))
                        {
                            a = true;
                        }
                        else if ('F' == track[y, x].name && ((trackMin < i % trackGrid && i % trackGrid < trackMax && j % trackGrid > trackMin) || (trackMin < j % trackGrid && j % trackGrid < trackMax && i % trackGrid > trackMin)))
                        {
                            a = true;
                        }

                        //if ("|-LJF7".Contains(track[y, x].name))
                        //{
                        //    a = true;
                        //}

                        if (a)
                        {
                            terrain.verts[(gridDimension + 1) * i + j].pos.Y += .25f;
                        }
                        else
                        {
                            terrain.verts[(gridDimension + 1) * i + j].pos.Y -= .1f;
                        }
                        //colours the start tile differently
                        if (new Vector2(x, y) == StartTile || new Vector2(x, y) == CheckpointTile)
                        {
                            Color temporaryColor = terrain.verts[(gridDimension+1) * i + j].color;
                            terrain.verts[(gridDimension + 1) * i + j].color = Color.FromArgb(255, Math.Max(0, temporaryColor.R - 50), Math.Max(0, temporaryColor.G - 50), Math.Min(255, temporaryColor.B + 60));
                        }
                    }

                    terrain.resetBuffers();
                }

            }
            public class WaveFunctionCollapse // wave function collapse algorithm, ask me to explain cause otherwise it'd be several paragraphs
            {
                public int x;
                public int y;

                //0 = not track, 1 = track, 2 = neither/both
                public byte Up;
                public byte Down;
                public byte Left;
                public byte Right;

                public char name;

                public static Dictionary<char, byte[]> lookup = new Dictionary<char, byte[]>()
            {
                { 'A',new byte[]
                {
                    0,
                    0,
                    0,
                    0
                }
                },

                { '|',new byte[]
                {
                    1,
                    1,
                    0,
                    0
                }
                },

                { '-',new byte[]
                {
                    0,
                    0,
                    1,
                    1
                }
                },

                { 'L',new byte[]
                {
                    1,
                    0,
                    0,
                    1
                }
                },

                { '7',new byte[]
                {
                    0,
                    1,
                    1,
                    0
                }
                },

                { 'F',new byte[]
                {
                    0,
                    1,
                    0,
                    1
                }
                },

                { 'J',new byte[]
                {
                    1,
                    0,
                    1,
                    0
                }
                },
            };

                public List<char> possibilities = new List<char>();

                public WaveFunctionCollapse(char name)
                {
                    this.name = name;
                }

                static void TrackOutline(ref WaveFunctionCollapse[,] track)
                {
                    for (int i = 0; i < track.GetLength(0); i++)
                    {
                        for (int j = 0; j < track.GetLength(1); j++)
                        {
                            if (i == 0 || j == 0 || i == track.GetLength(0) - 1 || j == track.GetLength(1) - 1)
                            {
                                track[i, j] = new WaveFunctionCollapse('A');
                                track[i, j].Up = 0;
                                track[i, j].Down = 0;
                                track[i, j].Left = 0;
                                track[i, j].Right = 0;
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

                static void GeneratePossibilities(ref List<WaveFunctionCollapse> tiles, ref WaveFunctionCollapse[,] track)
                {
                    for (int i = 1; i < track.GetLength(0) - 1; i++)
                    {
                        for (int j = 1; j < track.GetLength(0) - 1; j++)
                        {
                            byte up = 3;
                            byte down = 3;
                            byte left = 3;
                            byte right = 3;

                            if (track[i, j] == null)
                            {
                                //up
                                if (track[i - 1, j] == null)
                                {
                                    up = 2;
                                }
                                else if (track[i - 1, j].Down == 0)
                                {
                                    up = 0;
                                }
                                else
                                {
                                    up = 1;
                                }

                                //down
                                if (track[i + 1, j] == null)
                                {
                                    down = 2;
                                }
                                else if (track[i + 1, j].Up == 0)
                                {
                                    down = 0;
                                }
                                else
                                {
                                    down = 1;
                                }

                                //left
                                if (track[i, j + 1] == null)
                                {
                                    right = 2;
                                }
                                else if (track[i, j + 1].Left == 0)
                                {
                                    right = 0;
                                }
                                else
                                {
                                    right = 1;
                                }

                                //right
                                if (track[i, j - 1] == null)
                                {
                                    left = 2;
                                }
                                else if (track[i, j - 1].Right == 0)
                                {
                                    left = 0;
                                }
                                else
                                {
                                    left = 1;
                                }

                                tiles.Add(new WaveFunctionCollapse('U'));
                                tiles.Last().x = j;
                                tiles.Last().y = i;

                                //air
                                if ((up == 2 || up == 0) && (down == 2 || down == 0) && (left == 2 || left == 0) && (right == 2 || right == 0))
                                {
                                    tiles.Last().possibilities.Add('A');
                                }

                                //|
                                if ((up == 2 || up == 1) && (down == 2 || down == 1) && (left == 2 || left == 0) && (right == 2 || right == 0))
                                {
                                    tiles.Last().possibilities.Add('|');
                                }

                                //-
                                if ((up == 2 || up == 0) && (down == 2 || down == 0) && (left == 2 || left == 1) && (right == 2 || right == 1))
                                {
                                    tiles.Last().possibilities.Add('-');
                                }

                                //L
                                if ((up == 2 || up == 1) && (down == 2 || down == 0) && (left == 2 || left == 0) && (right == 2 || right == 1))
                                {
                                    tiles.Last().possibilities.Add('L');
                                }

                                //7
                                if ((up == 2 || up == 0) && (down == 2 || down == 1) && (left == 2 || left == 1) && (right == 2 || right == 0))
                                {
                                    tiles.Last().possibilities.Add('7');
                                }

                                //F
                                if ((up == 2 || up == 0) && (down == 2 || down == 1) && (left == 2 || left == 0) && (right == 2 || right == 1))
                                {
                                    tiles.Last().possibilities.Add('F');
                                }

                                //J
                                if ((up == 2 || up == 1) && (down == 2 || down == 0) && (left == 2 || left == 1) && (right == 2 || right == 0))
                                {
                                    tiles.Last().possibilities.Add('J');
                                }
                            }
                        }
                    }
                }

                public static void GenerateTrack(ref WaveFunctionCollapse[,] track, ref Vector2 startPosition, ref Vector2 CheckpointPosition)
                {


                    do
                    {
                        TrackOutline(ref track);



                        int z = rand.Next(1, track.GetLength(1) - 2);
                        int w = rand.Next(2, track.GetLength(0) - 4);
                        startPosition = new Vector2(z, w);
                        //Console.WriteLine(startPos.X + "  " + startPos.Y);
                        track[w, z] = new WaveFunctionCollapse('|');
                        track[w, z].Up = 1;
                        track[w, z].Down = 1;
                        track[w, z].Left = 0;
                        track[w, z].Right = 0;

                        while (true)
                        {
                            List<WaveFunctionCollapse> tiles = new List<WaveFunctionCollapse>();

                            GeneratePossibilities(ref tiles, ref track);


                            if (tiles.Count != 0)
                            {
                                WaveFunctionCollapse A = tiles[rand.Next(0, tiles.Count)];
                                int minPossibilities = A.possibilities.Count;

                                for (int i = 0; i < tiles.Count; i++)
                                {
                                    if (tiles[i].possibilities.Count < A.possibilities.Count)
                                    {
                                        A = tiles[i];
                                        minPossibilities = A.possibilities.Count;
                                    }
                                }
                                //bit of linq to find all where possibilies count is the minimum
                                A = tiles.Where(x => x.possibilities.Count == minPossibilities).ToArray()[rand.Next(0, tiles.Where(x => x.possibilities.Count == minPossibilities).Count())];


                                //A.name = A.possibilities[rand.Next(0, A.possibilities.Count)];
                                foreach (char possibility in A.possibilities)
                                {//if a tile is able to become Air then I must pick that otherwise multiple loops of track would be generated
                                    if (possibility == 'A')
                                    {
                                        A.name = 'A';
                                    }
                                }
                                if (A.name == 'U')
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


                                A.Up = WaveFunctionCollapse.lookup[A.name][0];
                                A.Down = WaveFunctionCollapse.lookup[A.name][1];
                                A.Left = WaveFunctionCollapse.lookup[A.name][2];
                                A.Right = WaveFunctionCollapse.lookup[A.name][3];
                                track[A.y, A.x] = A;
                            }
                            else
                            {
                                break;
                            }

                        }





                    } while (trackValid(ref track) == false);
                    CheckpointPosition = GetCheckpoint(track, startPosition);


                    //do regular track stuff to models in 3d
                }
                static bool trackValid(ref WaveFunctionCollapse[,] track)
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
                    if (cornerCount < 9)
                    {
                        return false;
                    }
                    else if (tracklength < 17)
                    {
                        return false;
                    }
                    return true;
                }
                static void displa(WaveFunctionCollapse[,] track)
                {
                    for (int i = 0; i < track.GetLength(0); i++)
                    {
                        for (int j = 0; j < track.GetLength(1); j++)
                        {
                            Console.Write(track[i, j].name == 'A' ? " " : track[i, j].name.ToString());
                        }
                        Console.WriteLine();
                    }
                }
                static Vector2 GetCheckpoint(WaveFunctionCollapse[,] track, Vector2 startPosition)
                {
                    //needs fixing BADLY

                    //returns the coordinates of a checkpoint tile, ~1/2 way along the track
                    int tracklength = 0;
                    for (int i = 0; i < track.GetLength(0); i++)
                    {
                        for (int j = 0; j < track.GetLength(1); j++)
                        {
                            if ("|-7FLJ".Contains(track[i, j].name))
                            {
                                tracklength++;
                            }
                        }
                    }

                    //tracklength/=2;
                    //displa(track);
                    //Console.WriteLine(tracklength);
                    int x, y;
                    y = (int)startPosition.Y;
                    x = (int)startPosition.X;
                    bool[,] discoverred = new bool[trackSize, trackSize];
                    List<WaveFunctionCollapse> tiles = new List<WaveFunctionCollapse>();
                    while (tracklength > 0)
                    {
                        //gonna traverse the track to find the place halfway along

                        tiles.Add(track[y, x]);
                        discoverred[y, x] = true;
                        switch (track[y, x].name)
                        {
                            case '|':
                                if ("F|7".Contains(track[y - 1, x].name) && discoverred[y - 1, x] == false)
                                {
                                    y -= 1;
                                }
                                else
                                {
                                    y += 1;
                                }
                                break;
                            case '-':
                                if ("L-F".Contains(track[y, x - 1].name) && discoverred[y, x - 1] == false)
                                {
                                    x -= 1;
                                }
                                else
                                {
                                    x += 1;
                                }
                                break;
                            case 'L':
                                if ("|F7".Contains(track[y - 1, x].name) && discoverred[y - 1, x] == false)
                                {
                                    y -= 1;
                                }
                                else
                                {
                                    x += 1;
                                }
                                break;
                            case '7':
                                if ("|JL".Contains(track[y + 1, x].name) && discoverred[y + 1, x] == false)
                                {
                                    y += 1;
                                }
                                else
                                {
                                    x -= 1;
                                }
                                break;
                            case 'J':
                                if ("|7F".Contains(track[y - 1, x].name) && discoverred[y - 1, x] == false)
                                {
                                    y -= 1;
                                }
                                else
                                {
                                    x -= 1;
                                }
                                break;
                            case 'F':
                                if ("|JL".Contains(track[y + 1, x].name) && discoverred[y + 1, x] == false)
                                {
                                    y += 1;
                                }
                                else
                                {
                                    x += 1;
                                }
                                break;
                        }

                        tracklength -= 1;
                    }
                    //for (int i = 0; i < tiles.Count(); i++)
                    //{
                    //    //Console.WriteLine(tiles[i].name);
                    //}

                    x = tiles[tiles.Count / 2].x;
                    y = tiles[tiles.Count / 2].y;

                    return new Vector2(x, y);

                }
            }
            public class Perlin //same as wave function collapse
            {
                public static float[,] DoPerlin(float[,] c, float xOffset, float yOffset, int octaves)
                {
                    float[,] a = new float[c.GetLength(0), c.GetLength(1)];
                    for (int i = 0; i < a.GetLength(0); i++)
                    {
                        for (int j = 0; j < a.GetLength(1); j++)
                        {
                            float x = xOffset + 2.5f*i / (float)a.GetLength(0);
                            float y = yOffset + 2.5f*j / (float)a.GetLength(1);

                            a[i, j] = SampleNoise(x, y, 1, 1, octaves, 2f);
                            //a[i, j] = (float)Math.Pow(a[i, j], 1.3f);
                            //function that manipulates the output to make it nicer
                            a[i, j] = (float)Math.Pow(Math.E, a[i, j]);

                        }
                    }

                    return a;
                }
                static float SampleNoise(float x, float y, float amplitude, float frequency, int octaves, float amplitudeModifier)
                {
                    float value = 0;
                    //add multiple 'octaves' to make it looks fancy
                    for (int i = 0; i < octaves; i++)
                    {
                        value += amplitude * perlin(x * frequency, y * frequency);
                        amplitude /= amplitudeModifier;
                        frequency *= amplitudeModifier;
                    }
                    //divide by 2- (1/2)^octaves to return to 0 -> 1
                    value = value / (float)(2 - Math.Pow(.5f, octaves - 1));
                    return value;
                }

                static float Lerp(float a0, float a1, float w)
                {
                    //lerp means linear interpolate, it isnt linearly interpolating cause I changed my mind
                    if (0.0 > w) return a0;
                    if (1.0 < w) return a1;

                    //return a1 * w + (1 - w) * a0;
                    //"smooth step" function from wikipedia, meant to be smoother instead of a linear mapping
                    return (float)((a1 - a0) * (3.0 - w * 2.0) * w * w + a0);
                }

                static Vector2 randomGradient(int ix, int iy)
                {
                    //PSEUDO random direction vector -> same ix and iy = same vector 

                    Random randx = new Random(ix);
                    Random randy = new Random(iy);
                    Random rar = new Random(randx.Next() * randy.Next());

                    Vector2 v = new Vector2((float)Math.Cos(rar.Next()*180/Math.PI), (float)(Math.Sin(rar.Next()*180/Math.PI)));
                    return v;
                }

                static float Dot_position_random(int x_Offset, int y_Offset, float x, float y)
                {
                    Vector2 gradient = randomGradient(x_Offset, y_Offset);
                    return Vector2.Dot(new Vector2(x - x_Offset, y - y_Offset), gradient);
                    //return ((x - (float)x_Offset) * gradient.X + (y - (float)y_Offset) * gradient.Y);
                }

                public static float perlin(float x, float y)
                {
                    //corners of a grid 
                    int xFloor = (int)Math.Floor(x);
                    int xCeiling = xFloor + 1;
                    int yFloor = (int)Math.Floor(y);
                    int yCeiling = yFloor + 1;

                    //0-1 how far along from the bottom left side is the point
                    float x_Offset = x - (float)xFloor;
                    float y_Offset = y - (float)yFloor;


                    //dot products find the value for the botton line of the square
                    float bottomLeft = Dot_position_random(xFloor, yFloor, x, y);
                    float bottomRight = Dot_position_random(xCeiling, yFloor, x, y);
                    float interpolate1 = Lerp(bottomLeft, bottomRight, x_Offset);

                    //dot products find the value for the botton line of the square
                    float topLeft = Dot_position_random(xFloor, yCeiling, x, y);
                    float topRight = Dot_position_random(xCeiling, yCeiling, x, y);
                    float interpolate2 = Lerp(topLeft, topRight, x_Offset);

                    float value = Lerp(interpolate1, interpolate2, y_Offset);

                    //from range -1 -> 1   to 0 -> 1
                    return (value/2f +0.5f);
                }
            }
        }
    }
    
}

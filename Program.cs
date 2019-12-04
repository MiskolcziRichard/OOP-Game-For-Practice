using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace Practise_Game
{
    class Program
    {
        #region StaticFields
        static bool _run = true;
        static Thread spawner;
        static Map map;
        static Player player;
        // static District[] districts;
        static BushField[] bushField;

        static bool[,,] coordinateMatrix = new bool[Console.WindowWidth, Console.WindowHeight, 5]; //0: bushes; 1: turrets; 2: enemies; 3: hasBullet, 4: active lasers

        static List<Turret> turrets = new List<Turret>();
        static List<Enemy> enemies = new List<Enemy>();

        #endregion

        class Item
        {
            protected ConsoleColor Color {get; set;}
            protected int[] coords;

            public Item()
            {
                coords = new int[2];
            }

            ~Item()
            {
                if (this is Turret)
                {
                    coordinateMatrix[this[0], this[1], 1] = false;
                }
                else if (this is Enemy)
                {

                }
                //extend if neccessary

                // this.Kill();

                ClearBehind();
            }

            public int this[int index]
            {
                get {return this.coords[index];}
                set {this.coords[index] = value;}
            }
            //i figured this out on my own so I'm very proud of myself please don't judge my pride

            protected void Draw()
            {
                Console.SetCursorPosition(this[0], this[1]);
                Console.ForegroundColor = this.Color;
                Console.Write("▓");

                //Console.SetCursorPosition(Console.WindowWidth, Console.WindowHeight);
                Console.ResetColor();
            }

            protected void ClearBehind()
            {
                ConsoleColor drawColor = this.Color;

                if (coordinateMatrix[this[0], this[1], 4])
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else if (coordinateMatrix[this[0], this[1], 0])
                {
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.White;
                }

                //clears behind me
                Console.Write("▓");

                Console.ForegroundColor = this.Color;
            }

            protected virtual bool CheckObstacles()
            {
              return false;
            }

            public void Move(string dir)
            {
                string operation = "";

                void ReverseMovement()
                {
                    switch (operation)
                    {
                        case "y-":
                            coords[1]++;
                            break;
                        case "y+":
                            coords[1]--;
                            break;
                        case "x+":
                            coords[0]--;
                            break;
                        case "x-":
                            coords[0]++;
                            break;
                    }

                    Draw();
                }

                Console.SetCursorPosition(coords[0], coords[1]);

                try
                {
                    ClearBehind();
                }
                catch (ArgumentOutOfRangeException)
                {
                    ReverseMovement();
                }

                switch (dir)
                {
                    case "UpArrow":
                        coords[1]--;
                        operation = "y-";
                        break;
                    case "DownArrow":
                        coords[1]++;
                        operation = "y+";
                        break;
                    case "LeftArrow":
                        coords[0]--;
                        operation = "x-";
                        break;
                    case "RightArrow":
                        coords[0]++;
                        operation = "x+";
                        break;
                }

                try
                {
                    //validating movement
                    if (CheckObstacles())
                    {
                        //obstacles are in the way, not cleared to move
                        ReverseMovement();
                    }
                    else
                    {
                        //move to new position
                        Console.SetCursorPosition(this[0], this[1]);
                        Console.ForegroundColor = this.Color;
                        Console.Write("▓");
                        Console.ResetColor();
                    }
                } catch (Exception)
                {
                    //the position would be outside the bounds of the map anyway
                    ReverseMovement();
                }

                Console.ResetColor();
                //Console.SetCursorPosition(Console.WindowWidth, Console.WindowHeight);
            }
        }

        class Turret : Item
        {
            private List<int[]> laserBeams = new List<int[]>();
            private int secondsRemaining;
            private string Orientation {get; set;} //see the static enumerator
            private Thread countDown;

            public Turret(string orientation)
            {
                this.Color = ConsoleColor.DarkGray;

                Orientation = orientation;
                secondsRemaining = 5;

                #region Spawn
                    switch (Orientation)
                    {
                        case "UpArrow":
                            this[0] = player[0];
                            this[1] = player[1] - 1;
                            break;
                        case "DownArrow":
                            this[0] = player[0];
                            this[1] = player[1] + 1;
                            break;
                        case "LeftArrow":
                            this[0] = player[0] - 1;
                            this[1] = player[1];
                            break;
                        case "RightArrow":
                            this[0] = player[0] + 1;
                            this[1] = player[1];
                            break;
                    }

                    Draw();
                #endregion

                coordinateMatrix[this[0], this[1], 1] = true;

                FireLaser();

                countDown = new Thread(CountSeconds);
                countDown.Start();
            }

            ~Turret()
            {
              for (int i = 0; i < laserBeams.Count; i++)
              {
                this[0] = laserBeams[i][0];
                this[1] = laserBeams[i][1];

                ClearBehind();
              }

              laserBeams.Clear();
            }

            private void FireLaser()
            {
                Console.SetCursorPosition(this[0], this[1]);
                Console.ForegroundColor = ConsoleColor.Cyan;

                int[] recordBeamPositions = new int[2];

                switch (Orientation)
                {
                    case "UpArrow":
                        for (int i = this[1] - 1; i >= 0; i--)
                        {
                            Console.SetCursorPosition(this[0], i);

                            if (coordinateMatrix[this[0], i, 1])
                            {
                                break;
                            }

                            Console.Write("▓");

                            recordBeamPositions[0] = this[0];
                            recordBeamPositions[1] = i;
                            this.laserBeams.Add(recordBeamPositions);
                            coordinateMatrix[this[0], i, 4] = true;
                        }

                        break;

                    case "DownArrow":
                        for (int i = this[1] + 1; i < Console.WindowHeight; i++)
                        {
                            Console.SetCursorPosition(this[0], i);

                            if (coordinateMatrix[this[0], i, 1])
                            {
                                break;
                            }

                            Console.Write("▓");

                            recordBeamPositions[0] = this[0];
                            recordBeamPositions[1] = i;
                            this.laserBeams.Add(recordBeamPositions);
                            coordinateMatrix[this[0], i, 4] = true;
                        }

                        break;

                    case "LeftArrow":
                        for (int i = this[0] - 1; i >= 0; i--)
                        {
                            Console.SetCursorPosition(i, this[1]);

                            if (coordinateMatrix[i, this[1], 1])
                            {
                                break;
                            }

                            Console.Write("▓");

                            recordBeamPositions[0] = i;
                            recordBeamPositions[1] = this[1];
                            this.laserBeams.Add(recordBeamPositions);
                            coordinateMatrix[i, this[1], 4] = true;
                        }

                        break;

                    case "RightArrow":
                        for (int i = this[0] + 1; i < Console.WindowWidth; i++)
                        {
                            Console.SetCursorPosition(i, this[1]);

                            if (coordinateMatrix[i, this[1], 1])
                            {
                                break;
                            }

                            Console.Write("▓");

                            recordBeamPositions[0] = i;
                            recordBeamPositions[1] = this[1];
                            this.laserBeams.Add(recordBeamPositions);
                            coordinateMatrix[i, this[1], 4] = true;
                        }

                        break;
                }

                //Console.SetCursorPosition(Console.WindowWidth, Console.WindowHeight);
                Console.ResetColor();
            }

            private void CountSeconds()
            {
                do
                {
                    Thread.Sleep(1000);
                    this.secondsRemaining--;
                } while (this.secondsRemaining != 0);

                turrets.Remove(this);
            }
        }

        class Enemy : Item
        {
            private Thread seek, life;
            public bool Alive {get; set;}

            public Enemy()
            {
                Alive = true;
                this.Color = ConsoleColor.Red;

                #region Get Coordinates
                    Random rndm = new Random();
                    bool invalidCoordinates = true;
                    int[] spawnCoords = new int[2];

                    do
                    {
                        try
                        {
                            // spawnCoords = {rndm.Next(1, Console.WindowWidth + 1), rndm.Next(1, Console.WindowHeight + 1)};
                            spawnCoords[0] = rndm.Next(1, Console.WindowWidth);
                            spawnCoords[1] = rndm.Next(1, Console.WindowHeight);

                            if (coordinateMatrix[spawnCoords[0], spawnCoords[1], 0])
                            {
                                throw new InvalidOperationException();
                            }
                            else
                            {
                                invalidCoordinates = false;
                            }
                        } catch (InvalidOperationException)
                        {
                            continue;
                        }
                    } while (invalidCoordinates);
                    coords[0] = spawnCoords[0];
                    coords[1] = spawnCoords[1];

                #endregion

                Draw();

                coordinateMatrix[this[0], this[1], 2] = true;

                enemies.Add(this);

                seek = new Thread(SeekPlayer);
                seek.Priority = ThreadPriority.Highest;
                seek.Start();

                life = new Thread(CheckLifeSigns);
                //life.Priority = ThreadPriority.BelowNormal;
                life.Start();
            }

            //this runs if the player respawns the entire map. If the enemy dies
            //because they've legit been killed, the 'Kill' method will be executed
            //from inside the Seek method due to the (Alive) condition no longer
            //being satisfied
            ~Enemy()
            {
              Kill();
            }

            protected override bool CheckObstacles()
            {
              if (this[0] == player[0] && this[1] == player[1])
              {
                // player.GameOver();
              }

              if (coordinateMatrix[this[0], this[1], 0]
               || coordinateMatrix[this[0], this[1], 1]
               || coordinateMatrix[this[0], this[1], 2])
               {
                 return true;
               }
               else
               {
                 return false;
               }
            }

            private string CalculateRoute()
            {
                string[] dir = new string[2];

                #region X Axis
                  if (player[0] > this[0])
                  {
                    dir[0] = "RightArrow";
                  }
                  else if (player[0] < this[0])
                  {
                    dir[0] = "LeftArrow";
                  }
                #endregion

                #region Y Axis
                  if (player[1] > this[1])
                  {
                    dir[1] = "DownArrow";
                  }
                  else if (player[1] < this[1])
                  {
                    dir[1] = "UpArrow";
                  }
                #endregion

                Random rndm = new Random();
                string result = dir[rndm.Next(0, 2)];

                //result != null ? return result : throw new Exception();
                if (result != null)
                {
                  return result;
                }
                else
                {
                  throw new Exception();
                }
            }

            private void SeekPlayer()
            {
              while (Alive)
              {
                try
                {
                  Move(CalculateRoute());
                  Thread.Sleep(200);
                }
                catch (Exception)
                {
                  //gotcha, just chillin' until the player moves
                }
              }

              this.Kill();
            }

            private void Kill()
            {
              try
              {
                seek.Abort();
                life.Abort();
              }
              catch (PlatformNotSupportedException)
              {
                seek = null;
                life = null;
              }

              coordinateMatrix[this[0], this[1], 2] = false;
              enemies.Remove(this);

              ClearBehind();
            }

            private void CheckLifeSigns()
            {
              while (true)
              {
                if (coordinateMatrix[this[0], this[1], 4])
                {
                  Alive = false;
                  break;
                }
              }
            }
        }

        class BushField
        {
            private Bush[,] bushes;

            public BushField()
            {
                Random rndm = new Random();
                bushes = new Bush[rndm.Next(1, 20), rndm.Next(1, 20)];

                #region Spawn Bushes
                    int[] currentCoordinates = {rndm.Next(0, Console.WindowWidth + 1), rndm.Next(0, Console.WindowHeight + 1)};
                    int startX = currentCoordinates[0];

                    Console.SetCursorPosition(currentCoordinates[0], currentCoordinates[1]);

                    for (int i = 0; i < bushes.GetLength(1); i++)
                    {
                        for (int j = 0; j < bushes.GetLength(0); j++)
                        {
                            bushes[j, i] = new Bush(currentCoordinates[0], currentCoordinates[1]);
                            try
                            {
                               currentCoordinates[0]++;
                            } catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        try
                        {
                           currentCoordinates[1]++;
                        } catch (ArgumentOutOfRangeException)
                        {
                            break;
                        }

                        currentCoordinates[0] = startX;
                    }
                #endregion
            }
        }

        class Bush
        {
            private bool hasBullet;

            public Bush(int x, int y)
            {
                Random rndm = new Random();
                int chance = rndm.Next(0, 101);

                if (chance <= 5)
                {
                    hasBullet = true;
                }
                else
                {
                    hasBullet = false;
                }

                #region Draw Bush
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.SetCursorPosition(x, y);
                    Console.Write("▓");
                    Console.ResetColor();
                #endregion

                try
                {
                    coordinateMatrix[x, y, 0] = true;

                    if (hasBullet)
                    {
                        coordinateMatrix[x, y, 3] = true;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    //gotcha
                }
            }
        }

        class Map
        {
            public Map()
            {
                try
                {
                   Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
                }
                catch (PlatformNotSupportedException)
                {
                    //gotcha
                }

                Console.Clear();

                //now paint it!

                for (int i = 0; i < Console.WindowHeight; i++)
                {
                    for (int j = 0; j < Console.WindowWidth; j++)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.Write("▓");
                    }
                }

                Console.ResetColor();
            }
        }

        class Bullet : Item
        {
            private Thread fly;
            private string Orientation {get; set;}

            public Bullet(string orientation)
            {
                this.Color = ConsoleColor.Black;
                Orientation = orientation;

                switch (Orientation)
                {
                    case "LeftArrow":
                        this[0] = player[0]--;
                        this[1] = player[1];
                        break;
                    case "DownArrow":
                        this[0] = player[0];
                        this[1] = player[1]++;
                        break;
                    case "RightArrow":
                        this[0] = player[0]++;
                        this[1] = player[1];
                        break;
                    case "UpArrow":
                        this[0] = player[0];
                        this[1] = player[1]--;
                        break;
                }

                fly = new Thread(Fly);
                fly.Start();
            }

            ~Bullet()
            {
                ClearBehind();
            }

            private void Fly()
            {
                Console.ForegroundColor = this.Color;

                switch (Orientation)
                {
                    case "UpArrow":
                        while (this[1] >= 0)
                        {
                            Console.SetCursorPosition(this[0], this[1]);
                            ClearBehind();
                            Move(Orientation);
                            this[1]--;
                            Thread.Sleep(25);
                        }

                        break;
                    case "DownArrow":
                        while (this[1] <= Console.WindowHeight)
                        {
                            Console.SetCursorPosition(this[0], this[1]);
                            Move(Orientation);
                            this[1]++;
                            Thread.Sleep(25);
                        }

                        break;
                    case "RightArrow":
                        while (this[0] <= Console.WindowWidth)
                        {
                            Console.SetCursorPosition(this[0], this[1]);
                            Move(Orientation);
                            this[0]++;
                            Thread.Sleep(25);
                        }

                        break;
                    case "LeftArrow":
                        while (this[0] >= 0)
                        {
                            Console.SetCursorPosition(this[0], this[1]);
                            Move(Orientation);
                            this[0]--;
                            Thread.Sleep(25);
                        }

                        break;
                }
            }
        }

        class Player : Item
        {
            public int Turrets {get; set;}
            public int Bullets {get; set;}

            public Player(int x = 40, int y = 20)
            {
                this.Color = ConsoleColor.Black;

                this[0] = x;
                this[1] = y;
                Turrets = 10; //for debugging only, 1 by default
                Bullets = 10; //for debugging only, 3 by default

                Draw();

                //Console.SetCursorPosition(Console.WindowWidth, Console.WindowHeight);
            }

            // public void GameOver()
            // {
            //   Console.Clear();
            //
            //   using (System.IO.StreamReader sr = new StreamReader("gameover.txt"))
            //   {
            //     Console.WriteLine(sr.ReadToEnd());
            //   }
            //
            //   Thread.Sleep(3000);
            // }

            public void Shoot(string orientation)
            {
                Bullet bullet = new Bullet(orientation);
                this.Bullets--;
            }

            protected override bool CheckObstacles()
            {
              if (coordinateMatrix[this[0], this[1], 1])
              {
                return true;
              }
              else
              {
                return false;
              }
            }

            public void PlaceTurret(string Orientation)
            {
                if (this.Turrets != 0)
                {
                    Turret turret = new Turret(Orientation);
                    turrets.Add(turret);
                    this.Turrets--;
                }

                //no turret for u :c
            }
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            // Thread spawner = new Thread(new ThreadStart(Instructions));
            // Thread t2 = new Thread(new ThreadStart(() => SpawnMap(out map, out player, out bushField)));

            Console.CursorVisible = false;
            Instructions();
            SpawnMap();

            spawner = new Thread(SpawnEnemies);
            spawner.Start();
            // Thread t2 = new Thread(new ThreadStart(TakeInput));
            TakeInput();

            Console.ResetColor();
            Console.Clear();
        }

        static void SpawnEnemies()
        {
            enemies.Add(new Enemy());

            Random rndm = new Random();
            int interval = rndm.Next(5000, 10000);

            Thread.Sleep(interval);
            Thread.Yield();

            if (_run)
            {
                SpawnEnemies();
            }
        }

        static void ShutDown()
        {
          _run = false;

          foreach (Enemy enemy in enemies)
          {
            enemy.Alive = false;
          }

          enemies.Clear();
        }

        static void TakeInput()
        {
            bool loopCondition = true;

            do
            {
                string input = Console.ReadKey(true).Key.ToString();

                switch (input)
                {
                    case "Q":
                        ShutDown();
                        loopCondition = false;

                        break;
                    case "D":
                        // map = new Map();
                        // SpawnMap(out map, out player, out bushField, out coordinateMatrix);
                        SpawnMap();
                        break;
                    case "T":
                        string tmp = Console.ReadKey(true).Key.ToString();

                        switch (tmp)
                        {
                            case "A":
                                player.PlaceTurret("UpArrow");
                                break;
                            case "S":
                                player.PlaceTurret("DownArrow");
                                break;
                            case "D":
                                player.PlaceTurret("RightArrow");
                                break;
                            case "W":
                                player.PlaceTurret("UpArrow");
                                break;
                            default:
                                player.Move(tmp);
                                break;
                        }

                        break;

                    case "F":
                        string fire = Console.ReadKey(true).Key.ToString();

                        switch (fire)
                        {
                            case "A":
                                player.Shoot("LeftArrow");
                                break;
                            case "S":
                                player.Shoot("DownArrow");
                                break;
                            case "D":
                                player.Shoot("RightArrow");
                                break;
                            case "W":
                                player.Shoot("UpArrow");
                                break;
                            default:
                                player.Move(fire);
                                break;
                        }

                        break;
                    default:
                        player.Move(input);
                        break;
                }
            } while (loopCondition);
        }

        static void SpawnMap()
        {
            Console.ResetColor();

            coordinateMatrix = new bool[Console.WindowWidth, Console.WindowHeight, 5];

            turrets.Clear();
            enemies.Clear();

            map = new Map();
            Random rndm = new Random();
            int length = rndm.Next(5, 10);

            bushField = new BushField[length];
            for (int i = 0; i < length; i++)
            {
                bushField[i] = new BushField();
            }

            #region SpawnEntities
                player = new Player();
            #endregion
        }

        static void Instructions()
        {
            Console.Clear();
            System.Console.WriteLine("Please keep in mind that this\ngame is nothing meant to be taken\nseriously. I made this a while ago to practice object oriented programming.\nAlso, it is not finished and has a ton of bugs in it.\n\n");
            System.Console.WriteLine("I literally don't have any idea why lasers shoot up instead of shooting left...\nDeal with it\n\n");
            Console.WriteLine("Use the arrow keys to move around");
            Console.WriteLine("Press 'Q' to exit");
            Console.WriteLine("Press 'D' to redraw the map");
            Console.WriteLine("Press 'T' and then 'W/A/S/D' to place a turret");
            Console.WriteLine("\nIf your character turns gold, you have found a turret in the bushes!");
            Console.WriteLine("\n\nPress any key to begin");

            Console.ReadKey(true);
        }
    }
}

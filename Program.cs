using SFML.Learning;
using SFML.Audio;
using System;
using System.IO;
using System.Reflection;
using SFML.Graphics;
using SFML.Window;

namespace _7_sfml
{
    public class Program
    {
        public static void Main()
        {
            using (var myGame = new MyGame())
            {
                myGame.Start();
            }
        }
    }

    public class Collider
    {
        public string name;
        public int originX;
        public int originY;
        public int width;
        public int height;
    }

    public class MyGame : Game, IDisposable
    {
        private Music music;
        private string crash;
        private string beep;

        private string bg;
        private string bgObjectPack;
        private string playerObjectPack;
        private string collectObjectPack;
        private int time;
        private int timeCollectable;
        private int timePlayer;

        private readonly string appDir = Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName;

        private Random rnd;

        private Collider[] colliders = new[] {

            new Collider { name = "Space ship at top left corner", originX = 0, originY = 0, width = 190, height = 130 },
            new Collider { name = "Space station in between", originX = 450, originY = 500, width = 150, height = 200 },
            new Collider { name = "Space ship near station", originX = 320, originY = 500, width = 160, height = 130 },
            new Collider { name = "Satellite at right border", originX = 600, originY = 220, width = 200, height = 128 },
        };

        private enum Direction
        {
            UP,
            DOWN,
            LEFT,
            RIGHT,
            NONE,
        };

        private int ScoreMax;
        private int ScoreCurrent;

        private int collectableWidth = 40;
        private int collectableHeight = 72;

        private bool isTestLocation = false;
        private float currentSpeed = 2;

        public MyGame()
        {
            InitWindow(800, 800, "Cosmic Roadster");
            rnd = new Random();
        }

        public void Start()
        {
            LoadAudio();
            LoadImages();
            SetFont($"{appDir}/assets/arial.ttf");

            music.Play();

            int delay = 10;
            bool isCollision = false;

            while (true)
            {
                Direction prevDirection = Direction.UP;
                float currentPlayerPosX = 100;
                float currentPlayerPosY = 700;

                float currentCollectablePosX = 730;
                float currentCollectablePosY = 730;

                DispatchEvents();

                while (!isCollision)
                {
                    DispatchEvents();
                    ClearWindow(Color.Transparent);

                    var currentDirection = GetDirection(prevDirection, out bool isMoving);

                    DrawStaticObjects();

                    isCollision = DetectPlayerCollision(currentPlayerPosX, currentPlayerPosY, currentDirection, out var collisionName);
                    if (isCollision)
                    {
                        HandleCollision(collisionName);
                    }
                    else
                    {
                        DrawWavingObjects(delay);

                        if (DetectPlayerCollectItem(currentPlayerPosX, currentPlayerPosY, currentDirection, currentCollectablePosX, currentCollectablePosY))
                        {
                            PlaySound(beep, 20);
                            ++ScoreCurrent;
                            GenerateCollectable(out currentCollectablePosX, out currentCollectablePosY);
                        }

                        DrawCollectable(currentCollectablePosX, currentCollectablePosY, delay);
                        prevDirection = DrawPlayer(currentPlayerPosX, currentPlayerPosY, delay, currentDirection, isMoving, out currentPlayerPosX, out currentPlayerPosY);
                    }

                    if (ScoreCurrent > ScoreMax)
                        ScoreMax = ScoreCurrent;

                    if (GetKeyDown(Keyboard.Key.L))
                        isTestLocation = !isTestLocation;

                    if (GetKeyDown(Keyboard.Key.Num1))
                        currentSpeed = 2;

                    if (GetKeyDown(Keyboard.Key.Num2))
                        currentSpeed = 3;

                    if (GetKeyDown(Keyboard.Key.Num3))
                        currentSpeed = 4;

                    if (GetKeyDown(Keyboard.Key.Num4))
                        currentSpeed = 5;

                    DrawInfo();
                    DisplayWindow();
                    Delay(delay);
                }

                var isExit = GetKeyDown(Keyboard.Key.Escape);
                var isRestart = GetKeyDown(Keyboard.Key.Space);

                if (isExit)
                    break;
                if (isRestart)
                {
                    isCollision = false;
                    SetFont($"{appDir}/assets/arial.ttf");
                    ScoreCurrent = 0;
                }

                Delay(delay);
            }
        }

        private bool DetectPlayerCollision(float playerOriginX, float playerOriginY, Direction dir, out string collisionName)
        {
            int playerWidth;
            int playerHeight;

            switch (dir)
            {
                case Direction.UP:
                case Direction.DOWN:
                    playerHeight = 66;
                    playerWidth = 30;
                    break;
                case Direction.RIGHT:
                case Direction.LEFT:
                    playerHeight = 30;
                    playerWidth = 66;
                    break;
                default: throw new Exception("Unsupported direction");
            }

            var leftCornerX = playerOriginX - playerWidth / 2;
            var leftCornerY = playerOriginY - playerHeight / 2;

            collisionName = "Space border ;)";
            if (DetectBorderCollision(leftCornerX, leftCornerY, playerWidth, playerHeight))
                return true;

            if (DetectObjectCollision(leftCornerX, leftCornerY, playerWidth, playerHeight, out string name))
            {
                collisionName = name;
                return true;
            }

            return false;
        }

        private bool DetectPlayerCollectItem(float playerOriginX, float playerOriginY, Direction dir, float collectableX, float collectableY)
        {
            int playerWidth;
            int playerHeight;

            switch (dir)
            {
                case Direction.UP:
                case Direction.DOWN:
                    playerHeight = 66;
                    playerWidth = 30;
                    break;
                case Direction.RIGHT:
                case Direction.LEFT:
                    playerHeight = 30;
                    playerWidth = 66;
                    break;
                default: throw new Exception("Unsupported direction");
            }

            var leftCornerX = playerOriginX - playerWidth / 2;
            var leftCornerY = playerOriginY - playerHeight / 2;

            var leftCornerX2 = collectableX - collectableWidth / 2;
            var leftCornerY2 = collectableY - collectableHeight / 2;

            return DetectBoxCollision(leftCornerX, leftCornerY, playerWidth, playerHeight, leftCornerX2, leftCornerY2, collectableWidth, collectableHeight);
        }

        private bool DetectBorderCollision(float originX, float originY, int width, int height)
        {
            if (originX < 0)
                return true;
            if (originX + width > 800)
                return true;
            if (originY < 0)
                return true;
            if (originY + height > 800)
                return true;

            return false;
        }

        private bool DetectObjectCollision(float originX, float originY, int width, int height, out string collisionName)
        {
            collisionName = string.Empty;

            for (var i = 0; i < colliders.Length; i++)
            {
                collisionName = colliders[i].name;

                if (DetectBoxCollision(originX, originY, width, height, colliders[i].originX, colliders[i].originY, colliders[i].width, colliders[i].height))
                    return true;
            }

            return false;
        }

        private bool DetectBoxCollision(float originX1, float originY1, int width1, int height1, float originX2, float originY2, int width2, int height2)
        {
            for (var i = 0; i < colliders.Length; i++)
            {
                var isLeftSide = originX1 < originX2;
                var isTopSide = originY1 < originY2;
                var isXCollision = false;
                var isYCollision = false;

                if (isLeftSide && originX1 + width1 > originX2)
                    isXCollision = true;
                else if (!isLeftSide && originX1 < originX2 + width2)
                    isXCollision = true;

                if (isTopSide && originY1 + height1 > originY2)
                    isYCollision = true;
                else if (!isTopSide && originY1 < originY2 + height2)
                    isYCollision = true;

                if (isXCollision && isYCollision)
                    return true;
            }

            return false;
        }

        private Direction GetDirection(Direction prev, out bool isMoving)
        {
            var isUp = GetKey(Keyboard.Key.Up);
            var isDown = GetKey(Keyboard.Key.Down);
            var isLeft = GetKey(Keyboard.Key.Left);
            var isRight = GetKey(Keyboard.Key.Right);

            if (isUp)
            {
                isMoving = true;
                return Direction.UP;
            }
            if (isDown)
            {
                isMoving = true;
                return Direction.DOWN;
            }
            if (isLeft)
            {
                isMoving = true;
                return Direction.LEFT;
            }
            if (isRight)
            {
                isMoving = true;
                return Direction.RIGHT;
            }

            isMoving = false;
            return prev;
        }

        private void GenerateCollectable(out float posX, out float posY)
        {
            posX = rnd.Next(5, 800 - 5 - 40);
            posY = rnd.Next(5, 800 - 5 - 72);
            while (DetectObjectCollision(posX, posY, 40, 72, out _))
            {
                posX = rnd.Next(5, 800 - 5 - 40);
                posY = rnd.Next(5, 800 - 5 - 72);
            }
        }

        private void DrawCollectable(float posX, float posY, int delay)
        {
            var angle = Math.PI * 2.5 * timeCollectable / 1000;
            var factor = (float)Math.Sin(angle);
            var factor2 = (float)Math.Cos(angle);

            float posCenterX = posX - collectableWidth / 2 + factor * (float)2.5;
            float posCenterY = posY - collectableHeight / 2 + factor2 * (float)1.5;

            if (isTestLocation)
                FillRectangle(posCenterX, posCenterY, collectableWidth, collectableHeight);
            DrawSprite(collectObjectPack, posCenterX, posCenterY);

            timeCollectable += delay;
            if (angle > Math.PI * 2 * 12)
            {
                timeCollectable = 0;
            }
        }

        private Direction DrawPlayer(float posX, float posY, int delay, Direction dir, bool isMoving, out float currentPlayerPosX, out float currentPlayerPosY)
        {
            int spriteX;
            int spriteY;
            float deltaX = 0;
            float deltaY = 0;
            float speed = isMoving ? currentSpeed : 0;

            switch (dir)
            {
                case Direction.UP:
                    spriteX = 75;
                    spriteY = 0;
                    deltaY -= speed;
                    break;
                case Direction.DOWN:
                    spriteX = 75;
                    spriteY = 75;
                    deltaY += speed;
                    break;
                case Direction.LEFT:
                    spriteX = 0;
                    spriteY = 0;
                    deltaX -= speed;
                    break;
                case Direction.RIGHT:
                    spriteX = 0;
                    spriteY = 75;
                    deltaX += speed;
                    break;
                default:
                    spriteX = 75;
                    spriteY = 0;
                    break;
            }

            var angle = Math.PI * 4 * timePlayer / 1000;
            var factor = (float)Math.Sin(angle);
            var factor2 = (float)Math.Cos(angle);

            currentPlayerPosX = posX + deltaX;
            currentPlayerPosY = posY + deltaY;

            var amplitudeCoef = (float)(isMoving ? 1 : 0.3);
            float posCenterX = posX - 75 / 2 + factor * (float)2.2 * amplitudeCoef + deltaX;
            float posCenterY = posY - 75 / 2 + factor2 * (float)3.3 * amplitudeCoef + deltaY;

            if (isTestLocation)
                FillRectangle(posCenterX, posCenterY, 75, 75);
            DrawSprite(playerObjectPack, posCenterX, posCenterY, spriteX, spriteY, 75, 75);

            timePlayer += delay;
            if (angle > Math.PI * 2 * 12)
                timePlayer = 0;

            return dir;
        }

        private void DrawInfo()
        {
            DrawText(640, 20, $"Score: {ScoreCurrent}", 20);
            DrawText(640, 46, $"Max score: {ScoreMax}", 20);
            DrawText(640, 46 + 34, $"Select speed: 1-4", 16);
            DrawText(640, 46 + 34 + 20, $"Move: Arrows", 16);
        }

        private void HandleCollision(string collisionName)
        {
            SetFont($"{appDir}/assets/comic.ttf");
            DrawText(250, 320, "You collided with", 26);
            DrawText(250, 350, collisionName + "!", 26);
            DrawText(250, 420, "Press SPACE key to restart", 26);
            DrawText(250, 450, "Press ESC key to exit", 26);
            PlaySound(crash, 35);
        }

        private void DrawStaticObjects()
        {
            DrawSprite(bg, 0, 0);
        }

        private void DrawWavingObjects(int delay)
        {
            var angle = Math.PI * 2 * time / 1000;
            var factor = (float)Math.Sin(angle);
            var factor2 = (float)Math.Cos(angle);

            var angle2 = Math.PI * 2 * time / 1000;
            var factor3 = (float)Math.Sin(angle2);
            var factor4 = (float)Math.Cos(angle2);

            var angle3 = Math.PI * 3 * time / 1000;
            var factor5 = (float)Math.Sin(angle3);
            var factor6 = (float)Math.Cos(angle3);

            if (isTestLocation)
                FillRectangle(colliders[0].originX, colliders[0].originY, colliders[0].width, colliders[0].height);
            DrawSprite(bgObjectPack, colliders[0].originX + factor * 5, colliders[0].originY + factor2 * 5, 40, 58, colliders[0].width, colliders[0].height);

            if (isTestLocation)
                FillRectangle(colliders[1].originX, colliders[1].originY, colliders[1].width, colliders[1].height);
            DrawSprite(bgObjectPack, colliders[1].originX + factor3 * 5, colliders[1].originY + factor4 * 4, 345, 212, colliders[1].width, colliders[1].height);

            if (isTestLocation)
                FillRectangle(colliders[2].originX, colliders[2].originY, colliders[2].width, colliders[2].height);
            DrawSprite(bgObjectPack, colliders[2].originX + factor4 * 3, colliders[2].originY + factor3 * 2, 585, 60, colliders[2].width, colliders[2].height);

            if (isTestLocation)
                FillRectangle(colliders[3].originX, colliders[3].originY, colliders[3].width, colliders[3].height);
            DrawSprite(bgObjectPack, colliders[3].originX + factor6 * 4, colliders[3].originY + factor5 * 4, 320, 600, colliders[3].width, colliders[3].height);

            time += delay;
            if (angle2 > Math.PI * 2 * 12)
                time = 0;
        }

        private void LoadAudio()
        {
            music = new Music($"{appDir}/assets/bg_music.wav")
            {
                Loop = true,
                Volume = 25,
            };

            crash = LoadSound($"{appDir}/assets/crash.wav");
            beep = LoadSound($"{appDir}/assets/beep.wav");
        }

        private void LoadImages()
        {
            bg = LoadTexture($"{appDir}/assets/bg_cosmos.jpg");
            bgObjectPack = LoadTexture($"{appDir}/assets/space_objects.png");
            playerObjectPack = LoadTexture($"{appDir}/assets/player.png");
            collectObjectPack = LoadTexture($"{appDir}/assets/collect.png");
        }

        public void Dispose()
        {
            music.Stop();
            music.Dispose();
        }
    }
}

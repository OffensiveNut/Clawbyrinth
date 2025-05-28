using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace Clawbyrinth
{
    public class GameEngine
    {
        private Player player = null!;
        private Level level = null!;
        private Camera camera = null!;
        private int windowWidth;
        private int windowHeight;
        private Stopwatch gameStopwatch = null!;
        private long lastUpdateTime;
        
        // Input buffering for responsiveness
        private Queue<Keys> inputBuffer = new Queue<Keys>();
        private readonly object inputLock = new object();
        
        public GameEngine(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
            gameStopwatch = Stopwatch.StartNew();
            lastUpdateTime = 0;
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Create a simple test level
            level = new Level(windowWidth, windowHeight);
            
            // Create player at starting position
            Point startPos = level.GetStartPosition();
            player = new Player(startPos.X, startPos.Y);
            
            // Create camera
            camera = new Camera(windowWidth, windowHeight);
            camera.SetPosition(player.Position);
        }

        public void Update()
        {
            long currentTime = gameStopwatch.ElapsedMilliseconds;
            float deltaTime = (currentTime - lastUpdateTime) / 1000.0f;
            lastUpdateTime = currentTime;
            
            // Process input buffer for better responsiveness
            ProcessInputBuffer();
            
            // Update game objects with delta time
            player.Update(level, deltaTime);
            
            // Update camera to follow player
            camera.FollowTarget(player.Position);
            camera.Update(deltaTime);
        }

        private void ProcessInputBuffer()
        {
            lock (inputLock)
            {
                while (inputBuffer.Count > 0)
                {
                    Keys key = inputBuffer.Dequeue();
                    ProcessInput(key);
                }
            }
        }

        public void HandleInput(Keys key)
        {
            // Buffer input for processing in the update loop
            lock (inputLock)
            {
                inputBuffer.Enqueue(key);
            }
        }

        private void ProcessInput(Keys key)
        {
            Direction direction = Direction.None;
            
            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    direction = Direction.Up;
                    break;
                case Keys.Down:
                case Keys.S:
                    direction = Direction.Down;
                    break;
                case Keys.Left:
                case Keys.A:
                    direction = Direction.Left;
                    break;
                case Keys.Right:
                case Keys.D:
                    direction = Direction.Right;
                    break;
                case Keys.R:
                    // Reset game
                    InitializeGame();
                    break;
                case Keys.Escape:
                    Application.Exit();
                    break;
                case Keys.Z:
                    // Zoom in
                    camera.AdjustZoom(0.1f);
                    break;
                case Keys.X:
                    // Zoom out
                    camera.AdjustZoom(-0.1f);
                    break;
                case Keys.C:
                    // Reset zoom
                    camera.SetZoom(1.0f);
                    break;
            }
            
            if (direction != Direction.None)
            {
                player.StartMoving(direction, level);
            }
        }

        public void Render(Graphics g)
        {
            // Clear screen with background color #342132
            g.Clear(Color.FromArgb(0x34, 0x21, 0x32));
            
            // Apply camera transformation for world objects
            var graphicsState = g.Save();
            camera.ApplyTransform(g);
            
            // Render level
            level.Render(g);
            
            // Render player
            player.Render(g);
            
            // Restore graphics state for UI rendering
            g.Restore(graphicsState);
            
            // Render UI (not affected by camera)
            RenderUI(g);
        }

        private void RenderUI(Graphics g)
        {
            
        }
    }
}
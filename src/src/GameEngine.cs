using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using Clawbyrinth.Levels;

namespace Clawbyrinth
{
    public class GameEngine
    {
        private Player player = null!;
        private Level level = null!;
        private Camera camera = null!;
        private LevelManager levelManager = null!;
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
            // Initialize level manager and generate levels
            levelManager = new LevelManager();
            levelManager.GenerateAllLevels();
            
            // Load the first generated level
            level = levelManager.GetCurrentLevel(windowWidth, windowHeight);
            
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
            
            // Check for dot collection
            int dotsCollected = level.CollectDots(player.Position.X, player.Position.Y, 
                Definition.PLAYER_COLLISION_SIZE, Definition.PLAYER_COLLISION_SIZE);
            
            // Check for coin collection
            int coinsCollected = level.CollectCoins(player.Position.X, player.Position.Y, 
                Definition.PLAYER_COLLISION_SIZE, Definition.PLAYER_COLLISION_SIZE);
            
            // TODO: Add dot/coin collection feedback/scoring here
            
            // Check for level completion
            if (level.IsLevelComplete(player.Position.X, player.Position.Y, 
                Definition.PLAYER_COLLISION_SIZE, Definition.PLAYER_COLLISION_SIZE))
            {
                AdvanceToNextLevel();
            }
            
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
                    // Restart current level
                    RestartCurrentLevel();
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
                case Keys.L:
                    // Reset level progression
                    ResetLevelProgression();
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
            // Display level information
            string levelText = $"Level: {levelManager.CurrentLevelIndex + 1}/{levelManager.TotalLevels}";
            using (Font font = new Font("Arial", 16, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                g.DrawString(levelText, font, brush, 10, 10);
            }
            
            // Display controls
            string controlsText = "R - Restart Level | L - Reset to Level 1 | ESC - Exit";
            using (Font font = new Font("Arial", 10))
            using (Brush brush = new SolidBrush(Color.LightGray))
            {
                g.DrawString(controlsText, font, brush, 10, windowHeight - 25);
            }
        }

        private void AdvanceToNextLevel()
        {
            Console.WriteLine($"Level {levelManager.CurrentLevelIndex + 1} completed!");
            
            if (levelManager.AdvanceToNextLevel())
            {
                // Load the next level
                level?.Dispose(); // Dispose current level
                level = levelManager.GetCurrentLevel(windowWidth, windowHeight);
                
                // Reset player to new start position
                Point startPos = level.GetStartPosition();
                player = new Player(startPos.X, startPos.Y);
                
                // Reset camera
                camera.SetPosition(player.Position);
                
                Console.WriteLine($"Started Level {levelManager.CurrentLevelIndex + 1}");
            }
            else
            {
                // All levels completed
                Console.WriteLine("Congratulations! All levels completed!");
                // TODO: Show victory screen or return to menu
            }
        }

        private void RestartCurrentLevel()
        {
            // Restart the current level
            level?.Dispose();
            level = levelManager.GetCurrentLevel(windowWidth, windowHeight);
            
            // Reset player to start position
            Point startPos = level.GetStartPosition();
            player = new Player(startPos.X, startPos.Y);
            
            // Reset camera
            camera.SetPosition(player.Position);
            
            Console.WriteLine($"Restarted Level {levelManager.CurrentLevelIndex + 1}");
        }
        
        private void ResetLevelProgression()
        {
            // Reset to level 1
            levelManager.ResetLevels();
            
            level?.Dispose();
            level = levelManager.GetCurrentLevel(windowWidth, windowHeight);
            
            // Reset player to start position
            Point startPos = level.GetStartPosition();
            player = new Player(startPos.X, startPos.Y);
            
            // Reset camera
            camera.SetPosition(player.Position);
            
            Console.WriteLine("Reset to Level 1");
        }

        // Dispose method to clean up resources
        public void Dispose()
        {
            level?.Dispose();
            player?.Dispose();
        }
    }
}
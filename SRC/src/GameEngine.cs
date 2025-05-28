using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Clawbyrinth
{
    public class GameEngine
    {
        private Player player;
        private Level level;
        private int windowWidth;
        private int windowHeight;
        
        public GameEngine(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Create a simple test level
            level = new Level(windowWidth, windowHeight);
            
            // Create player at starting position
            Point startPos = level.GetStartPosition();
            player = new Player(startPos.X, startPos.Y);
        }

        public void Update()
        {
            player.Update(level);
        }

        public void HandleInput(Keys key)
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
            }
            
            if (direction != Direction.None)
            {
                player.StartMoving(direction, level);
            }
        }

        public void Render(Graphics g)
        {
            // Clear screen
            g.Clear(Color.FromArgb(25, 25, 35)); // Dark blue background
            
            // Render level
            level.Render(g);
            
            // Render player
            player.Render(g);
            
            // Render UI
            RenderUI(g);
        }

        private void RenderUI(Graphics g)
        {
            
        }
    }
}
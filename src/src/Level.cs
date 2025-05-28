using System;
using System.Drawing;

namespace Clawbyrinth
{
    public class Level
    {
        private const int GRID_SIZE = 24;
        private const int WALL = 1;
        private const int EMPTY = 0;
        
        private int[,] levelData = null!;
        private int gridWidth;
        private int gridHeight;
        private int windowWidth;
        private int windowHeight;

        public Level(int windowWidth, int windowHeight)
        {
            this.windowWidth = windowWidth;
            this.windowHeight = windowHeight;
            this.gridWidth = windowWidth / GRID_SIZE;
            this.gridHeight = windowHeight / GRID_SIZE;
            
            GenerateLevel();
        }

        private void GenerateLevel()
        {
            levelData = new int[gridWidth, gridHeight];
            
            // Create border walls
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (x == 0 || x == gridWidth - 1 || y == 0 || y == gridHeight - 1)
                    {
                        levelData[x, y] = WALL;
                    }
                    else
                    {
                        levelData[x, y] = EMPTY;
                    }
                }
            }
            
            // Add some internal walls to create a maze-like structure
            Random random = new();
            
            // Add some horizontal walls
            for (int i = 0; i < 3; i++)
            {
                int y = random.Next(2, gridHeight - 2);
                int startX = random.Next(1, gridWidth / 2);
                int endX = random.Next(gridWidth / 2, gridWidth - 1);
                
                for (int x = startX; x < endX; x++)
                {
                    levelData[x, y] = WALL;
                }
            }
            
            // Add some vertical walls
            for (int i = 0; i < 3; i++)
            {
                int x = random.Next(2, gridWidth - 2);
                int startY = random.Next(1, gridHeight / 2);
                int endY = random.Next(gridHeight / 2, gridHeight - 1);
                
                for (int y = startY; y < endY; y++)
                {
                    levelData[x, y] = WALL;
                }
            }
            
            // Add some scattered walls
            for (int i = 0; i < 20; i++)
            {
                int x = random.Next(2, gridWidth - 2);
                int y = random.Next(2, gridHeight - 2);
                
                // Only place wall if it doesn't block the starting area
                if (Math.Abs(x - gridWidth/2) > 2 || Math.Abs(y - gridHeight/2) > 2)
                {
                    levelData[x, y] = WALL;
                }
            }
        }

        public Point GetStartPosition()
        {
            // Start player in the center of the level
            return new Point(gridWidth / 2, gridHeight / 2);
        }

        public bool IsValidPosition(int gridX, int gridY)
        {
            // Check bounds
            if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
                return false;
            
            // Check if position is empty
            return levelData[gridX, gridY] == EMPTY;
        }

        public void Render(Graphics g)
        {
            // Render walls
            using Brush wallBrush = new SolidBrush(Color.FromArgb(80, 80, 120));
            using Pen wallPen = new(Color.FromArgb(120, 120, 160), 1);
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (levelData[x, y] == WALL)
                    {
                        Rectangle wallRect = new(
                            x * GRID_SIZE, 
                            y * GRID_SIZE, 
                            GRID_SIZE, 
                            GRID_SIZE
                        );
                        
                        g.FillRectangle(wallBrush, wallRect);
                        g.DrawRectangle(wallPen, wallRect);
                    }
                }
            }
            
            // Render grid lines (optional, for debugging)
            // using Pen gridPen = new(Color.FromArgb(30, Color.Gray), 1);
            // // Vertical lines
            // for (int x = 0; x <= gridWidth; x++)
            // {
            //     g.DrawLine(gridPen, x * GRID_SIZE, 0, x * GRID_SIZE, windowHeight);
            // }
            
            // // Horizontal lines
            // for (int y = 0; y <= gridHeight; y++)
            // {
            //     g.DrawLine(gridPen, 0, y * GRID_SIZE, windowWidth, y * GRID_SIZE);
            // }
        }
    }
}
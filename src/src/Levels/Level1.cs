using System;
using System.Collections.Generic;
using System.Drawing;

namespace Clawbyrinth.Levels
{
    /// <summary>
    /// Level 1 implementation that loads its blueprint from an external file.
    /// </summary>
    public class Level1 : Level
    {
        private FileLevelDefinition levelDefinition;

        public Level1(int windowWidth, int windowHeight)
            : base(windowWidth, windowHeight, new FileLevelDefinition("Level1.txt"))
        {
            levelDefinition = new FileLevelDefinition("Level1.txt");
        }

        public override Point GetStartPosition()
        {
            return levelDefinition.StartPosition;
        }

        public new Point GetFinishPosition()
        {
            return levelDefinition.FinishPosition;
        }
        
        /// <summary>
        /// Gets the level name for display purposes.
        /// </summary>
        public string GetLevelName()
        {
            return levelDefinition.LevelName;
        }
        
        /// <summary>
        /// Gets any special positions (traps, portals, etc.) in this level.
        /// </summary>
        public Dictionary<char, List<Point>> GetSpecialPositions()
        {
            return levelDefinition.SpecialPositions;
        }
        
        /// <summary>
        /// Checks if the player has reached the finish position.
        /// </summary>
        public override bool IsLevelComplete(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            Point finishPos = GetFinishPosition();
            if (finishPos == Point.Empty) return false;
            
            // Convert finish position from grid to pixel coordinates
            int finishPixelX = Definition.GridToPixel(finishPos.X);
            int finishPixelY = Definition.GridToPixel(finishPos.Y);
            
            // Check if player's collision box overlaps with the finish area (2x2 grid area)
            Rectangle playerRect = new Rectangle((int)playerX, (int)playerY, playerWidth, playerHeight);
            Rectangle finishRect = new Rectangle(finishPixelX, finishPixelY, 
                Definition.GRID_SIZE * 2, Definition.GRID_SIZE * 2);
            
            return playerRect.IntersectsWith(finishRect);
        }
    }
}
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

        public Point GetFinishPosition()
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
    }
}
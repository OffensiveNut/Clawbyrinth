using System;
using System.Collections.Generic;
using System.Drawing;

namespace Clawbyrinth.Levels
{
    /// <summary>
    /// Generic level class that can load any level from a template file.
    /// </summary>
    public class GenericLevel : Level
    {
        private FileLevelDefinition levelDefinition;

        /// <summary>
        /// Creates a level from a template file.
        /// </summary>
        /// <param name="windowWidth">Window width in pixels</param>
        /// <param name="windowHeight">Window height in pixels</param>
        /// <param name="templateFileName">Name of the template file (e.g., "Level1.txt")</param>
        public GenericLevel(int windowWidth, int windowHeight, string templateFileName)
            : base(windowWidth, windowHeight, new FileLevelDefinition(templateFileName))
        {
            levelDefinition = new FileLevelDefinition(templateFileName);
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
    
    /// <summary>
    /// Factory class for creating levels.
    /// </summary>
    public static class LevelFactory
    {
        /// <summary>
        /// Creates a level instance based on the level number or name.
        /// </summary>
        /// <param name="levelIdentifier">Level number (1, 2, 3...) or filename ("Level1.txt")</param>
        /// <param name="windowWidth">Window width in pixels</param>
        /// <param name="windowHeight">Window height in pixels</param>
        /// <returns>Level instance</returns>
        public static Level CreateLevel(string levelIdentifier, int windowWidth, int windowHeight)
        {
            string fileName;
            
            // If it's a number, convert to filename
            if (int.TryParse(levelIdentifier, out int levelNumber))
            {
                fileName = $"Level{levelNumber}.txt";
            }
            else if (levelIdentifier.EndsWith(".txt"))
            {
                fileName = levelIdentifier;
            }
            else
            {
                fileName = $"{levelIdentifier}.txt";
            }
            
            // Check if we have a specific class for this level
            switch (levelIdentifier.ToLower())
            {
                case "1":
                case "level1":
                case "level1.txt":
                    return new Level1(windowWidth, windowHeight);
                default:
                    return new GenericLevel(windowWidth, windowHeight, fileName);
            }
        }
        
        /// <summary>
        /// Gets a list of available level template files.
        /// </summary>
        /// <returns>List of available level filenames</returns>
        public static List<string> GetAvailableLevels()
        {
            List<string> levels = new List<string>();
            
            try
            {
                string templatesPath = Definition.MAP_TEMPLATES_PATH;
                if (System.IO.Directory.Exists(templatesPath))
                {
                    string[] files = System.IO.Directory.GetFiles(templatesPath, "*.txt");
                    foreach (string file in files)
                    {
                        levels.Add(System.IO.Path.GetFileName(file));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting available levels: {ex.Message}");
            }
            
            return levels;
        }
    }
}

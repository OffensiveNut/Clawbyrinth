using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Clawbyrinth.Levels
{
    /// <summary>
    /// Utility class for creating and managing level template files.
    /// </summary>
    public static class LevelCreator
    {
        /// <summary>
        /// Creates a new level template file with the specified dimensions.
        /// </summary>
        /// <param name="fileName">Name of the level file (e.g., "Level2.txt")</param>
        /// <param name="width">Width of the level in grid cells</param>
        /// <param name="height">Height of the level in grid cells</param>
        /// <param name="addBorder">Whether to add a border of walls around the level</param>
        public static void CreateEmptyLevel(string fileName, int width, int height, bool addBorder = true)
        {
            try
            {
                string filePath = Path.Combine(Definition.MAP_TEMPLATES_PATH, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    for (int y = 0; y < height; y++)
                    {
                        string line = "";
                        for (int x = 0; x < width; x++)
                        {
                            if (addBorder && (x == 0 || x == width - 1 || y == 0 || y == height - 1))
                            {
                                line += Definition.WALL_CHAR;
                            }
                            else
                            {
                                line += Definition.EMPTY_CHAR;
                            }
                        }
                        writer.WriteLine(line);
                    }
                }
                
                Console.WriteLine($"Created empty level template: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating level template: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validates a level template file for common issues.
        /// </summary>
        /// <param name="fileName">Name of the level file to validate</param>
        /// <returns>List of validation messages</returns>
        public static List<string> ValidateLevel(string fileName)
        {
            List<string> issues = new List<string>();
            
            try
            {
                string[] blueprint = Definition.LoadLevelFromFile(fileName);
                
                if (blueprint.Length == 0)
                {
                    issues.Add("Level file is empty or could not be loaded.");
                    return issues;
                }
                
                // Check for consistent row lengths
                int expectedWidth = blueprint[0].Length;
                for (int i = 1; i < blueprint.Length; i++)
                {
                    if (blueprint[i].Length != expectedWidth)
                    {
                        issues.Add($"Row {i + 1} has inconsistent length: expected {expectedWidth}, got {blueprint[i].Length}");
                    }
                }
                
                // Check for start and finish positions
                Point startPos = Definition.FindMarkerCenter(blueprint, Definition.START_CHAR);
                Point finishPos = Definition.FindMarkerCenter(blueprint, Definition.FINISH_CHAR);
                
                bool hasStart = false;
                bool hasFinish = false;
                
                foreach (string row in blueprint)
                {
                    if (row.Contains(Definition.START_CHAR)) hasStart = true;
                    if (row.Contains(Definition.FINISH_CHAR)) hasFinish = true;
                }
                
                if (!hasStart)
                {
                    issues.Add("Level does not contain a start position (S).");
                }
                
                if (!hasFinish)
                {
                    issues.Add("Level does not contain a finish position (F).");
                }
                
                // Check for invalid characters
                HashSet<char> validChars = new HashSet<char>
                {
                    Definition.WALL_CHAR,
                    Definition.EMPTY_CHAR,
                    Definition.START_CHAR,
                    Definition.FINISH_CHAR,
                    Definition.PORTAL_CHAR,
                    Definition.SPIKES_CHAR,
                    Definition.CANNON_CHAR
                };
                
                for (int y = 0; y < blueprint.Length; y++)
                {
                    string row = blueprint[y];
                    for (int x = 0; x < row.Length; x++)
                    {
                        char c = row[x];
                        if (!validChars.Contains(c))
                        {
                            issues.Add($"Invalid character '{c}' at position ({x}, {y}).");
                        }
                    }
                }
                
                if (issues.Count == 0)
                {
                    issues.Add("Level validation passed!");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Error validating level: {ex.Message}");
            }
            
            return issues;
        }
        
        /// <summary>
        /// Converts an old-style level blueprint (with spaces) to new-style (with dots).
        /// </summary>
        /// <param name="oldBlueprint">Old blueprint with spaces for empty areas</param>
        /// <returns>New blueprint with dots for empty areas</returns>
        public static string[] ConvertOldBlueprint(string[] oldBlueprint)
        {
            string[] newBlueprint = new string[oldBlueprint.Length];
            
            for (int i = 0; i < oldBlueprint.Length; i++)
            {
                newBlueprint[i] = oldBlueprint[i].Replace(' ', Definition.EMPTY_CHAR);
            }
            
            return newBlueprint;
        }
        
        /// <summary>
        /// Saves a blueprint to a file.
        /// </summary>
        /// <param name="fileName">Name of the file to save</param>
        /// <param name="blueprint">Blueprint to save</param>
        public static void SaveLevel(string fileName, string[] blueprint)
        {
            try
            {
                string filePath = Path.Combine(Definition.MAP_TEMPLATES_PATH, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                File.WriteAllLines(filePath, blueprint);
                Console.WriteLine($"Saved level template: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving level template: {ex.Message}");
            }
        }
    }
}

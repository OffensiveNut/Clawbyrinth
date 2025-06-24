using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Clawbyrinth.ProceduralGeneration;

namespace Clawbyrinth.Levels
{
    /// <summary>
    /// Manages level progression and procedural level generation for the game.
    /// Handles generating levels, saving them to disk, and managing player progression.
    /// </summary>
    public class LevelManager
    {
        private const int MAX_LEVELS = 10; // Generate 10 levels for progression
        private const string GENERATED_LEVELS_PATH = "src/Levels/Generated";
        
        private int currentLevelIndex;
        private List<string> generatedLevelFiles;
        private bool levelsGenerated;
        
        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalLevels => generatedLevelFiles?.Count ?? 0;
        public bool HasMoreLevels => currentLevelIndex < TotalLevels - 1;
        
        public LevelManager()
        {
            currentLevelIndex = 0;
            generatedLevelFiles = new List<string>();
            levelsGenerated = false;
        }
        
        /// <summary>
        /// Generates all levels for the game session and saves them to disk.
        /// </summary>
        public void GenerateAllLevels()
        {
            if (levelsGenerated) return;
            
            Console.WriteLine("Generating procedural levels...");
            
            // Ensure the generated levels directory exists
            string fullPath = Path.Combine(Environment.CurrentDirectory, GENERATED_LEVELS_PATH);
            Directory.CreateDirectory(fullPath);
            
            // Clear any existing generated files
            foreach (string file in Directory.GetFiles(fullPath, "Level*.txt"))
            {
                File.Delete(file);
            }
            
            generatedLevelFiles.Clear();
            
            // Generate levels using the procedural generator
            var generator = new ProceduralMapGenerator("src/Levels/Map Templates/");
            
            for (int i = 1; i <= MAX_LEVELS; i++)
            {
                try
                {
                    string difficulty = GetDifficultyForLevel(i);
                    string outputFile = Path.Combine(fullPath, $"Level{i}.txt");
                    
                    // Generate the level
                    string levelContent = generator.GenerateProceduralLevel(difficulty, 3, 5);
                    
                    // Write to file
                    File.WriteAllText(outputFile, levelContent);
                    
                    if (File.Exists(outputFile))
                    {
                        generatedLevelFiles.Add($"Level{i}.txt");
                        Console.WriteLine($"Generated Level {i} ({difficulty})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to generate Level {i}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating Level {i}: {ex.Message}");
                }
            }
            
            levelsGenerated = true;
            Console.WriteLine($"Generated {generatedLevelFiles.Count} levels successfully.");
        }
        
        /// <summary>
        /// Gets the current level instance.
        /// </summary>
        public Level GetCurrentLevel(int windowWidth, int windowHeight)
        {
            if (!levelsGenerated)
            {
                GenerateAllLevels();
            }
            
            if (currentLevelIndex >= generatedLevelFiles.Count)
            {
                // No more levels, return a default level or handle end game
                Console.WriteLine("No more levels available. Creating default level.");
                return new Level(windowWidth, windowHeight);
            }
            
            string levelFileName = generatedLevelFiles[currentLevelIndex];
            return new GeneratedLevel(windowWidth, windowHeight, levelFileName);
        }
        
        /// <summary>
        /// Advances to the next level.
        /// </summary>
        /// <returns>True if there's a next level, false if this was the last level</returns>
        public bool AdvanceToNextLevel()
        {
            if (HasMoreLevels)
            {
                currentLevelIndex++;
                Console.WriteLine($"Advancing to Level {currentLevelIndex + 1}");
                return true;
            }
            
            Console.WriteLine("All levels completed!");
            return false;
        }
        
        /// <summary>
        /// Resets level progression to the beginning.
        /// </summary>
        public void ResetLevels()
        {
            currentLevelIndex = 0;
            Console.WriteLine("Level progression reset to Level 1");
        }
        
        /// <summary>
        /// Determines the difficulty setting for a given level number.
        /// </summary>
        private string GetDifficultyForLevel(int levelNumber)
        {
            // Progressive difficulty
            return levelNumber switch
            {
                <= 3 => "easy",
                <= 6 => "medium", 
                <= 8 => "hard",
                _ => "expert"
            };
        }
        
        /// <summary>
        /// Checks if all generated levels exist on disk.
        /// </summary>
        public bool AllLevelsExist()
        {
            string fullPath = Path.Combine(Environment.CurrentDirectory, GENERATED_LEVELS_PATH);
            
            for (int i = 1; i <= MAX_LEVELS; i++)
            {
                string levelFile = Path.Combine(fullPath, $"Level{i}.txt");
                if (!File.Exists(levelFile))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Forces regeneration of all levels.
        /// </summary>
        public void ForceRegenerate()
        {
            levelsGenerated = false;
            GenerateAllLevels();
        }
    }
}

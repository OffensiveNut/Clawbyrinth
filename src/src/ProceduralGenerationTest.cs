using System;
using System.IO;
using Clawbyrinth.ProceduralGeneration;

namespace Clawbyrinth
{
    /// <summary>
    /// Console application to test procedural map generation
    /// </summary>
    public static class ProceduralGenerationTest
    {
        public static void RunProceduralGenerationTest(string[] args)
        {
            try
            {
                Console.WriteLine("=== Clawbyrinth Procedural Map Generation ===");
                Console.WriteLine();

                // Set up paths
                string currentDirectory = Directory.GetCurrentDirectory();
                string mapTemplatesPath = Path.Combine(currentDirectory, "src", "Levels", "Map Templates");
                
                if (!Directory.Exists(mapTemplatesPath))
                {
                    Console.WriteLine($"Error: Map templates directory not found at: {mapTemplatesPath}");
                    Console.WriteLine("Current directory: " + currentDirectory);
                    return;
                }

                // Initialize the procedural map generator
                var generator = new ProceduralMapGenerator(mapTemplatesPath);

                // Show available difficulties
                var difficulties = generator.GetAvailableDifficulties();
                Console.WriteLine("Available difficulties:");
                foreach (var difficulty in difficulties)
                {
                    int mapCount = generator.GetMapCount(difficulty);
                    Console.WriteLine($"  - {difficulty}: {mapCount} maps");
                }
                Console.WriteLine();

                // Generate levels for each difficulty
                foreach (var difficulty in difficulties)
                {
                    if (generator.GetMapCount(difficulty) >= 2)
                    {
                        Console.WriteLine($"============================================================");
                        Console.WriteLine($"GENERATING PROCEDURAL LEVEL ({difficulty.ToUpper()})");
                        Console.WriteLine($"============================================================");

                        string result = generator.GenerateProceduralLevel(difficulty, 3, 5);
                        
                        if (result.StartsWith("Error:"))
                        {
                            Console.WriteLine(result);
                        }
                        else
                        {
                            // Save the generated level
                            string outputPath = Path.Combine(currentDirectory, $"generated_level_{difficulty}.txt");
                            File.WriteAllText(outputPath, result);
                            
                            Console.WriteLine($"Generated level saved to: {outputPath}");
                            
                            // Show first few lines
                            string[] lines = result.Split('\n');
                            Console.WriteLine("First few lines of generated level:");
                            for (int i = 0; i < Math.Min(10, lines.Length); i++)
                            {
                                Console.WriteLine(lines[i]);
                            }
                            Console.WriteLine("...");
                        }
                        Console.WriteLine();
                    }
                }

                Console.WriteLine("Procedural generation test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during procedural generation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Procedural generation test completed!");
        }
    }
}

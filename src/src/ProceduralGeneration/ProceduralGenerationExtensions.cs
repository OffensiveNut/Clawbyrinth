using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clawbyrinth.ProceduralGeneration
{
    /// <summary>
    /// Extension methods and utilities for procedural map generation
    /// </summary>
    public static class ProceduralGenerationExtensions
    {
        /// <summary>
        /// Get the content bounds aligned to 2x2 chunks
        /// </summary>
        public static (int minX, int minY, int maxX, int maxY)? GetContentBoundsChunked(this List<List<char>> grid)
        {
            if (!grid.Any() || !grid[0].Any())
                return null;

            int gridHeight = grid.Count;
            int gridWidth = grid[0].Count;

            // Find which 2x2 chunks contain content
            int chunkWidth = gridWidth / 2;
            int chunkHeight = gridHeight / 2;

            int minChunkX = chunkWidth;
            int minChunkY = chunkHeight;
            int maxChunkX = -1;
            int maxChunkY = -1;

            bool contentFound = false;

            // Check each 2x2 chunk
            for (int chunkY = 0; chunkY < chunkHeight; chunkY++)
            {
                for (int chunkX = 0; chunkX < chunkWidth; chunkX++)
                {
                    // Check if this 2x2 chunk has any non-dot content
                    bool chunkHasContent = false;

                    for (int dy = 0; dy < 2 && !chunkHasContent; dy++)
                    {
                        for (int dx = 0; dx < 2 && !chunkHasContent; dx++)
                        {
                            int gridX = chunkX * 2 + dx;
                            int gridY = chunkY * 2 + dy;

                            if (gridY < gridHeight && gridX < gridWidth && grid[gridY][gridX] != '.')
                            {
                                chunkHasContent = true;
                            }
                        }
                    }

                    // If this chunk has content, include it in bounds
                    if (chunkHasContent)
                    {
                        minChunkX = Math.Min(minChunkX, chunkX);
                        minChunkY = Math.Min(minChunkY, chunkY);
                        maxChunkX = Math.Max(maxChunkX, chunkX);
                        maxChunkY = Math.Max(maxChunkY, chunkY);
                        contentFound = true;
                    }
                }
            }

            // Return null if no content found
            if (!contentFound)
                return null;

            // Convert chunk coordinates back to grid coordinates
            int minX = minChunkX * 2;
            int minY = minChunkY * 2;
            int maxX = (maxChunkX + 1) * 2 - 1; // End of the chunk
            int maxY = (maxChunkY + 1) * 2 - 1; // End of the chunk

            return (minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Crop grid to content bounds aligned to 2x2 chunks (like DrawTool)
        /// </summary>
        public static (List<List<char>> grid, List<List<char>> trapLayer, int width, int height, int minX, int minY) 
            CropToContentChunked(this List<List<char>> grid, List<List<char>> trapLayer)
        {
            var bounds = grid.GetContentBoundsChunked();

            // If no content, return a minimal 2x2 grid
            if (bounds == null)
            {
                var emptyGrid = new List<List<char>>
                {
                    new List<char> { '.', '.' },
                    new List<char> { '.', '.' }
                };
                var emptyTrap = new List<List<char>>
                {
                    new List<char> { '.', '.' },
                    new List<char> { '.', '.' }
                };
                return (emptyGrid, emptyTrap, 2, 2, 0, 0);
            }

            var (minX, minY, maxX, maxY) = bounds.Value;

            // Align to 2x2 chunk boundaries
            int cropMinX = (minX / 2) * 2;
            int cropMinY = (minY / 2) * 2;
            int cropMaxX = ((maxX + 1) / 2) * 2 - 1;
            int cropMaxY = ((maxY + 1) / 2) * 2 - 1;

            // Ensure we don't go outside the grid
            cropMinX = Math.Max(0, cropMinX);
            cropMinY = Math.Max(0, cropMinY);
            cropMaxX = Math.Min(grid[0].Count - 1, cropMaxX);
            cropMaxY = Math.Min(grid.Count - 1, cropMaxY);

            // Calculate cropped dimensions
            int cropWidth = cropMaxX - cropMinX + 1;
            int cropHeight = cropMaxY - cropMinY + 1;

            // Extract the cropped data
            var croppedGrid = new List<List<char>>();
            var croppedTrap = new List<List<char>>();

            for (int y = cropMinY; y < cropMinY + cropHeight; y++)
            {
                var gridRow = new List<char>();
                var trapRow = new List<char>();

                for (int x = cropMinX; x < cropMinX + cropWidth; x++)
                {
                    gridRow.Add(grid[y][x]);
                    trapRow.Add(trapLayer[y][x]);
                }

                croppedGrid.Add(gridRow);
                croppedTrap.Add(trapRow);
            }

            return (croppedGrid, croppedTrap, cropWidth, cropHeight, cropMinX, cropMinY);
        }
    }

    public partial class ProceduralMapGenerator
    {
        /// <summary>
        /// Generate a procedural level by combining maps with retry logic for validation
        /// </summary>
        public string GenerateProceduralLevel(string difficulty = "easy", int numMaps = 3, int maxRetries = 5)
        {
            if (!maps.ContainsKey(difficulty))
                return "Error: Invalid difficulty level";

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                Console.WriteLine($"Generation attempt {attempt + 1}/{maxRetries}");

                try
                {
                    var availableMaps = new List<MapData>(maps[difficulty]);
                    if (availableMaps.Count < numMaps)
                    {
                        Console.WriteLine($"Warning: Only {availableMaps.Count} maps available for difficulty {difficulty}");
                        numMaps = availableMaps.Count;
                    }

                    // Select random maps for combination
                    var selectedMaps = new List<MapData>();
                    for (int i = 0; i < numMaps; i++)
                    {
                        if (!availableMaps.Any()) break;

                        MapData selectedMap;
                        if (i == 0)
                        {
                            // First map - pick randomly
                            selectedMap = availableMaps[random.Next(availableMaps.Count)];
                        }
                        else
                        {
                            // Subsequent maps - find compatible one
                            var currentFinishExits = selectedMaps.Last().FinishExit;
                            selectedMap = GetTransformedCompatibleMap(currentFinishExits, availableMaps);

                            if (selectedMap == null)
                            {
                                Console.WriteLine($"No compatible map found for connection, stopping at {i} maps");
                                break;
                            }
                        }

                        selectedMaps.Add(selectedMap);
                        availableMaps.Remove(availableMaps.FirstOrDefault(m => m.FileName == selectedMap.FileName));
                    }

                    if (selectedMaps.Count < 2)
                    {
                        Console.WriteLine("Error: Could not find enough compatible maps");
                        continue;
                    }

                    // Combine the selected maps
                    var (combinedGrid, combinedTrapLayer) = CombineMaps(selectedMaps, difficulty);

                    // Validate the generated level
                    if (!ValidateLevel(combinedGrid))
                    {
                        Console.WriteLine($"Attempt {attempt + 1}: Level validation failed, retrying...");
                        continue;
                    }

                    Console.WriteLine($"Attempt {attempt + 1}: Level generation successful!");

                    // Format and return the result
                    return FormatCombinedMap(combinedGrid, combinedTrapLayer, selectedMaps);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt + 1}: Generation failed with error: {ex.Message}");
                }
            }

            // If all attempts failed, return an error
            return $"Error: Failed to generate valid level after {maxRetries} attempts";
        }

        /// <summary>
        /// Format the combined map as a string in the original file format with proper chunk-based cropping
        /// </summary>
        public string FormatCombinedMap(List<List<char>> grid, List<List<char>> trapLayer, List<MapData> originalMaps)
        {
            if (!grid.Any())
                return "Error: Empty grid provided";

            // Apply chunk-based cropping like DrawTool
            var (croppedGrid, croppedTrap, cropWidth, cropHeight, cropMinX, cropMinY) = 
                grid.CropToContentChunked(trapLayer);

            var output = new StringBuilder();
            output.AppendLine($"# Grid dimensions: {cropWidth}x{cropHeight}");
            output.AppendLine("# Symbols: 1-9 = Oriented Walls, S = Start, F = Finish, * = Asterisk, , = Accessible, . = Empty");
            output.AppendLine("# Trap Layer: ! = Spike 1, ? = Spike 2, N = Cannon, . = Empty");
            output.AppendLine();

            // Add the cropped main grid
            foreach (var row in croppedGrid)
            {
                output.AppendLine(new string(row.ToArray()));
            }

            output.AppendLine();
            output.AppendLine("# Trap Layer");

            // Add the cropped trap layer
            foreach (var row in croppedTrap)
            {
                output.AppendLine(new string(row.ToArray()));
            }

            // Find and adjust start/finish positions in the cropped grid
            (int x, int y)? adjustedStart = null;
            (int x, int y)? adjustedFinish = null;

            for (int y = 0; y < croppedGrid.Count; y++)
            {
                for (int x = 0; x < croppedGrid[0].Count; x++)
                {
                    if (croppedGrid[y][x] == 'S' && !adjustedStart.HasValue)
                        adjustedStart = (x, y);
                    else if (croppedGrid[y][x] == 'F' && !adjustedFinish.HasValue)
                        adjustedFinish = (x, y);
                }
            }

            // Add metadata
            if (adjustedStart.HasValue)
                output.AppendLine($"start : {adjustedStart.Value.x},{adjustedStart.Value.y}");
            if (adjustedFinish.HasValue)
                output.AppendLine($"finish : {adjustedFinish.Value.x},{adjustedFinish.Value.y}");

            // Determine entry/exit directions from the first and last maps
            if (originalMaps.Any())
            {
                var firstMap = originalMaps.First();
                var lastMap = originalMaps.Last();

                if (firstMap.StartEntry.Any())
                    output.AppendLine($"Possible Start Entry : {string.Join(",", firstMap.StartEntry)}");
                if (lastMap.FinishExit.Any())
                    output.AppendLine($"Possible Finish Exit : {string.Join(",", lastMap.FinishExit)}");
            }

            // Add generation info
            var mapNames = originalMaps.Select(m => $"'{m.FileName}'").ToList();
            output.AppendLine($"# Generated from {originalMaps.Count} maps: [{string.Join(", ", mapNames)}]");

            return output.ToString();
        }

        /// <summary>
        /// Get available difficulties
        /// </summary>
        public List<string> GetAvailableDifficulties()
        {
            return maps.Keys.Where(difficulty => maps[difficulty].Any()).ToList();
        }

        /// <summary>
        /// Get map count for a difficulty
        /// </summary>
        public int GetMapCount(string difficulty)
        {
            return maps.ContainsKey(difficulty) ? maps[difficulty].Count : 0;
        }
    }
}

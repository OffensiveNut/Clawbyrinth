using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clawbyrinth.ProceduralGeneration
{
    /// <summary>
    /// Main class for procedural map generation
    /// </summary>
    public partial class ProceduralMapGenerator
    {
        private readonly string mapDirectory;
        private readonly Dictionary<string, List<MapData>> maps;
        private readonly Random random;

        public ProceduralMapGenerator(string mapDirectory)
        {
            this.mapDirectory = mapDirectory;
            this.random = new Random();
            this.maps = LoadMaps();
        }

        /// <summary>
        /// Load and parse all map files organized by difficulty
        /// </summary>
        private Dictionary<string, List<MapData>> LoadMaps()
        {
            var maps = new Dictionary<string, List<MapData>>
            {
                {"easy", new List<MapData>()},
                {"medium", new List<MapData>()},
                {"hard", new List<MapData>()},
                {"insane", new List<MapData>()}
            };

            if (!Directory.Exists(mapDirectory))
            {
                Console.WriteLine($"Warning: Map directory {mapDirectory} does not exist");
                return maps;
            }

            foreach (var filename in Directory.GetFiles(mapDirectory, "*.txt"))
            {
                var fileNameOnly = Path.GetFileName(filename);
                string difficulty = null;

                foreach (var diff in maps.Keys)
                {
                    if (fileNameOnly.StartsWith(diff, StringComparison.OrdinalIgnoreCase))
                    {
                        difficulty = diff;
                        break;
                    }
                }

                if (difficulty != null)
                {
                    var mapData = new MapData(fileNameOnly);
                    mapData.ParseFile(filename);
                    maps[difficulty].Add(mapData);
                    Console.WriteLine($"Loaded {fileNameOnly}: {mapData.Width}x{mapData.Height}, " +
                                    $"Start: [{string.Join(", ", mapData.StartEntry)}], " +
                                    $"Finish: [{string.Join(", ", mapData.FinishExit)}]");
                }
            }

            return maps;
        }

        /// <summary>
        /// Rotate a map 90 degrees clockwise
        /// </summary>
        public MapData RotateMap90Clockwise(MapData mapData)
        {
            var rotated = new MapData($"rotated_90_{mapData.FileName}");

            // Rotate grid
            int oldHeight = mapData.Grid.Count;
            int oldWidth = mapData.Grid[0].Count;
            rotated.Width = oldHeight;
            rotated.Height = oldWidth;

            rotated.Grid = new List<List<char>>();
            for (int i = 0; i < rotated.Height; i++)
            {
                rotated.Grid.Add(new List<char>(new char[rotated.Width]));
            }

            for (int y = 0; y < oldHeight; y++)
            {
                for (int x = 0; x < oldWidth; x++)
                {
                    int newX = oldHeight - 1 - y;
                    int newY = x;
                    rotated.Grid[newY][newX] = mapData.Grid[y][x];
                }
            }

            // Rotate trap layer if it exists
            if (mapData.TrapLayer.Any())
            {
                rotated.TrapLayer = new List<List<char>>();
                for (int i = 0; i < rotated.Height; i++)
                {
                    rotated.TrapLayer.Add(new List<char>(new char[rotated.Width]));
                }

                for (int y = 0; y < mapData.TrapLayer.Count; y++)
                {
                    for (int x = 0; x < mapData.TrapLayer[0].Count; x++)
                    {
                        int newX = mapData.TrapLayer.Count - 1 - y;
                        int newY = x;
                        rotated.TrapLayer[newY][newX] = mapData.TrapLayer[y][x];
                    }
                }
            }

            // Rotate positions
            if (mapData.StartPos.HasValue)
            {
                var (oldX, oldY) = mapData.StartPos.Value;
                rotated.StartPos = (oldHeight - 1 - oldY, oldX);
            }

            if (mapData.FinishPos.HasValue)
            {
                var (oldX, oldY) = mapData.FinishPos.Value;
                rotated.FinishPos = (oldHeight - 1 - oldY, oldX);
            }

            // Rotate entry/exit directions
            var directionRotation = new Dictionary<string, string>
            {
                {"up", "right"}, {"right", "down"}, {"down", "left"}, {"left", "up"}
            };

            rotated.StartEntry = mapData.StartEntry.Select(d => directionRotation.ContainsKey(d) ? directionRotation[d] : d).ToList();
            rotated.FinishExit = mapData.FinishExit.Select(d => directionRotation.ContainsKey(d) ? directionRotation[d] : d).ToList();

            return rotated;
        }

        /// <summary>
        /// Mirror a map horizontally
        /// </summary>
        public MapData MirrorMapHorizontal(MapData mapData)
        {
            var mirrored = new MapData($"mirrored_h_{mapData.FileName}")
            {
                Width = mapData.Width,
                Height = mapData.Height
            };

            // Mirror grid
            mirrored.Grid = new List<List<char>>();
            foreach (var row in mapData.Grid)
            {
                var mirroredRow = new List<char>(row);
                mirroredRow.Reverse();
                mirrored.Grid.Add(mirroredRow);
            }

            // Mirror trap layer
            if (mapData.TrapLayer.Any())
            {
                mirrored.TrapLayer = new List<List<char>>();
                foreach (var row in mapData.TrapLayer)
                {
                    var mirroredRow = new List<char>(row);
                    mirroredRow.Reverse();
                    mirrored.TrapLayer.Add(mirroredRow);
                }
            }

            // Mirror positions
            if (mapData.StartPos.HasValue)
            {
                var (oldX, oldY) = mapData.StartPos.Value;
                mirrored.StartPos = (mapData.Width - 1 - oldX, oldY);
            }

            if (mapData.FinishPos.HasValue)
            {
                var (oldX, oldY) = mapData.FinishPos.Value;
                mirrored.FinishPos = (mapData.Width - 1 - oldX, oldY);
            }

            // Mirror entry/exit directions
            var directionMirror = new Dictionary<string, string>
            {
                {"left", "right"}, {"right", "left"}, {"up", "up"}, {"down", "down"}
            };

            mirrored.StartEntry = mapData.StartEntry.Select(d => directionMirror.ContainsKey(d) ? directionMirror[d] : d).ToList();
            mirrored.FinishExit = mapData.FinishExit.Select(d => directionMirror.ContainsKey(d) ? directionMirror[d] : d).ToList();

            return mirrored;
        }

        /// <summary>
        /// Mirror a map vertically
        /// </summary>
        public MapData MirrorMapVertical(MapData mapData)
        {
            var mirrored = new MapData($"mirrored_v_{mapData.FileName}")
            {
                Width = mapData.Width,
                Height = mapData.Height
            };

            // Mirror grid (reverse row order)
            mirrored.Grid = new List<List<char>>(mapData.Grid);
            mirrored.Grid.Reverse();

            // Mirror trap layer
            if (mapData.TrapLayer.Any())
            {
                mirrored.TrapLayer = new List<List<char>>(mapData.TrapLayer);
                mirrored.TrapLayer.Reverse();
            }

            // Mirror positions
            if (mapData.StartPos.HasValue)
            {
                var (oldX, oldY) = mapData.StartPos.Value;
                mirrored.StartPos = (oldX, mapData.Height - 1 - oldY);
            }

            if (mapData.FinishPos.HasValue)
            {
                var (oldX, oldY) = mapData.FinishPos.Value;
                mirrored.FinishPos = (oldX, mapData.Height - 1 - oldY);
            }

            // Mirror entry/exit directions
            var directionMirror = new Dictionary<string, string>
            {
                {"up", "down"}, {"down", "up"}, {"left", "left"}, {"right", "right"}
            };

            mirrored.StartEntry = mapData.StartEntry.Select(d => directionMirror.ContainsKey(d) ? directionMirror[d] : d).ToList();
            mirrored.FinishExit = mapData.FinishExit.Select(d => directionMirror.ContainsKey(d) ? directionMirror[d] : d).ToList();

            return mirrored;
        }

        /// <summary>
        /// Find a map with start entry that matches any of the finish exits
        /// </summary>
        private MapData FindCompatibleMap(List<string> finishExits, List<MapData> availableMaps)
        {
            foreach (var mapData in availableMaps)
            {
                foreach (var finishExit in finishExits)
                {
                    if (mapData.StartEntry.Contains(finishExit))
                        return mapData;
                }
            }
            return null;
        }

        /// <summary>
        /// Find or create a compatible map through transformations
        /// </summary>
        private MapData GetTransformedCompatibleMap(List<string> finishExits, List<MapData> availableMaps)
        {
            // First try direct match
            var compatible = FindCompatibleMap(finishExits, availableMaps);
            if (compatible != null)
                return compatible;

            // Try transformations
            foreach (var mapData in availableMaps)
            {
                // Try rotations
                var currentMap = mapData;
                for (int i = 0; i < 3; i++)
                {
                    currentMap = RotateMap90Clockwise(currentMap);
                    foreach (var finishExit in finishExits)
                    {
                        if (currentMap.StartEntry.Contains(finishExit))
                            return currentMap;
                    }
                }

                // Try mirrors
                var hMirrored = MirrorMapHorizontal(mapData);
                foreach (var finishExit in finishExits)
                {
                    if (hMirrored.StartEntry.Contains(finishExit))
                        return hMirrored;
                }

                var vMirrored = MirrorMapVertical(mapData);
                foreach (var finishExit in finishExits)
                {
                    if (vMirrored.StartEntry.Contains(finishExit))
                        return vMirrored;
                }
            }

            return null;
        }

        /// <summary>
        /// Create a connecting path between two maps using the wall tool algorithm
        /// </summary>
        private void CreateConnectingPath(List<List<char>> combinedGrid,
                                        (int x, int y) map1Pos, MapData map1,
                                        (int x, int y) map2Pos, MapData map2,
                                        string connectionDirection)
        {
            var (map1OffsetX, map1OffsetY) = map1Pos;
            var (map2OffsetX, map2OffsetY) = map2Pos;

            // Get finish and start positions in absolute coordinates
            var (finishX, finishY) = map1.FinishPos.Value;
            int absFinishX = map1OffsetX + finishX;
            int absFinishY = map1OffsetY + finishY;

            var (startX, startY) = map2.StartPos.Value;
            int absStartX = map2OffsetX + startX;
            int absStartY = map2OffsetY + startY;

            // Replace 2x2 finish block with asterisks
            for (int dy = 0; dy < 2; dy++)
            {
                for (int dx = 0; dx < 2; dx++)
                {
                    if (absFinishY + dy < combinedGrid.Count &&
                        absFinishX + dx < combinedGrid[0].Count)
                    {
                        combinedGrid[absFinishY + dy][absFinishX + dx] = '*';
                    }
                }
            }

            // Replace 2x2 start block with asterisks
            for (int dy = 0; dy < 2; dy++)
            {
                for (int dx = 0; dx < 2; dx++)
                {
                    if (absStartY + dy < combinedGrid.Count &&
                        absStartX + dx < combinedGrid[0].Count)
                    {
                        combinedGrid[absStartY + dy][absStartX + dx] = '*';
                    }
                }
            }

            // Create the connecting corridor between the two maps
            switch (connectionDirection)
            {
                case "right":
                    CreateHorizontalCorridor(combinedGrid, absFinishX, absFinishY, absStartX, absStartY, true);
                    break;
                case "left":
                    CreateHorizontalCorridor(combinedGrid, absFinishX, absFinishY, absStartX, absStartY, false);
                    break;
                case "down":
                    CreateVerticalCorridor(combinedGrid, absFinishX, absFinishY, absStartX, absStartY, true);
                    break;
                case "up":
                    CreateVerticalCorridor(combinedGrid, absFinishX, absFinishY, absStartX, absStartY, false);
                    break;
            }
        }

        private void CreateHorizontalCorridor(List<List<char>> combinedGrid, int finishX, int finishY, int startX, int startY, bool goingRight)
        {
            int corridorY = finishY;
            int startXPos, endXPos;

            if (goingRight)
            {
                startXPos = finishX + 2; // Start after the finish block
                endXPos = startX - 1;    // End before the start block
            }
            else
            {
                startXPos = finishX - 1; // Start before the finish block
                endXPos = startX + 2;    // End after the start block
            }

            // Create horizontal corridor
            int minX = Math.Min(startXPos, endXPos);
            int maxX = Math.Max(startXPos, endXPos);

            for (int x = minX; x <= maxX; x++)
            {
                if (x >= 0 && x < combinedGrid[0].Count)
                {
                    // Create 2x2 corridor
                    for (int dy = 0; dy < 2; dy++)
                    {
                        if (corridorY + dy < combinedGrid.Count)
                        {
                            combinedGrid[corridorY + dy][x] = '*';
                        }
                    }
                }
            }

            // Add single layer walls above and below the corridor
            for (int x = minX; x <= maxX; x++)
            {
                if (x >= 0 && x < combinedGrid[0].Count)
                {
                    // Wall above
                    if (corridorY - 1 >= 0)
                    {
                        if (combinedGrid[corridorY - 1][x] == '.')
                            combinedGrid[corridorY - 1][x] = '#';
                    }
                    // Wall below
                    if (corridorY + 2 < combinedGrid.Count)
                    {
                        if (combinedGrid[corridorY + 2][x] == '.')
                            combinedGrid[corridorY + 2][x] = '#';
                    }
                }
            }
        }

        private void CreateVerticalCorridor(List<List<char>> combinedGrid, int finishX, int finishY, int startX, int startY, bool goingDown)
        {
            int corridorX = finishX;
            int startYPos, endYPos;

            if (goingDown)
            {
                startYPos = finishY + 2; // Start below the finish block
                endYPos = startY - 1;    // End above the start block
            }
            else
            {
                startYPos = finishY - 1; // Start above the finish block
                endYPos = startY + 2;    // End below the start block
            }

            // Create vertical corridor
            int minY = Math.Min(startYPos, endYPos);
            int maxY = Math.Max(startYPos, endYPos);

            for (int y = minY; y <= maxY; y++)
            {
                if (y >= 0 && y < combinedGrid.Count)
                {
                    // Create 2x2 corridor
                    for (int dx = 0; dx < 2; dx++)
                    {
                        if (corridorX + dx < combinedGrid[0].Count)
                        {
                            combinedGrid[y][corridorX + dx] = '*';
                        }
                    }
                }
            }

            // Add single layer walls left and right of the corridor
            for (int y = minY; y <= maxY; y++)
            {
                if (y >= 0 && y < combinedGrid.Count)
                {
                    // Wall left
                    if (corridorX - 1 >= 0)
                    {
                        if (combinedGrid[y][corridorX - 1] == '.')
                            combinedGrid[y][corridorX - 1] = '#';
                    }
                    // Wall right
                    if (corridorX + 2 < combinedGrid[0].Count)
                    {
                        if (combinedGrid[y][corridorX + 2] == '.')
                            combinedGrid[y][corridorX + 2] = '#';
                    }
                }
            }
        }

        /// <summary>
        /// Combine multiple maps into a single level with connecting paths
        /// </summary>
        public (List<List<char>> grid, List<List<char>> trapLayer) CombineMaps(List<MapData> mapsToCombine, string difficulty = "easy")
        {
            if (!mapsToCombine.Any())
                return (new List<List<char>>(), new List<List<char>>());

            // Start with the first map
            var currentMap = mapsToCombine[0];

            // Calculate total dimensions needed (generous estimate for all directions)
            int maxMapWidth = mapsToCombine.Max(m => m.Width);
            int maxMapHeight = mapsToCombine.Max(m => m.Height);
            int totalWidth = maxMapWidth * mapsToCombine.Count + mapsToCombine.Count * 20 + 100;
            int totalHeight = maxMapHeight * mapsToCombine.Count + mapsToCombine.Count * 20 + 100;

            // Create combined grid
            var combinedGrid = new List<List<char>>();
            var combinedTrapLayer = new List<List<char>>();

            for (int y = 0; y < totalHeight; y++)
            {
                combinedGrid.Add(new List<char>(new char[totalWidth]));
                combinedTrapLayer.Add(new List<char>(new char[totalWidth]));
                for (int x = 0; x < totalWidth; x++)
                {
                    combinedGrid[y][x] = '.';
                    combinedTrapLayer[y][x] = '.';
                }
            }

            // Place first map at center position to allow placement in all directions
            var currentPos = (totalWidth / 2, totalHeight / 2);
            PlaceMapInGrid(combinedGrid, combinedTrapLayer, currentMap, currentPos);

            // Keep track of map positions and data for collision detection
            var placedMaps = new List<((int x, int y) pos, MapData map)> { (currentPos, currentMap) };

            // Combine remaining maps
            for (int i = 1; i < mapsToCombine.Count; i++)
            {
                var nextMap = mapsToCombine[i];

                // Find connection direction (use the first compatible direction)
                string connectionDir = null;
                foreach (var finishExit in currentMap.FinishExit)
                {
                    if (nextMap.StartEntry.Contains(finishExit))
                    {
                        connectionDir = finishExit;
                        break;
                    }
                }

                if (connectionDir == null)
                {
                    Console.WriteLine($"Warning: No compatible connection found between maps {i - 1} and {i}");
                    connectionDir = "right"; // Default
                }

                // Find a valid position that doesn't cause overlap
                var nextPos = FindValidPosition(placedMaps, currentPos, currentMap, nextMap, connectionDir);

                if (nextPos == null)
                {
                    Console.WriteLine($"Warning: Could not find valid position for map {i}, skipping");
                    continue;
                }

                // Place the next map
                PlaceMapInGrid(combinedGrid, combinedTrapLayer, nextMap, nextPos.Value);
                placedMaps.Add((nextPos.Value, nextMap));

                // Create connecting path using wall tool algorithm
                CreateConnectingPath(combinedGrid, currentPos, currentMap, nextPos.Value, nextMap, connectionDir);

                // Update for next iteration
                currentMap = nextMap;
                currentPos = nextPos.Value;
            }

            // Crop to actual content first, but with extra padding for walls
            var (croppedGrid, croppedTrapLayer) = CropToContentWithPadding(combinedGrid, combinedTrapLayer);

            // Then do flood fill and wall typing on the cropped grid
            WallGenerator.FloodFillFromStart(croppedGrid);
            WallGenerator.UpdateWallTypes(croppedGrid);

            return (croppedGrid, croppedTrapLayer);
        }

        /// <summary>
        /// Place a map at the specified position in the combined grid
        /// </summary>
        private void PlaceMapInGrid(List<List<char>> combinedGrid, List<List<char>> combinedTrapLayer,
                                   MapData mapData, (int x, int y) position)
        {
            var (offsetX, offsetY) = position;

            // Place main grid
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    int targetY = offsetY + y;
                    int targetX = offsetX + x;
                    if (targetY >= 0 && targetY < combinedGrid.Count && targetX >= 0 && targetX < combinedGrid[0].Count)
                    {
                        combinedGrid[targetY][targetX] = mapData.Grid[y][x];
                    }
                }
            }

            // Place trap layer if it exists
            if (mapData.TrapLayer.Any())
            {
                for (int y = 0; y < mapData.TrapLayer.Count; y++)
                {
                    for (int x = 0; x < mapData.TrapLayer[0].Count; x++)
                    {
                        int targetY = offsetY + y;
                        int targetX = offsetX + x;
                        if (targetY >= 0 && targetY < combinedTrapLayer.Count && targetX >= 0 && targetX < combinedTrapLayer[0].Count)
                        {
                            if (mapData.TrapLayer[y][x] != '.')
                                combinedTrapLayer[targetY][targetX] = mapData.TrapLayer[y][x];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if placing a new map at the given position would overlap with existing maps
        /// </summary>
        private bool CheckMapCollision(List<((int x, int y) pos, MapData map)> existingPositions,
                                      (int x, int y) newPos, MapData newMap)
        {
            var (newX, newY) = newPos;
            var newBounds = (newX, newY, newX + newMap.Width, newY + newMap.Height);

            foreach (var (existingPos, existingMap) in existingPositions)
            {
                var (existingX, existingY) = existingPos;
                var existingBounds = (existingX, existingY, existingX + existingMap.Width, existingY + existingMap.Height);

                if (RectanglesOverlap(newBounds, existingBounds))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if two rectangles overlap
        /// </summary>
        private bool RectanglesOverlap((int x1, int y1, int x2, int y2) rect1, (int x1, int y1, int x2, int y2) rect2)
        {
            var (x1, y1, x2, y2) = rect1;
            var (x3, y3, x4, y4) = rect2;

            // Add small buffer zone to prevent maps from being too close
            const int buffer = 5;
            x1 -= buffer;
            y1 -= buffer;
            x2 += buffer;
            y2 += buffer;

            // Rectangles don't overlap if one is to the left, right, above, or below the other
            return !(x2 <= x3 || x4 <= x1 || y2 <= y3 || y4 <= y1);
        }

        /// <summary>
        /// Find a valid position for the next map that doesn't cause overlap
        /// </summary>
        private (int x, int y)? FindValidPosition(List<((int x, int y) pos, MapData map)> existingPositions,
                                                 (int x, int y) currentPos, MapData currentMap,
                                                 MapData nextMap, string connectionDir, int maxAttempts = 10)
        {
            const int baseGap = 6;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Increase gap with each attempt to find a non-overlapping position
                int gap = baseGap + (attempt * 2);

                var nextPos = CalculateNextPositionWithGap(currentPos, currentMap, nextMap, connectionDir, gap);

                // Check if this position causes overlap
                if (!CheckMapCollision(existingPositions, nextPos, nextMap))
                    return nextPos;
            }

            return null; // Could not find valid position
        }

        /// <summary>
        /// Calculate where to place the next map based on connection direction with custom gap
        /// </summary>
        private (int x, int y) CalculateNextPositionWithGap((int x, int y) currentPos, MapData currentMap,
                                                           MapData nextMap, string connectionDir, int gap)
        {
            var (currentX, currentY) = currentPos;

            return connectionDir switch
            {
                "right" => (currentX + currentMap.Width + gap,
                           currentY + (currentMap.FinishPos.Value.y - nextMap.StartPos.Value.y)),
                "left" => (currentX - nextMap.Width - gap,
                          currentY + (currentMap.FinishPos.Value.y - nextMap.StartPos.Value.y)),
                "down" => (currentX + (currentMap.FinishPos.Value.x - nextMap.StartPos.Value.x),
                          currentY + currentMap.Height + gap),
                "up" => (currentX + (currentMap.FinishPos.Value.x - nextMap.StartPos.Value.x),
                        currentY - nextMap.Height - gap),
                _ => (currentX + currentMap.Width + gap, currentY)
            };
        }

        /// <summary>
        /// Validate that a generated level doesn't have flood fill leakage
        /// </summary>
        public bool ValidateLevel(List<List<char>> grid)
        {
            if (!grid.Any() || !grid[0].Any())
                return false;

            int width = grid[0].Count;
            int height = grid.Count;

            // Count total accessible cells (commas)
            int accessibleCount = 0;
            int wallCount = 0;
            int totalCells = width * height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char cell = grid[y][x];
                    if (cell == ',')
                        accessibleCount++;
                    else if (cell == '#' || (cell >= '1' && cell <= '9'))
                        wallCount++;
                }
            }

            // Calculate ratio of accessible to total cells
            double accessibleRatio = (double)accessibleCount / totalCells;

            // If more than 50% of the level is accessible, it's likely flood fill leakage
            if (accessibleRatio > 0.5)
            {
                Console.WriteLine($"Validation failed: {accessibleRatio:P2} of level is accessible (likely flood fill leakage)");
                return false;
            }

            // Check if there are at least some walls
            if (wallCount < 10)
            {
                Console.WriteLine($"Validation failed: Only {wallCount} wall cells found");
                return false;
            }

            // Find start and finish
            bool startFound = false;
            bool finishFound = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[y][x] == 'S')
                        startFound = true;
                    else if (grid[y][x] == 'F')
                        finishFound = true;
                }
            }

            if (!startFound)
            {
                Console.WriteLine("Validation failed: No start position found");
                return false;
            }

            if (!finishFound)
            {
                Console.WriteLine("Validation failed: No finish position found");
                return false;
            }

            Console.WriteLine($"Validation passed: {accessibleRatio:P2} accessible, {wallCount} walls");
            return true;
        }

        /// <summary>
        /// Crop the grid to remove unnecessary empty space, but with extra padding for wall generation
        /// </summary>
        private (List<List<char>> grid, List<List<char>> trapLayer) CropToContentWithPadding(List<List<char>> grid, List<List<char>> trapLayer)
        {
            // Find content bounds
            int minX = grid[0].Count, maxX = 0;
            int minY = grid.Count, maxY = 0;

            for (int y = 0; y < grid.Count; y++)
            {
                for (int x = 0; x < grid[0].Count; x++)
                {
                    if (grid[y][x] != '.')
                    {
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            // Add generous padding for wall generation and flood fill containment
            const int padding = 5;
            minX = Math.Max(0, minX - padding);
            minY = Math.Max(0, minY - padding);
            maxX = Math.Min(grid[0].Count - 1, maxX + padding);
            maxY = Math.Min(grid.Count - 1, maxY + padding);

            // Crop both grids
            var croppedGrid = new List<List<char>>();
            var croppedTrap = new List<List<char>>();

            for (int y = minY; y <= maxY; y++)
            {
                var gridRow = new List<char>();
                var trapRow = new List<char>();
                for (int x = minX; x <= maxX; x++)
                {
                    gridRow.Add(grid[y][x]);
                    trapRow.Add(trapLayer[y][x]);
                }
                croppedGrid.Add(gridRow);
                croppedTrap.Add(trapRow);
            }

            return (croppedGrid, croppedTrap);
        }
    }
}

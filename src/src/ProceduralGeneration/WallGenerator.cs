using System;
using System.Collections.Generic;
using System.Linq;

namespace Clawbyrinth.ProceduralGeneration
{
    /// <summary>
    /// Handles wall generation using the same algorithm as the drawing tool
    /// </summary>
    public static class WallGenerator
    {
        /// <summary>
        /// Determine the wall type based on flood-filled accessible areas
        /// Walls face towards the nearest accessible areas
        /// Returns a number 1-9 representing different wall orientations
        /// </summary>
        public static char GetWallType(List<List<char>> grid, int x, int y)
        {
            if (!IsWall(grid, x, y))
                return '.';

            int width = grid[0].Count;
            int height = grid.Count;

            // Check for yellow marks (accessible areas) in all 8 directions
            var directions = new Dictionary<string, (int x, int y)>
            {
                {"top_left", (x-1, y-1)},
                {"top", (x, y-1)},
                {"top_right", (x+1, y-1)},
                {"left", (x-1, y)},
                {"right", (x+1, y)},
                {"bottom_left", (x-1, y+1)},
                {"bottom", (x, y+1)},
                {"bottom_right", (x+1, y+1)}
            };

            // Check which directions have yellow marks (accessible areas)
            var yellowDirs = new List<string>();
            foreach (var direction in directions)
            {
                var (checkX, checkY) = direction.Value;
                if (HasAccessibleMark(grid, checkX, checkY))
                {
                    yellowDirs.Add(direction.Key);
                }
            }

            // Also check for neighboring walls to determine corner/edge type
            var wallNeighbors = new Dictionary<string, bool>
            {
                {"left", IsWall(grid, x - 1, y)},
                {"right", IsWall(grid, x + 1, y)},
                {"top", IsWall(grid, x, y - 1)},
                {"bottom", IsWall(grid, x, y + 1)}
            };

            // Special corner detection based on 2x2 chunk position
            int chunkX = x % 2;
            int chunkY = y % 2;

            // Corner case: wall at bottom-right of 2x2 chunk (chunk_x=1, chunk_y=1)
            if (chunkX == 1 && chunkY == 1)
            {
                if (IsWall(grid, x + 1, y + 1) &&
                    HasAccessibleMark(grid, x, y + 1) &&
                    HasAccessibleMark(grid, x + 1, y))
                {
                    return '3'; // Bottom-right corner
                }
            }

            // Corner case: wall at bottom-left of 2x2 chunk (chunk_x=0, chunk_y=1)
            if (chunkX == 0 && chunkY == 1)
            {
                if (IsWall(grid, x - 1, y + 1) &&
                    HasAccessibleMark(grid, x, y + 1) &&
                    HasAccessibleMark(grid, x - 1, y))
                {
                    return '1'; // Bottom-left corner
                }
            }

            // Corner case: wall at top-right of 2x2 chunk (chunk_x=1, chunk_y=0)
            if (chunkX == 1 && chunkY == 0)
            {
                if (IsWall(grid, x + 1, y - 1) &&
                    HasAccessibleMark(grid, x, y - 1) &&
                    HasAccessibleMark(grid, x + 1, y))
                {
                    return '9'; // Top-right corner
                }
            }

            // Corner case: wall at top-left of 2x2 chunk (chunk_x=0, chunk_y=0)
            if (chunkX == 0 && chunkY == 0)
            {
                if (IsWall(grid, x - 1, y - 1) &&
                    HasAccessibleMark(grid, x, y - 1) &&
                    HasAccessibleMark(grid, x - 1, y))
                {
                    return '7'; // Top-left corner
                }
            }

            // Determine wall orientation based on accessible areas and wall neighbors
            // Corner cases - check for accessible areas in diagonal directions
            if (yellowDirs.Contains("bottom_right") || (yellowDirs.Contains("bottom") && yellowDirs.Contains("right")))
            {
                if (wallNeighbors["right"] && wallNeighbors["bottom"])
                    return '7'; // Top-left corner
            }

            if (yellowDirs.Contains("bottom_left") || (yellowDirs.Contains("bottom") && yellowDirs.Contains("left")))
            {
                if (wallNeighbors["left"] && wallNeighbors["bottom"])
                    return '9'; // Top-right corner
            }

            if (yellowDirs.Contains("top_right") || (yellowDirs.Contains("top") && yellowDirs.Contains("right")))
            {
                if (wallNeighbors["right"] && wallNeighbors["top"])
                    return '1'; // Bottom-left corner
            }

            if (yellowDirs.Contains("top_left") || (yellowDirs.Contains("top") && yellowDirs.Contains("left")))
            {
                if (wallNeighbors["left"] && wallNeighbors["top"])
                    return '3'; // Bottom-right corner
            }

            // Additional corner detection - check for multiple yellow directions
            if (yellowDirs.Contains("right") && yellowDirs.Contains("bottom_right") && yellowDirs.Contains("bottom"))
            {
                if (wallNeighbors["top"] && wallNeighbors["left"])
                    return '3'; // Bottom-right corner
            }

            if (yellowDirs.Contains("left") && yellowDirs.Contains("bottom_left") && yellowDirs.Contains("bottom"))
            {
                if (wallNeighbors["top"] && wallNeighbors["right"])
                    return '1'; // Bottom-left corner
            }

            if (yellowDirs.Contains("right") && yellowDirs.Contains("top_right") && yellowDirs.Contains("top"))
            {
                if (wallNeighbors["bottom"] && wallNeighbors["left"])
                    return '9'; // Top-right corner
            }

            if (yellowDirs.Contains("left") && yellowDirs.Contains("top_left") && yellowDirs.Contains("top"))
            {
                if (wallNeighbors["bottom"] && wallNeighbors["right"])
                    return '7'; // Top-left corner
            }

            // Edge cases - check for accessible areas in cardinal directions
            var bottomDirs = new[] { "bottom", "bottom_left", "bottom_right" };
            if (bottomDirs.Any(dir => yellowDirs.Contains(dir)))
            {
                if (wallNeighbors["left"] && wallNeighbors["right"] && !wallNeighbors["bottom"])
                    return '8'; // Top edge
            }

            var topDirs = new[] { "top", "top_left", "top_right" };
            if (topDirs.Any(dir => yellowDirs.Contains(dir)))
            {
                if (wallNeighbors["left"] && wallNeighbors["right"] && !wallNeighbors["top"])
                    return '2'; // Bottom edge
            }

            var leftDirs = new[] { "left", "top_left", "bottom_left" };
            if (leftDirs.Any(dir => yellowDirs.Contains(dir)))
            {
                if (wallNeighbors["top"] && wallNeighbors["bottom"] && !wallNeighbors["left"])
                    return '6'; // Right edge
            }

            var rightDirs = new[] { "right", "top_right", "bottom_right" };
            if (rightDirs.Any(dir => yellowDirs.Contains(dir)))
            {
                if (wallNeighbors["top"] && wallNeighbors["bottom"] && !wallNeighbors["right"])
                    return '4'; // Left edge
            }

            // Default to center if no clear pattern
            return '5'; // Center or standalone wall
        }

        /// <summary>
        /// Check if position has an accessible area - includes Start, Finish, Asterisk, and commas
        /// </summary>
        public static bool HasAccessibleMark(List<List<char>> grid, int x, int y)
        {
            if (x < 0 || x >= grid[0].Count || y < 0 || y >= grid.Count)
                return false;

            char cell = grid[y][x];
            return cell == ',' || cell == 'S' || cell == 'F' || cell == '*';
        }

        /// <summary>
        /// Check if position contains any wall type
        /// </summary>
        public static bool IsWall(List<List<char>> grid, int x, int y)
        {
            if (x < 0 || x >= grid[0].Count || y < 0 || y >= grid.Count)
                return false;

            char cell = grid[y][x];
            return cell == '#' || (cell >= '1' && cell <= '9');
        }

        /// <summary>
        /// Flood fill accessible areas from Start position with commas
        /// </summary>
        public static void FloodFillFromStart(List<List<char>> grid)
        {
            int width = grid[0].Count;
            int height = grid.Count;

            // Clear existing accessible marks (but preserve S, F, *)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[y][x] == ',')
                        grid[y][x] = '.';
                }
            }

            // Find start positions
            var startPositions = new List<(int x, int y)>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[y][x] == 'S')
                        startPositions.Add((x, y));
                }
            }

            if (!startPositions.Any())
                return;

            // Flood fill from all start positions
            var visited = new HashSet<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();

            foreach (var (startX, startY) in startPositions)
            {
                queue.Enqueue((startX, startY));
                visited.Add((startX, startY));
            }

            var directions = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                // Mark as accessible if it's empty space (don't overwrite S, F, *)
                if (grid[y][x] == '.')
                    grid[y][x] = ',';

                // Check all 4 directions (not diagonal for flood fill)
                foreach (var (dx, dy) in directions)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (!visited.Contains((nx, ny)) && nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (!IsWall(grid, nx, ny)) // Can reach any non-wall
                        {
                            visited.Add((nx, ny));
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update all wall types based on flood-filled accessible areas
        /// </summary>
        public static void UpdateWallTypes(List<List<char>> grid)
        {
            int width = grid[0].Count;
            int height = grid.Count;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsWall(grid, x, y))
                    {
                        char newType = GetWallType(grid, x, y);
                        if (newType != '.')
                            grid[y][x] = newType;
                    }
                }
            }
        }

        /// <summary>
        /// Draw a line of walls between two points
        /// </summary>
        public static void DrawWallLine(List<List<char>> grid, int startX, int startY, int endX, int endY)
        {
            var points = GetLinePoints(startX, startY, endX, endY);

            int width = grid[0].Count;
            int height = grid.Count;

            foreach (var (x, y) in points)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    grid[y][x] = '#'; // Place basic wall first
            }

            // Re-flood fill and update wall types
            FloodFillFromStart(grid);
            UpdateWallTypes(grid);
        }

        /// <summary>
        /// Get all points on a line between two coordinates (Bresenham's line algorithm)
        /// </summary>
        public static List<(int x, int y)> GetLinePoints(int x1, int y1, int x2, int y2)
        {
            var points = new List<(int x, int y)>();

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;

            while (true)
            {
                points.Add((x, y));

                if (x == x2 && y == y2)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return points;
        }
    }
}

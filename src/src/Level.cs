using System;
using System.Drawing;

namespace Clawbyrinth
{
    public enum WallType
    {
        None = 0,
        Upper1,
        Upper2,
        Lower1,
        Lower2,
        Left1,
        Left2,
        Right1,
        Right2,
        CornerUpperLeft,
        CornerUpperRight,
        CornerLowerLeft,
        CornerLowerRight
    }

    public class Level
    {
        private const int GRID_SIZE = 24;
        private const int WALL_TILE_SIZE = 12; // Each wall tile is 12x12 pixels (2x scaled from 6x6)
        private const int WALL_TILES_PER_GRID = GRID_SIZE / WALL_TILE_SIZE; // 2 wall tiles per grid cell
        protected const int WALL = 1;
        protected const int EMPTY = 0;
        
        protected int[,] levelData = null!;
        protected WallType[,] wallTypes = null!; // Store wall types for rendering
        protected int gridWidth;
        protected int gridHeight;
        private int windowWidth;
        private int windowHeight;
        private Image? wallTilemap;

        public Level(int windowWidth, int windowHeight)
        {
            this.windowWidth = windowWidth;
            this.windowHeight = windowHeight;
            this.gridWidth = windowWidth / GRID_SIZE;
            this.gridHeight = windowHeight / GRID_SIZE;
            
            LoadWallTilemap();
            GenerateLevel();
        }

        private void LoadWallTilemap()
        {
            try
            {
                wallTilemap = Image.FromFile("Assets/Walls/new_wall_tilemap.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load wall tilemap: {ex.Message}");
                wallTilemap = null;
            }
        }

        protected virtual void GenerateLevel()
        {
            levelData = new int[gridWidth, gridHeight];
            wallTypes = new WallType[gridWidth, gridHeight];
            
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
            
            // Determine wall types after all walls are placed
            DetermineAllWallTypes();
        }

        protected void DetermineAllWallTypes()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (levelData[x, y] == WALL)
                    {
                        wallTypes[x, y] = DetermineWallType(x, y);
                    }
                    else
                    {
                        wallTypes[x, y] = WallType.None;
                    }
                }
            }
        }

        private WallType DetermineWallType(int x, int y)
        {
            // Check neighboring walls to determine the appropriate wall type
            bool hasWallAbove = y > 0 && levelData[x, y - 1] == WALL;
            bool hasWallBelow = y < gridHeight - 1 && levelData[x, y + 1] == WALL;
            bool hasWallLeft = x > 0 && levelData[x - 1, y] == WALL;
            bool hasWallRight = x < gridWidth - 1 && levelData[x + 1, y] == WALL;
            
            // Check if we're at the edges of the level
            bool isTopEdge = y == 0;
            bool isBottomEdge = y == gridHeight - 1;
            bool isLeftEdge = x == 0;
            bool isRightEdge = x == gridWidth - 1;
            
            // Use consistent randomization based on position
            Random random = new Random(x * 1000 + y);
            
            // Determine corner cases first
            if (isTopEdge && isLeftEdge)
                return WallType.CornerUpperLeft;
            if (isTopEdge && isRightEdge)
                return WallType.CornerUpperRight;
            if (isBottomEdge && isLeftEdge)
                return WallType.CornerLowerLeft;
            if (isBottomEdge && isRightEdge)
                return WallType.CornerLowerRight;
            
            // Check for interior corners
            if (!hasWallAbove && !hasWallLeft && (hasWallBelow && hasWallRight))
                return WallType.CornerUpperLeft;
            if (!hasWallAbove && !hasWallRight && (hasWallBelow && hasWallLeft))
                return WallType.CornerUpperRight;
            if (!hasWallBelow && !hasWallLeft && (hasWallAbove && hasWallRight))
                return WallType.CornerLowerLeft;
            if (!hasWallBelow && !hasWallRight && (hasWallAbove && hasWallLeft))
                return WallType.CornerLowerRight;
            
            // Edge walls
            if (isTopEdge)
                return random.Next(2) == 0 ? WallType.Upper1 : WallType.Upper2;
            if (isBottomEdge)
                return random.Next(2) == 0 ? WallType.Lower1 : WallType.Lower2;
            if (isLeftEdge)
                return random.Next(2) == 0 ? WallType.Left1 : WallType.Left2;
            if (isRightEdge)
                return random.Next(2) == 0 ? WallType.Right1 : WallType.Right2;
            
            // Interior walls - determine based on open sides
            if (!hasWallAbove) // Open to the top
                return random.Next(2) == 0 ? WallType.Upper1 : WallType.Upper2;
            if (!hasWallBelow) // Open to the bottom
                return random.Next(2) == 0 ? WallType.Lower1 : WallType.Lower2;
            if (!hasWallLeft) // Open to the left
                return random.Next(2) == 0 ? WallType.Left1 : WallType.Left2;
            if (!hasWallRight) // Open to the right
                return random.Next(2) == 0 ? WallType.Right1 : WallType.Right2;
            
            // Default to upper wall type if completely surrounded
            return random.Next(2) == 0 ? WallType.Upper1 : WallType.Upper2;
        }

        public virtual Point GetStartPosition()
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
            if (wallTilemap == null)
            {
                // Fallback to solid color rendering if tilemap isn't loaded
                RenderFallback(g);
                return;
            }

            // Render walls using the new tilemap
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (levelData[x, y] == WALL)
                    {
                        WallType wallType = wallTypes[x, y];
                        RenderWallTiles(g, wallType, x, y);
                    }
                }
            }
        }

        private WallType GetTileVariation(WallType baseWallType, int tileX, int tileY)
        {
            // Use tile position to determine which variation to use
            // Create some pattern variation using tile coordinates
            bool useType2 = (tileX + tileY) % 2 == 1;
            
            switch (baseWallType)
            {
                case WallType.Upper1:
                case WallType.Upper2:
                    return useType2 ? WallType.Upper2 : WallType.Upper1;
                    
                case WallType.Lower1:
                case WallType.Lower2:
                    return useType2 ? WallType.Lower2 : WallType.Lower1;
                    
                case WallType.Left1:
                case WallType.Left2:
                    return useType2 ? WallType.Left2 : WallType.Left1;
                    
                case WallType.Right1:
                case WallType.Right2:
                    return useType2 ? WallType.Right2 : WallType.Right1;
                    
                // Corners don't have variations, return as-is
                default:
                    return baseWallType;
            }
        }

        private void RenderWallTiles(Graphics g, WallType wallType, int gridX, int gridY)
        {
            int baseX = gridX * GRID_SIZE;
            int baseY = gridY * GRID_SIZE;
            
            // Check if this is an edge wall
            bool isTopEdge = gridY == 0;
            bool isBottomEdge = gridY == gridHeight - 1;
            bool isLeftEdge = gridX == 0;
            bool isRightEdge = gridX == gridWidth - 1;
            
            switch (wallType)
            {
                case WallType.Upper1:
                case WallType.Upper2:
                    if (isTopEdge)
                    {
                        // Top edge: only 2x1 tiles (2 tiles horizontally, 1 row)
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX, baseY + WALL_TILE_SIZE); // Bottom-left
                        DrawWallTile(g, GetTileVariation(wallType, 1, 0), baseX + WALL_TILE_SIZE, baseY + WALL_TILE_SIZE); // Bottom-right
                    }
                    else
                    {
                        // Interior upper wall: 2 tiles on bottom + stretched tile on top
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX, baseY + WALL_TILE_SIZE); // Bottom-left
                        DrawWallTile(g, GetTileVariation(wallType, 1, 0), baseX + WALL_TILE_SIZE, baseY + WALL_TILE_SIZE); // Bottom-right
                        DrawWallTileStretched(g, wallType, baseX, baseY, GRID_SIZE, WALL_TILE_SIZE); // Top (back side, stretched)
                    }
                    break;
                    
                case WallType.Lower1:
                case WallType.Lower2:
                    if (isBottomEdge)
                    {
                        // Bottom edge: only 2x1 tiles (2 tiles horizontally, 1 row)
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX, baseY); // Top-left
                        DrawWallTile(g, GetTileVariation(wallType, 1, 0), baseX + WALL_TILE_SIZE, baseY); // Top-right
                    }
                    else
                    {
                        // Interior lower wall: 2 tiles on top + stretched tile on bottom
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX, baseY); // Top-left
                        DrawWallTile(g, GetTileVariation(wallType, 1, 0), baseX + WALL_TILE_SIZE, baseY); // Top-right
                        DrawWallTileStretched(g, wallType, baseX, baseY + WALL_TILE_SIZE, GRID_SIZE, WALL_TILE_SIZE); // Bottom (back side, stretched)
                    }
                    break;
                    
                case WallType.Left1:
                case WallType.Left2:
                    if (isLeftEdge)
                    {
                        // Left edge: only 1x2 tiles (1 column, 2 tiles vertically)
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX + WALL_TILE_SIZE, baseY); // Right-top
                        DrawWallTile(g, GetTileVariation(wallType, 0, 1), baseX + WALL_TILE_SIZE, baseY + WALL_TILE_SIZE); // Right-bottom
                    }
                    else
                    {
                        // Interior left wall: 2 tiles on right + stretched tile on left
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX + WALL_TILE_SIZE, baseY); // Right-top
                        DrawWallTile(g, GetTileVariation(wallType, 0, 1), baseX + WALL_TILE_SIZE, baseY + WALL_TILE_SIZE); // Right-bottom
                        DrawWallTileStretched(g, wallType, baseX, baseY, WALL_TILE_SIZE, GRID_SIZE); // Left (back side, stretched)
                    }
                    break;
                    
                case WallType.Right1:
                case WallType.Right2:
                    if (isRightEdge)
                    {
                        // Right edge: only 1x2 tiles (1 column, 2 tiles vertically)
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX, baseY); // Left-top
                        DrawWallTile(g, GetTileVariation(wallType, 0, 1), baseX, baseY + WALL_TILE_SIZE); // Left-bottom
                    }
                    else
                    {
                        // Interior right wall: 2 tiles on left + stretched tile on right
                        DrawWallTile(g, GetTileVariation(wallType, 0, 0), baseX, baseY); // Left-top
                        DrawWallTile(g, GetTileVariation(wallType, 0, 1), baseX, baseY + WALL_TILE_SIZE); // Left-bottom
                        DrawWallTileStretched(g, wallType, baseX + WALL_TILE_SIZE, baseY, WALL_TILE_SIZE, GRID_SIZE); // Right (back side, stretched)
                    }
                    break;
                    
                case WallType.CornerUpperLeft:
                    // Corner upper left: only 1 tile at bottom-right position
                    DrawWallTile(g, wallType, baseX + WALL_TILE_SIZE, baseY + WALL_TILE_SIZE);
                    break;
                    
                case WallType.CornerUpperRight:
                    // Corner upper right: only 1 tile at bottom-left position
                    DrawWallTile(g, wallType, baseX, baseY + WALL_TILE_SIZE);
                    break;
                    
                case WallType.CornerLowerLeft:
                    // Corner lower left: only 1 tile at top-right position
                    DrawWallTile(g, wallType, baseX + WALL_TILE_SIZE, baseY);
                    break;
                    
                case WallType.CornerLowerRight:
                    // Corner lower right: only 1 tile at top-left position
                    DrawWallTile(g, wallType, baseX, baseY);
                    break;
                    
                default:
                    // Default: fill with 2x2 pattern using type1/type2 variations
                    for (int tileX = 0; tileX < WALL_TILES_PER_GRID; tileX++)
                    {
                        for (int tileY = 0; tileY < WALL_TILES_PER_GRID; tileY++)
                        {
                            int screenX = baseX + tileX * WALL_TILE_SIZE;
                            int screenY = baseY + tileY * WALL_TILE_SIZE;
                            WallType tileType = GetTileVariation(WallType.Upper1, tileX, tileY); // Default to upper type
                            DrawWallTile(g, tileType, screenX, screenY);
                        }
                    }
                    break;
            }
        }

        private void DrawWallTile(Graphics g, WallType wallType, int screenX, int screenY)
        {
            if (wallTilemap == null) return;

            Rectangle sourceRect = GetWallTileSourceRect(wallType);
            Rectangle destRect = new Rectangle(screenX, screenY, WALL_TILE_SIZE, WALL_TILE_SIZE);
            
            // Scale the 6x6 source tile to 12x12 destination
            g.DrawImage(wallTilemap, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        private void DrawWallTileStretched(Graphics g, WallType wallType, int screenX, int screenY, int width, int height)
        {
            if (wallTilemap == null) return;

            Rectangle sourceRect = GetWallTileSourceRect(wallType);
            Rectangle destRect = new Rectangle(screenX, screenY, width, height);
            
            // Scale the 6x6 source tile to the specified destination size
            g.DrawImage(wallTilemap, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        private Rectangle GetWallTileSourceRect(WallType wallType)
        {
            // Based on the tilemap specification:
            // Each tile is 6x6 with 1 pixel spacing
            // So tile positions are: 0-5, 7-12, 14-19, 21-26, etc.
            
            const int TILEMAP_TILE_SIZE = 6; // Original tile size in the tilemap
            int tileX = 0, tileY = 0;
            
            switch (wallType)
            {
                // Upper walls (1st row: 0-5 y-axis)
                case WallType.Upper1:
                    tileX = 0; tileY = 0; // 0-5 x-axis, 0-5 y-axis
                    break;
                case WallType.Upper2:
                    tileX = 7; tileY = 0; // 7-12 x-axis, 0-5 y-axis
                    break;
                
                // Lower walls (2nd row: 7-12 y-axis)
                case WallType.Lower1:
                    tileX = 0; tileY = 7; // 0-5 x-axis, 7-12 y-axis
                    break;
                case WallType.Lower2:
                    tileX = 7; tileY = 7; // 7-12 x-axis, 7-12 y-axis
                    break;
                
                // Left walls (3rd row: 14-19 y-axis)
                case WallType.Left1:
                    tileX = 0; tileY = 14; // 0-5 x-axis, 14-19 y-axis
                    break;
                case WallType.Left2:
                    tileX = 7; tileY = 14; // 7-12 x-axis, 14-19 y-axis
                    break;
                
                // Right walls (4th row: 21-26 y-axis)
                case WallType.Right1:
                    tileX = 0; tileY = 21; // 0-5 x-axis, 21-26 y-axis
                    break;
                case WallType.Right2:
                    tileX = 7; tileY = 21; // 7-12 x-axis, 21-26 y-axis
                    break;
                
                // Corner walls (5th row: 28-33 y-axis)
                case WallType.CornerUpperLeft:
                    tileX = 0; tileY = 28; // 0-5 x-axis, 28-33 y-axis
                    break;
                case WallType.CornerUpperRight:
                    tileX = 7; tileY = 28; // 7-12 x-axis, 28-33 y-axis
                    break;
                case WallType.CornerLowerLeft:
                    tileX = 14; tileY = 28; // 14-19 x-axis, 28-33 y-axis
                    break;
                case WallType.CornerLowerRight:
                    tileX = 21; tileY = 28; // 21-26 x-axis, 28-33 y-axis
                    break;
                
                default:
                    tileX = 0; tileY = 0; // Default to upper1
                    break;
            }
            
            return new Rectangle(tileX, tileY, TILEMAP_TILE_SIZE, TILEMAP_TILE_SIZE);
        }

        private void RenderFallback(Graphics g)
        {
            // Fallback to the original solid color rendering
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
        }

        // Dispose method to clean up resources
        public void Dispose()
        {
            wallTilemap?.Dispose();
        }
    }
}
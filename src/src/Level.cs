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
            
            // Check if we have empty spaces (player-accessible areas) adjacent
            bool hasEmptyAbove = y > 0 ? levelData[x, y - 1] == EMPTY : true; // Treat edges as empty
            bool hasEmptyBelow = y < gridHeight - 1 ? levelData[x, y + 1] == EMPTY : true;
            bool hasEmptyLeft = x > 0 ? levelData[x - 1, y] == EMPTY : true;
            bool hasEmptyRight = x < gridWidth - 1 ? levelData[x + 1, y] == EMPTY : true;
            
            // Use consistent randomization based on position
            Random random = new Random(x * 1000 + y);
            
            // Corner detection: Check for exactly two adjacent empty spaces at right angles
            if (hasEmptyAbove && hasEmptyLeft && !hasEmptyBelow && !hasEmptyRight)
                return WallType.CornerUpperLeft;
            if (hasEmptyAbove && hasEmptyRight && !hasEmptyBelow && !hasEmptyLeft)
                return WallType.CornerUpperRight;
            if (hasEmptyBelow && hasEmptyLeft && !hasEmptyAbove && !hasEmptyRight)
                return WallType.CornerLowerLeft;
            if (hasEmptyBelow && hasEmptyRight && !hasEmptyAbove && !hasEmptyLeft)
                return WallType.CornerLowerRight;
            
            // Straight walls: Choose based on which direction has empty space
            // Prioritize the direction with empty space (where the wall should face)
            if (hasEmptyAbove && !hasEmptyBelow)
                return random.Next(2) == 0 ? WallType.Upper1 : WallType.Upper2;
            if (hasEmptyBelow && !hasEmptyAbove)
                return random.Next(2) == 0 ? WallType.Lower1 : WallType.Lower2;
            if (hasEmptyLeft && !hasEmptyRight)
                return random.Next(2) == 0 ? WallType.Left1 : WallType.Left2;
            if (hasEmptyRight && !hasEmptyLeft)
                return random.Next(2) == 0 ? WallType.Right1 : WallType.Right2;
            
            // For walls with multiple empty sides, choose based on priority
            if (hasEmptyAbove)
                return random.Next(2) == 0 ? WallType.Upper1 : WallType.Upper2;
            if (hasEmptyBelow)
                return random.Next(2) == 0 ? WallType.Lower1 : WallType.Lower2;
            if (hasEmptyLeft)
                return random.Next(2) == 0 ? WallType.Left1 : WallType.Left2;
            if (hasEmptyRight)
                return random.Next(2) == 0 ? WallType.Right1 : WallType.Right2;
            
            // Default fallback (should rarely be reached)
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
            // Each wall grid cell should render exactly ONE tile (12x12) on the side facing empty space
            int baseX = gridX * GRID_SIZE;
            int baseY = gridY * GRID_SIZE;

            // Determine if adjacent cells are empty (player-facing sides)
            bool openUp = (gridY > 0 && levelData[gridX, gridY - 1] == EMPTY) || gridY == 0;
            bool openDown = (gridY < gridHeight - 1 && levelData[gridX, gridY + 1] == EMPTY) || gridY == gridHeight - 1;
            bool openLeft = (gridX > 0 && levelData[gridX - 1, gridY] == EMPTY) || gridX == 0;
            bool openRight = (gridX < gridWidth - 1 && levelData[gridX + 1, gridY] == EMPTY) || gridX == gridWidth - 1;

            // For corners: check if this is an interior corner (wall with exactly 2 adjacent empty spaces at right angles)
            if (IsCornerType(wallType))
            {
                // Draw the corner tile exactly once at the correct position
                DrawWallTile(g, wallType, baseX, baseY);
            }
            else
            {
                // For straight walls: draw one tile on the side facing the empty space
                // The wallType already indicates which direction this wall should face
                switch (wallType)
                {
                    case WallType.Upper1:
                    case WallType.Upper2:
                        // Wall faces upward (empty space is above)
                        if (openUp)
                            DrawWallTile(g, wallType, baseX, baseY);
                        break;
                        
                    case WallType.Lower1:
                    case WallType.Lower2:
                        // Wall faces downward (empty space is below)
                        if (openDown)
                            DrawWallTile(g, wallType, baseX, baseY);
                        break;
                        
                    case WallType.Left1:
                    case WallType.Left2:
                        // Wall faces left (empty space is to the left)
                        if (openLeft)
                            DrawWallTile(g, wallType, baseX, baseY);
                        break;
                        
                    case WallType.Right1:
                    case WallType.Right2:
                        // Wall faces right (empty space is to the right)
                        if (openRight)
                            DrawWallTile(g, wallType, baseX, baseY);
                        break;
                }
            }
        }

        private bool IsCornerType(WallType wallType)
        {
            return wallType == WallType.CornerUpperLeft ||
                   wallType == WallType.CornerUpperRight ||
                   wallType == WallType.CornerLowerLeft ||
                   wallType == WallType.CornerLowerRight;
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
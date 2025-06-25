using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Clawbyrinth.Levels;

namespace Clawbyrinth
{
    /// <summary>
    /// Represents a portal in the game world.
    /// </summary>
    public class Portal
    {
        public Point Position { get; set; }  // Top-left corner of the 2x2 portal area
        public Portal? PairedPortal { get; set; }  // The portal this one is paired with
        public DateTime LastAnimationTime { get; set; }
        
        public Portal(Point position)
        {
            Position = position;
            LastAnimationTime = DateTime.Now;
        }
        
        /// <summary>
        /// Checks if the given position is within this portal's 2x2 area.
        /// </summary>
        public bool ContainsPosition(int gridX, int gridY)
        {
            return gridX >= Position.X && gridX <= Position.X + 1 &&
                   gridY >= Position.Y && gridY <= Position.Y + 1;
        }
    }

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
        // Use constants from Definition
        private const int GRID_SIZE = Definition.GRID_SIZE;
        private const int WALL_TILE_SIZE = Definition.WALL_TILE_SIZE;
        private const int TILEMAP_TILE_SIZE = Definition.TILEMAP_TILE_SIZE;
        private const int WALL_COLLISION_SIZE = Definition.WALL_COLLISION_SIZE;
        protected const int WALL = Definition.WALL;
        protected const int EMPTY = Definition.EMPTY;
        protected const int TRAP = Definition.TRAP;
        
        protected int[,] levelData = null!;
        protected WallType[,] wallTypes = null!; // Store wall types for rendering
        protected char[,] originalCharacters = null!; // Store original characters for oriented walls
        protected bool[,] dotsCollected = null!; // Track which dots have been collected
        protected bool[,] coinPositions = null!; // Track where coins are placed
        protected bool[,] coinsCollected = null!; // Track which coins have been collected
        protected char[,] trapTypes = null!; // Store trap types for rendering
        protected List<Portal> portals = new List<Portal>(); // List of all portals in the level
        protected List<Spike2Trap> spike2Traps = new List<Spike2Trap>(); // List of all Spike 2 traps
        protected int gridWidth;
        protected int gridHeight;
        private int windowWidth;
        private int windowHeight;
        private Image? wallTilemap;
        private Image? dotNormalTexture;
        private Image? dotWhiteTexture;
        private Image? coinNormalTexture;
        private Image? coinWhiteTexture;
        private Image? portalTexture;
        private Image? spikeTexture;
        private Image? cannonTexture;
        private Image? spike2DownTexture;
        private Image? spike2RightTexture;
        private Image? spike2LeftTexture;
        private Image? spike2UpTexture;
        private Image? spike2AttackTexture;
        private Image? spike2CornerUpLeftTexture;
        private Image? spike2CornerUpRightTexture;
        private Image? spike2CornerDownLeftTexture;
        private Image? spike2CornerDownRightTexture;
        private DateTime lastDotAnimationTime;
        private DateTime lastPortalTeleportTime = DateTime.MinValue;
        private const double PORTAL_COOLDOWN_SECONDS = 0.5; // Half second cooldown

        public Level(int windowWidth, int windowHeight)
        {
            this.windowWidth = windowWidth;
            this.windowHeight = windowHeight;
            this.gridWidth = windowWidth / GRID_SIZE;
            this.gridHeight = windowHeight / GRID_SIZE;
            
            lastDotAnimationTime = DateTime.Now;
            LoadWallTilemap();
            LoadDotTextures();
            LoadCoinTextures();
            LoadPortalTexture();
            LoadTrapTextures();
            GenerateLevel();
        }

        /// <summary>
        /// Constructor for levels that use blueprint definitions.
        /// </summary>
        /// <param name="windowWidth">Window width in pixels</param>
        /// <param name="windowHeight">Window height in pixels</param>
        /// <param name="levelDefinition">Level definition with blueprint</param>
        public Level(int windowWidth, int windowHeight, ILevelDefinition levelDefinition)
        {
            this.windowWidth = windowWidth;
            this.windowHeight = windowHeight;
            
            lastDotAnimationTime = DateTime.Now;
            LoadWallTilemap();
            LoadDotTextures();
            LoadCoinTextures();
            LoadPortalTexture();
            LoadTrapTextures();
            GenerateLevelFromBlueprint(levelDefinition);
        }

        private void LoadWallTilemap()
        {
            try
            {
                wallTilemap = Image.FromFile(Definition.WALL_TILEMAP_PATH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load wall tilemap: {ex.Message}");
                wallTilemap = null;
            }
        }

        private void LoadDotTextures()
        {
            try
            {
                dotNormalTexture = Image.FromFile(Definition.DOT_NORMAL_PATH);
                dotWhiteTexture = Image.FromFile(Definition.DOT_WHITE_PATH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load dot textures: {ex.Message}");
                dotNormalTexture = null;
                dotWhiteTexture = null;
            }
        }

        private void LoadCoinTextures()
        {
            try
            {
                coinNormalTexture = Image.FromFile(Definition.COIN_NORMAL_PATH);
                coinWhiteTexture = Image.FromFile(Definition.COIN_WHITE_PATH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load coin textures: {ex.Message}");
                coinNormalTexture = null;
                coinWhiteTexture = null;
            }
        }

        private void LoadPortalTexture()
        {
            try
            {
                portalTexture = Image.FromFile(Definition.PORTAL_SPRITE_PATH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load portal texture: {ex.Message}");
                portalTexture = null;
            }
        }

        private void LoadTrapTextures()
        {
            try
            {
                // Load basic spike texture (spike 1)
                spikeTexture = Image.FromFile("Assets/Traps/Spikes/Spikes_d.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load spike texture: {ex.Message}");
                spikeTexture = null;
            }
            
            try
            {
                // Load Spike 2 oriented textures (M_Spikes_*)
                spike2DownTexture = Image.FromFile("Assets/Traps/Spikes/M_Spikes_d.png");
                spike2RightTexture = Image.FromFile("Assets/Traps/Spikes/M_Spikes_r.png");
                spike2LeftTexture = Image.FromFile("Assets/Traps/Spikes/M_Spikes_l.png");
                spike2UpTexture = Image.FromFile("Assets/Traps/Spikes/M_Spikes_u.png");
                spike2AttackTexture = Image.FromFile("Assets/Traps/Spikes/M_Spikes_attack_sheet.png");
                
                // Load Spike 2 corner textures (Corner_M_spikes_*)
                spike2CornerUpLeftTexture = Image.FromFile("Assets/Traps/Spikes/Corner_M_spikes_u_l.png");
                spike2CornerUpRightTexture = Image.FromFile("Assets/Traps/Spikes/Corner_M_spikes_u_r.png");
                spike2CornerDownLeftTexture = Image.FromFile("Assets/Traps/Spikes/Corner_M_spikes_d_l.png");
                spike2CornerDownRightTexture = Image.FromFile("Assets/Traps/Spikes/Corner_M_spikes_d_r.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Spike 2 textures: {ex.Message}");
                spike2DownTexture = spike2RightTexture = spike2LeftTexture = spike2UpTexture = spike2AttackTexture = null;
                spike2CornerUpLeftTexture = spike2CornerUpRightTexture = spike2CornerDownLeftTexture = spike2CornerDownRightTexture = null;
            }
            
            try
            {
                // Load cannon texture
                cannonTexture = Image.FromFile("Assets/Traps/Canon/Cannon_sheet.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load cannon texture: {ex.Message}");
                cannonTexture = null;
            }
        }

        protected virtual void GenerateLevel()
        {
            levelData = new int[gridWidth, gridHeight];
            wallTypes = new WallType[gridWidth, gridHeight];
            originalCharacters = new char[gridWidth, gridHeight];
            dotsCollected = new bool[gridWidth, gridHeight];
            coinPositions = new bool[gridWidth, gridHeight];
            coinsCollected = new bool[gridWidth, gridHeight];
            trapTypes = new char[gridWidth, gridHeight];
            
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

        /// <summary>
        /// <summary>
        /// Generates level from a blueprint definition.
        /// Since the template now has SS and FF as 2x2 blocks, use 1:1 mapping.
        /// </summary>
        /// <param name="levelDefinition">Level definition containing the blueprint</param>
        protected virtual void GenerateLevelFromBlueprint(ILevelDefinition levelDefinition)
        {
            string[] blueprint = levelDefinition.Blueprint;
            
            // Use 1:1 mapping since the template is already properly sized
            this.gridHeight = blueprint.Length;
            this.gridWidth = blueprint.Length > 0 ? blueprint[0].Length : 0;
            
            // Ensure grid dimensions don't exceed window size
            this.gridWidth = Math.Min(this.gridWidth, windowWidth / GRID_SIZE);
            this.gridHeight = Math.Min(this.gridHeight, windowHeight / GRID_SIZE);
            
            // Initialize arrays
            levelData = new int[gridWidth, gridHeight];
            wallTypes = new WallType[gridWidth, gridHeight];
            originalCharacters = new char[gridWidth, gridHeight];
            dotsCollected = new bool[gridWidth, gridHeight];
            coinPositions = new bool[gridWidth, gridHeight];
            coinsCollected = new bool[gridWidth, gridHeight];
            trapTypes = new char[gridWidth, gridHeight];
            
            // Parse blueprint into level data (1:1 mapping)
            for (int y = 0; y < gridHeight && y < blueprint.Length; y++)
            {
                string row = blueprint[y];
                for (int x = 0; x < gridWidth && x < row.Length; x++)
                {
                    char c = row[x];
                    levelData[x, y] = Definition.CharacterToLevelData(c);
                    originalCharacters[x, y] = c; // Store original character for oriented walls
                }
            }
            
            // Determine wall types after all walls are placed
            DetermineAllWallTypes();
            
            // Load trap layer if available
            LoadTrapLayer(levelDefinition);
            
            // Generate portals from PPPP patterns
            GeneratePortals();
            
            // Spawn coins randomly on dot positions
            SpawnCoins();
        }

        protected void DetermineAllWallTypes()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (levelData[x, y] == WALL)
                    {
                        char originalChar = originalCharacters[x, y];
                        
                        // If it's an oriented wall character (1-9), use direct mapping with variants
                        if (Definition.IsOrientedWallCharacter(originalChar))
                        {
                            wallTypes[x, y] = Definition.CharacterToWallType(originalChar, x, y);
                        }
                        else
                        {
                            // Fallback to old neighbor-based determination for '#' walls
                            wallTypes[x, y] = DetermineWallType(x, y);
                        }
                    }
                    else
                    {
                        wallTypes[x, y] = WallType.None;
                    }
                }
            }
        }

        /// <summary>
        /// <summary>
        /// Generates portals from PPPP patterns in the level blueprint.
        /// Each 2x2 PPPP block becomes a single portal.
        /// Portals are automatically paired in the order they appear.
        /// </summary>
        private void GeneratePortals()
        {
            portals.Clear();
            bool[,] processed = new bool[gridWidth, gridHeight];
            
            // Find all 2x2 blocks of 'P' characters
            for (int x = 0; x < gridWidth - 1; x++)
            {
                for (int y = 0; y < gridHeight - 1; y++)
                {
                    if (processed[x, y]) continue;
                    
                    // Check if we have a 2x2 block of portals
                    if (originalCharacters[x, y] == Definition.PORTAL_CHAR &&
                        originalCharacters[x + 1, y] == Definition.PORTAL_CHAR &&
                        originalCharacters[x, y + 1] == Definition.PORTAL_CHAR &&
                        originalCharacters[x + 1, y + 1] == Definition.PORTAL_CHAR)
                    {
                        // Create a new portal at this position
                        Portal portal = new Portal(new Point(x, y));
                        portals.Add(portal);
                        
                        // Mark this 2x2 area as processed
                        processed[x, y] = true;
                        processed[x + 1, y] = true;
                        processed[x, y + 1] = true;
                        processed[x + 1, y + 1] = true;
                    }
                }
            }
            
            // Pair portals - connect them in pairs (0-1, 2-3, 4-5, etc.)
            for (int i = 0; i < portals.Count - 1; i += 2)
            {
                portals[i].PairedPortal = portals[i + 1];
                portals[i + 1].PairedPortal = portals[i];
            }
            
            System.Diagnostics.Debug.WriteLine($"Generated {portals.Count} portals, {portals.Count / 2} pairs");
        }

        /// <summary>
        /// Spawns coins randomly on dot positions.
        /// Spawns 2 coins per 20x20 map size, scaled proportionally.
        /// </summary>
        private void SpawnCoins()
        {
            // Calculate number of coins to spawn based on map size
            float mapArea = gridWidth * gridHeight;
            float baseArea = Definition.BASE_MAP_SIZE * Definition.BASE_MAP_SIZE;
            int coinsToSpawn = Math.Max(1, (int)((mapArea / baseArea) * Definition.COINS_PER_BASE_MAP));
            
            // Find all 2x2 dot blocks (potential coin spawn locations)
            List<Point> dotBlocks = new List<Point>();
            bool[,] processed = new bool[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth - 1; x++)
            {
                for (int y = 0; y < gridHeight - 1; y++)
                {
                    if (!processed[x, y] &&
                        levelData[x, y] == Definition.DOT &&
                        levelData[x + 1, y] == Definition.DOT &&
                        levelData[x, y + 1] == Definition.DOT &&
                        levelData[x + 1, y + 1] == Definition.DOT)
                    {
                        // Found a 2x2 dot block - add top-left corner as spawn location
                        dotBlocks.Add(new Point(x, y));
                        
                        // Mark as processed
                        processed[x, y] = true;
                        processed[x + 1, y] = true;
                        processed[x, y + 1] = true;
                        processed[x + 1, y + 1] = true;
                    }
                }
            }
            
            // Randomly select positions for coins
            Random random = new Random();
            int coinsSpawned = 0;
            
            while (coinsSpawned < coinsToSpawn && dotBlocks.Count > 0)
            {
                int randomIndex = random.Next(dotBlocks.Count);
                Point spawnLocation = dotBlocks[randomIndex];
                
                // Place coin at this 2x2 block
                coinPositions[spawnLocation.X, spawnLocation.Y] = true;
                coinsSpawned++;
                
                // Remove this location from available spots
                dotBlocks.RemoveAt(randomIndex);
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

        /// <summary>
        /// Check if a rectangular area collides with any wall tiles using precise collision.
        /// This directly checks the level data for wall presence.
        /// </summary>
        /// <param name="x">Left edge of the collision box in pixels</param>
        /// <param name="y">Top edge of the collision box in pixels</param>
        /// <param name="width">Width of the collision box in pixels</param>
        /// <param name="height">Height of the collision box in pixels</param>
        /// <returns>True if collision detected, false otherwise</returns>
        public bool CheckWallCollision(float x, float y, int width, int height)
        {
            // Create collision rectangle
            Rectangle collisionRect = new Rectangle((int)x, (int)y, width, height);
            
            // Calculate which grid cells the collision box overlaps
            int startGridX = Math.Max(0, Definition.PixelToGrid((int)x));
            int endGridX = Math.Min(gridWidth - 1, Definition.PixelToGrid((int)(x + width - 1)));
            int startGridY = Math.Max(0, Definition.PixelToGrid((int)y));
            int endGridY = Math.Min(gridHeight - 1, Definition.PixelToGrid((int)(y + height - 1)));
            
            // Check each grid cell for walls and traps
            for (int gridX = startGridX; gridX <= endGridX; gridX++)
            {
                for (int gridY = startGridY; gridY <= endGridY; gridY++)
                {
                    // Check for walls or traps (both are solid obstacles)
                    if (levelData[gridX, gridY] == WALL || levelData[gridX, gridY] == TRAP)
                    {
                        // Create precise collision rectangle for this wall/trap cell
                        Rectangle obstacleRect = new Rectangle(
                            Definition.GridToPixel(gridX),
                            Definition.GridToPixel(gridY),
                            Definition.GRID_SIZE,
                            Definition.GRID_SIZE
                        );
                        
                        if (collisionRect.IntersectsWith(obstacleRect))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Get the exact collision rectangle for a wall tile based on its type and grid position.
        /// Returns collision boxes that match the actual rendered wall tiles.
        /// </summary>
        /// <param name="wallType">Type of wall tile</param>
        /// <param name="gridX">Grid X coordinate</param>
        /// <param name="gridY">Grid Y coordinate</param>
        /// <returns>Rectangle representing the collision area of the wall tile</returns>
        private Rectangle GetWallTileCollisionRect(WallType wallType, int gridX, int gridY)
        {
            int baseX = Definition.GridToPixel(gridX);
            int baseY = Definition.GridToPixel(gridY);
            
            // Determine if adjacent cells are empty (where wall tiles are rendered)
            bool openUp = (gridY > 0 && levelData[gridX, gridY - 1] == EMPTY) || gridY == 0;
            bool openDown = (gridY < gridHeight - 1 && levelData[gridX, gridY + 1] == EMPTY) || gridY == gridHeight - 1;
            bool openLeft = (gridX > 0 && levelData[gridX - 1, gridY] == EMPTY) || gridX == 0;
            bool openRight = (gridX < gridWidth - 1 && levelData[gridX + 1, gridY] == EMPTY) || gridX == gridWidth - 1;

            // For corners: collision box is positioned at the corner where the tile is rendered
            if (IsCornerType(wallType))
            {
                // Corner tiles are positioned at the base of the grid cell
                return new Rectangle(baseX, baseY, Definition.WALL_COLLISION_SIZE, Definition.WALL_COLLISION_SIZE);
            }
            
            // For straight walls: collision box is positioned where the wall tile is actually rendered
            switch (wallType)
            {
                case WallType.Upper1:
                case WallType.Upper2:
                    // Wall faces upward - rendered at top of grid cell
                    if (openUp)
                        return new Rectangle(baseX, baseY, Definition.WALL_COLLISION_SIZE, Definition.WALL_COLLISION_SIZE);
                    break;
                    
                case WallType.Lower1:
                case WallType.Lower2:
                    // Wall faces downward - rendered at bottom of grid cell
                    if (openDown)
                        return new Rectangle(baseX, baseY + GRID_SIZE - Definition.WALL_COLLISION_SIZE, Definition.WALL_COLLISION_SIZE, Definition.WALL_COLLISION_SIZE);
                    break;
                    
                case WallType.Left1:
                case WallType.Left2:
                    // Wall faces left - rendered at left of grid cell
                    if (openLeft)
                        return new Rectangle(baseX, baseY, Definition.WALL_COLLISION_SIZE, Definition.WALL_COLLISION_SIZE);
                    break;
                    
                case WallType.Right1:
                case WallType.Right2:
                    // Wall faces right - rendered at right of grid cell
                    if (openRight)
                        return new Rectangle(baseX + GRID_SIZE - Definition.WALL_COLLISION_SIZE, baseY, Definition.WALL_COLLISION_SIZE, Definition.WALL_COLLISION_SIZE);
                    break;
            }
            
            // Return empty rectangle if no wall tile should be rendered at this position
            return Rectangle.Empty;
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
            
            // Render dots
            RenderDots(g);
            
            // Render coins
            RenderCoins(g);
            
            // Render portals
            RenderPortals(g);
            
            // Render traps
            RenderTraps(g);
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
            // For the new oriented wall system, always render a wall tile at every wall cell
            // using the wall type from the template, regardless of neighbors
            int baseX = Definition.GridToPixel(gridX);
            int baseY = Definition.GridToPixel(gridY);

            // Check if this wall was placed using the oriented system (1-9)
            char originalChar = originalCharacters[gridX, gridY];
            if (Definition.IsOrientedWallCharacter(originalChar))
            {
                // For oriented walls (1-9), always render the tile exactly as specified in the template
                DrawWallTile(g, wallType, baseX, baseY);
            }
            else
            {
                // Legacy rendering logic for '#' walls - use the old "open side" logic
                // Determine if adjacent cells are empty (player-facing sides)
                bool openUp = (gridY > 0 && levelData[gridX, gridY - 1] == EMPTY) || gridY == 0;
                bool openDown = (gridY < gridHeight - 1 && levelData[gridX, gridY + 1] == EMPTY) || gridY == gridHeight - 1;
                bool openLeft = (gridX > 0 && levelData[gridX - 1, gridY] == EMPTY) || gridX == 0;
                bool openRight = (gridX < gridWidth - 1 && levelData[gridX + 1, gridY] == EMPTY) || gridX == gridWidth - 1;

                if (IsCornerType(wallType))
                {
                    // Draw the corner tile exactly once at the correct position
                    DrawWallTile(g, wallType, baseX, baseY);
                }
                else
                {
                    // For straight walls: draw one tile on the side facing the empty space
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
            Rectangle destRect = new Rectangle(screenX, screenY, Definition.WALL_TILE_SIZE, Definition.WALL_TILE_SIZE);
            
            // Scale the source tile to destination
            g.DrawImage(wallTilemap, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        private void DrawWallTileStretched(Graphics g, WallType wallType, int screenX, int screenY, int width, int height)
        {
            if (wallTilemap == null) return;

            Rectangle sourceRect = GetWallTileSourceRect(wallType);
            Rectangle destRect = new Rectangle(screenX, screenY, width, height);
            
            // Scale the source tile to the specified destination size
            g.DrawImage(wallTilemap, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        private Rectangle GetWallTileSourceRect(WallType wallType)
        {
            // Based on the tilemap specification:
            // Each tile is TILEMAP_TILE_SIZE with 1 pixel spacing
            // So tile positions are: 0-5, 7-12, 14-19, 21-26, etc.
            
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
            
            return new Rectangle(tileX, tileY, Definition.TILEMAP_TILE_SIZE, Definition.TILEMAP_TILE_SIZE);
        }

        /// <summary>
        /// Checks if the player has reached the finish position.
        /// </summary>
        /// <param name="playerX">Player X position in pixels</param>
        /// <param name="playerY">Player Y position in pixels</param>
        /// <param name="playerWidth">Player width in pixels</param>
        /// <param name="playerHeight">Player height in pixels</param>
        /// <returns>True if player has reached the finish</returns>
        public virtual bool IsLevelComplete(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            // Default implementation - no finish position in base Level class
            return false;
        }
        
        /// <summary>
        /// Gets the finish position for this level.
        /// </summary>
        /// <returns>Finish position in grid coordinates, or Point.Empty if no finish</returns>
        public virtual Point GetFinishPosition()
        {
            return Point.Empty; // Default - no finish position
        }

        /// <summary>
        /// Checks if the player is overlapping any dots and collects them.
        /// When a player touches any part of a dot area, the entire area is collected.
        /// </summary>
        /// <param name="playerX">Player X position in pixels</param>
        /// <param name="playerY">Player Y position in pixels</param>
        /// <param name="playerWidth">Player width in pixels</param>
        /// <param name="playerHeight">Player height in pixels</param>
        /// <returns>Number of dot areas collected</returns>
        public int CollectDots(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            int dotsCollectedCount = 0;
            
            // Calculate which grid cells the player overlaps
            int startGridX = Math.Max(0, Definition.PixelToGrid((int)playerX));
            int endGridX = Math.Min(gridWidth - 1, Definition.PixelToGrid((int)(playerX + playerWidth - 1)));
            int startGridY = Math.Max(0, Definition.PixelToGrid((int)playerY));
            int endGridY = Math.Min(gridHeight - 1, Definition.PixelToGrid((int)(playerY + playerHeight - 1)));
            
            // Track which 2x2 blocks we've already processed
            bool[,] processed = new bool[gridWidth, gridHeight];
            
            // Check for 2x2 dot blocks that the player is touching
            for (int gridX = startGridX; gridX <= endGridX; gridX++)
            {
                for (int gridY = startGridY; gridY <= endGridY; gridY++)
                {
                    if (processed[gridX, gridY] || levelData[gridX, gridY] != Definition.DOT)
                        continue;
                    
                    // Check if this is the top-left corner of a 2x2 dot block
                    if (gridX < gridWidth - 1 && gridY < gridHeight - 1 &&
                        levelData[gridX, gridY] == Definition.DOT &&
                        levelData[gridX + 1, gridY] == Definition.DOT &&
                        levelData[gridX, gridY + 1] == Definition.DOT &&
                        levelData[gridX + 1, gridY + 1] == Definition.DOT)
                    {
                        // Check if this 2x2 block hasn't been collected yet
                        if (!dotsCollected[gridX, gridY] && !dotsCollected[gridX + 1, gridY] && 
                            !dotsCollected[gridX, gridY + 1] && !dotsCollected[gridX + 1, gridY + 1])
                        {
                            // Collect the entire 2x2 block
                            dotsCollected[gridX, gridY] = true;
                            dotsCollected[gridX + 1, gridY] = true;
                            dotsCollected[gridX, gridY + 1] = true;
                            dotsCollected[gridX + 1, gridY + 1] = true;
                            dotsCollectedCount++;
                        }
                        
                        // Mark as processed
                        processed[gridX, gridY] = true;
                        processed[gridX + 1, gridY] = true;
                        processed[gridX, gridY + 1] = true;
                        processed[gridX + 1, gridY + 1] = true;
                    }
                    // Also check if this cell is part of a 2x2 block starting elsewhere
                    else
                    {
                        // Check all possible 2x2 blocks this cell could be part of
                        for (int dx = -1; dx <= 0; dx++)
                        {
                            for (int dy = -1; dy <= 0; dy++)
                            {
                                int blockX = gridX + dx;
                                int blockY = gridY + dy;
                                
                                if (blockX >= 0 && blockY >= 0 && blockX < gridWidth - 1 && blockY < gridHeight - 1 &&
                                    !processed[blockX, blockY] &&
                                    levelData[blockX, blockY] == Definition.DOT &&
                                    levelData[blockX + 1, blockY] == Definition.DOT &&
                                    levelData[blockX, blockY + 1] == Definition.DOT &&
                                    levelData[blockX + 1, blockY + 1] == Definition.DOT)
                                {
                                    // Check if this 2x2 block hasn't been collected yet
                                    if (!dotsCollected[blockX, blockY] && !dotsCollected[blockX + 1, blockY] && 
                                        !dotsCollected[blockX, blockY + 1] && !dotsCollected[blockX + 1, blockY + 1])
                                    {
                                        // Collect the entire 2x2 block
                                        dotsCollected[blockX, blockY] = true;
                                        dotsCollected[blockX + 1, blockY] = true;
                                        dotsCollected[blockX, blockY + 1] = true;
                                        dotsCollected[blockX + 1, blockY + 1] = true;
                                        dotsCollectedCount++;
                                    }
                                    
                                    // Mark as processed
                                    processed[blockX, blockY] = true;
                                    processed[blockX + 1, blockY] = true;
                                    processed[blockX, blockY + 1] = true;
                                    processed[blockX + 1, blockY + 1] = true;
                                }
                            }
                        }
                    }
                }
            }
            
            return dotsCollectedCount;
        }

        /// <summary>
        /// Checks if the player is overlapping any coins and collects them.
        /// When a player touches any part of a coin area, the entire area is collected.
        /// </summary>
        /// <param name="playerX">Player X position in pixels</param>
        /// <param name="playerY">Player Y position in pixels</param>
        /// <param name="playerWidth">Player width in pixels</param>
        /// <param name="playerHeight">Player height in pixels</param>
        /// <returns>Number of coin areas collected</returns>
        public int CollectCoins(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            int coinsCollectedCount = 0;
            
            // Calculate which grid cells the player overlaps
            int startGridX = Math.Max(0, Definition.PixelToGrid((int)playerX));
            int endGridX = Math.Min(gridWidth - 1, Definition.PixelToGrid((int)(playerX + playerWidth - 1)));
            int startGridY = Math.Max(0, Definition.PixelToGrid((int)playerY));
            int endGridY = Math.Min(gridHeight - 1, Definition.PixelToGrid((int)(playerY + playerHeight - 1)));
            
            // Check for coins that the player is touching
            for (int gridX = startGridX; gridX <= endGridX; gridX++)
            {
                for (int gridY = startGridY; gridY <= endGridY; gridY++)
                {
                    // Check if there's a coin at this position and it hasn't been collected
                    if (gridX < gridWidth - 1 && gridY < gridHeight - 1 &&
                        coinPositions[gridX, gridY] && !coinsCollected[gridX, gridY])
                    {
                        // Collect the coin
                        coinsCollected[gridX, gridY] = true;
                        coinsCollectedCount++;
                        
                        System.Diagnostics.Debug.WriteLine($"Coin collected at grid position ({gridX}, {gridY})");
                    }
                }
            }
            
            return coinsCollectedCount;
        }

        private void RenderDots(Graphics g)
        {
            if (dotNormalTexture == null || dotWhiteTexture == null) return;
            
            // Calculate which dot texture to use based on time (0.4 second intervals, synchronized with coins)
            double timeElapsed = (DateTime.Now - lastDotAnimationTime).TotalSeconds;
            bool useWhiteDot = ((int)(timeElapsed / 0.4)) % 2 == 1;
            Image dotTexture = useWhiteDot ? dotWhiteTexture : dotNormalTexture;
            
            // Track which cells we've already processed to avoid duplicate dots
            bool[,] processed = new bool[gridWidth, gridHeight];
            
            // Look for 2x2 blocks of '*' characters, similar to how SS and FF work
            for (int x = 0; x < gridWidth - 1; x++)
            {
                for (int y = 0; y < gridHeight - 1; y++)
                {
                    // Skip if already processed
                    if (processed[x, y]) continue;
                    
                    // Check if we have a 2x2 block of dots
                    if (levelData[x, y] == Definition.DOT &&
                        levelData[x + 1, y] == Definition.DOT &&
                        levelData[x, y + 1] == Definition.DOT &&
                        levelData[x + 1, y + 1] == Definition.DOT)
                    {
                        // Check if this 2x2 dot block hasn't been collected AND doesn't have a coin
                        if (!dotsCollected[x, y] && !dotsCollected[x + 1, y] && 
                            !dotsCollected[x, y + 1] && !dotsCollected[x + 1, y + 1] &&
                            !coinPositions[x, y]) // Don't render dot if there's a coin here
                        {
                            // Render one dot in the center of the 2x2 block
                            float centerX = Definition.GridToPixel(x) + Definition.GRID_SIZE;
                            float centerY = Definition.GridToPixel(y) + Definition.GRID_SIZE;
                            int dotX = (int)(centerX - dotTexture.Width / 2);
                            int dotY = (int)(centerY - dotTexture.Height / 2);
                            
                            g.DrawImage(dotTexture, dotX, dotY);
                        }
                        
                        // Mark all 4 cells as processed
                        processed[x, y] = true;
                        processed[x + 1, y] = true;
                        processed[x, y + 1] = true;
                        processed[x + 1, y + 1] = true;
                    }
                }
            }
        }

        private void RenderCoins(Graphics g)
        {
            if (coinNormalTexture == null || coinWhiteTexture == null) return;
            
            // Calculate which coin texture to use based on time (0.4 second intervals, synchronized with dots)
            double timeElapsed = (DateTime.Now - lastDotAnimationTime).TotalSeconds;
            bool useWhiteCoin = ((int)(timeElapsed / 0.4)) % 2 == 1;
            
            Image coinTexture = useWhiteCoin ? coinWhiteTexture : coinNormalTexture;
            
            // Calculate animation frame within the current texture (4 frames in 0.4s = 0.1s per frame)
            double textureTime = (timeElapsed % 0.4); // Time within current 0.4s interval
            int frameIndex = (int)(textureTime / 0.1); // 4 frames per 0.4s = 0.1s per frame
            frameIndex = Math.Min(frameIndex, 3); // Ensure frame index is 0-3
            
            // Each frame is 12x12 pixels in a 48x12 sprite sheet
            Rectangle sourceRect = new Rectangle(frameIndex * 12, 0, 12, 12);
            
            // Render coins at their designated positions
            for (int x = 0; x < gridWidth - 1; x++)
            {
                for (int y = 0; y < gridHeight - 1; y++)
                {
                    // Check if there's a coin at this 2x2 block and it hasn't been collected
                    if (coinPositions[x, y] && !coinsCollected[x, y])
                    {
                        // Calculate center of the 2x2 block (same positioning as dots)
                        int blockCenterX = Definition.GridToPixel(x) + Definition.GRID_SIZE;
                        int blockCenterY = Definition.GridToPixel(y) + Definition.GRID_SIZE;
                        
                        // Center the 12x12 coin frame in the 24x24 block
                        int coinX = blockCenterX - 6; // 12/2 = 6
                        int coinY = blockCenterY - 6;
                        
                        Rectangle destRect = new Rectangle(coinX, coinY, 12, 12);
                        
                        // Draw the current animation frame
                        g.DrawImage(coinTexture, destRect, sourceRect, GraphicsUnit.Pixel);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the player is entering a portal and handles teleportation.
        /// Returns the new position if teleported, or the original position if not.
        /// </summary>
        /// <param name="gridX">Player's current grid X position</param>
        /// <param name="gridY">Player's current grid Y position</param>
        /// <param name="movementDirection">Direction the player is moving (for exit direction)</param>
        /// <returns>New position after teleportation, or original position if no teleportation</returns>
        public Point CheckPortalTeleportation(int gridX, int gridY)
        {
            // Check cooldown - prevent rapid re-teleportation
            if ((DateTime.Now - lastPortalTeleportTime).TotalSeconds < PORTAL_COOLDOWN_SECONDS)
            {
                return new Point(gridX, gridY);
            }
            
            // Check if player position overlaps with any portal
            foreach (Portal portal in portals)
            {
                if (portal.ContainsPosition(gridX, gridY) && portal.PairedPortal != null)
                {
                    // Teleport directly to the exact center position of the paired portal
                    Point teleportPosition = portal.PairedPortal.Position;
                    
                    // Update cooldown timer
                    lastPortalTeleportTime = DateTime.Now;
                    
                    System.Diagnostics.Debug.WriteLine($"Portal teleportation: ({gridX},{gridY}) -> ({teleportPosition.X},{teleportPosition.Y})");
                    return teleportPosition;
                }
            }
            
            // No portal teleportation occurred
            return new Point(gridX, gridY);
        }

        /// <summary>
        /// Checks if the given position is inside any portal area.
        /// </summary>
        /// <param name="gridX">Grid X position</param>
        /// <param name="gridY">Grid Y position</param>
        /// <returns>True if position is inside a portal</returns>
        public bool IsInPortal(int gridX, int gridY)
        {
            return portals.Any(portal => portal.ContainsPosition(gridX, gridY));
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
            dotNormalTexture?.Dispose();
            dotWhiteTexture?.Dispose();
            coinNormalTexture?.Dispose();
            coinWhiteTexture?.Dispose();
        }

        private void RenderPortals(Graphics g)
        {
            if (portalTexture == null) return;
            
            // Render all portals
            foreach (Portal portal in portals)
            {
                // Convert grid coordinates to pixel coordinates
                int portalPixelX = Definition.GridToPixel(portal.Position.X);
                int portalPixelY = Definition.GridToPixel(portal.Position.Y);
                
                // Calculate portal animation frame based on time
                double timeElapsed = (DateTime.Now - portal.LastAnimationTime).TotalSeconds;
                int frameIndex = (int)(timeElapsed * 8) % 4; // 8 fps animation, 4 frames
                
                // Assuming portal texture is a horizontal sprite sheet with 4 frames
                Rectangle sourceRect = new Rectangle(frameIndex * Definition.GRID_SIZE * 2, 0, 
                    Definition.GRID_SIZE * 2, Definition.GRID_SIZE * 2);
                Rectangle destRect = new Rectangle(portalPixelX, portalPixelY, 
                    Definition.GRID_SIZE * 2, Definition.GRID_SIZE * 2);
                
                g.DrawImage(portalTexture, destRect, sourceRect, GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// Loads trap layer data from a GeneratedLevel if available.
        /// </summary>
        /// <param name="levelDefinition">Level definition that may contain trap data</param>
        protected virtual void LoadTrapLayer(ILevelDefinition levelDefinition)
        {
            if (levelDefinition is GeneratedLevelDefinition generatedDef)
            {
                string[] trapLayer = generatedDef.TrapLayer;
                if (trapLayer != null && trapLayer.Length > 0)
                {
                    // Clear existing spike 2 traps
                    spike2Traps.Clear();
                    
                    // Process trap layer
                    for (int y = 0; y < Math.Min(gridHeight, trapLayer.Length); y++)
                    {
                        string row = trapLayer[y];
                        for (int x = 0; x < Math.Min(gridWidth, row.Length); x++)
                        {
                            char trapChar = row[x];
                            if (Definition.IsTrapCharacter(trapChar))
                            {
                                trapTypes[x, y] = trapChar;
                                levelData[x, y] = TRAP; // Override level data to mark as trap
                                
                                // Create Spike 2 trap instances for '?' characters
                                if (trapChar == '?')
                                {
                                    // Determine wall orientation based on surrounding walls
                                    int wallOrientation = DetermineWallOrientationForTrap(x, y);
                                    spike2Traps.Add(new Spike2Trap(new Point(x, y), wallOrientation));
                                    System.Diagnostics.Debug.WriteLine($"Created Spike 2 trap at ({x}, {y}) with orientation {wallOrientation}");
                                }
                            }
                            else
                            {
                                trapTypes[x, y] = '.'; // No trap
                            }
                        }
                    }
                }
            }
        }

        private void RenderTraps(Graphics g)
        {
            if (trapTypes == null) return;
            
            // Render static traps (like spike 1 and cannons)
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    char trapType = trapTypes[x, y];
                    if (trapType != '.' && trapType != '\0')
                    {
                        // Convert grid coordinates to pixel coordinates
                        int pixelX = Definition.GridToPixel(x);
                        int pixelY = Definition.GridToPixel(y);
                        
                        // Handle different trap types
                        switch (trapType)
                        {
                            case '!': // Spike type 1
                            case '^': // Original spike character
                                if (spikeTexture != null)
                                {
                                    Rectangle destRect = new Rectangle(pixelX, pixelY, GRID_SIZE, GRID_SIZE);
                                    g.DrawImage(spikeTexture, destRect);
                                }
                                else
                                {
                                    // Fallback: render a gray square
                                    using (Brush trapBrush = new SolidBrush(Color.Gray))
                                    {
                                        Rectangle trapRect = new Rectangle(pixelX, pixelY, GRID_SIZE, GRID_SIZE);
                                        g.FillRectangle(trapBrush, trapRect);
                                    }
                                }
                                break;
                                
                            case 'N': // Cannon from procedural generation
                            case 'C': // Original cannon character
                                if (cannonTexture != null)
                                {
                                    Rectangle destRect = new Rectangle(pixelX, pixelY, GRID_SIZE, GRID_SIZE);
                                    g.DrawImage(cannonTexture, destRect);
                                }
                                else
                                {
                                    // Fallback: render a dark red square
                                    using (Brush trapBrush = new SolidBrush(Color.DarkRed))
                                    {
                                        Rectangle trapRect = new Rectangle(pixelX, pixelY, GRID_SIZE, GRID_SIZE);
                                        g.FillRectangle(trapBrush, trapRect);
                                    }
                                }
                                break;
                                
                            case '?': // Spike type 2 - handled separately below
                                break;
                        }
                    }
                }
            }
            
            // Render Spike 2 traps with proper orientation and animation
            RenderSpike2Traps(g);
        }
        
        private void RenderSpike2Traps(Graphics g)
        {
            System.Diagnostics.Debug.WriteLine($"RenderSpike2Traps called with {spike2Traps.Count} traps");
            foreach (var spike2Trap in spike2Traps)
            {
                // Render the base spike (idle state)
                Image? baseTexture = GetSpike2BaseTexture(spike2Trap.WallOrientation);
                int pixelX = Definition.GridToPixel(spike2Trap.GridPosition.X);
                int pixelY = Definition.GridToPixel(spike2Trap.GridPosition.Y);
                Rectangle destRect = new Rectangle(pixelX, pixelY, GRID_SIZE, GRID_SIZE);
                
                if (baseTexture != null)
                {
                    g.DrawImage(baseTexture, destRect);
                }
                else
                {
                    // Fallback: render a blue square to show where spike 2 traps should be
                    using (Brush fallbackBrush = new SolidBrush(Color.Blue))
                    {
                        g.FillRectangle(fallbackBrush, destRect);
                    }
                }
                
                // Render attack animation if the spike is attacking
                if (spike2Trap.IsDeadly() && spike2AttackTexture != null)
                {
                    List<Point> attackPositions = spike2Trap.GetAttackPositions();
                    int frame = spike2Trap.GetCurrentFrame();
                    if (frame >= 0)
                    {
                        for (int i = 0; i < attackPositions.Count; i++)
                        {
                            int attackDirection = spike2Trap.AttackDirections[i];
                            RenderSpike2Attack(g, attackPositions[i], attackDirection, frame);
                        }
                    }
                }
            }
        }
        
        private Image? GetSpike2BaseTexture(int wallOrientation)
        {
            return wallOrientation switch
            {
                2 => spike2DownTexture,       // Down
                4 => spike2RightTexture,      // Right
                6 => spike2LeftTexture,       // Left
                8 => spike2UpTexture,         // Up
                10 => spike2CornerUpLeftTexture,     // Corner Up-Left
                12 => spike2CornerUpRightTexture,    // Corner Up-Right
                14 => spike2CornerDownLeftTexture,   // Corner Down-Left
                16 => spike2CornerDownRightTexture,  // Corner Down-Right
                _ => spike2RightTexture       // Default to right
            };
        }
        
        private void RenderSpike2Attack(Graphics g, Point attackPos, int direction, int frame)
        {
            if (spike2AttackTexture == null) return;
            
            // Assuming the attack sheet has 3 frames horizontally (left, right, middle)
            int frameWidth = spike2AttackTexture.Width / 3;
            int frameHeight = spike2AttackTexture.Height;
            
            Rectangle sourceRect = new Rectangle(frame * frameWidth, 0, frameWidth, frameHeight);
            Rectangle destRect = new Rectangle(attackPos.X, attackPos.Y, GRID_SIZE, GRID_SIZE);
            
            // Save the current graphics state for rotation
            var state = g.Save();
            
            try
            {
                // Rotate the graphics context based on attack direction
                // Default orientation is right (6), so we need to rotate for other directions
                float rotationAngle = direction switch
                {
                    2 => 90f,   // Down
                    4 => 0f,    // Right (default)
                    6 => 180f,  // Left  
                    8 => 270f,  // Up
                    _ => 0f
                };
                
                if (rotationAngle != 0f)
                {
                    // Translate to center of destination rectangle
                    g.TranslateTransform(attackPos.X + GRID_SIZE / 2, attackPos.Y + GRID_SIZE / 2);
                    g.RotateTransform(rotationAngle);
                    g.TranslateTransform(-GRID_SIZE / 2, -GRID_SIZE / 2);
                    destRect = new Rectangle(0, 0, GRID_SIZE, GRID_SIZE);
                }
                
                g.DrawImage(spike2AttackTexture, destRect, sourceRect, GraphicsUnit.Pixel);
            }
            finally
            {
                g.Restore(state);
            }
        }

        /// <summary>
        /// Determines the wall orientation for a trap based on surrounding accessible areas.
        /// Returns the direction the trap should face:
        /// - 2=down, 4=right, 6=left, 8=up (straight orientations)
        /// - 10=up-left corner, 12=up-right corner, 14=down-left corner, 16=down-right corner
        /// The spike should face INTO the open space (where the player can be).
        /// </summary>
        private int DetermineWallOrientationForTrap(int x, int y)
        {
            // Check adjacent cells for accessible areas (asterisk or comma) that the player can approach from
            // Face towards the nearest accessible area that directly touches the spike
            
            // Check all four adjacent directions for accessible areas
            bool hasAccessibleAbove = (y > 0 && IsAccessibleArea(x, y - 1));
            bool hasAccessibleBelow = (y < gridHeight - 1 && IsAccessibleArea(x, y + 1));
            bool hasAccessibleLeft = (x > 0 && IsAccessibleArea(x - 1, y));
            bool hasAccessibleRight = (x < gridWidth - 1 && IsAccessibleArea(x + 1, y));
            
            // Check for corner cases (two adjacent accessible areas)
            if (hasAccessibleAbove && hasAccessibleLeft) return 10; // Up-Left corner
            if (hasAccessibleAbove && hasAccessibleRight) return 12; // Up-Right corner
            if (hasAccessibleBelow && hasAccessibleLeft) return 14; // Down-Left corner
            if (hasAccessibleBelow && hasAccessibleRight) return 16; // Down-Right corner
            
            // Face towards the first accessible area found (priority: up, down, left, right)
            if (hasAccessibleAbove) return 8; // Face up towards accessible area above
            if (hasAccessibleBelow) return 2; // Face down towards accessible area below
            if (hasAccessibleLeft) return 6; // Face left towards accessible area left
            if (hasAccessibleRight) return 4; // Face right towards accessible area right
            
            // If no direct adjacent accessible areas, check diagonally for guidance
            bool hasAccessibleUpLeft = (x > 0 && y > 0 && IsAccessibleArea(x - 1, y - 1));
            bool hasAccessibleUpRight = (x < gridWidth - 1 && y > 0 && IsAccessibleArea(x + 1, y - 1));
            bool hasAccessibleDownLeft = (x > 0 && y < gridHeight - 1 && IsAccessibleArea(x - 1, y + 1));
            bool hasAccessibleDownRight = (x < gridWidth - 1 && y < gridHeight - 1 && IsAccessibleArea(x + 1, y + 1));
            
            // Check for diagonal corner cases
            if (hasAccessibleUpLeft && !hasAccessibleAbove && !hasAccessibleLeft) return 10; // Up-Left corner
            if (hasAccessibleUpRight && !hasAccessibleAbove && !hasAccessibleRight) return 12; // Up-Right corner
            if (hasAccessibleDownLeft && !hasAccessibleBelow && !hasAccessibleLeft) return 14; // Down-Left corner
            if (hasAccessibleDownRight && !hasAccessibleBelow && !hasAccessibleRight) return 16; // Down-Right corner
            
            // Face towards diagonal accessible areas (prioritize up/down over left/right)
            if (hasAccessibleUpLeft || hasAccessibleUpRight) return 8; // Face up
            if (hasAccessibleDownLeft || hasAccessibleDownRight) return 2; // Face down
            if (hasAccessibleUpLeft || hasAccessibleDownLeft) return 6; // Face left
            if (hasAccessibleUpRight || hasAccessibleDownRight) return 4; // Face right
            
            // Fallback to down if no accessible areas found
            return 2;
        }
        
        /// <summary>
        /// Checks if a position contains an accessible area where the player can move.
        /// </summary>
        private bool IsAccessibleArea(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
            
            // Check the original characters array for accessible characters
            char cell = originalCharacters[x, y];
            return cell == '*' || cell == ',' || cell == 'S' || cell == 'F'; // Asterisk, comma, start, finish
        }

        /// <summary>
        /// Updates all dynamic elements in the level including Spike 2 traps.
        /// This should be called every frame from the game engine.
        /// </summary>
        /// <param name="playerX">Player X position in pixels</param>
        /// <param name="playerY">Player Y position in pixels</param>
        public void Update(float playerX, float playerY)
        {
            // Update all Spike 2 traps
            foreach (var spike2Trap in spike2Traps)
            {
                spike2Trap.Update();
                
                // Check if player is near this trap (adjacent cell)
                if (spike2Trap.State == Spike2State.Idle)
                {
                    if (IsPlayerNearTrap(playerX, playerY, spike2Trap))
                    {
                        spike2Trap.Activate();
                        System.Diagnostics.Debug.WriteLine($"Spike 2 trap activated at ({spike2Trap.GridPosition.X}, {spike2Trap.GridPosition.Y}) with orientation {spike2Trap.WallOrientation}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if the player is near a Spike 2 trap (in cells that would be attacked).
        /// </summary>
        private bool IsPlayerNearTrap(float playerX, float playerY, Spike2Trap trap)
        {
            int playerGridX = Definition.PixelToGrid((int)playerX);
            int playerGridY = Definition.PixelToGrid((int)playerY);
            
            int trapX = trap.GridPosition.X;
            int trapY = trap.GridPosition.Y;
            
            // Don't trigger if player is on the trap itself
            if (playerGridX == trapX && playerGridY == trapY)
                return false;
            
            // Check if player is in any of the cells that would be attacked by this spike
            foreach (int attackDirection in trap.AttackDirections)
            {
                int attackGridX = trapX;
                int attackGridY = trapY;
                
                // Calculate the grid position that would be attacked in this direction
                switch (attackDirection)
                {
                    case 2: // Down
                        attackGridY += 1;
                        break;
                    case 4: // Right
                        attackGridX += 1;
                        break;
                    case 6: // Left
                        attackGridX -= 1;
                        break;
                    case 8: // Up
                        attackGridY -= 1;
                        break;
                }
                
                // Check if player is in this attack cell
                if (playerGridX == attackGridX && playerGridY == attackGridY)
                {
                    System.Diagnostics.Debug.WriteLine($"Player near trap at ({trapX}, {trapY}), player at ({playerGridX}, {playerGridY}), attack direction {attackDirection}");
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if the player is colliding with any Spike 2 traps.
        /// Returns true if the player should be killed (when spikes are attacking),
        /// or if the player is blocked by an idle spike (collision but no death).
        /// </summary>
        public bool CheckSpike2Collision(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            Rectangle playerRect = new Rectangle((int)playerX, (int)playerY, playerWidth, playerHeight);
            
            foreach (var spike2Trap in spike2Traps)
            {
                // Check for deadly collision (when spike is attacking)
                if (spike2Trap.IsDeadly())
                {
                    List<Rectangle> attackRects = spike2Trap.GetAttackCollisionRects();
                    foreach (Rectangle attackRect in attackRects)
                    {
                        if (playerRect.IntersectsWith(attackRect))
                        {
                            return true; // Player killed by spike attack
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if the player movement is blocked by any Spike 2 traps (solid collision).
        /// This should be called during movement to prevent walking through idle spikes.
        /// </summary>
        public bool CheckSpike2BlockingCollision(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            Rectangle playerRect = new Rectangle((int)playerX, (int)playerY, playerWidth, playerHeight);
            
            foreach (var spike2Trap in spike2Traps)
            {
                // Spike 2 traps always block movement (act as walls)
                int spikePixelX = Definition.GridToPixel(spike2Trap.GridPosition.X);
                int spikePixelY = Definition.GridToPixel(spike2Trap.GridPosition.Y);
                Rectangle spikeRect = new Rectangle(spikePixelX, spikePixelY, GRID_SIZE, GRID_SIZE);
                
                if (playerRect.IntersectsWith(spikeRect))
                {
                    return true; // Movement blocked by spike
                }
            }
            
            return false;
        }
    }
}
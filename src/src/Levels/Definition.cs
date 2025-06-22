using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Clawbyrinth.Levels
{
    /// <summary>
    /// Contains all the global definitions and constants for the Clawbyrinth game.
    /// This includes grid sizing, tile definitions, collision settings, and level parsing rules.
    /// </summary>
    public static class Definition
    {
        // === GRID AND TILE SYSTEM ===
        /// <summary>
        /// Size of each grid cell in pixels. All game elements are aligned to this grid.
        /// </summary>
        public const int GRID_SIZE = 12;
        
        /// <summary>
        /// Scaling factor for blueprint templates. Each character in the template 
        /// represents a 2x2 block of grid cells for better visual representation.
        /// </summary>
        public const int BLUEPRINT_SCALE = 2;
        
        /// <summary>
        /// Size of each wall tile in pixels when rendered (scaled from original tilemap).
        /// </summary>
        public const int WALL_TILE_SIZE = 12;
        
        /// <summary>
        /// Original tile size in the wall tilemap image before scaling.
        /// </summary>
        public const int TILEMAP_TILE_SIZE = 6;
        
        /// <summary>
        /// Collision box size for wall tiles, matching the actual rendered tile dimensions.
        /// </summary>
        public const int WALL_COLLISION_SIZE = 6;
        
        /// <summary>
        /// Player collision box size, should match wall collision for pixel-perfect detection.
        /// </summary>
        public const int PLAYER_COLLISION_SIZE = 6;
        
        /// <summary>
        /// Player sprite size (visual representation, larger than collision box).
        /// </summary>
        public const int PLAYER_SPRITE_SIZE = 12;
        
        /// <summary>
        /// Player grid occupation size (how many grid cells the player occupies).
        /// </summary>
        public const int PLAYER_GRID_SIZE = 2;
        
        /// <summary>
        /// Player render size (how big the player appears on screen).
        /// </summary>
        public const int PLAYER_RENDER_SIZE = GRID_SIZE * PLAYER_GRID_SIZE;

        // === LEVEL DEFINITION CHARACTERS ===
        /// <summary>
        /// Character representing a wall tile in level blueprints.
        /// </summary>
        public const char WALL_CHAR = '#';
        
        /// <summary>
        /// Character representing an empty space in level blueprints.
        /// </summary>
        public const char EMPTY_CHAR = '.';
        
        /// <summary>
        /// Character representing the player start position in level blueprints.
        /// Note: S appears multiple times in a block due to sprite scaling.
        /// </summary>
        public const char START_CHAR = 'S';
        
        /// <summary>
        /// Character representing the finish/goal position in level blueprints.
        /// Note: F appears multiple times in a block due to sprite scaling.
        /// </summary>
        public const char FINISH_CHAR = 'F';
        
        // === ORIENTED WALL CHARACTERS ===
        /// <summary>
        /// Oriented wall system - each number represents a specific wall type
        /// </summary>
        public const char LOWER_LEFT_CORNER = '1';     // Lower left corner
        public const char LOWER_WALL = '2';            // Lower/bottom wall
        public const char LOWER_RIGHT_CORNER = '3';    // Lower right corner
        public const char LEFT_WALL = '4';             // Left wall
        public const char RIGHT_WALL = '6';            // Right wall
        public const char UPPER_LEFT_CORNER = '7';     // Upper left corner
        public const char UPPER_WALL = '8';            // Upper/top wall
        public const char UPPER_RIGHT_CORNER = '9';    // Upper right corner
        
        /// <summary>
        /// Character representing a portal entrance in level blueprints.
        /// </summary>
        public const char PORTAL_CHAR = 'P';
        
        /// <summary>
        /// Character representing spikes trap in level blueprints.
        /// </summary>
        public const char SPIKES_CHAR = '^';
        
        /// <summary>
        /// Character representing cannon trap in level blueprints.
        /// </summary>
        public const char CANNON_CHAR = 'C';

        // === LEVEL DATA CONSTANTS ===
        /// <summary>
        /// Internal representation of a wall tile in the level data array.
        /// </summary>
        public const int WALL = 1;
        
        /// <summary>
        /// Internal representation of an empty space in the level data array.
        /// </summary>
        public const int EMPTY = 0;
        
        /// <summary>
        /// Internal representation of a trap in the level data array.
        /// </summary>
        public const int TRAP = 2;
        
        /// <summary>
        /// Internal representation of a portal in the level data array.
        /// </summary>
        public const int PORTAL = 3;

        // === MOVEMENT AND PHYSICS ===
        /// <summary>
        /// Player movement speed in pixels per second.
        /// </summary>
        public const float PLAYER_MOVE_SPEED = 1000.0f;
        
        /// <summary>
        /// Grid-based movement speed multiplier for smooth animation.
        /// </summary>
        public const float MOVEMENT_SMOOTHING = 8.0f;

        // === ASSET PATHS ===
        /// <summary>
        /// Path to the wall tilemap texture.
        /// </summary>
        public const string WALL_TILEMAP_PATH = "Assets/Walls/new_wall_tilemap.png";
        
        /// <summary>
        /// Path to the player sprite sheets directory.
        /// </summary>
        public const string PLAYER_SPRITES_PATH = "Assets/Player/";
        
        /// <summary>
        /// Path to the trap assets directory.
        /// </summary>
        public const string TRAPS_PATH = "Assets/Traps/";
        
        /// <summary>
        /// Path to the mechanics assets directory.
        /// </summary>
        public const string MECHANICS_PATH = "Assets/Mechanics/";
        
        /// <summary>
        /// Path to the level map templates directory.
        /// </summary>
        public const string MAP_TEMPLATES_PATH = "src/Levels/Map Templates/";

        // === UTILITY METHODS ===
        
        /// <summary>
        /// Converts grid coordinates to pixel coordinates.
        /// </summary>
        /// <param name="gridCoord">Grid coordinate (0-based)</param>
        /// <returns>Pixel coordinate</returns>
        public static int GridToPixel(int gridCoord)
        {
            return gridCoord * GRID_SIZE;
        }
        
        /// <summary>
        /// Converts pixel coordinates to grid coordinates.
        /// </summary>
        /// <param name="pixelCoord">Pixel coordinate</param>
        /// <returns>Grid coordinate (0-based)</returns>
        public static int PixelToGrid(int pixelCoord)
        {
            return pixelCoord / GRID_SIZE;
        }
        
        /// <summary>
        /// Converts grid coordinates to a Point in pixel space.
        /// </summary>
        /// <param name="gridX">Grid X coordinate</param>
        /// <param name="gridY">Grid Y coordinate</param>
        /// <returns>Point in pixel coordinates</returns>
        public static Point GridToPixelPoint(int gridX, int gridY)
        {
            return new Point(GridToPixel(gridX), GridToPixel(gridY));
        }
        
        /// <summary>
        /// Converts pixel coordinates to a Point in grid space.
        /// </summary>
        /// <param name="pixelX">Pixel X coordinate</param>
        /// <param name="pixelY">Pixel Y coordinate</param>
        /// <returns>Point in grid coordinates</returns>
        public static Point PixelToGridPoint(int pixelX, int pixelY)
        {
            return new Point(PixelToGrid(pixelX), PixelToGrid(pixelY));
        }
        
        /// <summary>
        /// Checks if a character represents a solid obstacle.
        /// </summary>
        /// <param name="c">Character from level blueprint</param>
        /// <returns>True if the character represents a solid wall</returns>
        public static bool IsWallCharacter(char c)
        {
            return c == WALL_CHAR || IsOrientedWallCharacter(c);
        }
        
        /// <summary>
        /// Checks if a character represents an oriented wall (1-9).
        /// </summary>
        /// <param name="c">Character from level blueprint</param>
        /// <returns>True if the character represents an oriented wall</returns>
        public static bool IsOrientedWallCharacter(char c)
        {
            return c >= '1' && c <= '9' && c != '5'; // 1-9 except 5 (no center wall type)
        }
        
        /// <summary>
        /// Checks if a character represents an empty, walkable space.
        /// </summary>
        /// <param name="c">Character from level blueprint</param>
        /// <returns>True if the character represents empty space</returns>
        public static bool IsEmptyCharacter(char c)
        {
            return c == EMPTY_CHAR || c == START_CHAR || c == FINISH_CHAR || c == PORTAL_CHAR;
        }
        
        /// <summary>
        /// Checks if a character represents a trap or hazard.
        /// </summary>
        /// <param name="c">Character from level blueprint</param>
        /// <returns>True if the character represents a trap</returns>
        public static bool IsTrapCharacter(char c)
        {
            return c == SPIKES_CHAR || c == CANNON_CHAR;
        }
        
        /// <summary>
        /// Converts a blueprint character to its corresponding level data value.
        /// </summary>
        /// <param name="c">Character from level blueprint</param>
        /// <returns>Integer value for level data array</returns>
        public static int CharacterToLevelData(char c)
        {
            return c switch
            {
                WALL_CHAR => WALL,
                LOWER_LEFT_CORNER => WALL,      // '1'
                LOWER_WALL => WALL,             // '2'
                LOWER_RIGHT_CORNER => WALL,     // '3'
                LEFT_WALL => WALL,              // '4'
                RIGHT_WALL => WALL,             // '6'
                UPPER_LEFT_CORNER => WALL,      // '7'
                UPPER_WALL => WALL,             // '8'
                UPPER_RIGHT_CORNER => WALL,     // '9'
                SPIKES_CHAR => TRAP,
                CANNON_CHAR => TRAP,
                PORTAL_CHAR => PORTAL,
                _ => EMPTY // Default to empty for START_CHAR, FINISH_CHAR, EMPTY_CHAR, etc.
            };
        }
        
        /// <summary>
        /// Converts an oriented wall character directly to its corresponding WallType.
        /// Uses both variants (1 and 2) for visual variety based on position.
        /// </summary>
        /// <param name="c">Character from level blueprint (1-9)</param>
        /// <param name="x">X position for variant selection</param>
        /// <param name="y">Y position for variant selection</param>
        /// <returns>WallType enum value</returns>
        public static WallType CharacterToWallType(char c, int x, int y)
        {
            // Use position to determine which variant to use for visual variety
            bool useVariant2 = (x + y) % 2 == 1;
            
            return c switch
            {
                LOWER_LEFT_CORNER => WallType.CornerLowerLeft,      // '1' - corners only have one type
                LOWER_WALL => useVariant2 ? WallType.Lower2 : WallType.Lower1,         // '2' - alternate variants
                LOWER_RIGHT_CORNER => WallType.CornerLowerRight,   // '3' - corners only have one type
                LEFT_WALL => useVariant2 ? WallType.Left2 : WallType.Left1,           // '4' - alternate variants
                RIGHT_WALL => useVariant2 ? WallType.Right2 : WallType.Right1,         // '6' - alternate variants
                UPPER_LEFT_CORNER => WallType.CornerUpperLeft,     // '7' - corners only have one type
                UPPER_WALL => useVariant2 ? WallType.Upper2 : WallType.Upper1,         // '8' - alternate variants
                UPPER_RIGHT_CORNER => WallType.CornerUpperRight,   // '9' - corners only have one type
                _ => WallType.Upper1 // Default fallback
            };
        }
        
        /// <summary>
        /// Gets the collision rectangle for a grid position.
        /// </summary>
        /// <param name="gridX">Grid X coordinate</param>
        /// <param name="gridY">Grid Y coordinate</param>
        /// <returns>Rectangle representing the collision area</returns>
        public static Rectangle GetGridCollisionRect(int gridX, int gridY)
        {
            return new Rectangle(
                GridToPixel(gridX), 
                GridToPixel(gridY), 
                WALL_COLLISION_SIZE, 
                WALL_COLLISION_SIZE
            );
        }
        
        /// <summary>
        /// Gets the collision rectangle for a player at pixel coordinates.
        /// </summary>
        /// <param name="pixelX">Player X position in pixels</param>
        /// <param name="pixelY">Player Y position in pixels</param>
        /// <returns>Rectangle representing the player's collision area</returns>
        public static Rectangle GetPlayerCollisionRect(float pixelX, float pixelY)
        {
            return new Rectangle(
                (int)pixelX, 
                (int)pixelY, 
                PLAYER_COLLISION_SIZE, 
                PLAYER_COLLISION_SIZE
            );
        }
        
        /// <summary>
        /// Loads a level blueprint from a text file.
        /// </summary>
        /// <param name="fileName">Name of the file (e.g., "Level1.txt")</param>
        /// <returns>Array of strings representing the level blueprint</returns>
        public static string[] LoadLevelFromFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(MAP_TEMPLATES_PATH, fileName);
                if (File.Exists(filePath))
                {
                    return File.ReadAllLines(filePath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Level file not found: {filePath}");
                    return new string[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading level file {fileName}: {ex.Message}");
                return new string[0];
            }
        }
        
        /// <summary>
        /// Finds the center position of a marker block (like SS or FF) in the blueprint.
        /// Since S and F appear as 2x2 blocks in the template, this finds the center.
        /// </summary>
        /// <param name="blueprint">The level blueprint</param>
        /// <param name="marker">The character to find (S or F)</param>
        /// <returns>Center position of the marker block</returns>
        public static Point FindMarkerCenter(string[] blueprint, char marker)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            bool found = false;
            
            // Find the bounds of the marker block in blueprint coordinates
            for (int y = 0; y < blueprint.Length; y++)
            {
                string row = blueprint[y];
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] == marker)
                    {
                        found = true;
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }
            
            if (!found)
            {
                // Default position if marker not found
                return new Point(1, 1);
            }
            
            // Return the center of the marker block (1:1 mapping)
            return new Point((minX + maxX) / 2, (minY + maxY) / 2);
        }
    }
    
    /// <summary>
    /// Interface for level blueprint parsing and generation.
    /// </summary>
    public interface ILevelDefinition
    {
        /// <summary>
        /// Gets the level blueprint as an array of strings.
        /// </summary>
        string[] Blueprint { get; }
        
        /// <summary>
        /// Gets the name/identifier of this level.
        /// </summary>
        string LevelName { get; }
        
        /// <summary>
        /// Gets the player's starting position in grid coordinates.
        /// </summary>
        Point StartPosition { get; }
        
        /// <summary>
        /// Gets the finish/goal position in grid coordinates.
        /// </summary>
        Point FinishPosition { get; }
        
        /// <summary>
        /// Gets any special positions (portals, traps, etc.) in grid coordinates.
        /// </summary>
        Dictionary<char, List<Point>> SpecialPositions { get; }
    }
    
    /// <summary>
    /// Base class for level definitions with common parsing functionality.
    /// </summary>
    public abstract class BaseLevelDefinition : ILevelDefinition
    {
        public abstract string[] Blueprint { get; }
        public abstract string LevelName { get; }
        
        private Point? _startPosition;
        private Point? _finishPosition;
        private Dictionary<char, List<Point>>? _specialPositions;
        
        public Point StartPosition
        {
            get
            {
                if (!_startPosition.HasValue)
                    ParseBlueprint();
                return _startPosition!.Value;
            }
        }
        
        public Point FinishPosition
        {
            get
            {
                if (!_finishPosition.HasValue)
                    ParseBlueprint();
                return _finishPosition!.Value;
            }
        }
        
        public Dictionary<char, List<Point>> SpecialPositions
        {
            get
            {
                if (_specialPositions == null)
                    ParseBlueprint();
                return _specialPositions!;
            }
        }
        
        /// <summary>
        /// Parses the blueprint and extracts special positions.
        /// </summary>
        private void ParseBlueprint()
        {
            _specialPositions = new Dictionary<char, List<Point>>();
            
            // Use Definition helper to find scaled markers
            _startPosition = Definition.FindMarkerCenter(Blueprint, Definition.START_CHAR);
            _finishPosition = Definition.FindMarkerCenter(Blueprint, Definition.FINISH_CHAR);
            
            // Find other special positions (non-scaled)
            for (int y = 0; y < Blueprint.Length; y++)
            {
                string row = Blueprint[y];
                for (int x = 0; x < row.Length; x++)
                {
                    char c = row[x];
                    Point position = new Point(x, y);
                    
                    if (!Definition.IsEmptyCharacter(c) && !Definition.IsWallCharacter(c) && 
                        c != Definition.START_CHAR && c != Definition.FINISH_CHAR)
                    {
                        if (!_specialPositions.ContainsKey(c))
                            _specialPositions[c] = new List<Point>();
                        _specialPositions[c].Add(position);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// File-based level definition that loads blueprints from external text files.
    /// </summary>
    public class FileLevelDefinition : BaseLevelDefinition
    {
        private string[] _blueprint;
        private string _levelName;
        
        public FileLevelDefinition(string fileName)
        {
            _levelName = Path.GetFileNameWithoutExtension(fileName);
            _blueprint = Definition.LoadLevelFromFile(fileName);
        }
        
        public override string[] Blueprint => _blueprint;
        public override string LevelName => _levelName;
    }
}

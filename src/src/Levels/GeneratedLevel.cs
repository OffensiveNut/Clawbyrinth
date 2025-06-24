using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Clawbyrinth.Levels
{
    /// <summary>
    /// Represents a generated level that loads from the Generated folder.
    /// </summary>
    public class GeneratedLevel : Level
    {
        private GeneratedLevelDefinition levelDefinition;
        
        public GeneratedLevel(int windowWidth, int windowHeight, string fileName)
            : base(windowWidth, windowHeight, new GeneratedLevelDefinition(fileName))
        {
            levelDefinition = new GeneratedLevelDefinition(fileName);
        }
        
        public override Point GetStartPosition()
        {
            Point gridPos = levelDefinition.StartPosition;
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Start - Grid: ({gridPos.X}, {gridPos.Y})");
            Console.WriteLine($"Start - Grid: ({gridPos.X}, {gridPos.Y})");
            
            // Return grid coordinates, not pixel coordinates (Player constructor expects grid coords)
            return gridPos;
        }
        
        public override Point GetFinishPosition()
        {
            Point gridPos = levelDefinition.FinishPosition;
            // Return grid coordinates for consistency
            return gridPos;
        }
        
        /// <summary>
        /// Checks if the player has reached the finish position.
        /// </summary>
        public override bool IsLevelComplete(float playerX, float playerY, int playerWidth, int playerHeight)
        {
            Point finishGridPos = GetFinishPosition();
            if (finishGridPos == Point.Empty) return false;
            
            // Convert finish position from grid to pixel coordinates for collision check
            int finishPixelX = Definition.GridToPixel(finishGridPos.X);
            int finishPixelY = Definition.GridToPixel(finishGridPos.Y);
            
            // Check if player's collision box overlaps with the finish area (2x2 grid area)
            Rectangle playerRect = new Rectangle((int)playerX, (int)playerY, playerWidth, playerHeight);
            Rectangle finishRect = new Rectangle(finishPixelX, finishPixelY, 
                Definition.GRID_SIZE * 2, Definition.GRID_SIZE * 2);
            
            return playerRect.IntersectsWith(finishRect);
        }
        
        /// <summary>
        /// Gets the level name for display purposes.
        /// </summary>
        public string GetLevelName()
        {
            return levelDefinition.LevelName;
        }
        
        /// <summary>
        /// Gets any special positions (traps, portals, etc.) in this level.
        /// </summary>
        public Dictionary<char, List<Point>> GetSpecialPositions()
        {
            return levelDefinition.SpecialPositions;
        }
    }
    
    /// <summary>
    /// Level definition that loads from the Generated folder and parses metadata.
    /// </summary>
    public class GeneratedLevelDefinition : BaseLevelDefinition
    {
        private const string GENERATED_FOLDER = "src/Levels/Generated";
        private string[] _blueprint;
        private string _levelName;
        private Point? _startPosition;
        private Point? _finishPosition;
        private Dictionary<char, List<Point>>? _specialPositions;
        
        public GeneratedLevelDefinition(string fileName)
        {
            _levelName = Path.GetFileNameWithoutExtension(fileName);
            string filePath = GetLevelFilePath(fileName);
            LoadAndParseFile(filePath);
        }
        
        protected string GetLevelFilePath(string fileName)
        {
            return System.IO.Path.Combine(GENERATED_FOLDER, fileName);
        }
        
        private void LoadAndParseFile(string filePath)
        {
            try
            {
                string[] allLines = Definition.LoadLevelFromPath(filePath);
                ParseGeneratedLevel(allLines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading generated level {filePath}: {ex.Message}");
                _blueprint = new string[0];
            }
        }
        
        private void ParseGeneratedLevel(string[] allLines)
        {
            var blueprintLines = new List<string>();
            var trapLayerLines = new List<string>();
            bool inTrapLayer = false;
            bool inMetadata = false;
            
            foreach (string line in allLines)
            {
                string trimmedLine = line.Trim();
                
                // Skip comments and empty lines for blueprint
                if (trimmedLine.StartsWith("#") || string.IsNullOrEmpty(trimmedLine))
                    continue;
                
                // Check for trap layer start
                if (trimmedLine == "# Trap Layer")
                {
                    inTrapLayer = true;
                    continue;
                }
                
                // Check for metadata (start, finish, etc.)
                if (trimmedLine.StartsWith("start :") || trimmedLine.StartsWith("finish :") || 
                    trimmedLine.StartsWith("Possible"))
                {
                    inMetadata = true;
                    // Skip metadata parsing for now, we'll use blueprint parsing instead
                    continue;
                }
                
                // Skip other metadata lines
                if (inMetadata)
                    continue;
                
                // Add to appropriate section
                if (inTrapLayer)
                {
                    trapLayerLines.Add(line);
                }
                else
                {
                    blueprintLines.Add(line);
                }
            }
            
            _blueprint = blueprintLines.ToArray();
            _specialPositions = new Dictionary<char, List<Point>>();
            
            // Always use blueprint parsing for start/finish positions to ensure accuracy
            ParseSpecialPositions();
        }
        
        private void ParseMetadataLine(string line)
        {
            if (line.StartsWith("start :"))
            {
                string coords = line.Substring("start :".Length).Trim();
                string[] parts = coords.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    _startPosition = new Point(x, y);
                }
            }
            else if (line.StartsWith("finish :"))
            {
                string coords = line.Substring("finish :".Length).Trim();
                string[] parts = coords.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    _finishPosition = new Point(x, y);
                }
            }
        }
        
        private void ParseSpecialPositions()
        {
            // Find start and finish positions from the blueprint
            _startPosition = Definition.FindMarkerCenter(Blueprint, Definition.START_CHAR);
            _finishPosition = Definition.FindMarkerCenter(Blueprint, Definition.FINISH_CHAR);
            
            // Debug output
            Console.WriteLine($"Blueprint has {Blueprint.Length} rows");
            Console.WriteLine($"Found start at grid: ({_startPosition?.X}, {_startPosition?.Y})");
            Console.WriteLine($"Found finish at grid: ({_finishPosition?.X}, {_finishPosition?.Y})");
            
            // Find other special positions
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
        
        public override string[] Blueprint => _blueprint;
        public override string LevelName => _levelName;
    }
}

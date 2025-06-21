# Clawbyrinth Level System

## Overview

The Clawbyrinth game now uses a modular, file-based level system that allows for easy creation and modification of levels without touching the code. This system is based on the `Definition.cs` file which centralizes all game constants and level parsing logic.

## Key Features

### 1. **Centralized Definitions**
All game constants are now defined in `Definition.cs`:
- Grid sizes and scaling factors
- Collision detection settings
- Character mappings for level blueprints
- Asset paths
- Utility methods for coordinate conversion

### 2. **File-Based Level Templates**
Levels are stored as simple text files in `src/Levels/Map Templates/`:
- Easy to edit with any text editor
- Visual representation of the level layout
- Support for scaled sprites (S and F markers)
- Automatic parsing and validation

### 3. **Character Mapping**
- `.` = Empty space (walkable)
- `#` = Wall
- `S` = Start position (appears as 4-character block due to scaling)
- `F` = Finish position (appears as 4-character block due to scaling)
- `P` = Portal (future feature)
- `^` = Spikes trap (future feature)
- `C` = Cannon trap (future feature)

## Usage

### Creating a New Level

1. **Using LevelCreator utility:**
```csharp
// Create an empty 32x20 level with border walls
LevelCreator.CreateEmptyLevel("Level3.txt", 32, 20, true);
```

2. **Manual creation:**
Create a new `.txt` file in `src/Levels/Map Templates/` and design your level using the character mapping above.

### Loading a Level

```csharp
// Method 1: Using the factory
Level level = LevelFactory.CreateLevel("1", windowWidth, windowHeight);

// Method 2: Using GenericLevel directly
Level level = new GenericLevel(windowWidth, windowHeight, "Level2.txt");

// Method 3: Using specific level class
Level level = new Level1(windowWidth, windowHeight);
```

### Validating a Level

```csharp
List<string> issues = LevelCreator.ValidateLevel("Level1.txt");
foreach (string issue in issues)
{
    Console.WriteLine(issue);
}
```

## Level Template Format

### Example Level Template (`Level1.txt`):
```
################################
#..............................#
#..######################......#
#..#..................#......#
#..#....FFFF..........#......#
#..#....FFFF..........#......#
#..#..................#......#
#..#..................#......#
#..######################......#
#..............................#
#..............................#
#......######################..#
#......#..................#..#
#......#..........SSSS....#..#
#......#..........SSSS....#..#
#......#..................#..#
#......#..................#..#
#......######################..#
#..............................#
################################
```

### Important Notes:
- Each line must be the same length (rectangular grid)
- Start (S) and Finish (F) positions appear as 4-character blocks (2x2) due to sprite scaling
- The system automatically finds the center of these blocks for positioning
- Walls (#) create solid collision boundaries
- Empty spaces (.) are walkable areas

## Class Structure

### Core Classes:
- **`Definition`**: Static class containing all game constants and utilities
- **`ILevelDefinition`**: Interface for level definitions
- **`BaseLevelDefinition`**: Base class with common parsing logic
- **`FileLevelDefinition`**: Loads blueprints from external files
- **`GenericLevel`**: Generic level implementation for any template file
- **`LevelFactory`**: Factory for creating level instances
- **`LevelCreator`**: Utility for creating and validating levels

### Integration:
- **`Level`**: Base level class updated to use Definition constants
- **`Player`**: Updated to use Definition constants for movement and collision
- **`GameEngine`**: Uses LevelFactory to load levels

## Migration from Old System

If you have existing levels with space characters for empty areas, use the conversion utility:

```csharp
string[] oldBlueprint = { "  #  ", " # # ", "  #  " };
string[] newBlueprint = LevelCreator.ConvertOldBlueprint(oldBlueprint);
LevelCreator.SaveLevel("ConvertedLevel.txt", newBlueprint);
```

## Best Practices

1. **Always validate levels** after creation using `LevelCreator.ValidateLevel()`
2. **Use consistent dimensions** for similar level types
3. **Test thoroughly** - ensure start and finish positions are reachable
4. **Comment your designs** by creating separate documentation files
5. **Back up templates** before making major changes

## Future Extensions

The system is designed to easily support:
- **Animated tiles** (portals, traps)
- **Multi-level campaigns** with progression
- **Level editor GUI** built on top of this system
- **Procedural generation** using the same template format
- **Custom game modes** with different objectives

## File Structure
```
src/
├── Levels/
│   ├── Definition.cs          # Core definitions and utilities
│   ├── Level1.cs              # Specific Level 1 implementation
│   ├── GenericLevel.cs        # Generic level loader
│   ├── LevelCreator.cs        # Level creation utilities
│   └── Map Templates/
│       ├── Level1.txt         # Level 1 blueprint
│       ├── Level2.txt         # Level 2 blueprint
│       └── ...                # Additional level templates
```

This modular system makes the game much more maintainable and allows for rapid level creation and iteration!

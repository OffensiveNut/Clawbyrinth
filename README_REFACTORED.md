# Clawbyrinth - Refactored OO Edition

## Overview

This is a complete refactoring of the Clawbyrinth game that demonstrates the 5 core Object-Oriented Programming principles:

1. **Interfaces** - Contract-based design
2. **Abstract Classes** - Common base functionality
3. **Polymorphism** - Same interface, different implementations  
4. **Encapsulation** - Data hiding and controlled access
5. **Class and Object** - Proper entity modeling

The refactored code maintains all original gameplay functionality including procedural level generation while significantly improving code organization, maintainability, and extensibility.

## Architecture Overview

### Core Interfaces (`src/Core/`)

- **`IGameEntity`** - Base interface for all game entities
- **`IUpdatable`** - Interface for entities that need update logic
- **`IRenderable`** - Interface for entities that can be rendered
- **`ICollidable`** - Interface for entities with collision detection
- **`ILevelDefinition`** - Interface for level data sources

### Abstract Base Classes (`src/Entities/Abstract/`)

- **`BaseGameEntity`** - Common functionality for all game entities
- **`BaseTrap`** - Common trap behavior and state management
- **`BaseCollectible`** - Common collectible behavior
- **`BaseMapTransformer`** - Template method pattern for map transformations

### Concrete Entity Classes

#### Characters (`src/Entities/Characters/`)
- **`RefactoredPlayer`** - Player entity with movement, input, and collision

#### Traps (`src/Entities/Traps/`)
- **`RefactoredSpike2Trap`** - Animated spike trap with timing states
- **`SimpleSpikeTracl`** - Basic static spike trap

#### Interactive (`src/Entities/Interactive/`)
- **`Portal`** - Teleportation portals with linking

#### Collectibles (`src/Entities/Collectibles/`)
- **`Dot`** - Basic collectible item
- **`Coin`** - Valuable collectible item

### Management System (`src/Management/`)

- **`EntityManager`** - Manages all entities using composition
- **`RefactoredLevelManager`** - Handles level progression and generation

### Procedural Generation (`src/ProceduralGeneration/`)

#### Interfaces
- **`IMapGenerator`** - Strategy pattern for map generation
- **`IMapData`** - Interface for map data abstraction

#### Implementations  
- **`RefactoredProceduralMapGenerator`** - Main procedural generation engine
- **`RefactoredMapData`** - Map data with encapsulated parsing

#### Transformers (Strategy Pattern)
- **`RotationTransformer`** - 90°, 180°, 270° rotations
- **`MirrorTransformer`** - Horizontal and vertical mirroring

### Factory Pattern (`src/Factories/`)

- **`EntityFactory`** - Creates entities based on character types

### Level System (`src/Levels/`)

- **`RefactoredLevel`** - Main level class using composition
- **`RefactoredLevelManager`** - Level progression management
- **`RefactoredGeneratedLevelDefinition`** - Procedural level loading

## OO Principles Demonstrated

### 1. Interfaces
```csharp
// Contract-based design allows polymorphic usage
IGameEntity player = new RefactoredPlayer(x, y);
IGameEntity trap = new RefactoredSpike2Trap(position, orientation);

// All entities can be updated through the same interface
entityManager.UpdateAll(deltaTime); // Calls Update() on each entity
```

### 2. Abstract Classes
```csharp
// Common behavior shared between trap types
public abstract class BaseTrap : BaseGameEntity, IUpdatable
{
    // Common trap properties and methods
    public abstract void Activate();
    protected virtual bool ValidateActivation() { ... }
}
```

### 3. Polymorphism
```csharp
// Different trap types behave differently when activated
BaseTrap spikeTrap = new RefactoredSpike2Trap(...);
BaseTrap simpleTrap = new SimpleSpikeTrap(...);

spikeTrap.Activate(); // Complex animation sequence
simpleTrap.Activate(); // Simple state change
```

### 4. Encapsulation
```csharp
// Private fields with controlled public access
public class RefactoredPlayer : BaseGameEntity
{
    private float _moveSpeed;           // Hidden implementation
    private PlayerState _currentState;  // Internal state
    
    public float MoveSpeed => _moveSpeed;  // Read-only access
    public void SetMoveSpeed(float speed)  // Validated setter
    {
        _moveSpeed = Math.Max(0, speed);
    }
}
```

### 5. Class and Object
```csharp
// Proper entity modeling with clear responsibilities
public class RefactoredLevel
{
    private readonly EntityManager _entityManager;  // Composition
    private readonly IMapGenerator _mapGenerator;   // Dependency injection
    
    public void Update(float deltaTime, IGameEntity player)
    {
        _entityManager.UpdateAll(deltaTime);        // Delegate to composed object
        HandlePlayerInteraction(player);           // Encapsulated method
    }
}
```

## Key Improvements

### Code Organization
- **Separation of Concerns**: Each class has a single, well-defined responsibility
- **Namespace Organization**: Logical grouping by functionality
- **Interface Segregation**: Small, focused interfaces rather than large monolithic ones

### Maintainability
- **DRY Principle**: Common code extracted to base classes
- **Open/Closed Principle**: Easy to extend without modifying existing code
- **Dependency Injection**: Loose coupling between components

### Extensibility
- **Plugin Architecture**: New entity types can be added by implementing interfaces
- **Strategy Pattern**: Map transformations are easily extensible
- **Factory Pattern**: Entity creation is centralized and configurable

### Performance
- **Object Pooling Ready**: Architecture supports efficient memory management
- **Polymorphic Collections**: Efficient iteration over mixed entity types
- **Event-Driven Updates**: Only active entities are updated

## Procedural Generation

The refactored system maintains full compatibility with the original C# procedural generation while adding:

- **Interface-based design** for map generators
- **Strategy pattern** for map transformations
- **Composition** over inheritance for map combination
- **Encapsulated parsing** with error handling

### Usage Example
```csharp
// Create generator with dependency injection
IMapGenerator generator = new RefactoredProceduralMapGenerator(templatesPath);

// Generate map polymorphically
IMapData mapData = generator.GenerateMap("medium", 2, 4);

// Create variations using transformers
var rotationTransformer = new RotationTransformer(90);
var mirrorTransformer = new MirrorTransformer(MirrorDirection.Horizontal);

IMapData rotatedMap = rotationTransformer.TransformMap(mapData);
IMapData mirroredMap = mirrorTransformer.TransformMap(mapData);
```

## Building and Running

### Prerequisites
- .NET 9.0 SDK
- Wine (for Linux builds)

### Build Commands
```bash
# Build the project
cd /home/rasya/Documents/PBO/Clawbyrinth/src
wd build

# Run the project  
wd run

# Or run with arguments
wd run -- --templates "custom/templates/" --debug
```

### Command Line Options
- `--templates <path>` - Specify map templates directory
- `--generated <path>` - Specify generated levels directory  
- `--demo` - Run in demonstration mode
- `--debug` - Enable debug output
- `--help` - Show help information

## Game Features Preserved

All original gameplay features are maintained:

- **Procedural Level Generation**: Maps are generated from templates
- **Entity System**: Players, traps, collectibles, portals
- **Collision Detection**: Wall and entity collision
- **Level Progression**: Advance through multiple generated levels
- **Score System**: Points for collectibles and level completion
- **Animation System**: Animated traps and entities

## Testing the OO Design

The refactored code includes demonstration of all OO principles:

1. **Run the game** to see polymorphism in action with entity updates and rendering
2. **Check console output** to see the procedural generation system working
3. **Examine the code** to see proper encapsulation and interface usage
4. **Add new entities** to test the extensibility of the factory pattern
5. **Create new transformers** to test the strategy pattern implementation

## Architecture Benefits

- **Testability**: Each component can be unit tested in isolation
- **Modularity**: Components can be reused in other projects
- **Scalability**: Easy to add new features without breaking existing code
- **Readability**: Clear separation of concerns and well-defined interfaces
- **Maintainability**: Changes are localized and don't ripple through the codebase

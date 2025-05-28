using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace Clawbyrinth
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public enum AnimationType
    {
        Idle,
        LongJump,
        Flight,
        Arrive
    }

    public class Player
    {
        // Constants
        private const int GRID_SIZE = 24;
        private const float MOVE_SPEED = 1000.0f; // pixels per second
        private const int SPRITE_SIZE = 14; // Actual sprite size
        private const int COLLISION_SIZE = 12; // Collision detection size
        
        // Position and movement
        private float animatedX, animatedY;
        private float targetX, targetY;
        private int gridX, gridY;
        private bool isMoving;
        private Direction currentDirection;
        private Direction lastWallDirection;
        
        // Animation
        private float rotation;
        private float targetRotation;
        private DateTime lastUpdateTime;
        private PlayerState currentState;
        private float animationTimer;
        
        // Sprite animation
        private AnimationType currentAnimation;
        private int currentFrame;
        private float frameTimer;
        private Dictionary<AnimationType, Image> spriteSheets = null!;
        private Dictionary<AnimationType, int> framecounts = null!;
        private Dictionary<AnimationType, float> frameRates = null!;
        private bool animationFinished;
        
        // Visual
        private float scale = 1.0f;

        // Public properties
        public PointF Position => new PointF(animatedX + GRID_SIZE / 2, animatedY + GRID_SIZE / 2);
        public bool IsMoving => isMoving;

        public enum PlayerState
        {
            Idle,
            Moving,
            AgainstWall
        }

        public Player(int startX, int startY)
        {
            gridX = startX;
            gridY = startY;
            animatedX = gridX * GRID_SIZE;
            animatedY = gridY * GRID_SIZE;
            targetX = animatedX;
            targetY = animatedY;
            
            isMoving = false;
            currentDirection = Direction.None;
            lastWallDirection = Direction.None;
            currentState = PlayerState.Idle;
            
            lastUpdateTime = DateTime.Now;
            
            // Initialize sprite animation
            InitializeSpriteSheets();
            currentAnimation = AnimationType.Idle;
            currentFrame = 0;
            frameTimer = 0;
            animationFinished = false;
        }

        private void InitializeSpriteSheets()
        {
            spriteSheets = new Dictionary<AnimationType, Image>();
            framecounts = new Dictionary<AnimationType, int>();
            frameRates = new Dictionary<AnimationType, float>();
            
            try
            {
                // Load sprite sheets
                spriteSheets[AnimationType.Idle] = Image.FromFile("Assets/Player/koceng_idle2.png");
                spriteSheets[AnimationType.LongJump] = Image.FromFile("Assets/Player/koceng_long_jump.png");
                spriteSheets[AnimationType.Flight] = Image.FromFile("Assets/Player/koceng_flight.png");
                spriteSheets[AnimationType.Arrive] = Image.FromFile("Assets/Player/koceng_arrive.png");
                
                // Set frame counts
                framecounts[AnimationType.Idle] = 6;
                framecounts[AnimationType.LongJump] = 5;
                framecounts[AnimationType.Flight] = 3;
                framecounts[AnimationType.Arrive] = 2;
                
                // Set frame rates (fps)
                frameRates[AnimationType.Idle] = 12.0f;        // Slower idle animation
                frameRates[AnimationType.LongJump] = 48.0f;    // Fast jump animation
                frameRates[AnimationType.Flight] = 48.0f;      // Fast flight animation
                frameRates[AnimationType.Arrive] = 48.0f;      // Fast arrive animation
            }
            catch (Exception ex)
            {
                // If sprites can't be loaded, we'll fall back to simple rendering
                System.Diagnostics.Debug.WriteLine($"Failed to load sprites: {ex.Message}");
            }
        }

        public void Update(Level level, float deltaTime)
        {
            animationTimer += deltaTime;
            
            // Update sprite animation
            UpdateSpriteAnimation(deltaTime);
            
            if (isMoving)
            {
                UpdateMovement(deltaTime, level);
            }
            
            // Update rotation smoothly
            UpdateRotation();
        }

        private void UpdateSpriteAnimation(float deltaTime)
        {
            frameTimer += deltaTime;
            
            // Get the frame rate for the current animation
            float currentFrameRate = frameRates.ContainsKey(currentAnimation) ? frameRates[currentAnimation] : 10.0f;
            
            if (frameTimer >= 1.0f / currentFrameRate)
            {
                frameTimer = 0;
                
                if (framecounts.ContainsKey(currentAnimation))
                {
                    int maxFrames = framecounts[currentAnimation];
                    
                    // Handle different animation behaviors
                    if (currentAnimation == AnimationType.Idle)
                    {
                        // Loop idle animation
                        currentFrame = (currentFrame + 1) % maxFrames;
                    }
                    else if (currentAnimation == AnimationType.LongJump)
                    {
                        if (currentFrame < maxFrames - 1)
                        {
                            currentFrame++;
                        }
                        else
                        {
                            // Long jump finished, switch to flight if still moving
                            if (isMoving)
                            {
                                PlayAnimation(AnimationType.Flight);
                            }
                            else
                            {
                                PlayAnimation(AnimationType.Idle);
                            }
                        }
                    }
                    else if (currentAnimation == AnimationType.Flight)
                    {
                        if (currentFrame < maxFrames - 1)
                        {
                            currentFrame++;
                        }
                        else
                        {
                            // Stay on last frame of flight while moving
                            if (isMoving)
                            {
                                // Keep last frame
                                currentFrame = maxFrames - 1;
                            }
                            else
                            {
                                PlayAnimation(AnimationType.Idle);
                            }
                        }
                    }
                    else if (currentAnimation == AnimationType.Arrive)
                    {
                        if (currentFrame < maxFrames - 1)
                        {
                            currentFrame++;
                        }
                        else
                        {
                            // Arrive animation finished
                            PlayAnimation(AnimationType.Idle);
                        }
                    }
                }
            }
        }

        private void PlayAnimation(AnimationType animation)
        {
            if (currentAnimation != animation)
            {
                currentAnimation = animation;
                currentFrame = 0;
                frameTimer = 0;
                animationFinished = false;
            }
        }

        private void UpdateMovement(float deltaTime, Level level)
        {
            // Calculate movement distance this frame
            float moveDistance = MOVE_SPEED * deltaTime;
            
            // Move towards target
            float dx = targetX - animatedX;
            float dy = targetY - animatedY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            
            if (distance <= moveDistance)
            {
                // Reached target
                animatedX = targetX;
                animatedY = targetY;
                gridX = (int)(targetX / GRID_SIZE);
                gridY = (int)(targetY / GRID_SIZE);
                
                // Check if we can continue moving in the same direction
                if (CanMoveInDirection(currentDirection, level))
                {
                    ContinueMoving();
                }
                else
                {
                    // Hit a wall, stop moving
                    StopMoving();
                    lastWallDirection = currentDirection;
                    SetAgainstWallRotation(currentDirection);
                    currentState = PlayerState.AgainstWall;
                    PlayAnimation(AnimationType.Idle);
                }
            }
            else
            {
                // Continue moving towards target
                float moveX = (dx / distance) * moveDistance;
                float moveY = (dy / distance) * moveDistance;
                animatedX += moveX;
                animatedY += moveY;
            }
        }

        private void UpdateRotation()
        {
            rotation = targetRotation;
            
            // Keep rotation in 0-360 range
            while (rotation < 0) rotation += 360;
            while (rotation >= 360) rotation -= 360;
        }

        public void StartMoving(Direction direction, Level level)
        {
            if (isMoving) return; // Already moving
            
            if (!CanMoveInDirection(direction, level))
            {
                // Can't move in this direction, just rotate
                currentDirection = direction;
                lastWallDirection = direction;
                SetAgainstWallRotation(direction);
                currentState = PlayerState.AgainstWall;
                PlayAnimation(AnimationType.Idle);
                return;
            }
            
            // Start moving
            isMoving = true;
            currentDirection = direction;
            currentState = PlayerState.Moving;
            
            SetTargetPosition(direction);
            SetMovementRotation(direction);
            
            // Play appropriate animation based on movement context
            PlayMovementAnimation(direction);
            
        }

        private void PlayMovementAnimation(Direction direction)
        {
            // Determine animation based on movement type
            if (IsMovingAwayFromWall(direction) || IsMovingAlongsideWall(direction))
            {
                // Moving away from wall or alongside wall - play long jump animation
                PlayAnimation(AnimationType.LongJump);
            }
            else
            {
                // Default to long jump for any other movement
                PlayAnimation(AnimationType.LongJump);
            }
        }

        private bool IsMovingAwayFromWall(Direction direction)
        {
            // Check if we're moving in the opposite direction of the last wall we hit
            return lastWallDirection != Direction.None && direction == GetOppositeDirection(lastWallDirection);
        }

        private bool IsMovingAlongsideWall(Direction direction)
        {
            // Check if we're moving perpendicular to the last wall we hit
            if (lastWallDirection == Direction.None) return false;
            
            // If last wall was vertical (left/right), moving up/down is alongside
            if (lastWallDirection == Direction.Left || lastWallDirection == Direction.Right)
            {
                return direction == Direction.Up || direction == Direction.Down;
            }
            // If last wall was horizontal (up/down), moving left/right is alongside
            else if (lastWallDirection == Direction.Up || lastWallDirection == Direction.Down)
            {
                return direction == Direction.Left || direction == Direction.Right;
            }
            
            return false;
        }

        private Direction GetOppositeDirection(Direction direction)
        {
            return direction switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => Direction.None
            };
        }

        private void ContinueMoving()
        {
            SetTargetPosition(currentDirection);
        }

        private void StopMoving()
        {
            isMoving = false;
            currentState = PlayerState.AgainstWall;
        }

        private void SetTargetPosition(Direction direction)
        {
            int newGridX = gridX;
            int newGridY = gridY;
            
            switch (direction)
            {
                case Direction.Up:
                    newGridY--;
                    break;
                case Direction.Down:
                    newGridY++;
                    break;
                case Direction.Left:
                    newGridX--;
                    break;
                case Direction.Right:
                    newGridX++;
                    break;
            }
            
            targetX = newGridX * GRID_SIZE;
            targetY = newGridY * GRID_SIZE;
        }

        private bool CanMoveInDirection(Direction direction, Level level)
        {
            int newGridX = gridX;
            int newGridY = gridY;
            
            switch (direction)
            {
                case Direction.Up:
                    newGridY--;
                    break;
                case Direction.Down:
                    newGridY++;
                    break;
                case Direction.Left:
                    newGridX--;
                    break;
                case Direction.Right:
                    newGridX++;
                    break;
            }
            
            return level.IsValidPosition(newGridX, newGridY);
        }

        private void SetMovementRotation(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    targetRotation = 0;
                    break;
                case Direction.Right:
                    targetRotation = 90;
                    break;
                case Direction.Down:
                    targetRotation = 180;
                    break;
                case Direction.Left:
                    targetRotation = 270;
                    break;
            }
        }

        private void SetAgainstWallRotation(Direction wallDirection)
        {
            switch (wallDirection)
            {
                case Direction.Up:
                    targetRotation = 180; // Feet up (head down)
                    break;
                case Direction.Right:
                    targetRotation = 270; // Feet to the right
                    break;
                case Direction.Down:
                    targetRotation = 0; // Feet down
                    break;
                case Direction.Left:
                    targetRotation = 90; // Feet to the left
                    break;
            }
        }

        public void Render(Graphics g)
        {
            // Update scale animation
            if (scale > 1.0f)
            {
                scale = Math.Max(1.0f, scale - 2.0f * (float)(DateTime.Now - lastUpdateTime).TotalSeconds);
            }
            
            // Save graphics state
            GraphicsState state = g.Save();
            
            // Calculate render position (center the sprite)
            float renderX = animatedX + GRID_SIZE / 2;
            float renderY = animatedY + GRID_SIZE / 2;
            
            // Apply transformations
            g.TranslateTransform(renderX, renderY);
            g.RotateTransform(rotation);
            g.ScaleTransform(scale, scale);
            
            DrawSprite(g);

            
            g.Restore(state);
        }

        private void DrawSprite(Graphics g)
        {
            if (!spriteSheets.ContainsKey(currentAnimation)) return;
            
            Image spriteSheet = spriteSheets[currentAnimation];
            int frameCount = framecounts.ContainsKey(currentAnimation) ? framecounts[currentAnimation] : 1;
            
            // Calculate source rectangle for current frame
            int frameWidth = spriteSheet.Width / frameCount;
            int frameHeight = spriteSheet.Height;
            
            Rectangle sourceRect = new Rectangle(
                currentFrame * frameWidth,
                0,
                frameWidth,
                frameHeight
            );
            
            // Calculate destination rectangle (centered)
            float spriteScale = (float)GRID_SIZE / SPRITE_SIZE;
            int scaledWidth = (int)(frameWidth * spriteScale);
            int scaledHeight = (int)(frameHeight * spriteScale);
            
            Rectangle destRect = new Rectangle(
                -scaledWidth / 2,
                -scaledHeight / 2,
                scaledWidth,
                scaledHeight
            );
            
            // Draw the sprite frame
            g.DrawImage(spriteSheet, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        // Dispose method to clean up resources
        public void Dispose()
        {
            if (spriteSheets != null)
            {
                foreach (var sprite in spriteSheets.Values)
                {
                    sprite?.Dispose();
                }
                spriteSheets.Clear();
            }
        }
    }
}
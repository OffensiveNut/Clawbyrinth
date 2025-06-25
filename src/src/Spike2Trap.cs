using System;
using System.Drawing;
using Clawbyrinth.Levels;

namespace Clawbyrinth
{
    /// <summary>
    /// Represents a Spike 2 trap that activates when player approaches.
    /// The spike activates with a 1-second delay and shows an attack animation.
    /// </summary>
    public class Spike2Trap
    {
        public Point GridPosition { get; private set; }
        public int WallOrientation { get; private set; } // 2=down, 4=right, 6=left, 8=up, 10=up-left, 12=up-right, 14=down-left, 16=down-right
        public Spike2State State { get; private set; }
        public DateTime StateChangeTime { get; private set; }
        public List<int> AttackDirections { get; private set; } // Directions the spike attacks towards
        
        // Animation timing constants
        private const double ACTIVATION_DELAY = 1.0; // 1 second delay before attack
        private const double FRAME_DURATION = 0.1;   // 0.1 seconds per frame
        private const double HOLD_DURATION = 1.0;    // Hold middle frame for 1 second
        
        public Spike2Trap(Point gridPosition, int wallOrientation)
        {
            GridPosition = gridPosition;
            WallOrientation = wallOrientation;
            State = Spike2State.Idle;
            StateChangeTime = DateTime.Now;
            
            // Determine attack directions based on wall orientation
            AttackDirections = new List<int>();
            switch (wallOrientation)
            {
                case 2: // Down
                    AttackDirections.Add(2);
                    break;
                case 4: // Right
                    AttackDirections.Add(4);
                    break;
                case 6: // Left
                    AttackDirections.Add(6);
                    break;
                case 8: // Up
                    AttackDirections.Add(8);
                    break;
                case 10: // Up-Left corner
                    AttackDirections.Add(8); // Up
                    AttackDirections.Add(6); // Left
                    break;
                case 12: // Up-Right corner
                    AttackDirections.Add(8); // Up
                    AttackDirections.Add(4); // Right
                    break;
                case 14: // Down-Left corner
                    AttackDirections.Add(2); // Down
                    AttackDirections.Add(6); // Left
                    break;
                case 16: // Down-Right corner
                    AttackDirections.Add(2); // Down
                    AttackDirections.Add(4); // Right
                    break;
                default:
                    AttackDirections.Add(wallOrientation); // Fallback
                    break;
            }
        }
        
        /// <summary>
        /// Updates the spike trap state and handles transitions.
        /// </summary>
        public void Update()
        {
            double timeSinceStateChange = (DateTime.Now - StateChangeTime).TotalSeconds;
            
            switch (State)
            {
                case Spike2State.Activating:
                    if (timeSinceStateChange >= ACTIVATION_DELAY)
                    {
                        ChangeState(Spike2State.AttackingOut);
                    }
                    break;
                    
                case Spike2State.AttackingOut:
                    if (timeSinceStateChange >= FRAME_DURATION * 2) // Show first 2 frames
                    {
                        ChangeState(Spike2State.AttackingHold);
                    }
                    break;
                    
                case Spike2State.AttackingHold:
                    if (timeSinceStateChange >= HOLD_DURATION)
                    {
                        ChangeState(Spike2State.AttackingIn);
                    }
                    break;
                    
                case Spike2State.AttackingIn:
                    if (timeSinceStateChange >= FRAME_DURATION * 2) // Show last 2 frames
                    {
                        ChangeState(Spike2State.Idle);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Activates the spike trap if it's currently idle.
        /// </summary>
        public void Activate()
        {
            if (State == Spike2State.Idle)
            {
                ChangeState(Spike2State.Activating);
            }
        }
        
        /// <summary>
        /// Returns true if the spike is currently in a deadly state.
        /// </summary>
        public bool IsDeadly()
        {
            return State == Spike2State.AttackingOut || 
                   State == Spike2State.AttackingHold || 
                   State == Spike2State.AttackingIn;
        }
        
        /// <summary>
        /// Gets the current animation frame index for rendering.
        /// </summary>
        public int GetCurrentFrame()
        {
            double timeSinceStateChange = (DateTime.Now - StateChangeTime).TotalSeconds;
            
            switch (State)
            {
                case Spike2State.AttackingOut:
                    // Show frames 0, 1
                    return Math.Min(1, (int)(timeSinceStateChange / FRAME_DURATION));
                    
                case Spike2State.AttackingHold:
                    // Show frame 2 (middle frame)
                    return 2;
                    
                case Spike2State.AttackingIn:
                    // Show frames 1, 0 (reverse)
                    int reverseFrame = (int)(timeSinceStateChange / FRAME_DURATION);
                    return Math.Max(0, 1 - reverseFrame);
                    
                default:
                    return -1; // No attack animation
            }
        }
        
        /// <summary>
        /// Gets the pixel positions where the attack spikes should be rendered.
        /// </summary>
        public List<Point> GetAttackPositions()
        {
            List<Point> attackPositions = new List<Point>();
            int baseX = Definition.GridToPixel(GridPosition.X);
            int baseY = Definition.GridToPixel(GridPosition.Y);
            
            foreach (int direction in AttackDirections)
            {
                // Offset the attack position based on direction
                Point attackPos = direction switch
                {
                    2 => new Point(baseX, baseY + Definition.GRID_SIZE), // Down
                    4 => new Point(baseX + Definition.GRID_SIZE, baseY), // Right
                    6 => new Point(baseX - Definition.GRID_SIZE, baseY), // Left
                    8 => new Point(baseX, baseY - Definition.GRID_SIZE), // Up
                    _ => new Point(baseX, baseY) // Fallback
                };
                attackPositions.Add(attackPos);
            }
            
            return attackPositions;
        }
        
        /// <summary>
        /// Gets the collision rectangles for all attack spikes.
        /// </summary>
        public List<Rectangle> GetAttackCollisionRects()
        {
            List<Rectangle> collisionRects = new List<Rectangle>();
            List<Point> attackPositions = GetAttackPositions();
            
            foreach (Point attackPos in attackPositions)
            {
                collisionRects.Add(new Rectangle(attackPos.X, attackPos.Y, Definition.GRID_SIZE, Definition.GRID_SIZE));
            }
            
            return collisionRects;
        }
        
        private void ChangeState(Spike2State newState)
        {
            State = newState;
            StateChangeTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// States for the Spike 2 trap animation sequence.
    /// </summary>
    public enum Spike2State
    {
        Idle,           // Waiting for player to approach
        Activating,     // 1-second delay before attack
        AttackingOut,   // Showing first 2 frames of attack
        AttackingHold,  // Holding middle frame for 1 second
        AttackingIn     // Showing last 2 frames (reverse)
    }
}

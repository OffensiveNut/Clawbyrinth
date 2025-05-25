using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{

	[Export]
	public int Speed = 400;

	// Grid system constants
	private const int GRID_SIZE = 12;
	private const int GRID_OFFSET = 6; // Half of grid size for centering

	private Dictionary<Vector2, int> _sprites = new()
	{
		{ Vector2.Right, 1 },
		{ Vector2.Left, 2 },
		{ Vector2.Up, 3 },
		{ Vector2.Down, 4 }
	};

	private AnimatedSprite2D _animatedSprite;
	private CollisionShape2D _collisionShape;
	private Vector2 _currentDirection = Vector2.Zero;
	private bool _isMoving = false;
	private Vector2 _lastWallDirection = Vector2.Zero; // Track which wall we last hit
	
	// Smooth animation variables
	private Vector2 _animatedPosition; // The position that gets smoothly animated
	private Vector2 _targetPosition; // Where we're moving to
	private float _moveSpeed = 300.0f; // Speed of smooth movement
	
	// Directional raycasts for collision detection
	private RayCast2D _raycastUp;
	private RayCast2D _raycastDown;
	private RayCast2D _raycastLeft;
	private RayCast2D _raycastRight;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		
		// Initialize animated position to current position
		_animatedPosition = GlobalPosition;
		_targetPosition = GlobalPosition;
		
		// Setup directional raycasts for movement collision detection
		SetupDirectionalRaycasts();
		
		// Connect animation finished signal
		if (_animatedSprite != null)
		{
			_animatedSprite.AnimationFinished += OnAnimationFinished;
			// Start with idle animation
			_animatedSprite.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isMoving)
		{
			// Smoothly animate the position towards the target
			_animatedPosition = _animatedPosition.MoveToward(_targetPosition, _moveSpeed * (float)delta);
			
			// Set the actual position to a pixel-snapped value of the animated position
			GlobalPosition = new Vector2(
				Mathf.Round(_animatedPosition.X),
				Mathf.Round(_animatedPosition.Y)
			);
			
			// Check if we've reached the target position
			if (_animatedPosition.DistanceTo(_targetPosition) < 1.0f)
			{
				// Snap to exact target position
				_animatedPosition = _targetPosition;
				GlobalPosition = _targetPosition;
				
				// Check if the next move in the current direction would hit a wall
				if (IsDirectionBlocked(_currentDirection))
				{
					// Stop moving when path is blocked
					_isMoving = false;
					_lastWallDirection = _currentDirection; // Remember which wall we hit
					// Rotate sprite to show player against the wall
					RotateSpriteAgainstWall(_currentDirection);
					// Play idle animation after hitting wall
					if (_animatedSprite != null)
					{
						_animatedSprite.Play("idle");
					}
					GD.Print($"Path blocked in direction {_currentDirection}! Stopping movement.");
				}
				else
				{
					// Continue moving in the same direction
					ContinueMoving();
				}
			}
		}
		
		// Always keep velocity at zero since we're handling movement manually
		Velocity = Vector2.Zero;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Only allow new movement if we're not currently moving
		if (!_isMoving)
		{
			if (@event.IsActionPressed("right"))
				StartMoving(Vector2.Right);
			else if (@event.IsActionPressed("left"))
				StartMoving(Vector2.Left);
			else if (@event.IsActionPressed("down"))
				StartMoving(Vector2.Down);
			else if (@event.IsActionPressed("up"))
				StartMoving(Vector2.Up);
		}
	}

	private void StartMoving(Vector2 direction)
	{
		// Check if the direction is blocked before starting movement
		if (IsDirectionBlocked(direction))
		{
			// Direction is blocked, only rotate the player without moving
			_currentDirection = direction;
			_lastWallDirection = direction; // Remember which wall we're against
			RotateSpriteAgainstWall(direction);
			
			// Play idle animation since we're not moving
			if (_animatedSprite != null)
			{
				_animatedSprite.Play("idle");
			}
			
			GD.Print($"Direction {direction} is blocked! Only rotating player.");
			return;
		}
		
		// Direction is clear, start moving
		_currentDirection = direction;
		_isMoving = true;
		
		// Set the target position for smooth movement
		_targetPosition = GlobalPosition + direction * GRID_SIZE;
		
		// Rotate sprite to face movement direction
		RotateSpriteToMovementDirection(direction);
		
		UpdateSprite(direction);
		PlayMovementAnimation(direction);
	}

	private void ContinueMoving()
	{
		// Set the next target position for continuous movement
		_targetPosition = GlobalPosition + _currentDirection * GRID_SIZE;
		
		GD.Print($"Continuing movement in direction: {_currentDirection}");
	}

	private void UpdateSprite(Vector2 direction)
	{
		if (_animatedSprite != null && _sprites.TryGetValue(direction, out int frame))
		{
			_animatedSprite.Frame = frame;
		}
	}

	private void RotateSpriteAgainstWall(Vector2 direction)
	{
		if (_animatedSprite != null)
		{
			// Reset scale to avoid flipping issues
			// _animatedSprite.Scale = new Vector2(6.5f, 6.5f);
			
			// Rotate sprite based on wall direction
			if (direction == Vector2.Right) // Hit wall on right
			{
				_animatedSprite.RotationDegrees = -90; // Rotate clockwise, feet to the right
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = -90;
			}
			else if (direction == Vector2.Left) // Hit wall on left
			{
				_animatedSprite.RotationDegrees = 90; // Rotate counter-clockwise, feet to the left
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = 90;
			}
			else if (direction == Vector2.Down) // Hit wall below
			{
				_animatedSprite.RotationDegrees = 0; // Rotate upside down, feet down
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = 0;
			}
			else if (direction == Vector2.Up) // Hit wall above
			{
				_animatedSprite.RotationDegrees = -180; // Keep normal, feet up (head down)
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = -180;
			}
		}
	}

	private void RotateSpriteToMovementDirection(Vector2 direction)
	{
		if (_animatedSprite != null)
		{
			// Reset scale to avoid flipping issues
			// _animatedSprite.Scale = new Vector2(6.5f, 6.5f);
			
			// Rotate sprite to face movement direction
			if (direction == Vector2.Right) // Moving right
			{
				_animatedSprite.RotationDegrees = 90; // Face right
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = 90;
			}
			else if (direction == Vector2.Left) // Moving left
			{
				_animatedSprite.RotationDegrees = -90; // Face left
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = -90;
			}
			else if (direction == Vector2.Down) // Moving down
			{
				_animatedSprite.RotationDegrees = 180; // Face down
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = 180;
			}
			else if (direction == Vector2.Up) // Moving up
			{
				_animatedSprite.RotationDegrees = 0; // Face up (normal orientation)
				if (_collisionShape != null)
					_collisionShape.RotationDegrees = 0;
			}
		}
	}

	private void PlayMovementAnimation(Vector2 direction)
	{
		if (_animatedSprite == null) return;

		// Determine animation based on movement type
		if (IsMovingAwayFromWall(direction))
		{
			// Moving away from wall - play jump animation
			_animatedSprite.Play("jump");
		}
		else if (IsMovingAlongsideWall(direction))
		{
			// Moving alongside wall - play run animation
			_animatedSprite.Play("jump");
		}
		else
		{
			// Default to jump for any other movement
			_animatedSprite.Play("jump");
		}
	}

	private bool IsMovingAwayFromWall(Vector2 direction)
	{
		// Check if we're moving in the opposite direction of the last wall we hit
		return _lastWallDirection != Vector2.Zero && direction == -_lastWallDirection;
	}

	private bool IsMovingAlongsideWall(Vector2 direction)
	{
		// Check if we're moving perpendicular to the last wall we hit
		if (_lastWallDirection == Vector2.Zero) return false;
		
		// If last wall was vertical (left/right), moving up/down is alongside
		if (_lastWallDirection == Vector2.Left || _lastWallDirection == Vector2.Right)
		{
			return direction == Vector2.Up || direction == Vector2.Down;
		}
		// If last wall was horizontal (up/down), moving left/right is alongside
		else if (_lastWallDirection == Vector2.Up || _lastWallDirection == Vector2.Down)
		{
			return direction == Vector2.Left || direction == Vector2.Right;
		}
		
		return false;
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite == null) return;

		string currentAnimation = _animatedSprite.Animation;
		
		// Handle animation transitions
		if (currentAnimation == "jump" || currentAnimation == "run")
		{
			// After jump or run, play flight animation
			if (_isMoving)
			{
				_animatedSprite.Play("flight");
			}
			else
			{
				_animatedSprite.Play("idle");
			}
		}
		else if (currentAnimation == "flight")
		{
			// After flight finishes, stay on the last frame (frame 2, which is the third frame)
			if (_isMoving)
			{
				_animatedSprite.Stop();
				_animatedSprite.Frame = 2; // Stay on the third frame (0-indexed)
			}
			else
			{
				_animatedSprite.Play("idle");
			}
		}
		// idle animation can loop on its own
	}

	private void SetupDirectionalRaycasts()
	{
		// Create and setup raycast for upward movement
		_raycastUp = new RayCast2D();
		AddChild(_raycastUp);
		_raycastUp.Enabled = true;
		_raycastUp.TargetPosition = Vector2.Up * 13.0f; // Longer distance for upward detection
		
		// Create and setup raycast for downward movement
		_raycastDown = new RayCast2D();
		AddChild(_raycastDown);
		_raycastDown.Enabled = true;
		_raycastDown.TargetPosition = Vector2.Down * (GRID_SIZE * 0.8f);
		
		// Create and setup raycast for leftward movement
		_raycastLeft = new RayCast2D();
		AddChild(_raycastLeft);
		_raycastLeft.Enabled = true;
		_raycastLeft.TargetPosition = Vector2.Left * (GRID_SIZE * 0.8f);
		
		// Create and setup raycast for rightward movement
		_raycastRight = new RayCast2D();
		AddChild(_raycastRight);
		_raycastRight.Enabled = true;
		_raycastRight.TargetPosition = Vector2.Right * (GRID_SIZE * 0.8f);
	}

	private bool IsDirectionBlocked(Vector2 direction)
	{
		RayCast2D raycast = null;
		
		if (direction == Vector2.Up)
			raycast = _raycastUp;
		else if (direction == Vector2.Down)
			raycast = _raycastDown;
		else if (direction == Vector2.Left)
			raycast = _raycastLeft;
		else if (direction == Vector2.Right)
			raycast = _raycastRight;
		
		if (raycast != null)
		{
			// Force raycast update
			raycast.ForceRaycastUpdate();
			bool isBlocked = raycast.IsColliding();
			GD.Print($"Direction {direction} blocked: {isBlocked}");
			return isBlocked;
		}
		
		return false;
	}

}

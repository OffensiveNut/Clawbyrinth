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
	
	// Pixel movement constants
	private const float PIXELS_PER_SECOND = 200.0f; // How many pixels to move per second
	private const float PIXEL_MOVE_INTERVAL = 1.0f / PIXELS_PER_SECOND; // Time between each pixel movement

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
	
	// Pixel-by-pixel movement variables
	private float _pixelMoveTimer = 0.0f;
	private Vector2 _nextPixelPosition;
	
	// Raycast for collision detection
	private RayCast2D _movementRaycast;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		
		// Setup raycast for movement collision detection
		SetupMovementRaycast();
		
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
			// Move in the current direction at high speed
			Velocity = _currentDirection * Speed;
			MoveAndSlide();
			
			// Check if raycast is colliding with an object
			if (_movementRaycast != null && _movementRaycast.IsColliding())
			{
				// Stop moving immediately when raycast hits something
				_isMoving = false;
				Velocity = Vector2.Zero;
				_lastWallDirection = _currentDirection; // Remember which wall we hit
				// Rotate sprite to show player against the wall
				RotateSpriteAgainstWall(_currentDirection);
				// Play idle animation after hitting wall
				if (_animatedSprite != null)
				{
					_animatedSprite.Play("idle");
				}
				GD.Print($"Raycast collision detected! Stopping movement.");
			}
		}
		else
		{
			// Not moving, keep velocity at zero
			Velocity = Vector2.Zero;
		}
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
		_currentDirection = direction;
		_isMoving = true;
		
		// Update raycast direction to match movement direction
		UpdateRaycastDirection(direction);
		
		// Rotate sprite to face movement direction
		RotateSpriteToMovementDirection(direction);
		
		UpdateSprite(direction);
		PlayMovementAnimation(direction);
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

	private void SetupMovementRaycast()
	{
		// Create and setup raycast for movement collision detection
		_movementRaycast = new RayCast2D();
		AddChild(_movementRaycast);
		
		// Set initial raycast properties
		_movementRaycast.Enabled = true;
		_movementRaycast.TargetPosition = Vector2.Zero; // Will be set during movement
	}

	private void UpdateRaycastDirection(Vector2 direction)
	{
		if (_movementRaycast != null)
		{
			// Set raycast to point in movement direction with appropriate distance
			float rayDistance = GRID_SIZE * 0.8f; // Slightly less than grid size
			_movementRaycast.TargetPosition = direction * rayDistance;
			
			// Force raycast update
			_movementRaycast.ForceRaycastUpdate();
			
			GD.Print($"Raycast direction updated: {direction}, distance: {rayDistance}, colliding: {_movementRaycast.IsColliding()}");
		}
	}
}

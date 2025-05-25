using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{

	[Export]
	public int Speed = 5000;

	private Dictionary<Vector2, int> _sprites = new()
	{
		{ Vector2.Right, 1 },
		{ Vector2.Left, 2 },
		{ Vector2.Up, 3 },
		{ Vector2.Down, 4 }
	};

	private AnimatedSprite2D _animatedSprite;
	private Vector2 _currentDirection = Vector2.Zero;
	private bool _isMoving = false;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isMoving)
		{
			// Store previous position to check if we actually moved
			Vector2 previousPosition = GlobalPosition;
			
			// Move in the current direction at high speed
			Velocity = _currentDirection * Speed;
			MoveAndSlide();
			
			// Check if we're blocked in the movement direction
			// Calculate how much we actually moved
			Vector2 actualMovement = GlobalPosition - previousPosition;
			
			// If we didn't move much in our intended direction, we hit a wall
			float movementInDirection = actualMovement.Dot(_currentDirection);
			if (movementInDirection < Speed * delta * 0.1f) // Very small movement threshold
			{
				// Stop moving when we hit a wall in our movement direction
				_isMoving = false;
				Velocity = Vector2.Zero;
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
		UpdateSprite(direction);
	}

	private void UpdateSprite(Vector2 direction)
	{
		if (_sprites.TryGetValue(direction, out int frame))
		{
			_animatedSprite.Frame = frame;
		}
	}

	
}

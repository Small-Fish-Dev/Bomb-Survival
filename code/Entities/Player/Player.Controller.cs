namespace BombSurvival;

public partial class Player
{
	public float WalkSpeed => 140f * ( GroundEntity != null ? Math.Max( 0.5f, CrouchLevel ) : 1f );
	public float AccelerationSpeed => 600f; // Units per second (Ex. 200f means that after 1 second you've reached 200f speed)
	public float JumpHeight => 250f;
	public float WishSpeed { get; private set; } = 0f;
	public Vector3 Direction { get; set; } = Vector3.Zero;

	public Vector3 WishVelocity => Direction.Normal * WishSpeed;
	public Rotation WishRotation => Rotation.LookAt( Direction, Vector3.Up );
	public float StepSize => 12f;
	public float MaxWalkableAngle => 55f;

	public TimeSince TimeSinceLostFooting = 0f;

	public void ComputeController()
	{
		if ( IsKnockedOut )
			knockedOutMotion();
		else
		{
			normalMotion();
			computeGrab();
			computeLaunch();
		}
	}

	void computeGrab()
	{
		WantsToGrab = Input.Down( "grab" );

		if ( WantsToGrab && !IsGrabbing )
			Grab();
		else if ( Input.Released( "grab" ) )
			Release();
	}

	void computeLaunch()
	{
		if ( Input.Down( "launch" ) )
			if ( !IsKnockedOut )
				KnockOut( CollisionCenter + InputRotation.Backward * 50f, 400f, 1f );
	}

	void normalMotion()
	{
		var animationHelper = new CitizenAnimationHelper( this );

		Direction = InputDirection.RotateAround( Vector3.Up, Rotation.FromYaw( 90f ) ).WithY( 0f );

		if ( Direction != Vector3.Zero )
			WishSpeed = Math.Clamp( WishSpeed + AccelerationSpeed * Time.Delta, 0f, WalkSpeed );
		else
			WishSpeed = 0f;

		Velocity = Vector3.Lerp( Velocity, WishVelocity, 15f * Time.Delta ) // Smooth horizontal movement
			.WithZ( Velocity.z ); // Don't smooth vertical movement

		if ( TimeSinceLostFooting > Time.Delta * 5f )
			Velocity -= Vector3.Down * (TimeSinceLostFooting + 1f) * Game.PhysicsWorld.Gravity * Time.Delta * 2f;

		if ( Input.Pressed( "punch" ) && !IsPunching && LastPunch >= 0.7f )
			using ( LagCompensation() )
			{
				Punch();
				LastPunch = 0f;
			}

		if ( IsGrabbing )
		{
			var delta = GrabbingPosition - Position;
			var magnitude = delta.Length;
			var normal = delta.Normal;
			var strength = Math.Max( magnitude - CollisionHeight * 1.5f, 0f );

			Velocity += normal * strength * strength + Vector3.Down * 20f;
			Velocity /= 1 + Time.Delta * strength / 10f;
		}

		if ( IsBeingGrabbed )
		{
			var delta = Grabber.CollisionTop - CollisionTop;
			var magnitude = delta.Length;
			var normal = delta.Normal;
			var strength = Math.Max( magnitude - CollisionHeight * 1.5f, 0f );

			Velocity += normal * strength * strength + Vector3.Down * 20f;
			Velocity /= 1 + Time.Delta * strength / 10f;
		}

		var moveHelper = new MoveHelper( Position, Velocity );
		moveHelper.MaxStandableAngle = MaxWalkableAngle;

		moveHelper.Trace = moveHelper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.WithoutTags( "puppet", "collider" )
			.Ignore( this );

		if ( GroundEntity == null )
			moveHelper.TryMove( Time.Delta );
		else
			moveHelper.TryMoveWithStep( Time.Delta, StepSize );

		moveHelper.TryUnstuck2D( CollisionHeight - 1f );

		Position = moveHelper.Position.WithY( 0 );
		Velocity = moveHelper.Velocity.WithY( 0 );

		var traceDown = moveHelper.TraceDirection( Vector3.Down * 3f * (Velocity.z > 50f ? 0.3f : 1f) );

		if ( traceDown.Entity != null )
		{
			GroundEntity = traceDown.Entity;
			Position = traceDown.EndPosition;

			if ( Vector3.GetAngle( Vector3.Up, traceDown.Normal ) <= moveHelper.MaxStandableAngle )
				TimeSinceLostFooting = 0f;

			Velocity = Velocity.WithZ( Velocity.z / 2 );
		}
		else
		{
			GroundEntity = null;
			TimeSinceLostFooting = 0f;
			Velocity += Vector3.Down * -Game.PhysicsWorld.Gravity * Time.Delta;
		}

		if ( Input.Down( "jump" ) )
		{
			if ( GroundEntity != null && !IsKnockedOut )
			{
				if ( Vector3.GetAngle( Vector3.Up, traceDown.Normal ) <= moveHelper.MaxStandableAngle )
				{
					Velocity = Velocity.WithZ( JumpHeight );
					GroundEntity = null;
					animationHelper.TriggerJump();
				}
			}

			if ( Velocity.z > 0f ) // Floaty jump
				Velocity += Vector3.Up * -Game.PhysicsWorld.Gravity * Time.Delta / 2f;
		}
	}

	void knockedOutMotion()
	{
		var moveHelper = new MoveHelper( Position, Velocity );
		moveHelper.MaxStandableAngle = MaxWalkableAngle;

		moveHelper.Trace = moveHelper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.WithoutTags( "puppet", "collider" )
			.Ignore( this );

		moveHelper.TryMove( Time.Delta );
		moveHelper.TryUnstuck2D( CollisionHeight - 1f );

		Position = moveHelper.Position.WithY( 0 );
		Velocity = moveHelper.Velocity.WithY( 0 );

		var traceDown = moveHelper.TraceDirection( Vector3.Down * 10f );

		if ( traceDown.Entity != null )
		{
			GroundEntity = traceDown.Entity;
			Velocity -= Velocity * Time.Delta * 3f;
		}
		else
			Velocity += Vector3.Down * -Game.PhysicsWorld.Gravity * Time.Delta;
	}
}

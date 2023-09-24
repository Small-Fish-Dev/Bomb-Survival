namespace BombSurvival;

public partial class Player
{
	public static float BaseWalkSpeed => 140f;
	public float WalkSpeed => BaseWalkSpeed * ( GroundEntity != null ? Math.Max( 0.5f, CrouchLevel ) : 1f );
	public float AccelerationSpeed => 600f; // Units per second (Ex. 200f means that after 1 second you've reached 200f speed)
	public static float JumpHeight => 250f;
	public static float DiveStrength => 400f;
	public float WishSpeed { get; private set; } = 0f;
	public Vector3 Direction { get; set; } = Vector3.Zero;

	public Vector3 WishVelocity => Direction.Normal * WishSpeed;
	public Rotation WishRotation => Rotation.LookAt( Direction, Vector3.Up );
	public static float StepSize => 12f;
	public static float MaxWalkableAngle => 55f;

	public TimeSince TimeSinceLostFooting = 0f;

	public void ComputeController()
	{
		if ( IsKnockedOut )
			knockedOutMotion();
		else
		{
			normalMotion();
			computeGrab();
			computeDive();
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

	void computeDive()
	{
		if ( Input.Down( "dive" ) )
			if ( !IsKnockedOut )
				KnockOut( CollisionTop + InputRotation.Backward * 50f, DiveStrength, 1f );
	}

	void normalMotion()
	{
		var animationHelper = new CitizenAnimationHelper( this );

		var wishDirection = InputDirection.RotateAround( Vector3.Up, Rotation.FromYaw( 90f ) ).WithY( 0f );

		if ( InputDirection != Vector3.Zero )
			Direction = wishDirection;
		else
		{
			if ( GroundEntity != null )
				Direction = wishDirection;
			else
				Direction = Vector3.Zero;
		}

		if ( Direction != Vector3.Zero )
			WishSpeed = Math.Clamp( WishSpeed + AccelerationSpeed * Time.Delta, 0f, WalkSpeed );
		else
		{
			if ( GroundEntity != null )
				WishSpeed = 0f;
		}

		if ( Direction != Vector3.Zero || GroundEntity != null )
			Velocity = Vector3.Lerp( Velocity, WishVelocity, 15f * Time.Delta ) // Smooth horizontal movement
				.WithZ( Velocity.z ); // Don't smooth vertical movement

		if ( TimeSinceLostFooting > Time.Delta * 5f )
			Velocity -= Vector3.Down * (TimeSinceLostFooting + 1f) * Game.PhysicsWorld.Gravity * Time.Delta * 2f;

		if ( Input.Pressed( "punch" ) && !IsPunching && CanPunch )
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
			var strength = Math.Clamp( magnitude - CollisionHeight * 1.5f, 0f, 100f );

			Velocity += normal * strength * strength + Vector3.Down * 20f;
			Velocity /= 1 + Time.Delta * strength / 10f;
		}

		if ( IsBeingGrabbed )
		{
			var delta = Grabber.CollisionTop - CollisionTop;
			var magnitude = delta.Length * ( Grabber.GroundEntity == null ? 0.5f : 1f );
			var normal = delta.Normal;
			var strength = Math.Max( magnitude - CollisionHeight * 1.5f, 0f );

			Velocity += normal * strength * strength + Vector3.Down * 20f;
			Velocity /= 1 + Time.Delta * strength / 10f;
		}

		foreach ( var toucher in touchingPlayers )
		{
			if ( toucher is not { IsValid: true } )
				continue;

			var direction = (Position - toucher.Position).WithZ( 0 ).Normal;
			var distance = Position.DistanceSquared( toucher.Position );
			var maxDistance = 500f;

			var pushOffset = direction * Math.Max( maxDistance - distance, 0f ) * Time.Delta * 1.8f;
			Velocity += pushOffset.WithY( 0 );
		}

		var moveHelper = new MoveHelper( Position, Velocity );
		moveHelper.MaxStandableAngle = MaxWalkableAngle;

		var noTags = new string[4] { "puppet", "collider", "bot", "" };

		if ( Client.IsBot || IsSafe )
			noTags[3] = "player";

		moveHelper.Trace = moveHelper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.WithoutTags( noTags )
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

		if ( Jumping )
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

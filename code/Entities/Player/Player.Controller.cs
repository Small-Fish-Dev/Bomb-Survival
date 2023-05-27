namespace BombSurvival;

public partial class Player
{
	public float WalkSpeed => 120f;
	public float RunSpeed => 200f;
	public float AccelerationSpeed => 600f; // Units per second (Ex. 200f means that after 1 second you've reached 200f speed)
	public float WishSpeed { get; private set; } = 0f;
	public Vector3 Direction { get; set; } = Vector3.Zero;

	public Vector3 WishVelocity => Direction.Normal * WishSpeed;
	public Rotation WishRotation => Rotation.LookAt( Direction, Vector3.Up );
	public float StepSize => 8f;
	public float MaxWalkableAngle => 55f;

	internal TimeUntil punchFinish { get; set; } = 0f;
	public bool IsPunching => !punchFinish;

	public TimeSince TimeSinceLostFooting = 0f;

	public void ComputeMotion()
	{
		var animationHelper = Animations;

		Direction = InputDirection.RotateAround( Vector3.Up, Rotation.FromYaw( 90f ) ).WithY( 0f );

		if ( Direction != Vector3.Zero )
			WishSpeed = Math.Clamp( WishSpeed + AccelerationSpeed * Time.Delta, 0f, Input.Down( "run" ) ? RunSpeed : WalkSpeed );
		else
			WishSpeed = 0f;

		Velocity = Vector3.Lerp( Velocity, WishVelocity, 15f * Time.Delta ) // Smooth horizontal movement
			.WithZ( Velocity.z ); // Don't smooth vertical movement

		if ( TimeSinceLostFooting > Time.Delta * 5f )
			Velocity -= Vector3.Down * (TimeSinceLostFooting + 1f) * Game.PhysicsWorld.Gravity * Time.Delta;

		if ( Input.Down( "jump" ) )
		{
			if ( GroundEntity != null )
			{
				GroundEntity = null;
				Velocity = Velocity.WithZ( 300f );
				animationHelper.TriggerJump();
			}

			if ( Velocity.z > 0f ) // Floaty jump
				Velocity += Vector3.Up * -Game.PhysicsWorld.Gravity * Time.Delta / 2f;
		}

		if ( Input.Pressed( "attack1" ) && !IsPunching )
			Punch();

		var helper = new MoveHelper( Position, Velocity );
		helper.MaxStandableAngle = MaxWalkableAngle;

		helper.Trace = helper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.WithoutTags( "puppet" )
			.Ignore( this );

		if ( GroundEntity == null )
			helper.TryMove( Time.Delta );
		else
			helper.TryMoveWithStep( Time.Delta, StepSize );

		helper.TryUnstuck2D();

		Position = helper.Position.WithY( 0 );
		Velocity = helper.Velocity.WithY( 0 );

		var traceDown = helper.TraceDirection( Vector3.Down * 5f );

		if ( traceDown.Entity != null )
		{
			GroundEntity = traceDown.Entity;
			Position = traceDown.EndPosition;

			if ( Vector3.GetAngle( Vector3.Up, traceDown.Normal ) <= helper.MaxStandableAngle )
				TimeSinceLostFooting = 0f;
		}
		else
		{
			GroundEntity = null;
			TimeSinceLostFooting = 0f;
			Velocity += Vector3.Down * -Game.PhysicsWorld.Gravity * Time.Delta;
		}
	}

	public void Punch()
	{
		var animationHelper = Animations;

		punchFinish = 0.3f;
		animationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		SetAnimParameter( "b_attack", true );

		if ( Game.IsClient ) return;

		// TODO CHANGE TO A RAY CASTING TOWARDS DIRECTION
		var punchEntities = Entity.FindInSphere( CollisionWorldSpaceCenter + InputRotation.Forward * CollisionHeight / 2f, CollisionHeight / 4f )
			.Where( x => x != this );

		if ( punchEntities.Count() <= 0 ) return;

		var entityToPunch = punchEntities.First();
		DebugOverlay.Sphere( entityToPunch.Position, 30f, Color.Red, 1f, false );
	}
}

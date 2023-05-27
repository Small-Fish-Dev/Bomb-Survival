using Sandbox.Internal;

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


	public TimeSince TimeSinceLostFooting = 0f;

	public void ComputeMotion()
	{
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
				Animations.TriggerJump();
			}

			if ( Velocity.z > 0f ) // Floaty jump
				Velocity += Vector3.Up * -Game.PhysicsWorld.Gravity * Time.Delta / 2f;
		}

		var helper = new MoveHelper( Position, Velocity );
		helper.MaxStandableAngle = MaxWalkableAngle;

		helper.Trace = helper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.Ignore( this );

		if ( GroundEntity == null )
			helper.TryMove( Time.Delta );
		else
			helper.TryMoveWithStep( Time.Delta, StepSize );


		Position = helper.Position;
		Velocity = helper.Velocity;

		var traceDown = helper.TraceDirection( Vector3.Down );

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
}

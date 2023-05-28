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
		var puppetAnimationsHelper = PuppetAnimations;

		if ( IsKnockedOut )
		{
			Position = Puppet.Position;
		}
		else
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
					animationHelper.TriggerJump();
					puppetAnimationsHelper.TriggerJump();
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
				.WithoutTags( "puppet", "collider" )
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
	}

	public void Punch()
	{
		var animationHelper = Animations;
		var puppetAnimationsHelper = PuppetAnimations;

		punchFinish = 0.3f;
		animationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		puppetAnimationsHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		SetAnimParameter( "b_attack", true );
		Puppet.SetAnimParameter( "b_attack", true );

		if ( Game.IsClient ) return;

		var punchTrace = Trace.Ray( CollisionCenter, CollisionCenter + InputRotation.Forward * CollisionHeight * 1.5f )
			.Size( CollisionHeight * 1.5f )
			.EntitiesOnly()
			.WithoutTags( "collider", "player" )
			.Ignore( Puppet )
			.Run();

		if ( punchTrace.Entity is ModelEntity punchTarget )
		{
			var player = punchTarget.GetPlayer();
			if ( player != null )
			{
				player.KnockOut( CollisionCenter, 500f, 1f );
			}
			else
			{
				if ( !punchTarget.PhysicsEnabled ) return;

				var targetBody = punchTarget.PhysicsBody;

				if ( !targetBody.IsValid() ) return;
				if ( targetBody.BodyType != PhysicsBodyType.Dynamic ) return;

				targetBody.ApplyImpulseAt( targetBody.LocalPoint( punchTrace.HitPosition ).LocalPosition, InputRotation.Forward * 10000f );
			}
		}
	}
}

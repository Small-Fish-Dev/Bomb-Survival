using Sandbox;
using Sandbox.Physics;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	[Net] public ModelEntity Grabbing { get; private set; } = null;
	[Net] public Vector3 GrabbingPosition { get; private set; } = Vector3.Zero;
	[Net] public bool WantsToGrab { get; private set; } = false;
	public bool IsGrabbing => Grabbing != null;
	[Net] public bool IsBeingGrabbed { get; private set; } = false;
	public SpringJoint GrabSpring { get; private set; }

	public void Grab()
	{
		if ( Game.IsClient ) return;
		if ( IsPunching ) return;
		if ( IsGrabbing ) return;

		var grabTrace = Trace.Ray( CollisionTop, CollisionTop + InputRotation.Forward * CollisionHeight * 0.8f )
			.Size( CollisionHeight * 0.8f )
			.DynamicOnly()
			.WithoutTags( "collider", "player" )
			.Run();

		if ( grabTrace.Entity is ModelEntity grabTarget )
		{
			var player = grabTarget.GetPlayer();
			if ( player != null )
			{
				Grabbing = player;
				player.IsBeingGrabbed = true;
			}
			else
			{
				if ( !grabTarget.PhysicsEnabled ) return;

				var targetBody = grabTarget.PhysicsBody;

				if ( !targetBody.IsValid() ) return;
				if ( targetBody.BodyType != PhysicsBodyType.Dynamic ) return;
				if ( grabTarget is Bomb bombTarget && bombTarget.IsExploding ) return;

				Grabbing = grabTarget;
				Grabbing.PhysicsBody.SurfaceMaterial = "slippery_wave_entity";

				var armPosition = CollisionTop + (InputRotation.Forward * CollisionHeight / 1.5f);
				var grabPosition = targetBody.FindClosestPoint( armPosition );
				var distance = armPosition.Distance( grabPosition );
				GrabSpring = PhysicsJoint.CreateSpring(
					PhysicsPoint.World( Collider.PhysicsBody, armPosition ),
					PhysicsPoint.World( targetBody, grabPosition ), distance, distance );

				GrabbingPosition = grabPosition;
			}
		}
	}

	internal void SimulateGrab()
	{
		if ( IsKnockedOut ) return;
		if ( IsDead ) return;
		if ( !IsGrabbing ) return;
		if ( Grabbing is Bomb bombTarget && bombTarget.IsExploding )
		{
			Release();
			return;
		}

		if ( Grabbing is Player player )
		{
			if ( player.IsDead )
			{
				Release();
				return;
			}

			player.Velocity += Velocity.WithZ( 0 );
			GrabbingPosition = player.CollisionTop;
		}
		else
		{
			GrabbingPosition = GrabSpring.Point2.Transform.Position;
		}

		DebugOverlay.Line( GrabSpring.Point1.Transform.Position, GrabSpring.Point2.Transform.Position );
		DebugOverlay.Sphere( GrabSpring.Point1.Transform.Position, 5f, Color.Red );
		DebugOverlay.Sphere( GrabSpring.Point2.Transform.Position, 5f, Color.Blue );
		//Grabbing.Position = Vector3.Lerp( Grabbing.Position, CollisionCenter + InputRotation.Forward * 50f, Time.Delta * 10f );
	}

	public void Release()
	{
		GrabSpring?.Remove();
		if ( Grabbing != null )
		{
			Grabbing.PhysicsBody.SurfaceMaterial = "normal_wave_entity";

			if ( Grabbing is Player player )
				player.IsBeingGrabbed = false;
		}
		Grabbing = null;
	}
}

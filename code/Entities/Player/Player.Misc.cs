using Sandbox;
using Sandbox.Physics;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	[Net] internal TimeUntil knockedOutTimer { get; private set; } = 0f;
	[Net] public bool IsKnockedOut { get; private set; } = false;

	internal TimeUntil punchFinish { get; set; } = 0f;
	[Net, Local, Predicted] public TimeSince LastPunch { get; set; } = 0f;
	public bool CanPunch => LastPunch >= 0.7f;
	public bool IsPunching => !punchFinish;


	[ClientRpc]
	public void SetCharred( bool charred )
	{
		var colorToApply = charred ? Color.Black : Color.White;

		if ( Ragdoll.IsValid() )
		{
			Ragdoll.RenderColor = colorToApply;

			foreach ( var child in Ragdoll.Children )
				if ( child is ModelEntity clothing )
					clothing.RenderColor = colorToApply;
		}
	}

	public void KnockOut( Vector3 sourcePosition, float strength, float amount )
	{
		if ( IsDead ) return;

		IsKnockedOut = true;
		knockedOutTimer = amount;

		var direction = ((CollisionTop - sourcePosition).WithY( 0 ).Normal + Vector3.Up * 0.5f).Normal;
		Velocity = direction * strength;

		if ( Game.IsServer )
			Collider.EnableSolidCollisions = false;

		Release();
	}

	public void WakeUp()
	{
		if ( IsDead ) return;

		IsKnockedOut = false;

		if ( Collider.IsValid() )
			Collider.EnableSolidCollisions = true;
	}

	internal void SimulateKnockedOut()
	{
		if ( knockedOutTimer && GroundEntity != null )
			WakeUp();
	}

	public void Punch()
	{
		punchFinish = 0.3f;

		Release();

		var punchTrace = Trace.Ray( CollisionTop, CollisionTop + InputRotation.Forward * CollisionHeight * 1.5f )
			.Size( CollisionHeight * 1.2f )
			.DynamicOnly()
			.Ignore( this )
			.Ignore( Collider )
			.WithoutTags( "terrain" )
			.Run();

		if ( punchTrace.Entity is ModelEntity punchTarget )
		{
			var player = punchTarget.GetPlayer();
			if ( player != null )
			{
				player.KnockOut( CollisionCenter, 400f, 1f );
				PlaySound( "sounds/punch/punch.sound" );
			}
			else
			{
				if ( !punchTarget.PhysicsEnabled ) return;

				var targetBody = punchTarget.PhysicsBody;

				if ( !targetBody.IsValid() ) return;
				if ( targetBody.BodyType != PhysicsBodyType.Dynamic ) return;

				targetBody.ApplyImpulseAt( targetBody.LocalPoint( punchTrace.HitPosition ).LocalPosition, InputRotation.Forward * 30000f );
				PlaySound( "sounds/punch/punch.sound" );
			}
		}
	}
}

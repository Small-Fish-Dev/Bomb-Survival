namespace BombSurvival;

public partial class HomingMine : Bomb
{
	public override string ModelPath { get; } = "models/missile/missile.vmdl";
	public Player Target { get; set; } = null;

	public override void Spawn()
	{
		base.Spawn();
		UseAnimGraph = false;
		AnimateOnServer = true;
		PlaybackRate = 1;
		Scale = 0.3f;

		PhysicsBody.GravityEnabled = false;
	}

	[GameEvent.Tick.Server]
	internal virtual void SeekAndExplode()
	{
		if ( Target == null || Target.IsDead )
			Target = Player.GetLongestLiving();

		if ( Target != null && !Target.IsDead )
		{
			var wishDirection = (Target.Position - Position).Normal;
			var wishRotation = Rotation.LookAt( wishDirection, Vector3.Forward );

			Rotation = Rotation.Lerp( Rotation, wishRotation, Time.Delta * 10f );
		}

		if ( PhysicsBody.IsValid() )
		{
			Velocity /= 1f + Time.Delta;
			PhysicsBody.ApplyForce( PhysicsBody.Mass * Rotation.Forward * Time.Delta * 90000f );
		}
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );

		var other = eventData.Other.Entity;

		if ( other.GetPlayer() is Player )
			Explode();
		else
			if ( Velocity.Length > 50f )
			Explode();

	}
}

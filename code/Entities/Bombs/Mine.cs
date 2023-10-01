namespace BombSurvival;

public partial class Mine : Bomb
{
	public override string ModelPath { get; } = "models/bomb/bomb.vmdl";
	TimeUntil enableExplosion = 0.5f;

	public override void Spawn()
	{
		base.Spawn();
		Scale = 0.4f;
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );
		
		if ( enableExplosion )
			if ( eventData.Velocity.Length > 10f || eventData.Other.Entity.GetPlayer() != null )
				Explode();
	}
}

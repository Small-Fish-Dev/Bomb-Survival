namespace BombSurvival;

public partial class Mine : Bomb
{
	public override string ModelPath { get; } = "models/bomb/bomb.vmdl";

	public override void Spawn()
	{
		base.Spawn();
		Scale = 0.4f;
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );
		
		if ( Velocity.Length > 50f )
				Explode();
	}
}

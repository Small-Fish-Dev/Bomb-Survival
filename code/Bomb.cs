namespace BombSurvival;

public partial class Bomb : ModelEntity
{
	public float ExplosionSize { get; set; } = 75f;
	public bool IsExploding { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/bomb/placeholder_bomb.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}

	public void Explode()
	{
		IsExploding = true;

		BombSurvival.Explosion( Position, ExplosionSize );

		var nearbyBombs = Entity.FindInSphere( Position, ExplosionSize )
			.OfType<Bomb>()
			.Where( x => !x.IsExploding );

		foreach( var otherBomb in nearbyBombs )
			otherBomb.Explode();

		Delete();
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );
		Explode();
	}


	[ConCmd.Admin( "bomb" )]
	public static void TestExplosion()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Player;
		new Bomb
		{
			Position = pawn.Position + Vector3.Up * 100f + Vector3.Forward * 100f
		};

	}
}

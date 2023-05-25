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

		var nearbyEntities = Entity.FindInSphere( Position, ExplosionSize );
		var nearbyBombs = nearbyEntities
			.OfType<Bomb>()
			.Where( x => !x.IsExploding );

		foreach ( var bomb in nearbyBombs )
			bomb.Explode();

		var nearbyPlayers = nearbyEntities
			.OfType<Player>();

		foreach ( var player in nearbyPlayers )
			player.Kill();

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
			Position = pawn.Position + pawn.InputRotation.Forward * 100f
		};
	}
}

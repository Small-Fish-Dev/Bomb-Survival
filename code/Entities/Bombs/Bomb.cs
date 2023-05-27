namespace BombSurvival;

public abstract partial class Bomb : ModelEntity
{
	public virtual float BaseExplosionSize { get; set; } = 75f;
	public virtual string ModelPath { get; } = "models/bomb/placeholder_bomb.vmdl";
	public float ExplosionSize => BaseExplosionSize * Scale;
	public float CharSize => ExplosionSize + 10f + 15f * (ExplosionSize / 75f);
	public bool IsExploding { get; internal set; } = false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( ModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}

	public virtual void Explode()
	{
		IsExploding = true;

		BombSurvival.Explosion( Position, ExplosionSize, CharSize );

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
}

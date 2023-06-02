namespace BombSurvival;

public abstract partial class Bomb : AnimatedEntity
{
	public virtual float BaseExplosionSize { get; set; } = 150f;
	public virtual string ModelPath { get; } = "models/bomb/placeholder_bomb.vmdl";
	public float ExplosionSize => BaseExplosionSize * Scale;
	public float CharSize => ExplosionSize + 20f + 20f * (ExplosionSize / 75f);
	public bool IsExploding { get; internal set; } = false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( ModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		BombSurvival.AxisLockedEntities.Add( this );
	}
	protected override void OnDestroy()
	{
		BombSurvival.AxisLockedEntities.Remove( this );
		base.OnDestroy();
	}

	public virtual void Explode()
	{
		IsExploding = true;

		BombSurvival.Explosion( Position, ExplosionSize, CharSize );

		var entitiesInExplosion = Entity.FindInSphere( Position, ExplosionSize );
		var entitiesInChar = Entity.FindInSphere( Position, CharSize );

		var bombsToExplode = entitiesInExplosion
			.OfType<Bomb>()
			.Where( x => !x.IsExploding );
		foreach ( var bomb in bombsToExplode )
			bomb.Explode();

		var bubblesToBreak = entitiesInExplosion
			.OfType<ScoreBubble>();
		foreach ( var bubble in bubblesToBreak )
			bubble.Break();

		var playersToChar = entitiesInChar
			.Select( x => x.GetPlayer() )
			.Where( x => x != null )
			.Distinct();

		foreach ( var player in playersToChar )
		{
			player.SetCharred( true );
			player.KnockOut( Position, 1000, 2f );
		}

		var playersToKill = entitiesInExplosion
			.Select( x => x.GetPlayer() )
			.Where( x => x != null )
			.Distinct();

		foreach ( var player in playersToKill )
			player.Kill();

		Delete();
	}
}

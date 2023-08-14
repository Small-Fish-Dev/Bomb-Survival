namespace BombSurvival;

public abstract partial class Bomb : AnimatedEntity
{
	public virtual float BaseExplosionSize { get; set; } = 150f;
	public virtual string ModelPath { get; } = "models/bomb/placeholder_bomb.vmdl";
	public float ExplosionSize => BaseExplosionSize * Scale;
	public float CharSize => ExplosionSize + 20f + 20f * (ExplosionSize / 75f);
	public bool IsExploding { get; internal set; } = false;
	internal TimeUntil ExplosionTimer { get; set; } = 0f;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( ModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		PhysicsBody.AngularDrag = 999999f;
		PhysicsBody.AngularDamping = 999999f;
		PhysicsBody.SurfaceMaterial = "sticky_wave_entity";

		BombSurvival.AxisLockedEntities.Add( this );
	}
	protected override void OnDestroy()
	{
		BombSurvival.AxisLockedEntities.Remove( this );
		base.OnDestroy();
	}

	public virtual void TriggerExplosion( float timer = 0.2f )
	{
		IsExploding = true;
		ExplosionTimer = timer;
	}

	public virtual void Explode()
	{
		IsExploding = true;

		var entitiesInExplosion = Entity.FindInSphere( Position, ExplosionSize );
		var entitiesInChar = Entity.FindInSphere( Position, CharSize );

		BombSurvival.Explosion( Position, ExplosionSize, CharSize );

		var bombsToExplode = entitiesInExplosion
			.OfType<Bomb>()
			.Where( x => !x.IsExploding );

		foreach ( var bomb in bombsToExplode )
		{
			bomb.TriggerExplosion( Game.Random.Float( 0.05f, 0.15f ) );
			bomb.RenderColor = Color.Black;

			var direction = ((bomb.CollisionWorldSpaceCenter - CollisionWorldSpaceCenter).WithY( 0 ).Normal + Vector3.Up * 0.5f).Normal;

			bomb.PhysicsGroup.Velocity = 0;
			bomb.PhysicsGroup.ApplyImpulse( direction * 2000f * bomb.PhysicsBody.Mass * Scale );
		}

		var bubblesToBreak = entitiesInExplosion
			.OfType<ScoreBubble>();
		foreach ( var bubble in bubblesToBreak )
			bubble.Break();

		var playersToKill = entitiesInExplosion
			.Select( x => x.GetPlayer() )
			.Where( x => x != null )
			.Distinct();

		foreach ( var player in playersToKill )
			player.Kill();

		var playersToChar = entitiesInChar
			.Select( x => x.GetPlayer() )
			.Where( x => x != null )
			.Where( x => !x.IsDead )
			.Distinct();

		foreach ( var player in playersToChar )
		{
			var traceCheck = Trace.Ray( Position, player.CollisionTop )
				.WithTag( "terrain" )
				.Run();

			if ( !traceCheck.Hit )
			{
				player.SetCharred( true );
				player.KnockOut( Position, 1000 * Scale, 2f );
			}
		}

		if ( Game.IsServer )
			Delete();
	}

	[GameEvent.Tick.Server]
	internal virtual void computeExplosion()
	{
		if ( IsExploding )
			if ( ExplosionTimer )
				Explode();
	}

	TimeUntil stopSticky = 1.5f;
	bool sticky = true;

	[GameEvent.Tick]
	void changeStick()
	{
		if ( stopSticky && sticky && PhysicsBody.IsValid() || PhysicsBody.IsValid() && PhysicsBody.Sleeping && sticky )
		{
			PhysicsBody.AngularDrag = 0f;
			PhysicsBody.AngularDamping = 0f;
			PhysicsBody.SurfaceMaterial = "normal_wave_entity";
			sticky = false;
		}
	}
}

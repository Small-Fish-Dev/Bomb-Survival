namespace BombSurvival;

public partial class ScoreBubble : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/score_bubble/score_bubble.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		Scale = 1.5f;
		Tags.Add( "bubble" );

		BombSurvival.AxisLockedEntities.Add( this );
	}

	protected override void OnDestroy()
	{
		BombSurvival.AxisLockedEntities.Remove( this );
		base.OnDestroy();
	}

	public void Break()
	{
		var particle = Particles.Create( "models/score_bubble/particles/score_bubble_break.vpcf", Position );
		particle.Set( "scale", Scale );

		Delete();
	}

	public void Award( Player player )
	{
		player.AssignPoints( 10 );
		Break();
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( Game.IsClient ) return;

		if ( other.GetPlayer() is Player player )
			Award( player );
	}
}

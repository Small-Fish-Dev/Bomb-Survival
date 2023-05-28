namespace BombSurvival;

public partial class ScoreBubble : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/score_bubble/score_bubble.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		Scale = 3f;
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

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( Game.IsClient ) return;

		if ( other is Player || other.Owner is Player )
			Break();
	}
}

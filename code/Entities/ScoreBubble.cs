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
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( Game.IsClient ) return;

		if ( other is Player || other.Owner is Player )
		{
			Delete();
		}
	}
}

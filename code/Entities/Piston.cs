using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/piston/piston.vmdl" )]
public partial class Piston : AnimatedEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/piston/piston.vmdl" );
		Rotation = Rotation.FromYaw( -90f );
		UseAnimGraph = false;
		PlaybackRate = 1;
		Scale = 2.5f;
	}

	[GameEvent.Tick]
	public void Tick()
	{
		DebugOverlay.Sphere( GetBoneTransform( 1 ).Position, 50f, Color.Red );
	}
}

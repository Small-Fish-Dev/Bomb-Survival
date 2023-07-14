using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/scoreboard/placeholder.vmdl" )]
public partial class Scoreboard : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/scoreboard/placeholder.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}
}

using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/checkpoint/placeholder_checkpoint.vmdl" )]
public partial class Checkpoint : ModelEntity
{

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/checkpoint/placeholder_checkpoint.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}
}

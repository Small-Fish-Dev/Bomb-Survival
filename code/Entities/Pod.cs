using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/pod/pod.vmdl" )]
public partial class Pod : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/pod/pod.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	[GameEvent.Tick]
	public void SetColorTest()
	{
		RenderColor = new ColorHsv( (Time.Now * 90f) % 360, 0.5f, 1 );
	}
}

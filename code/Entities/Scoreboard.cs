using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/scoreboard/placeholder.vmdl" )]
public partial class Scoreboard : ModelEntity
{
	ScorePanel panel;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/scoreboard/placeholder.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	public override void ClientSpawn()
	{
		createPanel();
	}

	[Event.Hotload]
	void createPanel()
	{
		if ( Game.IsServer )
			return;

		panel?.Delete( true );

		var size = new Vector2( 1200, 950 );
		panel = new( size, 4 );
		panel.Position = Position + Vector3.Up * 160f + Vector3.Left * 25f;
		panel.Rotation = Rotation.FromAxis( Vector3.Up, -90f );
	}
}

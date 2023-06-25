using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/checkpoint/checkpoint.vmdl" )]
public partial class Checkpoint : AnimatedEntity
{
	public AnimatedEntity ClientModel { get; set; }
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/piston/piston.vmdl" );
		Rotation = Rotation.FromYaw( -90f );
		UseAnimGraph = false;
		AnimateOnServer = true;
		PlaybackRate = 0.2f;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();
		ClientModel = new AnimatedEntity( "models/checkpoint/checkpoint.vmdl" );
		ClientModel.Position = Position;
		ClientModel.Rotation = Rotation.FromYaw( -90f );
		ClientModel.EnableDrawing = true;
		ClientModel.SetParent( this, true );
	}
}

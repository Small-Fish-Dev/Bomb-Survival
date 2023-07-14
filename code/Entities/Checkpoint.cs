using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/checkpoint/checkpoint.vmdl" )]
public partial class Checkpoint : AnimatedEntity
{
	public AnimatedEntity ClientModel { get; set; }
	[Property( Title = "Is Scoreboard Checkpoint" ), Net]
	public bool IsScoreboardCheckpoint { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/piston/piston.vmdl" );
		Rotation = Rotation.FromYaw( -90f );
		UseAnimGraph = false;
		AnimateOnServer = true;
		PlaybackRate = IsScoreboardCheckpoint ? 0f : 0.2f;
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

	[GameEvent.Tick]
	void blockAnimation()
	{
		if ( IsScoreboardCheckpoint )
			CurrentSequence.Time = ( (float)Math.Sin( (double)Time.Now ) + 1 ) / 4;
	}
}

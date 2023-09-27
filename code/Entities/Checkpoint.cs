using Editor;
using Sandbox;

namespace BombSurvival;

public enum CheckpointType
{
	None,
	Pod,
	Scoreboard,
	Play,
	Tutorial
}

[HammerEntity]
[EditorModel( "models/checkpoint/checkpoint.vmdl" )]
public partial class Checkpoint : AnimatedEntity
{
	public AnimatedEntity ClientModel { get; set; }
	[Property( Title = "Type of checkpoint" ), Net]
	public CheckpointType Type { get; set; } = CheckpointType.None;
	public Vector3 RespawnPosition => Type == CheckpointType.Pod ? Position : GetBoneTransform( 1 ).Position;
	Vector3 lastPosition = Vector3.Zero;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/piston/piston.vmdl" );
		Rotation = Rotation.FromYaw( -90f );
		UseAnimGraph = false;
		AnimateOnServer = true;
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
	void compute()
	{
		if ( Type == CheckpointType.Pod )
		{
			CurrentSequence.Time = 0f;
			EnableDrawing = false;

			if ( ClientModel.IsValid() )
				ClientModel.EnableDrawing = false;
		}
		if ( Type == CheckpointType.Scoreboard )
			CurrentSequence.Time = ( (float)Math.Sin( (double)Time.Now ) + 1 ) / 4;
		if ( Type == CheckpointType.Play )
			CurrentSequence.Time = Time.Now / 5f % CurrentSequence.Duration;
		if ( Type == CheckpointType.Tutorial )
			CurrentSequence.Time = 0f;

		if ( Game.IsServer )
		{
			if ( lastPosition != Vector3.Zero )
				Velocity = RespawnPosition - lastPosition;

			lastPosition = RespawnPosition;
		}
	}

	public static Checkpoint First( CheckpointType type )
	{
		return Entity.All.OfType<Checkpoint>()
			.Where( x => x.Type == type )
			.FirstOrDefault();
	}

	public static Checkpoint First()
	{
		var currentState = BombSurvival.Instance.CurrentState;

		if ( currentState is PodState )
			return First( CheckpointType.Pod );
		if ( currentState is StartingState )
			return First( CheckpointType.Scoreboard );
		if ( currentState is PlayingState )
			return First( CheckpointType.Play );
		if ( currentState is ScoringState )
			return First( CheckpointType.Scoreboard );
		if ( currentState is TutorialState )
			return First( CheckpointType.Tutorial );

		return First( CheckpointType.Pod );
	}

	public void SetLives( int lives )
	{
		ClientModel.CurrentSequence.Time = 0;

		if ( lives != 0 )
		{
			ClientModel.SetBodyGroup( "body", 4 - lives );
			ClientModel.Model.Materials.Last().Set( "g_vColorTint", Color.White );
		}
		else
		{
			ClientModel.SetBodyGroup( "body", 0 );
			ClientModel.Model.Materials.Last().Set( "g_vColorTint", Color.Red );
		}
	}
}

using Editor;
using Sandbox;

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

	Vector3 lastPosition = Vector3.Zero;

	[GameEvent.Tick]
	void compute()
	{
		if ( IsScoreboardCheckpoint )
			CurrentSequence.Time = ( (float)Math.Sin( (double)Time.Now ) + 1 ) / 4;
		else
			CurrentSequence.Time = Time.Now / 5f % CurrentSequence.Duration;

		if ( Game.IsServer )
		{
			var currentPosition = GetBoneTransform( 1 ).Position;

			if ( lastPosition != Vector3.Zero )
				Velocity = currentPosition - lastPosition;

			lastPosition = currentPosition;
		}
	}

	public static Checkpoint First()
	{
		return Entity.All.OfType<Checkpoint>()
			.Where( x => x.IsScoreboardCheckpoint != (BombSurvival.Instance.CurrentState is PlayingState) )
			.FirstOrDefault();
	}

	public static Vector3 FirstPosition() => First().GetBoneTransform( 1 ).Position;

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

using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/emitter/emitter.vmdl" )]
public partial class BombSpawner : AnimatedEntity
{
	public AnimatedEntity ClientModel { get; set; }
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/piston/piston.vmdl" );
		Rotation = Rotation.FromYaw( -90f ) * Rotation.FromPitch( 90f );
		UseAnimGraph = false;
		AnimateOnServer = true;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();
		ClientModel = new AnimatedEntity( "models/emitter/emitter.vmdl" );
		ClientModel.Position = Position;
		ClientModel.EnableDrawing = true;
		ClientModel.SetParent( this, true );
	}

	Vector3 lastPosition = Vector3.Zero;
	[GameEvent.Tick]
	void compute()
	{
		CurrentSequence.Time = Time.Now / 6f % CurrentSequence.Duration;

		if ( Game.IsServer )
		{
			var currentBonePosition = GetBoneTransform( 1 ).Position;
			if ( lastPosition == Vector3.Zero && Position != Vector3.Zero )
				lastPosition = currentBonePosition;

			if ( lastPosition != Vector3.Zero )
			{
				Velocity = ( currentBonePosition - lastPosition ) / Time.Delta;
				lastPosition = currentBonePosition;
			}
		}
	}

	public static BombSpawner First()
	{
		return Entity.All.OfType<BombSpawner>()
			.FirstOrDefault();
	}

	public static Vector3 FirstPosition() => First().GetBoneTransform( 1 ).Position;
}

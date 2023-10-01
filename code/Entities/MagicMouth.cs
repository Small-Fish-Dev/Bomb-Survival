using Editor;
using GridAStar;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/mouth/mouth.vmdl" )]
public partial class MagicMouth : AnimatedEntity, IBlowable, ICharrable
{
	[Net, Property]
	public string Dialogue { get; set; } = "Hello I am a talking mouth";
	[Net, Property]
	public float Range { get; set; } = 200f;
	Sound voice;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/mouth/mouth.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	[GameEvent.Tick.Server]
	public void TalkingTest()
	{
		var nearestPlayer = All.OfType<Player>()
			.Where( x => x.Position.Distance( Position ) <= Range )
			.OrderBy( x => x.Position.Distance( Position ) )
			.FirstOrDefault();

		SetAnimParameter( "speaking", nearestPlayer.IsValid() );

		if ( nearestPlayer.IsValid() )
		{
			if ( !voice.IsPlaying )
				voice = PlaySound( "sounds/gibberish/cartoony/cartoony_gibberish.sound" );
			voice.SetVolume( 2f );

			DebugOverlay.Text( Dialogue, Position + Vector3.Down * 15f );
		}
		else
		{
			if ( voice.IsPlaying )
				voice.Stop();
		}
	}
	public void Blow() => Delete();
	public void Char() => SetMaterialOverride( ICharrable.CharMaterial );
}

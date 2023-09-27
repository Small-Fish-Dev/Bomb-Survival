using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/mouth/mouth.vmdl" )]
public partial class MagicMouth : AnimatedEntity
{
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
			.Where( x => x.Position.Distance( Position ) <= 200f )
			.OrderBy( x => x.Position.Distance( Position ) )
			.FirstOrDefault();

		SetAnimParameter( "speaking", nearestPlayer.IsValid() );

		if ( nearestPlayer.IsValid() )
		{
			if ( !voice.IsPlaying )
				voice = PlaySound( "sounds/gibberish/cartoony/cartoony_gibberish.sound" );
			voice.SetVolume( 2f );
		}
		else
		{
			if ( voice.IsPlaying )
				voice.Stop();
		}


	}
}

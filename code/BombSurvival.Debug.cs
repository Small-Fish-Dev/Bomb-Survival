namespace BombSurvival;

public partial class BombSurvival
{
	public static bool Bombs = true;
	[ConCmd.Admin("toggle_bombs")]
	public static void ToggleBombs()
	{
		Bombs = !Bombs;
		Log.Info( $"Bombs {(Bombs ? "enabled" : "disabled")}" );
	}

	[ConCmd.Admin("set_state")]
	public static void SetState( string state )
	{
		if ( state == "start" || state == "starting" )
			SetState<StartingState>();
		if ( state == "play" || state == "playing" )
			SetState<PlayingState>();
		if ( state == "score" || state == "scoring" )
			SetState<ScoringState>();
		if ( state == "pod" )
			SetState<PodState>();
		if ( state == "tutorial" )
			SetState<TutorialState>();
	}
}

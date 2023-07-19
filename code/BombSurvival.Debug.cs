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
}

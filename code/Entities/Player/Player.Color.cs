namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	[Net] public Color PlayerColor { get; set; }

	public static List<Color> PlayerColors = new()
	{
		new Color( 204f / 255f, 0f, 1f / 255f ),			// Red
		new Color( 0f, 0f, 254f / 255f ),					// Blue
		new Color( 1f / 255f, 204f / 255f, 0f ),			// Green
		new Color( 3f / 255f, 192f / 255f, 198f / 255f ),	// Cyan
		new Color( 255f / 255f, 255f / 255f, 1f / 255f ),	// Yellow
		new Color( 118f / 255f, 44f / 255f, 167f / 255f ),	// Purple
		new Color( 251f / 255f, 148f / 255f, 11f / 255f ),	// Orange
		new Color( 254f / 255f, 152f / 255f, 191f / 255f )	// Pink
	};
	
	public static Color FirstAvailableColor()
	{
		var allPlayers = All.OfType<Player>();
		var availableColors = PlayerColors.Where( color => !allPlayers.Any( player => player.PlayerColor == color ) ); // Get colors that haven't been assigned to a player yet

		if ( availableColors.Count() > 0 )
			return availableColors.First(); // Return first unassigned color if available
		else
			return new Color( Game.Random.Float(), Game.Random.Float(), Game.Random.Float() ); // Else return a random color, shouldn't happen since max players is 8 but maybe people want to increase it
	}
}

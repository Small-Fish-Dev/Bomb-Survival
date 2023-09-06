namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	[Net, Change] int score { get; set; } = 0;
	public int Score
	{
		get => score;
		private set
		{
			if ( score == value ) return;
			if ( !Game.IsServer ) return;

			Event.Run( "score.changed", this, score, value );

			score = Math.Max( 0, value );
		}
	}

	public void OnscoreChanged( int oldValue, int newValue ) => Event.Run( "score.changed", oldValue, newValue );

	public void AssignPoints( int points )
	{
		Score = Math.Clamp( score + points, 0, int.MaxValue );
	}

	[ClientRpc]
	public static void SendScores()
	{
		if ( Game.LocalPawn is not Player player ) return;

		Sandbox.Services.Stats.SetValue( Game.LocalClient, "house-points", player.Score );
	}

	public void ResetScore() => AssignPoints( -Score );
}

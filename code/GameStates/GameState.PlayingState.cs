using System.Collections.Generic;

namespace BombSurvival;

public partial class PlayingState : GameState
{
	TimeUntil assignPoints = 1;
	TimeUntil nextGridRegen = 1;

	public async override void Start()
	{
		base.Start();

		await BombSurvival.GenerateLevel();	// First we generate the terrain
		BombSurvivalBot.CancelAllTokens();	// Cancel any leftover navigation tokens (Shouldn't be any but just to be safe)
		await BombSurvival.GenerateGrid();	// Generate the initial grid
		BombSurvival.InitializeHeatMap();	// Initialize heatmap (Including safe cells, which is why we do it after the grid)

		foreach ( var player in Entity.All.OfType<Player>() )
		{
			player.Respawn();
			player.ResetScore();
			player.ResetLives();
		}	
	}

	public override void Compute()
	{
		base.Compute();

		var allPlayers = Entity.All.OfType<Player>();
		var playersAlive = allPlayers.Where( x => !x.IsDead );
		var playersPlaying = allPlayers.Where( x => x.LivesLeft >= 0 );

		foreach ( var player in playersAlive )
		{
			if ( assignPoints )
			{
				player.AssignPoints( 1 );
				assignPoints = 1;
			}
		}

		if ( playersAlive.Count() == 0 && playersPlaying.Count() == 0)
			BombSurvival.SetState<ScoringState>();

		if ( nextGridRegen )
		{
			//BombSurvivalBot.CancelAllTokens();
			GameTask.RunInThreadAsync( async () => await BombSurvival.GenerateGrid() );
			nextGridRegen = 2;
		}

		BombSurvival.ComputeWaves();
	}
}

using System.Collections.Generic;

namespace BombSurvival;

public partial class ScoringState : GameState
{
	public async override Task Start()
	{
		await base.Start();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		if ( Game.IsServer )
			Player.SendScores();

		return;
	}

	public override void Compute()
	{
		base.Compute();

		if ( SinceStarted >= 10f )
			BombSurvival.SetState<PlayingState>();
	}
}

using System.Collections.Generic;

namespace BombSurvival;

public partial class StartingState : GameState
{
	public async override Task Start()
	{
		await base.Start();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		return;
	}

	public override void Compute()
	{
		base.Compute();

		if ( SinceStarted >= 5f )
			BombSurvival.SetState<PlayingState>();
	}
}

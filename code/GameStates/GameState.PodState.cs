using System.Collections.Generic;

namespace BombSurvival;

public partial class PodState : GameState
{
	public async override Task Start()
	{
		await base.Start();

		await BombSurvival.GenerateEmptyLevel();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		return;
	}

	public override void Compute()
	{
		base.Compute();

		if ( SinceStarted >= 10f )
			BombSurvival.SetState<StartingState>();
	}
}

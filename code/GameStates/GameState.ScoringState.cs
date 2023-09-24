using System.Collections.Generic;

namespace BombSurvival;

public partial class ScoringState : GameState
{
	public async override void Start()
	{
		base.Start();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		await BombSurvival.DeleteLevel();

		foreach ( var bubble in Entity.All.OfType<ScoreBubble>() )
			bubble.Delete();
		foreach ( var bomb in Entity.All.OfType<Bomb>() )
			bomb.Delete();

		if ( Game.IsServer )
			Player.SendScores();
	}

	public override void Compute()
	{
		base.Compute();

		if ( SinceStarted >= 10f )
			BombSurvival.SetState<PlayingState>();
	}
}

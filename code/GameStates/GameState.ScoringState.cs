using System.Collections.Generic;

namespace BombSurvival;

public partial class ScoringState : GameState
{
	public override void Start()
	{
		base.Start();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		if ( Game.IsServer )
			Player.SendScores();
	}
}

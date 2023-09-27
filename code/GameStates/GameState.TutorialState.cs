using System.Collections.Generic;

namespace BombSurvival;

public partial class TutorialState : GameState
{
	public override void Start()
	{
		base.Start();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

	}

	public override void Compute()
	{
		base.Compute();
	}
}

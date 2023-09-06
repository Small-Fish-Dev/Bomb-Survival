using System.Collections.Generic;

namespace BombSurvival;

public partial class PodState : GameState
{
	public override void Compute()
	{
		base.Compute();

		if ( SinceStarted >= 10f )
			BombSurvival.SetState<StartingState>();
	}
}

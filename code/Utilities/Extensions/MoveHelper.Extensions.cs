namespace BombSurvival;

public static class MoveHelperExtensions
{
	public static bool TryUnstuck2D( this ref MoveHelper helper )
	{
		var tr = helper.TraceFromTo( helper.Position, helper.Position );
		if ( !tr.StartedSolid ) return true;

		return helper.Unstuck2D();
	}

	public static bool Unstuck2D( this ref MoveHelper helper )
	{
		//
		// Try going straight up first, people are most of the time stuck in the floor
		//
		for ( int i = 1; i < 20; i++ )
		{
			var tryPos = helper.Position + Vector3.Up * i;

			var tr = helper.TraceFromTo( tryPos, helper.Position );

			if ( !tr.StartedSolid )
			{
				helper.Position = tryPos + tr.Direction.Normal * ( tr.Distance - 0.5f );
				return true;
			}
		}

		//
		// Then fuck it, we got to get unstuck some how, try random shit
		//
		for ( int i = 1; i < 100; i++ )
		{
			var tryPos = helper.Position + Vector3.Random.WithY(0) * i;

			var tr = helper.TraceFromTo( tryPos, helper.Position );
			if ( !tr.StartedSolid )
			{
				helper.Position = tryPos + tr.Direction.Normal * (tr.Distance - 0.5f);
				return true;
			}
		}

		return false;
	}
}

using GridAStar;
using Sandbox.Internal;

namespace BombSurvival;

public partial class BombSurvival
{
	public static Dictionary<IntVector2, float> HeatMap { get; private set; } = new Dictionary<IntVector2, float>();
	public const float HeatBlockSize = 100f;
	public static float HeatUpdateFrequency => 0.5f;
	public static float HeatTenPercentile = 0f;
	TimeUntil nextHeatUpdate = 0f;
	static bool initialized = false;

	public static void InitializeHeatMap()
	{
		HeatMap.Clear();
		HeatMap = new Dictionary<IntVector2, float>();

		var maximumX = (int)Math.Ceiling( WorldWidth / HeatBlockSize );
		var maximumY = (int)Math.Ceiling( WorldHeight / HeatBlockSize );

		for ( int x = -maximumX / 2; x <= maximumX / 2; x++ )
			for ( int y = -maximumY / 2; y <= maximumY / 2; y++ )
				HeatMap.Add( new IntVector2( x, y ), 0f );

		initialized = true;
	}

	public static IntVector2 GetHeatPosition( Vector3 point )
	{
		var localPosition = Foreground?.Transform.PointToLocal( point ) ?? point;
		return new IntVector2( (int)Math.Round( localPosition.x / HeatBlockSize ), (int)Math.Round( localPosition.y / HeatBlockSize ) );
	}

	public static float GetHeat( IntVector2 coordinates )
	{
		if ( HeatMap.TryGetValue( coordinates, out var heat ) )
			return heat;
		else
			return 0f;
	}

	public static float GetHeat( Vector3 point ) => GetHeat( GetHeatPosition( point ) );

	[GameEvent.Tick.Server]
	void calculateHeat()
	{
		if ( !initialized ) return;

		if ( nextHeatUpdate )
		{
			var maximumX = (int)Math.Ceiling( WorldWidth / HeatBlockSize );
			var maximumY = (int)Math.Ceiling( WorldHeight / HeatBlockSize );

			var allBombs = Entity.All.OfType<Bomb>();
			var highestPoints = GetHighestPoints().Take( 5 );

			for ( int x = -maximumX / 2; x <= maximumX / 2; x++ )
				for ( int y = -maximumY / 2; y <= maximumY / 2; y++ )
				{
					var position = new Vector3( x, 0f, y ) * HeatBlockSize;
					var mins = position - HeatBlockSize;
					var maxs = position + HeatBlockSize;
					var bbox = new BBox( mins, maxs );
					var currentHeat = 0f;

					foreach ( var bomb in allBombs )
						if ( bbox.Contains( bomb.Position ) )
							currentHeat += bomb.ExplosionSize / 150f;

					foreach ( var point in highestPoints )
						if ( bbox.Contains( point ) )
							currentHeat += 0.5f; // Gotta remember we drop mines on top of the highest place

					updateHeat( new IntVector2( x, y ), currentHeat );
				}

			nextHeatUpdate = HeatUpdateFrequency;
			iterateHeat();

			var sortedHeatMap = HeatMap.OrderBy( x => x.Value ).ToArray();
			var totalCount = HeatMap.Count();
			var tenthPlace = (int)Math.Round( totalCount / 10f );

			HeatTenPercentile = sortedHeatMap[tenthPlace].Value;
		}
	}

	void iterateHeat( int iterations = 5, float linearDecay = 8f, float flatDecay = 0.2f )
	{
		if ( !initialized ) return;

		var currentIteration = 0;

		while ( currentIteration < iterations )
		{
			currentIteration++;

			var validTiles = HeatMap.Where( x => x.Value > 0 );
			var tilesToUpdate = new Dictionary<IntVector2, float>();

			foreach ( var tile in validTiles )
			{
				var additionalValue = tile.Value / linearDecay - flatDecay;

				if ( additionalValue > 0 )
					for ( int x = -1; x <= 1; x++ )
						for ( int y = -1; y <= 1; y++ )
						{
							if ( x == 0 && y == 0 ) continue;
							var adjacentPosition = tile.Key + new IntVector2( x, y );

							if ( HeatMap.ContainsKey( adjacentPosition ) )
								if ( tilesToUpdate.TryGetValue( adjacentPosition, out var currentValue ) )
									tilesToUpdate[adjacentPosition] += additionalValue;
								else
									tilesToUpdate.Add( adjacentPosition, additionalValue );
						}
			}

			foreach ( var tile in tilesToUpdate )
				updateHeat( tile.Key, HeatMap[tile.Key] + tile.Value );
		}
	}

	void updateHeat( IntVector2 position, float value )
	{
		if ( !initialized ) return;

		HeatMap[position] = value;
	}


	[Event.Debug.Overlay( "displayHeatmap", "Display Heatmap", "fireplace" )]
	static void displayHeatmap()
	{
		foreach ( var heatTile in HeatMap )
		{
			var position = new Vector3( heatTile.Key.x, 0f, heatTile.Key.y ) * HeatBlockSize;
			var color = new ColorHsv( 45f - heatTile.Value * 5f, 1f, 1f, heatTile.Value > 0 ? 1f : 0f );
			
			DebugOverlay.Box( position - HeatBlockSize / 2f, position + HeatBlockSize / 2f, color );
			DebugOverlay.Text( MathF.Round( heatTile.Value, 1 ).ToString(), position + new Vector3( 0f, -HeatBlockSize / 2f, 0f ), maxDistance: 800f );
		}
	}
}

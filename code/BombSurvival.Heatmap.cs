using GridAStar;
using Sandbox.Internal;

namespace BombSurvival;

public partial class BombSurvival
{
	public static Dictionary<IntVector2, float> HeatMap { get; private set; } = new Dictionary<IntVector2, float>();
	public const float HeatBlockSize = 100f;
	public static float HeatUpdateFrequency => 0.5f;
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

	[GameEvent.Tick.Server]
	void calculateHeat()
	{
		if ( !initialized ) return;

		if ( nextHeatUpdate )
		{
			var maximumX = (int)Math.Ceiling( WorldWidth / HeatBlockSize );
			var maximumY = (int)Math.Ceiling( WorldHeight / HeatBlockSize );

			var allBombs = Entity.All.OfType<Bomb>();

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

					updateHeat( new IntVector2( x, y ), currentHeat );
				}

			nextHeatUpdate = HeatUpdateFrequency;
			iterateHeat();
		}
	}

	void iterateHeat( int iterations = 5, float exponentialDecay = 0.6f, float flatDecay = 0.1f )
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
				var additionalValue = exponentialDecay * MathF.Pow( exponentialDecay, tile.Value ) - flatDecay;

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
			DebugOverlay.Text( MathF.Round( heatTile.Value, 1 ).ToString(), position + new Vector3( 0f, -HeatBlockSize / 2f, 0f ) );
		}
	}
}

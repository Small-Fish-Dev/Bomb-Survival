using Sandbox.Sdf;

namespace BombSurvival;

public partial class BombSurvival
{
	public static Sdf2DWorld Terrain { get; set; }
	public static Sdf2DLayer TerrainLayer => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/sand.sdflayer" );
	public static Texture TerrainTexture => Texture.Load( FileSystem.Mounted, "terrains/hill.png" );

	[ConCmd.Admin("regenerate_terrain")]
	public static void GenerateLevel()
	{
		Terrain?.Clear();
		Terrain ??= new Sdf2DWorld
		{
			LocalRotation = Rotation.FromRoll( 90f )
		};
		var terrainSdf = new TextureSdf( TerrainTexture, 4, TerrainTexture.Width * 2 );

		Terrain?.Add( terrainSdf, TerrainLayer );
		GameTask.RunInThreadAsync( async () =>
		{
			await GameTask.NextPhysicsFrame();
			Event.Run( "TerrainLoaded" );
		} );
	}

	public static Vector2 PointToLocal( Vector3 point )
	{
		var localPosition = Terrain.Transform.PointToLocal( point );
		return new Vector2( localPosition.x, localPosition.y );
	}

	public static Vector3 PointToWorld( Vector2 point )
	{
		var worldPosition = Terrain.Transform.PointToWorld( point );
		return new Vector3( worldPosition.x, Terrain.Position.y, worldPosition.z );
	}

	public static Vector3 PointToTop( Vector3 point, float traceSize = 24f )
	{
		var worldPosition = point.WithY( Terrain.Position.y );
		var testTrace = Trace.Ray( worldPosition.WithZ( 9999f ), worldPosition.WithZ( -9999f ) ) // TODO: Put min max when it's an option
			.WithTag( "terrain" )
			.Size( traceSize )
			.Run();

		return testTrace.HitPosition;
	}
	public static Vector3 PointToTop( Vector2 point, float traceSize = 24f ) => PointToTop( PointToWorld( point ), traceSize );

	public static void CarveCircle( Vector2 position, float radius ) => Terrain?.Subtract( new CircleSdf( position, radius ), TerrainLayer );
	public static void CarveCircle( Vector3 position, float radius ) => CarveCircle( PointToLocal( position ), radius );

	public static void CarveBox( Vector2 min, Vector2 max, float cornerRadius = 0f ) => Terrain?.Subtract( new BoxSdf( min, max, cornerRadius ), TerrainLayer );
	public static void CarveBox( Vector3 min, Vector3 max, float cornerRadius = 0f ) => CarveBox( PointToLocal( min ), PointToLocal( max ), cornerRadius );

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		if ( Game.IsClient ) return;

		GenerateLevel();
	}

	[ConCmd.Admin( "testexplosion")]
	public static void TestExplosion()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Player;

		CarveCircle( pawn.Position, 50f );
	}
}

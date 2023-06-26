namespace BombSurvival;

public partial class BombSurvival
{
	public static Sdf2DWorld Terrain { get; set; }
	public static Sdf2DLayer TerrainLayer => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/fabric.sdflayer" );
	public static Sdf2DLayer HouseLayer => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/wood.sdflayer" );
	public static Sdf2DLayer GroundLayer => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/ground.sdflayer" );
	public static Sdf2DLayer ScorchLayer => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/scorch.sdflayer" );
	public static Texture TerrainTexture => Texture.Load( FileSystem.Mounted, "terrains/hill.png" );
	public static Texture HouseTexture => Texture.Load( FileSystem.Mounted, "terrains/house.png" );
	public static Texture GroundTexture => Texture.Load( FileSystem.Mounted, "terrains/ground.png" );

	[ConCmd.Admin( "regenerate_terrain" )]
	public static void GenerateLevel()
	{
		Terrain?.Clear();
		Terrain ??= new Sdf2DWorld
		{
			LocalRotation = Rotation.FromRoll( 90f )
		};
		var terrainSdf = new TextureSdf( TerrainTexture, 4, TerrainTexture.Width );
		var houseSdf = new TextureSdf( HouseTexture, 4, HouseTexture.Width );
		var groundSdf = new TextureSdf( GroundTexture, 4, GroundTexture.Width );

		Terrain?.Add( terrainSdf, TerrainLayer );
		Terrain?.Add( houseSdf, HouseLayer );
		Terrain?.Add( groundSdf, GroundLayer );

		GameTask.RunInThreadAsync( async () =>
		{
			await GameTask.Delay( 100 );
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

	public static void AddCircle( Vector2 position, float radius, Sdf2DLayer layer ) => Terrain?.Add( new CircleSdf( position, radius ), layer );
	public static void AddCircle( Vector3 position, float radius, Sdf2DLayer layer ) => AddCircle( PointToLocal( position ), radius, layer );
	public static void CarveCircle( Vector2 position, float radius ) => Terrain?.Subtract( new CircleSdf( position, radius ) );
	public static void CarveCircle( Vector3 position, float radius ) => CarveCircle( PointToLocal( position ), radius );

	public static void AddBox( Vector2 min, Vector2 max, Sdf2DLayer layer, float cornerRadius = 0f ) => Terrain?.Add( new RectSdf( min, max, cornerRadius ), layer );
	public static void AddBox( Vector3 min, Vector3 max, Sdf2DLayer layer, float cornerRadius = 0f ) => AddBox( PointToLocal( min ), PointToLocal( max ), layer, cornerRadius );
	public static void CarveBox( Vector2 min, Vector2 max, float cornerRadius = 0f ) => Terrain?.Subtract( new RectSdf( min, max, cornerRadius ) );
	public static void CarveBox( Vector3 min, Vector3 max, float cornerRadius = 0f ) => CarveBox( PointToLocal( min ), PointToLocal( max ), cornerRadius );

	public static void Explosion( Vector3 position, float size = 75f, float charSize = 100f )
	{
		CarveCircle( position, size );
		AddCircle( position, charSize, ScorchLayer );

		Particles.Create( "particles/explosion.vpcf", position )
			.Set( "size", charSize );

		Sound.FromWorld( "sounds/explosion.sound", position );
	}
	public static void Explosion( Vector2 position, float size = 75f ) => Explosion( PointToWorld( position ), size );

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		if ( Game.IsClient ) return;

		GenerateLevel();
	}

	[ConCmd.Admin( "testexplosion" )]
	public static void TestExplosion()
	{
		var pawn = ConsoleSystem.Caller.Pawn as Player;
		Explosion( pawn.CollisionWorldSpaceCenter );

	}
}

namespace BombSurvival;

public partial class BombSurvival
{
	public static string CurrentLevel { get; set; } = "house";
	public static Sdf2DWorld Terrain { get; set; }
	public static Sdf2DLayer GrassForeground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/grass_foreground.sdflayer" );
	public static Sdf2DLayer WoodForeground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/wood_foreground.sdflayer" );
	public static Sdf2DLayer DirtForeground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/dirt_foreground.sdflayer" );
	public static Sdf2DLayer ScorchForeground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/scorch_foreground.sdflayer" );
	public static Sdf2DLayer GrassBackground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/grass_background.sdflayer" );
	public static Sdf2DLayer WoodBackground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/wood_background.sdflayer" );
	public static Sdf2DLayer DirtBackground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/dirt_background.sdflayer" );
	public static Sdf2DLayer ScorchBackground => ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/scorch_background.sdflayer" );
	public static Texture GrassForegroundTexture => Texture.Load( FileSystem.Mounted, $"levels/{CurrentLevel}/grass_foreground.png" );
	public static Texture WoodForegroundTexture => Texture.Load( FileSystem.Mounted, $"levels/{CurrentLevel}/wood_foreground.png" );
	public static Texture DirtForegroundTexture => Texture.Load( FileSystem.Mounted, $"levels/{CurrentLevel}/dirt_foreground.png" );
	public static Texture GrassBackgroundTexture => Texture.Load( FileSystem.Mounted, $"levels/{CurrentLevel}/grass_background.png" );
	public static Texture WoodBackgroundTexture => Texture.Load( FileSystem.Mounted, $"levels/{CurrentLevel}/wood_background.png" );
	public static Texture DirtBackgroundTexture => Texture.Load( FileSystem.Mounted, $"levels/{CurrentLevel}/dirt_background.png" );

	[ConCmd.Admin( "regenerate_terrain" )]
	public static void GenerateLevel()
	{
		Terrain?.ClearAsync();
		Terrain ??= new Sdf2DWorld
		{
			LocalRotation = Rotation.FromRoll( 90f )
		};
		var terrainSdf = new TextureSdf( GrassForegroundTexture, 4, GrassForegroundTexture.Width );
		var houseSdf = new TextureSdf( WoodForegroundTexture, 4, WoodForegroundTexture.Width );
		var groundSdf = new TextureSdf( DirtForegroundTexture, 4, DirtForegroundTexture.Width );

		Terrain?.AddAsync( terrainSdf, GrassForeground );
		Terrain?.AddAsync( houseSdf, WoodForeground );
		Terrain?.AddAsync( groundSdf, DirtForeground );

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

	public static void AddCircle( Vector2 position, float radius, Sdf2DLayer layer ) => Terrain?.AddAsync( new CircleSdf( position, radius ), layer );
	public static void AddCircle( Vector3 position, float radius, Sdf2DLayer layer ) => AddCircle( PointToLocal( position ), radius, layer );
	public static void CarveCircle( Vector2 position, float radius ) => Terrain?.SubtractAsync( new CircleSdf( position, radius ) );
	public static void CarveCircle( Vector3 position, float radius ) => CarveCircle( PointToLocal( position ), radius );

	public static void AddBox( Vector2 min, Vector2 max, Sdf2DLayer layer, float cornerRadius = 0f ) => Terrain?.AddAsync( new RectSdf( min, max, cornerRadius ), layer );
	public static void AddBox( Vector3 min, Vector3 max, Sdf2DLayer layer, float cornerRadius = 0f ) => AddBox( PointToLocal( min ), PointToLocal( max ), layer, cornerRadius );
	public static void CarveBox( Vector2 min, Vector2 max, float cornerRadius = 0f ) => Terrain?.SubtractAsync( new RectSdf( min, max, cornerRadius ) );
	public static void CarveBox( Vector3 min, Vector3 max, float cornerRadius = 0f ) => CarveBox( PointToLocal( min ), PointToLocal( max ), cornerRadius );

	public static void Explosion( Vector3 position, float size = 75f, float charSize = 100f )
	{
		CarveCircle( position, size );
		AddCircle( position, charSize, ScorchForeground );

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

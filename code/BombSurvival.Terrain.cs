namespace BombSurvival;

public partial class BombSurvival
{
	public static string CurrentLevel { get; set; } = "house";
	public static Sdf2DWorld Foreground { get; set; }
	public static Sdf2DWorld Background { get; set; }
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
		Foreground?.ClearAsync();
		Foreground ??= new Sdf2DWorld
		{
			LocalRotation = Rotation.FromRoll( 90f )
		};

		var grassForeground = new TextureSdf( GrassForegroundTexture, 4, GrassForegroundTexture.Width );
		var woodForeground = new TextureSdf( WoodForegroundTexture, 4, WoodForegroundTexture.Width );
		var dirtForeground = new TextureSdf( DirtForegroundTexture, 4, DirtForegroundTexture.Width );

		Foreground?.AddAsync( grassForeground, GrassForeground );
		Foreground?.AddAsync( woodForeground, WoodForeground );
		Foreground?.AddAsync( dirtForeground, DirtForeground );

		Background?.ClearAsync();
		Background ??= new Sdf2DWorld
		{
			LocalRotation = Rotation.FromRoll( 90f )
		};

		var grassBackground = new TextureSdf( GrassBackgroundTexture, 4, GrassBackgroundTexture.Width );
		var woodBackground = new TextureSdf( WoodBackgroundTexture, 4, WoodBackgroundTexture.Width );
		var dirtBackground = new TextureSdf( DirtBackgroundTexture, 4, DirtBackgroundTexture.Width );

		Background?.AddAsync( grassBackground, GrassBackground );
		Background?.AddAsync( woodBackground, WoodBackground );
		Background?.AddAsync( dirtBackground, DirtBackground );

		GameTask.RunInThreadAsync( async () =>
		{
			await GameTask.Delay( 100 );
			Event.Run( "TerrainLoaded" );
		} );
	}

	public static Vector2 PointToLocal( Vector3 point )
	{
		var localPosition = Foreground.Transform.PointToLocal( point );
		return new Vector2( localPosition.x, localPosition.y );
	}

	public static Vector3 PointToWorld( Vector2 point )
	{
		var worldPosition = Foreground.Transform.PointToWorld( point );
		return new Vector3( worldPosition.x, Foreground.Position.y, worldPosition.z );
	}

	public static void AddCircle( Sdf2DWorld world, Vector2 position, float radius, Sdf2DLayer layer ) => world?.AddAsync( new CircleSdf( position, radius ), layer );
	public static void AddCircle( Sdf2DWorld world, Vector3 position, float radius, Sdf2DLayer layer ) => AddCircle( world, PointToLocal( position ), radius, layer );
	public static void CarveCircle( Sdf2DWorld world, Vector2 position, float radius ) => world?.SubtractAsync( new CircleSdf( position, radius ) );
	public static void CarveCircle( Sdf2DWorld world, Vector3 position, float radius ) => CarveCircle( world, PointToLocal( position ), radius );

	public static void AddBox( Sdf2DWorld world, Vector2 min, Vector2 max, Sdf2DLayer layer, float cornerRadius = 0f ) => world?.AddAsync( new RectSdf( min, max, cornerRadius ), layer );
	public static void AddBox( Sdf2DWorld world, Vector3 min, Vector3 max, Sdf2DLayer layer, float cornerRadius = 0f ) => AddBox( world, PointToLocal( min ), PointToLocal( max ), layer, cornerRadius );
	public static void CarveBox( Sdf2DWorld world, Vector2 min, Vector2 max, float cornerRadius = 0f ) => world?.SubtractAsync( new RectSdf( min, max, cornerRadius ) );
	public static void CarveBox( Sdf2DWorld world, Vector3 min, Vector3 max, float cornerRadius = 0f ) => CarveBox( world, PointToLocal( min ), PointToLocal( max ), cornerRadius );

	public static void Explosion( Vector3 position, float size = 75f, float charSize = 100f )
	{
		CarveCircle( Foreground, position, size );
		AddCircle( Foreground, position, charSize, ScorchForeground );

		CarveCircle( Background, position, size * 0.6f );
		AddCircle( Background, position, charSize * 0.8f, ScorchBackground );

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

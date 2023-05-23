using Sandbox;
using Sandbox.Sdf;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Sandbox;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class MyGame : GameManager
{
	public MyGame()
	{
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Pawn();
		client.Pawn = pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		if ( Game.IsClient ) return;

		var sdfWorld = new Sdf2DWorld
		{
			// Rotate so that Y is up
			LocalRotation = Rotation.FromRoll( 90f )
		};

		var material = ResourceLibrary.Get<Sdf2DLayer>( "sdflayers/sand.sdflayer" );

		var terrainTexture = Texture.Load( FileSystem.Mounted, "terrains/hill.png" );
		var terrainSdf = new TextureSdf( terrainTexture, 4, terrainTexture.Width * 4 );

		sdfWorld.Add( terrainSdf, material );

		/*var worldBox = new BoxSdf( new Vector2( -500f, 1000f ), new Vector2( 500f, 2000f ) );
		sdfWorld.Add( worldBox, material );


		var newBox = new BoxSdf( new Vector2( -250f, 1250f ), new Vector2( 250f, 1750f ) );
		sdfWorld.Subtract( newBox, material );
		sdfWorld.Subtract( newBox.Translate( new Vector3( 300f, 0f )), material );*/
	}
}

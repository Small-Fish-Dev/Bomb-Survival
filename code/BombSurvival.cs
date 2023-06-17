global using Sandbox;
global using Sandbox.Sdf;
global using Sandbox.UI.Construct;
global using System;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
using System.Numerics;

namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public static BombSurvival Instance { get; private set; }

	public BombSurvival()
	{
		Instance = this;

		Game.TickRate = 60;
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new Player();
		client.Pawn = pawn;

		var clothing = new ClothingContainer();
		clothing.LoadFromClient( client );
		clothing.DressEntity( pawn.ServerPuppet );

	}

	[GameEvent.Tick.Server]
	public static void SpawnBombs()
	{
		var frequency = (int)(Time.Now / 60) + 1;

		if ( Time.Tick % ( 60 / frequency ) == 0 )
		{
			var spawnPosition = new Vector3( Game.Random.Float( -950f, 950f ), 0f, 1200f );

			if ( Game.Random.Int( 10 ) <= 4 )
			{
				new TimedBomb
				{
					Position = spawnPosition,
					Rotation = Rotation.FromYaw( -90 ),
					Scale = Game.Random.Float( 0.8f, 1f )
				};
			}
			else
			{
				new InertBomb
				{
					Position = spawnPosition,
					Rotation = Rotation.FromYaw( -90 ),
					Scale = Game.Random.Float( 0.8f, 1f )
				};
			}
		}

		if ( Time.Tick % (60 / frequency ) == 0 )
		{
			var spawnPosition = new Vector3( Game.Random.Float( -950f, 950f ), 0f, 1200f );

			new ScoreBubble
			{
				Position = spawnPosition,
				Rotation = Rotation.FromYaw( -90 )
			};
		}
	}
}

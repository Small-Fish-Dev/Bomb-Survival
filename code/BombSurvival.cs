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
		pawn.Respawn();
		pawn.Clothe( client );
	}

	TimeUntil nextWave = 5f;
	TimeUntil nextBomb = 0f;
	int bombsSpawned = 0;

	int[] bombWave = new int[6] { 0, 0, 2, 1, 2, 0 };

	[GameEvent.Tick.Server]
	public void SpawnBombs()
	{
		var bombSpawner = Entity.All.OfType<BombSpawner>().FirstOrDefault();
		var bombPosition = bombSpawner.GetBoneTransform( 1 ).Position.WithY( 0 );

		if ( nextWave )
		{
			if ( nextBomb )
			{
				var currentSpawn = bombWave[bombsSpawned];

				Entity toSpawn = currentSpawn switch
				{
					1 => new TimedBomb(),
					2 => new ScoreBubble(),
					_ => new InertBomb()
				};

				toSpawn.Position = bombPosition;
				toSpawn.Rotation = Rotation.FromYaw( -90f );

				nextBomb = 0.1f;
				bombsSpawned++;
			}

			if ( bombsSpawned >= bombWave.Length )
			{
				nextWave = Game.Random.Float( 3.5f, 6.5f );
				bombsSpawned = 0;
			}
		}
	}
}

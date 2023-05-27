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
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new Player();
		client.Pawn = pawn;

		var clothing = new ClothingContainer();
		clothing.LoadFromClient( client );
		clothing.DressEntity( pawn.Puppet );

	}

	[GameEvent.Physics.PreStep]
	public static void PreStep() // Lock the Y axis
	{
		if ( Game.IsClient ) return;

		foreach( var entity in Entity.All.OfType<ModelEntity>() )
		{
			if ( entity.PhysicsBody.IsValid() && entity.PhysicsBody.BodyType == PhysicsBodyType.Dynamic && entity.Owner is not Player )
			{
				if ( entity.PhysicsBody.Sleeping ) continue;

				entity.Position = entity.Position.WithY( 0 );
				entity.Rotation = Rotation.LookAt( entity.Rotation.Forward, Vector3.Right );
				entity.AngularVelocity = entity.AngularVelocity.WithRoll( 0 );
				entity.PhysicsBody.AngularDrag = 10f;
			}
		}
	}

	[GameEvent.Tick.Server]
	public static void SpawnBombs()
	{
		var frequency = (int)(Time.Now / 60) + 1;

		if ( Time.Tick % ( 60 / frequency ) == 0 )
		{
			var spawnPosition = new Vector3( Game.Random.Float( -950f, 950f ), 0f, 1200f );

			if ( Game.Random.Int( 2 ) == 0 )
			{
				new TimedBomb
				{
					Position = spawnPosition,
					Scale = Game.Random.Float( 0.8f, 1f )
				};
			}
			else
			{
				new InertBomb
				{
					Position = spawnPosition,
					Scale = Game.Random.Float( 0.8f, 1f )
				};
			}
		}

		if ( Time.Tick % (60 / frequency / 3 ) == 0 )
		{
			var spawnPosition = new Vector3( Game.Random.Float( -950f, 950f ), 0f, 1200f );

			new ScoreBubble
			{
				Position = spawnPosition,
			};
		}
	}
}

global using Sandbox;
global using Sandbox.Sdf;
global using System;
global using System.Linq;

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
		pawn.Respawn();
		pawn.Clothe( client );
	}
}

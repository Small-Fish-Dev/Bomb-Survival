global using Sandbox;
global using Sandbox.Sdf;
global using System;
global using System.Linq;
global using System.Collections.Generic;

namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public static BombSurvival Instance { get; private set; }

	public BombSurvival()
	{
		Instance = this;

		if ( Game.IsClient )
			_ = new HUD();
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new Player();
		client.Pawn = pawn;
		pawn.Clothing = client.GetClientData( "avatar" );
		pawn.PlayerColor = Player.FirstAvailableColor();
		pawn.Respawn();
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Sound.FromScreen( "sounds/music/fifth_of_beethoven.sound" );
	}
}

namespace BombSurvival;

public partial class TutorialState : GameState
{
	bool timedBombSpawned = false;
	public async override Task Start()
	{
		await base.Start();

		await BombSurvival.GenerateEmptyLevel();

		spawnHamsters();
		spawnMines();
		spawnBlockingBomb();
		spawnBombPool();
		spawnBot();

		foreach ( var player in Entity.All.OfType<Player>() )
		{
			player.Respawn();
			player.ResetScore();
			player.ResetLives();
		}

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		return;
	}

	public override void Compute()
	{
		base.Compute();

		if ( !timedBombSpawned )
		{
			var anyInZone = Entity.All.OfType<Player>()
				.Any( x => x.Position.Distance( new Vector3( -650f, 0f, 3850f ) ) <= 100f );

			if ( anyInZone )
			{
				new TimedBomb().Position = new Vector3( -320f, 0f, 3850f );
				timedBombSpawned = true;
			}
		}
	}

	public async override Task End()
	{
		await base.End();

		await BombSurvival.ClearLevel();

		foreach ( var hamster in Hamster.All.ToList() ) // Make a copy so we can safely remove from the original Hamster.All
			hamster.Delete();

		foreach ( var bomb in Entity.All.OfType<Bomb>() )
			bomb.Delete();

		foreach ( var puppet in Entity.All.OfType<Player>().Where( x => x.Client == null ) )
			puppet.Delete();

		timedBombSpawned = false;
	}

	void spawnHamsters()
	{
		new Hamster( new Vector3( -3340f, 0f, 3820f), 0.5f, "Welcome to Bomb Survival! Walk to the right for the tutorial." );
		new Hamster( new Vector3( -2930f, 0f, 3900f ), 0.5f, "You can hold your SPACE to jump higher!", 300f );
		new Hamster( new Vector3( -2330f, 0f, 3820f ), 0.5f, "You can dive where you're aiming by presshing SHIFT. Combine it with jumps to get more air!" );
		new Hamster( new Vector3( -1630f, 0f, 3820f ), 0.5f, "For small spaces, you can walk up to them and you'll crouch automatically. But you'll move slower!" );
		new Hamster( new Vector3( -840f, 0f, 3820f ), 0.5f, "In that pit you'll find bombs. You need to avoid them if you want to survive! The ones with a dial explode after some time." );
		new Hamster( new Vector3( 600f, 0f, 3820f ), 0.5f, "Path is blocked? You can grab by aiming and holding RIGHT to move objects out of the way." );
		new Hamster( new Vector3( 1970f, 0f, 3940f ), 0.5f, "It's dangerous down there. Punch this bot into those mines by aiming and pressing LEFT to open the way." );
		new Hamster( new Vector3( 3300f, 0f, 3820f ), 0.5f, "HELP ME!!!", 450f );
	}

	void spawnMines()
	{
		new Mine().Position = new Vector3( 2140f, 0f, 3720f );
		new Mine().Position = new Vector3( 2170f, 0f, 3720f );
		new Mine().Position = new Vector3( 2200f, 0f, 3720f );
		new Mine().Position = new Vector3( 2230f, 0f, 3720f );
		new Mine().Position = new Vector3( 2260f, 0f, 3720f );
		new Mine().Position = new Vector3( 2290f, 0f, 3720f );

		new Mine().Position = new Vector3( 2155f, 0f, 3745f );
		new Mine().Position = new Vector3( 2185f, 0f, 3745f );
		new Mine().Position = new Vector3( 2215f, 0f, 3745f );
		new Mine().Position = new Vector3( 2245f, 0f, 3745f );
		new Mine().Position = new Vector3( 2275f, 0f, 3745f );
	}

	void spawnBlockingBomb()
	{
		var bomb = new InertBomb();
		bomb.Position = new Vector3( 870f, 0f, 3750f );
		bomb.Scale = 1.6f;
	}

	void spawnBombPool()
	{
		new InertBomb().Position = new Vector3( -460f, 0f, 3735f );
		new InertBomb().Position = new Vector3( -390f, 0f, 3735f );
		new InertBomb().Position = new Vector3( -320f, 0f, 3735f );
		new InertBomb().Position = new Vector3( -250f, 0f, 3735f );
		new InertBomb().Position = new Vector3( -180f, 0f, 3735f );
	}

	void spawnBot()
	{
		new Player().Position = new Vector3( 2010f, 0f, 3845f );
		Log.Info( Game.IsServer );
	}
}

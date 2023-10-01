namespace BombSurvival;

public partial class TutorialState : GameState
{
	public async override Task Start()
	{
		await base.Start();

		await BombSurvival.GenerateEmptyLevel();

		SpawnHamsters();

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
	}

	public async override Task End()
	{
		await base.End();

		await BombSurvival.ClearLevel();

		foreach ( var hamster in Hamster.All.ToList() ) // Make a copy so we can safely remove from the original Hamster.All
			hamster.Delete();
	}

	public void SpawnHamsters()
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
}

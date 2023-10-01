namespace BombSurvival;

public partial class TutorialState : GameState
{
	public async override Task Start()
	{
		await base.Start();

		await BombSurvival.GenerateEmptyLevel();

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
}

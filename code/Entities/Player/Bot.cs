using Sandbox.Utility;

namespace BombSurvival;

public partial class BombSurvivalBot : Bot
{
	Random RNG;
	public Player Pawn => Client.Pawn as Player;

	public BombSurvivalBot()
	{
		var timeSeed = RealTime.GlobalNow + Time.Now + All.OfType<Player>().Count() + All.OfType<BombSurvivalBot>().Count() + BombSurvival.CurrentLevel.Length;
		RNG = new Random( (int)(timeSeed % int.MaxValue) );
	}

	public override void BuildInput()
	{
		if ( !Pawn.IsValid() ) return;

		//Input.SetAction( "Jump", true );
		Pawn.InputDirection = Vector3.Right;

		//TODO Set InputRotation
	}

	public override void Tick()
	{
		if ( !Pawn.IsValid() ) return;

		if ( Time.Tick % 10  == 0 )
			Compute();
	}

	public void Compute()
	{
		ComputeNavigation();
	}

	[ConCmd.Admin( "bs_bot_add" )]
	internal static void SpawnCustomBot()
	{
		Sandbox.Game.AssertServer();
		new BombSurvivalBot();
	}
}

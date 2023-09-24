using Sandbox.Utility;
using System.Diagnostics;

namespace BombSurvival;

public partial class BombSurvivalBot : Bot
{
	public Random RNG;
	public Player Pawn => Client.Pawn as Player;

	public BombSurvivalBot()
	{
		var timeSeed = RealTime.GlobalNow + Time.Now + All.OfType<Player>().Count() + All.OfType<BombSurvivalBot>().Count() + BombSurvival.CurrentLevel.Length;
		RNG = new Random( (int)(timeSeed % int.MaxValue) );
	}

	public override void BuildInput()
	{
		if ( !Pawn.IsValid() ) return;

		if ( IsFollowingPath && Pawn.GroundEntity != null )
			Pawn.InputDirection = MovingLeft ? Vector3.Left : Vector3.Right;
		else
			Pawn.InputDirection = Vector3.Zero;
	}

	public override void Tick()
	{
		if ( !Pawn.IsValid() ) return;

		if ( Pawn.Tags.Has( "player" ) )
		{
			Pawn.Tags.Remove( "player" );
			Pawn.Tags.Add( "bot" );
		}

		if ( BombSurvival.Instance.CurrentState is PlayingState )
			ComputeNavigation();

		if ( Time.Tick % 5  == 0 )
			Compute();
	}

	public void Compute()
	{
		ComputeRevenge();
		ComputeMoveToSafeLocation();
	}

	[ConCmd.Admin( "bs_bot_add" )]
	internal static void SpawnCustomBot()
	{
		Sandbox.Game.AssertServer();
		var bot = new BombSurvivalBot();
	}
}

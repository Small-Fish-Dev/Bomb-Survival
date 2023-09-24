using Sandbox.Utility;
using System.Diagnostics;

namespace BombSurvival;

public partial class BombSurvivalBot : Bot
{
	public double Seed = 0f;
	public Random RNG;
	public Player Pawn => Client.Pawn as Player;

	public BombSurvivalBot()
	{
		Seed = RealTime.GlobalNow + Time.Now + All.OfType<Player>().Count() + All.OfType<BombSurvivalBot>().Count() + BombSurvival.CurrentLevel.Length;
		RNG = new Random( (int)(Seed % int.MaxValue) );
		StartingKarma = RNG.Float( 0.05f, 0.2f );

		foreach ( var player in Entity.All.OfType<Player>() )
		{
			if ( !GrabKarma.ContainsKey( player ) )
				GrabKarma.Add( player, StartingKarma );
			if ( !PunchKarma.ContainsKey( player ) )
				PunchKarma.Add( player, StartingKarma );
		}
	}

	public Vector3 MoveDirection { get; set; } = Vector3.Zero;

	public override void BuildInput()
	{
		if ( !Pawn.IsValid() ) return;

		Pawn.InputDirection = MoveDirection;
	}

	public override void Tick()
	{
		if ( !Pawn.IsValid() ) return;

		if ( Pawn.Tags.Has( "player" ) ) // Not sure if I can put this in the constructor
		{
			Pawn.Tags.Remove( "player" );
			Pawn.Tags.Add( "bot" );
		}

		if ( BombSurvival.Instance.CurrentState is PlayingState )
			ComputeSurvivalNavigation();
		else
			ComputeFunnyRandomMovement();

		if ( Time.Tick % 5  == 0 )
			Compute();
	}

	public void Compute()
	{
		if ( BombSurvival.Instance.CurrentState is PlayingState )
		{
			ComputeMoveToSafeLocation();
			ComputeHomingMine();
		}

		ComputeRevenge();
	}

	[ConCmd.Admin( "bs_bot_add" )]
	internal static void SpawnCustomBot()
	{
		Sandbox.Game.AssertServer();
		var bot = new BombSurvivalBot();
	}
}

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
		if ( IsFollowingPath )
			Pawn.InputDirection = MovingLeft ? Vector3.Left : Vector3.Right;
		else
			Pawn.InputDirection = Vector3.Zero;

		var closestPlayer = Entity.All.OfType<Player>()
			.Where( x => x != Pawn )
			.OrderBy( x => x.Position.Distance( Pawn.Position ) )
			.FirstOrDefault();

		if ( closestPlayer != null )
		{
			if ( closestPlayer.Position.Distance( Pawn.Position ) <= 50f )
				Pawn.Punch();

			Pawn.InputRotation = Rotation.LookAt( (closestPlayer.Position - Pawn.Position).Normal, Vector3.Right );
		}
	}

	public override void Tick()
	{
		if ( !Pawn.IsValid() ) return;

		ComputeNavigation();
		if ( Time.Tick % 5  == 0 )
			Compute();
	}

	public void Compute()
	{
	}

	[ConCmd.Admin( "bs_bot_add" )]
	internal static void SpawnCustomBot()
	{
		Sandbox.Game.AssertServer();
		new BombSurvivalBot();
	}
}

using Sandbox.Utility;

namespace BombSurvival;

public class BombSurvivalBot : Bot
{
	[ConCmd.Admin( "bs_bot_add" )]
	internal static void SpawnCustomBot()
	{
		Sandbox.Game.AssertServer();
		new BombSurvivalBot();
	}

	private float randSeed;

	public BombSurvivalBot()
	{
	}

	public override void BuildInput()
	{
		if ( Client.Pawn is not Player player ) return;

		Input.SetAction( "Jump", true );
		//Input.AnalogMove = pawn.Rotation.Forward;

		//TODO Set InputRotation
	}

	public override void Tick()
	{
		if ( Client.Pawn is not Player player ) return;
	}
}

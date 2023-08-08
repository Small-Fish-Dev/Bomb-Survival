namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Player.MoveRagdolls();
	}
}

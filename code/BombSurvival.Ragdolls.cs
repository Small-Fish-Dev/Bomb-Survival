namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		foreach ( var player in Entity.All.OfType<Player>() )
		{
			if ( player.IsDead ) continue;
			if ( player.Client == Game.LocalClient ) continue;

			if ( player.Ragdoll.IsValid() && player.Ragdoll.PhysicsBody.IsValid() )
				player.MoveRagdoll();
		}
	}
}

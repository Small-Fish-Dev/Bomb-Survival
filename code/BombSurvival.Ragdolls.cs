namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		var players = Entity.All.OfType<Player>().ToList();

		foreach ( var player in players.Where( player => !player.Ragdoll.IsValid() || player.Ragdoll == null ) )
		{
			player.SpawnRagdoll();
			player.PlaceRagdoll();
			player.DressRagdoll();
			player.DrawRagdoll( true );
		}

		Player.MoveRagdolls();
	}
}

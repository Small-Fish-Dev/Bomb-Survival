namespace BombSurvival;

public partial class HomingMine : Bomb
{
	public override string ModelPath { get; } = "models/missile/missile.vmdl";
	public Player Target { get; set; } = null;

	public override void Spawn()
	{
		base.Spawn();
		UseAnimGraph = false;
		AnimateOnServer = true;
		PlaybackRate = 1;
		Scale = 0.3f;
	}

	[GameEvent.Tick.Server]
	internal virtual void SeekAndExplode()
	{
		if ( Target == null || Target.IsDead )
			Target = Game.Random.FromList( Entity.All.OfType<Player>().Where( x => !x.IsDead ).ToList() );

		if ( Target == null || Target.IsDead ) return;

		var wishDirection = (Target.Position - Position ).Normal;
		var wishRotation = Rotation.LookAt( wishDirection, Vector3.Right );

		Rotation = Rotation.Lerp( Rotation, wishRotation, Time.Delta * 10f );
		Position += Rotation.Forward * Time.Delta * 500f;
	}
}

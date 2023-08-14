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

		PhysicsBody.GravityEnabled = false;
	}

	[GameEvent.Tick.Server]
	internal virtual void SeekAndExplode()
	{
		if ( Target == null || Target.IsDead )
			Target = Player.GetLongestLiving();

		if ( Target != null && !Target.IsDead )
		{
			var wishDirection = (Target.Position.WithY( 0 ) - Position.WithY( 0 )).Normal;
			var traceDistance = 300f * Scale;

			var rightTrace = Trace.Sphere( 30 * Scale, Position, Position + (Rotation.Left - Rotation.LookAt( Rotation.Right, Vector3.Forward ).Up * 0.6f).Normal * traceDistance )
				.WithTag( "terrain" )
				.Ignore( this )
				.Run();

			var leftTrace = Trace.Sphere( 30 * Scale, Position, Position + (Rotation.Left + Rotation.LookAt( Rotation.Right, Vector3.Forward ).Up * 0.6f).Normal * traceDistance )
				.WithTag( "terrain" )
				.Ignore( this )
				.Run();

			if ( rightTrace.Hit )
				wishDirection = ( wishDirection + Rotation.LookAt( wishDirection, Vector3.Forward ).Up * ( ( rightTrace.Distance / traceDistance ) * 2 ) ).Normal;

			SetBodyGroup( "sensor_bot", rightTrace.Hit ? 1 : 0 );

			if ( leftTrace.Hit )
				wishDirection = ( wishDirection - Rotation.LookAt( wishDirection, Vector3.Forward ).Up * ( ( leftTrace.Distance / traceDistance ) * 2 ) ).Normal;

			SetBodyGroup( "sensor_top", leftTrace.Hit ? 1 : 0 );

			var wishRotation = Rotation.LookAt( wishDirection, Vector3.Right ) * Rotation.FromYaw( -90 );

			Rotation = Rotation.Lerp( Rotation, wishRotation, Time.Delta * 3f );
		}

		if ( PhysicsBody.IsValid() )
		{
			PhysicsBody.LinearDamping = 3f;
			PhysicsBody.ApplyForce( PhysicsBody.Mass * Rotation.Left * Time.Delta * 30000f );
		}
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );

		var other = eventData.Other.Entity;

		if ( Transform.PointToLocal( eventData.Position ).y > 50f )
		{
			if ( other.GetPlayer() is Player )
				Explode();
			else
				if ( Velocity.Length > 50f )
				Explode();
		}
	}
}

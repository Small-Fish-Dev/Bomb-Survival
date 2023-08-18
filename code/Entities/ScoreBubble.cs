namespace BombSurvival;

public partial class ScoreBubble : AnimatedEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/score_bubble/score_bubble.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		Scale = 2f;
		Tags.Add( "bubble" );

		BombSurvival.AxisLockedEntities.Add( this );

		UseAnimGraph = false;
		AnimateOnServer = false;
		PlaybackRate = 1;
	}

	protected override void OnDestroy()
	{
		BombSurvival.AxisLockedEntities.Remove( this );
		base.OnDestroy();
	}

	public void Break()
	{
		var particle = Particles.Create( "models/score_bubble/particles/score_bubble_break.vpcf", Position );
		particle.Set( "scale", Scale );

		Delete();
	}

	public void Award( Player player )
	{
		player.AssignPoints( 10 );
		Break();
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( Game.IsClient ) return;

		if ( other.GetPlayer() is Player player )
			Award( player );
	}

	[GameEvent.Tick.Client]
	void playerInteract()
	{
		var timeOffset = Time.Tick + NetworkIdent; // Offset the timer so you don't have visible lag spikes

		if ( timeOffset % 10 == 0 ) // Call this expensive code once every 10 ticks
		{
			var nearestPlayer = All.OfType<Player>()
				.Where( x => !x.IsDead )
				.OrderBy( x => x.Position.Distance( Position ) )
				.FirstOrDefault();

			if ( !nearestPlayer.IsValid() || nearestPlayer == null ) return;

			var distance = nearestPlayer.Position.Distance( Position );

			PlaybackRate = Math.Clamp( 5f - distance / 100f, 1f, 5f );

			Model.Materials.Last().Set( "g_vColorTint", nearestPlayer.PlayerColor );
		}
	}
}

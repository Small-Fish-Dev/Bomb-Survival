namespace BombSurvival;

public partial class ScoreBubble : AnimatedEntity, IBlowable, ICharrable
{
	Player nearestPlayer = null;

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

	public void Blow() => Break();
	public void Char() => SetMaterialOverride( ICharrable.CharMaterial );

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

		if ( timeOffset % 15 == 0 ) // Call this expensive code once every 15 ticks
		{
			var currentNearestPlayer = All.OfType<Player>()
				.Where( x => !x.IsDead )
				.OrderBy( x => x.Position.Distance( Position ) )
				.FirstOrDefault();

			if ( !currentNearestPlayer.IsValid() || currentNearestPlayer == null ) return;

			var distance = currentNearestPlayer.Position.Distance( Position );

			PlaybackRate = Math.Clamp( 10f - distance / 50f, 1f, 10f );

			if ( nearestPlayer == null || currentNearestPlayer != nearestPlayer )
			{
				var material = Model.Materials.Last().CreateCopy();

				material.Set( "g_vColorTint", currentNearestPlayer.PlayerColor );
				SetMaterialOverride( material, "color" );

				nearestPlayer = currentNearestPlayer;
			}
		}
	}
}

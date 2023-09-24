using Sandbox.Utility;

namespace BombSurvival;

public partial class BombSurvivalBot : Bot
{
	public float StartingKarma = 0f;

	public Dictionary<Player, float> PunchKarma { get; internal set; } = new Dictionary<Player, float>();
	TimeSince lastPunchCheck = 0f;
	public Dictionary<Player, float> GrabKarma { get; internal set; } = new Dictionary<Player, float>();
	TimeSince lastGrabCheck = 0f;

	public virtual void ComputeRevenge()
	{
		var closestPlayer = Entity.All.OfType<Player>()
			.Where( x => x != Pawn )
			.OrderBy( x => x.CollisionTop.Distance( Pawn.CollisionTop ) )
			.FirstOrDefault();

		if ( closestPlayer.IsValid() )
		{
			if ( closestPlayer.CollisionTop.Distance( Pawn.CollisionTop ) <= 50f )
				if ( lastPunchCheck > 1f )
				{
					if ( PunchKarma.TryGetValue( closestPlayer, out var punchKarma ) )
					{
						var randomRoll = RNG.Float();
						if ( randomRoll < punchKarma )
						{
							Pawn.InputRotation = Rotation.LookAt( (closestPlayer.CollisionTop - Pawn.CollisionTop).Normal, Vector3.Right );
							Pawn.Punch();
						}
					}

					lastPunchCheck = 0f;
				}
		}
	}

	TimeUntil nextSafePositionCheck = 0f;

	public virtual void ComputeMoveToSafeLocation()
	{
		if ( nextSafePositionCheck )
		{
			if ( BombSurvival.GetHeat( Pawn.Position ) > BombSurvival.HeatTenPercentile ) // If the bot is currently in an unsafe position
				if ( !IsFollowingPath || BombSurvival.GetHeat( TargetPosition ) > BombSurvival.HeatTenPercentile ) // And it's not following a path OR the destination isn't safe anymore
				{
					var randomSafeCell = BombSurvival.GetRandomSafeCell( RNG );
					TargetPosition = randomSafeCell.Position; // Find a new safe destination
				}

			nextSafePositionCheck = RNG.Float( 0.5f, 0.7f );
		}
	}

	public virtual void ComputeHomingMine()
	{
		var closestMissile = Entity.All.OfType<HomingMine>()
			.Where( x => x.Target == Pawn )
			.OrderBy( x => x.Position.Distance( Pawn.CollisionTop ) )
			.FirstOrDefault();

		if ( closestMissile.IsValid() )
			if ( closestMissile.Position.Distance( Pawn.CollisionTop ) <= 70f )
			{
				Pawn.InputRotation = Rotation.LookAt( (closestMissile.Position - Pawn.CollisionTop).Normal, Vector3.Right );
				Pawn.Punch();
			}
	}

	public virtual void OnRespawn() { }

	public virtual void OnKnockout() { }

	public virtual void OnPunch( Player puncher )
	{
		var karmaAdded = 0.1f;

		if ( PunchKarma.ContainsKey( puncher ) )
			PunchKarma[puncher] = Math.Clamp( PunchKarma[puncher] + karmaAdded, 0f, 1f );
		else
			PunchKarma.Add( puncher, StartingKarma + karmaAdded );
	}

	public virtual void OnGrab( Player grabber )
	{
		var karmaAdded = 0.1f;

		if ( GrabKarma.ContainsKey( grabber ) )
			GrabKarma[grabber] = Math.Clamp( GrabKarma[grabber] + karmaAdded, 0f, 1f );
		else
			GrabKarma.Add( grabber, StartingKarma + karmaAdded );
	}
}

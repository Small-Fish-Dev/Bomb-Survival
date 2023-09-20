using Sandbox.Utility;

namespace BombSurvival;

public abstract partial class BotBehaviour
{
	public TimeSince SinceStarted { get; set; } = 0f;
	public Player Pawn { get; internal set; }
	public BombSurvivalBot Bot { get; internal set; }
	public Player ClosestPlayer { get; internal set; }

	public Dictionary<Player, float> PunchKarma { get; internal set; } = new Dictionary<Player, float>();
	TimeSince lastPunchCheck = 0f;
	public Dictionary<Player, float> GrabKarma { get; internal set; } = new Dictionary<Player, float>();
	TimeSince lastGrabCheck = 0f;

	public virtual void Compute()
	{
		var ClosestPlayer = Entity.All.OfType<Player>()
			.Where( x => x != Pawn )
			.OrderBy( x => x.Position.Distance( Pawn.Position ) )
			.FirstOrDefault();

		if ( ClosestPlayer.IsValid() )
		{
			if ( ClosestPlayer.Position.Distance( Pawn.Position ) <= 50f )
				if ( lastPunchCheck > 1f )
				{
					if ( PunchKarma.TryGetValue( ClosestPlayer, out var punchKarma ) )
					{
						var randomRoll = Bot.RNG.Float();
						if ( randomRoll < punchKarma )
						{
							Pawn.InputRotation = Rotation.LookAt( (ClosestPlayer.Position - Pawn.Position).Normal, Vector3.Right );
							Pawn.Punch();
						}
					}

					lastPunchCheck = 0f;
				}
		}
	}
	public virtual void Start() { }
	public virtual void End() { }
	public virtual void OnRespawn() { }
	public virtual void OnKnockout() { }
	public virtual void OnPunch( Player puncher )
	{
		var karmaAdded = 0.1f;

		if ( PunchKarma.ContainsKey( puncher ) )
			PunchKarma[puncher] = Math.Clamp( PunchKarma[puncher] + karmaAdded, 0f, 1f );
		else
			PunchKarma.Add( puncher, karmaAdded );
	}
	public virtual void OnGrab( Player grabber ) 
	{
		var karmaAdded = 0.1f;

		if ( GrabKarma.ContainsKey( grabber ) )
			GrabKarma[grabber] = Math.Clamp( GrabKarma[grabber] + karmaAdded, 0f, 1f );
		else
			GrabKarma.Add( grabber, karmaAdded );
	}
}

public partial class WanderingBehaviour : BotBehaviour
{
	TimeUntil nextTargetPosition = 0f;

	public override void Compute()
	{
		base.Compute();

		if ( nextTargetPosition )
		{
			var randomCell = BombSurvival.GetRandomSafeCell( Bot.RNG );
			Bot.TargetPosition = randomCell.Position;

			nextTargetPosition = Bot.RNG.Float( 6f, 8f );
		}
	}
}

public partial class FollowingBehaviour : BotBehaviour
{
	public override void Compute()
	{
		base.Compute();

		Bot.TargetEntity = Game.Clients.First().Pawn as Entity;
	}
}

public partial class BombSurvivalBot : Bot
{
	public BotBehaviour CurrentBehaviour { get; private set; }

	public void SetBehaviour<T>() where T : BotBehaviour
	{
		CurrentBehaviour?.End();
		CurrentBehaviour = Activator.CreateInstance<T>();
		CurrentBehaviour.Bot = this;
		CurrentBehaviour.Pawn = Pawn;
		CurrentBehaviour.Start();
	}
}

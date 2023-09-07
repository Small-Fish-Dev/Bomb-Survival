using Sandbox.Utility;

namespace BombSurvival;

public abstract partial class BotBehaviour
{
	public TimeSince SinceStarted { get; set; } = 0f;
	public Player Pawn { get; internal set; }
	public BombSurvivalBot Bot { get; internal set; }

	public Dictionary<Player, float> PunchKarma { get; internal set; } = new Dictionary<Player, float>();
	public Dictionary<Player, float> GrabKarma { get; internal set; } = new Dictionary<Player, float>();

	public virtual void Compute() { }
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

}

public partial class FollowingBehaviour : BotBehaviour
{
	public override void Compute()
	{
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

using Sandbox.Utility;

namespace BombSurvival;

public abstract partial class BotBehaviour
{
	public TimeSince SinceStarted { get; set; } = 0f;
	public Player Pawn { get; internal set; }
	public BombSurvivalBot Bot { get; internal set; }

	public virtual void Compute() { }
	public virtual void Start() { }
	public virtual void End() { }
	public virtual void OnPunch( Player puncher ) { }
	public virtual void OnKnockout() { }
	public virtual void OnGrab( Player grabber ) { }
	public virtual void OnRespawn() { }
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

using Sandbox.Utility;

namespace BombSurvival;

public abstract partial class BotBehaviour
{
	public TimeSince SinceStarted { get; set; } = 0f;

	public virtual void Compute() { }
	public virtual void Start() { }
	public virtual void End() { }
	public virtual void OnPunch( Player puncher ) { }
	public virtual void OnKnockout( Entity source ) { }
	public virtual void OnGrab( Player grabber ) { }
	public virtual void OnRespawn() { }
}

public partial class BombSurvivalBot : Bot
{
	public BotBehaviour CurrentBehaviour { get; set; }

	public void SetBehaviour<T>() where T : BotBehaviour
	{
		CurrentBehaviour?.End();
		CurrentBehaviour = Activator.CreateInstance<T>();
		CurrentBehaviour.Start();
	}
}

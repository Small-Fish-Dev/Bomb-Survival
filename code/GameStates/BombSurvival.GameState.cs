using System.Collections.Generic;

namespace BombSurvival;

public abstract partial class GameState : Entity // BaseNetworkable sucks
{
	public GameState() => Transmit = TransmitType.Always;
	[Net] public TimeSince SinceStarted { get; set; } = 0f;

	[GameEvent.Tick.Server]
	public virtual void Compute() { }

	public virtual void Start() { }
	public virtual void End() { }
}

public partial class BombSurvival
{
	[Net, Change] public GameState CurrentState { get; set; }

	public static void SetState<T>() where T : GameState
	{
		Instance.CurrentState?.End();
		Instance.CurrentState?.Delete();

		Instance.CurrentState = Activator.CreateInstance<T>();
		Instance.CurrentState.Start();
	}

	public void OnCurrentStateChanged()
	{
	}

	[GameEvent.Entity.PostSpawn]
	static void startStates()
	{
		SetState<PodState>();
	}
}

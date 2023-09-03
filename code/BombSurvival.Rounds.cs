using System.Collections.Generic;

namespace BombSurvival;

public abstract partial class GameState : Entity // BaseNetworkable sucks
{
	[Net] public TimeSince SinceStarted { get; set; } = 0f;

	[GameEvent.Tick.Server]
	public virtual void Compute() { }

	public virtual void Start() { }
	public virtual void End() { }
}

public partial class PodState : GameState
{
	public override void Compute()
	{
		base.Compute();
	}
}

public partial class StartingState : GameState
{
	public override void Compute()
	{
		base.Compute();

		if ( SinceStarted >= 5f )
			BombSurvival.SetState<PlayingState>();
	}
}

public partial class PlayingState : GameState
{
	TimeUntil assignPoints = 1;
	TimeUntil nextGridRegen = 1;

	public override void Start()
	{
		base.Start();

		BombSurvivalBot.CancelAllTokens();
		BombSurvival.GenerateGrid().Wait();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();
	}

	public override void Compute()
	{
		base.Compute();

		var allPlayers = Entity.All.OfType<Player>();
		var playersAlive = allPlayers.Where( x => !x.IsDead );
		var playersPlaying = allPlayers.Where( x => x.LivesLeft >= 0 );

		foreach ( var player in playersAlive )
		{
			if ( assignPoints )
			{
				player.AssignPoints( 1 );
				assignPoints = 1;
			}
		}

		if ( playersAlive.Count() == 0 && playersPlaying.Count() == 0 )
			BombSurvival.SetState<ScoringState>();

		if ( nextGridRegen )
		{
			//BombSurvivalBot.CancelAllTokens();
			GameTask.RunInThreadAsync( async () => await BombSurvival.GenerateGrid() );
			nextGridRegen = 2;
		}

		BombSurvival.ComputeWaves();
	}
}

public partial class ScoringState : GameState
{
	public override void Start()
	{
		base.Start();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		if ( Game.IsServer )
			Player.SendScores();
	}
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

	[GameEvent.Client.Frame]
	static void frame()
	{
		DebugOverlay.ScreenText( $"{Instance.CurrentState} state" );
	}
	
	[GameEvent.Entity.PostSpawn]
	static void startStates()
	{
		SetState<PodState>();
	}
}

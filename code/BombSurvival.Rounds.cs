using System.Collections.Generic;

namespace BombSurvival;

public enum GameState
{
	Starting,
	Playing,
	Ending
}


public partial class BombSurvival
{
	[Net, Change] public GameState CurrentState { get; private set; } = GameState.Playing;

	public static Dictionary<GameState, Action> StateActions = new()
	{
		{ GameState.Starting, () => StartingTick() },
		{ GameState.Playing, () => PlayingTick() },
		{ GameState.Ending, () => EndingTick() }
	};

	public void OnCurrentStateChanged( GameState oldState, GameState newState ) => Event.Run( "game.state.changed", oldState, newState );

	public static void ChangeState( GameState newState )
	{
		Event.Run( "game.state.changed", Instance.CurrentState, newState );
		Instance.CurrentState = newState;
	}

	[GameEvent.Tick.Server]
	public static void GameTick() => StateActions[Instance.CurrentState].Invoke();

	public static void StartingTick()
	{
	}

	static TimeUntil assignPoints = 1;

	public static void PlayingTick()
	{
		foreach ( var player in Entity.All.OfType<Player>() )
		{
			if ( player.IsDead ) continue;

			if ( assignPoints )
			{
				player.AssignPoints( 1 );
				assignPoints = 1;
			}

		}

		ComputeWaves();
	}

	public static void EndingTick()
	{
	}
}

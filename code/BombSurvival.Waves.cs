using System.Collections.Generic;

namespace BombSurvival;

public enum WaveEntity
{
	InertBomb,
	TimedBomb,
	ScoreBubble,
	SmallInertBomb,
	BigTimedBomb,
	SmallTimedBomb,
	Missile,
	Mine,
}

public partial class BombSurvival
{
	static float timeUntilMaxDifficulty => 60f * 2f; // 2 Minutes
	public static float CurrentDifficulty => Math.Min( BombSurvival.Instance.CurrentState.SinceStarted, timeUntilMaxDifficulty ) / timeUntilMaxDifficulty;

	public static Dictionary<WaveEntity, Func<(ModelEntity, float)>> WaveEntities = new()
	{
		{ WaveEntity.InertBomb, () => ( new InertBomb(), 0.1f ) },
		{ WaveEntity.TimedBomb, () => ( new TimedBomb(), 0.1f ) },
		{ WaveEntity.ScoreBubble, () => ( new ScoreBubble(), 0.04f ) },
		{ WaveEntity.SmallInertBomb, () =>
		{
			var bomb = new InertBomb();
			bomb.Scale = 0.5f;
			return ( bomb, 0.05f );
		} },
		{ WaveEntity.BigTimedBomb, () =>
		{
			var bomb = new TimedBomb();
			bomb.Scale = 1.2f;
			return ( bomb, 0.15f );
		} },
		{ WaveEntity.SmallTimedBomb, () =>
		{
			var bomb = new TimedBomb();
			bomb.Scale = 0.5f;
			return ( bomb, 0.4f );
		} },
		{WaveEntity.Missile, () => ( new HomingMine(), 1.5f ) },
		{WaveEntity.Mine, () => ( new Mine(), 0.35f ) }
	};

	public static (ModelEntity, float) CreateWaveEntity( WaveEntity type ) => WaveEntities[type].Invoke();

	public static List<List<WaveEntity>> Waves = new()
	{
		new() { WaveEntity.ScoreBubble, WaveEntity.InertBomb, WaveEntity.TimedBomb, WaveEntity.InertBomb, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble },
		new() { WaveEntity.InertBomb, WaveEntity.TimedBomb, WaveEntity.InertBomb },
		new() { WaveEntity.TimedBomb, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.TimedBomb, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble },
		new() { WaveEntity.TimedBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.InertBomb, WaveEntity.InertBomb },
		new() { WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.TimedBomb },
		new() { WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble },
		new() { WaveEntity.ScoreBubble, WaveEntity.InertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.InertBomb, WaveEntity.ScoreBubble },
		new() { WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb },
		new() { WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.BigTimedBomb, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble },
		new() { WaveEntity.Missile, WaveEntity.Missile, WaveEntity.Missile },
		new() { WaveEntity.SmallTimedBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallInertBomb }
	};

	static TimeUntil nextWave { get; set; } = 10f;

	static int currentWaveId = 0;
	static TimeUntil nextWaveEntity = 0f;
	static int waveEntititesSpawned = 0;

	static List<Vector3> currentHighestPoints = new();
	static TimeUntil nextDamnedMine = 4f;

	public static void ComputeWaves()
	{
		if ( !Bombs ) return;

		if ( nextWave )
		{
			if ( waveEntititesSpawned == 0 )
				currentWaveId = Game.Random.Int( 0, Waves.Count - 1 );

			var currentWave = Waves.ElementAt( currentWaveId );
			var currentWaveEntityId = currentWave[waveEntititesSpawned];

			if ( nextWaveEntity )
			{
				var spawner = BombSpawner.First();
				var spawnPosition = BombSpawner.FirstPosition().WithY( 0 );
				var currentWaveEntity = CreateWaveEntity( currentWaveEntityId );

				currentWaveEntity.Item1.Position = spawnPosition;
				currentWaveEntity.Item1.Rotation = Rotation.FromYaw( -90f );
				currentWaveEntity.Item1.Velocity = spawner.Velocity;

				nextWaveEntity = currentWaveEntity.Item2;
				waveEntititesSpawned++;
			}

			if ( waveEntititesSpawned >= currentWave.Count )
			{
				var timeRemoved = CurrentDifficulty * 2f;

				nextWave = Game.Random.Float( 2.5f, 3.5f ) - timeRemoved;
				waveEntititesSpawned = 0;
			}
		}

		if ( nextDamnedMine )
		{
			var spawnedPosition = BombSpawner.FirstPosition();

			if ( currentHighestPoints.Count() == 0)
				currentHighestPoints.AddRange( GetHighestPoints().Take( 5 ) );

			List<Vector3> usedPoints = new();

			foreach ( var point in currentHighestPoints )
			{
				var pointWithHeight = point.WithZ( spawnedPosition.z );
				if ( spawnedPosition.Distance( pointWithHeight ) <= 5f )
				{
					var damnedMine = new Mine();
					damnedMine.Position = pointWithHeight;

					usedPoints.Add( point );
				}
			}

			foreach ( var point in usedPoints )
				currentHighestPoints.Remove( point );

			if ( currentHighestPoints.Count() == 0 )
			{
				var timeRemoved = CurrentDifficulty * 5f;

				nextDamnedMine = Game.Random.Float( 5f, 8f ) - timeRemoved;
			}
		}
	}
}

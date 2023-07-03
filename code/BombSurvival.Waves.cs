using System.Collections.Generic;

namespace BombSurvival;

public enum WaveEntity
{
	InertBomb,
	TimedBomb,
	ScoreBubble,
	SmallInertBomb,
	BigTimedBomb,
	SmallTimedBomb
}

public partial class BombSurvival
{
	public static Dictionary<WaveEntity, Func<(ModelEntity, float)>> WaveEntities = new()
	{
		{ WaveEntity.InertBomb, () => ( new InertBomb(), 0.1f ) },
		{ WaveEntity.TimedBomb, () => ( new TimedBomb(), 0.1f ) },
		{ WaveEntity.ScoreBubble, () => ( new ScoreBubble(), 0.05f ) },
		{ WaveEntity.SmallInertBomb, () =>
		{
			var bomb = new InertBomb();
			bomb.Scale = 0.5f;
			return ( bomb, 0.05f );
		} },
		{ WaveEntity.BigTimedBomb, () =>
		{
			var bomb = new TimedBomb();
			bomb.Scale = 1.5f;
			return ( bomb, 0.15f );
		} },
		{ WaveEntity.SmallTimedBomb, () =>
		{
			var bomb = new TimedBomb();
			bomb.Scale = 0.5f;
			return ( bomb, 0.4f );
		} }
	};

	public static (ModelEntity, float) CreateWaveEntity( WaveEntity type ) => WaveEntities[type].Invoke();

	public static List<List<WaveEntity>> Waves = new()
	{
		new() { WaveEntity.ScoreBubble, WaveEntity.InertBomb, WaveEntity.TimedBomb, WaveEntity.InertBomb, WaveEntity.ScoreBubble },
		new() {  WaveEntity.InertBomb, WaveEntity.TimedBomb, WaveEntity.InertBomb },
		new() { WaveEntity.TimedBomb, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.TimedBomb },
		new() { WaveEntity.TimedBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.InertBomb, WaveEntity.InertBomb },
		new() { WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.TimedBomb },
		new() { WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble },
		new() { WaveEntity.ScoreBubble, WaveEntity.InertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.SmallInertBomb, WaveEntity.InertBomb, WaveEntity.ScoreBubble },
		new() { WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb, WaveEntity.SmallTimedBomb },
		new() { WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.BigTimedBomb, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble, WaveEntity.ScoreBubble }
	};

	static TimeUntil nextWave { get; set; } = 5f;

	static int currentWaveId = 0;
	static TimeUntil nextWaveEntity = 0f;
	static int waveEntititesSpawned = 0;

	public static void ComputeWaves()
	{
		if ( nextWave )
		{
			if ( waveEntititesSpawned == 0 )
				currentWaveId = Game.Random.Int( 0, Waves.Count - 1 );

			var currentWave = Waves.ElementAt( currentWaveId );
			var currentWaveEntityId = currentWave[waveEntititesSpawned];

			if ( nextWaveEntity )
			{
				var spawner = Entity.All.OfType<BombSpawner>().FirstOrDefault();
				var spawnPosition = spawner.GetBoneTransform( 1 ).Position.WithY( 0 );
				var currentWaveEntity = CreateWaveEntity( currentWaveEntityId );

				currentWaveEntity.Item1.Position = spawnPosition;
				currentWaveEntity.Item1.Rotation = Rotation.FromYaw( -90f );
				currentWaveEntity.Item1.Velocity = spawner.Velocity * 50f; // Eeeh because of the air drag it doesn't look right until I multiply it

				nextWaveEntity = currentWaveEntity.Item2;
				waveEntititesSpawned++;
			}

			if ( waveEntititesSpawned >= currentWave.Count )
			{
				nextWave = Game.Random.Float( 1.5f, 6.5f );
				waveEntititesSpawned = 0;
			}
		}
	}
}

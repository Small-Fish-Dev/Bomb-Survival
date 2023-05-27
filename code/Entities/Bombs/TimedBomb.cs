using Sandbox.Component;

namespace BombSurvival;

public partial class TimedBomb : Bomb
{
	public override string ModelPath { get; } = "models/bomb/placeholder_inert.vmdl";
	internal TimeUntil ExplosionTime { get; set; } = 0f;

	public override void Spawn()
	{
		base.Spawn();

		ExplosionTime = 8f;
	}

	[GameEvent.Tick.Server]
	public virtual void Timer()
	{
		if ( ExplosionTime )
		{
			Explode();
		}

		var glow = Components.GetOrCreate<Glow>();
		glow.Color = Color.Red;
		glow.Enabled = ExplosionTime.Fraction % ( 1.1f - ExplosionTime.Fraction ) < ( (1.1f - ExplosionTime.Fraction) / 2 );
	}
}

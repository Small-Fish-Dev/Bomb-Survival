using System.Collections.Generic;

namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public static List<ModelEntity> AxisLockedEntities = new List<ModelEntity>();

	[GameEvent.Physics.PreStep]
	public static void PreStep() // Lock the Y axis
	{
		if ( Game.IsClient ) return;

		foreach ( var entity in AxisLockedEntities.Where( entity => entity.PhysicsBody.IsValid() )
			         .Where( entity => !entity.PhysicsBody.Sleeping ) )
		{
			entity.Position = entity.Position.WithY( -5f );
			entity.Rotation = Rotation.FromAxis( Vector3.Right, entity.Rotation.Roll() )
				.RotateAroundAxis( Vector3.Up, -90f );
			entity.AngularVelocity = entity.AngularVelocity.WithRoll( 0 );
			entity.PhysicsBody.AngularDrag = 10f;
		}
	}
}

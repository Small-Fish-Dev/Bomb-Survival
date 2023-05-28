using System.Collections.Generic;

namespace BombSurvival;

public partial class BombSurvival : GameManager
{
	public static List<ModelEntity> AxisLockedEntities= new List<ModelEntity>();

	[GameEvent.Physics.PreStep]
	public static void PreStep() // Lock the Y axis
	{
		if ( Game.IsClient ) return;

		foreach( var entity in AxisLockedEntities )
		{
			if ( entity.PhysicsBody.Sleeping ) continue;

			entity.Position = entity.Position.WithY( 0 );
			entity.Rotation = Rotation.LookAt( entity.Rotation.Forward, Vector3.Right );
			entity.AngularVelocity = entity.AngularVelocity.WithRoll( 0 );
			entity.PhysicsBody.AngularDrag = 10f;
		}
	}
}

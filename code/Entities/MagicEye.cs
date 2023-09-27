using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/eye/eye.vmdl" )]
public partial class MagicEye : AnimatedEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/eye/eye.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	[GameEvent.Tick.Server]
	public void LookingTest()
	{
		var nearestPlayer = All.OfType<Player>()
			.Where( x => x.Position.Distance( Position ) <= 200f )
			.OrderBy( x => x.Position.Distance( Position ) )
			.FirstOrDefault();

		if ( nearestPlayer.IsValid() )
		{
			SetAnimParameter( "lookat", true );

			var localPosition = Transform.PointToLocal( nearestPlayer.CollisionTop );
			SetAnimParameter( "look_vector", localPosition );
		}
		else
			SetAnimParameter( "lookat", false );
	}
}

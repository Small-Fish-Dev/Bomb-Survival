using Editor;

namespace BombSurvival;

[HammerEntity]
[EditorModel( "models/eye/eye.vmdl" )]
public partial class MagicEye : AnimatedEntity, IBlowable, ICharrable
{
	[Net, Property]
	public Color EyeColor { get; set; } = Color.White;
	[Net, Property]
	public float Range { get; set; } = 200f;

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
			.Where( x => x.Position.Distance( Position ) <= Range )
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

		RenderColor = EyeColor;
	}

	public void Blow() => Delete();
	public void Char() => SetMaterialOverride( ICharrable.CharMaterial );
}

using Sandbox.Internal;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	public CitizenAnimationHelper Animations => new CitizenAnimationHelper( this );

	Vector3 currentLookAt = Vector3.Zero;
	public void ComputeAnimations()
	{
		var wishRotation = Rotation.LookAt( InputRotation.Forward.WithZ( 0f ) + Vector3.Right, Vector3.Up );
		Rotation = Rotation.Lerp( Rotation, wishRotation, Time.Delta * 10f );

		var animationHelper = new CitizenAnimationHelper( this );

		var wishLookAt = InputRotation.Forward + Vector3.Right;
		currentLookAt = Vector3.Lerp( currentLookAt, wishLookAt, Time.Delta * 5f );
		animationHelper.WithLookAt( Position + currentLookAt );

		animationHelper.WithVelocity( Velocity );
		animationHelper.IsGrounded = GroundEntity != null;
	}
}

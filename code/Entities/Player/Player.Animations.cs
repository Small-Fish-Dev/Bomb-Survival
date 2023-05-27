namespace BombSurvival;

public partial class Player
{
	public CitizenAnimationHelper Animations => new CitizenAnimationHelper( this );
	public CitizenAnimationHelper PuppetAnimations => new CitizenAnimationHelper( Puppet );

	Vector3 currentLookAt = Vector3.Zero;
	Vector3 nextLookAt = Vector3.Zero;

	public void ComputeAnimations()
	{
		var animationHelper = Animations;
		var puppetAnimationsHelper = PuppetAnimations;

		if ( InputRotation == new Rotation() )
			if ( !Velocity.IsNearlyZero( 1 ) )
				nextLookAt = Velocity.Normal;
			else
			 nextLookAt = Vector3.Zero;
		else
			nextLookAt = InputRotation.Forward;

		currentLookAt = Vector3.Lerp( currentLookAt, nextLookAt + Vector3.Right, Time.Delta * 5f );

		var wishRotation = Rotation.LookAt( currentLookAt.WithZ( 0f ), Vector3.Up );
		Rotation = Rotation.Lerp( Rotation, wishRotation, Time.Delta * 10f );

		animationHelper.WithLookAt( Position + currentLookAt );
		puppetAnimationsHelper.WithLookAt( Position + currentLookAt );

		animationHelper.WithVelocity( Velocity );
		animationHelper.IsGrounded = GroundEntity != null;
		puppetAnimationsHelper.WithVelocity( Velocity );
		puppetAnimationsHelper.IsGrounded = GroundEntity != null;

		animationHelper.HoldType = IsPunching ? CitizenAnimationHelper.HoldTypes.Punch : CitizenAnimationHelper.HoldTypes.None;
		puppetAnimationsHelper.HoldType = IsPunching ? CitizenAnimationHelper.HoldTypes.Punch : CitizenAnimationHelper.HoldTypes.None;
	}
}

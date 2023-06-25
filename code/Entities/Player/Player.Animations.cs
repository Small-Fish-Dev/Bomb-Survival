namespace BombSurvival;

public partial class Player
{
	public CitizenAnimationHelper Animations => new CitizenAnimationHelper( this );
	public CitizenAnimationHelper ServerPuppetAnimations => new CitizenAnimationHelper( ServerPuppet );

	Vector3 currentLookAt = Vector3.Zero;
	Vector3 nextLookAt = Vector3.Zero;

	public void ComputeAnimations()
	{
		var animationHelper = Animations;
		var puppetAnimationsHelper = ServerPuppetAnimations;

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

		SetAnimParameter( "b_vr", true );

		if ( Game.IsServer )
		{
			var startingOffset = Vector3.Up * CollisionHeight * Scale / 2f;
			var grabPosition = Position + InputRotation.Forward * 500f;
			var localPosition = Transform.PointToLocal( grabPosition + startingOffset );
			SetAnimParameter( "left_hand_ik.position", localPosition );
			SetAnimParameter( "right_hand_ik.position", localPosition );
		}
	}
}

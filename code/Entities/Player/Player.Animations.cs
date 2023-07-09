namespace BombSurvival;

public partial class Player
{
	Vector3 currentLookAt = Vector3.Zero;
	Vector3 nextLookAt = Vector3.Zero;

	public void ComputeAnimations( AnimatedEntity target )
	{
		var animationHelper = new CitizenAnimationHelper( target );

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

		animationHelper.WithVelocity( Velocity );
		animationHelper.IsGrounded = GroundEntity != null;

		if ( !target.GetAnimParameterBool( "b_attack" ) && IsPunching && animationHelper.HoldType != CitizenAnimationHelper.HoldTypes.Punch )
			target.SetAnimParameter( "b_attack", true );

		animationHelper.HoldType = IsPunching ? CitizenAnimationHelper.HoldTypes.Punch : CitizenAnimationHelper.HoldTypes.None;

		animationHelper.DuckLevel = 2 - CrouchLevel * 2f;

		if ( WantsToGrab && !IsPunching )
		{
			target.SetAnimParameter( "b_vr", true );

			var startingOffset = Vector3.Up * CollisionHeight * Scale / 2f;
			var grabPosition = InputRotation.Forward * 100f;
			var localPosition = Transform.PointToLocal( Position + grabPosition + startingOffset );
			target.SetAnimParameter( "left_hand_ik.position", localPosition + Vector3.Left * 50f );
			target.SetAnimParameter( "right_hand_ik.position", localPosition + Vector3.Right * 50f );

			var angleInRadians = Math.Atan2( InputRotation.Forward.z, InputRotation.Forward.x );
			var angleInDegrees = angleInRadians * (180 / Math.PI) + 180f;
			var pitch = ( 360 - (float)angleInDegrees + 180 ) % 360;
			var roll = ( 360 - (float)angleInDegrees - 90 ) % 360;
			var localRotation = Transform.RotationToLocal( Rotation.From( new Angles( pitch, 0f, roll ) ) );

			Log.Info( roll );
			target.SetAnimParameter( "left_hand_ik.rotation", localRotation );
			target.SetAnimParameter( "right_hand_ik.rotation", localRotation );
		}
		else
			target.SetAnimParameter( "b_vr", false );
	}
}

using Sandbox;
using Sandbox.Physics;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	internal ModelEntity Collider { get; private set; }

	internal void SpawnCollider()
	{
		Collider = new ModelEntity();
		Collider.SetModel( "models/editor/axis_helper_thick.vmdl_c" ); // Needs a model :)
		Collider.SetupPhysicsFromOrientedCapsule( PhysicsMotionType.Dynamic, new Capsule( Vector3.Up * CollisionWidth, Vector3.Up * ( CollisionHeight + CollisionWidth / 4f ), CollisionWidth / 1.5f ));

		Collider.PhysicsBody.Mass = 30f;
		Collider.Owner = this;

		Collider.EnableAllCollisions = true;
		Collider.EnableDrawing = false;
		Collider.Tags.Add( "collider" );

		PlaceCollider();

		PhysicsJoint.CreateSlider( new PhysicsPoint( Collider.PhysicsBody ), new PhysicsPoint( PhysicsBody, CollisionTopLocal ), 0f, CollisionHeight );
		var spring = PhysicsJoint.CreateSpring( new PhysicsPoint( Collider.PhysicsBody ), new PhysicsPoint( PhysicsBody, CollisionTopLocal ), 0f, 0f );
		spring.SpringLinear = new PhysicsSpring( 3f, 0.8f );
	}

	internal void PlaceCollider()
	{
		if ( !Collider.IsValid() ) return;

		Collider.Position = CollisionTop;
		Collider.Velocity = Velocity;
	}

	internal void MoveCollider()
	{
		if ( !Collider.IsValid() ) return;

		if ( Collider.Position.Distance( CollisionTop ) >= CollisionHeight * 2 )
			PlaceCollider();
	}
}

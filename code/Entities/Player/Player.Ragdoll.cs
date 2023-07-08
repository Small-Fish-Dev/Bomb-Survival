using Sandbox;
using Sandbox.Physics;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	internal AnimatedEntity Ragdoll { get; private set; }

	internal void SpawnRagdoll()
	{
		Ragdoll?.Delete();

		Ragdoll = new AnimatedEntity();
		Ragdoll.Tags.Add( "puppet", "solid" );
		Ragdoll.Position = Position;
		Ragdoll.Rotation = Rotation;
		Ragdoll.Scale = Scale;
		Ragdoll.UsePhysicsCollision = true;
		Ragdoll.EnableAllCollisions = true;
		Ragdoll.SetModel( GetModelName() );
		Ragdoll.CopyBonesFrom( this );
		Ragdoll.CopyBodyGroups( this );
		Ragdoll.CopyMaterialGroup( this );
		Ragdoll.CopyMaterialOverrides( this );
		Ragdoll.TakeDecalsFrom( this );
		Ragdoll.EnableAllCollisions = true;
		Ragdoll.SurroundingBoundsMode = SurroundingBoundsType.Physics;
		Ragdoll.RenderColor = RenderColor;
		Ragdoll.PhysicsEnabled = true;
		Ragdoll.PhysicsGroup.Velocity = Velocity;

		var spring = PhysicsJoint.CreateSpring( new PhysicsPoint( Ragdoll.PhysicsBody, Vector3.Down * CollisionHeight ), new PhysicsPoint( PhysicsBody ), 0f, 0f );
		spring.SpringLinear = new PhysicsSpring( 15f, 0.7f );
	}

	internal void PlaceRagdoll()
	{
		if ( !Ragdoll.IsValid() || !Ragdoll.PhysicsBody.IsValid() ) return;
		
		foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
		{
			var ragdollBone = GetBone( body );
			var boneTransform = GetBoneTransform( ragdollBone );

			body.Transform = boneTransform;
		}
	}

	internal void MoveRagdoll()
	{
		if ( !Ragdoll.IsValid() || !Ragdoll.PhysicsBody.IsValid() ) return;

		foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
		{
			var ragdollBone = GetBone( body );
			var boneTransform = GetBoneTransform( ragdollBone );

			var direction = (boneTransform.Position - body.Position).Normal;
			var distance = boneTransform.Position.Distance( body.Position );

			var damping = distance / 10f;
			var force = body.Mass * 100f * Time.Delta;

			body.ApplyForce( direction * force / damping );
		}
	}
}

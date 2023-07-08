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

		PlaceRagdoll();
	}

	public void DressRagdoll( IClient cl )
	{
		Game.AssertServer();

		var data = cl.GetClientData( "avatar" );
		dressRagdollClient( data );
	}

	[ClientRpc]
	private void dressRagdollClient( string data )
	{
		if ( !Ragdoll.IsValid() ) return;

		var container = new ClothingContainer();
		container.Deserialize( data );
		container.DressEntity( Ragdoll );
	}

	internal void PlaceRagdoll()
	{
		if ( !Ragdoll.IsValid() || !Ragdoll.PhysicsBody.IsValid() ) return;

		Ragdoll.ResetInterpolation();

		foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
			body.Position = Position;

		Ragdoll.ResetInterpolation(); // I always forget if I'm supposed to do it before or after
	}

	internal void MoveRagdoll()
	{
		if ( !Ragdoll.IsValid() || !Ragdoll.PhysicsBody.IsValid() ) return;

		foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
		{
			var ragdollBone = Ragdoll.GetBone( body );
			var boneTransform = GetBoneTransform( ragdollBone );

			var direction = boneTransform.Position - body.Position;
			var force = body.Mass * 500000f;

			body.ApplyForce( direction * force * Time.Delta );
			body.LinearDamping = 0.5f / Time.Delta;
			body.Rotation = boneTransform.Rotation;
		}
	}
}

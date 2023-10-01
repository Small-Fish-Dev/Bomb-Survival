using Sandbox;
using Sandbox.Physics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	internal AnimatedEntity Ragdoll { get; private set; }
	[Net] public string Clothing { get; set; }

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
		Ragdoll.UseAnimGraph = true;

		var spring = PhysicsJoint.CreateSpring( new PhysicsPoint( Ragdoll.PhysicsBody ), new PhysicsPoint( PhysicsBody, CollisionTopLocal ), 0f, 0f );
		spring.SpringLinear = new PhysicsSpring( 4f, 0.8f );
	}

	public void DressRagdoll()
	{
		if ( Client.IsBot )
		{
			var animatedModel = new AnimatedEntity();
			animatedModel.Model = Cloud.Model( "shadb.terryrobot" ); // Shout out ShadowBrain!

			for ( int bodyGroup = 0; bodyGroup <= 4; bodyGroup++ )
				Ragdoll.SetBodyGroup( bodyGroup, 1 );

			animatedModel.SetParent( Ragdoll, true );
		}
		else
		{
			var container = new ClothingContainer();
			container.Deserialize( Clothing );
			container.DressEntity( Ragdoll );
		}
	}

	public void DrawRagdoll( bool on )
	{
		Ragdoll.EnableDrawing = on;
		Ragdoll.Glow( on, PlayerColor );
	}

	internal void PlaceRagdoll()
	{
		if ( !Ragdoll.IsValid() || !Ragdoll.PhysicsBody.IsValid() ) return;

		foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
		{
			var ragdollBone = Ragdoll.GetBone( body );
			var boneTransform = GetBoneTransform( ragdollBone );

			body.Position = Position;
			body.Rotation = boneTransform.Rotation;
		}
	}

	internal void MoveRagdoll()
	{
		if ( !Ragdoll.IsValid() || !Ragdoll.PhysicsBody.IsValid() ) return;

		if ( !IsKnockedOut )
		{
			var positionDifference = Ragdoll.GetBoneTransform( 0 ).Position - Ragdoll.PhysicsBody.Position;

			foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
			{
				var ragdollBone = Ragdoll.GetBone( body );
				var boneName = Ragdoll.GetBoneName( ragdollBone );

				if ( IsGrabbing && boneName.Contains( "hand" ) || IsGrabbing && boneName.Contains( "arm" ) )
				{
					if ( boneName.Contains( "hand" ) )
					{
						body.Enabled = false;
						body.Position = GrabbingPosition;
					}
					else
						body.Position = body.Position.LerpTo( GrabbingPosition, Time.Delta * 10f );
				}
				else
				{
					var boneTransform = GetBoneTransform( ragdollBone ).Add( positionDifference, true );

					var direction = boneTransform.Position - body.Position;
					var force = body.Mass * 30000f;

					body.ApplyForce( direction * force * Time.Delta );
					body.LinearDamping = 1000f * Time.Delta;
					body.Rotation = boneTransform.Rotation;
					body.Enabled = true;
				}
			}
		}
		else
		{
			foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
			{
				body.LinearDamping = 0f;
				body.Enabled = true;
			}

			if ( knockedOutTimer.Passed <= 0.1f )
			{
				Ragdoll.PhysicsGroup.Velocity = Velocity;
				Ragdoll.PhysicsGroup.AngularVelocity = Vector3.Right * (Velocity.x >= 0f ? -50f : 50f );
			}
		}
	}

	public static void MoveRagdolls()
	{
		foreach ( var player in Entity.All.OfType<Player>() )
		{
			if ( player.IsDead ) continue;
			if ( player.Client == Game.LocalClient ) continue;

			if ( player.Ragdoll.IsValid() && player.Ragdoll.PhysicsBody.IsValid() )
				player.MoveRagdoll();
		}
	}
}

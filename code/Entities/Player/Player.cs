namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	public float CollisionHeight => 30f;
	public float CollisionWidth => 24f;
	public BBox CollisionBox => new BBox( new Vector3( -CollisionWidth / 2f, -CollisionWidth / 2f, 0f ) * Scale, new Vector3( CollisionWidth / 2f, CollisionWidth / 2f, CollisionHeight ) * Scale );
	[Net] public bool IsDead { get; set; } = false;
	
	[Net] internal AnimatedEntity Puppet { get; set; }
	[Net] internal ModelEntity Collider { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		Puppet = new AnimatedEntity();
		Puppet.SetModel( "models/citizen/citizen.vmdl" );
		Puppet.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		Puppet.Tags.Add( "puppet" );
		Puppet.Owner = this;

		PlacePuppet();
		Puppet.EnableAllCollisions = true;
		Puppet.EnableDrawing = true;

		Collider = new ModelEntity();
		Collider.SetModel( "models/editor/axis_helper_thick.vmdl_c" );
		Collider.SetupPhysicsFromSphere( PhysicsMotionType.Dynamic, Vector3.Zero, CollisionHeight / 1.5f );
		Collider.Tags.Add( "puppet" );
		Puppet.Owner = this;

		PlaceCollider();
		Collider.EnableAllCollisions = true;
		Collider.EnableDrawing = false;

		EnableAllCollisions = true;
		EnableDrawing = false;

		Respawn();
	}

	public void Respawn()
	{
		var spawnPoint = Entity.All.OfType<Checkpoint>().FirstOrDefault();

		Position = spawnPoint.Position.WithY( 0 );
		Velocity = Vector3.Zero;

		PlacePuppet();
		Puppet.EnableAllCollisions = true;
		Puppet.EnableDrawing = true;

		PlaceCollider();
		Collider.EnableAllCollisions = true;

		EnableAllCollisions = true;

		IsDead = false;
	}

	public void Kill()
	{
		EnableAllCollisions = false;
		Puppet.EnableAllCollisions = false;
		Puppet.EnableDrawing = false;
		Collider.EnableAllCollisions = false;

		IsDead = true;

		GameTask.RunInThreadAsync( async () =>
		{
			await GameTask.Delay( 1000 );
			Respawn();
		} );
	}

	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Rotation InputRotation { get; set; }
	TimeSince lastRotation = 0f;

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var lookInput = Input.AnalogLook;
		var direction = new Vector3( -lookInput.yaw, 0f, -lookInput.pitch ).Normal;

		if ( lookInput != Angles.Zero )
		{
			InputRotation = Rotation.LookAt( direction, Vector3.Left );
			lastRotation = 0f;
		}
		else
			if ( lastRotation >= 0.5f )
				InputRotation = new Rotation();
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( IsDead ) return;

		ComputeAnimations();
		ComputeMotion();

		if ( Game.IsClient ) return;

		MovePuppet();
		MoveCollider();
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Camera.Position = Vector3.Lerp( Camera.Position, Position + Vector3.Right * 200f + Vector3.Up * 64f, Time.Delta * 5f );
		Camera.Rotation = Rotation.FromYaw( 90f );

		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		if ( IsDead ) return;

		ComputeAnimations();
	}

	public void PlacePuppet()
	{
		if ( Puppet == null ) return;
		Puppet.Position = Position;
	}

	public void MovePuppet()
	{
		if ( Puppet == null ) return;

		for ( int boneId = 0; boneId < BoneCount; boneId++ )
		{
			var puppetBoneBody = Puppet.GetBonePhysicsBody( boneId );
			var playerBoneTransform = GetBoneTransform( boneId );
			var boneName = GetBoneName( boneId );

			if ( puppetBoneBody.IsValid() )
			{
				if ( boneName.Contains( "ankle" ) )
					puppetBoneBody.Position = playerBoneTransform.Position;
				else
				{
					var moveDirection = playerBoneTransform.Position - puppetBoneBody.Position;
					puppetBoneBody.ApplyForce( moveDirection * 500000 * puppetBoneBody.Mass * Time.Delta );
					puppetBoneBody.LinearDamping = 30;
				}

				puppetBoneBody.Rotation = playerBoneTransform.Rotation;
			}
		}
	}
	public void PlaceCollider()
	{
		if ( Collider == null ) return;
		Collider.Position = Position + Vector3.Up * CollisionHeight / 1.5f;
	}

	public void MoveCollider()
	{
		if ( Collider == null ) return;

		var positionGoal = Position + Vector3.Up * CollisionHeight / 1.5f;
		var moveDirection = positionGoal - Collider.Position;
		Collider.PhysicsBody.ApplyForce( moveDirection * 300000 * Collider.PhysicsBody.Mass * Time.Delta );
		Collider.PhysicsBody.LinearDamping = 30;

		if ( Collider.Position.Distance( positionGoal ) >= CollisionHeight )
		{
			PlaceCollider();
		}
	}
}

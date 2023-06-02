namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	public float CollisionHeight => 30f;
	public float CollisionWidth => 24f;
	public BBox CollisionBox => new BBox( new Vector3( -CollisionWidth / 2f, -CollisionWidth / 2f, 0f ) * Scale, new Vector3( CollisionWidth / 2f, CollisionWidth / 2f, CollisionHeight ) * Scale );
	public Vector3 CollisionCenter => Position + Vector3.Up * CollisionHeight * Scale;
	public Vector3 CollisionTop => Position + Vector3.Up * CollisionHeight * Scale / 1.5f;
	[Net] public bool IsDead { get; set; } = false;
	[Net] internal TimeUntil knockedOutTimer { get; set; } = 0f;
	public bool IsKnockedOut => !knockedOutTimer;
	
	internal AnimatedEntity Puppet { get; set; }
	internal ModelEntity Collider { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		SpawnPuppet();
		SpawnCollider();

		EnableAllCollisions = true;
		EnableDrawing = false;

		Respawn();
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		SpawnPuppet();
		Respawn();
	}

	public void Respawn()
	{
		if ( Game.IsServer )
		{
			var spawnPoint = Entity.All.OfType<Checkpoint>().FirstOrDefault();

			Position = spawnPoint.Position.WithY( 0 );
			Velocity = Vector3.Zero;

			EnableAllCollisions = true;

			SetCharred( false );

			knockedOutTimer = 0f;

			IsDead = false;
		}

		if ( Puppet.IsValid() )
		{
			PlacePuppet();
			Puppet.EnableAllCollisions = true;
			Puppet.EnableDrawing = true;
		}

		if ( Collider.IsValid() )
		{
			PlaceCollider();
			Collider.EnableAllCollisions = true;
		}
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

	private float cameraDistance = 200f;
	private TimeSince lastMoved;

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		if ( Velocity.Length > 1f )
			lastMoved = 0f;
		
		if ( lastMoved > 2.5f )
			cameraDistance = cameraDistance.LerpTo( 100f, Time.Delta * lastMoved );
		else
			cameraDistance = cameraDistance.LerpTo( 200f + Velocity.Length * 0.15f, Time.Delta * 0.5f );

		Camera.Position = Vector3.Lerp( Camera.Position, Position + Vector3.Right * cameraDistance + Vector3.Up * 64f, Time.Delta * 5f );
		Camera.Rotation = Rotation.FromYaw( 90f );

		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		if ( IsDead ) return;

		ComputeAnimations();
		MovePuppet();
	}

	public void SetCharred( bool charred )
	{
		if ( !Puppet.IsValid ) return;
		
		var colorToApply = charred ? Color.Black : Color.White;

		Puppet.RenderColor = colorToApply;

		foreach ( var child in Puppet.Children )
			if ( child is ModelEntity clothing )
				clothing.RenderColor = colorToApply;
	}

	public void KnockOut( Vector3 sourcePosition, float strength, float amount )
	{
		if ( !Puppet.IsValid ) return;
		if ( IsDead ) return;

		knockedOutTimer = amount;

		var direction = ((CollisionCenter - sourcePosition).WithY( 0 ).Normal + Vector3.Up * 0.5f).Normal;

		Puppet.PhysicsGroup.Velocity = 0;
		Puppet.PhysicsGroup.ApplyImpulse( direction * strength );
	}

	internal void SpawnPuppet()
	{
		Puppet = new AnimatedEntity();
		Puppet.SetModel( "models/citizen/citizen.vmdl" );
		Puppet.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		Puppet.Tags.Add( "puppet" );
		Puppet.Owner = this;

		PlacePuppet();
		Puppet.EnableAllCollisions = true;
		Puppet.EnableDrawing = true;

		Puppet.Owner = this;
	}

	internal void PlacePuppet()
	{
		if ( !Puppet.IsValid() ) return;

		Puppet.Position = Position;
		Puppet.PhysicsGroup.Velocity = 0f;

		for ( int boneId = 0; boneId < BoneCount; boneId++ )
		{
			var puppetBoneBody = Puppet.GetBonePhysicsBody( boneId );
			var playerBoneTransform = GetBoneTransform( boneId );
			var boneName = GetBoneName( boneId );

			if ( puppetBoneBody.IsValid() )
			{
				puppetBoneBody.Position = playerBoneTransform.Position;
				puppetBoneBody.Rotation = playerBoneTransform.Rotation;
			}
		}
	}

	internal void MovePuppet()
	{
		if ( !Puppet.IsValid() ) return;

		Puppet.ResetInterpolation();

		for ( int boneId = 0; boneId < BoneCount; boneId++ )
		{
			var puppetBoneBody = Puppet.GetBonePhysicsBody( boneId );
			var playerBoneTransform = GetBoneTransform( boneId );
			var boneName = GetBoneName( boneId );

			if ( puppetBoneBody.IsValid() )
			{
				if ( !IsKnockedOut )
				{
					if ( boneName.Contains( "ankle" ) )
					{
						puppetBoneBody.Position = playerBoneTransform.Position;
						puppetBoneBody.Rotation = playerBoneTransform.Rotation;
					}
					else
					{
						var moveDirection = playerBoneTransform.Position - puppetBoneBody.Position;
						puppetBoneBody.ApplyForce( moveDirection * 500000 * puppetBoneBody.Mass * Time.Delta );
						puppetBoneBody.LinearDamping = 0.5f / Time.Delta;
					}

					puppetBoneBody.Rotation = playerBoneTransform.Rotation;
				}
				else
				{
					puppetBoneBody.LinearDamping = 0f;
				}
			}
		}
	}

	internal void SpawnCollider()
	{
		Collider = new ModelEntity();
		Collider.SetModel( "models/editor/axis_helper_thick.vmdl_c" );
		Collider.SetupPhysicsFromSphere( PhysicsMotionType.Dynamic, Vector3.Zero, CollisionHeight / 1.5f );
		Collider.Tags.Add( "collider" );

		PlaceCollider();
		Collider.EnableAllCollisions = true;
		Collider.EnableDrawing = false;
	}
	internal void PlaceCollider()
	{
		if ( !Collider.IsValid() ) return;

		Collider.Position = CollisionTop;
		Collider.Velocity = 0f;
	}

	internal void MoveCollider()
	{
		if ( !Collider.IsValid() ) return;

		var positionGoal = CollisionTop;
		var moveDirection = positionGoal - Collider.Position;
		Collider.PhysicsBody.ApplyForce( moveDirection * 10000 * Collider.PhysicsBody.Mass * Time.Delta );
		Collider.PhysicsBody.LinearDamping = 30;

		if ( Collider.Position.Distance( positionGoal ) >= CollisionHeight )
		{
			PlaceCollider();
		}
	}

	public void Punch()
	{
		var animationHelper = Animations;
		var puppetAnimationsHelper = PuppetAnimations;

		punchFinish = 0.3f;
		animationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		puppetAnimationsHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		SetAnimParameter( "b_attack", true );
		Puppet.SetAnimParameter( "b_attack", true );

		if ( Game.IsClient ) return;

		var punchTrace = Trace.Ray( CollisionCenter, CollisionCenter + InputRotation.Forward * CollisionHeight * 1.5f )
			.Size( CollisionHeight * 1.5f )
			.EntitiesOnly()
			.WithoutTags( "collider", "player" )
			.Ignore( Puppet )
			.Run();

		if ( punchTrace.Entity is ModelEntity punchTarget )
		{
			var player = punchTarget.GetPlayer();
			if ( player != null )
			{
				player.KnockOut( CollisionCenter, 500f, 1f );
				PlaySound( "sounds/punch.sound" );
			}
			else
			{
				if ( !punchTarget.PhysicsEnabled ) return;

				var targetBody = punchTarget.PhysicsBody;

				if ( !targetBody.IsValid() ) return;
				if ( targetBody.BodyType != PhysicsBodyType.Dynamic ) return;

				targetBody.ApplyImpulseAt( targetBody.LocalPoint( punchTrace.HitPosition ).LocalPosition, InputRotation.Forward * 300f * targetBody.Mass );
				PlaySound( "sounds/punch.sound" );
			}
		}
	}
}

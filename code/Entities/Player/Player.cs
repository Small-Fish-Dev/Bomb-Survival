using Sandbox.Physics;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	public float CollisionHeight => 30f;
	public float CollisionWidth => 24f;
	public BBox CollisionBox => new BBox( new Vector3( -CollisionWidth / 2f, -CollisionWidth / 2f, 0f ) * Scale, new Vector3( CollisionWidth / 2f, CollisionWidth / 2f, CollisionHeight ) * Scale );
	public Vector3 CollisionCenter => Position + Vector3.Up * CollisionHeight * Scale;
	public Vector3 CollisionTop => Position + Vector3.Up * CollisionHeight * Scale / 1.5f;
	[Net] public bool IsDead { get; set; } = false;
	[Net] internal TimeUntil respawnTimer { get; set; } = 0f;
	[Net] internal TimeUntil knockedOutTimer { get; set; } = 0f;
	public bool IsKnockedOut { get; set; } = false;
	[Net] public int LivesLeft { get; set; } = 4;
	
	[Net] internal AnimatedEntity ServerPuppet { get; set; }
	internal AnimatedEntity ClientPuppet { get; set; }
	internal ModelEntity Collider { get; set; }
	[Net] public ModelEntity Grabbing { get; set; } = null;
	[Net] public Vector3 GrabbingPosition { get; set; } = Vector3.Zero;
	[Net] public bool WantsToGrab { get; set; } = false;
	public bool IsGrabbing => Grabbing != null;
	public SpringJoint GrabSpring { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		SpawnServerPuppet();
		SpawnCollider();

		EnableAllCollisions = true;
		EnableDrawing = false;

		Respawn();
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		SpawnClientPuppet();
	}


	public void Respawn()
	{
		var spawnPoint = Entity.All.OfType<Checkpoint>().FirstOrDefault();

		Position = spawnPoint.Position.WithY( 0 );
		Velocity = Vector3.Zero;

		EnableAllCollisions = true;
		knockedOutTimer = 0f;
		IsKnockedOut = false;
		IsDead = false;

		SetCharred( false );

		if ( Game.IsServer )
		{
			if ( ServerPuppet.IsValid() )
			{
				PlaceServerPuppet();
				ServerPuppet.EnableAllCollisions = true;
			}

			if ( Collider.IsValid() )
			{
				PlaceCollider();
				Collider.EnableAllCollisions = true;
			}
		}
		else
		{
			if ( ServerPuppet.IsValid() )
			{
				PlaceClientPuppet();
				ClientPuppet.EnableAllCollisions = true;
				ClientPuppet.EnableDrawing = true;
			}

			spawnPoint.ClientModel.CurrentSequence.Time = 0;
			spawnPoint.ClientModel.SetBodyGroup( "body", 4 - LivesLeft );
		}
	}

	public void Kill()
	{
		IsDead = true;
		respawnTimer = 1f;
		EnableAllCollisions = false;
		Release();

		if ( Game.IsServer )
		{
			ServerPuppet.EnableAllCollisions = false;
			Collider.EnableAllCollisions = false;
			LivesLeft--;
			KillToClients();
		}
		else
		{
			ClientPuppet.EnableAllCollisions = false;
			ClientPuppet.EnableDrawing = false;
		}
	}

	[ClientRpc]
	internal void KillToClients()
	{
		Kill();
	}

	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Rotation InputRotation { get; set; }
	private Rotation wishRotation;
	TimeSince lastRotation = 0f;

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var lookInput = Input.AnalogLook;
		var direction = new Vector3( -lookInput.yaw, 0f, -lookInput.pitch ).Normal;

		if ( IsGrabbing )
		{
			direction = (Grabbing.PhysicsBody.FindClosestPoint( CollisionTop ) - CollisionTop).Normal;
			wishRotation = Rotation.LookAt( direction, Vector3.Left );
		}
		else
		{
			if ( lookInput != Angles.Zero )
			{
				wishRotation = Rotation.LookAt( direction, Vector3.Left );
				lastRotation = 0f;
			}
			else if ( lastRotation >= 1f )
				if ( !Velocity.IsNearlyZero( 2 ) )
					wishRotation = Rotation.LookAt( Velocity, Vector3.Left );
				else
					wishRotation = Rotation.LookAt( Vector3.Right, Vector3.Left );
		}

		InputRotation = Rotation.Slerp( InputRotation, wishRotation, Time.Delta * 5f );
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( IsDead )
		{
			if ( LivesLeft > 0 )
				if ( respawnTimer )
					Respawn();

			return;
		}

		if ( IsKnockedOut )
			if ( knockedOutTimer && GroundEntity != null )
				IsKnockedOut = false;

		ComputeAnimations();
		ComputeMotion();

		if ( Game.IsServer )
		{
			MoveServerPuppet();
			MoveCollider();
			SimulateGrab();
		}
		else
			PlaceClientPuppet();
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

		ComputeAnimations();
		PlaceClientPuppet();
	}

	public void Clothe( IClient client )
	{
		var data = client.GetClientData( "avatar" );
		Clothe( data );
	}

	[ClientRpc]
	public void Clothe( string data )
	{
		var clothing = new ClothingContainer();
		clothing.Deserialize( data );
		clothing.DressEntity( ClientPuppet );
	}

	public void SetCharred( bool charred )
	{
		if ( Game.IsServer )
			SetCharredToClients( To.Everyone, charred );
		else
		{
			if ( !ClientPuppet.IsValid ) return;

			var colorToApply = charred ? Color.Black : Color.White;

			ClientPuppet.RenderColor = colorToApply;

			foreach ( var child in ClientPuppet.Children )
				if ( child is ModelEntity clothing )
					clothing.RenderColor = colorToApply;
		}
	}

	[ClientRpc]
	internal void SetCharredToClients( bool charred )
	{
		SetCharred( charred );
	}

	public void KnockOut( Vector3 sourcePosition, float strength, float amount )
	{
		if ( !ServerPuppet.IsValid ) return;
		if ( IsDead ) return;

		IsKnockedOut = true;
		knockedOutTimer = amount;

		var direction = ((CollisionCenter - sourcePosition).WithY( 0 ).Normal + Vector3.Up * 0.5f).Normal;

		ServerPuppet.PhysicsGroup.Velocity = 0;
		ServerPuppet.PhysicsGroup.ApplyImpulse( direction * strength );

		Release();
	}

	internal void SpawnServerPuppet()
	{
		ServerPuppet = new AnimatedEntity();
		ServerPuppet.SetModel( "models/citizen/citizen.vmdl" );
		ServerPuppet.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		ServerPuppet.Tags.Add( "puppet" );
		ServerPuppet.Owner = this;

		PlaceServerPuppet();
		ServerPuppet.EnableAllCollisions = true;
		ServerPuppet.EnableDrawing = false;

		ServerPuppet.Owner = this;
	}
	internal void SpawnClientPuppet()
	{
		ClientPuppet = new AnimatedEntity();
		ClientPuppet.SetModel( "models/citizen/citizen.vmdl" );
		ClientPuppet.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		ClientPuppet.Tags.Add( "puppet" );
		ClientPuppet.Owner = this;

		PlaceClientPuppet();
		ClientPuppet.EnableAllCollisions = true;
		ClientPuppet.EnableDrawing = true;

		ClientPuppet.Owner = this;
	}

	internal void PlaceServerPuppet()
	{
		if ( !ServerPuppet.IsValid() ) return;

		ServerPuppet.Position = Position;
		ServerPuppet.PhysicsGroup.Velocity = 0f;

		for ( int boneId = 0; boneId < BoneCount; boneId++ )
		{
			var puppetBoneBody = ServerPuppet.GetBonePhysicsBody( boneId );
			var playerBoneTransform = GetBoneTransform( boneId );

			if ( puppetBoneBody.IsValid() )
			{
				puppetBoneBody.Position = playerBoneTransform.Position;
				puppetBoneBody.Rotation = playerBoneTransform.Rotation;
			}
		}
	}

	internal void PlaceClientPuppet()
	{
		if ( !ClientPuppet.IsValid() ) return;

		ClientPuppet.ResetInterpolation();

		ClientPuppet.Position = Position;
		ClientPuppet.PhysicsGroup.Velocity = 0f;

		var positionDifference = ServerPuppet.Position - ( IsKnockedOut ? Position : CollisionCenter );

		for ( int boneId = 0; boneId < ServerPuppet.BoneCount; boneId++ )
		{
			var serverBoneBody = ServerPuppet.GetBonePhysicsBody( boneId );
			var clientBoneBody = ClientPuppet.GetBonePhysicsBody( boneId );

			if ( serverBoneBody.IsValid() && clientBoneBody.IsValid() )
			{
				var newTransform = serverBoneBody.Transform.WithPosition( serverBoneBody.Transform.Position - positionDifference );
				clientBoneBody.Transform = newTransform;
			}
		}

		if ( IsGrabbing )
		{
			for ( int boneId = 0; boneId < ServerPuppet.BoneCount; boneId++ )
			{
				var clientBoneName = ClientPuppet.GetBoneName( boneId );

				if ( clientBoneName.Contains( "hand" ) )
				{
					ClientPuppet.SetBoneTransform( boneId, ClientPuppet.GetBoneTransform(boneId).WithPosition( GrabbingPosition ) );
				}
			}
		}
	}

	internal void MoveServerPuppet()
	{
		if ( !ServerPuppet.IsValid() ) return;

		for ( int boneId = 0; boneId < BoneCount; boneId++ )
		{
			var puppetBoneBody = ServerPuppet.GetBonePhysicsBody( boneId );
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
		Collider.PhysicsBody.ApplyForce( moveDirection * 10000 * Collider.PhysicsBody.Mass * Time.Delta * ( IsGrabbing ? 3f : 1f ) );
		Collider.PhysicsBody.LinearDamping = 30;
		Collider.Rotation = InputRotation;

		if ( Collider.Position.Distance( positionGoal ) >= CollisionHeight )
			PlaceCollider();
	}

	public void Punch()
	{
		var animationHelper = Animations;
		var puppetAnimationsHelper = ServerPuppetAnimations;

		punchFinish = 0.3f;
		animationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		puppetAnimationsHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		SetAnimParameter( "b_attack", true );
		ServerPuppet?.SetAnimParameter( "b_attack", true );
		ClientPuppet?.SetAnimParameter( "b_attack", true );

		Release();

		if ( Game.IsClient ) return;

		var punchTrace = Trace.Ray( CollisionTop, CollisionTop + InputRotation.Forward * CollisionHeight * 1.5f )
			.Size( CollisionHeight * 1.5f )
			.EntitiesOnly()
			.WithoutTags( "collider", "player" )
			.Ignore( ServerPuppet )
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

	public void Grab()
	{
		if ( Game.IsClient ) return;
		if ( IsPunching ) return;
		if ( IsGrabbing ) return;

		var grabTrace = Trace.Ray( CollisionTop, CollisionTop + InputRotation.Forward * CollisionHeight * 1.3f )
			.Size( CollisionHeight )
			.EntitiesOnly()
			.WithoutTags( "collider", "player" )
			.Ignore( ServerPuppet )
			.Run();

		if ( grabTrace.Entity is ModelEntity grabTarget )
		{
			var player = grabTarget.GetPlayer();
			if ( player != null )
				Grabbing = player;
			else
			{
				if ( !grabTarget.PhysicsEnabled ) return;

				var targetBody = grabTarget.PhysicsBody;

				if ( !targetBody.IsValid() ) return;
				if ( targetBody.BodyType != PhysicsBodyType.Dynamic ) return;
				if ( grabTarget is Bomb bombTarget && bombTarget.IsExploding ) return;

				Grabbing = grabTarget;
				
				var armPosition = CollisionTop + (InputRotation.Forward * CollisionHeight / 1.5f);
				var grabPosition = targetBody.FindClosestPoint( armPosition );
				var distance = armPosition.Distance( grabPosition );
				GrabSpring = PhysicsJoint.CreateSpring(
					PhysicsPoint.World( Collider.PhysicsBody, armPosition ),
					PhysicsPoint.World( targetBody, grabPosition ), distance, distance );

				GrabbingPosition = grabPosition;
			}
		}
	}

	internal void SimulateGrab()
	{
		if ( IsKnockedOut ) return;
		if ( IsDead ) return;
		if ( !IsGrabbing ) return;
		if ( Grabbing is Bomb bombTarget && bombTarget.IsExploding )
		{
			Release();
			return;
		}

		DebugOverlay.Line( GrabSpring.Point1.Transform.Position, GrabSpring.Point2.Transform.Position );
		DebugOverlay.Sphere( GrabSpring.Point1.Transform.Position, 5f, Color.Red );
		DebugOverlay.Sphere( GrabSpring.Point2.Transform.Position, 5f, Color.Blue );
		GrabbingPosition = GrabSpring.Point2.Transform.Position;
		//Grabbing.Position = Vector3.Lerp( Grabbing.Position, CollisionCenter + InputRotation.Forward * 50f, Time.Delta * 10f );
	}

	public void Release()
	{
		GrabSpring?.Remove();
		Grabbing = null;
	}
}

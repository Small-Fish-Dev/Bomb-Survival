using Sandbox.Physics;

namespace BombSurvival;

public partial class Player : AnimatedEntity
{
	public float CollisionHeight => 30f;
	public float CollisionWidth => 24f;
	public BBox CollisionBox => new BBox( new Vector3( -CollisionWidth / 2f, -CollisionWidth / 2f, 0f ) * Scale, new Vector3( CollisionWidth / 2f, CollisionWidth / 2f, CollisionHeight ) * Scale );
	public Vector3 CollisionCenter => Position + Vector3.Up * CollisionHeight * Scale;
	public Vector3 CollisionTopLocal => Vector3.Up * CollisionHeight * Scale / 1.5f;
	public Vector3 CollisionTop => Position + CollisionTopLocal;
	[Net] public bool IsDead { get; private set; } = false;
	[Net] internal TimeUntil respawnTimer { get; private set; } = 0f;
	[Net] internal TimeUntil knockedOutTimer { get; private set; } = 0f;
	[Net] public bool IsKnockedOut { get; private set; } = false;
	[Net] public int LivesLeft { get; private set; } = 4;

	[Net] public ModelEntity Collider { get; private set; }
	[Net] public ModelEntity Grabbing { get; private set; } = null;
	[Net] public Vector3 GrabbingPosition { get; private set; } = Vector3.Zero;
	[Net] public bool WantsToGrab { get; private set; } = false;
	public bool IsGrabbing => Grabbing != null;
	[Net] public bool IsBeingGrabbed { get; private set; } = false;
	public SpringJoint GrabSpring { get; private set; }
	public float CrouchLevel => Math.Clamp( ( Collider?.Position.z - Position.z ) / ( CollisionWidth / 1.5f ) * 0.7f ?? 0f, 0f, 0.7f ) + 0.3f;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		SpawnCollider();
		Respawn();
	}

	public void Respawn()
	{
		var spawnPoint = Entity.All.OfType<Checkpoint>().FirstOrDefault();

		Position = spawnPoint.GetBoneTransform( 1 ).Position;
		Velocity = Vector3.Zero;

		EnableAllCollisions = true;
		knockedOutTimer = 0f;
		IsKnockedOut = false;
		IsDead = false;

		SetCharred( false );

		if ( Game.IsServer )
		{
			if ( Collider.IsValid() )
			{
				PlaceCollider();
				Collider.EnableAllCollisions = true;
			}

			AssignPoints( (int)(Score * -0.05f) ); // Remove 5% of their score
		}
		else
		{
			spawnPoint.ClientModel.CurrentSequence.Time = 0;
			spawnPoint.ClientModel.SetBodyGroup( "body", 4 - LivesLeft );
		}
	}

	public void Kill()
	{
		IsDead = true;
		respawnTimer = 1f;
		EnableAllCollisions = false;
		Collider.EnableAllCollisions = false;
		LivesLeft--;

		Release();
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

		ComputeAnimations();
		ComputeMotion();

		if ( Game.IsServer )
		{
			MoveCollider();
			SimulateGrab();
		}

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
	}

	public void SetCharred( bool charred )
	{
		var colorToApply = charred ? Color.Black : Color.White;

		RenderColor = colorToApply;

		foreach ( var child in Children )
			if ( child is ModelEntity clothing )
				clothing.RenderColor = colorToApply;
	}

	public void KnockOut( Vector3 sourcePosition, float strength, float amount )
	{
		if ( IsDead ) return;

		IsKnockedOut = true;
		knockedOutTimer = amount;

		var direction = ((CollisionCenter - sourcePosition).WithY( 0 ).Normal + Vector3.Up * 0.5f).Normal;


		Release();
	}

	internal void SpawnCollider()
	{
		Collider = new ModelEntity();
		Collider.SetModel( "models/editor/axis_helper_thick.vmdl_c" );
		Collider.SetupPhysicsFromOrientedCapsule( PhysicsMotionType.Dynamic, new Capsule( Vector3.Up * CollisionWidth, Vector3.Up * ( CollisionHeight + CollisionWidth / 4f ), CollisionWidth / 1.5f ));

		Collider.PhysicsBody.Mass = 150f;

		Collider.EnableAllCollisions = true;
		Collider.EnableDrawing = false;
		Collider.Tags.Add( "collider" );

		PlaceCollider();

		PhysicsJoint.CreateSlider( new PhysicsPoint( Collider.PhysicsBody ), new PhysicsPoint( PhysicsBody, CollisionTopLocal ), 0f, CollisionHeight );
		var spring = PhysicsJoint.CreateSpring( new PhysicsPoint( Collider.PhysicsBody ), new PhysicsPoint( PhysicsBody, CollisionTopLocal ), 0f, 0f );
		spring.SpringLinear = new PhysicsSpring( 2f, 0.5f );
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

		if ( Collider.Position.Distance( CollisionTop ) >= CollisionHeight * 2 )
			PlaceCollider();
	}

	public void Punch()
	{
		punchFinish = 0.3f;

		var animationHelper = new CitizenAnimationHelper( this );
		animationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		SetAnimParameter( "b_attack", true );

		Release();

		if ( Game.IsClient ) return;

		var punchTrace = Trace.Ray( CollisionTop, CollisionTop + InputRotation.Forward * CollisionHeight * 1.5f )
			.Size( CollisionHeight * 1.5f )
			.DynamicOnly()
			.WithoutTags( "collider", "player" )
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

		var grabTrace = Trace.Ray( CollisionTop, CollisionTop + InputRotation.Forward * CollisionHeight * 0.8f )
			.Size( CollisionHeight * 0.8f )
			.DynamicOnly()
			.WithoutTags( "collider", "player" )
			.Run();

		if ( grabTrace.Entity is ModelEntity grabTarget )
		{
			var player = grabTarget.GetPlayer();
			if ( player != null )
			{
				Grabbing = player;
				player.IsBeingGrabbed = true;
			}
			else
			{
				if ( !grabTarget.PhysicsEnabled ) return;

				var targetBody = grabTarget.PhysicsBody;

				if ( !targetBody.IsValid() ) return;
				if ( targetBody.BodyType != PhysicsBodyType.Dynamic ) return;
				if ( grabTarget is Bomb bombTarget && bombTarget.IsExploding ) return;

				Grabbing = grabTarget;
				Grabbing.PhysicsBody.SurfaceMaterial = "slippery_wave_entity";

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

		if ( Grabbing is Player player )
		{
			if ( player.IsDead ) return;
			player.Velocity += Velocity.WithZ( 0 );
			GrabbingPosition = player.Position + Vector3.Up * player.CollisionHeight;
		}
		else
		{
			GrabbingPosition = GrabSpring.Point2.Transform.Position;
		}

		//DebugOverlay.Line( GrabSpring.Point1.Transform.Position, GrabSpring.Point2.Transform.Position );
		//DebugOverlay.Sphere( GrabSpring.Point1.Transform.Position, 5f, Color.Red );
		//DebugOverlay.Sphere( GrabSpring.Point2.Transform.Position, 5f, Color.Blue );
		//Grabbing.Position = Vector3.Lerp( Grabbing.Position, CollisionCenter + InputRotation.Forward * 50f, Time.Delta * 10f );
	}

	public void Release()
	{
		GrabSpring?.Remove();
		if ( Grabbing != null )
		{
			Grabbing.PhysicsBody.SurfaceMaterial = "normal_wave_entity";

			if ( Grabbing is Player player )
				player.IsBeingGrabbed = false;
		}
		Grabbing = null;
	}
}

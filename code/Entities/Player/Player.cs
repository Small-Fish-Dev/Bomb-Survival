using Sandbox;
using Sandbox.Internal;
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
	[Net, Local] public int LivesLeft { get; private set; } = 4;
	public TimeSince LastRespawn { get; private set; } = 0f;


	public float CrouchLevel { get; set; } = 1f;

	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Rotation InputRotation { get; set; }
	private Rotation wishRotation;
	TimeSince lastRotation = 0f;

	private float cameraDistance = 200f;
	private TimeSince lastMoved;

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
			if ( Game.IsServer )
			{
				Position = Checkpoint.FirstPosition();

				if ( LivesLeft >= 0 )
					if ( respawnTimer )
						Respawn();
			}
			else
				PlaceRagdoll();

			return;
		}

		ComputeAnimations( this );
		ComputeController();

		if ( Game.IsServer )
		{
			MoveCollider();
			SimulateGrab();
		}

		if ( IsKnockedOut )
			SimulateKnockedOut();

		var capsule = new Capsule( Vector3.Up * CollisionWidth, Vector3.Up * (CollisionHeight + CollisionWidth / 4f), CollisionWidth / 1.5f );
		var crouchTrace = Trace.Capsule( capsule, Position, CollisionTop )
			.Ignore( this )
			.WithoutTags( "puppet", "collider" )
			.Run();

		CrouchLevel = crouchTrace.Distance / (CollisionHeight * Scale / 1.5f);
	}

	public bool IsZoomed = false;

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		var nearbyPlayers = Entity.FindInSphere( Position, 512 )
			.OfType<Player>()
			.Where( x => x != this ) // Exclude the current player
			.ToList();

		Vector3 centerPoint = this.Position;
		float maxDistance = 200f; // Default camera distance
		float minDistance = 150f; // Minimum camera distance

		if ( nearbyPlayers.Count > 0 )
		{
			Vector3 othersCenter = nearbyPlayers.Aggregate( Vector3.Zero, ( sum, player ) => sum + player.Position ) / nearbyPlayers.Count;
			maxDistance = nearbyPlayers.Max( player => Vector3.DistanceBetween( centerPoint, player.Position ) );

			// Adjust centerPoint to be a weighted average between player's position (70% weight) and othersCenter (30% weight)
			centerPoint = 0.7f * this.Position + 0.3f * othersCenter;
		}

		if ( Velocity.Length > 1f )
			lastMoved = 0f;

		if ( lastMoved > 2.5f )
		{
			centerPoint = this.Position;  // focus on the current player
			cameraDistance = cameraDistance.LerpTo( 100f, Time.Delta * lastMoved );
			IsZoomed = true;
		}
		else
		{
			cameraDistance = Math.Clamp( maxDistance, minDistance, float.MaxValue );
			IsZoomed = false;
		}

		if ( Camera.Position == Vector3.Zero )
			Camera.Position = Checkpoint.FirstPosition();
		else
		{
			var wishPosition = centerPoint;
			Camera.Position = Vector3.Lerp( Camera.Position, wishPosition + Vector3.Right * cameraDistance + Vector3.Up * 64f, Time.Delta * 5f );
		}

		Camera.Rotation = Rotation.FromYaw( 90f );
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		if ( Ragdoll.IsValid() )
			ComputeAnimations( Ragdoll );

		if ( IsKnockedOut )
			SimulateKnockedOut();

		MoveRagdoll();
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		EnableDrawing = false;
	}

	public void Respawn( bool initial = false )
	{
		Position = Checkpoint.FirstPosition();
		Velocity = Vector3.Zero;

		EnableAllCollisions = true;
		knockedOutTimer = 0f;
		IsKnockedOut = false;
		IsDead = false;
		LastRespawn = 0f;
		SetCharred( false );

		if ( initial )
		{
			SpawnCollider();
		}
		else
		{
			if ( Collider.IsValid() )
			{
				PlaceCollider();
				Collider.EnableAllCollisions = true;
			}
			AssignPoints( (int)(Score * -0.05f) ); // Remove 5% of their score
		}

		respawnToClient( initial );
	}

	public static Player GetLongestLiving()
	{
		return All.OfType<Player>()
			.Where( x => !x.IsDead )
			.OrderByDescending( x => x.LastRespawn ).FirstOrDefault();
	}

	[ConCmd.Admin( "respawn" )]
	public static void RespawnCommand()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player caller ) return;
		caller.Respawn();
	}

	[ClientRpc]
	void respawnToClient( bool initial = false )
	{
		var spawnPoint = Checkpoint.First();

		spawnPoint.ClientModel.CurrentSequence.Time = 0;
		spawnPoint.ClientModel.SetBodyGroup( "body", 4 - LivesLeft );

		if ( initial )
		{
			SpawnRagdoll();
			DressRagdoll();
		}

		PlaceRagdoll();
		Ragdoll.EnableDrawing = true;
	}

	public void Kill()
	{
		Release();
		WakeUp();

		IsDead = true;
		respawnTimer = 1f;
		EnableAllCollisions = false;
		if ( Collider.IsValid() )
			Collider.EnableAllCollisions = false;
		LivesLeft--;

		killToClient();
	}

	[ClientRpc]
	void killToClient()
	{
		if ( !Ragdoll.IsValid() ) return;
		Ragdoll.EnableDrawing = false;
	}
}

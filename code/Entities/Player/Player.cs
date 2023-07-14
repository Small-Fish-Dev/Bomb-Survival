using Sandbox;
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
	[Net] public int LivesLeft { get; private set; } = 4;

	public float CrouchLevel => Math.Clamp( ( Collider?.Position.z - Position.z ) / ( CollisionWidth / 1.5f ) * 0.7f ?? 0f, 0f, 0.7f ) + 0.3f;

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
				var spawnPoint = Entity.All.OfType<Checkpoint>().FirstOrDefault();
				Position = spawnPoint.GetBoneTransform( 1 ).Position;

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
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		if ( IsDead ) return;

		if ( Velocity.Length > 1f )
			lastMoved = 0f;

		if ( lastMoved > 2.5f )
			cameraDistance = cameraDistance.LerpTo( 100f, Time.Delta * lastMoved );
		else
			cameraDistance = cameraDistance.LerpTo( 200f + Velocity.Length * 0.15f, Time.Delta * 0.5f );

		var wishPosition = Position;
		Camera.Position = Vector3.Lerp( Camera.Position, wishPosition + Vector3.Right * cameraDistance + Vector3.Up * 64f, Time.Delta * 5f );
		Camera.Rotation = Rotation.FromYaw( 90f );

		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		if ( Ragdoll.IsValid() )
			ComputeAnimations( Ragdoll );

		MoveCollider();

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

		SpawnCollider();
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );

		SpawnRagdoll();
		SpawnCollider();
	}

	public void Respawn()
	{
		var spawnPoint = Entity.All.OfType<Checkpoint>()
			.Where( x => x.IsScoreboardCheckpoint != ( BombSurvival.Instance.CurrentState == GameState.Playing ) )
			.FirstOrDefault();

		Position = spawnPoint.GetBoneTransform( 1 ).Position;
		Velocity = Vector3.Zero;

		EnableAllCollisions = true;
		knockedOutTimer = 0f;
		IsKnockedOut = false;
		IsDead = false;
		SetCharred( false );

		if ( Collider.IsValid() )
		{
			PlaceCollider();
			Collider.EnableAllCollisions = true;
		}

		AssignPoints( (int)(Score * -0.05f) ); // Remove 5% of their score

		respawnToClient();
	}

	[ConCmd.Admin( "respawn" )]
	public static void RespawnCommand()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player caller ) return;
		caller.Respawn();
	}

	[ClientRpc]
	void respawnToClient()
	{
		var spawnPoint = Entity.All.OfType<Checkpoint>()
			.Where( x => x.IsScoreboardCheckpoint != (BombSurvival.Instance.CurrentState == GameState.Playing) )
			.FirstOrDefault();

		spawnPoint.ClientModel.CurrentSequence.Time = 0;
		spawnPoint.ClientModel.SetBodyGroup( "body", 4 - LivesLeft );

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

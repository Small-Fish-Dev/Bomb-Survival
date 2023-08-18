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

		DebugOverlay.Line( CollisionTop, CollisionTop + InputRotation.Forward * 50f );
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

		var capsule = new Capsule( Vector3.Up * CollisionWidth, Vector3.Up * (CollisionHeight + CollisionWidth / 4f), CollisionWidth / 2f );
		var crouchTrace = Trace.Capsule( capsule, Position, CollisionTop )
			.Ignore( this )
			.WithoutTags( "puppet", "collider" )
			.Run();

		CrouchLevel = crouchTrace.Distance / (CollisionHeight * Scale / 1.5f);
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		if ( Ragdoll.IsValid() )
			ComputeAnimations( Ragdoll );

		if ( IsKnockedOut )
			SimulateKnockedOut();

		MoveRagdoll();
		ComputeCamera();
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		EnableDrawing = false;
		Transmit = TransmitType.Always;

		SpawnCollider();
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

		if ( Collider.IsValid() )
		{
			PlaceCollider();
			Collider.EnableAllCollisions = true;
		}

		respawnToClient();
	}

	public static Player GetLongestLiving()
	{
		return All.OfType<Player>()
			.Where( x => !x.IsDead )
			.OrderByDescending( x => x.LastRespawn.Relative ).FirstOrDefault();
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
		if ( Client == Game.LocalClient )
		{
			var spawnPoint = Checkpoint.First();

			spawnPoint.ClientModel.CurrentSequence.Time = 0;

			if ( LivesLeft != 0 )
			{
				spawnPoint.ClientModel.SetBodyGroup( "body", 4 - LivesLeft );
				spawnPoint.ClientModel.Model.Materials.Last().Set( "g_vColorTint", Color.White );
			}
			else
			{
				spawnPoint.ClientModel.SetBodyGroup( "body", 0 );
				spawnPoint.ClientModel.Model.Materials.Last().Set( "g_vColorTint", Color.Red );
			}
		}

		if ( Ragdoll.IsValid() )
		{
			PlaceRagdoll();
			DrawRagdoll( true );
		}
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
		DrawRagdoll( false );
	}
}

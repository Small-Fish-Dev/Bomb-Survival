using GridAStar;
using System.Threading.Tasks;

namespace BombSurvival;

public partial class BombSurvival
{
	public static float WorldWidth => 2048f;
	public static float WorldHeight => 2048f;
	public static BBox WorldBox => new BBox( new Vector3( -WorldWidth / 2f, -10f, -WorldWidth / 2f ), new Vector3( WorldWidth / 2f, 10f, WorldHeight / 2f ) );
	public static JumpDefinition NormalJump = new JumpDefinition( "jump", Player.BaseWalkSpeed, Player.JumpHeight, 2 );
	public static JumpDefinition DiveJump = new JumpDefinition( "dive", Player.DiveStrength / 2f, Player.DiveStrength / 2f, 2 );

	public static async Task GenerateGrid()
	{
		var builder = new GridAStar.GridBuilder()
			.WithBounds( Vector3.Zero, WorldBox, Rotation.Identity )
			.WithHeightClearance( Player.CollisionHeight)
			.WithWidthClearance( Player.CollisionWidth)
			.WithEdgeNeighbourCount( 2 )
			.WithStepSize( Player.StepSize )
			.WithStandableAngle( Player.MaxWalkableAngle )
			.WithMaxDropHeight( 9999f )
			.AddJumpDefinition( NormalJump )
			.AddJumpDefinition( DiveJump )
			.JumpsIgnoreConnections( true );

		await builder.Create( 2, printInfo: true );
	}

	[ConCmd.Admin( "bs_grid" )]
	static void GridDebug()
	{
		GenerateGrid().Wait();
	}
}

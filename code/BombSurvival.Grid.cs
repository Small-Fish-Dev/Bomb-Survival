using GridAStar;
using System.Threading;
using System.Threading.Tasks;

namespace BombSurvival;

public partial class BombSurvival
{
	public static Grid MainGrid { get; private set; }
	public static Grid CachedGrid { get; private set; }
	public static float WorldWidth => 2048f;
	public static float WorldHeight => 2048f;
	public static BBox WorldBox => new BBox( new Vector3( -WorldWidth / 2f, -15f, -WorldWidth / 2f + 10f), new Vector3( WorldWidth / 2f, 15f, WorldHeight / 2f ) ); // Shift the grid towards the camera so it doesn't generate cells inside of terrain since it will collide with the walls
	public static Dictionary<string, JumpDefinition> JumpDictionary => new Dictionary<string, JumpDefinition>
	{
		{ "longJump", new JumpDefinition("longJump", Player.BaseWalkSpeed, Player.JumpHeight * 1.35f, 2, maxPerCell: 1) },
		{ "highJump", new JumpDefinition("highJump", 60f, Player.JumpHeight * 1.35f, 2, maxPerCell: 1) },
		{ "mediumJump", new JumpDefinition("mediumJump", Player.BaseWalkSpeed, Player.JumpHeight, 2, maxPerCell: 1) },
		{ "shortJump", new JumpDefinition("shortJump", 60f, Player.JumpHeight, 2, maxPerCell: 1) },
		{ "highDiveJump", new JumpDefinition("highDiveJump", 60f, Player.JumpHeight * 1.95f, 2, maxPerCell: 1) },
		{ "longDiveJump", new JumpDefinition("longDiveJump", Player.BaseWalkSpeed * 2.7f, Player.JumpHeight * 1.8f, 2, maxPerCell: 1) }
	};
	static int gridId = 0;

	public static async Task GenerateGrid()
	{
		CachedGrid?.Delete();
		CachedGrid = MainGrid;

		var builder = new GridAStar.GridBuilder( $"Grid{gridId}" )
			.WithBounds( Vector3.Zero, WorldBox, Rotation.Identity )
			.WithStaticOnly( false )
			.WithCellSize( Player.CollisionWidth )
			.WithHeightClearance( Player.CollisionHeight )
			.WithWidthClearance( Player.CollisionWidth )
			.WithEdgeNeighbourCount( 2 )
			.WithStepSize( Player.StepSize )
			.WithStandableAngle( Player.MaxWalkableAngle )
			.WithMaxDropHeight( 9999f )
			.JumpsIgnoreConnections( true )
			.JumpsIgnoreLOS( true );

		foreach ( var definition in JumpDictionary )
			builder = builder.AddJumpDefinition( definition.Value );

		MainGrid = await builder.Create( 1, printInfo: false );

		await BombSurvivalBot.RecalculateAllPaths();
		CachedGrid?.Delete();

		gridId++;

		Event.Run( "UpdateSafeCells" );
	}

	[ConCmd.Admin( "bs_grid" )]
	static void GridDebug()
	{
		GenerateGrid().Wait();
	}
}

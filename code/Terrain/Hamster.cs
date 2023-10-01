using Sandbox.Sdf;
using System.Threading.Tasks;

namespace BombSurvival;

public partial class BombSurvival
{
	public static Texture HamsterFurTexture => Texture.Load( FileSystem.Mounted, $"textures/hamster_fur_sdf.png" );
	public static Texture HamsterSkinTexture => Texture.Load( FileSystem.Mounted, $"textures/hamster_skin_sdf.png" );

	public async static void PlaceHamster( Vector2 position )
	{
		var hamsterFur = new TextureSdf( HamsterFurTexture, 4, 512f )
			.Transform( position );
		var hamsterSkin = new TextureSdf( HamsterSkinTexture, 4, 512f )
			.Transform( position );
		await Background?.AddAsync( hamsterFur, GrassBackground );
		await Background?.AddAsync( hamsterSkin, WoodBackground );
	}

	[ConCmd.Admin( "testhamster" )]
	public static void TestHamster()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player player ) return;
		PlaceHamster( PointToLocal( player.Position ) );
	}
}

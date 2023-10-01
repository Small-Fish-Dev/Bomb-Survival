using Sandbox.Sdf;
using System.Threading.Tasks;

namespace BombSurvival;

public class Hamster
{
	public MagicEye LeftEye { get; set; }
	public MagicEye RightEye { get; set; }
	public MagicMouth Mouth { get; set; }

	public Hamster( Vector3 position, float scale = 1, string dialogue = "Hello I am tutorial hamser" )
	{
		BombSurvival.PlaceHamster( BombSurvival.PointToLocal( position ), scale );

		LeftEye = new MagicEye();
		LeftEye.Scale = scale;
		LeftEye.Position = position + ( Vector3.Forward * 40f + Vector3.Up * 70f ) * scale + Vector3.Left * 25f;
		LeftEye.Rotation = Rotation.FromYaw( -90f );
		RightEye = new MagicEye();
		RightEye.Scale = scale;
		RightEye.Position = position + ( Vector3.Backward * 35f + Vector3.Up * 70f ) * scale + Vector3.Left * 25f;
		RightEye.Rotation = Rotation.FromYaw( -90f );
		Mouth = new MagicMouth();
		Mouth.Scale = scale;
		Mouth.Position = position + ( Vector3.Forward * 5f + Vector3.Down * 10f) * scale + Vector3.Left * 25f;
		Mouth.Rotation = Rotation.FromYaw( -90f );

		SetDialogue( dialogue );
	}

	public void Delete()
	{
		LeftEye?.Delete();
		RightEye?.Delete();
		Mouth?.Delete();
	}

	public void SetDialogue( string dialogue ) => Mouth.Dialogue = dialogue;
}

public partial class BombSurvival
{
	public static Texture HamsterFurTexture => Texture.Load( FileSystem.Mounted, $"textures/hamster_fur_sdf.png" );
	public static Texture HamsterSkinTexture => Texture.Load( FileSystem.Mounted, $"textures/hamster_skin_sdf.png" );

	public async static void PlaceHamster( Vector2 position, float scale = 1 )
	{
		var hamsterFur = new TextureSdf( HamsterFurTexture, 4, 512f * scale )
			.Transform( position );
		var hamsterSkin = new TextureSdf( HamsterSkinTexture, 4, 512f * scale )
			.Transform( position );

		await Background?.AddAsync( hamsterFur, GrassBackground );
		await Background?.AddAsync( hamsterSkin, WoodBackground );
	}

	[ConCmd.Admin( "testhamster" )]
	public static void TestHamster()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player player ) return;

		new Hamster( player.Position );
	}
}

using System.Text.Json.Serialization;

namespace BombSurvival;

public class SDFText
{
	public static SDFText Poppins = new( "Poppins Medium" );

	public struct FontJSON
	{
		public struct Glyph
		{
			public int X { get; set; }
			public int Y { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }
			public int OriginX { get; set; }
			public int OriginY { get; set; }
			public int Advance { get; set; }

			[JsonIgnore]
			public Vector2 Origin => new Vector2( OriginX, OriginY );
		}

		public string Name { get; set; }
		public int Size { get; set; }
		public bool Bold { get; set; }
		public bool Italic { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }

		public Dictionary<string, Glyph> Characters { get; set; }
	}

	public FontJSON Font { get; private set; }
	public Texture SDFTexture { get; private set; }

	public SDFText( string name )
	{
		Font = FileSystem.Mounted.ReadJson<FontJSON>( $"imagefont/{name}.json" );
		SDFTexture = Texture.Load( FileSystem.Mounted, $"imagefont/{name}.png" );
	}

	public float Character( char character, Vector2 center, float scale = 1f, Color? col = null, float? rotation = null )
	{
		// Make sure the font contains our character.
		if ( !Font.Characters.TryGetValue( character.ToString(), out var glyph ) )
			return 0f;

		// If the character is a space, we can just advance.
		if ( character == ' ' )
			return glyph.Advance;

		// Render our glyph.
		var vertices = new Vertex[4];
		var indices = new ushort[]
		{
			0, 2, 1,
			2, 1, 3
		};
		var size = new Vector2( Font.Width, Font.Height );
		var uvs = new Vector2[]
		{
			new Vector2( glyph.X, glyph.Y ) / size, // Top Left
			new Vector2( glyph.X + glyph.Width, glyph.Y ) / size, // Top Right
			new Vector2( glyph.X, glyph.Y + glyph.Height ) / size, // Bottom Left
			new Vector2( glyph.X + glyph.Width, glyph.Y + glyph.Height ) / size // Bottom Right
		};

		var positions = new Vector2[]
		{
			new Vector2( 0, 0 ),
			new Vector2( glyph.Width, 0 ),
			new Vector2( 0, glyph.Height ),
			new Vector2( glyph.Width, glyph.Height )
		};

		for ( int i = 0; i < 4; i++ )
		{
			var pos = center + (positions[i] - glyph.Origin) * scale * HUD.Instance.ScaleToScreen;
			var rad = (rotation ?? 0f).DegreeToRadian();

			vertices[i] = new Vertex
			{
				Position = rotation != null
					? new Vector2(
						MathF.Cos( rad ) * (pos.x - center.x) - MathF.Sin( rad ) * (pos.y - center.y) + center.x,
						MathF.Sin( rad ) * (pos.x - center.x) + MathF.Cos( rad ) * (pos.y - center.y) + center.y )
					: pos,
				TexCoord0 = uvs[i],
				Color = Color.Red
			};
		}

		var attributes = new RenderAttributes();
		attributes.Set( "Font", SDFTexture );
		attributes.Set( "Color", col ?? Color.White );

		Graphics.Draw( vertices, vertices.Length, indices, indices.Length, Material.FromShader( "shaders/sdftext.shader" ), attributes );

		return glyph.Advance * scale * HUD.Instance.ScaleToScreen;
	}
}

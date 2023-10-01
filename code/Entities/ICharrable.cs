namespace BombSurvival;

public interface ICharrable
{
	public static Material CharMaterial => Material.Load( "materials/coal/coal.vmat" );
	public void Char();
}

namespace BombSurvival;

public static class EntityExtensions
{
	public static Player GetPlayer( this Entity entity )
	{
		if ( entity == null ) return null;
		if ( entity is Player player ) return player;
		if ( entity.Owner is Player owner ) return owner;
		return null;
	}
}

using Sandbox.Component;

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

	public static void Glow( this Entity entity, bool on, Color color )
	{
		if ( !on )
		{
			if ( entity.Components.TryGet<Glow>( out Glow oldGlow ) )
				oldGlow.Enabled = false;

			foreach ( var child in entity.Children )
			{
				if ( child.Components.TryGet<Glow>( out Glow childOldGlow ) )
					childOldGlow.Enabled = false;
			}
		}
		else
		{
			var newGlow = entity.Components.GetOrCreate<Glow>();
			newGlow.Enabled = true;
			newGlow.Color = color;

			foreach ( var child in entity.Children )
			{
				var newChildGlow = child.Components.GetOrCreate<Glow>();
				newChildGlow.Enabled = true;
				newChildGlow.Color = color;
			}
		}
	}
}

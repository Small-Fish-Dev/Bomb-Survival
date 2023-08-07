global using System;
global using Sandbox;
global using Sandbox.UI;
global using Sandbox.MenuSystem;
global using Sandbox.Menu;
global using Sandbox.UI.Construct; // TODO Move these somewhere else
using Sandbox.Utility;

namespace BombSurvival.UI;

public class PodView : Panel
{
	ScenePanel scenePanel;
	float cameraDistance = 250f;
	float cameraMinimumDistance => 250f;
	float cameraMaximumDistance => 1000f;
	Vector3 cameraCenter => new Vector3( 200f, 0f, 125f );
	Vector3 cameraShake = Vector3.Zero;

	public PodView()
	{
		StyleSheet.Load( "UI/MainMenu/PodView/PodView.cs.scss" );

		scenePanel?.Delete( true );

		var sceneWorld = new SceneWorld();
		var sceneMap = new SceneMap( sceneWorld, "maps/pod" );

		scenePanel = Add.ScenePanel( sceneWorld, Vector3.Zero, Rotation.Identity, Game.Preferences.FieldOfView, "scenePanel" );

		scenePanel.Camera.Position = cameraCenter + Vector3.Backward * cameraDistance;
	}

	public override void OnMouseWheel( float value )
	{
		cameraDistance = Math.Clamp( cameraDistance + value * 5f, cameraMinimumDistance, cameraMaximumDistance );
		base.OnMouseWheel( value );
	}

	public override void Tick()
	{
		if ( scenePanel == null )
			return;

		var deltaCameraDistance = ( cameraDistance - cameraMinimumDistance ) / ( cameraMaximumDistance - cameraMinimumDistance );

		var oldRotation = scenePanel.Camera.Rotation;
		var newRotation = Rotation.FromPitch( (float)Math.Pow( deltaCameraDistance * 4, 2.5 ) );

		var oldPosition = scenePanel.Camera.Position;
		var newPosition = cameraCenter + newRotation.Backward * cameraDistance;

		var cameraShakeAmount = Math.Max( deltaCameraDistance - 0.33f, 0f ) * 3f;
		var cameraShakeX = Noise.Perlin( Time.Now * cameraShakeAmount * 50f ) - 0.5f;
		var cameraShakeY = Noise.Perlin( Time.Now * cameraShakeAmount * 50f + 1000f ) - 0.5f;

		cameraShake = ( newRotation.Right * cameraShakeX + newRotation.Up * cameraShakeY ) * cameraShakeAmount;

		scenePanel.Camera.Rotation = Rotation.Lerp( oldRotation, newRotation, Time.Delta * 5f );
		scenePanel.Camera.Position = Vector3.Lerp( oldPosition, newPosition, Time.Delta * 5f ) + cameraShake;
	}
}

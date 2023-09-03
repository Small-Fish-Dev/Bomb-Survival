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
	SceneWorld sceneWorld;

	public PodView()
	{
		StyleSheet.Load( "UI/MainMenu/PodView/PodView.cs.scss" );

		scenePanel?.Delete( true );
		sceneWorld?.Delete();

		sceneWorld = new SceneWorld();
		new SceneMap( sceneWorld, "maps/pod" );

		scenePanel = Add.ScenePanel( sceneWorld, Vector3.Zero, Rotation.Identity, Game.Preferences.FieldOfView, "scenePanel" );
		scenePanel.Camera.AmbientLightColor = new Color( 0.3f, 0.3f, 1f ) * 0.1f;

		scenePanel.Camera.Rotation = Rotation.FromPitch( 45f );
		scenePanel.Camera.Position = scenePanel.Camera.Rotation.Backward * 3000f;
	}

	public override void Tick()
	{
		if ( scenePanel == null )
			return;

		var cameraShakeX = Noise.Perlin( Time.Now * 150f ) - 0.5f;
		var cameraShakeY = Noise.Perlin( Time.Now * 150f + 1000f ) - 0.5f;
		var cameraShake = ( scenePanel.Camera.Rotation.Right * cameraShakeX + scenePanel.Camera.Rotation.Up * cameraShakeY ) * 50f;

		scenePanel.Camera.Rotation = Rotation.FromPitch( 45f );
		scenePanel.Camera.Position = scenePanel.Camera.Rotation.Backward * 3000f + cameraShake;
		scenePanel.Camera.ZFar = 10000f;
	}
}

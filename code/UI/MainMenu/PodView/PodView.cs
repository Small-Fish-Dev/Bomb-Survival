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
	SceneModel scenePlayer;
	List<SceneModel> scenePlayerClothing;
	Vector3 scenePlayerTarget = Vector3.Zero;
	float scenePlayerSpeed = 0f;
	TimeUntil scenePlayerNextTarget = 2f;
	float cameraDistance;
	float cameraStartDistance = 400f;
	float cameraMinimumDistance => 250f;
	float cameraMaximumDistance => 1000f;
	Vector3 cameraCenter => new Vector3( 200f, 0f, 125f );
	Vector3 cameraShake = Vector3.Zero;
	TimeUntil transitionTime = 3f;

	public PodView()
	{
		StyleSheet.Load( "UI/MainMenu/PodView/PodView.cs.scss" );

		scenePanel?.Delete( true );
		sceneWorld?.Delete();

		sceneWorld = new SceneWorld();
		new SceneMap( sceneWorld, "maps/pod" );

		scenePlayer = new SceneModel( sceneWorld, Model.Load( "models/citizen/citizen.vmdl" ), Transform.Zero );
		
		var clothingContainer = new ClothingContainer();
		clothingContainer.Deserialize( ConsoleSystem.GetValue( "avatar" ) ); // ResourceLibrary doesn't load clothing??
		scenePlayerClothing = clothingContainer.DressSceneObject( scenePlayer );

		scenePanel = Add.ScenePanel( sceneWorld, Vector3.Zero, Rotation.Identity, Game.Preferences.FieldOfView, "scenePanel" );
		scenePanel.Camera.AmbientLightColor = new Color( 0.3f, 0.3f, 1f ) * 0.1f;

		//var roomLight = new SceneSpotLight( sceneWorld, Vector3.Up * 200f, new Color( 1f, 0.4f, 0.4f ) );
		//roomLight.ShadowsEnabled = false;
		//roomLight.Rotation = Rotation.FromPitch( 90 );
		//roomLight.ConeInner = 60f;
		//roomLight.ConeOuter = 90f;
		//roomLight.Radius = 512f;

		//var ceilingLight = new SceneSpotLight( sceneWorld, Vector3.Up * 200f, new Color( 1f, 0.4f, 0.4f ) );
		//ceilingLight.ShadowsEnabled = true;
		//ceilingLight.Rotation = Rotation.FromPitch( -90 );
		//ceilingLight.ConeInner = 60f;
		//ceilingLight.ConeOuter = 90f;
		//ceilingLight.Radius = 512f;

		scenePanel.Camera.Rotation = Rotation.FromPitch( (float)Math.Pow( 4, 2.5 ) );
		scenePanel.Camera.Position = cameraCenter + scenePanel.Camera.Rotation.Backward * cameraMaximumDistance; // Make it zoom in when first loading :-)
		cameraDistance = cameraStartDistance;
	}

	public override void OnMouseWheel( float value )
	{
		if ( transitionTime )
			cameraDistance = Math.Clamp( cameraDistance + value * 5f, cameraMinimumDistance, cameraMaximumDistance );

		base.OnMouseWheel( value );
	}

	public override void Tick()
	{
		if ( scenePanel == null )
			return;

		var transitionDelta = 1 - transitionTime.Fraction;
		var distanceOffset = ( cameraMaximumDistance - cameraStartDistance ) * transitionDelta;
		var currentDistance = cameraDistance + distanceOffset;

		var deltaCameraDistance = (currentDistance - cameraMinimumDistance ) / ( cameraMaximumDistance - cameraMinimumDistance );

		var oldRotation = scenePanel.Camera.Rotation;
		var newRotation = Rotation.FromPitch( (float)Math.Pow( deltaCameraDistance * 4, 2.5 ) );

		var oldPosition = scenePanel.Camera.Position;
		var newPosition = cameraCenter + newRotation.Backward * currentDistance;

		var cameraShakeAmount = Math.Max( deltaCameraDistance - 0.33f, 0f ) * 3f;
		var cameraShakeX = Noise.Perlin( Time.Now * cameraShakeAmount * 50f ) - 0.5f;
		var cameraShakeY = Noise.Perlin( Time.Now * cameraShakeAmount * 50f + 1000f ) - 0.5f;

		cameraShake = ( newRotation.Right * cameraShakeX + newRotation.Up * cameraShakeY ) * cameraShakeAmount * ( 1 + transitionDelta * 5 );

		scenePanel.Camera.Rotation = Rotation.Lerp( oldRotation, newRotation, Time.Delta * 5f );
		scenePanel.Camera.Position = Vector3.Lerp( oldPosition, newPosition, Time.Delta * 5f ) + cameraShake;

		/*var startPos = scenePanel.Camera.Position;
		var direction = Screen.GetDirection( Mouse.Position, Game.Preferences.FieldOfView, scenePanel.Camera.Rotation );
		var endPos = startPos + direction * ( 50f + currentDistance);

		var trace = sceneWorld.Trace.Ray( startPos, endPos )
			.Run();*/

		if ( scenePlayerNextTarget )
		{
			scenePlayerTarget = new Vector3( Game.Random.Float( -120f, 120f ), Game.Random.Float( -120f, 120f ), 0 );
			scenePlayerNextTarget = Game.Random.Float( 4f, 8f );
		}

		var wishRotation = Rotation.LookAt( (scenePlayerTarget - scenePlayer.Position).Normal, Vector3.Up );
		var distance = scenePlayer.Position.Distance( scenePlayerTarget );
		scenePlayerSpeed = Math.Clamp( Math.Min( scenePlayerSpeed + Time.Delta * 20f, distance ), 0f, 60f );

		scenePlayer.Rotation = Rotation.Slerp( scenePlayer.Rotation, wishRotation, Time.Delta * 2f);
		scenePlayer.Position += scenePlayer.Rotation.Forward * Time.Delta * scenePlayerSpeed;
		scenePlayer.SetAnimParameter( "move_x", scenePlayerSpeed );

		scenePlayer.Update( Time.Delta );

		foreach ( var clothing in scenePlayerClothing )
			clothing.Update( Time.Delta );
		
	}
}

global using System;
global using Sandbox;
global using Sandbox.UI;
global using Sandbox.MenuSystem;
global using Sandbox.Menu;
global using Sandbox.UI.Construct; // TODO Move these somewhere else

namespace BombSurvival.UI;

public class PodView : Panel
{
	private readonly ScenePanel _renderScene;
	private float _renderSceneDistance = 100f;
	private float _yaw = -175;

	public PodView()
	{
		StyleSheet.Load( "UI/MainMenu/PodView/PodView.cs.scss" );

		_renderScene?.Delete( true );

		var sceneWorld = new SceneWorld();
		var map = new SceneMap( sceneWorld, "maps/pod" );

		_renderScene = Add.ScenePanel( sceneWorld, Vector3.One, Rotation.Identity, 75, "renderScene" );
		_renderScene.Camera.Position = Vector3.Backward * _renderSceneDistance;
	}

	public override void OnMouseWheel( float value )
	{
		_renderSceneDistance += value * 3;
		_renderSceneDistance = _renderSceneDistance.Clamp( 150, 800 );
		base.OnMouseWheel( value );
	}

	public override void Tick()
	{
		
		if ( _renderScene == null )
			return;

		if ( HasMouseCapture )
			_yaw -= Mouse.Delta.x * 0.05f;

		_yaw = _yaw.Clamp( -200, -130 );

		float yawRad = MathX.DegreeToRadian( _yaw );
		float height = 16;

		var currentPosition = _renderScene.Camera.Position;
		_renderScene.Camera.Position = currentPosition.LerpTo( Vector3.Backward * _renderSceneDistance, Time.Delta * 4.0f );
	}
}

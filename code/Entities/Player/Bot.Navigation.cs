using GridAStar;
using System.Threading;

namespace BombSurvival;

public partial class BombSurvivalBot
{
	AStarPath currentPath;
	public AStarPath CurrentPath
	{
		get => currentPath;
		private set
		{
			if ( !currentPath.IsEmpty && currentPath.Nodes == value.Nodes )
				return;
			currentPath = value;
		}
	}
	public AStarNode CurrentPathNode => CurrentPath.Nodes[0] ?? null; // The latest cell crossed in the path
	public AStarNode LastPathNode => CurrentPath.Nodes[^1] ?? null; // The final cell in the path
	public AStarNode NextPathNode => CurrentPath.Nodes[Math.Min( 1, CurrentPath.Count - 1 )] ?? null;
	public string NextMovementTag => NextPathNode.MovementTag ?? string.Empty;
	public bool IsFollowingPath => CurrentPath.Count > 0; // Is the entity following a path
	public bool IsFollowingSomeone => IsFollowingPath && TargetEntity != null; // Is the entity following a moving target

	float pathRetraceFrequency => 0.5f; // How many seconds before it checks if the path is being followed or the target position changed
	TimeUntil nextRetraceCheck = 0f;
	public CancellationTokenSource CurrentPathToken = new();
	AStarPathBuilder pathBuilder => new AStarPathBuilder( Grid.Main ).WithPathCreator( Pawn );

	public Entity TargetEntity = null;
	public Vector3 TargetPosition = Vector3.Zero;
	public Vector3 Target => TargetEntity == null ? TargetPosition : TargetEntity.Position;
	public Cell TargetCell => Grid.Main.GetCell( Target ) ?? Grid.Main.GetNearestCell( Target );
	public Cell CurrentCell => Grid.Main.GetCell( Pawn.Position ) ?? Grid.Main.GetNearestCell( Pawn.Position );

	public void ComputeNavigation()
	{
		if ( Game.IsClient )
			return;

		TargetEntity = Game.Clients.First().Pawn as Entity;

		if ( nextRetraceCheck )
		{
			if ( Target != Vector3.Zero )
				NavigateTo( TargetCell );

			nextRetraceCheck = pathRetraceFrequency;
		}
		/*
		if ( IsFollowingPath )
		{
			for ( var i = 1; i < CurrentPath.Count; i++ )
			{
				DebugOverlay.Line( CurrentPath.Nodes[i - 1].EndPosition, CurrentPath.Nodes[i].EndPosition, Time.Delta, false );
				DebugOverlay.Text( CurrentPath.Nodes[i - 1].MovementTag, CurrentPath.Nodes[i - 1].EndPosition, Time.Delta, 5000f );
			}

			var minimumDistanceUntilNext = CurrentGrid.CellSize; // * 1.42f ?

			if ( Position.WithZ( 0 ).Distance( NextPathNode.EndPosition.WithZ( 0 ) ) <= minimumDistanceUntilNext ) // Move onto the next cell
				if ( Math.Abs( Position.z - NextPathNode.EndPosition.z ) <= CurrentGrid.StepSize ) // Make sure it's within the stepsize
				{
					CurrentPath.Nodes.RemoveAt( 0 );

					if ( NextMovementTag == "shortjump" )
					{
						Position = CurrentPathNode.EndPosition;
						Direction = (NextPathNode.EndPosition.WithZ( 0 ) - Position.WithZ( 0 )).Normal;
						Velocity = (Direction * 200f).WithZ( 300f );
						SetAnimParameter( "jump", true );
						GroundEntity = null;
					}

					if ( CurrentPathNode == LastPathNode )
						HasArrivedDestination = true;
				}

			if ( NextMovementTag == "drop" )
				IsRunning = false;

			if ( GroundEntity != null )
			{
				if ( NextMovementTag.Contains( "jump" ) )
				{
					
				}
			}

		}*/

	}

	public void NavigateTo( Cell targetCell )
	{
		GameTask.RunInThreadAsync( async () =>
		{
			var startingCell = CurrentCell;

			if ( startingCell == null || targetCell == null || startingCell == targetCell ) return;

			CurrentPathToken.Cancel();
			CurrentPathToken = new CancellationTokenSource();

			var computedPath = await pathBuilder.RunAsync( startingCell, targetCell, CurrentPathToken.Token );

			if ( computedPath.IsEmpty || computedPath.Length < 1 )
				return;

			//computedPath.Simplify();

			CurrentPath = computedPath;
		} );
	}

	public static void CancelAllTokens()
	{
		foreach ( var bot in All )
		{
			var bsBot = bot as BombSurvivalBot;
			bsBot.CurrentPathToken.Cancel();
			bsBot.CurrentPathToken = new CancellationTokenSource();
		}
	}
}

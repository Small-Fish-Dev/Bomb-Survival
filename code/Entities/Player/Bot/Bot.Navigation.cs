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
	Grid currentGrid => BombSurvival.MainGrid;
	Grid followedGrid => CurrentPath.Grid;
	public AStarNode CurrentPathNode => CurrentPath.Nodes[0] ?? null; // The latest cell crossed in the path
	public AStarNode LastPathNode => CurrentPath.Nodes[^1] ?? null; // The final cell in the path
	public AStarNode NextPathNode => CurrentPath.Nodes[Math.Min( 1, CurrentPath.Count - 1)] ?? null;
	public string NextMovementTag => NextPathNode.MovementTag ?? string.Empty;
	public string CurrentMovementTag => CurrentPathNode.MovementTag ?? string.Empty;
	public bool IsFollowingPath => CurrentPath.Count > 0; // Is the entity following a path
	public bool IsFollowingSomeone => IsFollowingPath && TargetEntity != null; // Is the entity following a moving target

	float pathRetraceFrequency => 0.5f; // How many seconds before it checks if the path is being followed or the target position changed
	TimeUntil nextRetraceCheck = 0f;
	public CancellationTokenSource CurrentPathToken = new();
	AStarPathBuilder pathBuilder => new AStarPathBuilder( currentGrid )
		.WithPathCreator( Pawn );

	public Entity TargetEntity = null;
	public Vector3 TargetPosition = Vector3.Zero;
	public Vector3 Target => TargetEntity == null ? TargetPosition : TargetEntity.Position;
	public Cell TargetCell
	{
		get
		{
			if ( CurrentPath.IsEmpty )
				return currentGrid != null ? currentGrid.GetCell( Target ) ?? currentGrid.GetNearestCell( Target ) : null;
			else
				return followedGrid != null ? followedGrid.GetCell( Target ) ?? followedGrid.GetNearestCell( Target ) : null;
		}
	} 
	public Cell CurrentCell
	{
		get
		{
			if ( CurrentPath.IsEmpty )
				return currentGrid != null ? currentGrid.GetCell( Pawn.Position ) ?? currentGrid.GetNearestCell( Pawn.Position ) : null;
			else
				return followedGrid != null ? followedGrid.GetCell( Pawn.Position ) ?? followedGrid.GetNearestCell( Pawn.Position ) : null;
		}
	}
	public bool MovingLeft = true;

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

		if ( IsFollowingPath )
		{
			/*for ( var i = 1; i < CurrentPath.Count; i++ )
			{
				DebugOverlay.Line( CurrentPath.Nodes[i - 1].EndPosition, CurrentPath.Nodes[i].EndPosition, Time.Delta, false );
				DebugOverlay.Text( CurrentPath.Nodes[i - 1].MovementTag, CurrentPath.Nodes[i - 1].EndPosition, Time.Delta, 5000f );
			}*/

			if ( Pawn.GroundEntity != null )
			{
				MovingLeft = NextPathNode.EndPosition.x - Pawn.Position.x < 0f;
			}

			foreach ( var cell in CurrentPath.Nodes )
				cell.Current.Draw( 0.1f );

			var minimumDistanceUntilNext = BombSurvival.MainGrid.CellSize; // * 1.42f ?

			if ( Pawn.Position.WithZ( 0 ).Distance( NextPathNode.EndPosition.WithZ( 0 ) ) <= minimumDistanceUntilNext ) // Move onto the next cell
				if ( Math.Abs( Pawn.Position.z - NextPathNode.EndPosition.z ) <= currentGrid.StepSize ) // Make sure it's within the stepsize
					CurrentPath.Nodes.RemoveAt( 0 );

			if ( IsFollowingPath )
			{
				if ( CurrentMovementTag == "longJump" )
				{
					var distanceBetweenNodes = CurrentPathNode.EndPosition.Distance( NextPathNode.EndPosition );
					var distanceBetweenPlayer = Pawn.Position.Distance( NextPathNode.EndPosition );

					if ( Pawn.GroundEntity != null && distanceBetweenNodes >= distanceBetweenPlayer )
					{
						Pawn.Velocity = ((MovingLeft ? Vector3.Left : Vector3.Right) * Player.BaseWalkSpeed * 1.5f).WithZ( Player.JumpHeight * 1.6f );
						Pawn.SetAnimParameter( "jump", true );
						Pawn.GroundEntity = null;
					}
				}

				if ( CurrentMovementTag == "highJump" )
				{
					var distanceBetweenNodes = CurrentPathNode.EndPosition.Distance( NextPathNode.EndPosition );
					var distanceBetweenPlayer = Pawn.Position.Distance( NextPathNode.EndPosition );

					if ( Pawn.GroundEntity != null && distanceBetweenNodes >= distanceBetweenPlayer )
					{
						Pawn.Velocity = ((MovingLeft ? Vector3.Left : Vector3.Right) * 60f).WithZ( Player.JumpHeight * 1.5f );
						Pawn.SetAnimParameter( "jump", true );
						Pawn.GroundEntity = null;
					}
				}
			}
		}

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

			/*foreach ( var node  in computedPath.Nodes )
				node.Current.Draw( 2f, false );*/

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
			bsBot.CurrentPath = AStarPath.Empty();
		}
	}
}

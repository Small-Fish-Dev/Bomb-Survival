using GridAStar;
using System.Threading;
using System.Threading.Tasks;

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
	public AStarNode CurrentPathNode => IsFollowingPath ? CurrentPath.Nodes[0] ?? null : null; // The latest cell crossed in the path
	public AStarNode LastPathNode => IsFollowingPath ? CurrentPath.Nodes[^1] ?? null : null; // The final cell in the path
	public AStarNode NextPathNode => IsFollowingPath ? CurrentPath.Nodes[Math.Min( 1, CurrentPath.Count - 1)] ?? null : null;
	public string NextMovementTag => IsFollowingPath ? NextPathNode?.MovementTag ?? string.Empty : null;
	public string CurrentMovementTag => IsFollowingPath ? CurrentPathNode?.MovementTag ?? string.Empty : null;
	public bool IsFollowingPath => CurrentPath.Count > 0; // Is the entity following a path
	public bool IsFollowingSomeone => IsFollowingPath && TargetEntity != null; // Is the entity following a moving target

	float pathRetraceFrequency => 0.5f; // How many seconds before it checks if the path is being followed or the target position changed
	TimeUntil nextRetraceCheck = 0f;
	public CancellationTokenSource CurrentPathToken = new();
	AStarPathBuilder pathBuilder => new AStarPathBuilder( currentGrid )
		.WithPathCreator( Pawn )
		.WithPartialEnabled();

	public Entity TargetEntity = null;
	public Vector3 TargetPosition = Vector3.Zero;
	public Vector3 Target => TargetEntity == null ? TargetPosition : TargetEntity.Position;
	public Cell TargetCell => currentGrid != null ? currentGrid.GetCellInArea( Target, Player.CollisionWidth ) ?? currentGrid.GetNearestCell( Target ) : null;
	public Cell CurrentCell => currentGrid != null ? currentGrid.GetCellInArea( Pawn.Position, Player.CollisionWidth ) ?? currentGrid.GetNearestCell( Pawn.Position ) : null;
	float minimumDistanceUntilNext => currentGrid.CellSize / 2f;
	Vector3 differenceBetweenNext => Pawn.Position.WithY(0) - NextPathNode.EndPosition.WithY( 0 );
	bool withinDistanceForNext => differenceBetweenNext.Length <= minimumDistanceUntilNext && Math.Abs( differenceBetweenNext.z ) <= currentGrid.StepSize;
	Vector3 differenceBetweenCurrent => CurrentPathNode != null ? Pawn.Position.WithY( 0 ) - CurrentPathNode.EndPosition.WithY( 0 ) : Vector3.Zero;
	bool withinDistanceForCurrent => differenceBetweenCurrent.Length <= minimumDistanceUntilNext && Math.Abs( differenceBetweenCurrent.z ) <= currentGrid.StepSize;

	public bool MovingLeft => NextPathNode.EndPosition.x - Pawn.Position.x < 0f;

	public async void ComputeNavigation()
	{
		if ( Game.IsClient ) return;
		if ( currentGrid == null ) return;

		if ( nextRetraceCheck )
		{
			if ( IsFollowingPath )
			{
				if ( TargetCell != LastPathNode.Parent.Current && TargetCell != LastPathNode.Current || differenceBetweenCurrent.Length >= minimumDistanceUntilNext * 2f && Pawn.GroundEntity != null )
					if ( Target != Vector3.Zero )
						await NavigateToTarget();
			}
			else
				if ( TargetCell != CurrentCell )
					await NavigateToTarget();

			nextRetraceCheck = pathRetraceFrequency;
		}

		if ( IsFollowingPath && !Pawn.IsKnockedOut )
		{
			/*for ( var i = 1; i < CurrentPath.Count; i++ )
			{
				DebugOverlay.Line( CurrentPath.Nodes[i - 1].EndPosition, CurrentPath.Nodes[i].EndPosition, Time.Delta, false );
				DebugOverlay.Text( CurrentPath.Nodes[i - 1].MovementTag, CurrentPath.Nodes[i - 1].EndPosition, Time.Delta, 5000f );
			}*/

			foreach ( var cell in CurrentPath.Nodes )
				cell.Current.Draw( 0.1f );

			if ( withinDistanceForNext )
				CurrentPath.Nodes.RemoveAt( 0 );

			if ( CurrentMovementTag != null || CurrentMovementTag != String.Empty )
			{
				if ( withinDistanceForCurrent )
				{
					if ( Pawn.GroundEntity != null )
					{
						if ( CurrentMovementTag != null && BombSurvival.JumpDictionary.ContainsKey( CurrentMovementTag ) )
						{

							var jumpDefinition = BombSurvival.JumpDictionary[CurrentMovementTag];
							var direction = MovingLeft ? Vector3.Backward : Vector3.Forward;
							var horizontalSpeed = Math.Min( jumpDefinition.HorizontalSpeed, Player.BaseWalkSpeed );
							var verticalSpeed = Math.Min( jumpDefinition.VerticalSpeed, Player.JumpHeight * 1.35f );
							var wishVelocity = (direction * horizontalSpeed).WithZ( verticalSpeed ) * 1.1f;
							var middleDirection = (direction + wishVelocity.Normal).Normal;

							Pawn.GroundEntity = null;
							Pawn.SetAnimParameter( "jump", true );

							if ( jumpDefinition.HorizontalSpeed > horizontalSpeed && !Pawn.IsKnockedOut )
							{
								Pawn.Velocity = wishVelocity;

								await GameTask.Delay( 700 );
								Pawn.KnockOut( Pawn.Position - middleDirection * 10f + Vector3.Up * 10f, Player.DiveStrength * 1.1f, 1f );
							}

							if ( jumpDefinition.VerticalSpeed > verticalSpeed && !Pawn.IsKnockedOut )
							{
								Pawn.Velocity = Vector3.Up * verticalSpeed;

								await GameTask.Delay( 500 );
								Pawn.KnockOut( Pawn.Position - middleDirection * 10f, Player.DiveStrength, 1f );
							}

							if ( !Pawn.IsKnockedOut )
								Pawn.Velocity = wishVelocity;
						}
					}
				}
			}
		}
	}

	public async Task NavigateToTarget()
	{
		var startingCell = CurrentCell;
		var endingCell = TargetCell;

		if ( startingCell == null || endingCell == null || startingCell == endingCell ) return;

		CurrentPathToken.Cancel();
		CurrentPathToken = new CancellationTokenSource();

		var computedPath = await pathBuilder.RunAsync( startingCell, endingCell, CurrentPathToken.Token );

		if ( computedPath.IsEmpty || computedPath.Length < 1 )
			return;

		//computedPath.Simplify();

		/*foreach ( var node  in computedPath.Nodes )
			node.Current.Draw( 2f, false );*/

		CurrentPath = computedPath;
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
	public static async Task RecalculateAllPaths()
	{
		List<Task> tasks = new();

		foreach ( var bot in All )
		{
			tasks.Add( GameTask.RunInThreadAsync( async () =>
			{
				var bsBot = bot as BombSurvivalBot;
				await bsBot.NavigateToTarget();
			} ) );
		}

		await GameTask.WhenAll( tasks );
	}
}

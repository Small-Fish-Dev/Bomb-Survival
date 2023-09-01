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
	public Cell TargetCell => currentGrid != null ? currentGrid.GetCell( Target ) ?? currentGrid.GetNearestCell( Target ) : null;
	public Cell CurrentCell => currentGrid != null ? currentGrid.GetCell( Pawn.Position ) ?? currentGrid.GetNearestCell( Pawn.Position ) : null;
	float minimumDistanceUntilNext => currentGrid.CellSize / 2f;
	Vector3 differenceBetweenNext => Pawn.Position - NextPathNode.EndPosition;
	bool withinDistanceForNext => differenceBetweenNext.Length <= minimumDistanceUntilNext && Math.Abs( differenceBetweenNext.z ) <= currentGrid.StepSize;
	Vector3 differenceBetweenCurrent => Pawn.Position - CurrentPathNode.EndPosition;
	bool withinDistanceForCurrent => differenceBetweenCurrent.Length <= minimumDistanceUntilNext && Math.Abs( differenceBetweenCurrent.z ) <= currentGrid.StepSize;

	public bool MovingLeft => NextPathNode.EndPosition.x - Pawn.Position.x < 0f;

	public async void ComputeNavigation()
	{
		if ( Game.IsClient )
			return;

		TargetEntity = Game.Clients.First().Pawn as Entity;

		if ( nextRetraceCheck )
		{
			if ( TargetCell != LastPathNode.Parent.Current && TargetCell != LastPathNode.Current )
				if ( Target != Vector3.Zero )
					await NavigateToTarget();

			nextRetraceCheck = pathRetraceFrequency;
		}

		if ( IsFollowingPath )
		{
			/*for ( var i = 1; i < CurrentPath.Count; i++ )
			{
				DebugOverlay.Line( CurrentPath.Nodes[i - 1].EndPosition, CurrentPath.Nodes[i].EndPosition, Time.Delta, false );
				DebugOverlay.Text( CurrentPath.Nodes[i - 1].MovementTag, CurrentPath.Nodes[i - 1].EndPosition, Time.Delta, 5000f );
			}

			foreach ( var cell in CurrentPath.Nodes )
				cell.Current.Draw( 0.1f );*/

			if ( withinDistanceForNext )
				CurrentPath.Nodes.RemoveAt( 0 );

			if ( CurrentMovementTag != null || CurrentMovementTag != String.Empty )
			{
				if ( withinDistanceForCurrent )
				{
					if ( Pawn.GroundEntity != null )
					{
						if ( BombSurvival.JumpDictionary.ContainsKey( CurrentMovementTag ) )
						{

							var jumpDefinition = BombSurvival.JumpDictionary[CurrentMovementTag];
							var direction = MovingLeft ? Vector3.Left : Vector3.Right;
							var horizontalSpeed = jumpDefinition.HorizontalSpeed;
							var verticalSpeed = Math.Min( jumpDefinition.VerticalSpeed, Player.JumpHeight * 1.35f );
							var scaledDirection = (direction * horizontalSpeed + jumpDefinition.VerticalSpeed).Normal.WithZ( 0 );
							Pawn.Velocity = (scaledDirection * horizontalSpeed).WithZ( verticalSpeed );
							Pawn.SetAnimParameter( "jump", true );
							Pawn.GroundEntity = null;

							if ( jumpDefinition.VerticalSpeed > verticalSpeed )
							{
								await GameTask.Delay( 300 );
								//Pawn.KnockOut( Pawn.Velocity.Normal, Player.DiveStrength, 1f );
							}
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

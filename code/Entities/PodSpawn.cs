using Editor;

namespace BombSurvival;

/// <summary>
/// Spawn for players in the pod
/// </summary>
[HammerEntity]
[EditorModel( "models/checkpoint/checkpoint.vmdl" )]
public class PodSpawn : Entity
{
	public static PodSpawn Get() => All.OfType<PodSpawn>().SingleOrDefault();
}

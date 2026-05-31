using DG.Tweening;

namespace DolocTown.GameData;

public interface IMonsterMoverProtoDoTween : IMonsterMoverProto
{
	float MoveSpeed { get; }

	Ease MoveEase { get; }
}

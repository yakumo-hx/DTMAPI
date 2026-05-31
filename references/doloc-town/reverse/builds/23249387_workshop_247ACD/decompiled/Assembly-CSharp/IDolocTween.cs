using DG.Tweening;

public interface IDolocTween
{
	bool isPlaying { get; }

	void play();

	void play(TweenCallback cb);

	void forcePlay();

	void forcePlay(TweenCallback cb);

	void join(Tween t);

	void add(Tween t);
}

namespace DolocTown;

public readonly struct ProcessingMission
{
	public readonly Recipe recipe;

	public readonly Case[] containers;

	public readonly Synthesizer machine;

	public readonly int maxTimes;

	public ProcessingMission(Recipe recipe, Case[] containers, Synthesizer machine, int maxTimes)
	{
		this.recipe = recipe;
		this.containers = containers;
		this.machine = machine;
		this.maxTimes = maxTimes;
	}
}

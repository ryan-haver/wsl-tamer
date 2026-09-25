namespace WslTamer.Core.Tests.Functional;

/// <summary>Tests that use real WSL run one at a time: some of them shut WSL down.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WslCollection
{
    public const string Name = "Real WSL";
}

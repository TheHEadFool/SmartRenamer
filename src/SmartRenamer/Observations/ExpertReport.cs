using SmartRenamer.Observations;
using System.Linq;
using System.Collections.Generic;

public class ExpertReport
{
    public List<ExpertFinding> Findings { get; } = new();

    public bool FoundAnything =>
        Findings.Any(f => f.FoundSomething);
}
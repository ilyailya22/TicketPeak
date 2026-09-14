using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TicketPeak.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    public static TheoryData<string> Modules => MonolithArchitecture.Modules;

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_InEachModule_DependsOnlyOnBclKernelAndItsOwnDomain(string module)
    {
        Types().That().ResideInNamespaceMatching($@"^TicketPeak\.Modules\.{module}\.Domain(\..+)?$")
            .Should().OnlyDependOn(Types().That().ResideInNamespaceMatching(
                $@"^(System(\..+)?|TicketPeak\.Shared\.Kernel(\..+)?|TicketPeak\.Modules\.{module}\.Domain(\..+)?)$"))
            .Because("the domain model must be testable and reasoned about with no framework or layer in the way")
            .WithoutRequiringPositiveResults()
            .Check(MonolithArchitecture.Instance);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_DoesNotDependOnAnotherModulesInternals(string module)
    {
        Types().That().ResideInNamespaceMatching($@"^TicketPeak\.Modules\.{module}(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(
                $@"^TicketPeak\.Modules\.(?!{module}\.)\w+\.(Domain|Application|Infrastructure|Endpoints)(\..+)?$"))
            .Because("modules talk to each other only through their I<Module>Api, so any one can be extracted without rewriting its neighbours")
            .WithoutRequiringPositiveResults()
            .Check(MonolithArchitecture.Instance);
    }

    [Fact]
    public void LayerTypes_InEveryModule_AreNotPublic()
    {
        Types().That().ResideInNamespaceMatching(@"^TicketPeak\.Modules\.\w+\.(Domain|Application|Infrastructure|Endpoints)(\..+)?$")
            .Should().NotBePublic()
            .Because("only a module's root namespace (its I<Module>Api and composition module) is a public surface")
            .WithoutRequiringPositiveResults()
            .Check(MonolithArchitecture.Instance);
    }
}

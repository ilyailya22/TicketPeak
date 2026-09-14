using ArchUnitNET.xUnitV3;
using TicketPeak.Shared.Kernel;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TicketPeak.ArchitectureTests;

public sealed class SharedKernelTests
{
    [Fact]
    public void Kernel_DependsOnlyOnTheBcl()
    {
        Types().That().ResideInAssembly(typeof(Result).Assembly)
            .Should().OnlyDependOn(Types().That().ResideInNamespaceMatching(
                @"^(System(\..+)?|TicketPeak\.Shared\.Kernel(\..+)?)$"))
            .Because("every module's Domain references the kernel, so anything the kernel depends on leaks into every domain")
            .Check(MonolithArchitecture.Instance);
    }
}

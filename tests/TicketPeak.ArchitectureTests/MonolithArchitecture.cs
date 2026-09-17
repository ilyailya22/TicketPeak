using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using TicketPeak.Modules.Catalog;
using TicketPeak.Modules.Identity;
using TicketPeak.Modules.Inventory;
using TicketPeak.Modules.Ordering;
using TicketPeak.Modules.Payments;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.ArchitectureTests;

/// <summary>
/// The loaded monolith. Loading is the expensive part, so it happens once; the result is
/// immutable, so sharing it between tests shares no mutable state.
/// </summary>
/// <remarks>
/// Loaded with <c>LoadAssembliesIncludingDependencies</c>, not <c>LoadAssemblies</c>. Do not
/// "simplify" this. With the plain loader, a Domain type exposing <c>MediatR.INotification</c>
/// and a kernel type exposing <c>Autofac.ContainerBuilder</c> both passed the BCL-only rules:
/// dependencies on types from assemblies outside the loaded set were not flagged, so only the
/// rules between TicketPeak's own assemblies worked. Found by deliberately breaking every rule
/// once, as CLAUDE.md requires.
/// </remarks>
internal static class MonolithArchitecture
{
    public static Architecture Instance { get; } = new ArchLoader()
        .LoadAssembliesIncludingDependencies(
            typeof(Result).Assembly,
            typeof(IdentityModule).Assembly,
            typeof(CatalogModule).Assembly,
            typeof(InventoryModule).Assembly,
            typeof(OrderingModule).Assembly,
            typeof(PaymentsModule).Assembly)
        .Build();

    public static TheoryData<string> Modules { get; } = new() { "Identity", "Catalog", "Inventory", "Ordering", "Payments" };
}

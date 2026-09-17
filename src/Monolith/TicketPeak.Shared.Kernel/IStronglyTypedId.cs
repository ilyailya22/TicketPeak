namespace TicketPeak.Shared.Kernel;

/// <summary>
/// Implemented by every identifier, e.g. <c>readonly record struct OrderId(Guid Value)</c>, so an
/// <c>OrderId</c> can never be passed where a <c>SeatId</c> is expected. The shared shape lets
/// Phase 2 map all of them with one EF Core value converter.
/// </summary>
public interface IStronglyTypedId
{
    Guid Value { get; }
}

using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TicketPeak.Api.Composition;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.Api.Tests.Behaviors;

/// <summary>
/// The pipeline as the host actually builds it: real Autofac modules, real MediatR, real generic
/// constraints. The behaviour unit tests prove each piece; these prove the container assembles
/// them the way ADR 0005 claims.
/// </summary>
public sealed class PipelineTests : IDisposable
{
    private readonly RecordingUnitOfWork _unitOfWork = new();
    private readonly HandlerLog _log = new();
    private readonly IContainer _container;

    public PipelineTests() => _container = BuildContainer(_unitOfWork, _log);

    [Fact]
    public async Task Send_AValidCommand_RunsTheHandlerThenSaves()
    {
        Result<Guid> result = await SendAsync(new CreateVenue("Roundhouse"));

        result.IsSuccess.ShouldBeTrue();
        _log.Handled.ShouldBe([nameof(CreateVenue)]);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Send_AnInvalidCommand_NeverReachesTheHandlerOrTheUnitOfWork()
    {
        Result<Guid> result = await SendAsync(new CreateVenue(""));

        result.Error.ShouldNotBeNull().Type.ShouldBe(ErrorType.Validation);
        _log.Handled.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Send_AQuery_IsHandledWithoutEverTouchingTheUnitOfWork()
    {
        using ILifetimeScope scope = _container.BeginLifetimeScope();

        Result<string> result = await scope.Resolve<IMediator>().Send(new FindVenue(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.Value.ShouldBe("Roundhouse");
        _log.Handled.ShouldBe([nameof(FindVenue)]);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    public void Dispose() => _container.Dispose();

    private static IContainer BuildContainer(RecordingUnitOfWork unitOfWork, HandlerLog log)
    {
        ContainerBuilder builder = new();
        builder.Populate(new ServiceCollection().AddLogging());
        builder.RegisterInstance(TimeProvider.System).As<TimeProvider>();
        builder.RegisterModule<MediatorModule>();
        builder.RegisterModule<PipelineModule>();

        builder.RegisterInstance(unitOfWork).As<IUnitOfWork>();
        builder.RegisterInstance(log);
        builder.RegisterType<CreateVenueHandler>().As<IRequestHandler<CreateVenue, Result<Guid>>>();
        builder.RegisterType<FindVenueHandler>().As<IRequestHandler<FindVenue, Result<string>>>();
        builder.RegisterType<CreateVenueValidator>().As<IValidator<CreateVenue>>();

        return builder.Build();
    }

    private async Task<Result<Guid>> SendAsync(CreateVenue command)
    {
        using ILifetimeScope scope = _container.BeginLifetimeScope();
        return await scope.Resolve<IMediator>().Send(command, TestContext.Current.CancellationToken);
    }

    internal sealed record CreateVenue(string Name) : IRequest<Result<Guid>>, ICommand;

    internal sealed record FindVenue(Guid Id) : IRequest<Result<string>>;

    internal sealed class HandlerLog
    {
        public List<string> Handled { get; } = [];
    }

    internal sealed class CreateVenueHandler(HandlerLog log) : IRequestHandler<CreateVenue, Result<Guid>>
    {
        public Task<Result<Guid>> Handle(CreateVenue request, CancellationToken cancellationToken)
        {
            log.Handled.Add(nameof(CreateVenue));
            return Task.FromResult(Result.Success(Guid.NewGuid()));
        }
    }

    internal sealed class FindVenueHandler(HandlerLog log) : IRequestHandler<FindVenue, Result<string>>
    {
        public Task<Result<string>> Handle(FindVenue request, CancellationToken cancellationToken)
        {
            log.Handled.Add(nameof(FindVenue));
            return Task.FromResult(Result.Success("Roundhouse"));
        }
    }

    internal sealed class CreateVenueValidator : AbstractValidator<CreateVenue>
    {
        public CreateVenueValidator() => RuleFor(command => command.Name).NotEmpty().WithMessage("A venue needs a name.");
    }
}

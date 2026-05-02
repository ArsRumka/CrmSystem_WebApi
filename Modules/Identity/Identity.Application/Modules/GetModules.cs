using BuildingBlocks.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Common;
using Identity.Application.Contracts;
using MediatR;

namespace Identity.Application.Modules;

public sealed record GetModulesQuery : IRequest<IReadOnlyList<ModuleResponse>>;

public sealed class GetModulesQueryHandler : IRequestHandler<GetModulesQuery, IReadOnlyList<ModuleResponse>>
{
    private readonly IModuleRepository _moduleRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetModulesQueryHandler(IModuleRepository moduleRepository, ICurrentUserService currentUserService)
    {
        _moduleRepository = moduleRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ModuleResponse>> Handle(GetModulesQuery request, CancellationToken cancellationToken)
    {
        HandlerGuards.RequireUserId(_currentUserService);

        var modules = await _moduleRepository.GetAllAsync(cancellationToken);

        return modules
            .Select(module => new ModuleResponse(module.Id, module.Code, module.Name))
            .ToList();
    }
}

namespace EDCL.Shared.Kernel.Events;

using MediatR;
using System.Collections.Generic;

public sealed record ManifestsAssignedToRouteIntegrationEvent(
    IReadOnlyList<string> ManifestNos,
    bool IsAssigned) : INotification;

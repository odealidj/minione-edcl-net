using System;

namespace EDCL.Module.Driver.Application.DTOs;

public sealed record TruckDriverAssignmentDto(
    long TruckId,
    string PlateNumber,
    long TransporterId,
    string? TransporterName,
    long DriverId,
    string DriverName,
    string DriverNik,
    DateTime AssignedAt
);

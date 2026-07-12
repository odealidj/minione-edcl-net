using System;

namespace EDCL.Module.Driver.Application.DTOs;

public sealed record TruckDriverAssignmentDto(
    long TruckId,
    string PlateNumber,
    long LogisticPartnerId,
    string? LogisticPartnerName,
    long DriverId,
    string DriverName,
    string DriverNik,
    DateTime AssignedAt
);

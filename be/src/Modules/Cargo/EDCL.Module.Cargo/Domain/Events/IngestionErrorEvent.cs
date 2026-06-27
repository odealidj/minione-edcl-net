using System;

namespace EDCL.Module.Cargo.Domain.Events;

public record IngestionErrorEvent(
    string EventType,
    string Payload,
    string ErrorMessage,
    string StackTrace,
    DateTime OccurredAt
);

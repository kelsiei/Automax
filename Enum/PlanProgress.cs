namespace CarCareTracker.Enum;

public enum PlanProgress
{
    NotStarted = 0,
    InProgress = 1,
    OnHold = 2,
    Done = 3
    // TODO: ensure validation rules around Done on create/update are enforced in API layer.
}

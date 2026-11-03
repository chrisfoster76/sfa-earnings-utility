namespace SFA.DAS.CommitmentsV2.Messages.Events;

// Shaped as Learning's copy (the agreed array), not Approvals' current dictionary.

public class LearningChangeApprovedEvent : LearningChangeEvent
{
}

public class LearningChangeRejectedEvent : LearningChangeEvent
{
}

public class LearningChangeEvent
{
    public Guid LearningKey { get; set; }
    public long ApprenticeshipId { get; set; }
    public List<Change> Changes { get; set; } = new();

    public class Change
    {
        public string ChangeType { get; set; } = string.Empty;
        public ChangeData Data { get; set; } = new();
    }

    public class ChangeData
    {
        public string? Old { get; set; }
        public string? New { get; set; }
    }
}

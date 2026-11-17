namespace SFA.DAS.CommitmentsV2.Messages.Events;

// Matches das-commitments master LearningChangeEvents.cs.

public class LearningChangeApprovedEvent : LearningChangeEvent
{
}

public class LearningChangeRejectedEvent : LearningChangeEvent
{
    public string Reason { get; set; } = string.Empty;
}

public class LearningChangeEvent
{
    public Guid LearningKey { get; set; }
    public long ApprenticeshipId { get; set; }
    public List<Change> Changes { get; set; } = new();
    public List<Price>? Prices { get; set; }

    public class Change
    {
        public string ChangeType { get; set; } = string.Empty;
        public string? ApprovalStatus { get; set; }
        public ChangeData Data { get; set; } = new();
    }

    public class ChangeData
    {
        public string? Old { get; set; }
        public string? New { get; set; }
    }

    public class Price
    {
        public decimal? TrainingPrice { get; set; }
        public decimal? AssessmentPrice { get; set; }
        public string? EffectiveFrom { get; set; }
    }
}

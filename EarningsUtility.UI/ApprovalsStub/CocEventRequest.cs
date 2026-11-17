using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.UI.ApprovalsStub;

public record CocEventRequest(
    bool Approved,
    Guid LearningKey,
    long ApprenticeshipId,
    List<LearningChangeEvent.Change> Changes,
    List<LearningChangeEvent.Price>? Prices = null);

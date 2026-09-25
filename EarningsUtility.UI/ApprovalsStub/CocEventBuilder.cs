using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// Builds the async leg of the CoC flow — the LearningChangeApprovedEvent/RejectedEvent
/// Commitments publishes once an employer has acted on a pending change (see
/// tickets/upcoming/approvals-integration.md#gaps-vs-current-code). das-api-stub only fakes
/// the synchronous PUT approvals/{learningKey} leg, so exercising a Learning-side consumer of
/// this event needs it published directly onto the service bus, same as the "approve" flow.
/// </summary>
public static class CocEventBuilder
{
    public static LearningChangeEvent Build(CocEventRequest request)
    {
        LearningChangeEvent result = request.Approved
            ? new LearningChangeApprovedEvent()
            : new LearningChangeRejectedEvent();

        result.LearningKey = request.LearningKey;
        result.ApprenticeshipId = request.ApprenticeshipId;
        result.Changes = request.Changes;
        return result;
    }
}

using EarningsUtility.UI.ApprovalsStub;
using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.Tests.ApprovalsStub;

public class CocEventBuilderTests
{
    [Fact]
    public void Build_returns_LearningChangeApprovedEvent_when_approved()
    {
        var request = new CocEventRequest(true, Guid.NewGuid(), 99, new());

        var result = CocEventBuilder.Build(request);

        Assert.IsType<LearningChangeApprovedEvent>(result);
    }

    [Fact]
    public void Build_returns_LearningChangeRejectedEvent_when_not_approved()
    {
        var request = new CocEventRequest(false, Guid.NewGuid(), 99, new());

        var result = CocEventBuilder.Build(request);

        Assert.IsType<LearningChangeRejectedEvent>(result);
    }

    [Fact]
    public void Build_carries_learning_key_apprenticeship_id_and_changes_through_unchanged()
    {
        var learningKey = Guid.NewGuid();
        var changes = new Dictionary<string, LearningChangeEvent.Change>
        {
            ["TrainingPrice"] = new() { Old = "25000", New = "27000", EffectiveFromDate = new DateTime(2026, 8, 1) }
        };
        var request = new CocEventRequest(true, learningKey, 99, changes);

        var result = CocEventBuilder.Build(request);

        Assert.Equal(learningKey, result.LearningKey);
        Assert.Equal(99, result.ApprenticeshipId);
        Assert.Same(changes, result.Changes);
    }
}

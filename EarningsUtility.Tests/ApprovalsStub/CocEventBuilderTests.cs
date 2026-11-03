using System.Text.Json;
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
        var changes = new List<LearningChangeEvent.Change>
        {
            new() { ChangeType = "TrainingPrice", Data = new() { Old = "25000", New = "27000" } }
        };
        var request = new CocEventRequest(true, learningKey, 99, changes);

        var result = CocEventBuilder.Build(request);

        Assert.Equal(learningKey, result.LearningKey);
        Assert.Equal(99, result.ApprenticeshipId);
        Assert.Same(changes, result.Changes);
    }

    [Fact]
    public void Built_event_serialises_changes_as_the_agreed_array_shape()
    {
        var learningKey = Guid.Parse("98ef8fee-dc63-4005-b20a-8723999f5ca8");
        var changes = new List<LearningChangeEvent.Change>
        {
            new() { ChangeType = "StartDate", Data = new() { Old = "2026-08-07", New = "2026-09-07" } }
        };
        var request = new CocEventRequest(true, learningKey, 7501659094, changes);

        var json = JsonSerializer.Serialize(CocEventBuilder.Build(request), typeof(LearningChangeApprovedEvent));

        Assert.Equal(
            """{"LearningKey":"98ef8fee-dc63-4005-b20a-8723999f5ca8","ApprenticeshipId":7501659094,"Changes":[{"ChangeType":"StartDate","Data":{"Old":"2026-08-07","New":"2026-09-07"}}]}""",
            json);
    }
}

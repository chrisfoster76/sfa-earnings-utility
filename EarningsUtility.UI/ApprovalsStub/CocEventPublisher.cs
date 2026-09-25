using NServiceBus;

namespace EarningsUtility.UI.ApprovalsStub;

public static class CocEventPublisher
{
    public static async Task Publish(IEndpointInstance endpointInstance, CocEventRequest request)
    {
        var eventMessage = CocEventBuilder.Build(request);
        await endpointInstance.Publish(eventMessage).ConfigureAwait(false);

        var typeLabel = request.Approved ? "LearningChangeApprovedEvent" : "LearningChangeRejectedEvent";
        ConsoleWriter.WriteColor($"{typeLabel} sent for learningKey {request.LearningKey} (Apprenticeship ID: {request.ApprenticeshipId}).", ConsoleColor.Green);
    }
}

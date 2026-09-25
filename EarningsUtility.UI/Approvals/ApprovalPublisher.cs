using NServiceBus;
using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.UI.Approvals;

public static class ApprovalPublisher
{
    public static async Task Publish(IEndpointInstance endpointInstance, ApprovalRequest request)
    {
        var eventMessage = ApprovalEventBuilder.Build(request);
        await endpointInstance.Publish(eventMessage).ConfigureAwait(false);

        var typeLabel = request.LearningType == LearningType.Apprenticeship ? "Apprenticeship" : "Short Course";
        ConsoleWriter.WriteColor($"{typeLabel} Approval sent for ULN {request.Uln} (Employer Account ID: {request.EmployerAccountId}).", ConsoleColor.Green);
    }
}

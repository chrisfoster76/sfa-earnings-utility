using System.Text.Json;
using Azure.Identity;
using EarningsUtility.UI.ApprovalsStub;
using EarningsUtility.UI.Approvals;
using EarningsUtility.UI.Cli;
using Microsoft.Extensions.Configuration;
using NServiceBus.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;
using static EarningsUtility.UI.ConsoleWriter;

namespace EarningsUtility.UI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // The CoC mappings screen draws its two-pane layout with box-drawing characters —
            // needs UTF-8 output to render correctly on Windows conhost. Swallow failures (e.g.
            // redirected/piped output in CI) since the console is unused in one-shot mode anyway.
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch (IOException) { }

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var appSettings = configuration.Get<AppSettings>()
                ?? throw new InvalidOperationException("appsettings.json could not be bound to AppSettings.");

            if (appSettings.Environments.Count == 0)
                throw new InvalidOperationException("No environments configured in appsettings.json.");

            var cliOptions = CliOptions.Parse(args);
            var cliEnv = cliOptions.Env;
            var cliUln = cliOptions.Uln;
            var cliEmployer = cliOptions.Employer;
            var cliEmployerType = cliOptions.EmployerType;
            var cliApprenticeshipId = cliOptions.ApprenticeshipId;
            var cliTransferSender = cliOptions.TransferSender;
            var cliType = cliOptions.Type;
            var cliUkprn = cliOptions.Ukprn;
            var cliTrainingCode = cliOptions.TrainingCode;
            bool oneShot = cliOptions.IsOneShot;

            var actionName = cliOptions.ActionOrDefault;
            if (actionName == "coc-setup") return await RunCocSetupOneShot(appSettings, cliOptions);
            if (actionName == "coc-find") return await RunCocFindOneShot(appSettings, cliOptions);
            if (actionName == "coc-delete") return await RunCocDeleteOneShot(appSettings, cliOptions);
            if (actionName == "coc-event") return await RunCocEventOneShot(appSettings, cliOptions);
            if (actionName != "approve")
            {
                Console.Error.WriteLine($"Unknown --action '{cliOptions.Action}'. Expected: approve, coc-setup, coc-find, coc-delete, coc-event.");
                return 1;
            }

            string selectedEnvName;
            string selectedNamespace;

            if (oneShot)
            {
                if (!appSettings.Environments.TryGetValue(cliEnv!, out var ns))
                {
                    Console.Error.WriteLine($"Unknown environment '{cliEnv}'. Available: {string.Join(", ", appSettings.Environments.Keys)}");
                    return 1;
                }
                selectedEnvName = cliEnv!;
                selectedNamespace = ns;
            }
            else
            {
                Console.Clear();
                WriteColor("========================================", ConsoleColor.Cyan);
                WriteColor("  SFA Earnings Utility", ConsoleColor.Cyan);
                WriteColor("========================================", ConsoleColor.Cyan);
                Console.WriteLine();
                (selectedEnvName, selectedNamespace) = Menu.SelectEnvironment(appSettings.Environments);
            }

            if (oneShot)
            {
                var endpointInstance = await ConnectToServiceBus(selectedEnvName, selectedNamespace);

                var employerAccountId = long.Parse(cliEmployer!);
                var employerType = Enum.Parse<ApprenticeshipEmployerType>(cliEmployerType!, ignoreCase: true);
                var apprenticeshipId = long.Parse(cliApprenticeshipId!);
                long? transferSenderId = long.TryParse(cliTransferSender, out var ts) ? ts : null;
                var ukprn = long.Parse(cliUkprn!);
                var learningType = cliType!.Equals("Apprenticeship", StringComparison.OrdinalIgnoreCase)
                    ? LearningType.Apprenticeship
                    : LearningType.ApprenticeshipUnit;
                Console.WriteLine();
                WriteColor("Sending...", ConsoleColor.DarkGray);
                var oneShotRequest = new ApprovalRequest(cliUln!, employerAccountId, employerType, apprenticeshipId, learningType, ukprn, cliTrainingCode!, transferSenderId);
                await ApprovalPublisher.Publish(endpointInstance, oneShotRequest);

                await endpointInstance.Stop().ConfigureAwait(false);
            }
            else
            {
                Console.WriteLine();
                var endpointInstance = await ConnectToServiceBus(selectedEnvName, selectedNamespace);

                ApprovalsStubClient? stubClient = null;

                Console.Clear();
                WriteMenuHeader(selectedEnvName);

                while (true)
                {
                    var action = Menu.SelectAction();
                    if (action == Menu.Action.Exit)
                        break;

                    Console.WriteLine();

                    switch (action)
                    {
                        case Menu.Action.Approve:
                            var request = ApprovalPrompts.Collect();
                            Console.WriteLine();
                            WriteColor("Sending...", ConsoleColor.DarkGray);
                            await ApprovalPublisher.Publish(endpointInstance, request);
                            break;

                        case Menu.Action.CocMaintain:
                            stubClient ??= CreateApprovalsStubClient(appSettings, selectedEnvName);
                            if (stubClient == null) break;
                            await CocMappingsScreen.RunAsync(stubClient, appSettings.ApprovalsUrlPrefix);
                            Console.Clear();
                            WriteMenuHeader(selectedEnvName);
                            break;

                        case Menu.Action.CocEvent:
                            stubClient ??= CreateApprovalsStubClient(appSettings, selectedEnvName);
                            if (stubClient == null) break;
                            var eventRequest = await CocEventPrompts.CollectAsync(stubClient, appSettings.ApprovalsUrlPrefix);
                            if (eventRequest == null) break;

                            var eventMessage = CocEventBuilder.Build(eventRequest);
                            var eventJson = JsonSerializer.Serialize(eventMessage, new JsonSerializerOptions { WriteIndented = true });
                            Console.WriteLine();
                            WriteColor("Preview:", ConsoleColor.White);
                            WriteColor(eventJson, ConsoleColor.DarkGray);
                            Console.WriteLine();
                            if (!Menu.Confirm("Send this event? (Enter to send, Escape to cancel): "))
                                break;

                            WriteColor("Sending...", ConsoleColor.DarkGray);
                            await CocEventPublisher.Publish(endpointInstance, eventRequest);
                            break;
                    }

                    Console.WriteLine();
                }

                WriteColor("Goodbye.", ConsoleColor.DarkGray);

                await endpointInstance.Stop().ConfigureAwait(false);
            }

            return 0;
        }

        private static void WriteMenuHeader(string envName)
        {
            WriteColor("========================================", ConsoleColor.Cyan);
            WriteColor("  SFA Earnings Utility", ConsoleColor.Cyan);
            WriteColor("========================================", ConsoleColor.Cyan);
            Console.WriteLine();
            WriteColor($"Environment: {envName}", ConsoleColor.Green);
            Console.WriteLine();
        }

        private static async Task<int> RunCocSetupOneShot(AppSettings appSettings, CliOptions cliOptions)
        {
            if (cliOptions.Env == null || !appSettings.Environments.ContainsKey(cliOptions.Env))
            {
                Console.Error.WriteLine($"Unknown environment '{cliOptions.Env}'. Available: {string.Join(", ", appSettings.Environments.Keys)}");
                return 1;
            }

            if (cliOptions.LearningKey == null || !Guid.TryParse(cliOptions.LearningKey, out var learningKey))
            {
                Console.Error.WriteLine("--learning-key is required and must be a valid GUID for --action coc-setup.");
                return 1;
            }

            if (!CocOutcomeMapper.TryParse(cliOptions.Outcome, out var outcome))
            {
                Console.Error.WriteLine("--outcome is required for --action coc-setup. Expected: approved, rejected, pending.");
                return 1;
            }

            if (!CocChangeKindMapper.TryParseMany(cliOptions.ChangeType, out var changeKinds))
            {
                Console.Error.WriteLine("--change-type is required for --action coc-setup. Expected one or more of: price, startdate (comma-separated, e.g. price,startdate).");
                return 1;
            }

            var stubClient = CreateApprovalsStubClient(appSettings, cliOptions.Env);
            if (stubClient == null) return 1;

            try
            {
                // One-shot mode keeps a single --outcome flag applied uniformly to every
                // selected --change-type — the swagger contract allows per-field outcomes (see
                // approvals-integration.md), but interactive mode is where that granularity is
                // exposed; scripted callers wanting mixed outcomes can issue separate coc-setup
                // calls against the same learningKey (an upsert, so the last call's kinds win —
                // note that would currently overwrite rather than merge, since the request body
                // is the whole mapping, not a per-field patch).
                var outcomes = changeKinds.ToDictionary(k => k, _ => outcome);
                await stubClient.RegisterApproval(learningKey, outcomes);
                WriteColor($"Registered approvals/{learningKey} as {string.Join(" + ", changeKinds)} -> {outcome.ToApprovalStatus()}.", ConsoleColor.Green);
                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Failed to register mapping: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunCocFindOneShot(AppSettings appSettings, CliOptions cliOptions)
        {
            if (cliOptions.Env == null || !appSettings.Environments.ContainsKey(cliOptions.Env))
            {
                Console.Error.WriteLine($"Unknown environment '{cliOptions.Env}'. Available: {string.Join(", ", appSettings.Environments.Keys)}");
                return 1;
            }

            var stubClient = CreateApprovalsStubClient(appSettings, cliOptions.Env);
            if (stubClient == null) return 1;

            var found = await stubClient.FindMappings(appSettings.ApprovalsUrlPrefix);
            PrintMappings(found);
            return 0;
        }

        private static async Task<int> RunCocDeleteOneShot(AppSettings appSettings, CliOptions cliOptions)
        {
            if (cliOptions.Env == null || !appSettings.Environments.ContainsKey(cliOptions.Env))
            {
                Console.Error.WriteLine($"Unknown environment '{cliOptions.Env}'. Available: {string.Join(", ", appSettings.Environments.Keys)}");
                return 1;
            }

            var stubClient = CreateApprovalsStubClient(appSettings, cliOptions.Env);
            if (stubClient == null) return 1;

            var matches = await stubClient.FindMappings(appSettings.ApprovalsUrlPrefix);
            if (matches.Count == 0)
            {
                WriteColor("No mappings found.", ConsoleColor.Yellow);
                return 0;
            }

            if (matches.Count > 1 && !cliOptions.All)
            {
                Console.Error.WriteLine($"{matches.Count} mappings match '{appSettings.ApprovalsUrlPrefix}'. Pass --all to delete every match:");
                PrintMappings(matches);
                return 1;
            }

            var exitCode = 0;
            foreach (var mapping in matches)
            {
                try
                {
                    await stubClient.DeleteMapping(mapping.HttpMethod, mapping.Url);
                    WriteColor($"Deleted {mapping.HttpMethod} {mapping.Url}", ConsoleColor.Green);
                }
                catch (HttpRequestException ex)
                {
                    Console.Error.WriteLine($"Failed to delete {mapping.Url}: {ex.Message}");
                    exitCode = 1;
                }
            }
            return exitCode;
        }

        private static async Task<int> RunCocEventOneShot(AppSettings appSettings, CliOptions cliOptions)
        {
            if (cliOptions.Env == null || !appSettings.Environments.TryGetValue(cliOptions.Env, out var ns))
            {
                Console.Error.WriteLine($"Unknown environment '{cliOptions.Env}'. Available: {string.Join(", ", appSettings.Environments.Keys)}");
                return 1;
            }

            if (cliOptions.LearningKey == null || !Guid.TryParse(cliOptions.LearningKey, out var learningKey))
            {
                Console.Error.WriteLine("--learning-key is required and must be a valid GUID for --action coc-event.");
                return 1;
            }

            if (cliOptions.ApprenticeshipId == null || !long.TryParse(cliOptions.ApprenticeshipId, out var apprenticeshipId))
            {
                Console.Error.WriteLine("--apprenticeship-id is required and must be numeric for --action coc-event.");
                return 1;
            }

            bool approved;
            switch (cliOptions.EventType?.Trim().ToLowerInvariant())
            {
                case "approved": approved = true; break;
                case "rejected": approved = false; break;
                default:
                    Console.Error.WriteLine("--event-type is required for --action coc-event. Expected: approved, rejected.");
                    return 1;
            }

            if (string.IsNullOrWhiteSpace(cliOptions.Field))
            {
                Console.Error.WriteLine("--field is required for --action coc-event.");
                return 1;
            }

            DateTime? effectiveFrom = null;
            if (!string.IsNullOrWhiteSpace(cliOptions.EffectiveFrom))
            {
                if (!DateTime.TryParse(cliOptions.EffectiveFrom, out var parsed))
                {
                    Console.Error.WriteLine($"--effective-from '{cliOptions.EffectiveFrom}' could not be parsed as a date.");
                    return 1;
                }
                effectiveFrom = parsed;
            }

            var changes = new Dictionary<string, LearningChangeEvent.Change>
            {
                [cliOptions.Field] = new LearningChangeEvent.Change
                {
                    Old = cliOptions.OldValue,
                    New = cliOptions.NewValue,
                    EffectiveFromDate = effectiveFrom
                }
            };

            var endpointInstance = await ConnectToServiceBus(cliOptions.Env, ns);
            var request = new CocEventRequest(approved, learningKey, apprenticeshipId, changes);
            await CocEventPublisher.Publish(endpointInstance, request);
            await endpointInstance.Stop().ConfigureAwait(false);
            return 0;
        }

        private static async Task<IEndpointInstance> ConnectToServiceBus(string envName, string selectedNamespace)
        {
            WriteColor($"Connecting to {envName}...", ConsoleColor.DarkGray);

            LogManager.Use<DefaultFactory>().Level(LogLevel.Fatal);

            var endpointConfiguration = new EndpointConfiguration("Test.Sender");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.SendOnly();

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.UseWebSockets();
            transport.CustomTokenCredential(selectedNamespace, new DefaultAzureCredential());

            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            var conventions = endpointConfiguration.Conventions();
            conventions.DefiningEventsAs(type => type.Name.EndsWith("Event"))
                       .DefiningCommandsAs(type => type.Namespace != null && type.Name.EndsWith("Command"));

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
            WriteColor("Connected.", ConsoleColor.Green);
            return endpointInstance;
        }

        private static ApprovalsStubClient? CreateApprovalsStubClient(AppSettings appSettings, string envName)
        {
            if (!appSettings.ApprovalsStubBaseUrl.TryGetValue(envName, out var baseUrl))
            {
                WriteColor($"No approvals-stub configured for '{envName}' — add it under ApprovalsStubBaseUrl in appsettings.json.", ConsoleColor.Red);
                return null;
            }

            var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            return new ApprovalsStubClient(httpClient);
        }

        private static void PrintMappings(IReadOnlyList<MappingData> mappings)
        {
            if (mappings.Count == 0)
            {
                WriteColor("No mappings found.", ConsoleColor.Yellow);
                return;
            }

            for (int i = 0; i < mappings.Count; i++)
            {
                var m = mappings[i];
                WriteColor($"  {i + 1}. {m.HttpMethod} {m.Url} -> {m.HttpStatusCode}", ConsoleColor.Yellow);
            }
        }
    }
}

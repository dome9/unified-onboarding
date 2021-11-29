using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class UpdateStackWorkflow : OnboardingWorkflowBase
    {
        private readonly OnboardingType _onboardingType;
        public UpdateStackWorkflow(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService, bool isUserBased) 
            : base(apiProvider, retryAndBackoffService)
        {
            _onboardingType = isUserBased ? OnboardingType.UserBased : OnboardingType.RoleBased;
        }
       
        private IStep CreateStep(ConfigurationResponseModel configuration, Enums.Feature feature, OnboardingRequest request)
        {
            switch (feature)
            {
                case Enums.Feature.Permissions:
                    if (_onboardingType == OnboardingType.RoleBased)
                    {
                        return new PermissionsStackUpdateStep(_apiProvider, _retryAndBackoffService, request.S3BucketName, request.AwsAccountRegion, configuration.PermissionsStackName, configuration.PermissionsTemplateS3Path, configuration.CloudGuardAwsAccountId, configuration.RoleExternalTrustSecret, request.OnboardingId);

                    }
                    else if (_onboardingType == OnboardingType.UserBased)
                    {
                        return new PermissionsUserBasedStackUpdateStep(_apiProvider, _retryAndBackoffService, configuration.PermissionsStackName, request.AwsAccountRegion, request.S3BucketName, configuration.PermissionsTemplateS3Path, request.OnboardingId, request.AwsPartition);
                    }
                    else
                    {
                        throw new NotImplementedException($"Unupported {nameof(OnboardingType)} '{_onboardingType}'");
                    }

                case Enums.Feature.ServerlessProtection:
                    return new EmptyStep();

                case Enums.Feature.Intelligence:
                    return new EmptyStep();

                default:
                    throw new NotImplementedException($"Unsupported feature '{feature}'");
            }
        }

        public override async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            var cfnWrapper = CloudFormationWrapper.Get();
            {
                try
                {
                    _apiProvider.SetLocalCredentials(new ServiceAccount(request.CloudGuardApiKeyId, request.CloudGuardApiKeySecret, request.ApiBaseUrl));
                    var configStep = new GetConfigurationStep(_apiProvider, _retryAndBackoffService, request.OnboardingId, request.Version);
                    await ExecuteStep(configStep);


                    var tasks = ChildStacksConfig.GetSupportedFeaturesStackNames(_onboardingType)
                        .Select(f => Task.Run(async () => { await ExecuteStep(CreateStep(configStep.Configuration, f.Key, request)); }));

                    // wait until all the update tasks are finished
                    await Task.WhenAll(tasks);
                }
                catch (CloudGuardUnauthorizedException ex)
                {
                    Console.WriteLine($"[{nameof(UpdateStackWorkflow)}.{nameof(RunAsync)}][ERROR] The CloudGuardOrchestrator resource (CloudGuardOrchestratorInvoke) may have had an 'Update' request triggered without valid credentials. Error={ex}");
                }
                catch (AggregateException ex)
                {
                    List<string> failedStacksNames = new List<string>();
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (innerEx is OnboardingUpdateStackException)
                        {
                            var e = innerEx as OnboardingUpdateStackException;
                            failedStacksNames.Add(e?.StackName);
                            Console.WriteLine($"[{nameof(UpdateStackWorkflow)}.{nameof(RunAsync)}][ERROR] Feature='{e?.Feature}', StackName='{e?.StackName}', Error={e?.Message}");
                        }
                        else
                        {
                            Console.WriteLine($"[{nameof(UpdateStackWorkflow)}.{nameof(RunAsync)}][ERROR] Error={innerEx.Message}");
                        }
                    }
                }
                finally
                {
                    await customResourceResponseHandler.PostbackSuccess();
                }
            }
        }       
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;
using Dome9.CloudGuardOnboarding.Orchestrator.CloudGuardApi;
using Dome9.CloudGuardOnboarding.Orchestrator.Retry;
using Dome9.CloudGuardOnboarding.Orchestrator.AwsSecretsManager;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class UpdateStackWorkflow : OnboardingWorkflowBase
    {
        private readonly OnboardingType _onboardingType;
        private readonly bool _isUserBased;
        public UpdateStackWorkflow(bool isUserBased)
        {
            _onboardingType = isUserBased ? OnboardingType.UserBased : OnboardingType.RoleBased;
            _isUserBased = isUserBased;
        }
       
        private IStep CreateStep(ConfigurationResponseModel configuration, Enums.Feature feature, OnboardingRequest request)
        {
            switch (feature)
            {
                case Enums.Feature.Permissions:
                    if (_onboardingType == OnboardingType.RoleBased)
                    {
                        return new PermissionsStackUpdateStep(request.S3BucketName, request.AwsAccountRegion, configuration.PermissionsStackName, configuration.PermissionsTemplateS3Path, configuration.CloudGuardAwsAccountId, configuration.RoleExternalTrustSecret, request.OnboardingId, request.UniqueSuffix);

                    }
                    else if (_onboardingType == OnboardingType.UserBased)
                    {
                        return new PermissionsUserBasedStackUpdateStep(configuration.PermissionsStackName, request.AwsAccountRegion, request.S3BucketName, configuration.PermissionsTemplateS3Path, request.OnboardingId, request.AwsPartition, request.UniqueSuffix);
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

        public override async Task RunAsync(CloudFormationRequest cloudFormationRequest, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            var request = cloudFormationRequest.ResourceProperties;
            var oldRequest = cloudFormationRequest.OldResourceProperties;
            using (var cfnWrapper = CloudFormationWrapper.Get())
            {
                try
                {
                    var serviceAccount = new ServiceAccount(request.CloudGuardApiKeyId, request.CloudGuardApiKeySecret, request.ApiBaseUrl);
                    var replaceServiceAccountStep = new ReplaceServiceAccountStep(serviceAccount, request.OnboardingId, OnboardingAction.Update);
                    await ExecuteStep(replaceServiceAccountStep);

                    if (oldRequest != null)
                    {
                        if (oldRequest.EnableRemoteStackModify != request.EnableRemoteStackModify)
                        {
                            ApiCredentials cred = null;
                            if (_isUserBased)
                            {
                                cred = await SecretsManagerWrapper.Get().GetCredentialsFromSecretsManager("CloudGuardOnboardingStackModifyPermissions");
                            }
                            var switchModeStep = new SwitchManagedModeStep(request.OnboardingId, bool.Parse(request.EnableRemoteStackModify), request.OnboardingStackModifyRoleArn, cred);
                            await ExecuteStep(switchModeStep);
                        }
                    }

                    var configStep = new GetConfigurationStep(request.OnboardingId, request.Version, OnboardingAction.Update);
                    await ExecuteStep(configStep);

                    var configuration = configStep.Configuration;
                    configuration.SetStackNameSuffix(request.UniqueSuffix);

                    var tasks = ChildStacksConfig.GetSupportedFeaturesStackNames(_onboardingType)
                        .Select(f => Task.Run(async () => { await ExecuteStep(CreateStep(configStep.Configuration, f.Key, request)); }));

                    // wait until all the update tasks are finished
                    await Task.WhenAll(tasks);

                    var updateOnboardingVersionStep = new UpdateOnboardingVersionStep(request.OnboardingId, request.Version, OnboardingAction.Update);
                    await ExecuteStep(updateOnboardingVersionStep);
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
                    await TryDeleteServiceAccount(request.OnboardingId);
                    await customResourceResponseHandler.PostbackSuccess();
                }
            }
        }

        private async Task TryDeleteServiceAccount(string onboardingId)
        {
            try
            {
                // Delete the service account if possible
                await ExecuteStep(new DeleteServiceAccountStep(onboardingId, OnboardingAction.Update));
            }
            catch 
            {
            }
        }
    }
}
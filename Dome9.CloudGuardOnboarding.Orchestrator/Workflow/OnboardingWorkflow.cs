﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dome9.CloudGuardOnboarding.Orchestrator.Steps;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class OnboardingWorkflow : OnboardingWorkflowBase
    {
        public OnboardingWorkflow(ICloudGuardApiWrapper apiProvider, IRetryAndBackoffService retryAndBackoffService) : base(apiProvider, retryAndBackoffService) { }

        public override async Task RunAsync(OnboardingRequest request, LambdaCustomResourceResponseHandler customResourceResponseHandler)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException($"{nameof(OnboardingRequest)} {nameof(request)} is null");
                }

                if (string.IsNullOrWhiteSpace(request.OnboardingId))
                {
                    throw new ArgumentException($"{nameof(request.OnboardingId)} is null or whitespace");
                }

                // 0. set the credentials step - will delete the service account on rollback in case of workflow error
                var initServiceAccountStep = new InitServiceAccountStep(_apiProvider, _retryAndBackoffService, request.OnboardingId, request.CloudGuardApiKeyId, request.CloudGuardApiKeySecret, request.ApiBaseUrl);
                await ExecuteStep(initServiceAccountStep);

                Console.WriteLine($"[INFO] Executing onboarding process - OnboardingId: {request?.OnboardingId}");

                // 1. create new service account and delete initial service account with exposed credentials (stack url containing the credentials could be passed around)               
                var replaceServiceAccountStep = new ReplaceServiceAccountStep(_apiProvider, _retryAndBackoffService, initServiceAccountStep.ServiceAccount, request.OnboardingId);
                await ExecuteStep(replaceServiceAccountStep);
                initServiceAccountStep.ServiceAccount = replaceServiceAccountStep.ServiceAccount;

                // 2.  validate onboarding id
                await ExecuteStep(new ValidateOnboardingStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 3. get configuration from API
                var configurationStep = await ExecuteStep(new GetConfigurationStep(_apiProvider, _retryAndBackoffService, request.OnboardingId, request.Version));
                var configuration = (configurationStep as GetConfigurationStep).Configuration;
                configuration.SetStackNameSuffix(request.UniqueSuffix);

                // 4. run the Permissions stack (create cross account role for CloudGuard)
                var permissionsStackStep = new PermissionsStackCreationStep(_apiProvider, _retryAndBackoffService, request.S3BucketName, request.AwsAccountRegion, configuration.PermissionsStackName, configuration.PermissionsTemplateS3Path, configuration.CloudGuardAwsAccountId, configuration.RoleExternalTrustSecret, request.OnboardingId, request.UniqueSuffix);
                await ExecuteStep(permissionsStackStep);

                // 5. complete onboarding - create cloud account, rulesets, serverless account if selected
                await ExecuteStep(new AccountCreationStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.AwsAccountRegion, request.OnboardingId, request.OnboardingStackModifyRoleArn, request.RootStackId, null, null, permissionsStackStep.CrossAccountRoleArn));

                // 6. create Posture policies - create cloud account, rulesets, serverless account if selected
                await ExecuteStep(new CreatePosturePoliciesStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));


                // serverless - do not fail workflow on exceptions
                try
                {
                    if (configuration.ServerlessProtectionEnabled)
                    {
                        if (configuration.ServerlessCftRegion != request.AwsAccountRegion)
                        {
                            Console.WriteLine($"[ERROR] Failed handling Serverless protection - can not run Serverless protection CFT in the {request.AwsAccountRegion} region, Serverless protection CFT must run in {configuration.ServerlessCftRegion} region.");
                            await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Enums.Feature.ServerlessProtection, Enums.Status.ERROR, $"Can not run Serverless protection CFT in the {request.AwsAccountRegion} region, Serverless protection CFT must run in {configuration.ServerlessCftRegion} region.", null, null, null)));
                        }
                        else
                        {
                            // 7. add serverless protection account
                            await ExecuteStep(new ServerlessAddAccountStep(_apiProvider, _retryAndBackoffService, request.AwsAccountId, request.OnboardingId));

                            // 8. create serverless protection stack if enabled
                            await ExecuteStep(new ServerlessStackCreationStep(_apiProvider, _retryAndBackoffService, request.S3BucketName, request.AwsAccountRegion, request.OnboardingId, configuration.ServerlessTemplateS3Path, configuration.ServerlessStackName, request.UniqueSuffix, configuration.CloudGuardAwsAccountId, configuration.ServerlessStage, configuration.ServerlessCftRegion));
                        }
                    }
                    else
                    {
                        await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Enums.Feature.ServerlessProtection, Enums.Status.INACTIVE, "Serverless protection disabled", null, null, null)));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed handling Serverless protection. Error={ex}");
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Enums.Feature.ServerlessProtection, Enums.Status.ERROR, "Failed to acivate Serverless protection", null, null, null)));
                }

                // 9. Intelligence step - do not fail workflow on exceptions
                try
                {
                    if (configuration.IntelligenceEnabled)
                    {
                        await ExecuteStep(new IntelligenceCloudTrailStep(_apiProvider, _retryAndBackoffService, request.S3BucketName, request.AwsAccountRegion,
                        request.AwsAccountId, request.OnboardingId, configuration.PermissionsTemplateS3Path, configuration.CloudGuardAwsAccountId,
                        configuration.IntelligenceTemplateS3Path, configuration.IntelligenceStackName, configuration.IntelligenceSnsTopicArn, configuration.IntelligenceRulesetsIds, request.UniqueSuffix));
                    }
                    else
                    {
                        await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Enums.Feature.Intelligence, Enums.Status.INACTIVE, "Intelligence disabled", null, null, null)));
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed handling Intelligence. Error={ex}");
                    await _retryAndBackoffService.RunAsync(() => _apiProvider.UpdateOnboardingStatus(new StatusModel(request.OnboardingId, Enums.Feature.Intelligence, Enums.Status.ERROR, ex.Message, null, null, null)));
                }

                // 10. Delete the service account
                await ExecuteStep(new DeleteServiceAccountStep(_apiProvider, _retryAndBackoffService, request.OnboardingId));

                // 11. Write cloudformation lambda custom resoource reponse back to S3 to signal Stack created succesfully.
                Console.WriteLine($"[INFO] About to postback custom resource response success");
                await _retryAndBackoffService.RunAsync(() => customResourceResponseHandler?.PostbackSuccess(), 3);
                Console.WriteLine($"[INFO] Custom resource response successful");

                Console.WriteLine($"[INFO] Finished onboarding workflow successfully - OnboardingId: {request?.OnboardingId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed onboarding process. Error={ex}");
                await TryUpdateStatusFailureInDynamo(request.OnboardingId, ex.ToString(), ex is OnboardingException ? (ex as OnboardingException).Feature : Enums.Feature.None);
                await TryRollback();                
                await TryPostCustomResourceFailureResultToS3(customResourceResponseHandler, ex.ToString());

                throw;
            }
            finally
            {
                await TryCleanUpResources();
            }
        }
    }
}
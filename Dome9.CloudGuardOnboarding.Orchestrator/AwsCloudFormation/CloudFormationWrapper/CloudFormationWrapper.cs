using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    /// <summary>
    /// Singleton that executes operations on an AWS CloudFormation client
    /// </summary>
    public class CloudFormationWrapper : ICloudFormationWrapper, IDisposable
    {
        private readonly AmazonCloudFormationClient _client;
        private static ICloudFormationWrapper _cfnWrapper = null;
        private static readonly object _instanceLock = new object();
        private const int STATUS_POLLING_INTERVAL_MILLISECONDS = 500;
        private bool _disposed = false;

        private CloudFormationWrapper()
        {
            _client = new AmazonCloudFormationClient();
        }
        
        public static ICloudFormationWrapper Get()
        {
            lock (_instanceLock)
            {
                return _cfnWrapper ??= new CloudFormationWrapper();
            }
        }
        
        public async Task<string> CreateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters, 
            Action<string> statusUpdate,
            int executionTimeoutMinutes)
        {           

            int statusPollCount = 0;

            var requestParameters = parameters?.Select(p => new Parameter
                {
                    ParameterKey = p.Key,
                    ParameterValue = p.Value
                }).ToList();
            
            var request = new CreateStackRequest
            {
                StackName = stackName,
                TemplateURL = stackTemplateS3Url,
                TimeoutInMinutes = executionTimeoutMinutes,
                Parameters = requestParameters,
                Capabilities = capabilities
            };

            try
            {
                StackSummary stackSummary = null;
                var response = await _client.CreateStackAsync(request);
                do
                {
                    stackSummary = await GetStackSummaryAsync(feature, stackName);
                    if (!stackSummary.StackStatus.IsFinal())
                    {
                        Console.WriteLine($"[INFO] Waiting {STATUS_POLLING_INTERVAL_MILLISECONDS}ms to poll stack status again, {stackSummary.ToDetailedString()}");
                        statusUpdate($"{stackSummary.StackStatus}");
                        await Task.Delay(STATUS_POLLING_INTERVAL_MILLISECONDS);
                    }

                    if(STATUS_POLLING_INTERVAL_MILLISECONDS * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
                    {
                        Console.WriteLine("[WARNING] Execution timeout exceeded");
                        break;
                    }
                }
                while (stackSummary == null || !stackSummary.StackStatus.IsFinal());
                
                if(stackSummary == null || !stackSummary.StackStatus.IsFinal() || stackSummary.StackStatus.IsError())
                {
                    throw new Exception(stackSummary?.ToDetailedString() ?? "Unable to get stack summary");
                }

                Console.WriteLine($"[INFO] Success, {stackSummary.ToDetailedString()}");
                return response.StackId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(CreateStackAsync)}] Failed to create stack. StackName:'{stackName}', StackTemplateS3Url:'{stackTemplateS3Url}', Error={ex}");
                throw new OnboardingException(ex.Message, feature);
            }
        }

        public async Task<string> UpdateStackAsync(
            Enums.Feature feature, 
            string stackTemplateS3Url, 
            string stackName,
            List<string> capabilities, 
            Dictionary<string, string> parameters)
        {
            var requestParameters = parameters.Select(p => new Parameter
            {
                ParameterKey = p.Key,
                ParameterValue = p.Value
            }).ToList();
            
            var request = new UpdateStackRequest
            {
                StackName = stackName,
                TemplateURL = stackTemplateS3Url,
                Parameters = requestParameters,
                Capabilities = capabilities,
            };
            
            try
            {
                var response = await _client.UpdateStackAsync(request);
                return response.StackId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(UpdateStackAsync)}] Failed to update stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature);
            }
        }

        public async Task<string> GetStackTemplateAsync(Enums.Feature feature, string stackName)
        {
            var request = new GetTemplateRequest
            {
                StackName = stackName,
            };
            
            try
            {
                var response = await _client.GetTemplateAsync(request);
                return response.TemplateBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetStackTemplateAsync)}] Failed to get stack template for stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature);
            }
        }
        
        public async Task<StackSummary> GetStackSummaryAsync(Enums.Feature feature, string stackName)
        {
            try
            {
                var nextToken = "";
                StackSummary stack = null;
                while (stack == null && nextToken != null)
                {
                    var response = await _client.ListStacksAsync();
                    stack = response.StackSummaries.FirstOrDefault(s => s.StackName == stackName);
                    nextToken = response.NextToken;
                }

                return stack;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetStackSummaryAsync)}] Failed to get StackSummary for stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature); ;
            }
        }

        public async Task DeleteStackAsync(Enums.Feature feature, string stackName)
        {
            var request = new DeleteStackRequest
            {
                StackName = stackName,
            };
            
            try
            {
                await _client.DeleteStackAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete stack '{stackName}'. Error={ex}");
                throw new OnboardingException(ex.Message, feature); ;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
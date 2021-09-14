using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public class StackWrapper : IStackWrapper, IDisposable
    {
        private static IStackWrapper _cftWrapper = null;
        private bool _disposed = false;
        private static readonly object padlock = new object();
        private readonly AmazonCloudFormationClient _client;

        private StackWrapper()
        {
            _client = new AmazonCloudFormationClient();
        }
        
        public static IStackWrapper Get()
        {
            lock (padlock)
            {
                return _cftWrapper ??= new StackWrapper();
            }
        }
        
        public async Task<string> CreateStackAsync(string stackTemplateS3Url, string stackName,
            List<string> capabilities, Dictionary<string, string> parameters, int executionTimeoutMinutes = 5)
        {
            int statusPollingIntervalMilliseconds = 500;
            int statusPollCount = 0;

            var requestParameters = parameters.Select(p => new Parameter
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
                    stackSummary = await GetStackSummaryAsync(stackName);
                    if (!stackSummary.StackStatus.IsFinal())
                    {
                        Console.WriteLine($"[INFO] Waiting 500ms to poll stack status again, {stackSummary.ToDetailedString()}");
                        await Task.Delay(statusPollingIntervalMilliseconds);
                    }

                    if(statusPollingIntervalMilliseconds * ++statusPollCount > executionTimeoutMinutes * 60 * 1000)
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
                throw;
            }
        }

        // TODO: was TimeoutInMinutes omitted on purpose? should it be included with a longer duration default value?
        public async Task<string> UpdateStackAsync(string stackTemplateS3Url, string stackName,
            List<string> capabilities, Dictionary<string, string> parameters)
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
                Capabilities = capabilities
            };
            
            try
            {
                var response = await _client.UpdateStackAsync(request);
                return response.StackId;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to update stack. Error={e}");
                throw;
            }
        }

        public async Task<string> GetStackTemplateAsync(string stackName)
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
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get stack. Error={e}");
                throw;
            }
        }
        
        public async Task<StackSummary> GetStackSummaryAsync(string stackName)
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
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] [{nameof(GetStackSummaryAsync)}]Failed to get StackSummary. Error={e}");
                throw;
            }
        }

        public async Task DeleteStackAsync(string stackName)
        {
            var request = new DeleteStackRequest
            {
                StackName = stackName,
            };
            
            try
            {
                await _client.DeleteStackAsync(request);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete stack. Error={e}");
                throw;
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
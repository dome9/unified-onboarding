using System;

namespace Dome9.CloudGuardOnboarding.Orchestrator.AwsCloudFormation;

public class AwsPartition
{
    public string Name { get; set; }
    public string Domain { get; set; }

    private const string AwsGlobalPartitionName = "aws";
    private const string AwsChinaPartitionName = "aws-cn";
    private const string AwsGovPartitionName = "aws-us-gov";
    
    public static AwsPartition GetPartition(string partitionName)
    {
        switch (partitionName)
        {
            case AwsGlobalPartitionName:
                return new AwsPartition { Name = AwsGlobalPartitionName, Domain = "amazonaws.com" };
            case AwsChinaPartitionName:
                return new AwsPartition { Name = AwsChinaPartitionName, Domain = "amazonaws.com.cn" };
            case AwsGovPartitionName:
                return new AwsPartition { Name = AwsGovPartitionName, Domain = "amazonaws.com" };
            default:
                throw new NotSupportedException($"{partitionName} is not supported.");
        }
    }
}
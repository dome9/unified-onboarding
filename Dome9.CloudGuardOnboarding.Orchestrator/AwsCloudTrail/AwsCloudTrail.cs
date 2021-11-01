namespace Falconetix.Model.Entities.Cloud.Trail
{
    public class AwsCloudTrail
    {
        public string HomeRegion { get; set; }
        public string S3BucketName { get; set; }
        public bool IsMultiRegionTrail { get; set; }
        public string TrailArn { get; set; }
        public string ExternalId { get; set; }
        public string BucketRegion { get; set; }
        public bool BuckethasSubscribtions { get; set; }
        public bool BucketIsAccessible { get; set; }

        public AwsCloudTrail()
        {
        }

        public AwsCloudTrail(string homeRegion, bool isMultiRegionTrail, string s3BucketName, string trailArn, string externalId)
        {
            HomeRegion = homeRegion;
            IsMultiRegionTrail = isMultiRegionTrail;
            S3BucketName = s3BucketName;
            TrailArn = trailArn;
            ExternalId = externalId;
            BuckethasSubscribtions = false;
            BucketIsAccessible = false;

        }
    }  
}

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;

namespace LearningPortal.Services;

public static class AwsFactory
{
    public static readonly RegionEndpoint Region = RegionEndpoint.CACentral1;
    private const string ProfileName = "profile4comp306";

    private static readonly AWSCredentials? ProfileCredentials = LoadProfile();

    private static AWSCredentials? LoadProfile()
    {
        var chain = new CredentialProfileStoreChain();
        return chain.TryGetAWSCredentials(ProfileName, out var credentials) ? credentials : null;
    }

    public static AmazonS3Client CreateS3() =>
        ProfileCredentials != null ? new AmazonS3Client(ProfileCredentials, Region) : new AmazonS3Client(Region);

    public static AmazonDynamoDBClient CreateDynamoDb() =>
        ProfileCredentials != null ? new AmazonDynamoDBClient(ProfileCredentials, Region) : new AmazonDynamoDBClient(Region);

    public static AmazonSimpleSystemsManagementClient CreateSsm() =>
        ProfileCredentials != null ? new AmazonSimpleSystemsManagementClient(ProfileCredentials, Region) : new AmazonSimpleSystemsManagementClient(Region);
}
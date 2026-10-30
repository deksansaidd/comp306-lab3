
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace LearningPortal.Services;

public class ConnectionStringProvider
{
    private const string ParameterName =
        "/lab3/db/connection-string";

    public async Task<string> GetConnectionStringAsync()
    {
        using var ssm = AwsFactory.CreateSsm();

        var response = await ssm.GetParameterAsync(
            new GetParameterRequest
            {
                Name = ParameterName,
                WithDecryption = true
            });

        if (string.IsNullOrWhiteSpace(response.Parameter?.Value))
        {
            throw new InvalidOperationException(
                "Database connection string was not found in Parameter Store.");
        }

        return response.Parameter.Value;
    }
}

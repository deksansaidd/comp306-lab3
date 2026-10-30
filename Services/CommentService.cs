
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LearningPortal.Models;

namespace LearningPortal.Services;

public class CommentService
{
    private const string TableName = "Comments";
    private readonly IAmazonDynamoDB _db;

    public CommentService()
    {
        _db = AwsFactory.CreateDynamoDb();
    }

    public async Task<List<CommentItem>> GetCommentsAsync(int resourceId)
    {
        var response = await _db.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "ResourceID = :rid",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":rid"] = new AttributeValue { N = resourceId.ToString() }
            }
        });

        return response.Items.Select(ToComment).ToList();
    }

    public async Task AddCommentAsync(CommentItem comment)
    {
        comment.CommentId = Guid.NewGuid().ToString();
        comment.CreatedUtc = DateTime.UtcNow;

        await _db.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = ToAttributes(comment),
            ConditionExpression =
                "attribute_not_exists(ResourceID) AND attribute_not_exists(CommentID)"
        });
    }

    public async Task<bool> EditCommentAsync(
        int resourceId,
        string commentId,
        int currentUserId,
        string newText)
    {
        var comment = await GetCommentAsync(resourceId, commentId);

        if (comment == null || !comment.CanEdit(currentUserId))
            return false;

        if (string.IsNullOrWhiteSpace(newText))
            return false;

        var cutoff = DateTime.UtcNow.AddHours(-24);

        try
        {
            await _db.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = CommentKey(resourceId, commentId),
                UpdateExpression = "SET CommentText = :text",
                ConditionExpression =
                    "UserID = :uid AND CreatedUtc >= :cutoff",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":text"] = new AttributeValue { S = newText.Trim() },
                    [":uid"] = new AttributeValue { N = currentUserId.ToString() },
                    [":cutoff"] = new AttributeValue { S = cutoff.ToString("O") }
                }
            });

            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<CommentItem?> GetCommentAsync(
        int resourceId, string commentId)
    {
        var response = await _db.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = CommentKey(resourceId, commentId)
        });

        return response.Item == null || response.Item.Count == 0
            ? null
            : ToComment(response.Item);
    }

    private static Dictionary<string, AttributeValue> CommentKey(
        int resourceId, string commentId) => new()
        {
            ["ResourceID"] = new AttributeValue { N = resourceId.ToString() },
            ["CommentID"] = new AttributeValue { S = commentId }
        };

    private static Dictionary<string, AttributeValue> ToAttributes(
        CommentItem comment) => new()
        {
            ["ResourceID"] = new AttributeValue { N = comment.ResourceId.ToString() },
            ["CommentID"] = new AttributeValue { S = comment.CommentId },
            ["UserID"] = new AttributeValue { N = comment.UserId.ToString() },
            ["Username"] = new AttributeValue { S = comment.Username },
            ["CommentText"] = new AttributeValue { S = comment.Text },
            ["CreatedUtc"] = new AttributeValue { S = comment.CreatedUtc.ToString("O") }
        };

    private static CommentItem ToComment(
        Dictionary<string, AttributeValue> item)
    {
        return new CommentItem
        {
            ResourceId = int.Parse(item["ResourceID"].N),
            CommentId = item["CommentID"].S,
            UserId = int.Parse(item["UserID"].N),
            Username = item["Username"].S,
            Text = item["CommentText"].S,
            CreatedUtc = DateTime.Parse(
                item["CreatedUtc"].S,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }
}

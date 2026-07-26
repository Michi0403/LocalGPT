using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;

namespace LocalGPT.Security;

public sealed class HumanApprovalActionFilter(
    string operationKey,
    string title,
    string description,
    string riskLevel,
    string requestedRole,
    bool requiredBeforeCompletion,
    IHumanCollaborationService collaboration,
    IAmbientLocalGptContext ambientContext,
    IHumanApprovalExecutionContext approvalExecutionContext,
    ILogger<HumanApprovalActionFilter> logger) : IAsyncActionFilter
{
    private static readonly JsonSerializerOptions FingerprintJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };
    private static readonly HashSet<string> ConfirmationMemberNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "userConfirmed",
        "UserConfirmedBuild"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var fingerprint = BuildFingerprint(context);
        var correlationId = $"controller:{operationKey}:{fingerprint}";
        var ambient = ambientContext.Current;
        var gate = await collaboration.AuthorizeOrEnqueueAsync(
            new HumanApprovalRequestSpec(
                correlationId,
                operationKey,
                title,
                description,
                riskLevel,
                $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}",
                ambient.ActorDisplayName,
                requestedRole,
                ambient.CouncilRunId,
                ambient.CouncilRound + 1,
                requiredBeforeCompletion,
                IsSensitive: true,
                ParameterFingerprint: fingerprint),
            directHumanConfirmation: false,
            context.HttpContext.RequestAborted).ConfigureAwait(false);

        if (gate.IsDeclined)
        {
            context.Result = new ObjectResult(new
            {
                Status = gate.Status,
                gate.RequestId,
                gate.Message,
                gate.DecisionReason,
                CorrelationId = gate.CorrelationId
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        if (!gate.IsAuthorized || gate.RequestId is not Guid requestId)
        {
            context.Result = new ObjectResult(new
            {
                Status = gate.Status,
                gate.RequestId,
                gate.Message,
                CorrelationId = gate.CorrelationId,
                RetryAfterApproval = true
            })
            {
                StatusCode = StatusCodes.Status202Accepted
            };
            return;
        }

        var profile = await collaboration.GetProfileAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
        ApplyLegacyConfirmationFlags(context.ActionArguments);

        using var approvalScope = approvalExecutionContext.PushHumanApproval(
            profile.Id,
            profile.DisplayName,
            requestId,
            $"Controller approval: {operationKey}",
            gate.CorrelationId,
            ambient.CouncilRunId,
            ambient.CouncilRound,
            ambient.Phase);
        logger.LogInformation(
            "Executing approved controller operation {OperationKey} under approval request {RequestId}; arguments were omitted from logs.",
            operationKey,
            requestId);
        await next().ConfigureAwait(false);
    }

    private static string BuildFingerprint(ActionExecutingContext context)
    {
        var builder = new StringBuilder()
            .Append(context.HttpContext.Request.Method)
            .Append('|')
            .Append(context.HttpContext.Request.Path.Value);

        foreach (var route in context.RouteData.Values.OrderBy(item => item.Key, StringComparer.Ordinal))
            builder.Append('|').Append(route.Key).Append('=').Append(route.Value);

        var serviceArgumentNames = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Services)
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var argument in context.ActionArguments
                     .Where(item => item.Value is not CancellationToken &&
                         !string.Equals(item.Key, "userConfirmed", StringComparison.OrdinalIgnoreCase) &&
                         !serviceArgumentNames.Contains(item.Key))
                     .OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.Append('|').Append(argument.Key).Append('=');
            AppendFingerprintValue(builder, argument.Value);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static void AppendFingerprintValue(StringBuilder builder, object? value)
    {
        try
        {
            var node = value is null
                ? null
                : JsonSerializer.SerializeToNode(value, value.GetType(), FingerprintJsonOptions);
            RemoveConfirmationMembers(node);
            builder.Append(node?.ToJsonString(FingerprintJsonOptions) ?? "null");
        }
        catch
        {
            builder.Append(value?.GetType().FullName ?? "null");
        }
    }

    private static void RemoveConfirmationMembers(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var propertyName in jsonObject.Select(item => item.Key).ToList())
            {
                if (ConfirmationMemberNames.Contains(propertyName))
                {
                    jsonObject.Remove(propertyName);
                    continue;
                }
                RemoveConfirmationMembers(jsonObject[propertyName]);
            }
            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
                RemoveConfirmationMembers(child);
        }
    }

    private static void ApplyLegacyConfirmationFlags(IDictionary<string, object?> actionArguments)
    {
        foreach (var argumentName in actionArguments.Keys.ToList())
        {
            if (string.Equals(argumentName, "userConfirmed", StringComparison.OrdinalIgnoreCase))
            {
                actionArguments[argumentName] = true;
                continue;
            }

            var argument = actionArguments[argumentName];
            if (argument is null)
                continue;

            SetBooleanProperty(argument, "UserConfirmed", true);
            if (GetBooleanProperty(argument, "BuildAfterGeneration"))
                SetBooleanProperty(argument, "UserConfirmedBuild", true);
        }
    }

    private static bool GetBooleanProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.PropertyType == typeof(bool) && property.GetValue(instance) is true;
    }

    private static void SetBooleanProperty(object instance, string propertyName, bool value)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property?.CanWrite == true && property.PropertyType == typeof(bool))
            property.SetValue(instance, value);
    }
}

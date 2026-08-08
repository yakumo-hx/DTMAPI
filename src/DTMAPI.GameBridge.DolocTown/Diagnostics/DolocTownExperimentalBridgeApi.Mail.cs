using System;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        public MailItemDeliveryResult SendItemMail(
            IManifest owner,
            MailItemDeliveryRequest request)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            request ??= new MailItemDeliveryRequest();
            string itemId = (request.ItemId ?? string.Empty).Trim();
            int count = Math.Max(0, request.Count);
            string templateName =
                FirstText(request.TemplateName, "send_item_template");
            var result = new MailItemDeliveryResult
            {
                ItemId = itemId,
                RequestedCount = count,
                EmailName = request.EmailName ?? string.Empty,
                TemplateName = templateName
            };
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return MailItemDeliveryFailed(
                    result,
                    "invalid-request",
                    "Invalid item id or count.");
            }

            try
            {
                Type? dolocApi =
                    ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "missing-dolocapi",
                        "DolocAPI is not available.");
                }

                MethodInfo? queryItemProto = dolocApi.GetMethod(
                    "QueryItemProto",
                    BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = { itemId, null };
                if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) ||
                    !found ||
                    queryArgs[1] == null)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "unknown-item",
                        "Item is not present in DolocConfig.Tables.TbItem.");
                }

                object proto = queryArgs[1]!;
                result.DisplayName =
                    FirstText(ReadStringMember(proto, "Title"), itemId);
                IContentItemInfo? sourceInfo =
                    runtime.GetIndexedContentItem(itemId);
                if (sourceInfo != null)
                {
                    result.SourceId = sourceInfo.SourceId;
                    result.SourceEnabled = sourceInfo.Enabled;
                    result.SourceEnablementKnown =
                        sourceInfo.EnablementKnown;
                }
                else
                {
                    result.SourceEnablementKnown = false;
                }

                string requiredSourceId =
                    (request.RequiredSourceId ?? string.Empty).Trim();
                if (request.RequireEnabledContentSource &&
                    sourceInfo == null)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "missing-content-source",
                        "Mail item " + itemId +
                        " has no indexed DTMAPI/Workshop content source.");
                }
                if (!string.IsNullOrWhiteSpace(requiredSourceId) &&
                    (sourceInfo == null ||
                     !sourceInfo.SourceId.Equals(
                         requiredSourceId,
                         StringComparison.OrdinalIgnoreCase)))
                {
                    return MailItemDeliveryFailed(
                        result,
                        "source-mismatch",
                        "Mail item " + itemId + " source is " +
                        (sourceInfo?.SourceId ?? "none") +
                        ", expected " + requiredSourceId + ".");
                }
                if (sourceInfo != null && !sourceInfo.Enabled)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "source-disabled",
                        "Item source is disabled by Doloc Town's official Mod UI or Steam Workshop enablement state.");
                }
                if (request.RequireEnabledContentSource &&
                    sourceInfo != null &&
                    !sourceInfo.EnablementKnown)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "source-enable-state-unknown",
                        "Item source enablement state is unknown for " +
                        sourceInfo.SourceId + ".");
                }
                if (!TryGenerateNativeItem(
                        itemId,
                        count,
                        out _,
                        out string itemReason,
                        out string itemMessage))
                {
                    return MailItemDeliveryFailed(
                        result,
                        "attachment-" + itemReason,
                        "Mail attachment cannot be generated before native delivery: " +
                        itemMessage);
                }

                MethodInfo? countItem = dolocApi.GetMethod(
                    "CountItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) },
                    null);
                result.BackpackCount = InvokeInt(
                    countItem,
                    null,
                    new object?[] { itemId, false },
                    0);
                result.PendingMailCount =
                    CountPendingUnacceptedItemMail(dolocApi, itemId);
                int pendingMailBeforeSend = result.PendingMailCount;

                if (request.SkipIfAlreadyOwned &&
                    result.BackpackCount >= count)
                {
                    result.Success = true;
                    result.Skipped = true;
                    result.Message =
                        "Skipped mail delivery; backpack already contains " +
                        result.BackpackCount + " " + itemId + ".";
                    LogMailDeliveryResult(ownerId, result);
                    runtime.SetHookStatus(
                        "Mail.ItemDelivery",
                        "skipped",
                        "DolocAPI.CountItem",
                        result.Message);
                    return result;
                }

                if (request.PreventDuplicatePendingMail &&
                    result.PendingMailCount >= count)
                {
                    result.Success = true;
                    result.Skipped = true;
                    result.Message =
                        "Skipped mail delivery; an unclaimed item mail already contains " +
                        result.PendingMailCount + " " + itemId + ".";
                    LogMailDeliveryResult(ownerId, result);
                    runtime.SetHookStatus(
                        "Mail.ItemDelivery",
                        "skipped",
                        "EmailManager.emails",
                        result.Message);
                    return result;
                }

                MethodInfo? sendItemAsEmail = dolocApi.GetMethod(
                    "SendItemAsEmail",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(string)
                    },
                    null);
                if (sendItemAsEmail == null)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "missing-native-method",
                        "DolocAPI.SendItemAsEmail was not found.");
                }

                object? sent = sendItemAsEmail.Invoke(
                    null,
                    new object?[]
                    {
                        itemId,
                        count,
                        string.IsNullOrWhiteSpace(request.EmailName)
                            ? null
                            : request.EmailName,
                        string.IsNullOrWhiteSpace(request.Content)
                            ? null
                            : request.Content,
                        string.IsNullOrWhiteSpace(request.Sender)
                            ? null
                            : request.Sender,
                        templateName
                    });
                if (!(sent is bool ok && ok))
                {
                    return MailItemDeliveryFailed(
                        result,
                        "native-rejected",
                        "DolocAPI.SendItemAsEmail returned false.");
                }

                result.PendingMailCount =
                    CountPendingUnacceptedItemMail(dolocApi, itemId);
                if (result.PendingMailCount <
                    pendingMailBeforeSend + count)
                {
                    return MailItemDeliveryFailed(
                        result,
                        "missing-attachment-after-send",
                        "Native mail send returned true but no unclaimed " +
                        itemId + " attachment was observed. before=" +
                        pendingMailBeforeSend + ", after=" +
                        result.PendingMailCount + ".");
                }

                result.Sent = true;
                result.Success = true;
                result.Message =
                    "Sent " + count + " " + itemId +
                    " through native DolocAPI.SendItemAsEmail template=" +
                    templateName + ".";
                LogMailDeliveryResult(ownerId, result);
                runtime.SetHookStatus(
                    "Mail.ItemDelivery",
                    "experimental",
                    "DolocAPI.SendItemAsEmail",
                    result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Mail item delivery failed for " + itemId + ".",
                    error.ToString());
                return MailItemDeliveryFailed(
                    result,
                    error.GetType().Name,
                    error.Message);
            }
        }

        BridgeFeatureStatus IMailDeliveryApi.GetStatus() =>
            new BridgeFeatureStatus(
                "experimental",
                "Uses native DolocAPI.SendItemAsEmail with backpack-count and pending-unclaimed-mail duplicate guards. The current game build ignores custom email title/content/sender parameters for item mail, so DTMAPI treats this as a template-based delivery bridge.");

        private static int CountPendingUnacceptedItemMail(
            Type dolocApi,
            string itemId)
        {
            if (dolocApi == null ||
                string.IsNullOrWhiteSpace(itemId))
                return 0;

            object? archive =
                ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData =
                archive == null ? null : ReadMember(archive, "farmData");
            object? emailManager =
                farmData == null
                    ? null
                    : ReadMember(farmData, "emailManager");
            object? emails =
                emailManager == null
                    ? null
                    : ReadMember(emailManager, "emails");
            int count = 0;
            foreach (object email in EnumerateObjects(emails))
            {
                object? attaches =
                    ReadMember(email, "emailAttaches");
                foreach (object attach in EnumerateObjects(attaches))
                {
                    if (ReadBoolMember(attach, "IsAccept", false) ||
                        ReadBoolMember(attach, "isAccept", false))
                        continue;

                    object? reward = ReadMember(attach, "reward");
                    if (reward == null)
                        continue;
                    string rewardItemId =
                        ReadStringMember(reward, "itemName");
                    if (!itemId.Equals(
                            rewardItemId,
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    count += Math.Max(
                        0,
                        ReadIntMember(reward, "itemCount", 0));
                }
            }
            return count;
        }

        private void LogMailDeliveryResult(
            string ownerId,
            MailItemDeliveryResult result)
        {
            runtime.RuntimeMonitor.Log(
                "Mail item delivery owner=" + ownerId +
                " item=" + result.ItemId +
                " requested=" + result.RequestedCount +
                " sent=" + result.Sent +
                " skipped=" + result.Skipped +
                " backpack=" + result.BackpackCount +
                " pendingMail=" + result.PendingMailCount +
                " template=" + result.TemplateName +
                " source=" + FirstText(result.SourceId, "none") +
                " sourceEnabled=" + result.SourceEnabled +
                " sourceKnown=" + result.SourceEnablementKnown +
                " success=" + result.Success +
                " reason=" + result.FailureReason + ".");
        }

        private static MailItemDeliveryResult MailItemDeliveryFailed(
            MailItemDeliveryResult result,
            string reason,
            string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }
    }
}

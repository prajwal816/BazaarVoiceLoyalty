# BazaarVoice Loyalty Integration — Troubleshooting Guide

## Common Issues

### 1. Blob Trigger Not Firing

**Symptoms**: Files uploaded to `incoming/fromBazaarVoice/` are not processed.

**Checks**:
- Verify the file has a `.gz` extension
- Check `AzureWebJobsStorage` connection string is valid
- Check `BazaarVoiceStorageConnection` points to the correct storage account
- Verify the blob container name is `bazaarvoice` (case-sensitive)
- Check Application Insights for errors in the `BazaarVoiceBlobTrigger` function
- Verify the Function App is running: `az functionapp show --name func-bazaarvoice-blobprocessor-{env}`

**Resolution**:
```powershell
# Restart the Function App
az functionapp restart --name func-bazaarvoice-blobprocessor-dev --resource-group rg-bazaarvoice-loyalty-dev
```

### 2. XML Parsing Failures

**Symptoms**: Blob trigger fires but records are not published to Service Bus.

**Checks**:
- Check Application Insights for `ProcessingException` with "XML parsing failed"
- Verify the XML element names in `XmlParserService.cs` match the actual BazaarVoice schema
- Download the extracted XML from the `loyalty/` folder and validate its structure

**Resolution**: Update the XML element names in `XmlParserService.cs` to match the actual schema.

### 3. Checkpoint/Restart Issues

**Symptoms**: Processing restarts from the beginning or skips records.

**Checks**:
- Check the `checkpoint/` folder for `_checkpoint.json` files
- Verify the checkpoint JSON contains valid `LastProcessedIndex` and `FileName`

**Resolution**:
```powershell
# Clear a stuck checkpoint manually
az storage blob delete --container-name bazaarvoice --name "checkpoint/<filename>_checkpoint.json" --account-name stbazaarvoicedev
```

### 4. Service Bus Messages Not Being Processed

**Symptoms**: Messages accumulate in the topic subscription.

**Checks**:
- Verify Function App #2 is running
- Check `ServiceBusConnection` in Function App #2 settings
- Verify subscription name matches `sbs-bazaarvoice-loyalty`
- Check for exceptions in Application Insights

**Resolution**:
```powershell
# Check active message count
az servicebus topic subscription show --namespace-name sb-bazaarvoice-loyalty-dev --topic-name sbt-bazaarvoice-records --name sbs-bazaarvoice-loyalty --resource-group rg-bazaarvoice-loyalty-dev --query "countDetails"
```

### 5. Dead Letter Queue Messages

**Symptoms**: Messages appearing in the Dead Letter Queue.

**Common Causes**:
- `LookupNotFoundException`: Customer email not found in Cosmos DB
- `ApiCommunicationException`: Common Loyalty API or Annex Cloud API unreachable
- `ProcessingException`: Points assignment failed

**Investigation**:
```powershell
# Peek at DLQ messages using Service Bus Explorer or Azure CLI
az servicebus topic subscription show --namespace-name sb-bazaarvoice-loyalty-dev \
    --topic-name sbt-bazaarvoice-records \
    --name sbs-bazaarvoice-loyalty \
    --resource-group rg-bazaarvoice-loyalty-dev \
    --query "countDetails.deadLetterMessageCount"
```

**Resolution**:
1. Check the DLQ reason in the message system properties
2. For `LookupNotFoundException`: Verify the email exists in Cosmos DB
3. For API errors: Check API connectivity and credentials
4. After fixing, resubmit messages from DLQ using Service Bus Explorer

### 6. Cosmos DB Lookup Failures

**Symptoms**: `LookupNotFoundException` in Application Insights.

**Checks**:
- Verify the `CommonLoyaltyApiBaseUrl` is correct
- Check APIM subscription key validity
- Verify the Cosmos DB query matches the document structure
- Test the API endpoint directly: `GET {baseUrl}/api/members/search?email=test@example.com`

### 7. Annex Cloud Action Not Found

**Symptoms**: `ApiCommunicationException` with "Review & Rating action not found".

**Checks**:
- Verify `AnnexCloudApiBaseUrl` is correct (default: `https://s15.socialannex.net`)
- Check if the action name "Review & Rating" exists in Annex Cloud
- Test directly: `GET https://s15.socialannex.net/api/3.0/actions?action_name=Review%20%26%20Rating`
- Check if API key/authentication is required

### 8. Points Assignment Failures

**Symptoms**: `ProcessingException` with "Points assignment failed".

**Checks**:
- Verify the points assignment API endpoint is correct
- Check the member Annex ID is valid
- Verify the action code from Step 4 is valid
- Check APIM logs for the failed request

## Monitoring Queries (Application Insights / KQL)

### Failed Function Executions
```kusto
requests
| where success == false
| where name startswith "BazaarVoice"
| order by timestamp desc
| take 50
```

### Dead-Lettered Records by Email
```kusto
exceptions
| where type == "BazaarVoice.Common.Exceptions.LookupNotFoundException"
| extend email = tostring(customDimensions.Email)
| summarize count() by email
| order by count_ desc
```

### Processing Throughput
```kusto
customMetrics
| where name in ("BlobsProcessed", "RecordsPublished", "PointsAssigned")
| summarize sum(value) by name, bin(timestamp, 1h)
| render timechart
```

### End-to-End Latency
```kusto
requests
| where name startswith "BlobProcessor"
| project operationId = operation_Id, blobDuration = duration, blobTimestamp = timestamp
| join kind=inner (
    requests
    | where name == "MessageProcessor"
    | project operationId = operation_Id, msgDuration = duration, msgTimestamp = timestamp
) on operationId
| extend e2eLatency = datetime_diff('second', msgTimestamp, blobTimestamp)
| summarize avg(e2eLatency), max(e2eLatency) by bin(blobTimestamp, 1h)
```

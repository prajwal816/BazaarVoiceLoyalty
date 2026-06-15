# BazaarVoice Loyalty Integration — Architecture

## Overview

This system integrates BazaarVoice product review data with the Annex Cloud loyalty platform
to automatically award loyalty points to customers who submit reviews.

## Data Flow

```
BazaarVoice → Axway → Azure Blob Storage (.gz) → Function App #1 → Service Bus → Function App #2 → Loyalty Points
```

### Step-by-Step Flow

| Step | Component | Action |
|------|-----------|--------|
| 0 | BazaarVoice → Axway | `.gz` file delivered to `incoming/fromBazaarVoice/` blob container |
| 1 | **Function App #1** (Blob Trigger) | Extracts `.gz` → XML, writes to `loyalty/` and `ecomm/` folders |
| 2 | **Function App #1** | Parses XML into individual review records, publishes each to Service Bus topic |
| 3 | **Function App #2** (Service Bus Trigger) | Looks up member Annex ID by email via Common Loyalty API → Cosmos DB |
| 4 | **Function App #2** | Gets "Review & Rating" action details from Annex Cloud (cached) |
| 5 | **Function App #2** | Assigns manual loyalty points using the action code via Common Loyalty API |

## Components

### Function App #1 — Blob Processor (`func-bazaarvoice-blobprocessor-{env}`)

- **Trigger**: Blob trigger on `bazaarvoice/incoming/fromBazaarVoice/{fileName}`
- **Responsibilities**:
  - GZip decompression
  - XML extraction and dual-folder write (loyalty + eComm)
  - XML parsing into individual records
  - Checkpoint/restart for failure recovery
  - Sequential record publishing to Service Bus topic
  - File archiving (7-day retention via lifecycle policy)

### Function App #2 — Message Processor (`func-bazaarvoice-msgprocessor-{env}`)

- **Trigger**: Service Bus topic subscription (`sbt-bazaarvoice-records` / `sbs-bazaarvoice-loyalty`)
- **Responsibilities**:
  - Member lookup by email via APIM → Cosmos DB
  - Review & Rating action retrieval from Annex Cloud (with caching)
  - Manual loyalty points assignment
  - Dead-letter queue handling for unfound members

### Blob Storage Structure

```
bazaarvoice/
├── incoming/fromBazaarVoice/   ← Landing zone (Axway writes .gz files here)
├── loyalty/                    ← Extracted XML files
├── loyalty/archive/            ← Archived files (7-day retention)
├── ecomm/                      ← eComm folder (HCL Commerce reads)
└── checkpoint/                 ← Checkpoint/restart state
```

### Service Bus

- **Namespace**: `sb-bazaarvoice-loyalty-{env}`
- **Topic**: `sbt-bazaarvoice-records` (duplicate detection enabled)
- **Subscription**: `sbs-bazaarvoice-loyalty` (maxDeliveryCount=10, dead-letter enabled)

## Resilience Patterns

| Pattern | Implementation |
|---------|---------------|
| **Checkpoint/Restart** | Blob-persisted state tracks last published record index |
| **Dead-Letter Queue** | Failed messages after 10 retries go to DLQ for investigation |
| **Circuit Breaker** | Polly circuit breaker on HTTP clients (5 failures → 30s cooldown) |
| **Retry with Backoff** | Exponential backoff retry (3 attempts) on transient HTTP errors |
| **Caching** | In-memory cache for Annex Cloud action details (60-minute TTL) |
| **Poison Blob Handling** | Azure Functions poison blob threshold set to 3 |

## Observability

- **Application Insights**: Full telemetry with custom metrics and events
- **Structured Logging**: Correlation IDs link blob processing to message processing
- **Custom Metrics**: `BlobsProcessed`, `RecordsPublished`, `PointsAssigned`
- **Custom Events**: `ReviewProcessedSuccessfully` with email/review details

## Security

- **Managed Identity**: System-assigned identity on both Function Apps (recommended for production)
- **Key Vault**: All secrets stored in Azure Key Vault with RBAC authorization
- **TLS 1.2**: Enforced on all Azure resources
- **No Public Blob Access**: Storage account configured with `allowBlobPublicAccess: false`

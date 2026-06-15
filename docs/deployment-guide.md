# BazaarVoice Loyalty Integration — Deployment Guide

## Prerequisites

- Azure CLI (`az`) installed and logged in
- .NET 6 SDK installed
- Azure subscription with sufficient permissions
- Access to the target resource group

## Infrastructure Deployment

### 1. Deploy Azure Resources (Bicep)

```powershell
# Navigate to the infrastructure scripts directory
cd infrastructure/scripts

# Deploy to dev environment
.\deploy.ps1 -Environment dev

# Deploy to UAT
.\deploy.ps1 -Environment uat

# Deploy to Production
.\deploy.ps1 -Environment prod -Location westus2
```

### 2. Update Key Vault Secrets

After infrastructure deployment, update the placeholder secrets in Key Vault:

```powershell
$env = "dev"
$kvName = "kv-bazaarvoice-$env"

# Storage connection string
az keyvault secret set --vault-name $kvName --name "BazaarVoiceStorageConnection" --value "<actual-connection-string>"

# Service Bus connection string
az keyvault secret set --vault-name $kvName --name "ServiceBusConnection" --value "<actual-connection-string>"

# Cosmos DB connection string
az keyvault secret set --vault-name $kvName --name "CosmosDbConnectionString" --value "<actual-connection-string>"

# APIM subscription key
az keyvault secret set --vault-name $kvName --name "CommonLoyaltyApiSubscriptionKey" --value "<actual-key>"

# Annex Cloud API key (if required)
az keyvault secret set --vault-name $kvName --name "AnnexCloudApiKey" --value "<actual-key>"
```

## Application Deployment

### 1. Build the Solution

```powershell
dotnet build BazaarVoice.Loyalty.Integration.sln --configuration Release
```

### 2. Run Tests

```powershell
dotnet test BazaarVoice.Loyalty.Integration.sln --configuration Release --verbosity normal
```

### 3. Deploy Function App #1 (Blob Processor)

```powershell
cd src/BazaarVoice.Functions.BlobProcessor
dotnet publish --configuration Release --output ./publish

# Deploy using Azure CLI
az functionapp deployment source config-zip \
    --resource-group rg-bazaarvoice-loyalty-dev \
    --name func-bazaarvoice-blobprocessor-dev \
    --src ./publish.zip
```

### 4. Deploy Function App #2 (Message Processor)

```powershell
cd src/BazaarVoice.Functions.MessageProcessor
dotnet publish --configuration Release --output ./publish

# Deploy using Azure CLI
az functionapp deployment source config-zip \
    --resource-group rg-bazaarvoice-loyalty-dev \
    --name func-bazaarvoice-msgprocessor-dev \
    --src ./publish.zip
```

## Local Development

### 1. Start Azurite (Storage Emulator)

```powershell
azurite --silent --location ./azurite-data
```

### 2. Update local.settings.json

Update the `local.settings.json` files in both Function App projects with your
local development connection strings.

### 3. Run Function App #1

```powershell
cd src/BazaarVoice.Functions.BlobProcessor
func start
```

### 4. Run Function App #2

```powershell
cd src/BazaarVoice.Functions.MessageProcessor
func start --port 7072
```

## Post-Deployment Verification

1. **Check Application Insights**: Verify telemetry is flowing
2. **Upload a test `.gz` file**: Drop a test file into `incoming/fromBazaarVoice/`
3. **Monitor Service Bus**: Verify messages appear in the topic
4. **Check Dead Letter Queue**: Ensure no unexpected DLQ messages
5. **Verify Cosmos DB lookups**: Confirm member search returns results
6. **Check archive folder**: Verify processed files are archived

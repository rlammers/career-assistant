targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = 'australiaeast'

@description('Stable prefix used for resource names.')
param namePrefix string = 'career-assistant-demo'

@description('Log Analytics retention in days.')
@minValue(30)
param logRetentionDays int = 30

var suffix = uniqueString(subscription().subscriptionId, resourceGroup().id)
var compactPrefix = replace(namePrefix, '-', '')
var registryName = take('${compactPrefix}${suffix}', 50)
var storageAccountName = take('${compactPrefix}${suffix}', 24)
var environmentName = '${namePrefix}-env'
var identityName = '${namePrefix}-pull'
var workspaceName = '${namePrefix}-logs'
var fileShareName = 'app-data'
var environmentStorageName = 'app-data'
var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource imagePullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, imagePullIdentity.id, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    principalId: imagePullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    retentionInDays: logRetentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource fileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: fileShareName
  properties: {
    accessTier: 'TransactionOptimized'
    enabledProtocols: 'SMB'
    shareQuota: 5
  }
}

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource environmentStorage 'Microsoft.App/managedEnvironments/storages@2025-01-01' = {
  parent: environment
  name: environmentStorageName
  properties: {
    azureFile: {
      accountKey: storage.listKeys().keys[0].value
      accountName: storage.name
      accessMode: 'ReadWrite'
      shareName: fileShare.name
    }
  }
}

output registryName string = registry.name
output registryLoginServer string = registry.properties.loginServer
output environmentName string = environment.name
output environmentStorageName string = environmentStorage.name
output imagePullIdentityName string = imagePullIdentity.name
output imagePullIdentityResourceId string = imagePullIdentity.id
output storageAccountName string = storage.name
output fileShareName string = fileShare.name

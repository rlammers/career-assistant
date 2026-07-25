targetScope = 'resourceGroup'

@description('Azure region for the Azure SQL resources.')
param location string = 'australiaeast'

@description('Stable prefix used for resource names.')
param namePrefix string = 'career-assistant-demo'

@description('SQL administrator login used only to provision and migrate the disposable database.')
@minLength(1)
param sqlAdministratorLogin string

@description('SQL administrator password. Supply this only as a secure deployment parameter.')
@secure()
param sqlAdministratorPassword string

@description('Name of the disposable application database.')
@minLength(1)
param databaseName string = 'careerassistant'

var suffix = uniqueString(subscription().subscriptionId, resourceGroup().id)
var compactPrefix = replace(namePrefix, '-', '')
var sqlServerName = take('${compactPrefix}sql${suffix}', 63)

resource sqlServer 'Microsoft.Sql/servers@2025-01-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
}

// Container Apps has no fixed egress IP without the private-networking work
// deliberately excluded from this increment. This special Azure SQL rule is
// therefore the narrowest public rule that allows the Container App to connect.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2025-01-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2025-01-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60
    freeLimitExhaustionBehavior: 'AutoPause'
    maxSizeBytes: 34359738368
    // Bicep has no decimal literal syntax; ARM accepts 0.5 vCore here.
    minCapacity: json('0.5')
    requestedBackupStorageRedundancy: 'Local'
    useFreeLimit: true
  }
}

output sqlServerName string = sqlServer.name
output sqlServerFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name

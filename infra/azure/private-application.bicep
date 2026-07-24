targetScope = 'resourceGroup'

@description('Azure region for the Container App.')
param location string = 'australiaeast'

@description('Stable prefix used for resource names.')
param namePrefix string = 'career-assistant-demo'

@description('Existing Container Apps environment name from foundation.bicep.')
param environmentName string

@description('Existing environment storage link name from foundation.bicep.')
param environmentStorageName string

@description('Existing ACR name from foundation.bicep.')
param registryName string

@description('Existing user-assigned image-pull identity name from foundation.bicep.')
param imagePullIdentityName string

@description('Immutable frontend image reference, including a commit-specific tag or digest.')
param frontendImage string

@description('Immutable backend image reference, including a commit-specific tag or digest.')
param backendImage string

@description('Microsoft Entra tenant ID accepted by the backend API.')
@minLength(1)
param authenticationTenantId string

@description('Microsoft Entra API application client ID.')
@minLength(1)
param authenticationClientId string

@description('Audience expected in Microsoft Entra access tokens.')
@minLength(1)
param authenticationAudience string

@description('Issuer expected in Microsoft Entra access tokens.')
@minLength(1)
param authenticationIssuer string

@description('Microsoft Entra application role required by the owner-only deployment.')
@minLength(1)
param authenticationRequiredAppRole string

module application './application.bicep' = {
  name: 'private-application'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    environmentStorageName: environmentStorageName
    registryName: registryName
    imagePullIdentityName: imagePullIdentityName
    frontendImage: frontendImage
    backendImage: backendImage
    authenticationTenantId: authenticationTenantId
    authenticationClientId: authenticationClientId
    authenticationAudience: authenticationAudience
    authenticationIssuer: authenticationIssuer
    authenticationRequiredAppRole: authenticationRequiredAppRole
    migrateOnStartup: true
  }
}

output applicationName string = application.outputs.applicationName
output publicUrl string = application.outputs.publicUrl

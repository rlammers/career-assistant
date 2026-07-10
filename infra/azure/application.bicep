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

var appName = '${namePrefix}-app'

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: environmentName
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

resource imagePullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: imagePullIdentityName
}

resource app 'Microsoft.App/containerApps@2025-01-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${imagePullIdentity.id}': {}
    }
  }
  properties: {
    environmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: false
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          identity: imagePullIdentity.id
          server: registry.properties.loginServer
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: frontendImage
          env: [
            {
              name: 'API_UPSTREAM'
              value: 'http://localhost:8081'
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 20
              periodSeconds: 30
            }
          ]
        }
        {
          name: 'backend'
          image: backendImage
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8081'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              value: 'Data Source=/app/data/CareerAssistant.db'
            }
            {
              name: 'AI__Provider'
              value: 'Mock'
            }
            {
              name: 'Database__MigrateOnStartup'
              value: 'true'
            }
            {
              name: 'Demo__Enabled'
              value: 'true'
            }
            {
              name: 'Demo__MaxJobs'
              value: '100'
            }
            {
              name: 'Demo__MaxAnalyses'
              value: '200'
            }
            {
              name: 'ForwardedHeaders__Enabled'
              value: 'true'
            }
            {
              name: 'Logging__LogLevel__Default'
              value: 'Information'
            }
            {
              name: 'Logging__LogLevel__Microsoft.AspNetCore'
              value: 'Warning'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          volumeMounts: [
            {
              mountPath: '/app/data'
              volumeName: 'app-data'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8081
                scheme: 'HTTP'
              }
              initialDelaySeconds: 20
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'app-data'
          storageName: environmentStorageName
          storageType: 'AzureFile'
          mountOptions: 'dir_mode=0770,file_mode=0660,uid=1654,gid=1654'
        }
      ]
    }
  }
}

output applicationName string = app.name
output publicUrl string = 'https://${app.properties.configuration.ingress.fqdn}'
